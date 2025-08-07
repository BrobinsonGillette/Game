 using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class CamMagger : MonoBehaviour
{
    public static CamMagger instance { get; private set; }
    public Camera mainCamera { get; private set; }

    [Header("Camera Settings")]
    [SerializeField] private float smoothSpeed = 0.125f;
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minZoom = 0.98f;
    [SerializeField] private float maxZoom = 7f;

    public Vector3 WorldMousePosition => Camera.main.ScreenToWorldPoint(Input.mousePosition);

    private Transform target; // Player transform
    private Vector3 offset = new Vector3(0, 0, -10);
    private Vector3 smoothedPosition = Vector3.zero;
    private float targetOrthographicSize;

    // Camera bounds
    private Vector2 minBounds;
    private Vector2 maxBounds;
    private bool boundsSet = false;

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

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        mainCamera = Camera.main;
        targetOrthographicSize = mainCamera.orthographicSize;
    }

    private void Update()
    {
        HandleZoom();
        HandleMovement();
        ApplyCameraBounds();
        SetCamBounds(new Vector2(-62, -93), new Vector2(62, 93));
    }

    private void HandleZoom()
    {
        float zoomInput = InputManager.instance.ZoomInput;

        if (zoomInput != 0)
        {
            // Calculate target zoom size
            targetOrthographicSize -= zoomInput * zoomSpeed * Time.deltaTime;
            targetOrthographicSize = Mathf.Clamp(targetOrthographicSize, minZoom, maxZoom);
        }

        // Smoothly interpolate to target zoom
        mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, targetOrthographicSize, Time.deltaTime * 5f);
    }

    private void HandleMovement()
    {
        if (target == null)
        {
            // Free camera movement with input
            Vector2 input = InputManager.instance.MovementInput;
            Vector3 targetPosition = new Vector3(
                transform.position.x + input.x * smoothSpeed * Time.deltaTime,
                transform.position.y + input.y * smoothSpeed * Time.deltaTime,
                offset.z
            );

            smoothedPosition = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        }
        else
        {
            // Follow target
            Vector3 desiredPosition = target.position + offset;
            smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        }

        transform.position = smoothedPosition;
    }

    private void ApplyCameraBounds()
    {
        if (!boundsSet) return;

        // Calculate camera bounds based on orthographic size and aspect ratio
        float camHeight = mainCamera.orthographicSize;
        float camWidth = camHeight * mainCamera.aspect;

        // Adjust bounds to account for camera size
        float minX = minBounds.x + camWidth;
        float maxX = maxBounds.x - camWidth;
        float minY = minBounds.y + camHeight;
        float maxY = maxBounds.y - camHeight;

        // Clamp camera position
        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, minX, maxX);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, minY, maxY);

        transform.position = clampedPosition;
    }

    public void SetCamBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
        boundsSet = true;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }


}
