using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Collections;
using UnityEngine;

public class AttackHitMainbox : MonoBehaviour
{
    [Header("Hitbox Settings")]
    [SerializeField] GameObject hitboxPrefab;
    [SerializeField] GameObject rotationPoint;
    private int Range = 1;
    private float damage = 10;
    private Team ownerTeam = Team.player;
    private float lifetime = 2f;
    private bool isActivated = false;
    public bool fistTargetHit = false;

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

    // Store the tiles in the line for visualization
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

    public void InitializeForPreview(float hitboxDamage, Team team, float hitboxLifetime, int range)
    {
        damage = hitboxDamage;
        ownerTeam = team;
        lifetime = hitboxLifetime;
        isActivated = false;
        Range = range;
        SetupVisual(previewColor, previewAlpha);
        hasHit = false;

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }

        // Create hitboxes along the line
        CreateHitboxLine();
    }

    private void CreateHitboxLine()
    {
        // Clear existing hitboxes
        ClearHitboxes();

        if (mapMaker == null || mouseHandler == null) return;

        // Get player's current tile
        Vector2Int playerHexCoord = mapMaker.WorldToHexPosition(transform.position);
        HexTile playerTile = mapMaker.GetHexTile(playerHexCoord);
        if (playerTile == null) return;

        // Get tiles in line towards mouse
        targetTiles = GetTilesInLineToMouse(playerTile, Range);

        // Create hitbox for each tile (excluding player's tile)
        for (int i = 0; i < targetTiles.Count; i++)
        {
            HexTile tile = targetTiles[i];
            if (tile == playerTile) continue; // Skip player's tile

            CreateHitboxAtTile(tile, i);
        }
    }

    private List<HexTile> GetTilesInLineToMouse(HexTile startTile, int maxRange)
    {
        List<HexTile> tiles = new List<HexTile>();

        if (mouseHandler == null || mapMaker == null) return tiles;

        // Get mouse tile
        Vector2Int mouseHexCoord = mapMaker.WorldToHexPosition(mouseHandler.worldMousePos);
        HexTile mouseTile = mapMaker.GetHexTile(mouseHexCoord);

        // If mouse is not on a valid tile, get direction and project
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

        // Convert to cube coordinates for easier direction calculation
        Vector3 fromCube = AxialToCube(from);
        Vector3 toCube = AxialToCube(to);
        Vector3 cubeDiff = toCube - fromCube;

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

    private Vector3 AxialToCube(Vector2Int axial)
    {
        float x = axial.x;
        float z = axial.y;
        float y = -x - z;
        return new Vector3(x, y, z);
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
            // Use simplified initialization
            hitbox.InitializeSimple(damage, ownerTeam, lifetime, this);
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

        // Update hitbox line in preview mode
        if (!isActivated && Time.frameCount % 5 == 0) // Update every 5 frames for performance
        {
            UpdateHitboxLine();
        }
    }

    private void UpdateHitboxLine()
    {
        if (mapMaker == null || mouseHandler == null) return;

        // Get current player tile
        Vector2Int playerHexCoord = mapMaker.WorldToHexPosition(transform.position);
        HexTile playerTile = mapMaker.GetHexTile(playerHexCoord);
        if (playerTile == null) return;

        // Get new target tiles
        List<HexTile> newTargetTiles = GetTilesInLineToMouse(playerTile, Range);

        // Check if tiles have changed
        bool tilesChanged = newTargetTiles.Count != targetTiles.Count;
        if (!tilesChanged)
        {
            for (int i = 0; i < newTargetTiles.Count; i++)
            {
                if (newTargetTiles[i] != targetTiles[i])
                {
                    tilesChanged = true;
                    break;
                }
            }
        }

        // Recreate hitboxes if tiles changed
        if (tilesChanged)
        {
            CreateHitboxLine();
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

        // Activate all child hitboxes
        foreach (AttackHitbox hitbox in hitboxes)
        {
            if (hitbox != null)
            {
                hitbox.ActivateForDamage();
            }
        }

        StartCoroutine(DestroyAfterTime());
        Debug.Log($"Attack hitbox line activated for {hitboxes.Count} tiles!");
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
            if (damageable != null && !hasHit)
            {
                damageable.TakeDamage(damage);
                hitTargets.Add(character);
                hasHit = true;
                Debug.Log($"Main hitbox hit {character.name} for {damage} damage!");
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
