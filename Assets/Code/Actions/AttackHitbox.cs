using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Collections;
using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    private float damage = 10;
    private Team ownerTeam = Team.player;
    private float lifetime = 2f;
    private bool isActivated = false;
    private int Range = 1;
    private bool hasHit = false;
    [Header("Visual Settings")]
    public Color previewColor = Color.yellow;
    public Color activeColor = Color.red;
    public float previewAlpha = 0.5f;
    public float activeAlpha = 1f;
    private AttackHitMainbox parent;
    [SerializeField] Transform groundCheck;
    [SerializeField] private Renderer hitboxRenderer;
    private Collider hitboxCollider;
    private HashSet<Char> hitTargets = new HashSet<Char>();



    private void Awake()
    {
        hitboxCollider = GetComponent<Collider>();

        // Disable collider initially
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }

    }

    public void InitializeForPreview(float hitboxDamage, Team team, float hitboxLifetime, int range, GameObject hitboxPrefab, AttackHitMainbox Parent, Vector3 direction, int segmentIndex = 1)
    {
        damage = hitboxDamage;
        ownerTeam = team;
        lifetime = hitboxLifetime;
        isActivated = false; // Start in preview mode
        Range = range;
        SetupVisual(previewColor, previewAlpha);
        hasHit = false;
        parent = Parent;

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false; // No collision in preview mode
        }

        if (Range > 1 && segmentIndex + 1 < Range)
        {
            // Calculate next position in the direction
            Vector3 nextPosition = transform.position + direction.normalized;

            GameObject SpawnAttack = Instantiate(hitboxPrefab, nextPosition, Quaternion.identity);
            AttackHitbox hitbox = SpawnAttack.GetComponentInChildren<AttackHitbox>();
            SpawnAttack.transform.parent = transform;

            if (hitbox != null)
            {
                // Initialize next hitbox with same direction and incremented segment index
                hitbox.InitializeForPreview(damage, team, hitboxLifetime, Range, hitboxPrefab, Parent, direction, segmentIndex + 1);
            }
            Parent.hitboxes.Add(hitbox);
        }
    }

    private void Update()
    {
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

    public void ActivateForDamage()
    {
        if (isActivated) return; // Already activated

        isActivated = true;
        SetupVisual(activeColor, activeAlpha);

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = true;
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
            if (damageable != null || hasHit || parent.fistTargetHit)
            {
                damageable.TakeDamage(damage);
                hitTargets.Add(character);
                hasHit = true;
                parent.fistTargetHit = true;
                Debug.Log($"Hitbox hit {character.name} for {damage} damage!");
            }
        }
    }

    private IEnumerator DestroyAfterTime()
    {
        yield return new WaitForSeconds(lifetime);

        if (gameObject != null)
        {
            Destroy(this.gameObject);
        }
    }

    private void OnDestroy()
    {
        hitTargets.Clear();
    }
}
