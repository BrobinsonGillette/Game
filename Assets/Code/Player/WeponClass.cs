using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "Wepon")]
public class WeponClass : ScriptableObject
{
    [Header("Attack Settings")]
    public Sprite weponSprite;
    public Sprite weponIcon;
    public float AreaRadius = 1.5f; // Radius of damage area
    public float AttackDuration = 0.5f; // How long damage area stays active
    public float AttackDamage = 10f;
    public float AttackCooldown = 1f;
    public GameObject Prefab; // Projectile prefab to shoot
    public Transform firePoint; // Where projectiles spawn from
    public float projectileSpeed = 10f;


    [Header("Attack Detection")]
    public LayerMask EnemyLayer = 1; // FIXED: Changed from EnamyLayer to EnemyLayer
    public weaponType currentType = weaponType.melee;
    public event Action OnAttack;

    public void PerformMeleeAttack(Transform transform)
    {
        OnAttack?.Invoke();

        Vector3 attackPosition = transform.position;

        GameObject damageArea = null;
        if (Prefab != null)
        {
            damageArea = Instantiate(Prefab, attackPosition, Quaternion.identity);
            BasicProjectile basicProjectile = damageArea.AddComponent<BasicProjectile>();
            basicProjectile.Initialize(0, 0, AttackDuration);
        }



        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(attackPosition, AreaRadius, EnemyLayer); // FIXED: EnemyLayer
        foreach (Collider2D hitCollider in hitColliders)
        {
            IDamable damageTarget = hitCollider.GetComponent<IDamable>();

            if (damageTarget != null)
            {
                damageArea.transform.position = hitCollider.GetComponent<Collider2D>().bounds.center;
                damageTarget.TakeDamage(AttackDamage);
                Debug.Log($"Player dealt {AttackDamage} melee damage to {hitCollider.name}");
            }
            else
            {
                Debug.Log($"No IDamable component found on {hitCollider.name}");
            }
        }


    }

    public void PerformRangedAttack()
    {
        OnAttack?.Invoke();

        if (Prefab != null && firePoint != null) // FIXED: Added firePoint null check
        {
            // FIXED: Improved mouse position to world position conversion
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f; // Ensure z is 0 for 2D

            Vector2 direction = ((Vector2)mousePos - (Vector2)firePoint.position).normalized;

            // Calculate 2D rotation angle
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            // Spawn projectile
            GameObject projectile = Instantiate(Prefab, firePoint.position, rotation);

            // Fallback to BasicProjectile
            BasicProjectile basicProjectile = projectile.GetComponent<BasicProjectile>();
            if (basicProjectile == null)
            {
                basicProjectile = projectile.AddComponent<BasicProjectile>();
            }

            // Add Rigidbody2D if needed
            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = projectile.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
            }

            rb.velocity = direction * projectileSpeed;
            basicProjectile.Initialize(AttackDamage, EnemyLayer, AttackDuration);
        }
    }
}
