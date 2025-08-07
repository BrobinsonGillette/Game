 using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CamMagger : MonoBehaviour
{
    public static CamMagger instance { get; private set; }
    [Header("Camera Settings")]
    [SerializeField] private Transform target; // Player transform
    [SerializeField] private float smoothSpeed = 0.125f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);

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
    private void Update()
    {
        Vector3 smoothedPosition = Vector3.zero;

        if (target == null)
        {
            Vector2 input = InputManager.instance.Movement.action.ReadValue<Vector2>();
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

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetSmoothSpeed(float newSpeed)
    {
        smoothSpeed = Mathf.Clamp01(newSpeed);
    }
}
