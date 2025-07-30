using System.Collections.Generic;
using UnityEngine.Tilemaps;
using UnityEngine;
using System.Collections;

[System.Serializable]
public class TileData
{
    [Header("Ground Tiles (No Collision)")]
    public TileBase grassTile;

    [Header("Collision Tiles")]
    public TileBase stoneTile;
    public TileBase waterTile;
    public TileBase wallTile;
    public TileBase treeTile;

    [Header("Entrance Tiles (No Collision)")]
    public TileBase townEntranceTile;
    public TileBase dungeonEntranceTile;
}

[System.Serializable]
public class PremadeTilemap
{
    public string locationName;
    public GameObject tilemapPrefab;
    public Vector3 spawnPosition;
}

public class DynamicTilemapSystem : MonoBehaviour
{
    [Header("Tilemap Settings")]
    public Tilemap tilemap;
    public TilemapCollider2D tilemapCollider;
    public TileData tileData;
    public int chunkSize = 16;
    public int renderDistance = 3; // chunks around player

    [Header("World Generation")]
    public float noiseScale = 0.1f;
    public int worldSeed = 12345;

    [Header("Premade Locations")]
    public List<PremadeTilemap> premadeLocations = new List<PremadeTilemap>();

    [Header("Player Reference")]
    public Transform player;

    private Dictionary<Vector2Int, bool> loadedChunks = new Dictionary<Vector2Int, bool>();
    private Dictionary<Vector2Int, LocationEntrance> locationEntrances = new Dictionary<Vector2Int, LocationEntrance>();
    private Vector2Int lastPlayerChunk;

    // Tiles that should have collision
    private HashSet<TileBase> collisionTiles;

    private void Start()
    {
        if (player == null)
            player = GameObject.Find("Player").transform;

        // Setup tilemap collider if not assigned
        if (tilemapCollider == null)
            tilemapCollider = tilemap.GetComponent<TilemapCollider2D>();

        // Add TilemapCollider2D if it doesn't exist
        if (tilemapCollider == null)
            tilemapCollider = tilemap.gameObject.AddComponent<TilemapCollider2D>();

        // Configure collider settings
        tilemapCollider.usedByComposite = false; // Set to true if using CompositeCollider2D

        // Initialize collision tiles set
        InitializeCollisionTiles();

        Random.InitState(worldSeed);
        GenerateInitialWorld();
    }

    private void InitializeCollisionTiles()
    {
        collisionTiles = new HashSet<TileBase>
        {
            tileData.stoneTile,
            tileData.waterTile,
            tileData.wallTile,
            tileData.treeTile
        };
    }

    private void Update()
    {
        Vector2Int currentPlayerChunk = GetChunkPosition(player.position);

        if (currentPlayerChunk != lastPlayerChunk)
        {
            UpdateWorldAroundPlayer(currentPlayerChunk);
            lastPlayerChunk = currentPlayerChunk;
        }

        CheckForLocationEntry();
    }

