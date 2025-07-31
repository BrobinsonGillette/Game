using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum TerrainType
{
    none,
    Background,
    Collision,
    Interactable,
    Other
}

[CreateAssetMenu(fileName = "New Terrain Tile", menuName = "Terrain/Terrain Tile Data")]
public class TerrainTileData : ScriptableObject
{
    [Header("Tile Properties")]
    public TileBase tile;
    public bool hasCollision;
    public bool canInteract;
    public TerrainType type;

    [Header("Height Range")]
    [Range(0f, 1f)]
    public float minHeight = 0f; // 0-1 range for height threshold
    [Range(0f, 1f)]
    public float maxHeight = 1f; // 0-1 range for height threshold

    [Header("Spawn Settings")]
    [Range(0f, 1f)]
    [Tooltip("How often this terrain type spawns relative to others in its height range (0 = never, 1 = always when in range)")]
    public float spawnFrequency = 1f;

    [Range(0f, 1f)]
    [Tooltip("Additional randomness factor for spawn chance")]
    public float randomSpawnChance = 1f;


    [Header("Additional Properties")]
    public AudioClip interactionSound;


    /// <summary>
    /// Calculate spawn probability based on noise value, biome conditions, and spawn settings
    /// </summary>
    public float CalculateSpawnProbability(float noiseValue, float temperature = 0f, float humidity = 0f, float randomValue = 0f)
    {
        // Check if within height range
        if (noiseValue < minHeight || noiseValue > maxHeight)
            return 0f;

        float probability = spawnFrequency;

       

        // Apply random spawn chance
        if (randomSpawnChance < 1f)
        {
            probability *= (randomValue <= randomSpawnChance) ? 1f : 0f;
        }

        return Mathf.Clamp01(probability);
    }

    /// <summary>
    /// Check if this terrain type should spawn at given conditions
    /// </summary>
    public bool ShouldSpawn(float noiseValue, float temperature = 0f, float humidity = 0f, float randomValue = 0f)
    {
        return CalculateSpawnProbability(noiseValue, temperature, humidity, randomValue) > 0f;
    }
}
