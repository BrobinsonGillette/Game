using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerTileInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionRange = 1.5f;
    public LayerMask interactionLayerMask = -1;
    public float interactionCooldown = 0.5f;
    [Header("Search Settings")]
    public int searchRadius = 2; // Increased search radius
    public bool prioritizePlayerPosition = true; // New option to prioritize player position over mouse

    public Tilemap mainTilemap { get; private set; }
    public ChunkedTilemapManager tilemapManager { get; private set; }

    private Camera playerCamera;
    private Dictionary<Vector3Int, Iteract> interactableObjects = new Dictionary<Vector3Int, Iteract>();
    private float lastInteractionTime = 0f;
    private bool inputPressed = false;

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
        // Check for input press (not hold) with cooldown
        float currentInteractValue = PlayerMove.instance.inputSystem.Interact.action.ReadValue<float>();

        if (currentInteractValue > 0 && !inputPressed && Time.time >= lastInteractionTime + interactionCooldown)
        {
            inputPressed = true;
            TryInteractWithTile();
            lastInteractionTime = Time.time;
        }
        else if (currentInteractValue == 0)
        {
            inputPressed = false;
        }

        // Optional: Show interaction indicator
        ShowInteractionIndicator();
    }

    void TryInteractWithTile()
    {
        Vector3 playerPosition = transform.position;

        if (mainTilemap == null)
        {
            tilemapManager = FindObjectOfType<ChunkedTilemapManager>();
            mainTilemap = tilemapManager != null ? tilemapManager.mainTilemap : null;
            Debug.Log("Main Tilemap is not assigned or found!");
            return;
        }

        // Always check player position first
        Vector3Int playerTilePos = mainTilemap.WorldToCell(playerPosition);

        // Check if there's an interactable object at player position
        if (interactableObjects.ContainsKey(playerTilePos))
        {
            Iteract interactObj = interactableObjects[playerTilePos];
            if (interactObj != null && !interactObj.IsoutOfFrame())
            {
                interactObj.InteractWithObject(playerTilePos, gameObject);
                Debug.Log($"Interacted with object at player position {playerTilePos}");
                return;
            }
        }

        // If no object at player position, check interaction point
        Vector3 interactionPoint = GetInteractionPoint();
        Vector3Int interactionTilePos = mainTilemap.WorldToCell(interactionPoint);

        // Check if there's an interactable object at interaction point (if different from player position)
        if (interactionTilePos != playerTilePos && interactableObjects.ContainsKey(interactionTilePos))
        {
            Iteract interactObj = interactableObjects[interactionTilePos];
            if (interactObj != null && !interactObj.IsoutOfFrame())
            {
                float distance = Vector3.Distance(playerPosition, mainTilemap.CellToWorld(interactionTilePos));
                if (distance <= interactionRange)
                {
                    interactObj.InteractWithObject(interactionTilePos, gameObject);
                    Debug.Log($"Interacted with object at interaction point {interactionTilePos}");
                    return;
                }
            }
        }

        // If no direct interaction found, search nearby
        TryInteractWithNearbyObjects(playerTilePos);
    }

    void TryInteractWithNearbyObjects(Vector3Int centerTile)
    {
        List<KeyValuePair<Vector3Int, float>> nearbyInteractables = new List<KeyValuePair<Vector3Int, float>>();

        // Use configurable search radius instead of fixed 3x3
        for (int x = -searchRadius; x <= searchRadius; x++)
        {
            for (int y = -searchRadius; y <= searchRadius; y++)
            {
                // Skip center tile (already checked)
                if (x == 0 && y == 0) continue;

                Vector3Int checkPos = centerTile + new Vector3Int(x, y, 0);

                if (interactableObjects.ContainsKey(checkPos))
                {
                    Iteract interactObj = interactableObjects[checkPos];
                    if (interactObj != null && !interactObj.IsoutOfFrame())
                    {
                        Vector3 objWorldPos = mainTilemap.CellToWorld(checkPos);
                        float distance = Vector3.Distance(transform.position, objWorldPos);

                        if (distance <= interactionRange)
                        {
                            nearbyInteractables.Add(new KeyValuePair<Vector3Int, float>(checkPos, distance));
                        }
                    }
                }
            }
        }

        // Interact with the closest object only
        if (nearbyInteractables.Count > 0)
        {
            // Sort by distance and take the closest one
            nearbyInteractables.Sort((a, b) => a.Value.CompareTo(b.Value));
            Vector3Int closestPos = nearbyInteractables[0].Key;

            Iteract closestObj = interactableObjects[closestPos];
            closestObj.InteractWithObject(closestPos, gameObject);
            Debug.Log($"Interacted with closest nearby object at {closestPos}, distance: {nearbyInteractables[0].Value:F2}");
        }
        else
        {
            Debug.Log($"No interactable objects found within range. Searched {nearbyInteractables.Count} objects in {searchRadius} tile radius");
        }
    }

    Vector3 GetInteractionPoint()
    {
        Vector3 playerPosition = transform.position;

        // If prioritizing player position, return it directly
        if (prioritizePlayerPosition)
        {
            return playerPosition;
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            return playerPosition;
        }

        // Use mouse position if not prioritizing player position
        Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        // Check if mouse position is within interaction range
        if (Vector3.Distance(playerPosition, mouseWorldPos) <= interactionRange)
        {
            return mouseWorldPos;
        }

        // Fallback: use player position
        return playerPosition;
    }

    void ShowInteractionIndicator()
    {
        Vector3 playerPosition = transform.position;

        if (mainTilemap == null) return;

        // Always show indicator at player position
        Vector3Int playerTilePos = mainTilemap.WorldToCell(playerPosition);
        Debug.DrawLine(playerPosition, mainTilemap.CellToWorld(playerTilePos), Color.blue, 0.1f);

        // Show interaction point if different
        Vector3 interactionPoint = GetInteractionPoint();
        if (Vector3.Distance(playerPosition, interactionPoint) <= interactionRange)
        {
            Debug.DrawLine(playerPosition, interactionPoint, Color.yellow, 0.1f);

            Vector3Int tilePos = mainTilemap.WorldToCell(interactionPoint);
            if (interactableObjects.ContainsKey(tilePos))
            {
                Debug.DrawLine(playerPosition, mainTilemap.CellToWorld(tilePos), Color.green, 0.1f);
            }
        }

        // Show all nearby interactables within range
        for (int x = -searchRadius; x <= searchRadius; x++)
        {
            for (int y = -searchRadius; y <= searchRadius; y++)
            {
                Vector3Int checkPos = playerTilePos + new Vector3Int(x, y, 0);
                if (interactableObjects.ContainsKey(checkPos))
                {
                    Vector3 objWorldPos = mainTilemap.CellToWorld(checkPos);
                    float distance = Vector3.Distance(playerPosition, objWorldPos);
                    if (distance <= interactionRange)
                    {
                        Debug.DrawLine(playerPosition, objWorldPos, Color.cyan, 0.1f);
                    }
                }
            }
        }
    }

    // Public methods for registering/unregistering interactable objects
    public void RegisterInteractable(Vector3Int tilePosition, Iteract interactable)
    {
        if (!interactableObjects.ContainsKey(tilePosition))
        {
            interactableObjects[tilePosition] = interactable;
            Debug.Log($"Registered interactable at {tilePosition}");
        }
        else
        {
            Debug.LogWarning($"Interactable already exists at {tilePosition}, skipping registration");
        }
    }

    public void UnregisterInteractable(Vector3Int tilePosition)
    {
        if (interactableObjects.ContainsKey(tilePosition))
        {
            interactableObjects.Remove(tilePosition);
            Debug.Log($"Unregistered interactable at {tilePosition}");
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw interaction range in editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);

        // Draw search area
        if (mainTilemap != null)
        {
            Vector3Int playerTilePos = mainTilemap.WorldToCell(transform.position);
            Gizmos.color = Color.cyan;
            for (int x = -searchRadius; x <= searchRadius; x++)
            {
                for (int y = -searchRadius; y <= searchRadius; y++)
                {
                    Vector3Int checkPos = playerTilePos + new Vector3Int(x, y, 0);
                    Vector3 worldPos = mainTilemap.CellToWorld(checkPos);
                    Gizmos.DrawWireCube(worldPos, Vector3.one * 0.1f);
                }
            }
        }
    }
}