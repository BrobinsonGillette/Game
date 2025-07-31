using System.Collections.Generic;
using UnityEngine.Tilemaps;
using UnityEngine;

[System.Serializable]
public class InteractableTileMapping
{
    public TileBase tile;
    public GameObject interactablePrefab;
}

public class InteractionObjectSpawner : MonoBehaviour
{
    [Header("References")]
    public ChunkedTilemapManager tilemapManager;
    public Tilemap mainTilemap;

    [Header("Interactable Mappings")]
    public InteractableTileMapping[] interactableMappings;

    [Header("Spawning Settings")]
    public Transform interactableParent;
    public bool spawnOnChunkLoad = true;
    public bool destroyOnChunkUnload = true;

    [Header("Position Offset")]
    public Vector2 positionOffset = new Vector2(0.5f, 0.5f);

    private Dictionary<TileBase, InteractableTileMapping> tileMappings;
    private Dictionary<Vector2Int, List<GameObject>> chunkInteractables;
    private HashSet<Vector3Int> spawnedPositions; // Track spawned positions to avoid duplicates

    private void Start()
    {
        InitializeSpawner();
    }

    private void InitializeSpawner()
    {
        // Initialize dictionaries
        tileMappings = new Dictionary<TileBase, InteractableTileMapping>();
        chunkInteractables = new Dictionary<Vector2Int, List<GameObject>>();
        spawnedPositions = new HashSet<Vector3Int>();

        // Build tile mappings dictionary
        foreach (var mapping in interactableMappings)
        {
            if (mapping.tile != null && mapping.interactablePrefab != null)
            {
                tileMappings[mapping.tile] = mapping;
            }
        }

        // Find references if not assigned
        if (tilemapManager == null)
        {
            tilemapManager = FindObjectOfType<ChunkedTilemapManager>();
        }

        if (mainTilemap == null && tilemapManager != null)
        {
            mainTilemap = tilemapManager.mainTilemap;
        }

        // Create parent object if not assigned
        if (interactableParent == null)
        {
            GameObject parentObj = new GameObject("Interactable Objects");
            interactableParent = parentObj.transform;
        }

        // Subscribe to chunk events
        if (tilemapManager != null)
        {
            tilemapManager.OnChunkLoaded += OnChunkLoaded;
            tilemapManager.OnChunkUnloaded += OnChunkUnloaded;
        }

        // Spawn interactables for already loaded chunks
        if (tilemapManager != null)
        {
            SpawnInteractablesForLoadedChunks();
        }
        else
        {
            // If no chunk manager, spawn for entire tilemap
            SpawnExistingInteractables();
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (tilemapManager != null)
        {
            tilemapManager.OnChunkLoaded -= OnChunkLoaded;
            tilemapManager.OnChunkUnloaded -= OnChunkUnloaded;
        }
    }

    private void OnChunkLoaded(Vector2Int chunkPosition)
    {
        if (spawnOnChunkLoad)
        {
            SpawnInteractablesInChunk(chunkPosition);
        }
    }

    private void OnChunkUnloaded(Vector2Int chunkPosition)
    {
        if (destroyOnChunkUnload && chunkInteractables.ContainsKey(chunkPosition))
        {
            List<GameObject> objects = chunkInteractables[chunkPosition];

            foreach (GameObject obj in objects)
            {
                if (obj != null)
                {
                    // Remove from spawned positions tracker
                    Vector3Int tilePos = mainTilemap.WorldToCell(obj.transform.position);
                    spawnedPositions.Remove(tilePos);

                    // Destroy the object
                    DestroyImmediate(obj);
                }
            }

            chunkInteractables.Remove(chunkPosition);
        }
    }

    private void SpawnInteractablesForLoadedChunks()
    {
        // Get all currently loaded chunks and spawn interactables for them
        var loadedChunks = tilemapManager.GetLoadedChunks();

        foreach (var chunkPosition in loadedChunks)
        {
            SpawnInteractablesInChunk(chunkPosition);
        }
    }

    private void SpawnExistingInteractables()
    {
        if (mainTilemap == null) return;

        // Get current tilemap bounds
        BoundsInt bounds = mainTilemap.cellBounds;

        // Iterate through all tiles
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);
                TileBase tile = mainTilemap.GetTile(position);

                if (tile != null && tileMappings.ContainsKey(tile) && !spawnedPositions.Contains(position))
                {
                    SpawnInteractableAtPosition(position, tileMappings[tile]);
                }
            }
        }
    }

    private void SpawnInteractablesInChunk(Vector2Int chunkPosition)
    {
        if (mainTilemap == null || tilemapManager == null) return;

        // Initialize list for this chunk if it doesn't exist
        if (!chunkInteractables.ContainsKey(chunkPosition))
        {
            chunkInteractables[chunkPosition] = new List<GameObject>();
        }

        List<GameObject> chunkObjects = chunkInteractables[chunkPosition];

        // Calculate chunk bounds
        Vector2Int worldStart = chunkPosition * tilemapManager.chunkSize;
        BoundsInt chunkBounds = new BoundsInt(
            worldStart.x,
            worldStart.y,
            0,
            tilemapManager.chunkSize,
            tilemapManager.chunkSize,
            1
        );

        // Iterate through chunk tiles
        for (int x = chunkBounds.xMin; x < chunkBounds.xMax; x++)
        {
            for (int y = chunkBounds.yMin; y < chunkBounds.yMax; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);
                TileBase tile = mainTilemap.GetTile(position);

                if (tile != null && tileMappings.ContainsKey(tile) && !spawnedPositions.Contains(position))
                {
                    GameObject spawnedObj = SpawnInteractableAtPosition(position, tileMappings[tile]);
                    if (spawnedObj != null)
                    {
                        chunkObjects.Add(spawnedObj);
                    }
                }
            }
        }
    }

    private GameObject SpawnInteractableAtPosition(Vector3Int tilePosition, InteractableTileMapping mapping)
    {
        // Check if already spawned at this position
        if (spawnedPositions.Contains(tilePosition))
        {
            return null;
        }

        // Convert tile position to world position
        Vector3 worldPosition = mainTilemap.CellToWorld(tilePosition);

        // Apply offset to center the object on the tile
        worldPosition.x += positionOffset.x;
        worldPosition.y += positionOffset.y;
        worldPosition.z = 0; // Ensure Z is 0 for 2D

        // Spawn the interactable object
        GameObject spawnedObj = Instantiate(mapping.interactablePrefab, worldPosition, Quaternion.identity, interactableParent);

        // Ensure the object has an Interact component (fixed typo)
        Iteract iteractComponent = spawnedObj.GetComponent<Iteract>();
        if (iteractComponent == null)
        {
            iteractComponent = spawnedObj.AddComponent<Iteract>();
        }

        // Track this position as spawned
        spawnedPositions.Add(tilePosition);

        return spawnedObj;
    }

    // Public method to manually spawn interactables (useful for testing)
    public void ManualSpawnAll()
    {
        SpawnExistingInteractables();
    }

    // Public method to clear all spawned interactables
    public void ClearAllInteractables()
    {
        foreach (var chunkObjects in chunkInteractables.Values)
        {
            foreach (var obj in chunkObjects)
            {
                if (obj != null)
                {
                    DestroyImmediate(obj);
                }
            }
        }

        chunkInteractables.Clear();
        spawnedPositions.Clear();
    }

    // Get spawned object at specific tile position
    public GameObject GetInteractableAtPosition(Vector3Int tilePosition)
    {
        if (!spawnedPositions.Contains(tilePosition))
            return null;

        Vector3 worldPos = mainTilemap.CellToWorld(tilePosition);
        worldPos.x += positionOffset.x;
        worldPos.y += positionOffset.y;

        // Find object near this position
        foreach (var chunkObjects in chunkInteractables.Values)
        {
            foreach (var obj in chunkObjects)
            {
                if (obj != null && Vector3.Distance(obj.transform.position, worldPos) < 0.1f)
                {
                    return obj;
                }
            }
        }

        return null;
    }
}