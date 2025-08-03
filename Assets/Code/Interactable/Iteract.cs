using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Iteract : MonoBehaviour
{
    [Header("Interactable Settings")]
    public Interactable interactable;

    [Header("Visibility Settings")]
    public bool hideWhenOutOfFrame = true;
    public float visibilityCheckInterval = 0.1f;

    private Camera cam;
    private PlayerTileInteraction playerInteraction;
    private Vector3Int tilePosition;
    private Renderer[] renderers;
    private Collider2D[] colliders;
    private bool isVisible = true;
    private Coroutine visibilityCheckCoroutine;

    private void Awake()
    {
        cam = Camera.main;

        // Get all renderers and colliders for visibility management
        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider2D>();

        // Calculate tile position
        tilePosition = Vector3Int.FloorToInt(transform.position);
    }

    private void Start()
    {
        // Find and register with player interaction system
        playerInteraction = FindObjectOfType<PlayerTileInteraction>();
        if (playerInteraction != null)
        {
            playerInteraction.RegisterInteractable(tilePosition, this);
        }

        // Start visibility checking
        if (hideWhenOutOfFrame)
        {
            visibilityCheckCoroutine = StartCoroutine(VisibilityCheckLoop());
        }
    }

    private void OnDestroy()
    {
        // Unregister from player interaction system
        if (playerInteraction != null)
        {
            playerInteraction.UnregisterInteractable(tilePosition);
        }

        // Stop visibility checking
        if (visibilityCheckCoroutine != null)
        {
            StopCoroutine(visibilityCheckCoroutine);
        }
    }

    private IEnumerator VisibilityCheckLoop()
    {
        while (true)
        {
            bool shouldBeVisible = !IsoutOfFrame();

            if (shouldBeVisible != isVisible)
            {
                SetVisibility(shouldBeVisible);
            }

            yield return new WaitForSeconds(visibilityCheckInterval);
        }
    }

    public bool IsoutOfFrame()
    {
        if (cam == null) return true;

        Vector3 viewportPoint = cam.WorldToViewportPoint(transform.position);

        // Add a small buffer to prevent flickering at edges
        float buffer = 0.1f;
        return viewportPoint.x < -buffer ||
               viewportPoint.x > 1 + buffer ||
               viewportPoint.y < -buffer ||
               viewportPoint.y > 1 + buffer;
    }

    private void SetVisibility(bool visible)
    {
        isVisible = visible;

        // Toggle renderers
        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }

        // Toggle colliders (optional - you might want to keep colliders active)
        foreach (var collider in colliders)
        {
            if (collider != null)
            {
                collider.enabled = visible;
            }
        }

        // Debug log for testing
        if (Application.isEditor)
        {
            Debug.Log($"{gameObject.name} visibility set to: {visible}");
        }
    }

    public void InteractWithObject(Vector3Int tilePos, GameObject player)
    {
        if (interactable != null && isVisible)
        {
            interactable.OnInteract(tilePos, player);
        }
        else if (!isVisible)
        {
            Debug.Log("Cannot interact with object - it's not visible!");
        }
        else
        {
            Debug.Log("No interactable assigned to this object!");
        }
    }


    private void OnDrawGizmosSelected()
    {
        // Draw interaction indicator in editor
        Gizmos.color = isVisible ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);

        if (interactable != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}
