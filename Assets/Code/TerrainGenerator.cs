using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;




public class TerrainGenerator : MonoBehaviour
{
    [Header("World Settings")]
    public Transform player;
    public int chunkSize = 16;
    public int renderDistance = 3; // chunks around player

    [Header("Noise Settings")]
    public float noiseScale = 0.1f;
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public int seed = 12345;

    [Header("Biome Settings")]
    public bool useBiomes = false;
    public float biomeScale = 0.05f; // Larger scale for biome variation
    public float temperatureOffset = 0f;
    public float humidityOffset = 100f; // Different offset for humidity

    [Header("Terrain Types")]
    public TerrainTileData[] terrainTypes;
    public TerrainTileData fallbackTerrain; // Default terrain if no match found

    [Header("Tilemaps")]
    public Tilemap terrainTilemap;
    public Tilemap collisionTilemap;

    [Header("Interaction")]
    public bool enableInteraction = true;
    public LayerMask interactionLayer = 1;

    [Header("Spawn Frequency")]
    [Tooltip("Global multiplier for all spawn frequencies")]
    [Range(0.1f, 2f)]
    public float globalSpawnMultiplier = 1f;

    [Tooltip("Enable debugging spawn probabilities")]
    public bool debugSpawnProbabilities = false;

    // Private variables
    private Dictionary<Vector2Int, TerrainChunk> loadedChunks = new Dictionary<Vector2Int, TerrainChunk>();
    private Vector2Int lastPlayerChunk;
    private System.Random rng;

    // Chunk data structure
    private class TerrainChunk
    {
        public Vector2Int chunkCoord;
        public TileBase[,] tiles;
        public bool[,] collisions;
        public bool[,] interactions;
        public TerrainTileData[,] terrainData; // Store terrain data for each tile
        public bool isLoaded;

        public TerrainChunk(Vector2Int coord, int size)
        {
            chunkCoord = coord;
            tiles = new TileBase[size, size];
            collisions = new bool[size, size];
            interactions = new bool[size, size];
            terrainData = new TerrainTileData[size, size];
            isLoaded = false;
        }
    }

    void Start()
    {
        InitializeGenerator();
        GenerateInitialTerrain();
    }

    void Update()
    {
        UpdateTerrain();

        if (enableInteraction && Input.GetMouseButtonDown(0))
        {
            HandleInteraction();
        }
    }

    void InitializeGenerator()
    {
        rng = new System.Random(seed);

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        // Ensure collision tilemap has collider
        if (collisionTilemap != null)
        {
            TilemapCollider2D collider = collisionTilemap.GetComponent<TilemapCollider2D>();
            if (collider == null)
                collisionTilemap.gameObject.AddComponent<TilemapCollider2D>();
        }

        lastPlayerChunk = GetChunkCoordinate(player.position);

        // Validate terrain types
        ValidateTerrainTypes();
    }

    void ValidateTerrainTypes()
    {
        if (terrainTypes == null || terrainTypes.Length == 0)
        {
            Debug.LogWarning("No terrain types assigned! Please assign terrain ScriptableObjects in the inspector.");
            return;
        }

        // Sort terrain types by minHeight for better organization
        System.Array.Sort(terrainTypes, (a, b) => a.minHeight.CompareTo(b.minHeight));

        // Check for gaps in height coverage
        for (int i = 0; i < terrainTypes.Length - 1; i++)
        {
            if (terrainTypes[i].maxHeight < terrainTypes[i + 1].minHeight)
            {
                Debug.LogWarning($"Height gap detected between {terrainTypes[i].name} (max: {terrainTypes[i].maxHeight}) and {terrainTypes[i + 1].name} (min: {terrainTypes[i + 1].minHeight})");
            }
        }
    }

