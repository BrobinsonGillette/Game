using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum ActionModes
{
    None,
    Move,
    Actions,
    Item,
    Special
}
public class MouseHandler : MonoBehaviour
{
    public static MouseHandler instance;

    [Header("Settings")]
    [SerializeField] private bool enablePathPreview = true;
    [SerializeField] private bool enableRightClickToCancel = true;

    // Core references
    private MapMaker mapMaker;
    private Camera mainCamera;

    // Current state tracking
    private HexTile currentHoveredTile = null;
    private HexTile selectedTile = null;
    private Char selectedPlayer = null;
    public ActionModes currentActionType = ActionModes.Move; // Default to move mode

    // Movement visualization
    private HashSet<HexTile> currentMovementRange = new HashSet<HexTile>();
    private HashSet<HexTile> highlightedNeighbors = new HashSet<HexTile>();

    // Events
    public System.Action<Char> OnPlayerSelected;
    public System.Action OnSelectionCancelled;
    public System.Action<Char, HexTile> OnPlayerMoved;

    private void Awake()
    {
        InitializeSingleton();
        InitializeReferences();
    }

    private void InitializeSingleton()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple MouseHandler instances found! Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    private void InitializeReferences()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
    }

    private void Start()
    {
        mapMaker = MapMaker.instance;
        if (mapMaker == null)
        {
            enabled = false;
            return;
        }

        if (mapMaker.OnGridGenerated != null)
        {
            mapMaker.OnGridGenerated += OnMapGenerated;
        }
    }

    private void OnMapGenerated()
    {
        CancelSelection();
    }

    private void Update()
    {
        if (!IsInitialized()) return;
        HandleMouseInput();
    }

    private bool IsInitialized()
    {
        return mapMaker != null && mainCamera != null;
    }

    private void HandleMouseInput()
    {
        Vector3 worldMousePos = GetWorldMousePosition();
        HexTile hoveredTile = GetHexTileAtPosition(worldMousePos);

        HandleHover(hoveredTile);
        HandleMouseClicks(hoveredTile);
    }

    private Vector3 GetWorldMousePosition()
    {
        if (CamMagger.instance != null)
        {
            return CamMagger.instance.WorldMousePosition;
        }

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -mainCamera.transform.position.z;
        return mainCamera.ScreenToWorldPoint(mousePos);
    }

    private HexTile GetHexTileAtPosition(Vector3 worldPos)
    {
        Vector2Int hexCoord = mapMaker.WorldToHexPosition(worldPos);
        return mapMaker.GetHexTile(hexCoord);
    }

    private void HandleHover(HexTile newHoveredTile)
    {
        if (newHoveredTile == currentHoveredTile) return;

        // Clear previous hover
        if (currentHoveredTile != null)
        {
            currentHoveredTile.MouseExit();
        }

        // Set new hover
        currentHoveredTile = newHoveredTile;

        if (currentHoveredTile != null)
        {
            currentHoveredTile.MousedOver();
        }
    }

    private void HandleMouseClicks(HexTile hoveredTile)
    {
        if (Input.GetMouseButtonDown(0)) // Left click
        {
            HandleLeftClick(hoveredTile);
        }

        if (Input.GetMouseButtonDown(1) && enableRightClickToCancel) // Right click
        {
            HandleRightClick();
        }
    }

    private void HandleLeftClick(HexTile clickedTile)
    {
        if (clickedTile == null) return;

        clickedTile.Interact();

        // Get character on this tile (if any)
        Char characterOnTile = GetCharacterOnTile(clickedTile);

        if (characterOnTile != null)
        {
            HandleClickOnCharacter(clickedTile, characterOnTile);
        }
        else if (selectedPlayer != null)
        {
            HandleClickOnEmptyTile(clickedTile);
        }

    }

    private Char GetCharacterOnTile(HexTile tile)
    {
        // Find any character whose current hex matches this tile
        Char[] allCharacters = FindObjectsOfType<Char>();
        foreach (Char character in allCharacters)
        {
            if (character.currentHex == tile)
            {
                return character;
            }
        }
        return null;
    }

    private void HandleClickOnCharacter(HexTile clickedTile, Char character)
    {
        if (currentActionType != ActionModes.Move) return;

        // Only allow selection of player team characters
        if (character.team != Team.player) return;

        // Toggle selection if clicking the same character
        if (selectedPlayer == character)
        {
            CancelSelection();
            return;
        }

        // Select the new character
        SelectCharacter(character, clickedTile);
    }

    private void SelectCharacter(Char character, HexTile tile)
    {
        // Clear previous selection
        ClearMovementRange();
        if (selectedTile != null)
        {
            selectedTile.SetSelected(false);
        }

        // Set new selection
        selectedPlayer = character;
        selectedTile = tile;
        tile.SetSelected(true);

        // Show movement range if character can move
        if (character.movementSpeed > 0)
        {
            ShowMovementRange();
        }

        // Set camera target
        if (CamMagger.instance != null)
        {
            CamMagger.instance.SetTarget(character.transform);
        }

        // Fire event
        OnPlayerSelected?.Invoke(character);
    }

    private void HandleClickOnEmptyTile(HexTile clickedTile)
    {
        if (!IsValidMoveTarget(clickedTile))
        {
            return;
        }

        // Attempt to move the selected player
        bool moveSuccessful = selectedPlayer.MovePlayerToTile(clickedTile);

        if (moveSuccessful)
        {
            OnPlayerMoved?.Invoke(selectedPlayer, clickedTile);

            // Update visual displays
            UpdateCharacterPositionDisplays();
            UpdateMovementRangeDisplay();

            // Auto-deselect if no moves remaining
            if (selectedPlayer != null && !selectedPlayer.CanMove())
            {
                CancelSelection();
            }
        }
    }


    private void HandleRightClick()
    {
        CancelSelection();
    }

    private bool IsValidMoveTarget(HexTile tile)
    {
        return tile != null &&
               tile.IsWalkable &&
               !tile.HasCharacter &&
               IsInMovementRange(tile);
    }

    private void ShowMovementRange()
    {
        if (selectedPlayer == null) return;

        ClearMovementRange();

        // Get movement range from the character
        List<HexTile> movementTiles = selectedPlayer.GetMovementRange();
        currentMovementRange = new HashSet<HexTile>(movementTiles);

        // Update visual display for movement range
        foreach (HexTile tile in currentMovementRange)
        {
            if (tile != null && tile.IsWalkable && !tile.HasCharacter)
            {
                bool isNeighbor = IsNeighborOfSelected(tile);

                if (isNeighbor)
                {
                    tile.SetMovementDestination(true);
                    highlightedNeighbors.Add(tile);
                }
                else
                {
                    tile.SetMovementRange(true);
                }
            }
        }
    }

    private bool IsNeighborOfSelected(HexTile tile)
    {
        if (selectedTile == null) return false;

        List<Vector2Int> neighborCoords = mapMaker.GetNeighbors(selectedTile.Coordinates);
        return neighborCoords.Contains(tile.Coordinates);
    }

    private void UpdateMovementRangeDisplay()
    {
        if (selectedPlayer == null) return;

        ClearMovementRange();

        if (selectedPlayer.movementSpeed > 0)
        {
            ShowMovementRange();
        }
    }

    public void UpdateCharacterPositionDisplays()
    {
        // Update all tile displays to show current character positions
        Char[] allCharacters = FindObjectsOfType<Char>();

        // First clear all character displays
        foreach (var tile in mapMaker.GetAllTiles())
        {
            tile.SetCharacterPresent(false);
        }

        // Then set displays for tiles with characters
        foreach (Char character in allCharacters)
        {
            if (character.currentHex != null)
            {
                character.currentHex.SetCharacterPresent(true, character.team);
            }
        }
    }

    private void ClearMovementRange()
    {
        // Clear movement range highlighting
        foreach (HexTile tile in currentMovementRange)
        {
            if (tile != null)
            {
                tile.SetMovementRange(false);
            }
        }
        currentMovementRange.Clear();

        // Clear neighbor highlighting
        foreach (HexTile tile in highlightedNeighbors)
        {
            if (tile != null)
            {
                tile.SetMovementDestination(false);
            }
        }
        highlightedNeighbors.Clear();
    }

    private bool IsInMovementRange(HexTile tile)
    {
        return currentMovementRange.Contains(tile);
    }

    public void CancelSelection()
    {
        // Clear visual indicators
        ClearMovementRange();

        // Deselect tiles
        if (selectedTile != null)
        {
            selectedTile.SetSelected(false);
            selectedTile = null;
        }

        // Clear selected player
        selectedPlayer = null;

        // Reset camera
        if (CamMagger.instance != null)
        {
            CamMagger.instance.SetTarget(null);
        }

        // Fire event
        OnSelectionCancelled?.Invoke();
    }

    public void SetActionMode(ActionModes mode)
    {
        currentActionType = mode;

        // Clear selection when changing modes
        if (mode != ActionModes.Move)
        {
            CancelSelection();
        }
    }

    private void OnDisable()
    {
        CancelSelection();
    }

    private void OnDestroy()
    {
        if (mapMaker != null && mapMaker.OnGridGenerated != null)
        {
            mapMaker.OnGridGenerated -= OnMapGenerated;
        }
    }
}



