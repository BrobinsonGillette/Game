using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapMaker : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float hexSize = 1f;

    [Header("Prefabs")]
    public GameObject hexTilePrefab;

    [Header("Visual Settings")]
    public bool showCoordinates = true;
    public bool showGridLines = true;

    private Dictionary<Vector2Int, HexTile> hexTiles = new Dictionary<Vector2Int, HexTile>();
    private float hexWidth;
    private float hexHeight;

    // Hexagon math constants for pointed-top orientation
    private readonly float sqrt3 = Mathf.Sqrt(3f);

    void Start()
    {
        CalculateHexDimensions();
        GenerateGrid();
    }

    void CalculateHexDimensions()
    {
        // For pointed-top hexagons
        hexWidth = sqrt3 * hexSize;
        hexHeight = 2f * hexSize;
    }

    void GenerateGrid()
    {
        // Calculate offset to center the grid
        Vector3 gridCenter = CalculateGridCenter();

        for (int q = 0; q < gridWidth; q++)
        {
            for (int r = 0; r < gridHeight; r++)
            {
                Vector2Int hexCoord = new Vector2Int(q, r);
                Vector3 worldPos = HexToWorldPosition(hexCoord) - gridCenter;

                GameObject hexObj = Instantiate(hexTilePrefab, worldPos, Quaternion.identity, transform);
                HexTile hexTile = hexObj.GetComponent<HexTile>();

                if (hexTile != null)
                {
                    hexTile.Initialize(hexCoord, this);
                }

                hexTiles[hexCoord] = hexTile;
            }
        }
    }

    Vector3 CalculateGridCenter()
    {
        // Calculate the center point of the entire grid
        Vector2Int centerCoord = new Vector2Int(gridWidth / 2, gridHeight / 2);
        return HexToWorldPosition(centerCoord);
    }

    public Vector3 HexToWorldPosition(Vector2Int hexCoord)
    {
        float x = hexSize * (sqrt3 * hexCoord.x + sqrt3 / 2f * hexCoord.y);
        float y = hexSize * (3f / 2f * hexCoord.y);

        return new Vector3(x, y, 0f);
    }

    public Vector2Int WorldToHexPosition(Vector3 worldPos)
    {
        // Add the grid center offset back when converting from world to hex
        Vector3 gridCenter = CalculateGridCenter();
        Vector3 adjustedWorldPos = worldPos + gridCenter;

        float q = (sqrt3 / 3f * adjustedWorldPos.x - 1f / 3f * adjustedWorldPos.y) / hexSize;
        float r = (2f / 3f * adjustedWorldPos.y) / hexSize;

        return CubeToAxial(CubeRound(new Vector3(q, -q - r, r)));
    }

    Vector3 CubeRound(Vector3 cube)
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

    Vector2Int CubeToAxial(Vector3 cube)
    {
        return new Vector2Int((int)cube.x, (int)cube.z);
    }

    public List<Vector2Int> GetNeighbors(Vector2Int hexCoord)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, -1),
            new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1)
        };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int neighbor = hexCoord + dir;
            if (IsValidHexCoord(neighbor))
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }

    public bool IsValidHexCoord(Vector2Int hexCoord)
    {
        return hexCoord.x >= 0 && hexCoord.x < gridWidth &&
               hexCoord.y >= 0 && hexCoord.y < gridHeight;
    }

    public HexTile GetHexTile(Vector2Int hexCoord)
    {
        hexTiles.TryGetValue(hexCoord, out HexTile tile);
        return tile;
    }

    public int GetDistance(Vector2Int hexA, Vector2Int hexB)
    {
        return (Mathf.Abs(hexA.x - hexB.x) +
                Mathf.Abs(hexA.x + hexA.y - hexB.x - hexB.y) +
                Mathf.Abs(hexA.y - hexB.y)) / 2;
    }

    void OnDrawGizmos()
    {
        if (!showGridLines) return;

        CalculateHexDimensions();
        Vector3 gridCenter = CalculateGridCenter();

        Gizmos.color = Color.white;
        for (int q = 0; q < gridWidth; q++)
        {
            for (int r = 0; r < gridHeight; r++)
            {
                Vector3 center = HexToWorldPosition(new Vector2Int(q, r)) - gridCenter;
                DrawHexagonGizmo(center);
            }
        }
    }

    void DrawHexagonGizmo(Vector3 center)
    {
        Vector3[] corners = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            float angle_deg = 60 * i - 30; // -30 for pointed-top
            float angle_rad = Mathf.PI / 180 * angle_deg;
            corners[i] = center + new Vector3(
                hexSize * Mathf.Cos(angle_rad),
                hexSize * Mathf.Sin(angle_rad),
                0
            );
        }

        for (int i = 0; i < 6; i++)
        {
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 6]);
        }
    }
}
