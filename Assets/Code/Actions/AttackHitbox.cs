using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    [Header("Hitbox Settings")]
    public GameObject rotationPoint;
    public float damage = 10;
    public Team ownerTeam = Team.player;
    public float lifetime = 2f;
    public bool isActivated = false;

    [Header("Visual Settings")]
    public Color previewColor = Color.yellow;
    public Color activeColor = Color.red;
    public float previewAlpha = 0.5f;
    public float activeAlpha = 1f;

    [SerializeField]private Renderer hitboxRenderer;
    private Collider hitboxCollider;
    private HashSet<Char> hitTargets = new HashSet<Char>();
    MouseHandler mouseHandler;

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

    public void InitializeForPreview(float hitboxDamage, Team team, float hitboxLifetime)
    {
        damage = hitboxDamage;
        ownerTeam = team;
        lifetime = hitboxLifetime;
        isActivated = false; // Start in preview mode

        SetupVisual(previewColor, previewAlpha);

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false; // No collision in preview mode
        }
    }
    private void Update()
    {
        // Calculate Z-axis rotation based on mouse position
        if (rotationPoint != null && mouseHandler != null)
        {
            RotateTowardsMouseZAxis();
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
            IDamable damageable = character.GetComponent<IDamable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                hitTargets.Add(character);
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
