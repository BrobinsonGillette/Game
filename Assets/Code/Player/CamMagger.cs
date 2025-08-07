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
    public Vector3 WorldMousePosition => Camera.main.ScreenToWorldPoint(Input.mousePosition);
    private Transform target; // Player transform
    Vector3 offset = new Vector3(0, 0, -10);
    Vector3 smoothedPosition = Vector3.zero;
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
    }
    private void Update()
    {
        float zoomInput = InputManager.instance.ZoomInput;
        if (zoomInput != 0)
        {
            mainCamera.orthographicSize =Mathf.Lerp(mainCamera.orthographicSize, mainCamera.orthographicSize + zoomInput,Time.deltaTime); 
        }
        mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize, -3.28f, - 0.12f); 



        if (target == null)
        {
            Vector2 input = InputManager.instance.MovementInput;
            smoothedPosition = Vector3.Lerp(transform.position, new Vector3(transform.position .x- input.x, transform.position.y - input.y, offset.z), smoothSpeed * Time.deltaTime);
            transform.position = smoothedPosition;
            return;
        }
        else
        {
            Vector3 desiredPosition = target.position + offset;
            // Smooth camera movement using Lerp
            smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;
        }
   
    }
    public void SetCamBounds(Vector2 min, Vector2 max)
    {
        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, min.x, max.x);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, min.y, max.y);
        transform.position = clampedPosition;
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
