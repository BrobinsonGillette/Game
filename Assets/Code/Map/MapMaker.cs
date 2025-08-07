using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapMaker : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 10;
    [SerializeField] private int gridHeight = 10;
    [SerializeField] private float hexSize = 1f;


    [Header("Prefabs")]
    [SerializeField] private GameObject hexTilePrefab;


    [Header("Performance")]
    [SerializeField] private Transform tilesParent;

    [Header("Generation Constraints")]
    [SerializeField] private float worldBoundSize = 52f;

    // Cache frequently used values
    private readonly float sqrt3 = Mathf.Sqrt(3f);
    private readonly float sqrt3Div3 = Mathf.Sqrt(3f) / 3f;
    private readonly float sqrt3Div2 = Mathf.Sqrt(3f) / 2f;
    private readonly float twoThirds = 2f / 3f;
    private readonly float oneThird = 1f / 3f;
    private readonly float threeHalfs = 3f / 2f;

    // Hex direction vectors for neighbor calculation
    private static readonly Vector2Int[] HexDirections = new Vector2Int[]
    {
        new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, -1),
        new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1)
    };

    // Data structures
    private Dictionary<Vector2Int, HexTile> hexTiles = new Dictionary<Vector2Int, HexTile>();
    private List<HexTile> allTiles = new List<HexTile>();
    private Vector3 gridCenter;

    // Events
    public System.Action<HexTile> OnTileCreated;
    public System.Action OnGridGenerated;

    // Properties
    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;
    public float HexSize => hexSize;
    public int TileCount => allTiles.Count;
    public Vector3 GridCenter => gridCenter;

    void Start()
    {
        GenerateGrid();
    }

    void OnValidate()
    {
        // Clamp values to reasonable ranges
        gridWidth = Mathf.Max(1, gridWidth);
        gridHeight = Mathf.Max(1, gridHeight);
        hexSize = Mathf.Max(0.1f, hexSize);
        worldBoundSize = Mathf.Max(1f, worldBoundSize);
    }

    /// <summary>
    /// Generate the hex grid based on current settings
    /// </summary>
    public void GenerateGrid()
    {
        ClearExistingGrid();

        if (hexTilePrefab == null)
        {
            Debug.LogError("MapMaker: hexTilePrefab is not assigned!");
            return;
        }

        // Setup parent transform
        if (tilesParent == null)
        {
            GameObject parentObj = new GameObject("HexTiles");
            parentObj.transform.SetParent(transform);
            tilesParent = parentObj.transform;
        }

        // Calculate grid center once
        gridCenter = CalculateGridCenter();

        // Pre-allocate collections
        int estimatedTileCount = gridWidth * gridHeight;
        hexTiles = new Dictionary<Vector2Int, HexTile>(estimatedTileCount);
        allTiles = new List<HexTile>(estimatedTileCount);

        // Generate tiles
        for (int q = 0; q < gridWidth; q++)
        {
            for (int r = 0; r < gridHeight; r++)
            {
                Vector2Int hexCoord = new Vector2Int(q, r);

                if (ShouldCreateTileAt(hexCoord))
                {
                    CreateHexTile(hexCoord);
                }
            }
        }

        OnGridGenerated?.Invoke();
        Debug.Log($"MapMaker: Generated {allTiles.Count} hex tiles");
    }

    /// <summary>
    /// Clear all existing tiles from the grid
    /// </summary>
    public void ClearExistingGrid()
    {
        // Destroy existing tiles
        foreach (var tile in allTiles)
        {
            if (tile != null && tile.gameObject != null)
            {
                if (Application.isPlaying)
                    Destroy(tile.gameObject);
                else
                    DestroyImmediate(tile.gameObject);
            }
        }

        hexTiles.Clear();
        allTiles.Clear();
    }

    /// <summary>
    /// Check if a tile should be created at the given coordinate
    /// </summary>
    private bool ShouldCreateTileAt(Vector2Int hexCoord)
    {
        Vector3 worldPos = HexToWorldPosition(hexCoord) - gridCenter;
        return Mathf.Abs(worldPos.x) <= worldBoundSize &&
               Mathf.Abs(worldPos.y) <= worldBoundSize;
    }

    /// <summary>
    /// Create a hex tile at the specified coordinate
    /// </summary>
    private void CreateHexTile(Vector2Int hexCoord)
    {
        Vector3 worldPos = HexToWorldPosition(hexCoord) - gridCenter;

        GameObject hexObj = Instantiate(hexTilePrefab, worldPos, Quaternion.identity, tilesParent);
        hexObj.name = $"HexTile_{hexCoord.x}_{hexCoord.y}";

        HexTile hexTile = hexObj.GetComponent<HexTile>();
        if (hexTile == null)
        {
            Debug.LogWarning($"MapMaker: HexTile component not found on prefab at {hexCoord}");
            hexTile = hexObj.AddComponent<HexTile>();
        }

        hexTile.Initialize(hexCoord, this);
        hexTiles[hexCoord] = hexTile;
        allTiles.Add(hexTile);

        OnTileCreated?.Invoke(hexTile);
    }

    /// <summary>
    /// Calculate the center point of the entire grid
    /// </summary>
    private Vector3 CalculateGridCenter()
    {
        Vector2Int centerCoord = new Vector2Int(gridWidth / 2, gridHeight / 2);
        return HexToWorldPosition(centerCoord);
    }

    /// <summary>
    /// Convert hex coordinates to world position
    /// </summary>
    public Vector3 HexToWorldPosition(Vector2Int hexCoord)
    {
        float x = hexSize * (sqrt3 * hexCoord.x + sqrt3Div2 * hexCoord.y);
        float y = hexSize * (threeHalfs * hexCoord.y);
        return new Vector3(x, y, 0f);
    }

    /// <summary>
    /// Convert world position to hex coordinates
    /// </summary>
    public Vector2Int WorldToHexPosition(Vector3 worldPos)
    {
        Vector3 adjustedWorldPos = worldPos + gridCenter;

        float q = (sqrt3Div3 * adjustedWorldPos.x - oneThird * adjustedWorldPos.y) / hexSize;
        float r = (twoThirds * adjustedWorldPos.y) / hexSize;

        return CubeToAxial(CubeRound(new Vector3(q, -q - r, r)));
    }

    /// <summary>
    /// Round cube coordinates to nearest integer values
    /// </summary>
    private Vector3 CubeRound(Vector3 cube)
    {
        float rx = Mathf.Round(cube.x);
        float ry = Mathf.Round(cube.y);
        float rz = Mathf.Round(cube.z);

        float x_diff = Mathf.Abs(rx - cube.x);
        float y_diff = Mathf.Abs(ry - cube.y);
        float z_diff = Mathf.Abs(rz - cube.z);

        if (x_diff > y_diff && x_diff > z_diff)
            rx = -ry - rz;
        else if (y_diff > z_diff)
            ry = -rx - rz;
        else
            rz = -rx - ry;

        return new Vector3(rx, ry, rz);
    }

    /// <summary>
    /// Convert cube coordinates to axial coordinates
    /// </summary>
    private Vector2Int CubeToAxial(Vector3 cube)
    {
        return new Vector2Int((int)cube.x, (int)cube.z);
    }

    /// <summary>
    /// Get all neighboring coordinates of a hex
    /// </summary>
    public List<Vector2Int> GetNeighbors(Vector2Int hexCoord)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>(6);

        foreach (Vector2Int dir in HexDirections)
        {
            Vector2Int neighbor = hexCoord + dir;
            if (IsValidHexCoord(neighbor))
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }


    /// <summary>
    /// Check if hex coordinate is within grid bounds
    /// </summary>
    public bool IsValidHexCoord(Vector2Int hexCoord)
    {
        return hexCoord.x >= 0 && hexCoord.x < gridWidth &&
               hexCoord.y >= 0 && hexCoord.y < gridHeight;
    }


    /// <summary>
    /// Get hex tile at specified coordinates
    /// </summary>
    public HexTile GetHexTile(Vector2Int hexCoord)
    {
        hexTiles.TryGetValue(hexCoord, out HexTile tile);
        return tile;
    }

    /// <summary>
    /// Get all tiles within a certain distance from a center coordinate
    /// </summary>
    public List<HexTile> GetTilesInRange(Vector2Int centerCoord, int range)
    {
        List<HexTile> tilesInRange = new List<HexTile>();

        for (int q = -range; q <= range; q++)
        {
            int r1 = Mathf.Max(-range, -q - range);
            int r2 = Mathf.Min(range, -q + range);

            for (int r = r1; r <= r2; r++)
            {
                Vector2Int coord = centerCoord + new Vector2Int(q, r);
                HexTile tile = GetHexTile(coord);
                if (tile != null)
                {
                    tilesInRange.Add(tile);
                }
            }
        }

        return tilesInRange;
    }

    /// <summary>
    /// Get a line of coordinates between two hex positions
    /// </summary>
    public List<Vector2Int> GetLine(Vector2Int hexA, Vector2Int hexB)
    {
        int distance = GetDistance(hexA, hexB);
        List<Vector2Int> results = new List<Vector2Int>(distance + 1);

        for (int i = 0; i <= distance; i++)
        {
            float t = distance == 0 ? 0f : (float)i / distance;
            Vector3 cubeA = AxialToCube(hexA);
            Vector3 cubeB = AxialToCube(hexB);
            Vector3 lerpedCube = Vector3.Lerp(cubeA, cubeB, t);
            results.Add(CubeToAxial(CubeRound(lerpedCube)));
        }

        return results;
    }
    /// <summary>
    /// Calculate distance between two hex coordinates
    /// </summary>
    public int GetDistance(Vector2Int hexA, Vector2Int hexB)
    {
        return (Mathf.Abs(hexA.x - hexB.x) +
                Mathf.Abs(hexA.x + hexA.y - hexB.x - hexB.y) +
                Mathf.Abs(hexA.y - hexB.y)) / 2;
    }
    /// <summary>
    /// Convert axial coordinates to cube coordinates
    /// </summary>
    private Vector3 AxialToCube(Vector2Int axial)
    {
        float x = axial.x;
        float z = axial.y;
        float y = -x - z;
        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Get the closest hex tile to a world position
    /// </summary>
    public HexTile GetClosestTile(Vector3 worldPos)
    {
        Vector2Int hexCoord = WorldToHexPosition(worldPos);
        return GetHexTile(hexCoord);
    }

    /// <summary>
    /// Get all tiles in the grid
    /// </summary>
    public List<HexTile> GetAllTiles()
    {
        return new List<HexTile>(allTiles);
    }

   
}
