using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float damage;
    private LayerMask targetLayer;
    private float lifetime = 5f; // Destroy after 5 seconds if no hit
    private Rigidbody2D rb2d;

    public void Initialize(Vector3 dir, float spd, float dmg, LayerMask layer)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        targetLayer = layer;

        // Setup Rigidbody2D for physics-based movement
        rb2d = GetComponent<Rigidbody2D>();
        if (rb2d == null)
        {
            rb2d = gameObject.AddComponent<Rigidbody2D>();
        }

        // Configure rigidbody for projectile
        rb2d.gravityScale = 0f; // No gravity for top-down
        rb2d.freezeRotation = true;
        rb2d.velocity = direction * speed;

        // Add 2D collider if none exists
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            CircleCollider2D circleCol = gameObject.AddComponent<CircleCollider2D>();
            circleCol.isTrigger = true;
            circleCol.radius = 0.1f; // Small radius for projectile
        }
        else
        {
            col.isTrigger = true;
        }

        // Destroy projectile after lifetime
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if hit target layer
        if (((1 << other.gameObject.layer) & targetLayer) != 0)
        {
            IDamable target = other.GetComponent<IDamable>();
            if (target != null)
            {
                target.TakeDamage(damage);
                Debug.Log($"Projectile hit {other.name} for {damage} damage");
            }

            // Destroy projectile on hit
            Destroy(gameObject);
        }
        // Destroy on hitting walls/obstacles
        else if (other.CompareTag("Wall") || other.CompareTag("Obstacle"))
        {
            Debug.Log("Projectile hit obstacle and was destroyed");
            Destroy(gameObject);
        }
    }
}
