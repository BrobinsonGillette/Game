using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [Header("References")]
    public ChartorMove playerCharacter;

    [Header("Starting Position")]
    public Vector2Int startingHexPosition = Vector2Int.zero;

    private MapMaker mapMaker;
    private HexTile currentPlayerTile;

    void Start()
    {
        mapMaker = MapMaker.instance;

        if (mapMaker == null)
        {
            Debug.LogError("PlayerManager: MapMaker not found!");
            return;
        }

        if (playerCharacter == null)
        {
            playerCharacter = FindObjectOfType<ChartorMove>();
        }

        if (playerCharacter == null)
        {
            Debug.LogError("PlayerManager: ChartorMove component not found!");
            return;
        }

        // Subscribe to player movement events
        playerCharacter.OnMoveStarted += OnPlayerMoveStarted;
        playerCharacter.OnMoveCompleted += OnPlayerMoveCompleted;
        playerCharacter.OnMovementModeActivated += OnMovementModeActivated;
        playerCharacter.OnMovementModeDeactivated += OnMovementModeDeactivated;

        // Wait a frame to ensure map is generated, then set initial position
        StartCoroutine(SetInitialPlayerPosition());
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (playerCharacter != null)
        {
            playerCharacter.OnMoveStarted -= OnPlayerMoveStarted;
            playerCharacter.OnMoveCompleted -= OnPlayerMoveCompleted;
            playerCharacter.OnMovementModeActivated -= OnMovementModeActivated;
            playerCharacter.OnMovementModeDeactivated -= OnMovementModeDeactivated;
        }
    }

    /// <summary>
    /// Set the initial position of the player
    /// </summary>
    private IEnumerator SetInitialPlayerPosition()
    {
        // Wait for map generation to complete
        yield return new WaitForEndOfFrame();

        // Find a valid starting position
        Vector2Int validStartPos = FindValidStartingPosition();

        // Set player position
        playerCharacter.SetHexPosition(validStartPos);

        // Update tile visual
        UpdatePlayerTileVisuals(validStartPos);

        Debug.Log($"PlayerManager: Set initial player position to {validStartPos}");
    }

    /// <summary>
    /// Find a valid starting position for the player
    /// </summary>
    private Vector2Int FindValidStartingPosition()
    {
        // First, try the specified starting position
        HexTile startTile = mapMaker.GetHexTile(startingHexPosition);
        if (startTile != null && startTile.isWalkable)
        {
            return startingHexPosition;
        }

        // If starting position is invalid, find the first walkable tile
        foreach (HexTile tile in mapMaker.GetAllTiles())
        {
            if (tile.isWalkable)
            {
                Debug.LogWarning($"PlayerManager: Starting position {startingHexPosition} invalid, using {tile.coordinates} instead");
                return tile.coordinates;
            }
        }

        // If no walkable tiles found, return origin
        Debug.LogError("PlayerManager: No walkable tiles found! Using origin.");
        return Vector2Int.zero;
    }

    /// <summary>
    /// Called when player starts moving
    /// </summary>
    private void OnPlayerMoveStarted(Vector2Int targetPosition)
    {
        Debug.Log($"PlayerManager: Player started moving to {targetPosition}");

        // Clear current tile's player presence
        if (currentPlayerTile != null)
        {
            currentPlayerTile.SetPlayerPresence(false);
        }
    }

    /// <summary>
    /// Called when player finishes moving
    /// </summary>
    private void OnPlayerMoveCompleted(Vector2Int newPosition)
    {
        Debug.Log($"PlayerManager: Player completed move to {newPosition}");
        UpdatePlayerTileVisuals(newPosition);
    }

    /// <summary>
    /// Update the visual representation of which tile has the player
    /// </summary>
    private void UpdatePlayerTileVisuals(Vector2Int playerPosition)
    {
        // Clear previous tile
        if (currentPlayerTile != null)
        {
            currentPlayerTile.SetPlayerPresence(false);
        }

        // Set new tile
        currentPlayerTile = mapMaker.GetHexTile(playerPosition);
        if (currentPlayerTile != null)
        {
            currentPlayerTile.SetPlayerPresence(true);
        }
    }

    /// <summary>
    /// Called when movement mode is activated
    /// </summary>
    private void OnMovementModeActivated()
    {
        Debug.Log("PlayerManager: Movement mode activated");
    }

    /// <summary>
    /// Called when movement mode is deactivated
    /// </summary>
    private void OnMovementModeDeactivated()
    {
        Debug.Log("PlayerManager: Movement mode deactivated");
    }

    /// <summary>
    /// Get the current tile the player is on
    /// </summary>
    public HexTile GetCurrentPlayerTile()
    {
        return currentPlayerTile;
    }

    /// <summary>
    /// Get the player's current hex position
    /// </summary>
    public Vector2Int GetPlayerHexPosition()
    {
        return playerCharacter != null ? playerCharacter.GetCurrentHexPosition() : Vector2Int.zero;
    }
}
