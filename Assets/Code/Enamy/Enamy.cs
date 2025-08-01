using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Playables;
using UnityEngine;


public class Enamy : MonoBehaviour, IDamable
{
    [Header("Enemy Basic Info")]
    public string enemyName = "Enemy";
    public Sprite enemyIcon;
    [SerializeField] SpriteRenderer spriteRenderer;
    public int expGiven = 10;

    [Header("Health System")]
    public float currentHealth = 100f;
    public float maxHealth = 100f;
    public float DamageReduction = 1f;

    [Header("Combat Ranges")]
    public float Rangeattack = 5f; // Range for ranged attack
    public float MeleeAttackRange = 2f; // Range for melee attack

    [Header("Melee Attack Settings")]
    public float MeleeAttackDamage = 10f;
    public float MeleeAttackCooldown = 1f;
    public float meleeAreaRadius = 1.5f; // Radius of damage area
    public float meleeAreaDuration = 0.5f; // How long damage area stays active
    public GameObject meleeAreaPrefab; // Optional visual prefab for damage area

    [Header("Ranged Attack Settings")]
    public float RangeAttackDamage = 10f;
    public float RangeAttackCooldown = 1f;
    public GameObject projectilePrefab; // Projectile prefab to shoot
    public Transform firePoint; // Where projectiles spawn from
    public float projectileSpeed = 10f;

    [Header("Attack Detection")]
    public LayerMask playerLayer = 1; // What layer the player is on

    // Private variables
    private Transform player;
    private float lastMeleeAttackTime;
    private float lastRangeAttackTime;
    private bool isAttacking = false;

    // Events
    public event Action<float, float> OnHealthChanged; // current, max
    public event Action OnDeath;
    public event Action OnMeleeAttack;
    public event Action OnRangedAttack;

    private void Start()
    {
        // Initialize health
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        spriteRenderer.sprite = enemyIcon;
        // Find player (assuming player has "Player" tag)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // If no fire point is set, use this transform
        if (firePoint == null)
        {
            firePoint = transform;
        }
    }

    private void Update()
    {
        if (player != null && !IsDead() && !isAttacking)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            // Check if player is in melee range
            if (distanceToPlayer <= MeleeAttackRange)
            {
                TryMeleeAttack();
            }
            // Check if player is in ranged attack range but not in melee range
            else if (distanceToPlayer <= Rangeattack)
            {
                TryRangedAttack();
            }
        }
    }

    private void TryMeleeAttack()
    {
        if (Time.time >= lastMeleeAttackTime + MeleeAttackCooldown)
        {
            StartCoroutine(PerformMeleeAttack());
            lastMeleeAttackTime = Time.time;
        }
    }

    private void TryRangedAttack()
    {
        if (Time.time >= lastRangeAttackTime + RangeAttackCooldown)
        {
            PerformRangedAttack();
            lastRangeAttackTime = Time.time;
        }
    }

    private IEnumerator PerformMeleeAttack()
    {
        isAttacking = true;
        OnMeleeAttack?.Invoke();

        // Create damage area
        Vector3 attackPosition = transform.position;

        // Spawn visual effect if prefab is assigned
        GameObject damageArea = null;
        if (meleeAreaPrefab != null)
        {
            damageArea = Instantiate(meleeAreaPrefab, attackPosition, Quaternion.identity);
        }

        // Wait a brief moment for attack animation/telegraphing
        yield return new WaitForSeconds(0.1f);

        // Deal damage to all players in the area
        Collider[] hitColliders = Physics.OverlapSphere(attackPosition, meleeAreaRadius, playerLayer);
        foreach (Collider hitCollider in hitColliders)
        {
            IDamable damageTarget = hitCollider.GetComponent<IDamable>();
            if (damageTarget != null)
            {
                damageTarget.TakeDamage(MeleeAttackDamage);
                Debug.Log($"{enemyName} dealt {MeleeAttackDamage} melee damage to {hitCollider.name}");
            }
        }

        // Keep damage area active for duration
        yield return new WaitForSeconds(meleeAreaDuration - 0.1f);

        // Clean up visual effect
        if (damageArea != null)
        {
            Destroy(damageArea);
        }

        isAttacking = false;
    }

    private void PerformRangedAttack()
    {
        OnRangedAttack?.Invoke();

        if (projectilePrefab != null && player != null)
        {
            // Calculate direction to player
            Vector3 direction = (player.position - firePoint.position).normalized;

            // Spawn projectile
            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));

            // Set up projectile
            EnemyProjectile projectileScript = projectile.GetComponent<EnemyProjectile>();
            if (projectileScript != null)
            {
                projectileScript.Initialize(direction, projectileSpeed, RangeAttackDamage, playerLayer);
            }
            else
            {
                // If no projectile script, add rigidbody and move it
                Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
                if (rb == null)
                {
                    rb = projectile.AddComponent<Rigidbody2D>();
                }
                rb.velocity = direction * projectileSpeed;

                // Add basic projectile behavior
                projectile.AddComponent<BasicProjectile>().Initialize(RangeAttackDamage, playerLayer);
            }

           // Debug.Log($"{enemyName} fired a ranged attack at {player.name}");
        }
        else
        {
            Debug.LogWarning($"{enemyName} tried to perform ranged attack but projectilePrefab is null or player not found!");
        }
    }

    // Health and damage system (unchanged from original)
    public float Health
    {
        get => currentHealth;
        set => currentHealth = Mathf.Clamp(value, 0, maxHealth);
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0 || currentHealth <= 0) return;

        float actualDamage = damage;
        float damageReduction = 1f - (DamageReduction * 0.01f);
        actualDamage *= damageReduction;

        currentHealth = Mathf.Clamp(currentHealth - actualDamage, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        Debug.Log($"{enemyName} took {actualDamage:F1} damage (reduced from {damage:F1}). Health: {currentHealth:F1}/{maxHealth:F1}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public bool IsDead() => currentHealth <= 0;

    private void Die()
    {
        if (IsDead()) return;

        // Award experience to player
        if (PlayerStats.instance != null)
        {
            PlayerStats.instance.GainExp(expGiven);
        }

        OnDeath?.Invoke();

        Debug.Log($"{enemyName} died and gave {expGiven} experience!");

        // Destroy enemy after death
        Destroy(gameObject, 0.5f);
    }

    // Debug visualization in Scene view
    private void OnDrawGizmosSelected()
    {
        // Draw melee attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, MeleeAttackRange);

        // Draw ranged attack range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, Rangeattack);

        // Draw melee damage area
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, meleeAreaRadius);
    }
}
