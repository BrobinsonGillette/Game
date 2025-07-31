using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerTileInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public KeyCode interactionKey = KeyCode.E;
    public float interactionRange = 1.5f;
    public LayerMask interactionLayerMask = -1;

    [Header("References")]
    public Tilemap mainTilemap;
    public ChunkedTilemapManager tilemapManager;

    private Camera playerCamera;
    private Dictionary<Vector3Int, Iteract> interactableObjects = new Dictionary<Vector3Int, Iteract>();

    void Start()
    {
        playerCamera = Camera.main;

        // Find tilemap manager if not assigned
        if (tilemapManager == null)
        {
            tilemapManager = FindObjectOfType<ChunkedTilemapManager>();
        }

        // Find main tilemap if not assigned
        if (mainTilemap == null && tilemapManager != null)
        {
            mainTilemap = tilemapManager.mainTilemap;
        }
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

        // Convert world position to tile position
        Vector3Int tilePosition = mainTilemap.WorldToCell(interactionPoint);

        // Check if there's an interactable object at this position
        if (interactableObjects.ContainsKey(tilePosition))
        {
            Iteract interactObj = interactableObjects[tilePosition];
            if (interactObj != null && !interactObj.IsoutOfFrame())
            {
                interactObj.InteractWithObject(tilePosition, gameObject);
                Debug.Log($"Interacted with object at {tilePosition}");
            }
        }
        else
        {
            // Try to find nearby interactable objects
            TryInteractWithNearbyObjects(tilePosition);
        }
    }

    void TryInteractWithNearbyObjects(Vector3Int centerTile)
    {
        // Check surrounding tiles for interactables
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int checkPos = centerTile + new Vector3Int(x, y, 0);

                if (interactableObjects.ContainsKey(checkPos))
                {
                    Iteract interactObj = interactableObjects[checkPos];
                    if (interactObj != null && !interactObj.IsoutOfFrame())
                    {
                        Vector3 objWorldPos = mainTilemap.CellToWorld(checkPos);
                        if (Vector3.Distance(transform.position, objWorldPos) <= interactionRange)
                        {
                            interactObj.InteractWithObject(checkPos, gameObject);
                            Debug.Log($"Interacted with nearby object at {checkPos}");
                            return;
                        }
                    }
                }
            }
        }

        Debug.Log("No interactable objects found nearby");
    }

    Vector3 GetInteractionPoint()
    {
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
        return playerPosition + Vector3.up * 1f; // Assuming 2D top-down
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

            // Check if there's an interactable at this position
            Vector3Int tilePos = mainTilemap.WorldToCell(interactionPoint);
            if (interactableObjects.ContainsKey(tilePos))
            {
                Debug.DrawLine(playerPosition, mainTilemap.CellToWorld(tilePos), Color.green, 0.1f);
            }
        }
    }

    // Public methods for registering/unregistering interactable objects
    public void RegisterInteractable(Vector3Int tilePosition, Iteract interactable)
    {
        interactableObjects[tilePosition] = interactable;
    }

    public void UnregisterInteractable(Vector3Int tilePosition)
    {
        if (interactableObjects.ContainsKey(tilePosition))
        {
            interactableObjects.Remove(tilePosition);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw interaction range in editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}