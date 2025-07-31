using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public static PlayerMove instance { get; private set; }
    [SerializeField] private PlayerCam playerCam;
    [SerializeField] private SpriteRenderer spriteRenderer;
    public InputManager inputSystem;
    [SerializeField] private Rigidbody2D rb;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private bool flipSpriteOnDirection = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private Vector2 moveDirection;
    private bool isSprinting;
    private float currentSpeed;
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
        HandleInput();
        HandleSpriteFlipping();

        if (showDebugInfo)
        {
            Debug.Log($"Move Direction: {moveDirection}, Is Sprinting: {isSprinting}, Current Speed: {currentSpeed}");
        }
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    private void HandleInput()
    {
        // Get movement input
        Vector2 input = inputSystem.Movement.action.ReadValue<Vector2>();
        moveDirection = input.normalized;

        // Check if sprinting
        isSprinting = inputSystem.Sprint.action.ReadValue<float>() > 0;

        // Determine target speed
        float targetSpeed = isSprinting ? sprintSpeed : moveSpeed;

        // Apply acceleration/deceleration
        if (moveDirection.magnitude > 0)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, deceleration * Time.deltaTime);
        }
    }

    private void MovePlayer()
    {
        // Calculate target velocity
        Vector2 targetVelocity = moveDirection * currentSpeed;

        // Apply movement to rigidbody
        rb.velocity = targetVelocity;
    }

    private void HandleSpriteFlipping()
    {
        if (!flipSpriteOnDirection || spriteRenderer == null) return;

        if (moveDirection.x > 0)
        {
            spriteRenderer.flipX = false;
        }
        else if (moveDirection.x < 0)
        {
            spriteRenderer.flipX = true;
        }
    }

    void OnDestroy()
    {
        // Clean up input system
        inputSystem.DeSetUp();
    }

    // Public methods for external access
    public Vector2 GetMoveDirection() => moveDirection;
    public bool IsSprinting() => isSprinting;
    public bool IsMoving() => moveDirection.magnitude > 0;
    public float GetCurrentSpeed() => currentSpeed;
}
