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

    public void Initialize(Vector3 dir, float spd, float dmg, LayerMask layer)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        targetLayer = layer;

        // Destroy projectile after lifetime
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // Move projectile
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
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
        // Destroy on hitting walls/obstacles (optional)
        else if (other.CompareTag("Wall") || other.CompareTag("Obstacle"))
        {
            Destroy(gameObject);
        }
    }
}
