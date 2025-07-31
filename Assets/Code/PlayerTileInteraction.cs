using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTileInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public KeyCode interactionKey = KeyCode.E;
    public float interactionRange = 1.5f;
    public LayerMask interactionLayerMask = -1;

    private Camera playerCamera;

    void Start()
    {
        playerCamera = Camera.main;
    }

    void Update()
    {
        if (Input.GetKeyDown(interactionKey))
        {
            TryInteractWithTile();
        }

        // Optional: Show interaction indicator
        ShowInteractionIndicator();
    }

    void TryInteractWithTile()
    {
        Vector3 playerPosition = transform.position;
        Vector3 interactionPoint = GetInteractionPoint();
    }

    Vector3 GetInteractionPoint()
    {
        // Get interaction point based on player's facing direction
        Vector3 playerPosition = transform.position;

        // Simple method: use mouse position
        Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        // Check if mouse position is within interaction range
        if (Vector3.Distance(playerPosition, mouseWorldPos) <= interactionRange)
        {
            return mouseWorldPos;
        }

        // Fallback: use player's forward direction
        return playerPosition + transform.up * 1f; // Assuming 2D top-down
    }

    void ShowInteractionIndicator()
    {
        Vector3 interactionPoint = GetInteractionPoint();
        Vector3 playerPosition = transform.position;

        // Only show indicator if within range
        if (Vector3.Distance(playerPosition, interactionPoint) <= interactionRange)
        {
            // Draw a debug line to show interaction point (visible in Scene view)
            Debug.DrawLine(playerPosition, interactionPoint, Color.yellow, 0.1f);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw interaction range in editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
