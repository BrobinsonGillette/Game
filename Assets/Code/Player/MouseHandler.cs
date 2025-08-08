using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MouseHandler : MonoBehaviour
{
    public static MouseHandler instance;
    private MapMaker mapMaker;
    private HexTile currentHoveredTile = null;
    private HexTile ClickedTile = null;
    private Char SelectedPlayer;

    // Movement visualization
    private List<HexTile> currentMovementRange = new List<HexTile>();
    private List<HexTile> currentPath = new List<HexTile>();
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
        void Start()
    {
        mapMaker = MapMaker.instance;
        if (mapMaker == null)
        {
            Debug.Log("MapMaker not found! HexMouseHandler needs MapMaker to work.");
        }
    }

    void Update()
    {
        HandleMouseInput();
    }

    void HandleMouseInput()
    {
        if (mapMaker == null || CamMagger.instance == null) return;

        // Get world mouse position from CamMagger
        Vector3 worldMousePos = CamMagger.instance.WorldMousePosition;

        // Convert world position to hex coordinates
        Vector2Int hexCoord = mapMaker.WorldToHexPosition(worldMousePos);

        // Get the hex tile at those coordinates
        HexTile hoveredTile = mapMaker.GetHexTile(hexCoord);

        // Handle hover logic
        HandleHover(hoveredTile);

        // Handle click interactions
        if (Input.GetMouseButtonDown(0)) // Left mouse button
        {
            HandleClick(hoveredTile);
        }

        // Right click to cancel selection
        if (Input.GetMouseButtonDown(1))
        {
            CancelSelection();
        }
    }

    void HandleHover(HexTile newHoveredTile)
    {
        // If we're hovering over a different tile
        if (newHoveredTile != currentHoveredTile)
        {
            // Call MouseExit on the previously hovered tile
            if (currentHoveredTile != null)
            {
                currentHoveredTile.MouseExit();
            }

            // Update current hovered tile
            currentHoveredTile = newHoveredTile;

            // Call MousedOver on the new tile
            if (currentHoveredTile != null)
            {
                currentHoveredTile.MousedOver();

                // If we have a selected player, show path preview
                if (SelectedPlayer != null && currentHoveredTile != null)
                {
                    UpdatePathPreview(currentHoveredTile);
                }
            }
        }
    }

    void HandleClick(HexTile clickedTile)
    {
        if (clickedTile == null) return;

        // If clicking on a tile with a player
        if (clickedTile.hasPlayer)
        {
            // If we already have a different selected player, deselect them first
            if (SelectedPlayer != null && SelectedPlayer != clickedTile.CurrentPlayer)
            {
                CancelSelection();
            }

            // Select the clicked player
            HandleClickOnChar(clickedTile);
        }
        // If we have a selected player and clicking on an empty tile
        else if (SelectedPlayer != null)
        {
            // Try to move the selected player
            if (IsInMovementRange(clickedTile))
            {
                bool moved = SelectedPlayer.MovePlayerToTile(clickedTile);
                if (moved)
                {
                    // Update movement range display after move
                    UpdateMovementRangeDisplay();

                    // If no moves left, deselect
                    if (!SelectedPlayer.CanMove())
                    {
                        CancelSelection();
                    }
                }
            }
            else
            {
                Debug.Log("Target tile is out of movement range!");
                // Optional: Play error sound or show feedback
            }
        }
        // Clicking on an empty tile with no player selected
        else
        {
            if (ClickedTile != null)
            {
                ClickedTile.DeSelect();
            }

            ClickedTile = clickedTile;
            ClickedTile.Interact();
        }
    }

    void HandleClickOnChar(HexTile clickedTile)
    {
        if (clickedTile == null) return;

        Char clickedChar = clickedTile.CurrentPlayer;
        if (clickedChar == null) return;

        // If clicking the same character, toggle selection
        if (SelectedPlayer == clickedChar)
        {
            CancelSelection();
            return;
        }

        // Select the new character
        SelectedPlayer = clickedChar;

        if (ClickedTile != null && ClickedTile != clickedTile)
        {
            ClickedTile.DeSelect();
        }

        ClickedTile = clickedTile;
        ClickedTile.Interact();

        // Show movement range
        ShowMovementRange();

        // Camera follow
        if (CamMagger.instance != null)
        {
            CamMagger.instance.SetTarget(clickedChar.transform);
        }

        Debug.Log($"Selected {clickedChar.characterName} - Moves: {clickedChar.remainingMoves}/{clickedChar.moveSpeed}");
    }

    void ShowMovementRange()
    {
        if (SelectedPlayer == null) return;

        // Clear previous range display
        ClearMovementRange();

        // Get tiles in movement range
        currentMovementRange = SelectedPlayer.GetMovementRange();

        // Highlight tiles in range
        foreach (HexTile tile in currentMovementRange)
        {
            if (tile != null && tile.isWalkable && !tile.hasPlayer)
            {
                tile.SetInMovementRange(true);
            }
        }

        Debug.Log($"Showing {currentMovementRange.Count} tiles in movement range");
    }

    void UpdateMovementRangeDisplay()
    {
        if (SelectedPlayer == null) return;

        // Clear and refresh the movement range
        ClearMovementRange();
        ShowMovementRange();
    }

    void ClearMovementRange()
    {
        foreach (HexTile tile in currentMovementRange)
        {
            if (tile != null)
            {
                tile.SetInMovementRange(false);
            }
        }
        currentMovementRange.Clear();

        ClearPathPreview();
    }

    void UpdatePathPreview(HexTile targetTile)
    {
        if (SelectedPlayer == null || targetTile == null) return;

        // Clear previous path
        ClearPathPreview();

        // Don't show path to current position or blocked tiles
        if (targetTile == SelectedPlayer.currentHex || !targetTile.isWalkable || targetTile.hasPlayer)
            return;

        // Only show path if target is in movement range
        if (!IsInMovementRange(targetTile))
            return;

        // Calculate path (simplified - just showing direct path for now)
        currentPath = CalculateSimplePath(SelectedPlayer.currentHex, targetTile);

        // Highlight path tiles
        foreach (HexTile tile in currentPath)
        {
            if (tile != null && tile != SelectedPlayer.currentHex)
            {
                tile.SetOnPath(true);
            }
        }
    }

    void ClearPathPreview()
    {
        foreach (HexTile tile in currentPath)
        {
            if (tile != null)
            {
                tile.SetOnPath(false);
            }
        }
        currentPath.Clear();
    }

    List<HexTile> CalculateSimplePath(HexTile start, HexTile end)
    {
        List<HexTile> path = new List<HexTile>();

        // For now, just return the end tile as a simple path
        // You can implement A* pathfinding here for proper path calculation
        path.Add(end);

        return path;
    }

    bool IsInMovementRange(HexTile tile)
    {
        return currentMovementRange.Contains(tile);
    }

    void CancelSelection()
    {
        // Clear movement range display
        ClearMovementRange();

        // Deselect tiles
        if (ClickedTile != null)
        {
            ClickedTile.DeSelect();
            ClickedTile = null;
        }

        // Clear selected player
        SelectedPlayer = null;

        // Reset camera
        if (CamMagger.instance != null)
        {
            CamMagger.instance.SetTarget(null);
        }

        Debug.Log("Selection cancelled");
    }

    void OnDisable()
    {
        CancelSelection();
    }

}


