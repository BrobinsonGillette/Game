using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class ChunkData
{
    public Vector2Int chunkPosition;
    public TileBase[,] mainTiles;
    public TileBase[,] backgroundTiles;
    public bool isLoaded;
    public bool isGenerated;

    public ChunkData(Vector2Int position, int chunkSize)
    {
        chunkPosition = position;
        mainTiles = new TileBase[chunkSize, chunkSize];
        backgroundTiles = new TileBase[chunkSize, chunkSize];
        isLoaded = false;
        isGenerated = false;
    }
}

[System.Serializable]
public class Tile
{
    public TileBase tiles;
    public float probability = 1f; // Probability of selecting a tile from this set
}

[System.Serializable]
public class TileSet
{
    public Tile[] tiles;
    public TileBase GetRandomTile()
    {
        if (tiles == null || tiles.Length == 0) return null;
        foreach (Tile tile in tiles)
        {
            if (tile == null || tile.tiles == null) continue;
            float probability = tile.probability;
            if (Random.value <= probability)
            {
                return tile.tiles;
            }
        }
        return null; // No tile selected based on probability
    }
}
public class ChunkedTilemapManager : MonoBehaviour
{
    [Header("Player Reference")]
    public Transform player;

    [Header("Tilemaps")]
    public Tilemap mainTilemap;
    public Tilemap collisionTilemap;
    public Tilemap backgroundTilemap;

    [Header("Chunk Settings")]
    public int chunkSize = 16; // Size of each chunk (16x16 tiles)
    public int loadRadius = 2; // How many chunks around player to keep loaded
    public float updateInterval = 0.5f; // How often to check for chunk updates

    [Header("Tiles for Auto-Fill")]
    public TileSet rockTile;
    public TileSet grassTile;
    public TileBase[] collisionTiles;
    public TileBase[] interactiveTiles;

    [Header("Generation Settings")]
    [Range(0f, 1f)]
    public float rockProbability = 0.3f;
    [Range(0f, 1f)]
    public float grassProbability = 0.7f;
    public bool generateChunksOnDemand = true;

    [Header("Performance Settings")]
    public int maxChunksToLoadPerFrame = 1;
    public int maxChunksToUnloadPerFrame = 2;

    private Dictionary<Vector2Int, ChunkData> chunks = new Dictionary<Vector2Int, ChunkData>();
    private HashSet<Vector2Int> loadedChunks = new HashSet<Vector2Int>();
    private Vector2Int lastPlayerChunk;
    private Coroutine chunkUpdateCoroutine;

    // For saving/loading painted tiles
    private Dictionary<Vector3Int, TileBase> paintedTiles = new Dictionary<Vector3Int, TileBase>();

