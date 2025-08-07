 using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class CamMagger : MonoBehaviour
{
    public static CamMagger instance { get; private set; }
    public Camera mainCamera { get; private set; }

    [Header("Movement Settings")]
    [SerializeField] private float followSpeed = 8f;
    [SerializeField] private float freeMovementSpeed = 15f;
    [Range(0f, 1f)]
    [SerializeField] private float movementDamping = 0.9f;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 8f;
    [SerializeField] private float minZoom = 0.98f;
    [SerializeField] private float maxZoom = 7f;
    [Range(0f, 1f)]
    [SerializeField] private float zoomDamping = 0.85f;

    public Vector3 WorldMousePosition => mainCamera.ScreenToWorldPoint(Input.mousePosition);

    private Transform target; // Player transform
    private Vector3 offset = new Vector3(0, 0, -10);
    private Vector3 velocity = Vector3.zero;
    private float targetOrthographicSize;
    private float zoomVelocity = 0f;

    // Camera bounds
    [SerializeField] Vector2 maxCamBounds;
    private Vector2 minBounds;
    private Vector2 maxBounds;
    private bool boundsSet = false;

    // Cached values to reduce calculations
    private float camHeight;
    private float camWidth;
    private bool needsBoundsRecalculation = true;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        mainCamera = Camera.main;
        targetOrthographicSize = mainCamera.orthographicSize;

        // Initialize bounds
        SetCamBounds(-maxCamBounds, maxCamBounds);
    }

    private void Update()
    {
        HandleZoom();
        HandleMovement();
        ApplyCameraBounds();

    }

    private void HandleZoom()
    {
        float zoomInput = InputManager.instance.ZoomInput;

        if (zoomInput != 0)
        {
            // Direct zoom adjustment with velocity
            zoomVelocity += zoomInput * zoomSpeed * Time.deltaTime;
            targetOrthographicSize -= zoomVelocity;
            targetOrthographicSize = Mathf.Clamp(targetOrthographicSize, minZoom, maxZoom);

            needsBoundsRecalculation = true;
        }

        // Apply damping to zoom velocity
        zoomVelocity *= zoomDamping;

        // Faster zoom interpolation
        float zoomDifference = targetOrthographicSize - mainCamera.orthographicSize;
        if (Mathf.Abs(zoomDifference) > 0.01f)
        {
            mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, targetOrthographicSize, Time.deltaTime * 12f);
        }
        else
        {
            mainCamera.orthographicSize = targetOrthographicSize;
        }
    }

    private void HandleMovement()
    {
        Vector3 targetPosition;

        if (target == null)
        {
            // Free camera movement - scale speed by zoom level for consistent feel
            Vector2 input = InputManager.instance.MovementInput;

            // Scale movement speed by current orthographic size to maintain consistent feel
            float zoomScaledSpeed = freeMovementSpeed * mainCamera.orthographicSize;

            // Add to velocity instead of direct position change
            velocity.x += input.x * zoomScaledSpeed * Time.deltaTime;
            velocity.y += input.y * zoomScaledSpeed * Time.deltaTime;

            // Apply damping
            velocity *= movementDamping;

            targetPosition = transform.position + velocity;
            targetPosition.z = offset.z;
        }
        else
        {
            // Follow target - much more responsive
            targetPosition = target.position + offset;

            // Use SmoothDamp for better following behavior
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, 1f / followSpeed);
            return; // Exit early to avoid the lerp below
        }

        // Apply movement
        transform.position = targetPosition;
    }

    private void ApplyCameraBounds()
    {
        if (!boundsSet) return;

        // Only recalculate camera dimensions when zoom changes
        if (needsBoundsRecalculation)
        {
            camHeight = mainCamera.orthographicSize;
            camWidth = camHeight * mainCamera.aspect;
            needsBoundsRecalculation = false;
        }

        // Calculate bounds
        float minX = minBounds.x + camWidth;
        float maxX = maxBounds.x - camWidth;
        float minY = minBounds.y + camHeight;
        float maxY = maxBounds.y - camHeight;

        // Ensure bounds are valid
        if (minX > maxX) minX = maxX = (minBounds.x + maxBounds.x) * 0.5f;
        if (minY > maxY) minY = maxY = (minBounds.y + maxBounds.y) * 0.5f;

        // Clamp camera position
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        transform.position = pos;
    }

    public void SetCamBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
        boundsSet = true;
        needsBoundsRecalculation = true;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        // Reset velocity when switching targets to prevent jarring movement
        velocity = Vector3.zero;
        zoomVelocity = 0f;
    }

  
}
