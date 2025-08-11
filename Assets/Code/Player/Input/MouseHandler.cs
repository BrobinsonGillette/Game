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
    [SerializeField] private float doubleClickTime = 0.3f;
    [SerializeField] private bool enablePathPreview = true;
    [SerializeField] private bool enableRightClickToCancel = true;

    // Core references
    private MapMaker mapMaker;
    private Camera mainCamera;

    // Current state tracking
    private HexTile currentHoveredTile = null;
    private HexTile clickedTile = null;
    private Char selectedPlayer = null;
    public ActionModes currentActionType = ActionModes.None;

    // Movement visualization
    private HashSet<HexTile> currentMovementRange = new HashSet<HexTile>();
    private HashSet<HexTile> highlightedNeighbors = new HashSet<HexTile>();

    // Input timing
    private float lastClickTime = 0f;
    private HexTile lastClickedTile = null;

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

        if (mainCamera == null)
        {
            Debug.LogError("MouseHandler: No camera found!");
        }
    }

    private void Start()
    {
        mapMaker = MapMaker.instance;
        if (mapMaker == null)
        {
            Debug.LogError("MouseHandler: MapMaker instance not found!");
            enabled = false;
            return;
        }

        // Subscribe to map generation events if needed
        if (mapMaker.OnGridGenerated != null)
        {
            mapMaker.OnGridGenerated += OnMapGenerated;
        }
    }

    private void OnMapGenerated()
    {
        // Reset state when map is regenerated
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
        // Use CamMagger if available, otherwise fallback to camera conversion
        if (CamMagger.instance != null)
        {
            return CamMagger.instance.WorldMousePosition;
        }

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -mainCamera.transform.position.z; // Adjust for 2D
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

        // Handle mouse exit on previous tile
        if (currentHoveredTile != null)
        {
            currentHoveredTile.MouseExit();
        }

        // Update current hovered tile
        currentHoveredTile = newHoveredTile;

        // Handle mouse enter on new tile
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

        // Handle middle mouse or other inputs here if needed
    }

    private void HandleLeftClick(HexTile clickedTile)
    {
        if (clickedTile == null) return;

        // Check for double click
        bool isDoubleClick = CheckForDoubleClick(clickedTile);

        if (clickedTile.hasChar)
        {
            HandleClickOnPlayer(clickedTile, isDoubleClick);
        }
        else if (selectedPlayer != null)
        {
            HandleClickOnEmptyTile(clickedTile, isDoubleClick);
        }
        else
        {
            HandleClickOnTile(clickedTile, isDoubleClick);
        }
    }

    private bool CheckForDoubleClick(HexTile clickedTile)
    {
        float currentTime = Time.time;
        bool isDoubleClick = (currentTime - lastClickTime) < doubleClickTime &&
                            lastClickedTile == clickedTile;

        lastClickTime = currentTime;
        lastClickedTile = clickedTile;

        return isDoubleClick;
    }

    private void HandleClickOnPlayer(HexTile clickedTile, bool isDoubleClick)
    {
        Char clickedChar = clickedTile.CurrentPlayer;
        if (clickedChar == null) return;

        // Double click to center camera on player
        if (isDoubleClick && CamMagger.instance != null)
        {
            CamMagger.instance.SetTarget(clickedChar.transform);
            return;
        }

        // Toggle selection if clicking the same character
        if (selectedPlayer == clickedChar)
        {
            CancelSelection();
            return;
        }

        switch(currentActionType)
        {
            case ActionModes.Move:
                // If in move mode, select the player
                HandleClickOnPlayerInMoveMode(clickedChar, clickedTile);
                break;
            case ActionModes.Actions:
                // Handle actions if needed
                break;
            case ActionModes.Item:
                // Handle item interactions if needed
                break;
            case ActionModes.Special:
                // Handle special actions if needed
                break;
     
        }
    }

    private void HandleClickOnEmptyTile(HexTile clickedTile, bool isDoubleClick)
    {
        if (!IsValidMoveTarget(clickedTile))
        {
            // Invalid move target - could show feedback here
            Debug.Log("Invalid move target!");
            return;
        }

        // Attempt to move the selected player
        bool moveSuccessful = AttemptPlayerMove(clickedTile);

        if (moveSuccessful)
        {
            OnPlayerMoved?.Invoke(selectedPlayer, clickedTile);

            // Update movement display
            UpdateMovementRangeDisplay();

            // Auto-deselect if no moves remaining
            if (selectedPlayer != null && !selectedPlayer.CanMove())
            {
                CancelSelection();
            }
        }
    }

    private void HandleClickOnTile(HexTile clickedTile, bool isDoubleClick)
    {
        // Deselect previous tile
        if (this.clickedTile != null && this.clickedTile != clickedTile)
        {
            this.clickedTile.Deselect();
        }

        // Select new tile
        this.clickedTile = clickedTile;
        this.clickedTile.Interact();
    }

    private void HandleRightClick()
    {
        CancelSelection();
    }

    private void HandleClickOnPlayerInMoveMode(Char player, HexTile playerTile)
    {

        if (player.team != Team.player) return;

        // Deselect previous tile if different
        if (clickedTile != null && clickedTile != playerTile)
        {
            clickedTile.SetSelected(false);
        }

        // Update selection state
        selectedPlayer = player;
        clickedTile = playerTile;
        clickedTile.SetSelected(true);

        if (selectedPlayer.charClass.remainingMoves > 0)
            ShowMovementRange();

        // Set camera target
        if (CamMagger.instance != null)
        {
            CamMagger.instance.SetTarget(player.transform);
        }

        // Fire event
        OnPlayerSelected?.Invoke(player);
    }

    private bool IsValidMoveTarget(HexTile tile)
    {
        return tile != null &&
               tile.isWalkable &&
               !tile.hasChar &&
               IsInMovementRange(tile);
    }

    private bool AttemptPlayerMove(HexTile targetTile)
    {
        if (selectedPlayer == null || targetTile == null) return false;

        return selectedPlayer.MovePlayerToTile(targetTile);
    }

    private void ShowMovementRange()
    {
        if (selectedPlayer == null) return;

        ClearMovementRange();

        // Get movement range from the character
        List<HexTile> movementTiles = selectedPlayer.GetMovementRange();

        // Convert to HashSet for faster lookups
        currentMovementRange = new HashSet<HexTile>(movementTiles);

        // Highlight movement range tiles
        foreach (HexTile tile in currentMovementRange)
        {
            if (tile != null && tile.isWalkable && !tile.hasChar)
            {
                tile.SetInMovementRange(true);
            }
        }

        // Highlight immediate neighbors differently
        HighlightNeighbors();

        Debug.Log($"Showing {currentMovementRange.Count} tiles in movement range");
    }

    private void HighlightNeighbors()
    {
        if (clickedTile == null) return;

        List<Vector2Int> neighborCoords = mapMaker.GetNeighbors(clickedTile.coordinates);

        foreach (Vector2Int neighborCoord in neighborCoords)
        {
            HexTile neighborTile = mapMaker.GetHexTile(neighborCoord);
            if (neighborTile != null && neighborTile.isWalkable && !neighborTile.hasChar)
            {
                neighborTile.SetMovementRange(true);
                highlightedNeighbors.Add(neighborTile);
            }
        }
    }

    private void UpdateMovementRangeDisplay()
    {
        if (selectedPlayer == null) return;
        ClearMovementRange();
        if(selectedPlayer.charClass.remainingMoves > 0)
            ShowMovementRange();
    }

    private void ClearMovementRange()
    {
        // Clear movement range highlighting
        foreach (HexTile tile in currentMovementRange)
        {
            if (tile != null)
            {
                tile.SetInMovementRange(false);
            }
        }
        currentMovementRange.Clear();

        // Clear neighbor highlighting
        foreach (HexTile tile in highlightedNeighbors)
        {
            if (tile != null)
            {
                tile.SetMovementRange(false);
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
        if (clickedTile != null)
        {
            clickedTile.SetSelected(false);
            clickedTile = null;
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

        Debug.Log("Selection cancelled");
    }

    // Public getters for other systems
    public Char SelectedPlayer => selectedPlayer;
    public HexTile ClickedTile => clickedTile;
    public bool HasSelection => selectedPlayer != null;

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



