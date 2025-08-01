using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicProjectile : MonoBehaviour
{
    [SerializeField] private float damage;
    [SerializeField] private LayerMask targetLayer;

    public void Initialize(float dmg, LayerMask layer)
    {
        damage = dmg;
        targetLayer = layer;

        // Add collider if none exists
        if (GetComponent<BoxCollider2D>() == null)
        {
            BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
        }

        // Destroy after 5 seconds
        Destroy(gameObject, 5f);
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & targetLayer) != 0)
        {
            IDamable target = other.GetComponent<IDamable>();
            if (target != null)
            {
                target.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
        else
        {
            Debug.Log($"Projectile hit target layer {targetLayer} layer: {other.gameObject.name} & layer is {other.gameObject.layer}");
        }
    }
 
}
