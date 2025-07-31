using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PresetSpawner : MonoBehaviour
{
    [Header("Preset Settings")]
    [Tooltip("All available presets that can be spawned")]
    public TerrainPreset[] availablePresets;

    [Header("Spawning Controls")]
    [Tooltip("Enable natural spawning of presets during terrain generation")]
    public bool enableNaturalSpawning = true;

    [Tooltip("Enable manual spawning through tile interaction")]
    public bool enableInteractionSpawning = true;

    [Range(1, 10)]
    [Tooltip("Maximum number of presets to attempt spawning per chunk")]
    public int maxPresetsPerChunk = 2;
    public bool showPreview = true;


    // Private variables
    private TerrainGenerator terrainGenerator;
    private Dictionary<Vector2Int, List<Vector3Int>> spawnedPresetPositions = new Dictionary<Vector2Int, List<Vector3Int>>();
    private TerrainPreset selectedPreset;
    private bool isInPresetMode = false;
    private GameObject previewObject;

    void Start()
    {
        terrainGenerator = GetComponent<TerrainGenerator>();
        if (terrainGenerator == null)
        {
            Debug.LogError("PresetSpawner requires TerrainGenerator component!");
            enabled = false;
            return;
        }

        ValidatePresets();
    }

    void Update()
    {
        if (enableInteractionSpawning && isInPresetMode)
        {
            HandlePresetPlacement();
        }

        // Toggle preset mode with key press (example: P key)
        if (Input.GetKeyDown(KeyCode.P))
        {
            TogglePresetMode();
        }

        // Cancel preset mode with escape
        if (Input.GetKeyDown(KeyCode.Escape) && isInPresetMode)
        {
            ExitPresetMode();
        }
    }

    void ValidatePresets()
    {
        if (availablePresets == null || availablePresets.Length == 0)
        {
            Debug.LogWarning("No presets assigned to PresetSpawner!");
            return;
        }

        foreach (var preset in availablePresets)
        {
            if (preset == null) continue;

            if (preset.presetTiles == null || preset.presetTiles.Length == 0)
            {
                Debug.LogWarning($"Preset '{preset.presetName}' has no tiles defined!");
            }
        }
    }

    /// <summary>
    /// Try to spawn presets naturally in a chunk during generation
    /// </summary>
    public void TrySpawnPresetsInChunk(Vector2Int chunkCoord, float[,] noiseValues, float[,] temperatureValues, float[,] humidityValues)
    {
        if (!enableNaturalSpawning || availablePresets == null) return;

        List<Vector3Int> chunkSpawnedPositions = new List<Vector3Int>();
        int presetsSpawned = 0;

        var naturalPresets = availablePresets.Where(p => p != null && p.canSpawnNaturally).ToArray();
        if (naturalPresets.Length == 0) return;

        // Try to spawn presets at random positions in the chunk
        for (int attempts = 0; attempts < maxPresetsPerChunk * 3 && presetsSpawned < maxPresetsPerChunk; attempts++)
        {
            // Random position within chunk
            int localX = Random.Range(0, terrainGenerator.chunkSize);
            int localY = Random.Range(0, terrainGenerator.chunkSize);

            Vector3Int worldPos = new Vector3Int(
                chunkCoord.x * terrainGenerator.chunkSize + localX,
                chunkCoord.y * terrainGenerator.chunkSize + localY,
                0
            );

            float noiseValue = noiseValues[localX, localY];
            float temperature = temperatureValues[localX, localY];
            float humidity = humidityValues[localX, localY];

            // Find suitable presets for this location
            var suitablePresets = naturalPresets.Where(p => p.CanSpawnAt(noiseValue, temperature, humidity)).ToArray();
            if (suitablePresets.Length == 0) continue;

            // Select random preset from suitable ones
            TerrainPreset selectedPreset = suitablePresets[Random.Range(0, suitablePresets.Length)];

            // Check spawn chance
            if (Random.value > selectedPreset.naturalSpawnChance) continue;

            // Check distance from other presets
            if (!CheckMinimumDistance(worldPos, selectedPreset.minDistanceBetweenSpawns, chunkSpawnedPositions))
                continue;

            // Try to spawn the preset
            if (TrySpawnPreset(selectedPreset, worldPos))
            {
                chunkSpawnedPositions.AddRange(selectedPreset.GetWorldPositions(worldPos));
                presetsSpawned++;
            }
        }

        if (chunkSpawnedPositions.Count > 0)
        {
            spawnedPresetPositions[chunkCoord] = chunkSpawnedPositions;
        }
    }

    /// <summary>
    /// Handle manual preset placement through interaction
    /// </summary>
    public bool TrySpawnPresetByInteraction(Vector3Int tilePosition)
    {
        if (!enableInteractionSpawning) return false;

        TerrainTileData terrainData = terrainGenerator.GetTerrainData(tilePosition);
        if (terrainData == null) return false;

        // Find presets that can be triggered by this terrain type
        var compatiblePresets = availablePresets?.Where(p => p != null && p.CanBeTriggeredBy(terrainData)).ToArray();

        if (compatiblePresets == null || compatiblePresets.Length == 0) return false;

        // If in preset mode and have selected preset
        if (isInPresetMode && selectedPreset != null)
        {
            return TrySpawnPreset(selectedPreset, tilePosition);
        }

        // If only one compatible preset, spawn it directly
        if (compatiblePresets.Length == 1)
        {
            return TrySpawnPreset(compatiblePresets[0], tilePosition);
        }

        // Multiple presets available - show selection UI or pick randomly
        ShowPresetSelection(compatiblePresets, tilePosition);
        return true;
    }

    /// <summary>
    /// Try to spawn a specific preset at a position
    /// </summary>
    public bool TrySpawnPreset(TerrainPreset preset, Vector3Int centerPosition)
    {
        if (preset == null || preset.presetTiles == null) return false;

        // Check if we can place all tiles
        List<Vector3Int> positionsToPlace = new List<Vector3Int>();
        List<PresetTile> tilesToPlace = new List<PresetTile>();

        foreach (var presetTile in preset.presetTiles)
        {
            Vector3Int worldPos = centerPosition + new Vector3Int(presetTile.position.x, presetTile.position.y, 0);

        
            TileBase existingTile = terrainGenerator.GetTile(worldPos);
            if (existingTile != null && !CanReplaceTile(worldPos))
            {
                // Can't place here, try to find alternative or fail
                continue;
            }
            

            positionsToPlace.Add(worldPos);
            tilesToPlace.Add(presetTile);
        }

        if (positionsToPlace.Count == 0) return false;

        // Sort by priority (higher priority first)
        var sortedTiles = tilesToPlace.Zip(positionsToPlace, (tile, pos) => new { tile, pos })
                                    .OrderByDescending(x => x.tile.placementPriority)
                                    .ToList();

        // Place all tiles
        foreach (var item in sortedTiles)
        {
            if (item.tile.terrainData != null)
            {
                terrainGenerator.SetTile(item.pos, item.tile.terrainData.tile, item.tile.terrainData.hasCollision);
            }
        }

        // Play effects
        PlaySpawnEffects(preset, centerPosition);

        Debug.Log($"Spawned preset '{preset.presetName}' at {centerPosition}");
        return true;
    }

    void HandlePresetPlacement()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int tilePosition = terrainGenerator.terrainTilemap.WorldToCell(mouseWorldPos);

        // Update preview
        if (showPreview && selectedPreset != null)
        {
            UpdatePreview(tilePosition);
        }

        // Place preset on click
        if (Input.GetMouseButtonDown(0) && selectedPreset != null)
        {
            if (TrySpawnPreset(selectedPreset, tilePosition))
            {
                ExitPresetMode();
            }
        }
    }

    void UpdatePreview(Vector3Int centerPosition)
    {
        // Clear existing preview
        ClearPreview();

        if (selectedPreset == null || selectedPreset.presetTiles == null) return;

        // Create preview object
        previewObject = new GameObject("PresetPreview");

        foreach (var presetTile in selectedPreset.presetTiles)
        {
            if (presetTile.terrainData == null) continue;

            Vector3Int worldPos = centerPosition + new Vector3Int(presetTile.position.x, presetTile.position.y, 0);
            Vector3 worldPosFloat = terrainGenerator.terrainTilemap.CellToWorld(worldPos);

            // Create preview sprite
            GameObject previewTile = new GameObject($"Preview_{presetTile.position}");
            previewTile.transform.SetParent(previewObject.transform);
            previewTile.transform.position = worldPosFloat;

            SpriteRenderer sr = previewTile.AddComponent<SpriteRenderer>();
            if (presetTile.terrainData.tile is Tile tile && tile.sprite != null)
            {
                sr.sprite = tile.sprite;
                sr.color = new Color(1f, 1f, 1f, 0.5f); // Semi-transparent
            }
        }
    }

    void ClearPreview()
    {
        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
        }
    }

    void PlaySpawnEffects(TerrainPreset preset, Vector3Int position)
    {
        Vector3 worldPos = terrainGenerator.terrainTilemap.CellToWorld(position);
    }

    bool CheckMinimumDistance(Vector3Int position, float minDistance, List<Vector3Int> existingPositions)
    {
        foreach (var existingPos in existingPositions)
        {
            if (Vector3Int.Distance(position, existingPos) < minDistance)
                return false;
        }
        return true;
    }

    bool CanReplaceTile(Vector3Int position)
    {
        // Add logic to determine if a tile can be replaced
        // For example, don't replace important structures, player buildings, etc.
        return true;
    }

    void ShowPresetSelection(TerrainPreset[] presets, Vector3Int position)
    {
        // Implement UI for preset selection
        // For now, just select the first one
        if (presets.Length > 0)
        {
            TrySpawnPreset(presets[0], position);
        }
    }

    public void TogglePresetMode()
    {
        isInPresetMode = !isInPresetMode;

        if (isInPresetMode)
        {
            EnterPresetMode();
        }
        else
        {
            ExitPresetMode();
        }
    }

    void EnterPresetMode()
    {
        isInPresetMode = true;

        // Select first available preset by default
        if (availablePresets != null && availablePresets.Length > 0)
        {
            selectedPreset = availablePresets[0];
        }

        Debug.Log("Entered preset mode. Click to place presets, ESC to exit.");
    }

    void ExitPresetMode()
    {
        isInPresetMode = false;
        selectedPreset = null;

        ClearPreview();


        Debug.Log("Exited preset mode.");
    }

    public void SelectPreset(TerrainPreset preset)
    {
        selectedPreset = preset;
        Debug.Log($"Selected preset: {preset.presetName}");
    }

    public void SelectPreset(int index)
    {
        if (availablePresets != null && index >= 0 && index < availablePresets.Length)
        {
            SelectPreset(availablePresets[index]);
        }
    }

    // Public methods for external access
    public TerrainPreset[] GetAvailablePresets() => availablePresets;
    public TerrainPreset GetSelectedPreset() => selectedPreset;
    public bool IsInPresetMode() => isInPresetMode;
}