    void Start()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player == null)
            {
                Debug.LogError("Player not found! Please assign player transform or add 'Player' tag.");
                return;
            }
        }

        // Save any pre-painted tiles
        SaveExistingTiles();

        // Start the chunk update coroutine
        chunkUpdateCoroutine = StartCoroutine(UpdateChunksAroundPlayer());

        // Initial load
        UpdatePlayerChunk();
    }

    void SaveExistingTiles()
    {
        if (mainTilemap == null) return;

        BoundsInt bounds = mainTilemap.cellBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);
                TileBase tile = mainTilemap.GetTile(position);
                if (tile != null)
                {
                    paintedTiles[position] = tile;
                }
            }
        }

        Debug.Log($"Saved {paintedTiles.Count} existing painted tiles");
    }

    IEnumerator UpdateChunksAroundPlayer()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);

            if (player == null) continue;

            Vector2Int currentPlayerChunk = WorldToChunk(player.position);

            if (currentPlayerChunk != lastPlayerChunk)
            {
                UpdatePlayerChunk();
                lastPlayerChunk = currentPlayerChunk;
            }
        }
    }

    void UpdatePlayerChunk()
    {
        if (player == null) return;

        Vector2Int playerChunk = WorldToChunk(player.position);
        HashSet<Vector2Int> chunksToLoad = new HashSet<Vector2Int>();

        // Determine which chunks should be loaded
        for (int x = -loadRadius; x <= loadRadius; x++)
        {
            for (int y = -loadRadius; y <= loadRadius; y++)
            {
                Vector2Int chunkPos = playerChunk + new Vector2Int(x, y);
                chunksToLoad.Add(chunkPos);
            }
        }

        // Start loading/unloading chunks
        StartCoroutine(LoadUnloadChunks(chunksToLoad));
    }

    IEnumerator LoadUnloadChunks(HashSet<Vector2Int> targetChunks)
    {
        // Unload chunks that are too far
        List<Vector2Int> chunksToUnload = new List<Vector2Int>();
        foreach (Vector2Int loadedChunk in loadedChunks)
        {
            if (!targetChunks.Contains(loadedChunk))
            {
                chunksToUnload.Add(loadedChunk);
            }
        }

        int unloadedThisFrame = 0;
        foreach (Vector2Int chunkPos in chunksToUnload)
        {
            if (unloadedThisFrame >= maxChunksToUnloadPerFrame)
            {
                yield return null;
                unloadedThisFrame = 0;
            }

            UnloadChunk(chunkPos);
            unloadedThisFrame++;
        }

        // Load new chunks
        int loadedThisFrame = 0;
        foreach (Vector2Int chunkPos in targetChunks)
        {
            if (!loadedChunks.Contains(chunkPos))
            {
                if (loadedThisFrame >= maxChunksToLoadPerFrame)
                {
                    yield return null;
                    loadedThisFrame = 0;
                }

                LoadChunk(chunkPos);
                loadedThisFrame++;
            }
        }
    }

    void LoadChunk(Vector2Int chunkPos)
    {
        if (!chunks.ContainsKey(chunkPos))
        {
            chunks[chunkPos] = new ChunkData(chunkPos, chunkSize);
        }

        ChunkData chunk = chunks[chunkPos];

        if (!chunk.isGenerated && generateChunksOnDemand)
        {
            GenerateChunk(chunk);
        }

        if (!chunk.isLoaded)
        {
            ApplyChunkToTilemap(chunk);
            chunk.isLoaded = true;
        }

        loadedChunks.Add(chunkPos);
    }

    void UnloadChunk(Vector2Int chunkPos)
    {
        if (!chunks.ContainsKey(chunkPos)) return;

        ChunkData chunk = chunks[chunkPos];

        if (chunk.isLoaded)
        {
            // Save current state before unloading
            SaveChunkFromTilemap(chunk);

            // Clear tiles from tilemap
            ClearChunkFromTilemap(chunk);
            chunk.isLoaded = false;
        }

        loadedChunks.Remove(chunkPos);
    }

    void GenerateChunk(ChunkData chunk)
    {
        Vector2Int worldStart = ChunkToWorld(chunk.chunkPosition);

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector3Int worldPos = new Vector3Int(worldStart.x + x, worldStart.y + y, 0);

                // Check if there's a painted tile at this position
                if (paintedTiles.ContainsKey(worldPos))
                {
                    chunk.mainTiles[x, y] = paintedTiles[worldPos];
                }
                else
                {
                    TileBase _rockTile = rockTile.GetRandomTile();
                    TileBase _grassTile = grassTile.GetRandomTile();
                    if(Random.value < grassProbability)
                    {
                        chunk.mainTiles[x, y] = _grassTile;
                    }
                    else if(Random.value < rockProbability)
                    {
                        chunk.backgroundTiles[x, y] = _rockTile;
                    }
                    else
                    {
                        chunk.backgroundTiles[x, y] = null; // No tile
                    }
                }
            }
        }

        chunk.isGenerated = true;
    }

    void ApplyChunkToTilemap(ChunkData chunk)
    {
        Vector2Int worldStart = ChunkToWorld(chunk.chunkPosition);

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector3Int worldPos = new Vector3Int(worldStart.x + x, worldStart.y + y, 0);

                // Apply main tiles
                if (chunk.mainTiles[x, y] != null)
                {
                    mainTilemap.SetTile(worldPos, chunk.mainTiles[x, y]);

                    // Add collision if needed
                    if (ShouldHaveCollision(chunk.mainTiles[x, y]))
                    {
                        collisionTilemap.SetTile(worldPos, chunk.mainTiles[x, y]);
                    }
                }

                // Apply background tiles
                if (chunk.backgroundTiles[x, y] != null)
                {
                    backgroundTilemap.SetTile(worldPos, chunk.backgroundTiles[x, y]);
                }
            }
        }
    }

    void SaveChunkFromTilemap(ChunkData chunk)
    {
        Vector2Int worldStart = ChunkToWorld(chunk.chunkPosition);

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector3Int worldPos = new Vector3Int(worldStart.x + x, worldStart.y + y, 0);

                // Save current state
                chunk.mainTiles[x, y] = mainTilemap.GetTile(worldPos);
                chunk.backgroundTiles[x, y] = backgroundTilemap.GetTile(worldPos);
            }
        }
    }

    void ClearChunkFromTilemap(ChunkData chunk)
    {
        Vector2Int worldStart = ChunkToWorld(chunk.chunkPosition);

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector3Int worldPos = new Vector3Int(worldStart.x + x, worldStart.y + y, 0);

                // Clear all tilemaps
                mainTilemap.SetTile(worldPos, null);
                collisionTilemap.SetTile(worldPos, null);
                backgroundTilemap.SetTile(worldPos, null);
            }
        }
    }

    bool ShouldHaveCollision(TileBase tile)
    {
        if (collisionTiles == null) return false;

        foreach (TileBase collisionTile in collisionTiles)
        {
            if (tile == collisionTile)
                return true;
        }
        return false;
    }

    // Utility methods
    Vector2Int WorldToChunk(Vector3 worldPosition)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPosition.x / chunkSize),
            Mathf.FloorToInt(worldPosition.y / chunkSize)
        );
    }

    Vector2Int ChunkToWorld(Vector2Int chunkPosition)
    {
        return new Vector2Int(
            chunkPosition.x * chunkSize,
            chunkPosition.y * chunkSize
        );
    }

    // Public methods for interaction
    public bool TryInteractWithTile(Vector3 worldPosition)
    {
        Vector3Int cellPosition = mainTilemap.WorldToCell(worldPosition);
        TileBase tile = mainTilemap.GetTile(cellPosition);

        if (tile != null && IsInteractiveTile(tile))
        {
            OnTileInteraction(cellPosition, tile);
            return true;
        }

        return false;
    }

    bool IsInteractiveTile(TileBase tile)
    {
        if (interactiveTiles == null) return false;

        foreach (TileBase interactiveTile in interactiveTiles)
        {
            if (tile == interactiveTile)
                return true;
        }
        return false;
    }

    void OnTileInteraction(Vector3Int position, TileBase tile)
    {
        Debug.Log($"Interacted with tile {tile.name} at position {position}");

        // Update the chunk data when tiles are modified
        Vector2Int chunkPos = WorldToChunk(position);
        if (chunks.ContainsKey(chunkPos))
        {
            Vector2Int localPos = new Vector2Int(
                position.x - ChunkToWorld(chunkPos).x,
                position.y - ChunkToWorld(chunkPos).y
            );

            // Update chunk data if tile is removed/changed
            // chunks[chunkPos].mainTiles[localPos.x, localPos.y] = null;
        }
    }

    // Debug information
    void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Vector2Int playerChunk = WorldToChunk(player.position);

        // Draw loaded chunks
        Gizmos.color = Color.green;
        foreach (Vector2Int chunkPos in loadedChunks)
        {
            Vector2Int worldPos = ChunkToWorld(chunkPos);
            Vector3 center = new Vector3(worldPos.x + chunkSize * 0.5f, worldPos.y + chunkSize * 0.5f, 0);
            Gizmos.DrawWireCube(center, new Vector3(chunkSize, chunkSize, 0));
        }

        // Draw load radius
        Gizmos.color = Color.yellow;
        Vector2Int playerWorldChunk = ChunkToWorld(playerChunk);
        Vector3 playerChunkCenter = new Vector3(
            playerWorldChunk.x + chunkSize * 0.5f,
            playerWorldChunk.y + chunkSize * 0.5f,
            0
        );

        float radiusSize = (loadRadius * 2 + 1) * chunkSize;
        Gizmos.DrawWireCube(playerChunkCenter, new Vector3(radiusSize, radiusSize, 0));
    }

    void OnDestroy()
    {
        if (chunkUpdateCoroutine != null)
        {
            StopCoroutine(chunkUpdateCoroutine);
        }
    }

    // Public utility methods
    public int GetLoadedChunkCount()
    {
        return loadedChunks.Count;
    }

    public int GetTotalChunkCount()
    {
        return chunks.Count;
    }
}