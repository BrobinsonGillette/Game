using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PresetType
{
    None,
    Dungeon,
    Town,
    Village,
    Castle,
    Tower,
    Cave,
    Forest,
    Lake,
    Custom
}

[System.Serializable]
public class PresetTile
{
    public Vector2Int position; // Relative position from center
    public TerrainTileData terrainData;
    [Tooltip("Priority when multiple tiles want the same position (higher = more important)")]
    public int placementPriority = 0;
}

[CreateAssetMenu(fileName = "New Terrain Preset", menuName = "Terrain/Terrain Preset")]
public class TerrainPreset : ScriptableObject
{
    [Header("Preset Info")]
    public string presetName;
    public PresetType presetType;


    [Header("Spawn Settings")]
    [Tooltip("Can this preset spawn randomly during generation?")]
    public bool canSpawnNaturally = false;

    [Range(0f, 1f)]
    [Tooltip("Chance to spawn naturally when conditions are met")]
    public float naturalSpawnChance = 0.01f;

    [Tooltip("Minimum distance between instances of this preset")]
    public float minDistanceBetweenSpawns = 50f;

    [Header("Spawn Conditions")]
    [Range(0f, 1f)]
    public float minHeightToSpawn = 0.3f;
    [Range(0f, 1f)]
    public float maxHeightToSpawn = 0.7f;

    [Range(-1f, 1f)]
    public float preferredTemperature = 0f;
    [Range(0f, 1f)]
    public float temperatureTolerance = 1f;

    [Range(-1f, 1f)]
    public float preferredHumidity = 0f;
    [Range(0f, 1f)]
    public float humidityTolerance = 1f;

    [Header("Structure Layout")]
    [Tooltip("The tiles that make up this preset. Position (0,0) is the center/spawn point")]
    public PresetTile[] presetTiles;




    [Header("Interaction Settings")]
    [Tooltip("Can players manually spawn this preset by interacting with tiles?")]
    public bool canSpawnByInteraction = true;

    [Tooltip("Specific terrain types that can spawn this preset when interacted with")]
    public TerrainTileData[] triggerTerrains;

    /// <summary>
    /// Check if this preset can spawn at the given conditions
    /// </summary>
    public bool CanSpawnAt(float noiseValue, float temperature, float humidity)
    {
        // Check height range
        if (noiseValue < minHeightToSpawn || noiseValue > maxHeightToSpawn)
            return false;

        // Check temperature tolerance
        if (Mathf.Abs(temperature - preferredTemperature) > temperatureTolerance)
            return false;

        // Check humidity tolerance  
        if (Mathf.Abs(humidity - preferredHumidity) > humidityTolerance)
            return false;

        return true;
    }

    /// <summary>
    /// Get the size bounds of this preset
    /// </summary>
    public BoundsInt GetBounds()
    {
        if (presetTiles == null || presetTiles.Length == 0)
            return new BoundsInt(0, 0, 0, 1, 1, 1);

        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (var tile in presetTiles)
        {
            minX = Mathf.Min(minX, tile.position.x);
            maxX = Mathf.Max(maxX, tile.position.x);
            minY = Mathf.Min(minY, tile.position.y);
            maxY = Mathf.Max(maxY, tile.position.y);
        }

        return new BoundsInt(minX, minY, 0, maxX - minX + 1, maxY - minY + 1, 1);
    }

    /// <summary>
    /// Get all world positions this preset would occupy when spawned at centerPosition
    /// </summary>
    public List<Vector3Int> GetWorldPositions(Vector3Int centerPosition)
    {
        List<Vector3Int> positions = new List<Vector3Int>();

        foreach (var tile in presetTiles)
        {
            Vector3Int worldPos = centerPosition + new Vector3Int(tile.position.x, tile.position.y, 0);
            positions.Add(worldPos);
        }

        return positions;
    }

    /// <summary>
    /// Check if the trigger terrain can spawn this preset
    /// </summary>
    public bool CanBeTriggeredBy(TerrainTileData terrainData)
    {
        if (!canSpawnByInteraction) return false;
        if (triggerTerrains == null || triggerTerrains.Length == 0) return true;

        foreach (var trigger in triggerTerrains)
        {
            if (trigger == terrainData) return true;
        }

        return false;
    }
}
