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
    private bool HasHit = false;
    private AttackHitMainbox parentMainbox;
    private TargetType targetType = TargetType.SingleTarget;

    [Header("Visual Settings")]
    public Color previewColor = Color.yellow;
    public Color activeColor = Color.red;
    public float previewAlpha = 0.5f;
    public float activeAlpha = 1f;

    [SerializeField] private Renderer hitboxRenderer;
    private Collider hitboxCollider;
    private HashSet<Char> hitTargets = new HashSet<Char>();
    private HexTile currentTile;
    private MapMaker mapMaker;

    private void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }

        mapMaker = MapMaker.instance;
    }

    // Updated initialization with target type
    public void InitializeWithTargetType(float hitboxDamage, Team team, float hitboxLifetime, AttackHitMainbox parent, TargetType type)
    {
        damage = hitboxDamage;
        ownerTeam = team;
        lifetime = hitboxLifetime;
        parentMainbox = parent;
        targetType = type;
        isActivated = false;
        HasHit = false;
        SetupVisual(previewColor, previewAlpha);

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }

        // Snap to nearest tile
        SnapToNearestTile();
    }

    private void Update()
    {
        // Keep snapped to tile
        if (currentTile != null)
        {
            transform.position = currentTile.transform.position;
        }
    }

    private void SnapToNearestTile()
    {
        if (mapMaker == null) return;

        Vector2Int hexCoords = mapMaker.WorldToHexPosition(transform.position);
        HexTile tile = mapMaker.GetHexTile(hexCoords);

        if (tile != null)
        {
            currentTile = tile;
            transform.position = tile.transform.position;
        }
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

        StartCoroutine(DestroyAfterTime());
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

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isActivated) return;

        Char character = other.GetComponent<Char>();
        if (character != null && character.team != ownerTeam && !hitTargets.Contains(character))
        {
            IDamage damageable = character.GetComponent<IDamage>();

            if (damageable != null)
            {
                // Handle based on target type
                if (targetType == TargetType.SingleTarget)
                {
                    // For single target, check if parent already hit something
                    if (parentMainbox == null || !parentMainbox.fistTargetHit)
                    {
                        damageable.TakeDamage(damage);
                        hitTargets.Add(character);

                        if (parentMainbox != null)
                        {
                            parentMainbox.fistTargetHit = true;
                        }

                        Debug.Log($"Hitbox hit {character.name} for {damage} damage! (SingleTarget)");
                    }
                }
                else if (targetType == TargetType.MultiTarget && !HasHit)
                {
                    // For multi-target, hit everyone in range
                    damageable.TakeDamage(damage);
                    hitTargets.Add(character);
                    HasHit = true;
                    Debug.Log($"Hitbox hit {character.name} for {damage} damage! (MultiTarget)");
                }
            }
        }
    }

    private IEnumerator DestroyAfterTime()
    {
        yield return new WaitForSeconds(lifetime);

        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        hitTargets.Clear();
        currentTile = null;
    }

    // Helper method to get the tile this hitbox is on
    public HexTile GetCurrentTile()
    {
        return currentTile;
    }
}
