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
    MouseHandler mouseHandler;
    public List<AttackHitbox> hitboxes { get; private set; } = new List<AttackHitbox>();

    // Properties for external access
    public Team OwnerTeam => ownerTeam;
    public bool IsActivated => isActivated;

    private void Awake()
    {
        hitboxCollider = GetComponent<Collider>();

        // Disable collider initially
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }
        mouseHandler = MouseHandler.instance;
    }

    public void InitializeForPreview(float hitboxDamage, Team team, float hitboxLifetime, int range)
    {
        damage = hitboxDamage;
        ownerTeam = team;
        lifetime = hitboxLifetime;
        isActivated = false; // Start in preview mode
        Range = range;
        SetupVisual(previewColor, previewAlpha);
        hasHit = false;

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false; // No collision in preview mode
        }

        if (Range > 1)
        {
            // Calculate direction towards mouse
            Vector3 direction = GetDirectionToMouse();

            // Calculate first hitbox position (one unit away in direction)
            Vector3 firstHitboxPosition = groundCheck.position + direction.normalized;

            GameObject SpawnAttack = Instantiate(hitboxPrefab, firstHitboxPosition, Quaternion.identity);
            AttackHitbox hitbox = SpawnAttack.GetComponentInChildren<AttackHitbox>();
            SpawnAttack.transform.parent = transform;

            if (hitbox != null)
            {
                // Initialize hitbox chain with direction and starting from segment 2
                hitbox.InitializeForPreview(damage, team, hitboxLifetime, Range, hitboxPrefab, this, direction, 2);
            }
            hitboxes.Add(hitbox);
        }
    }

    private Vector3 GetDirectionToMouse()
    {
        if (mouseHandler != null)
        {
            Vector3 direction = mouseHandler.worldMousePos - groundCheck.position;
            // For hex grid, you might want to snap to the 6 main directions
            return SnapToHexDirection(direction);
        }
        return Vector3.right; // Default direction if no mouse handler
    }

    private Vector3 SnapToHexDirection(Vector3 direction)
    {
        // Define the 6 main hex directions (right, up-right, up-left, left, down-left, down-right)
        Vector3[] hexDirections = new Vector3[]
        {
            Vector3.right,                    // 0°
            new Vector3(0.5f, 0.866f, 0),    // 60°
            new Vector3(-0.5f, 0.866f, 0),   // 120°
            Vector3.left,                     // 180°
            new Vector3(-0.5f, -0.866f, 0),  // 240°
            new Vector3(0.5f, -0.866f, 0)    // 300°
        };

        direction.Normalize();
        Vector3 closestDirection = hexDirections[0];
        float maxDot = Vector3.Dot(direction, closestDirection);

        for (int i = 1; i < hexDirections.Length; i++)
        {
            float dot = Vector3.Dot(direction, hexDirections[i]);
            if (dot > maxDot)
            {
                maxDot = dot;
                closestDirection = hexDirections[i];
            }
        }

        return closestDirection;
    }

    private void Update()
    {
        // Calculate Z-axis rotation based on mouse position
        if (rotationPoint != null && mouseHandler != null)
        {
            RotateTowardsMouseZAxis();
        }
        getTileOnGround();

        // Update hitbox positions in real-time during preview
        if (!isActivated && Range > 1)
        {
            UpdateHitboxPositions();
        }
    }

    private void UpdateHitboxPositions()
    {
        Vector3 direction = GetDirectionToMouse();

        for (int i = 0; i < hitboxes.Count; i++)
        {
            if (hitboxes[i] != null)
            {
                // Position each hitbox at incrementally further distances
                Vector3 newPosition = transform.position + direction.normalized * (i + 2);
                hitboxes[i].transform.position = newPosition;
            }
        }
    }

    void getTileOnGround()
    {
        MapMaker mapMaker = MapMaker.instance;
        if (mapMaker == null) return;
        Vector3 position = groundCheck.position;
        Vector2Int hexCoords = mapMaker.WorldToHexPosition(position);
        if (mapMaker.hexTiles.TryGetValue(hexCoords, out HexTile tile))
        {
            transform.position = tile.transform.position;
        }
    }

    private void RotateTowardsMouseZAxis()
    {
        // Get the direction from rotation point to mouse position
        Vector3 direction = mouseHandler.worldMousePos - rotationPoint.transform.position;

        // Calculate the angle in degrees (atan2 returns radians, so convert to degrees)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Apply only Z-axis rotation, keeping X and Y rotation unchanged
        rotationPoint.transform.rotation = Quaternion.Euler(0, 0, angle);
        transform.localRotation = Quaternion.Euler(0, 0, -angle);
    }

    public void ActivateForDamage()
    {
        if (isActivated) return; // Already activated

        isActivated = true;
        SetupVisual(activeColor, activeAlpha);

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = true;
        }

        if (hasHit || fistTargetHit) return;

        foreach (AttackHitbox hitbox in hitboxes)
        {
            if (hitbox != null)
            {
                hitbox.ActivateForDamage();
            }
        }

        StartCoroutine(DestroyAfterTime());
        Debug.Log($"Attack hitbox activated for damage!");
    }

    private void SetupVisual(Color color, float alpha)
    {
        if (hitboxRenderer != null)
        {
            Material material = hitboxRenderer.material;
            Color newColor = color;
            newColor.a = alpha;
            material.color = newColor;

            // If using standard shader, set rendering mode to transparent
            if (alpha < 1f)
            {
                material.SetFloat("_Mode", 3); // Transparent
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

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isActivated) return; // Don't damage in preview mode

        Char character = other.GetComponent<Char>();
        if (character != null && character.team != ownerTeam && !hitTargets.Contains(character))
        {
            IDamage damageable = character.GetComponent<IDamage>();
            if (damageable != null || hasHit)
            {
                damageable.TakeDamage(damage);
                hitTargets.Add(character);
                hasHit = true;
                Debug.Log($"Hitbox hit {character.name} for {damage} damage!");
            }
        }
    }

    private IEnumerator DestroyAfterTime()
    {
        yield return new WaitForSeconds(lifetime);

        if (gameObject != null)
        {
            Destroy(rotationPoint);
        }
    }

    private void OnDestroy()
    {
        // Clean up any references
        hitTargets.Clear();
    }
}