    void GenerateInitialTerrain()
    {
        Vector2Int playerChunk = GetChunkCoordinate(player.position);

        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int y = -renderDistance; y <= renderDistance; y++)
            {
                Vector2Int chunkCoord = playerChunk + new Vector2Int(x, y);
                LoadChunk(chunkCoord);
            }
        }
    }

    void UpdateTerrain()
    {
        if (player == null) return;

        Vector2Int currentPlayerChunk = GetChunkCoordinate(player.position);

        if (currentPlayerChunk != lastPlayerChunk)
        {
            // Load new chunks
            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                for (int y = -renderDistance; y <= renderDistance; y++)
                {
                    Vector2Int chunkCoord = currentPlayerChunk + new Vector2Int(x, y);
                    if (!loadedChunks.ContainsKey(chunkCoord))
                    {
                        LoadChunk(chunkCoord);
                    }
                }
            }

            // Unload distant chunks
            List<Vector2Int> chunksToUnload = new List<Vector2Int>();
            foreach (var kvp in loadedChunks)
            {
                Vector2Int chunkCoord = kvp.Key;
                float distance = Vector2Int.Distance(chunkCoord, currentPlayerChunk);
                if (distance > renderDistance + 1)
                {
                    chunksToUnload.Add(chunkCoord);
                }
            }

            foreach (Vector2Int chunkCoord in chunksToUnload)
            {
                UnloadChunk(chunkCoord);
            }

            lastPlayerChunk = currentPlayerChunk;
        }
    }

    Vector2Int GetChunkCoordinate(Vector3 worldPosition)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPosition.x / chunkSize),
            Mathf.FloorToInt(worldPosition.y / chunkSize)
        );
    }

    void LoadChunk(Vector2Int chunkCoord)
    {
        if (loadedChunks.ContainsKey(chunkCoord)) return;

        TerrainChunk chunk = new TerrainChunk(chunkCoord, chunkSize);
        GenerateChunk(chunk);
        RenderChunk(chunk);

        loadedChunks[chunkCoord] = chunk;
    }

    void UnloadChunk(Vector2Int chunkCoord)
    {
        if (!loadedChunks.ContainsKey(chunkCoord)) return;

        // Clear tiles from tilemap
        BoundsInt bounds = new BoundsInt(
            chunkCoord.x * chunkSize,
            chunkCoord.y * chunkSize,
            0,
            chunkSize,
            chunkSize,
            1
        );

        terrainTilemap.SetTilesBlock(bounds, new TileBase[chunkSize * chunkSize]);
        if (collisionTilemap != null)
            collisionTilemap.SetTilesBlock(bounds, new TileBase[chunkSize * chunkSize]);

        loadedChunks.Remove(chunkCoord);
    }

    void GenerateChunk(TerrainChunk chunk)
    {
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector2Int worldPos = new Vector2Int(
                    chunk.chunkCoord.x * chunkSize + x,
                    chunk.chunkCoord.y * chunkSize + y
                );

                float noiseValue = GenerateNoiseValue(worldPos.x, worldPos.y);
                float temperature = 0f;
                float humidity = 0f;

                if (useBiomes)
                {
                    temperature = GenerateBiomeValue(worldPos.x, worldPos.y, temperatureOffset);
                    humidity = GenerateBiomeValue(worldPos.x, worldPos.y, humidityOffset);
                }

                TerrainTileData selectedTerrain = SelectTerrainType(noiseValue, temperature, humidity, worldPos);

                if (selectedTerrain != null)
                {
                    chunk.tiles[x, y] = selectedTerrain.tile;
                    chunk.collisions[x, y] = selectedTerrain.hasCollision;
                    chunk.interactions[x, y] = selectedTerrain.canInteract;
                    chunk.terrainData[x, y] = selectedTerrain;
                }
                else if (fallbackTerrain != null)
                {
                    chunk.tiles[x, y] = fallbackTerrain.tile;
                    chunk.collisions[x, y] = fallbackTerrain.hasCollision;
                    chunk.interactions[x, y] = fallbackTerrain.canInteract;
                    chunk.terrainData[x, y] = fallbackTerrain;
                }
            }
        }

        chunk.isLoaded = true;
    }

    float GenerateNoiseValue(int x, int y)
    {
        float amplitude = 1f;
        float frequency = noiseScale;
        float noiseValue = 0f;
        float maxValue = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float sampleX = (x + seed) * frequency;
            float sampleY = (y + seed) * frequency;

            float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
            noiseValue += perlinValue * amplitude;

            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return noiseValue / maxValue;
    }

    float GenerateBiomeValue(int x, int y, float offset)
    {
        float sampleX = (x + seed + offset) * biomeScale;
        float sampleY = (y + seed + offset) * biomeScale;
        return (Mathf.PerlinNoise(sampleX, sampleY) - 0.5f) * 2f; // -1 to 1 range
    }

    TerrainTileData SelectTerrainType(float noiseValue, float temperature, float humidity, Vector2Int worldPos)
    {
        if (terrainTypes == null || terrainTypes.Length == 0)
            return fallbackTerrain;

        // Create a seeded random for this position to ensure consistency
        System.Random positionRng = new System.Random(seed + worldPos.x * 73856093 + worldPos.y * 19349663);
        float randomValue = (float)positionRng.NextDouble();

        // Find all terrain types that could spawn at this location
        List<(TerrainTileData terrain, float probability)> candidates = new List<(TerrainTileData, float)>();

        foreach (var terrainType in terrainTypes)
        {
            if (terrainType == null) continue;

            float probability = terrainType.CalculateSpawnProbability(noiseValue, temperature, humidity, randomValue) * globalSpawnMultiplier;

            if (probability > 0f)
            {
                candidates.Add((terrainType, probability));
            }
        }

        if (candidates.Count == 0)
            return fallbackTerrain;

        // If only one candidate, return it
        if (candidates.Count == 1)
            return candidates[0].terrain;

        // Weight-based selection for multiple candidates
        float totalWeight = candidates.Sum(c => c.probability);
        float randomSelection = (float)positionRng.NextDouble() * totalWeight;
        float currentWeight = 0f;

        foreach (var candidate in candidates)
        {
            currentWeight += candidate.probability;
            if (randomSelection <= currentWeight)
            {
                if (debugSpawnProbabilities)
                {
                    Debug.Log($"Selected {candidate.terrain.name} at {worldPos} with probability {candidate.probability:F3} (noise: {noiseValue:F3}, temp: {temperature:F3}, humidity: {humidity:F3})");
                }
                return candidate.terrain;
            }
        }

        // Fallback to highest probability candidate
        return candidates.OrderByDescending(c => c.probability).First().terrain;
    }

    void RenderChunk(TerrainChunk chunk)
    {
        Vector3Int basePosition = new Vector3Int(
            chunk.chunkCoord.x * chunkSize,
            chunk.chunkCoord.y * chunkSize,
            0
        );

        // Render terrain tiles
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector3Int tilePosition = basePosition + new Vector3Int(x, y, 0);

                if (chunk.tiles[x, y] != null)
                {
                    terrainTilemap.SetTile(tilePosition, chunk.tiles[x, y]);

                    // Set collision tile if needed
                    if (collisionTilemap != null && chunk.collisions[x, y])
                    {
                        collisionTilemap.SetTile(tilePosition, chunk.tiles[x, y]);
                    }
                }
            }
        }
    }

    void HandleInteraction()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int tilePosition = terrainTilemap.WorldToCell(mouseWorldPos);

        // Check if tile can be interacted with
        if (CanInteractWithTile(tilePosition))
        {
            OnTileInteraction(tilePosition);
        }
    }

    bool CanInteractWithTile(Vector3Int tilePosition)
    {
        Vector2Int chunkCoord = GetChunkCoordinate(tilePosition);

        if (!loadedChunks.ContainsKey(chunkCoord)) return false;

        TerrainChunk chunk = loadedChunks[chunkCoord];

        int localX = tilePosition.x - (chunkCoord.x * chunkSize);
        int localY = tilePosition.y - (chunkCoord.y * chunkSize);

        if (localX < 0 || localX >= chunkSize || localY < 0 || localY >= chunkSize)
            return false;

        return chunk.interactions[localX, localY];
    }

    // Get terrain data at a specific position
    public TerrainTileData GetTerrainData(Vector3Int tilePosition)
    {
        Vector2Int chunkCoord = GetChunkCoordinate(tilePosition);

        if (!loadedChunks.ContainsKey(chunkCoord)) return null;

        TerrainChunk chunk = loadedChunks[chunkCoord];

        int localX = tilePosition.x - (chunkCoord.x * chunkSize);
        int localY = tilePosition.y - (chunkCoord.y * chunkSize);

        if (localX < 0 || localX >= chunkSize || localY < 0 || localY >= chunkSize)
            return null;

        return chunk.terrainData[localX, localY];
    }

    // Virtual method for tile interaction - override this in derived classes
    protected virtual void OnTileInteraction(Vector3Int tilePosition)
    {
        TerrainTileData terrainData = GetTerrainData(tilePosition);

        if (terrainData != null)
        {
            Debug.Log($"Interacted with {terrainData.name} at {tilePosition}");

            // Play interaction sound if available
            if (terrainData.interactionSound != null)
            {
                AudioSource.PlayClipAtPoint(terrainData.interactionSound, Camera.main.ScreenToWorldPoint(Input.mousePosition));
            }
        }

        // Example: Remove tile on interaction
        // terrainTilemap.SetTile(tilePosition, null);
        // if (collisionTilemap != null)
        //     collisionTilemap.SetTile(tilePosition, null);
    }

    // Public methods for external interaction
    public void SetTile(Vector3Int position, TileBase tile, bool hasCollision = false)
    {
        terrainTilemap.SetTile(position, tile);

        if (collisionTilemap != null)
        {
            if (hasCollision)
                collisionTilemap.SetTile(position, tile);
            else
                collisionTilemap.SetTile(position, null);
        }
    }

    public TileBase GetTile(Vector3Int position)
    {
        return terrainTilemap.GetTile(position);
    }

    public bool HasCollision(Vector3Int position)
    {
        return collisionTilemap != null && collisionTilemap.GetTile(position) != null;
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Vector2Int playerChunk = GetChunkCoordinate(player.position);

        // Draw loaded chunks
        Gizmos.color = Color.green;
        foreach (var kvp in loadedChunks)
        {
            Vector2Int chunkCoord = kvp.Key;
            Vector3 chunkCenter = new Vector3(
                (chunkCoord.x + 0.5f) * chunkSize,
                (chunkCoord.y + 0.5f) * chunkSize,
                0
            );
            Gizmos.DrawWireCube(chunkCenter, Vector3.one * chunkSize);
        }

        // Draw render distance
        Gizmos.color = Color.yellow;
        Vector3 playerChunkCenter = new Vector3(
            (playerChunk.x + 0.5f) * chunkSize,
            (playerChunk.y + 0.5f) * chunkSize,
            0
        );
        float renderSize = (renderDistance * 2 + 1) * chunkSize;
        Gizmos.DrawWireCube(playerChunkCenter, Vector3.one * renderSize);
    }
}

// Example extension for custom interactions
public class InteractableTerrainGenerator : TerrainGenerator
{
    [Header("Custom Interaction")]
    public TerrainTileData replacementTerrain;

    protected override void OnTileInteraction(Vector3Int tilePosition)
    {
        base.OnTileInteraction(tilePosition);

        TerrainTileData currentTerrainData = GetTerrainData(tilePosition);

        if (currentTerrainData != null)
        {
            // Replace with replacement terrain
            if (replacementTerrain != null)
            {
                SetTile(tilePosition, replacementTerrain.tile, replacementTerrain.hasCollision);
            }
            else
            {
                // Remove tile
                SetTile(tilePosition, null, false);
            }
        }
    }
}