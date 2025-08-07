 using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerCam : MonoBehaviour
{
    public static PlayerCam instance { get; private set; }
    [Header("Camera Settings")]
    [SerializeField] private Transform target; // Player transform
    [SerializeField] private float smoothSpeed = 0.125f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);
    [SerializeField] private bool useLerpSmoothing = true;

    [Header("Camera Boundaries (Optional)")]
    [SerializeField] private bool useBoundaries = false;
    [SerializeField] private Vector2 minBounds;
    [SerializeField] private Vector2 maxBounds;
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
    }
    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;

        // Apply boundaries if enabled
        if (useBoundaries)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);
        }

        if (useLerpSmoothing)
        {
            // Smooth camera movement using Lerp
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;
        }
        else
        {
            // Instant camera follow
            transform.position = desiredPosition;
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetSmoothSpeed(float newSpeed)
    {
        smoothSpeed = Mathf.Clamp01(newSpeed);
    }
}
