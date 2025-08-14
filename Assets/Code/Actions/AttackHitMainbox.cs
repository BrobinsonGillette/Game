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
    private int Width = 1;
    private float damage = 10;
    private Team ownerTeam = Team.player;
    private float lifetime = 2f;
    private bool isActivated = false;
    public bool fistTargetHit = false;
    private ActionData targetType;

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
    private Vector2 startPos;

    // Performance optimization variables
    private int updateCounter = 0;
    private const int UPDATE_FREQUENCY = 5; // Update every 5 frames
    private Vector3 lastMousePosition;
    private Vector3 lastPlayerPosition;

    // Range checking cache
    private HexTile playerTile;
    private bool isWithinRange = true;

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

        // Validate dependencies
        if (mouseHandler == null)
        {
            Debug.LogError("MouseHandler instance not found!");
        }
        if (mapMaker == null)
        {
            Debug.LogError("MapMaker instance not found!");
        }
    }

    public void InitializeForPreview(float hitboxDamage, Team team, float hitboxLifetime, int length, int width, ActionData type, Vector2 StartPos)
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
        startPos = StartPos;

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }

        // Initialize position tracking
        lastMousePosition = mouseHandler?.worldMousePos ?? Vector3.zero;
        lastPlayerPosition = transform.position;

        // Get initial player tile
        UpdatePlayerTile();

        // Create initial hitbox area
        CreateHitboxArea();
    }

    private void UpdatePlayerTile()
    {
        if (mapMaker == null) return;

        Vector2Int playerHexCoord = mapMaker.WorldToHexPosition(transform.position);
        playerTile = mapMaker.GetHexTile(playerHexCoord);
    }

    private void CreateHitboxArea()
    {
        // Clear existing hitboxes
        ClearHitboxes();

        if (mapMaker == null || mouseHandler == null || playerTile == null) return;

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

        if (mouseHandler == null || mapMaker == null || startTile == null) return tiles;

        // Check if we're within range first
        if (!IsWithinActionRange())
        {
            return tiles; // Return empty list if out of range
        }

        // Get the main direction towards mouse
        Vector2Int mouseHexCoord = mapMaker.WorldToHexPosition(mouseHandler.worldMousePos);
        Vector2Int mainDirection = GetHexDirection(startTile.coordinates, mouseHexCoord);

        // If width is 1, it's just a line
        if (width <= 1)
        {
            return GetTilesInLineToMouse(startTile, length);
        }

        // For width > 1, create a cone or wide area
        Vector2Int[] perpendicularDirs = GetPerpendicularDirections(mainDirection);

        // For each distance along the length
        for (int dist = 1; dist <= length; dist++)
        {
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

    private bool IsWithinActionRange()
    {
        if (targetType == null || targetType.range <= 0 || mouseHandler == null)
        {
            return true; // No range limit or invalid data
        }

        Vector2 mousePos = new Vector2(Mathf.Floor(mouseHandler.worldMousePos.x), Mathf.Floor(mouseHandler.worldMousePos.y));
        float distance = Vector2.Distance(mousePos, startPos.normalized);
         distance = Mathf.Floor(distance);
        isWithinRange = distance <= targetType.range*2f;
        return isWithinRange;
    }

    private Vector2Int[] GetPerpendicularDirections(Vector2Int mainDirection)
    {
        Vector2Int[] hexDirections = new Vector2Int[]
        {
            new Vector2Int(1, 0),   // East
            new Vector2Int(1, -1),  // Southeast
            new Vector2Int(0, -1),  // Southwest
            new Vector2Int(-1, 0),  // West
            new Vector2Int(-1, 1),  // Northwest
            new Vector2Int(0, 1)    // Northeast
        };

        int mainIndex = GetDirectionIndex(mainDirection, hexDirections);

        if (mainIndex == -1) return new Vector2Int[] { hexDirections[0], hexDirections[1] };

        int perpIndex1 = (mainIndex + 2) % 6;
        int perpIndex2 = (mainIndex + 4) % 6;

        return new Vector2Int[] { hexDirections[perpIndex1], hexDirections[perpIndex2] };
    }

    private int GetDirectionIndex(Vector2Int direction, Vector2Int[] directions)
    {
        for (int i = 0; i < directions.Length; i++)
        {
            if (directions[i] == direction)
            {
                return i;
            }
        }

        // Find closest direction if exact match not found
        float maxDot = float.MinValue;
        int bestIndex = 0;
        Vector2 normalizedDirection = new Vector2(direction.x, direction.y).normalized;

        for (int i = 0; i < directions.Length; i++)
        {
            Vector2 dirNorm = new Vector2(directions[i].x, directions[i].y).normalized;
            float dot = Vector2.Dot(normalizedDirection, dirNorm);

            if (dot > maxDot)
            {
                maxDot = dot;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private List<HexTile> GetTilesInLineToMouse(HexTile startTile, int maxRange)
    {
        List<HexTile> tiles = new List<HexTile>();

        if (mouseHandler == null || mapMaker == null || startTile == null) return tiles;

        // Check range first
        if (!IsWithinActionRange())
        {
            return tiles;
        }

        Vector2Int mouseHexCoord = mapMaker.WorldToHexPosition(mouseHandler.worldMousePos);
        Vector2Int direction = GetHexDirection(startTile.coordinates, mouseHexCoord);
        Vector2Int currentCoord = startTile.coordinates;

        for (int i = 0; i <= maxRange; i++)
        {
            HexTile tile = mapMaker.GetHexTile(currentCoord);
            if (tile != null && tile.IsWalkable)
            {
                tiles.Add(tile);
            }
            else if (i > 0)
            {
                break;
            }

            currentCoord = currentCoord + direction;
        }

        return tiles;
    }

    private Vector2Int GetHexDirection(Vector2Int from, Vector2Int to)
    {
        Vector2Int diff = to - from;
        Vector2Int[] hexDirections = new Vector2Int[]
        {
            new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, -1),
            new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1)
        };

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

        Vector3 spawnPosition = tile.transform.position;
        GameObject spawnedHitbox = Instantiate(hitboxPrefab, spawnPosition, Quaternion.identity);
        spawnedHitbox.transform.parent = transform;

        AttackHitbox hitbox = spawnedHitbox.GetComponentInChildren<AttackHitbox>();
        if (hitbox != null)
        {
            hitbox.InitializeWithTargetType(damage, ownerTeam, lifetime, this, targetType);
            hitboxes.Add(hitbox);
        }

        hitboxObjects.Add(spawnedHitbox);
    }

    private void Update()
    {
        if (isActivated) return;

        updateCounter++;
        bool shouldUpdate = updateCounter % UPDATE_FREQUENCY == 0;

        // Always update rotation point position if within range
        UpdateRotationPointPosition();

        // Always update rotation for smooth visual feedback
        if (rotationPoint != null && mouseHandler != null)
        {
            RotateTowardsMouseZAxis();
        }

        // Snap to player's tile every frame for smooth movement
        SnapToTile();

        // Only update hitbox area periodically or when significant change detected
        if (shouldUpdate || HasSignificantChange())
        {
            UpdateHitboxArea();
            CacheCurrentPositions();
        }
    }

    private void UpdateRotationPointPosition()
    {
        if (rotationPoint == null || mouseHandler == null || targetType == null) return;

        if (targetType.range > 0)
        {
            Vector2 mousePos = new Vector2(Mathf.Floor(mouseHandler.worldMousePos.x), Mathf.Floor(mouseHandler.worldMousePos.y));
            float distance = Vector2.Distance(mousePos, startPos.normalized);
            distance = Mathf.Floor(distance);
            if (distance <= targetType.range  * 2f)
            {
                rotationPoint.transform.position = mouseHandler.worldMousePos;
            }
            else
            {
                mousePos = new Vector2(mouseHandler.worldMousePos.x,mouseHandler.worldMousePos.y);
                Vector2 direction = (mousePos - startPos).normalized;
                Vector2 clampedPos = startPos + direction * targetType.range;
              //  rotationPoint.transform.position = new Vector3(clampedPos.x, clampedPos.y, rotationPoint.transform.position.z);
            }
        }
        else
        {
            rotationPoint.transform.position = mouseHandler.worldMousePos;
        }
    }

    private bool HasSignificantChange()
    {
        if (mouseHandler == null) return false;

        bool mouseChanged = Vector3.Distance(lastMousePosition, mouseHandler.worldMousePos) > 0.1f;
        bool playerChanged = Vector3.Distance(lastPlayerPosition, transform.position) > 0.1f;

        return mouseChanged || playerChanged;
    }

    private void CacheCurrentPositions()
    {
        if (mouseHandler != null)
        {
            lastMousePosition = mouseHandler.worldMousePos;
        }
        lastPlayerPosition = transform.position;
    }

    private void UpdateHitboxArea()
    {
        if (mapMaker == null || mouseHandler == null) return;

        // Update player tile if position changed
        if (Vector3.Distance(lastPlayerPosition, transform.position) > 0.1f)
        {
            UpdatePlayerTile();
        }

        if (playerTile == null) return;

        List<HexTile> newTargetTiles = GetAttackAreaTiles(playerTile, Length, Width);

        if (HasTilesChanged(newTargetTiles))
        {
            CreateHitboxArea();
        }
    }

    private bool HasTilesChanged(List<HexTile> newTargetTiles)
    {
        if (newTargetTiles.Count != targetTiles.Count) return true;

        for (int i = 0; i < newTargetTiles.Count; i++)
        {
            if (i >= targetTiles.Count || newTargetTiles[i] != targetTiles[i])
            {
                return true;
            }
        }

        return false;
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

        if (targetType != null && targetType.CanTargetMultipleTargets)
        {
            fistTargetHit = false;
        }

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
        if (hitboxRenderer == null) return;

        Material material = hitboxRenderer.material;
        Color newColor = color;
        newColor.a = alpha;
        material.color = newColor;

        if (alpha < 1f)
        {
            // Setup transparent rendering
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

            if (damageable != null && targetType != null)
            {
                if (!targetType.CanTargetMultipleTargets && !hasHit)
                {
                    damageable.TakeDamage(damage);
                    hitTargets.Add(character);
                    hasHit = true;
                    fistTargetHit = true;
                    Debug.Log($"Main hitbox hit {character.name} for {damage} damage! (SingleTarget)");
                }
                else if (targetType.CanTargetMultipleTargets && !hitTargets.Contains(character))
                {
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