    private Vector2Int GetChunkPosition(Vector3 worldPosition)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPosition.x / chunkSize),
            Mathf.FloorToInt(worldPosition.y / chunkSize)
        );
    }

    private void GenerateInitialWorld()
    {
        Vector2Int playerChunk = GetChunkPosition(player.position);
        UpdateWorldAroundPlayer(playerChunk);
        lastPlayerChunk = playerChunk;
    }

    private void UpdateWorldAroundPlayer(Vector2Int playerChunk)
    {
        // Generate chunks around player
        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int y = -renderDistance; y <= renderDistance; y++)
            {
                Vector2Int chunkPos = playerChunk + new Vector2Int(x, y);

                if (!loadedChunks.ContainsKey(chunkPos))
                {
                    GenerateChunk(chunkPos);
                    loadedChunks[chunkPos] = true;
                }
            }
        }

        // Refresh collider after unloading chunks
        if (tilemapCollider != null)
        {
            StartCoroutine(RefreshColliderNextFrame());
        }

        // Unload distant chunks (optional optimization)
        List<Vector2Int> chunksToUnload = new List<Vector2Int>();
        foreach (var chunk in loadedChunks.Keys)
        {
            float distance = Vector2Int.Distance(chunk, playerChunk);
            if (distance > renderDistance + 1)
            {
                chunksToUnload.Add(chunk);
            }
        }

        foreach (var chunk in chunksToUnload)
        {
            UnloadChunk(chunk);
            loadedChunks.Remove(chunk);
        }
    }

    private IEnumerator RefreshColliderNextFrame()
    {
        yield return null; // Wait one frame
        if (tilemapCollider != null)
        {
            tilemapCollider.ProcessTilemapChanges();
        }
    }

    private void GenerateChunk(Vector2Int chunkPos)
    {
        Vector3Int startPos = new Vector3Int(chunkPos.x * chunkSize, chunkPos.y * chunkSize, 0);

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector3Int tilePos = startPos + new Vector3Int(x, y, 0);
                TileBase tileToPlace = GetTileForPosition(tilePos);

                tilemap.SetTile(tilePos, tileToPlace);

                // Update collider for this chunk after placing all tiles
                if (x == chunkSize - 1 && y == chunkSize - 1)
                {
                    StartCoroutine(RefreshColliderNextFrame());
                }

                // Check if we should place a location entrance
                if (ShouldPlaceLocationEntrance(tilePos))
                {
                    PlaceLocationEntrance(tilePos);
                }
            }
        }
    }

    private TileBase GetTileForPosition(Vector3Int position)
    {
        float noiseValue = Mathf.PerlinNoise(position.x * noiseScale, position.y * noiseScale);
        float treeNoise = Mathf.PerlinNoise(position.x * noiseScale * 2 + 500, position.y * noiseScale * 2 + 500);

        if (noiseValue < 0.2f)
            return tileData.waterTile; // Water blocks movement
        else if (noiseValue < 0.3f)
            return tileData.stoneTile; // Stone blocks movement
        else if (noiseValue > 0.8f && treeNoise > 0.7f)
            return tileData.treeTile; // Trees block movement
        else if (noiseValue > 0.9f)
            return tileData.wallTile; // Walls block movement
        else
            return tileData.grassTile; // Grass allows movement
    }

    private bool ShouldPlaceLocationEntrance(Vector3Int position)
    {
        // Use a different noise function for location placement
        float locationNoise = Mathf.PerlinNoise(position.x * 0.05f + 1000, position.y * 0.05f + 1000);

        // Only place locations on grass tiles (non-collision tiles) and with low probability
        TileBase currentTile = GetTileForPosition(position);
        return currentTile == tileData.grassTile && locationNoise > 0.95f;
    }

    private bool HasCollision(TileBase tile)
    {
        return collisionTiles != null && collisionTiles.Contains(tile);
    }

    private void PlaceLocationEntrance(Vector3Int position)
    {
        // Randomly choose between town and dungeon
        bool isTown = Random.value > 0.5f;
        TileBase entranceTile = isTown ? tileData.townEntranceTile : tileData.dungeonEntranceTile;

        tilemap.SetTile(position, entranceTile);

        // Store entrance data
        Vector2Int pos2D = new Vector2Int(position.x, position.y);
        locationEntrances[pos2D] = new LocationEntrance
        {
            position = position,
            locationType = isTown ? LocationType.Town : LocationType.Dungeon,
            locationName = isTown ? "Town_" + pos2D : "Dungeon_" + pos2D
        };
    }

    private void UnloadChunk(Vector2Int chunkPos)
    {
        Vector3Int startPos = new Vector3Int(chunkPos.x * chunkSize, chunkPos.y * chunkSize, 0);

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector3Int tilePos = startPos + new Vector3Int(x, y, 0);
                tilemap.SetTile(tilePos, null);

                // Remove location entrance data
                Vector2Int pos2D = new Vector2Int(tilePos.x, tilePos.y);
                if (locationEntrances.ContainsKey(pos2D))
                {
                    locationEntrances.Remove(pos2D);
                }
            }
        }
    }

    private void CheckForLocationEntry()
    {
        Vector3Int playerTilePos = tilemap.WorldToCell(player.position);
        Vector2Int playerPos2D = new Vector2Int(playerTilePos.x, playerTilePos.y);

        if (locationEntrances.ContainsKey(playerPos2D) && Input.GetKeyDown(KeyCode.E))
        {
            LocationEntrance entrance = locationEntrances[playerPos2D];
            EnterLocation(entrance);
        }
    }

    private void EnterLocation(LocationEntrance entrance)
    {
        // Find matching premade tilemap
        PremadeTilemap premadeLocation = null;

        foreach (var location in premadeLocations)
        {
            if (location.locationName.Contains(entrance.locationType.ToString()))
            {
                premadeLocation = location;
                break;
            }
        }

        if (premadeLocation != null)
        {
            StartCoroutine(TransitionToLocation(premadeLocation));
        }
        else
        {
            Debug.Log($"Entering {entrance.locationName} - No premade tilemap found!");
        }
    }

    private IEnumerator TransitionToLocation(PremadeTilemap location)
    {
        // Fade out or show loading screen here
        yield return new WaitForSeconds(0.5f);

        // Instantiate the premade tilemap
        GameObject locationInstance = Instantiate(location.tilemapPrefab);

        // Move player to spawn position
        player.position = location.spawnPosition;

        // Optionally disable the main world tilemap system temporarily
        gameObject.SetActive(false);

        // You can add a LocationManager component to handle returning to the main world
        LocationManager locationManager = locationInstance.GetComponent<LocationManager>();
        if (locationManager != null)
        {
            locationManager.Initialize(this, transform.position);
        }
    }

    public void ReturnToMainWorld(Vector3 returnPosition)
    {
        player.position = returnPosition;
        gameObject.SetActive(true);

        // Regenerate world around new position
        Vector2Int playerChunk = GetChunkPosition(player.position);
        UpdateWorldAroundPlayer(playerChunk);
    }
}



