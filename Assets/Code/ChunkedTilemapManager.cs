using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public bool isDirty; // Track if chunk needs saving

    public ChunkData(Vector2Int position, int chunkSize)
    {
        chunkPosition = position;
        mainTiles = new TileBase[chunkSize, chunkSize];
        backgroundTiles = new TileBase[chunkSize, chunkSize];
        isLoaded = false;
        isGenerated = false;
        isDirty = false;
    }

    public void MarkDirty() => isDirty = true;
    public void MarkClean() => isDirty = false;
}

[System.Serializable]
public class WeightedTile
{
    public TileBase tile;
    [Range(0f, 100)]
    public int weight = 1;
}

[System.Serializable]
public class TileSet
{
    public WeightedTile[] tiles;
    private float totalWeight = -1f;

    private float GetTotalWeight()
    {
        if (totalWeight < 0)
        {
            totalWeight = 0f;
            foreach (var weightedTile in tiles)
            {
                if (weightedTile?.tile != null)
                {
                    totalWeight += weightedTile.weight;
                }
            }
        }
        return totalWeight;
    }

    public TileBase GetRandomTile()
    {
        if (tiles == null || tiles.Length == 0) return null;

        float total = GetTotalWeight();
        if (total <= 0) return null;

        // FIXED: Generate random value between 0 and total weight (not 1 to total)
        float randomValue = Random.Range(0f, total);
        float currentWeight = 0f;

        // Iterate through tiles and find which one the random value falls into
        foreach (var weightedTile in tiles)
        {
            if (weightedTile?.tile == null) continue;

            currentWeight += weightedTile.weight;
            if (randomValue <= currentWeight)
            {
                return weightedTile.tile;
            }
        }

        // Fallback (shouldn't normally reach here)
        return tiles.LastOrDefault(t => t?.tile != null)?.tile;
    }

  
  

}



[System.Serializable]
public class NoiseSettings
{
    [Range(0.001f, 0.1f)]
    public float scale = 0.05f;
    [Range(1, 8)]
    public int octaves = 3;
    [Range(0f, 1f)]
    public float persistence = 0.5f;
    [Range(1f, 4f)]
    public float lacunarity = 2f;
    public Vector2 offset = Vector2.zero;
}

