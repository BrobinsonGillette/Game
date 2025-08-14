using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Collections;
using UnityEngine;

public class AttackHitMainbox : MonoBehaviour
{
    [Header("Hitbox Settings")]
    [SerializeField] GameObject hitboxPrefab;
    [SerializeField] GameObject rotationPoint;
    private int Length = 1;
    private int Width = 1;  // Added width parameter
    private float damage = 10;
    private Team ownerTeam = Team.player;
    private float lifetime = 2f;
    private bool isActivated = false;
    public bool fistTargetHit = false;
    private TargetType targetType = TargetType.SingleTarget;  // Added target type tracking

    [Header("Visual Settings")]
    public Color previewColor = Color.yellow;
    public Color activeColor = Color.red;
    public float previewAlpha = 0.5f;
    public float activeAlpha = 1f;
    [SerializeField] Transform groundCheck;
    [SerializeField] private Renderer hitboxRenderer;

    private bool hasHit = false;
    private Collider hitboxCollider;
    private HashSet<Char> hitTargets = new HashSet<Char>();
    private MouseHandler mouseHandler;
    private MapMaker mapMaker;

    public List<GameObject> hitboxObjects { get; private set; } = new List<GameObject>();
    public List<AttackHitbox> hitboxes { get; private set; } = new List<AttackHitbox>();

    // Store the tiles in the attack area for visualization
    private List<HexTile> targetTiles = new List<HexTile>();

    // Properties for external access
    public Team OwnerTeam => ownerTeam;
    public bool IsActivated => isActivated;

