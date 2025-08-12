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
    [SerializeField] private Char selectedPlayer = null;
    public ActionModes currentActionType = ActionModes.Move;

    // Action system state
    [SerializeField] private ActionData selectedAction = null;
    private ItemData selectedItem = null;
    private CharacterActions selectedPlayerActions = null;

    // Movement and action visualization
    private HashSet<HexTile> currentMovementRange = new HashSet<HexTile>();
    private HashSet<HexTile> currentActionRange = new HashSet<HexTile>();
    private HashSet<HexTile> highlightedNeighbors = new HashSet<HexTile>();

    // Events
    public System.Action<Char> OnPlayerSelected;
    public System.Action OnSelectionCancelled;
    public System.Action<Char, HexTile> OnPlayerMoved;
    public System.Action<Char, ActionData, HexTile> OnActionUsed;
    public System.Action<Char, ItemData, HexTile> OnItemUsed;

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

        switch (currentActionType)
        {
            case ActionModes.Move:
                HandleMoveMode(clickedTile);
                break;
            case ActionModes.Actions:
                HandleActionMode(clickedTile);
                break;
            case ActionModes.Item:
                HandleItemMode(clickedTile);
                break;
            case ActionModes.Special:
                HandleSpecialMode(clickedTile);
                break;
        }
    }

    private void HandleMoveMode(HexTile clickedTile)
    {
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

    private void HandleActionMode(HexTile clickedTile)
    {
        if (selectedPlayer == null || selectedPlayerActions == null || selectedAction == null)
        {
            Debug.Log("Select a character and action first!");
            return;
        }

        if (IsValidActionTarget(clickedTile))
        {
            Char targetCharacter = GetCharacterOnTile(clickedTile);
            selectedPlayerActions.UseAction(selectedAction, clickedTile, targetCharacter);
            OnActionUsed?.Invoke(selectedPlayer, selectedAction, clickedTile);

            // Clear action selection after use
            selectedAction = null;
            UpdateActionRangeDisplay();

            Debug.Log($"{selectedPlayer.name} used action on {clickedTile.coordinates}");
        }
        else
        {
            Debug.Log("Invalid target for this action!");
        }
    }

    private void HandleItemMode(HexTile clickedTile)
    {
        if (selectedPlayer == null || selectedPlayerActions == null || selectedItem == null)
        {
            Debug.Log("Select a character and item first!");
            return;
        }

        if (IsValidItemTarget(clickedTile))
        {
            Char targetCharacter = GetCharacterOnTile(clickedTile);
            selectedPlayerActions.UseItem(selectedItem, clickedTile, targetCharacter);
            OnItemUsed?.Invoke(selectedPlayer, selectedItem, clickedTile);

            // Clear item selection after use
            selectedItem = null;
            UpdateActionRangeDisplay();

            Debug.Log($"{selectedPlayer.name} used item on {clickedTile.coordinates}");
        }
        else
        {
            Debug.Log("Invalid target for this item!");
        }
    }

    private void HandleSpecialMode(HexTile clickedTile)
    {
        if (selectedPlayer == null || selectedPlayerActions == null)
        {
            Debug.Log("Select a character first!");
            return;
        }

        // For special actions, you might want to implement a separate system
        // This is a simplified version
        Debug.Log($"Special action at {clickedTile.coordinates} - implement your special logic here!");
    }

    private bool IsValidActionTarget(HexTile tile)
    {
        if (selectedAction == null || selectedPlayerActions == null) return false;

        List<HexTile> validTargets = selectedPlayerActions.GetValidTargets(selectedAction);
        return validTargets.Contains(tile);
    }

    private bool IsValidItemTarget(HexTile tile)
    {
        if (selectedItem == null || selectedPlayerActions == null) return false;

        List<HexTile> validTargets = selectedPlayerActions.GetValidTargets(selectedItem.actionEffect);
        return validTargets.Contains(tile);
    }

    private Char GetCharacterOnTile(HexTile tile)
    {
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
        // In move mode, only allow selection of player team characters
        if (currentActionType == ActionModes.Move)
        {
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
        // In action/item modes, clicking on characters can be targeting them
        else if (currentActionType == ActionModes.Actions || currentActionType == ActionModes.Item)
        {
            // If we have a selected player and action/item, try to target this character
            if (selectedPlayer != null && (selectedAction != null || selectedItem != null))
            {
                if (currentActionType == ActionModes.Actions)
                {
                    HandleActionMode(clickedTile);
                }
                else if (currentActionType == ActionModes.Item)
                {
                    HandleItemMode(clickedTile);
                }
                return;
            }

            // Otherwise, if it's a player character, select them
            if (character.team == Team.player)
            {
                if (selectedPlayer == character)
                {
                    CancelSelection();
                    return;
                }
                SelectCharacter(character, clickedTile);
            }
        }
    }

    private void SelectCharacter(Char character, HexTile tile)
    {
        // Clear previous selection
        ClearAllRanges();
        if (selectedTile != null)
        {
            selectedTile.SetSelected(false);
        }

        // Set new selection
        selectedPlayer = character;
        selectedTile = tile;
        selectedPlayerActions = character.GetComponent<CharacterActions>();
        tile.SetSelected(true);

        // Auto-select basic attack and show attack range
        AutoSelectBasicAttack();

        // Set camera target
        if (CamMagger.instance != null)
        {
            CamMagger.instance.SetTarget(character.transform);
        }

        // Fire event
        OnPlayerSelected?.Invoke(character);
    }

    private void UpdateRangeDisplays()
    {
        switch (currentActionType)
        {
            case ActionModes.Move:
                if (selectedPlayer.movementSpeed > 0)
                {
                    ShowMovementRange();
                }
                break;
            case ActionModes.Actions:
                if (selectedAction != null)
                {
                    ShowActionRange();
                }
                break;
            case ActionModes.Item:
                if (selectedItem != null)
                {
                    ShowItemRange();
                }
                break;
        }
    }

    private void HandleClickOnEmptyTile(HexTile clickedTile)
    {
        if (!IsValidMoveTarget(clickedTile))
        {
            return;
        }

        bool moveSuccessful = selectedPlayer.MovePlayerToTile(clickedTile);

        if (moveSuccessful)
        {
            OnPlayerMoved?.Invoke(selectedPlayer, clickedTile);
            UpdateCharacterPositionDisplays();
            UpdateMovementRangeDisplay();

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

    // Action and Item Selection Methods
    public void SelectAction(ActionData action)
    {
        selectedAction = action;
        selectedItem = null; // Clear item selection
        UpdateActionRangeDisplay();
        Debug.Log($"Selected action: {action.actionName}");
    }

    public void SelectItem(ItemData item)
    {
        selectedItem = item;
        selectedAction = null; // Clear action selection
        UpdateActionRangeDisplay();
        Debug.Log($"Selected item: {item.itemName}");
    }

    // Range Display Methods
    private void ShowMovementRange()
    {
        if (selectedPlayer == null) return;

        ClearMovementRange();
        List<HexTile> movementTiles = selectedPlayer.GetMovementRange();
        currentMovementRange = new HashSet<HexTile>(movementTiles);

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

    private void ShowActionRange()
    {
        if (selectedPlayer == null || selectedAction == null || selectedPlayerActions == null) return;

        ClearActionRange();
        List<HexTile> validTargets = selectedPlayerActions.GetValidTargets(selectedAction);
        currentActionRange = new HashSet<HexTile>(validTargets);

        foreach (HexTile tile in currentActionRange)
        {
            if (tile != null)
            {
                // Use different colors for different target types
                Char characterOnTile = GetCharacterOnTile(tile);
                if (characterOnTile != null)
                {
                    if (characterOnTile.team != selectedPlayer.team)
                    {
                        // Enemy target - use attack color (red-ish)
                        tile.SetAttackTarget(true);
                    }
                    else
                    {
                        // Ally target - use support color (blue-ish) 
                        tile.SetSupportTarget(true);
                    }
                }
                else
                {
                    // Empty tile in range
                    tile.SetMovementDestination(true);
                }
            }
        }
    }

    private void ShowItemRange()
    {
        if (selectedPlayer == null || selectedItem == null || selectedPlayerActions == null) return;

        ClearActionRange();
        List<HexTile> validTargets = selectedPlayerActions.GetValidTargets(selectedItem.actionEffect);
        currentActionRange = new HashSet<HexTile>(validTargets);

        foreach (HexTile tile in currentActionRange)
        {
            if (tile != null)
            {
                tile.SetMovementDestination(true); // Reuse this visual for item targets
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

    private void UpdateActionRangeDisplay()
    {
        ClearActionRange();

        switch (currentActionType)
        {
            case ActionModes.Actions:
                if (selectedAction != null)
                {
                    ShowActionRange();
                }
                break;
            case ActionModes.Item:
                if (selectedItem != null)
                {
                    ShowItemRange();
                }
                break;
        }
    }

    public void UpdateCharacterPositionDisplays()
    {
        Char[] allCharacters = FindObjectsOfType<Char>();

        foreach (var tile in mapMaker.GetAllTiles())
        {
            tile.SetCharacterPresent(false);
        }

        foreach (Char character in allCharacters)
        {
            if (character.currentHex != null)
            {
                character.currentHex.SetCharacterPresent(true, character.team);
            }
        }
    }

    // Cleanup Methods
    private void ClearMovementRange()
    {
        foreach (HexTile tile in currentMovementRange)
        {
            if (tile != null)
            {
                tile.SetMovementRange(false);
            }
        }
        currentMovementRange.Clear();

        foreach (HexTile tile in highlightedNeighbors)
        {
            if (tile != null)
            {
                tile.SetMovementDestination(false);
            }
        }
        highlightedNeighbors.Clear();
    }

    private void ClearActionRange()
    {
        foreach (HexTile tile in currentActionRange)
        {
            if (tile != null)
            {
                tile.SetMovementDestination(false);
                tile.SetAttackTarget(false);
                tile.SetSupportTarget(false);
            }
        }
        currentActionRange.Clear();
    }

    private void ClearAllRanges()
    {
        ClearMovementRange();
        ClearActionRange();
    }

    private bool IsInMovementRange(HexTile tile)
    {
        return currentMovementRange.Contains(tile);
    }

    public void CancelSelection()
    {
        ClearAllRanges();

        if (selectedTile != null)
        {
            selectedTile.SetSelected(false);
            selectedTile = null;
        }

        selectedPlayer = null;
        selectedPlayerActions = null;
        selectedAction = null;
        selectedItem = null;

        if (CamMagger.instance != null)
        {
            CamMagger.instance.SetTarget(null);
        }

        OnSelectionCancelled?.Invoke();
    }

    public void SetActionMode(ActionModes mode)
    {
        currentActionType = mode;

        // Clear action/item selection when changing modes
        if (mode != ActionModes.Actions)
        {
            selectedAction = null;
        }
        if (mode != ActionModes.Item)
        {
            selectedItem = null;
        }

        // Update range displays
        ClearActionRange();
        if (selectedPlayer != null)
        {
            UpdateRangeDisplays();
        }
    }

    // Auto-select basic attack when character is selected
    public void AutoSelectBasicAttack()
    {
        if (selectedPlayer == null || selectedPlayerActions == null) return;

        // Find the first attack action
        foreach (ActionData action in selectedPlayerActions.availableActions)
        {
            if (action.actionType == ActionType.Attack)
            {
                selectedAction = action;
                SetActionMode(ActionModes.Actions);
                ShowActionRange();
                Debug.Log($"Auto-selected attack: {action.actionName}");
                return;
            }
        }
    }

    // Getter methods for UI
    public ActionData GetSelectedAction() => selectedAction;
    public ItemData GetSelectedItem() => selectedItem;
    public Char GetSelectedPlayer() => selectedPlayer;
    public CharacterActions GetSelectedPlayerActions() => selectedPlayerActions;

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



