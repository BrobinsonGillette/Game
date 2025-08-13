using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    [Header("Hitbox Settings")]
    public Transform rotationPoint;
    public float damage = 10f;
    public float lifetime = 1f;
    public bool dealDamageOnSpawn = true;
    public bool dealDamaged = false;
    public bool destroyAfterHit = false;

    [Header("Visual Effects")]
    public bool fadeOut = true;
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    private SpriteRenderer spriteRenderer;
    private List<IDamable> hitTargets = new List<IDamable>();
    private Team attackerTeam;
    MouseHandler mouseHandler;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (dealDamageOnSpawn)
        {
            DealDamageToTargetsInArea();
        }
        mouseHandler = MouseHandler.instance;
        // StartCoroutine(HandleLifetime());
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
        Vector3 direction = mouseHandler.worldMousePos - rotationPoint.position;

        // Calculate the angle in degrees (atan2 returns radians, so convert to degrees)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Apply only Z-axis rotation, keeping X and Y rotation unchanged
        rotationPoint.rotation = Quaternion.Euler(0, 0, angle);

        // Alternative: If you want to preserve existing X and Y rotations
        // Vector3 currentEuler = rotationPoint.eulerAngles;
        // rotationPoint.rotation = Quaternion.Euler(currentEuler.x, currentEuler.y, angle);
    }

    public void Initialize(float hitboxDamage, Team team, float duration = 1f)
    {
        damage = hitboxDamage;
        attackerTeam = team;
        lifetime = duration;
    }

    private void DealDamageToTargetsInArea()
    {
        // Get all characters in the scene
        Char[] allCharacters = FindObjectsOfType<Char>();

        foreach (Char character in allCharacters)
        {
            // Skip if same team as attacker
            if (character.team == attackerTeam) continue;

            // Check if character is within hitbox bounds
            if (IsCharacterInHitbox(character))
            {
                IDamable damageable = character.GetComponent<IDamable>();
                if (damageable != null && !hitTargets.Contains(damageable))
                {
                    damageable.TakeDamage(damage);
                    hitTargets.Add(damageable);

                    Debug.Log($"Hitbox dealt {damage} damage to {character.name}");

                    if (destroyAfterHit)
                    {
                        Destroy(gameObject);
                        return;
                    }
                }
            }
        }
    }

    private bool IsCharacterInHitbox(Char character)
    {
        if (character == null || character.currentHex == null) return false;

        // Check if character's hex position overlaps with hitbox position
        float distance = Vector3.Distance(transform.position, character.transform.position);

        // You can adjust this threshold based on your hitbox size
        float hitboxRadius = transform.localScale.x * 0.5f;

        return distance <= hitboxRadius;
    }

    private IEnumerator HandleLifetime()
    {
        float elapsed = 0f;
        Color originalColor = spriteRenderer ? spriteRenderer.color : Color.white;

        while (elapsed < lifetime)
        {
            if (dealDamaged)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / lifetime;

                // Handle fade out effect
                if (fadeOut && spriteRenderer != null)
                {
                    Color currentColor = originalColor;
                    currentColor.a = fadeCurve.Evaluate(progress);
                    spriteRenderer.color = currentColor;
                }

                yield return null;
            }
        }

        Destroy(gameObject);
    }

    // Alternative method for continuous damage detection
    private void OnTriggerStay2D(Collider2D other)
    {
        Char character = other.GetComponent<Char>();
        if (character == null || character.team == attackerTeam) return;

        IDamable damageable = character.GetComponent<IDamable>();
        if (damageable != null && !hitTargets.Contains(damageable))
        {
            if (dealDamaged)
            {
                damageable.TakeDamage(damage);
                hitTargets.Add(damageable);
                Debug.Log($"Hitbox dealt {damage} damage to {character.name}");
                if (destroyAfterHit)
                {
                    Destroy(gameObject);
                }
                dealDamaged = false;
            }
        }
    }
}