    private void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }

        mouseHandler = MouseHandler.instance;
        mapMaker = MapMaker.instance;
    }

    // Updated initialization to include width and target type
    public void InitializeForPreview(float hitboxDamage, Team team, float hitboxLifetime, int length, int width, TargetType type)
    {
        damage = hitboxDamage;
        ownerTeam = team;
        lifetime = hitboxLifetime;
        isActivated = false;
        this.Length = length;
        this.Width = width;
        this.targetType = type;
        SetupVisual(previewColor, previewAlpha);
        hasHit = false;
        fistTargetHit = false;

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }

        // Create hitboxes based on width and length
        CreateHitboxArea();
    }

    private void CreateHitboxArea()
    {
        // Clear existing hitboxes
        ClearHitboxes();

        if (mapMaker == null || mouseHandler == null) return;

        // Get player's current tile
        Vector2Int playerHexCoord = mapMaker.WorldToHexPosition(transform.position);
        HexTile playerTile = mapMaker.GetHexTile(playerHexCoord);
        if (playerTile == null) return;

        // Get tiles in the attack area (cone/line with width)
        targetTiles = GetAttackAreaTiles(playerTile, Length, Width);

        // Create hitbox for each tile
        for (int i = 0; i < targetTiles.Count; i++)
        {
            HexTile tile = targetTiles[i];
            if (tile == playerTile) continue; // Skip player's tile

            CreateHitboxAtTile(tile, i);
        }
    }

    private List<HexTile> GetAttackAreaTiles(HexTile startTile, int length, int width)
    {
        List<HexTile> tiles = new List<HexTile>();

        if (mouseHandler == null || mapMaker == null) return tiles;

        // Get the main direction towards mouse
        Vector2Int mouseHexCoord = mapMaker.WorldToHexPosition(mouseHandler.worldMousePos);
        Vector2Int mainDirection = GetHexDirection(startTile.coordinates, mouseHexCoord);

        // If width is 1, it's just a line
        if (width <= 1)
        {
            return GetTilesInLineToMouse(startTile, length);
        }

        // For width > 1, create a cone or wide area
        // Get perpendicular directions for width
        Vector2Int[] perpendicularDirs = GetPerpendicularDirections(mainDirection);

        // For each distance along the length
        for (int dist = 1; dist <= length; dist++)
        {
            // Calculate how wide the attack should be at this distance
            int currentWidth = width;

            // Center tile in the direction
            Vector2Int centerCoord = startTile.coordinates + (mainDirection * dist);
            HexTile centerTile = mapMaker.GetHexTile(centerCoord);

            if (centerTile != null && centerTile.IsWalkable)
            {
                tiles.Add(centerTile);
            }

            // Add tiles to the sides based on width
            for (int w = 1; w <= (currentWidth - 1) / 2; w++)
            {
                // Add tiles on both sides
                foreach (Vector2Int perpDir in perpendicularDirs)
                {
                    Vector2Int sideCoord = centerCoord + (perpDir * w);
                    HexTile sideTile = mapMaker.GetHexTile(sideCoord);

                    if (sideTile != null && sideTile.IsWalkable)
                    {
                        tiles.Add(sideTile);
                    }
                }
            }
        }

        return tiles;
    }

    private Vector2Int[] GetPerpendicularDirections(Vector2Int mainDirection)
    {
        // Hex directions in axial coordinates
        Vector2Int[] hexDirections = new Vector2Int[]
        {
            new Vector2Int(1, 0),   // East
            new Vector2Int(1, -1),  // Southeast
            new Vector2Int(0, -1),  // Southwest
            new Vector2Int(-1, 0),  // West
            new Vector2Int(-1, 1),  // Northwest
            new Vector2Int(0, 1)    // Northeast
        };

        // Find the index of the main direction
        int mainIndex = -1;
        for (int i = 0; i < hexDirections.Length; i++)
        {
            if (hexDirections[i] == mainDirection)
            {
                mainIndex = i;
                break;
            }
        }

        if (mainIndex == -1)
        {
            // If exact match not found, find closest
            float maxDot = float.MinValue;
            for (int i = 0; i < hexDirections.Length; i++)
            {
                float dot = Vector2.Dot(mainDirection, hexDirections[i]);
                if (dot > maxDot)
                {
                    maxDot = dot;
                    mainIndex = i;
                }
            }
        }

        // Get the two perpendicular directions (roughly 60 degrees off)
        int perpIndex1 = (mainIndex + 2) % 6;
        int perpIndex2 = (mainIndex + 4) % 6;

        return new Vector2Int[] { hexDirections[perpIndex1], hexDirections[perpIndex2] };
    }

    private List<HexTile> GetTilesInLineToMouse(HexTile startTile, int maxRange)
    {
        List<HexTile> tiles = new List<HexTile>();

        if (mouseHandler == null || mapMaker == null) return tiles;

        // Get mouse tile
        Vector2Int mouseHexCoord = mapMaker.WorldToHexPosition(mouseHandler.worldMousePos);

        // Get direction and project
        Vector2Int direction = GetHexDirection(startTile.coordinates, mouseHexCoord);

        // Get tiles in that direction
        Vector2Int currentCoord = startTile.coordinates;

        for (int i = 0; i <= maxRange; i++)
        {
            HexTile tile = mapMaker.GetHexTile(currentCoord);
            if (tile != null && tile.IsWalkable)
            {
                tiles.Add(tile);
            }
            else if (i > 0) // Stop if we hit an unwalkable tile (but include starting tile)
            {
                break;
            }

            currentCoord = currentCoord + direction;
        }

        return tiles;
    }

    private Vector2Int GetHexDirection(Vector2Int from, Vector2Int to)
    {
        // Calculate the difference
        Vector2Int diff = to - from;

        // Hex has 6 main directions in axial coordinates
        Vector2Int[] hexDirections = new Vector2Int[]
        {
            new Vector2Int(1, 0),   // East
            new Vector2Int(1, -1),  // Southeast
            new Vector2Int(0, -1),  // Southwest
            new Vector2Int(-1, 0),  // West
            new Vector2Int(-1, 1),  // Northwest
            new Vector2Int(0, 1)    // Northeast
        };

        // Find the closest matching direction
        float maxDot = float.MinValue;
        Vector2Int bestDirection = hexDirections[0];

        Vector2 normalizedDiff = new Vector2(diff.x, diff.y).normalized;

        foreach (Vector2Int dir in hexDirections)
        {
            Vector2 dirNorm = new Vector2(dir.x, dir.y).normalized;
            float dot = Vector2.Dot(normalizedDiff, dirNorm);

            if (dot > maxDot)
            {
                maxDot = dot;
                bestDirection = dir;
            }
        }

        return bestDirection;
    }

    private void CreateHitboxAtTile(HexTile tile, int index)
    {
        if (hitboxPrefab == null || tile == null) return;

        // Spawn at tile position
        Vector3 spawnPosition = tile.transform.position;
        GameObject spawnedHitbox = Instantiate(hitboxPrefab, spawnPosition, Quaternion.identity);
        spawnedHitbox.transform.parent = transform;

        AttackHitbox hitbox = spawnedHitbox.GetComponentInChildren<AttackHitbox>();
        if (hitbox != null)
        {
            // Pass the target type to child hitboxes
            hitbox.InitializeWithTargetType(damage, ownerTeam, lifetime, this, targetType);
            hitboxes.Add(hitbox);
        }

        hitboxObjects.Add(spawnedHitbox);
    }

    private void Update()
    {
        // Update rotation
        if (rotationPoint != null && mouseHandler != null && !isActivated)
        {
            RotateTowardsMouseZAxis();
        }

        // Snap to player's tile
        SnapToTile();

        // Update hitbox area in preview mode
        if (!isActivated && Time.frameCount % 5 == 0) // Update every 5 frames for performance
        {
            UpdateHitboxArea();
        }
    }

    private void UpdateHitboxArea()
    {
        if (mapMaker == null || mouseHandler == null) return;

        // Get current player tile
        Vector2Int playerHexCoord = mapMaker.WorldToHexPosition(transform.position);
        HexTile playerTile = mapMaker.GetHexTile(playerHexCoord);
        if (playerTile == null) return;

        // Get new target tiles
        List<HexTile> newTargetTiles = GetAttackAreaTiles(playerTile, Length, Width);

        // Check if tiles have changed
        bool tilesChanged = newTargetTiles.Count != targetTiles.Count;
        if (!tilesChanged)
        {
            for (int i = 0; i < newTargetTiles.Count; i++)
            {
                if (i >= targetTiles.Count || newTargetTiles[i] != targetTiles[i])
                {
                    tilesChanged = true;
                    break;
                }
            }
        }

        // Recreate hitboxes if tiles changed
        if (tilesChanged)
        {
            CreateHitboxArea();
        }
    }

    private void SnapToTile()
    {
        if (mapMaker == null || groundCheck == null) return;

        Vector2Int hexCoords = mapMaker.WorldToHexPosition(groundCheck.position);
        HexTile tile = mapMaker.GetHexTile(hexCoords);

        if (tile != null)
        {
            transform.position = tile.transform.position;
        }
    }

    private void RotateTowardsMouseZAxis()
    {
        if (mouseHandler == null || rotationPoint == null) return;

        Vector3 direction = mouseHandler.worldMousePos - rotationPoint.transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        rotationPoint.transform.rotation = Quaternion.Euler(0, 0, angle);

        // Counter-rotate this object to keep it upright
        transform.localRotation = Quaternion.Euler(0, 0, -angle);
    }

    public void ActivateForDamage()
    {
        if (isActivated) return;

        isActivated = true;
        SetupVisual(activeColor, activeAlpha);

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = true;
        }

        // Reset hit tracking for MultiTarget
        if (targetType == TargetType.MultiTarget)
        {
            fistTargetHit = false;
        }

        // Activate all child hitboxes
        foreach (AttackHitbox hitbox in hitboxes)
        {
            if (hitbox != null)
            {
                hitbox.ActivateForDamage();
            }
        }

        StartCoroutine(DestroyAfterTime());
        Debug.Log($"Attack hitbox activated: {hitboxes.Count} tiles, Width: {Width}, Type: {targetType}");
    }

    private void SetupVisual(Color color, float alpha)
    {
        if (hitboxRenderer != null)
        {
            Material material = hitboxRenderer.material;
            Color newColor = color;
            newColor.a = alpha;
            material.color = newColor;

            if (alpha < 1f)
            {
                material.SetFloat("_Mode", 3);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
            }
        }
    }

    private void ClearHitboxes()
    {
        foreach (GameObject obj in hitboxObjects)
        {
            if (obj != null)
            {
                if (Application.isPlaying)
                    Destroy(obj);
                else
                    DestroyImmediate(obj);
            }
        }

        hitboxObjects.Clear();
        hitboxes.Clear();
        targetTiles.Clear();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isActivated) return;

        Char character = other.GetComponent<Char>();
        if (character != null && character.team != ownerTeam && !hitTargets.Contains(character))
        {
            IDamage damageable = character.GetComponent<IDamage>();

            // Handle based on target type
            if (damageable != null)
            {
                if (targetType == TargetType.SingleTarget && !hasHit)
                {
                    // Single target: only hit once
                    damageable.TakeDamage(damage);
                    hitTargets.Add(character);
                    hasHit = true;
                    fistTargetHit = true;
                    Debug.Log($"Main hitbox hit {character.name} for {damage} damage! (SingleTarget)");
                }
                else if (targetType == TargetType.MultiTarget)
                {
                    // Multi target: can hit multiple enemies
                    damageable.TakeDamage(damage);
                    hitTargets.Add(character);
                    Debug.Log($"Main hitbox hit {character.name} for {damage} damage! (MultiTarget)");
                }
            }
        }
    }

    private IEnumerator DestroyAfterTime()
    {
        yield return new WaitForSeconds(lifetime);

        ClearHitboxes();

        if (rotationPoint != null)
        {
            Destroy(rotationPoint);
        }

        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        ClearHitboxes();
        hitTargets.Clear();
    }
}
