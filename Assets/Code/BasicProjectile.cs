using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicProjectile : MonoBehaviour
{
    [SerializeField] private float damage;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private float destroyTime = 5f;
    [SerializeField] private float projectileSpeed = 10f;
    private bool isInitialized = false;
    [SerializeField] bool onstay = false;
    private void Awake()
    {
        if (!isInitialized)
        {
            Initialize(damage , targetLayer, destroyTime);
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
            }
            Vector2 direction = Vector2.right; // Default direction
            rb.velocity = direction * projectileSpeed;
        }
    }
    public void Initialize(float dmg, LayerMask layer,float destroyTime)
    {
        isInitialized = true;
        damage = dmg;
        targetLayer = layer;

        // Add collider if none exists
        if (GetComponent<BoxCollider2D>() == null)
        {
            BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.5f, 0.5f);
        }

        // Destroy after 5 seconds
        Destroy(gameObject, destroyTime);
    }
    private void OnTriggerStay2D(Collider2D other)
    {
        if (onstay)
        {
            if (((1 << other.gameObject.layer) & targetLayer) != 0)
            {
                IDamable target = other.GetComponent<IDamable>();
                if (target != null)
                {
                    target.TakeDamage(damage);
                    onstay = true; // Set onstay to true to prevent multiple hits
                }
            }
            else
            {
                Debug.Log($"Projectile hit target layer {targetLayer} layer: {other.gameObject.name} & layer is {other.gameObject.layer}");
            }
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if(onstay)
        {
            return; // Prevent multiple hits
        }
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
