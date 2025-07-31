using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

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

    private Dictionary<TileBase, InteractableTileMapping> tileMappings;
    private Dictionary<Vector2Int, List<GameObject>> chunkInteractables;

    private void Start()
    {
        InitializeSpawner();
    }

    private void InitializeSpawner()
    {
        // Initialize dictionaries
        tileMappings = new Dictionary<TileBase, InteractableTileMapping>();
        chunkInteractables = new Dictionary<Vector2Int, List<GameObject>>();

        // Build tile mappings dictionary
        foreach (var mapping in interactableMappings)
        {
            if (mapping.tile != null)
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
        SpawnExistingInteractables();
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
        if (destroyOnChunkUnload)
        {
            DestroyInteractablesInChunk(chunkPosition);
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

                if (tile != null && tileMappings.ContainsKey(tile))
                {
                    SpawnInteractableAtPosition(position, tileMappings[tile]);
                }
            }
        }
    }

    private void SpawnInteractablesInChunk(Vector2Int chunkPosition)
    {
        if (mainTilemap == null || tilemapManager == null) return;

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

        List<GameObject> spawnedObjects = new List<GameObject>();

        // Iterate through chunk tiles
        for (int x = chunkBounds.xMin; x < chunkBounds.xMax; x++)
        {
            for (int y = chunkBounds.yMin; y < chunkBounds.yMax; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);
                TileBase tile = mainTilemap.GetTile(position);

                if (tile != null && tileMappings.ContainsKey(tile))
                {
                    GameObject spawnedObj = SpawnInteractableAtPosition(position, tileMappings[tile]);
                    if (spawnedObj != null)
                    {
                        spawnedObjects.Add(spawnedObj);
                    }
                }
            }
        }

        // Store spawned objects for this chunk
        chunkInteractables[chunkPosition] = spawnedObjects;
    }

    private GameObject SpawnInteractableAtPosition(Vector3Int tilePosition, InteractableTileMapping mapping)
    {

        // Convert tile position to world position
        Vector3 worldPosition = mainTilemap.CellToWorld(tilePosition);
        worldPosition.z = 0; // Ensure Z is 0 for 2D

        // Spawn the interactable object
        GameObject spawnedObj = Instantiate(mapping.interactablePrefab, worldPosition, Quaternion.identity, interactableParent);

        // Ensure the object has an Iteract component
        Iteract iteractComponent = spawnedObj.GetComponent<Iteract>();
        if (iteractComponent == null)
        {
            iteractComponent = spawnedObj.AddComponent<Iteract>();
        }

        return spawnedObj;
    }

    private void DestroyInteractablesInChunk(Vector2Int chunkPosition)
    {
        if (chunkInteractables.ContainsKey(chunkPosition))
        {
            List<GameObject> objects = chunkInteractables[chunkPosition];

            foreach (GameObject obj in objects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }

            chunkInteractables.Remove(chunkPosition);
        }
    }

    // Public method to manually spawn an interactable at a specific position
    public GameObject SpawnInteractableManually(Vector3Int tilePosition, GameObject prefab)
    {
        Vector3 worldPosition = mainTilemap.CellToWorld(tilePosition);
        worldPosition.z = 0;

        GameObject spawnedObj = Instantiate(prefab, worldPosition, Quaternion.identity, interactableParent);

        Iteract iteractComponent = spawnedObj.GetComponent<Iteract>();
        if (iteractComponent == null)
        {
            iteractComponent = spawnedObj.AddComponent<Iteract>();
        }

        return spawnedObj;
    }


}