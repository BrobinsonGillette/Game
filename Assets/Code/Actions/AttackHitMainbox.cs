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
    private int currentRange = 1;
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

    public void InitializeForPreview(float hitboxDamage, Team team, float hitboxLifetime,int range)
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
            // Always spawn at player's position when action is selected
            Vector3 spawnPosition = transform.position;

            GameObject SpawnAttack = Instantiate(hitboxPrefab, spawnPosition, Quaternion.identity);
            AttackHitbox hitbox = SpawnAttack.GetComponentInChildren<AttackHitbox>();
            currentRange -= Range - 1;
            SpawnAttack.transform.parent = transform;
            if (hitbox != null)
            {
                // Initialize hitbox but don't activate damage yet
                hitbox.InitializeForPreview(damage, team,hitboxLifetime, currentRange, SpawnAttack,this);
            }
            hitboxes.Add(hitbox);
        }
    }
    private void Update()
    {
        // Calculate Z-axis rotation based on mouse position
        if (rotationPoint != null && mouseHandler != null)
        {
            RotateTowardsMouseZAxis();
        }
        getTileOnGround();
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
        if(hasHit || fistTargetHit) return;
        foreach (AttackHitbox hitbox in hitboxes)
        {
            hitbox.ActivateForDamage();
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
            IDamable damageable = character.GetComponent<IDamable>();
            if (damageable != null || hasHit)
            {
                damageable.TakeDamage(damage);
                hitTargets.Add(character);
                hasHit=true;
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