public class ChunkedTilemapManager : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private Transform player;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap mainTilemap;
    [SerializeField] private Tilemap collisionTilemap;
    [SerializeField] private Tilemap backgroundTilemap;

    [Header("Chunk Settings")]
    [SerializeField] private int chunkSize = 16;
    [SerializeField] private int loadRadius = 2;
    [SerializeField] private float updateInterval = 0.5f;

    [Header("Tile Generation")]
    [SerializeField] private TileSet rockTiles;
    [SerializeField] private TileSet grassTiles;
    [SerializeField] private TileSet decorationTiles;
    [SerializeField] private TileBase[] collisionTiles;
    [SerializeField] private TileBase[] interactiveTiles;

    [Header("Generation Settings")]
    [SerializeField] private NoiseSettings terrainNoise;
    [SerializeField] private NoiseSettings decorationNoise;
    [Range(0f, 1f)]
    [SerializeField] private float grassThreshold = 0.4f;
    [Range(0f, 1f)]
    [SerializeField] private float rockThreshold = 0.6f;
    [Range(0f, 1f)]
    [SerializeField] private float decorationThreshold = 0.8f;
    [SerializeField] private bool generateChunksOnDemand = true;
    [SerializeField] private int worldSeed = 12345;

    [Header("Performance Settings")]
    [SerializeField] private int maxChunksToLoadPerFrame = 1;
    [SerializeField] private int maxChunksToUnloadPerFrame = 2;
    [SerializeField] private int maxTilesPerFrame = 256;
    [SerializeField] private bool usePooling = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private bool showChunkBounds = true;

    // Core data structures
    private readonly Dictionary<Vector2Int, ChunkData> chunks = new Dictionary<Vector2Int, ChunkData>();
    private readonly HashSet<Vector2Int> loadedChunks = new HashSet<Vector2Int>();
    private readonly Dictionary<Vector3Int, TileBase> paintedTiles = new Dictionary<Vector3Int, TileBase>();

    // Performance tracking
    private Vector2Int lastPlayerChunk = Vector2Int.zero;
    private Coroutine chunkUpdateCoroutine;
    private readonly Queue<Vector2Int> chunkLoadQueue = new Queue<Vector2Int>();
    private readonly Queue<Vector2Int> chunkUnloadQueue = new Queue<Vector2Int>();

    // Object pooling for better performance
    private readonly Queue<TileBase[,]> tileArrayPool = new Queue<TileBase[,]>();

    // Events for extensibility
    public System.Action<Vector2Int> OnChunkLoaded;
    public System.Action<Vector2Int> OnChunkUnloaded;
    public System.Action<Vector3Int, TileBase> OnTileInteracted;

    #region Unity Lifecycle

    private void Start()
    {
        InitializeManager();
    }

    private void OnDestroy()
    {
        CleanupManager();
    }

    private void OnDrawGizmosSelected()
    {
        if (!showChunkBounds || player == null) return;
        DrawChunkGizmos();
    }

    #endregion

    #region Initialization

    private void InitializeManager()
    {
        // Set random seed for consistent generation
        Random.InitState(worldSeed);

        // Find player if not assigned
        if (player == null)
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
                player = playerGO.transform;
            else
            {
                Debug.LogError("Player not found! Please assign player transform or add 'Player' tag.");
                return;
            }
        }

        // Validate tilemaps
        if (!ValidateTilemaps()) return;

        // Save existing tiles
        SaveExistingTiles();

        // Initialize object pools
        if (usePooling)
            InitializePools();

        // Start chunk management
        lastPlayerChunk = WorldToChunk(player.position);
        chunkUpdateCoroutine = StartCoroutine(ChunkUpdateLoop());

        // Initial chunk load
        UpdatePlayerChunk();

        Debug.Log($"ChunkedTilemapManager initialized with seed: {worldSeed}");
    }

    private bool ValidateTilemaps()
    {
        if (mainTilemap == null)
        {
            Debug.LogError("Main tilemap is not assigned!");
            return false;
        }

        if (backgroundTilemap == null)
            Debug.LogWarning("Background tilemap is not assigned.");

        if (collisionTilemap == null)
            Debug.LogWarning("Collision tilemap is not assigned.");

        return true;
    }

    private void InitializePools()
    {
        // Pre-populate tile array pool
        for (int i = 0; i < 10; i++)
        {
            tileArrayPool.Enqueue(new TileBase[chunkSize, chunkSize]);
        }
    }

    private void CleanupManager()
    {
        if (chunkUpdateCoroutine != null)
        {
            StopCoroutine(chunkUpdateCoroutine);
            chunkUpdateCoroutine = null;
        }

        // Save all dirty chunks before cleanup
        SaveAllDirtyChunks();
    }

    #endregion

    #region Tile Management

    private void SaveExistingTiles()
    {
        if (mainTilemap == null) return;

        var bounds = mainTilemap.cellBounds;
        int savedCount = 0;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                var position = new Vector3Int(x, y, 0);
                var tile = mainTilemap.GetTile(position);
                if (tile != null)
                {
                    paintedTiles[position] = tile;
                    savedCount++;
                }
            }
        }

        if (showDebugInfo)
            Debug.Log($"Saved {savedCount} existing painted tiles");
    }

    #endregion

    #region Chunk Management

    private IEnumerator ChunkUpdateLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);

            if (player == null) continue;

            var currentPlayerChunk = WorldToChunk(player.position);
            if (currentPlayerChunk != lastPlayerChunk)
            {
                UpdatePlayerChunk();
                lastPlayerChunk = currentPlayerChunk;
            }

            // Process chunk queues
            yield return ProcessChunkQueues();
        }
    }

    private void UpdatePlayerChunk()
    {
        if (player == null) return;

        var playerChunk = WorldToChunk(player.position);
        var targetChunks = GetChunksInRadius(playerChunk, loadRadius);

        // Queue chunks for unloading
        foreach (var loadedChunk in loadedChunks.ToList())
        {
            if (!targetChunks.Contains(loadedChunk))
            {
                chunkUnloadQueue.Enqueue(loadedChunk);
            }
        }

        // Queue chunks for loading
        foreach (var targetChunk in targetChunks)
        {
            if (!loadedChunks.Contains(targetChunk) && !chunkLoadQueue.Contains(targetChunk))
            {
                chunkLoadQueue.Enqueue(targetChunk);
            }
        }
    }

    private HashSet<Vector2Int> GetChunksInRadius(Vector2Int center, int radius)
    {
        var chunks = new HashSet<Vector2Int>();
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                chunks.Add(center + new Vector2Int(x, y));
            }
        }
        return chunks;
    }

    private IEnumerator ProcessChunkQueues()
    {
        int processedThisFrame = 0;

        // Process unload queue
        while (chunkUnloadQueue.Count > 0 && processedThisFrame < maxChunksToUnloadPerFrame)
        {
            var chunkPos = chunkUnloadQueue.Dequeue();
            UnloadChunk(chunkPos);
            processedThisFrame++;
        }

        if (processedThisFrame >= maxChunksToUnloadPerFrame)
        {
            yield return null;
            processedThisFrame = 0;
        }

        // Process load queue
        while (chunkLoadQueue.Count > 0 && processedThisFrame < maxChunksToLoadPerFrame)
        {
            var chunkPos = chunkLoadQueue.Dequeue();
            yield return LoadChunkCoroutine(chunkPos);
            processedThisFrame++;
        }
    }

    private IEnumerator LoadChunkCoroutine(Vector2Int chunkPos)
    {
        if (!chunks.ContainsKey(chunkPos))
        {
            chunks[chunkPos] = new ChunkData(chunkPos, chunkSize);
        }

        var chunk = chunks[chunkPos];

        if (!chunk.isGenerated && generateChunksOnDemand)
        {
            yield return GenerateChunkCoroutine(chunk);
        }

        if (!chunk.isLoaded)
        {
            yield return ApplyChunkToTilemapCoroutine(chunk);
            chunk.isLoaded = true;
        }

        loadedChunks.Add(chunkPos);
        OnChunkLoaded?.Invoke(chunkPos);
    }

    private void UnloadChunk(Vector2Int chunkPos)
    {
        if (!chunks.ContainsKey(chunkPos)) return;

        var chunk = chunks[chunkPos];

        if (chunk.isLoaded)
        {
            SaveChunkFromTilemap(chunk);
            ClearChunkFromTilemap(chunk);
            chunk.isLoaded = false;
        }

        loadedChunks.Remove(chunkPos);
        OnChunkUnloaded?.Invoke(chunkPos);
    }

    #endregion

    #region Chunk Generation

    private IEnumerator GenerateChunkCoroutine(ChunkData chunk)
    {
        var worldStart = ChunkToWorld(chunk.chunkPosition);
        int tilesProcessed = 0;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                var worldPos = new Vector3Int(worldStart.x + x, worldStart.y + y, 0);

                // Check for painted tiles first
                if (paintedTiles.ContainsKey(worldPos))
                {
                    chunk.mainTiles[x, y] = paintedTiles[worldPos];
                }
                else
                {
                    GenerateTileAtPosition(chunk, x, y, worldPos);
                }

                tilesProcessed++;
                if (tilesProcessed >= maxTilesPerFrame)
                {
                    yield return null;
                    tilesProcessed = 0;
                }
            }
        }

        chunk.isGenerated = true;
    }


    private void GenerateTileAtPosition(ChunkData chunk, int localX, int localY, Vector3Int worldPos)
    {
        float terrainValue = GenerateNoise(worldPos.x, worldPos.y, terrainNoise);
        float decorationValue = GenerateNoise(worldPos.x, worldPos.y, decorationNoise);

        // Create weighted selection based on noise value
        TileBase selectedTile = null;

        // Adjust these weights to control spawn rates
        float grassWeight = Mathf.Lerp(80f, 20f, terrainValue); // More grass at low noise
        float rockWeight = Mathf.Lerp(5f, 20f, terrainValue);   // More rocks at high noise
        float emptyWeight = Mathf.Lerp(15f, 20f, terrainValue); // Some empty space

        float totalWeight = grassWeight + rockWeight + emptyWeight;
        float randomValue = Random.Range(0f, totalWeight);

        if (randomValue < grassWeight)
        {
            selectedTile = grassTiles?.GetRandomTile();
        }
        else if (randomValue < grassWeight + rockWeight)
        {
            selectedTile = rockTiles?.GetRandomTile();
        }
        // else empty

        chunk.mainTiles[localX, localY] = selectedTile;

        // Decorations
        if (decorationValue > decorationThreshold && selectedTile != null)
        {
            var decoration = decorationTiles?.GetRandomTile();
            if (decoration != null)
            {
                chunk.mainTiles[localX, localY] = decoration;
            }
        }
    }
    

    private float GenerateNoise(float x, float y, NoiseSettings settings)
    {
        float value = 0f;
        float amplitude = 1f;
        float frequency = settings.scale;

        for (int i = 0; i < settings.octaves; i++)
        {
            float sampleX = (x + settings.offset.x) * frequency;
            float sampleY = (y + settings.offset.y) * frequency;

            value += Mathf.PerlinNoise(sampleX, sampleY) * amplitude;

            amplitude *= settings.persistence;
            frequency *= settings.lacunarity;
        }

        return Mathf.Clamp01(value);
    }

    #endregion

    #region Tilemap Operations

    private IEnumerator ApplyChunkToTilemapCoroutine(ChunkData chunk)
    {
        var worldStart = ChunkToWorld(chunk.chunkPosition);
        int tilesProcessed = 0;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                var worldPos = new Vector3Int(worldStart.x + x, worldStart.y + y, 0);

                // Apply main tiles
                if (chunk.mainTiles[x, y] != null)
                {
                    mainTilemap.SetTile(worldPos, chunk.mainTiles[x, y]);

                    // Add collision if needed
                    if (collisionTilemap != null && ShouldHaveCollision(chunk.mainTiles[x, y]))
                    {
                        collisionTilemap.SetTile(worldPos, chunk.mainTiles[x, y]);
                    }
                }

                // Apply background tiles
                if (backgroundTilemap != null && chunk.backgroundTiles[x, y] != null)
                {
                    backgroundTilemap.SetTile(worldPos, chunk.backgroundTiles[x, y]);
                }

                tilesProcessed++;
                if (tilesProcessed >= maxTilesPerFrame)
                {
                    yield return null;
                    tilesProcessed = 0;
                }
            }
        }
    }

    private void SaveChunkFromTilemap(ChunkData chunk)
    {
        if (!chunk.isDirty) return;

        var worldStart = ChunkToWorld(chunk.chunkPosition);

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                var worldPos = new Vector3Int(worldStart.x + x, worldStart.y + y, 0);
                chunk.mainTiles[x, y] = mainTilemap.GetTile(worldPos);

                if (backgroundTilemap != null)
                    chunk.backgroundTiles[x, y] = backgroundTilemap.GetTile(worldPos);
            }
        }

        chunk.MarkClean();
    }

    private void ClearChunkFromTilemap(ChunkData chunk)
    {
        var worldStart = ChunkToWorld(chunk.chunkPosition);

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                var worldPos = new Vector3Int(worldStart.x + x, worldStart.y + y, 0);

                mainTilemap.SetTile(worldPos, null);
                collisionTilemap?.SetTile(worldPos, null);
                backgroundTilemap?.SetTile(worldPos, null);
            }
        }
    }

    private bool ShouldHaveCollision(TileBase tile)
    {
        if (collisionTiles == null || tile == null) return false;
        return System.Array.IndexOf(collisionTiles, tile) >= 0;
    }

    #endregion

    #region Public API

    public bool TryInteractWithTile(Vector3 worldPosition)
    {
        var cellPosition = mainTilemap.WorldToCell(worldPosition);
        var tile = mainTilemap.GetTile(cellPosition);

        if (tile != null && IsInteractiveTile(tile))
        {
            OnTileInteraction(cellPosition, tile);
            return true;
        }

        return false;
    }

    public void SetTileAt(Vector3Int position, TileBase tile, bool isMainTile = true)
    {
    
        if (backgroundTilemap != null)
        {
            backgroundTilemap.SetTile(position, tile);
        }
        else if (isMainTile)
        {
            mainTilemap.SetTile(position, tile);
            paintedTiles[position] = tile;
        }

        // Mark chunk as dirty
        var chunkPos = WorldToChunk(position);
        if (chunks.ContainsKey(chunkPos))
        {
            chunks[chunkPos].MarkDirty();
        }
    }

    public TileBase GetTileAt(Vector3Int position, bool isMainTile = true)
    {
        return isMainTile ? mainTilemap.GetTile(position) : backgroundTilemap?.GetTile(position);
    }

    public void SaveAllDirtyChunks()
    {
        foreach (var chunk in chunks.Values.Where(c => c.isDirty))
        {
            SaveChunkFromTilemap(chunk);
        }
    }

    public void RegenerateChunk(Vector2Int chunkPosition)
    {
        if (chunks.ContainsKey(chunkPosition))
        {
            var chunk = chunks[chunkPosition];
            chunk.isGenerated = false;
            chunk.MarkDirty();

            if (chunk.isLoaded)
            {
                StartCoroutine(RegenerateLoadedChunk(chunk));
            }
        }
    }

    private IEnumerator RegenerateLoadedChunk(ChunkData chunk)
    {
        ClearChunkFromTilemap(chunk);
        yield return GenerateChunkCoroutine(chunk);
        yield return ApplyChunkToTilemapCoroutine(chunk);
    }

    #endregion

    #region Utility Methods

    private bool IsInteractiveTile(TileBase tile)
    {
        if (interactiveTiles == null || tile == null) return false;
        return System.Array.IndexOf(interactiveTiles, tile) >= 0;
    }

    private void OnTileInteraction(Vector3Int position, TileBase tile)
    {
        OnTileInteracted?.Invoke(position, tile);

        if (showDebugInfo)
            Debug.Log($"Interacted with tile {tile.name} at position {position}");

        // Mark chunk as dirty when tiles are modified
        var chunkPos = WorldToChunk(position);
        if (chunks.ContainsKey(chunkPos))
        {
            chunks[chunkPos].MarkDirty();
        }
    }

    private Vector2Int WorldToChunk(Vector3 worldPosition)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPosition.x / chunkSize),
            Mathf.FloorToInt(worldPosition.y / chunkSize)
        );
    }

    private Vector2Int ChunkToWorld(Vector2Int chunkPosition)
    {
        return chunkPosition * chunkSize;
    }

    #endregion

    #region Debug and Gizmos

    private void DrawChunkGizmos()
    {
        var playerChunk = WorldToChunk(player.position);

        // Draw loaded chunks
        Gizmos.color = Color.green;
        foreach (var chunkPos in loadedChunks)
        {
            DrawChunkGizmo(chunkPos);
        }

        // Draw chunks in queue
        Gizmos.color = Color.yellow;
        foreach (var chunkPos in chunkLoadQueue)
        {
            DrawChunkGizmo(chunkPos);
        }

        // Draw load radius
        Gizmos.color = Color.cyan;
        var playerWorldChunk = ChunkToWorld(playerChunk);
        var center = new Vector3(
            playerWorldChunk.x + chunkSize * 0.5f,
            playerWorldChunk.y + chunkSize * 0.5f,
            0
        );

        var radiusSize = (loadRadius * 2 + 1) * chunkSize;
        Gizmos.DrawWireCube(center, new Vector3(radiusSize, radiusSize, 0));
    }

    private void DrawChunkGizmo(Vector2Int chunkPos)
    {
        var worldPos = ChunkToWorld(chunkPos);
        var center = new Vector3(worldPos.x + chunkSize * 0.5f, worldPos.y + chunkSize * 0.5f, 0);
        Gizmos.DrawWireCube(center, new Vector3(chunkSize, chunkSize, 0));
    }

    #endregion

    #region Public Properties

    public int LoadedChunkCount => loadedChunks.Count;
    public int TotalChunkCount => chunks.Count;
    public int ChunkLoadQueueCount => chunkLoadQueue.Count;
    public int ChunkUnloadQueueCount => chunkUnloadQueue.Count;
    public Vector2Int CurrentPlayerChunk => WorldToChunk(player.position);

    #endregion
}