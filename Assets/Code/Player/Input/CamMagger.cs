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

    [Header("Free Cam Settings")]
    [SerializeField] private float freeCamTransitionSpeed = 5f;

    public Vector3 WorldMousePosition => mainCamera.ScreenToWorldPoint(Input.mousePosition);
    public bool IsFreeCamMode { get; private set; } = false;

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

    // Free cam state
    private Vector3 freeCamPosition;
    private bool wasFreeCamLastFrame = false;

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
        freeCamPosition = transform.position;

        // Initialize bounds
        SetCamBounds(-maxCamBounds, maxCamBounds);
    }

    private void Update()
    {
        HandleFreeCamInput();
        HandleZoom();
        HandleMovement();
        ApplyCameraBounds();
    }

    private void HandleFreeCamInput()
    {
        bool freeCamInput = InputManager.instance.IsFreeCamMode;

        // Toggle free cam mode based on input
        bool previousFreeCamMode = IsFreeCamMode;
        IsFreeCamMode = freeCamInput;

        // Handle transitions
        if (IsFreeCamMode && !wasFreeCamLastFrame)
        {
            // Just entered free cam mode - store current position
            freeCamPosition = transform.position;
            velocity = Vector3.zero; // Reset velocity for smooth transition
        }
        else if (!IsFreeCamMode && wasFreeCamLastFrame)
        {
            // Just exited free cam mode - reset velocity for smooth return to player
            velocity = Vector3.zero;
        }

        wasFreeCamLastFrame = IsFreeCamMode;
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

        if (IsFreeCamMode)
        {
            // Free camera movement - direct and responsive
            Vector2 input = InputManager.instance.MovementInput;

            // Scale movement speed by current orthographic size to maintain consistent feel
            float zoomScaledSpeed = freeMovementSpeed * mainCamera.orthographicSize;

            // Direct movement - much more responsive than velocity-based
            Vector3 movement = new Vector3(input.x, input.y, 0) * zoomScaledSpeed * Time.deltaTime;
            freeCamPosition += movement;
            freeCamPosition.z = offset.z;

            targetPosition = freeCamPosition;
        }
        else if (target != null)
        {
            // Follow target with bounds consideration
            Vector3 idealPosition = target.position + offset;

            // Apply bounds to the ideal position before moving camera
            Vector3 boundedIdealPosition = ApplyBoundsToPosition(idealPosition);

            // Use SmoothDamp for better following behavior
            targetPosition = Vector3.SmoothDamp(transform.position, boundedIdealPosition, ref velocity, 1f / followSpeed);
        }
        else
        {
            // No target and not in free cam - stay where we are
            targetPosition = transform.position;
        }

        // Apply movement
        transform.position = targetPosition;
    }

    private Vector3 ApplyBoundsToPosition(Vector3 position)
    {
        if (!boundsSet) return position;

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

        // Clamp position
        Vector3 clampedPos = position;
        clampedPos.x = Mathf.Clamp(clampedPos.x, minX, maxX);
        clampedPos.y = Mathf.Clamp(clampedPos.y, minY, maxY);

        return clampedPos;
    }

    private void ApplyCameraBounds()
    {
        if (!boundsSet) return;

        // Apply bounds to current position (this handles any edge cases)
        transform.position = ApplyBoundsToPosition(transform.position);

        // Update free cam position to match if we're in free cam mode
        if (IsFreeCamMode)
        {
            freeCamPosition = transform.position;
        }
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

        // If not in free cam mode, immediately update free cam position to current camera position
        if (!IsFreeCamMode)
        {
            freeCamPosition = transform.position;
        }
    }
}
