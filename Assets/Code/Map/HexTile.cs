using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HexTile : MonoBehaviour
{
    [Header("Tile Properties")]
    public Vector2Int coordinates;
    public bool isWalkable = true;
    public int movementCost = 1;

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    public Color selectedColor = Color.green;
    public Color blockedColor = Color.black;
    public Color playerPositionColor = Color.blue;
    public Color movementTargetColor = Color.cyan;

    private MapMaker gridManager;
    public bool isSelected = false;
    private bool isHovered = false;
    private bool hasPlayer = false;
    private bool isMovementTarget = false;

    // Reference to the player
   public ChartorMove playerCharacter;

    public void Initialize(Vector2Int coords, MapMaker manager)
    {
        coordinates = coords;
        gridManager = manager;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        UpdateVisual();
    }

    void UpdateVisual()
    {
        if (spriteRenderer == null) return;

        Color targetColor = normalColor;

        if (!isWalkable)
            targetColor = blockedColor;
        else if (hasPlayer)
            targetColor = playerPositionColor;
        else if (isMovementTarget)
            targetColor = movementTargetColor;
        else if (isSelected)
            targetColor = selectedColor;
        else if (isHovered)
            targetColor = hoverColor;

        spriteRenderer.color = targetColor;
    }

    public void MousedOver()
    {
        if (!isWalkable) return;

        isHovered = true;
        UpdateVisual();
    }

    public void MouseExit()
    {
        isHovered = false;
        UpdateVisual();
    }

    public void Interact()
    {
        if (!isWalkable) return;

        // If the player is on this tile, activate movement mode
        if (hasPlayer && playerCharacter != null)
        {
            ActivateCharacterMovement();
        }
        // If this tile is a movement target, try to move the player here
        else if (isMovementTarget && playerCharacter != null)
        {
            TryMovePlayerHere();
        }
        else
        {
            // Regular tile selection for other tiles
            ToggleSelection();
        }
    }

    /// <summary>
    /// Activate the character's movement mode when clicking on their current tile
    /// </summary>
    private void ActivateCharacterMovement()
    {
        if (playerCharacter == null)
        {
            return;
        }

        if (playerCharacter.IsMoving())
        {
            return;
        }

        // Activate movement mode
        playerCharacter.ActivateMovementMode();
    }

    /// <summary>
    /// Try to move the player to this tile (only works if this is a movement target)
    /// </summary>
    private void TryMovePlayerHere()
    {
        if (playerCharacter == null || !isMovementTarget)
        {
            return;
        }

        // Attempt to move the player here
        playerCharacter.TryMoveToTile(coordinates);
    }

    /// <summary>
    /// Set this tile as a movement target
    /// </summary>
    public void SetAsMovementTarget(bool isTarget)
    {
        isMovementTarget = isTarget;
        UpdateVisual();
    }

    public void ToggleSelection()
    {
        isSelected = !isSelected;
        UpdateVisual();

        if (isSelected)
        {
            // Show neighbors or perform other selection logic
            var neighbors = gridManager.GetNeighbors(coordinates);
        }
    }

    public void DeSelect()
    {
        isSelected = false;
        UpdateVisual();
    }

    /// <summary>
    /// Set whether the player is currently on this tile
    /// </summary>
    public void SetPlayerPresence(bool hasPlayerOnTile)
    {
        hasPlayer = hasPlayerOnTile;
        UpdateVisual();
    }

    /// <summary>
    /// Check if the player is currently on this tile
    /// </summary>
    public bool HasPlayer()
    {
        return hasPlayer;
    }
}
