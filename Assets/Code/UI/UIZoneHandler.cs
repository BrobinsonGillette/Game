using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIZoneHandler : MonoBehaviour
{
    public static UIZoneHandler instance;

    [Header("UI Zone Settings")]
    [SerializeField] private bool enableUIZoneBlocking = true;
    [SerializeField] private float uiZoneBuffer = 50f; // Extra buffer around UI elements

    [Header("UI Zone Areas")]
    [SerializeField] private List<RectTransform> uiZoneRects = new List<RectTransform>();

    [Header("Debug")]
    [SerializeField] private bool showDebugZones = false;

    private Camera uiCamera;
    private Camera mainCamera; // For gizmo drawing
    private Canvas mainCanvas;

    [System.Serializable]
    public class UIZone
    {
        public string zoneName;
        public Rect screenRect; // In screen coordinates
        public bool isActive = true;
    }

    private void Awake()
    {
        InitializeSingleton();
        InitializeReferences();
    }

    private void InitializeSingleton()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple UIZoneHandler instances found! Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    private void InitializeReferences()
    {
        // Find the main canvas
        mainCanvas = FindObjectOfType<Canvas>();
        if (mainCanvas != null)
        {
            uiCamera = mainCanvas.worldCamera;
            if (uiCamera == null)
            {
                uiCamera = Camera.main;
            }
        }

        // Get main camera for gizmo drawing
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }

        AutoDetectUIZones();
    }

    private void Start()
    {
        AutoDetectUIZones();
    }

    /// <summary>
    /// Check if mouse position is within any UI zone
    /// </summary>
    public bool IsMouseOverUIZone()
    {
        if (!enableUIZoneBlocking) return false;

        Vector2 mousePosition = Input.mousePosition;

        // Check if mouse is over any UI element (Unity's built-in system)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }
        return IsMouseInAutoDetectedZones(mousePosition);
    }

    private bool IsMouseInAutoDetectedZones(Vector2 mousePosition)
    {
        foreach (RectTransform uiRect in uiZoneRects)
        {
            if (uiRect == null || !uiRect.gameObject.activeInHierarchy) continue;

            Rect screenRect = GetScreenRect(uiRect);

            // Add buffer around UI element
            screenRect.xMin -= uiZoneBuffer;
            screenRect.yMin -= uiZoneBuffer;
            screenRect.xMax += uiZoneBuffer;
            screenRect.yMax += uiZoneBuffer;

            if (screenRect.Contains(mousePosition))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Auto-detect UI zones by finding common UI elements
    /// </summary>
    private void AutoDetectUIZones()
    {
        uiZoneRects.Clear();

        // Find all Canvas components
        Canvas[] canvases = FindObjectsOfType<Canvas>();

        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay ||
                canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                // Find UI panels, buttons, and other interactive elements
                RectTransform[] rectTransforms = canvas.GetComponentsInChildren<RectTransform>();

                foreach (RectTransform rect in rectTransforms)
                {
                    // Skip the canvas itself
                    if (rect == canvas.transform) continue;

                    // Check if this is a UI element we should block
                    if (ShouldBlockUIElement(rect))
                    {
                        uiZoneRects.Add(rect);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Determine if a UI element should block tile clicks
    /// </summary>
    private bool ShouldBlockUIElement(RectTransform rectTransform)
    {
        GameObject go = rectTransform.gameObject;

        // Check for common UI components that should block clicks
        if (go.GetComponent<UnityEngine.UI.Button>() != null ||
            go.GetComponent<UnityEngine.UI.Dropdown>() != null ||
            go.GetComponent<UnityEngine.UI.InputField>() != null ||
            go.GetComponent<UnityEngine.UI.Slider>() != null ||
            go.GetComponent<UnityEngine.UI.Toggle>() != null ||
            go.GetComponent<UnityEngine.UI.Scrollbar>() != null ||
            go.GetComponent<UnityEngine.UI.ScrollRect>() != null)
        {
            return true;
        }

        // Check for panels or other containers with specific names or tags
        if (go.name.ToLower().Contains("panel") ||
            go.name.ToLower().Contains("menu") ||
            go.name.ToLower().Contains("hud") ||
            go.name.ToLower().Contains("ui") ||
            go.tag == "UIPanel")
        {
            return true;
        }

        // Check if the RectTransform is large enough to be considered a UI zone
        Rect rect = rectTransform.rect;
        if (rect.width > 100f && rect.height > 100f)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Convert RectTransform to screen space rectangle
    /// </summary>
    private Rect GetScreenRect(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        if (mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return new Rect(corners[0].x, corners[0].y,
                           corners[2].x - corners[0].x,
                           corners[2].y - corners[0].y);
        }
        else
        {
            // Convert world corners to screen points
            Vector2 min = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[2]);

            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }
    }

    /// <summary>
    /// Get screen rect with buffer applied (for gizmo drawing)
    /// </summary>
    private Rect GetScreenRectWithBuffer(RectTransform rectTransform)
    {
        Rect screenRect = GetScreenRect(rectTransform);

        // Add buffer around UI element
        screenRect.xMin -= uiZoneBuffer;
        screenRect.yMin -= uiZoneBuffer;
        screenRect.xMax += uiZoneBuffer;
        screenRect.yMax += uiZoneBuffer;

        return screenRect;
    }

    public void LateUpdate()
    {
        if (uiZoneRects.Count == 0 || uiZoneRects[0] == null || !uiZoneRects[0].gameObject.activeInHierarchy)
        {
            AutoDetectUIZones();
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebugZones || mainCamera == null) return;

        // Draw auto-detected zones with buffer
        Gizmos.color = Color.red;
        foreach (RectTransform rect in uiZoneRects)
        {
            if (rect != null && rect.gameObject.activeInHierarchy)
            {
                Rect screenRect = GetScreenRectWithBuffer(rect);
                DrawScreenRectGizmo(screenRect, Color.red);
            }
        }

        // Draw original zones (without buffer) in a different color for comparison
        Gizmos.color = Color.yellow;
        foreach (RectTransform rect in uiZoneRects)
        {
            if (rect != null && rect.gameObject.activeInHierarchy)
            {
                Rect screenRect = GetScreenRect(rect);
                DrawScreenRectGizmo(screenRect, Color.yellow);
            }
        }
    }

    /// <summary>
    /// Helper method to draw screen rect as world space gizmo
    /// </summary>
    private void DrawScreenRectGizmo(Rect screenRect, Color color)
    {
        if (mainCamera == null) return;

        Gizmos.color = color;

        // Convert screen rect corners to world space
        Vector3 bottomLeft = mainCamera.ScreenToWorldPoint(new Vector3(screenRect.xMin, screenRect.yMin, mainCamera.nearClipPlane + 1f));
        Vector3 bottomRight = mainCamera.ScreenToWorldPoint(new Vector3(screenRect.xMax, screenRect.yMin, mainCamera.nearClipPlane + 1f));
        Vector3 topLeft = mainCamera.ScreenToWorldPoint(new Vector3(screenRect.xMin, screenRect.yMax, mainCamera.nearClipPlane + 1f));
        Vector3 topRight = mainCamera.ScreenToWorldPoint(new Vector3(screenRect.xMax, screenRect.yMax, mainCamera.nearClipPlane + 1f));

        // Draw the rectangle outline
        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);

        // Optionally draw a filled rectangle (less visible but shows area)
        // Vector3 center = (bottomLeft + topRight) * 0.5f;
        // Vector3 size = new Vector3(Vector3.Distance(bottomLeft, bottomRight), Vector3.Distance(bottomLeft, topLeft), 0.1f);
        // Gizmos.DrawWireCube(center, size);
    }

    // Public methods for external use
    public void SetUIZoneBlocking(bool enabled)
    {
        enableUIZoneBlocking = enabled;
    }

    public void SetUIZoneBuffer(float buffer)
    {
        uiZoneBuffer = buffer;
    }

    public void SetShowDebugZones(bool show)
    {
        showDebugZones = show;
    }

    public void RefreshUIZones()
    {
        AutoDetectUIZones();
    }

    public void AddManualUIZone(RectTransform rectTransform)
    {
        if (rectTransform != null && !uiZoneRects.Contains(rectTransform))
        {
            uiZoneRects.Add(rectTransform);
        }
    }

    public void RemoveManualUIZone(RectTransform rectTransform)
    {
        if (rectTransform != null && uiZoneRects.Contains(rectTransform))
        {
            uiZoneRects.Remove(rectTransform);
        }
    }
}
