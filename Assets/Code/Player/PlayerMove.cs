using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public static PlayerMove instance { get; private set; }
    public Rigidbody2D rb;
    public SpriteRenderer spriteRenderer;
    public InputManager inputSystem;
    public PlayerCam playerCam;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Initialize components
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // Setup input system
        inputSystem.StartUp();

        // Setup camera to follow this player
        if (playerCam != null)
        {
            playerCam.SetTarget(transform);
        }

        // Initialize physics settings
        rb.freezeRotation = true; // Prevent rotation for 2D movement


    }

    void Update()
    {
      
    }

  
}
