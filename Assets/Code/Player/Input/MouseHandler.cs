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
    public Vector3 worldMousePos { get; private set; }
    // Core references
    private MapMaker mapMaker;
    private Camera mainCamera;

    // Current state tracking
    private HexTile currentHoveredTile = null;
    private HexTile selectedTile = null;
    private Char selectedPlayer = null;
    public ActionModes currentActionType = ActionModes.None; // Changed from Move to None
    public GameObject SpawnAttack;

    // Action system state
    private ActionData selectedAction = null;
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
            Debug.LogError("MapMaker instance not found! MouseHandler will be disabled.");
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
        worldMousePos = GetWorldMousePosition();
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
        if (mapMaker == null) return null;

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
        // Check if mouse is over UI before processing any clicks
        if (UIZoneHandler.instance != null && UIZoneHandler.instance.IsMouseOverUIZone())
        {
            return; // Don't process clicks when over UI
        }

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
            case ActionModes.None:
                HandleNoneMode(clickedTile);
                break;
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

    // New method to handle clicks when no mode is selected
    private void HandleNoneMode(HexTile clickedTile)
    {
        Char characterOnTile = GetCharacterOnTile(clickedTile);

        // Only allow selection of player team characters
        if (characterOnTile != null && characterOnTile.team == Team.player)
        {
            SelectCharacter(characterOnTile, clickedTile);
            // Don't auto-set any mode - wait for user to choose
        }
    }

    private void HandleMoveMode(HexTile clickedTile)
    {
        Char characterOnTile = GetCharacterOnTile(clickedTile);

        if (characterOnTile != null && characterOnTile.team == Team.player)
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
        if (selectedPlayer == null)
        {
            Char characterOnTile = GetCharacterOnTile(clickedTile);
            if (characterOnTile == null || characterOnTile.team != Team.player) return;
            selectedPlayer = characterOnTile;
        }

        if (selectedPlayerActions == null)
        {
            SelectCharacter(selectedPlayer, clickedTile);
        }

        if (selectedAction == null)
        {
            Debug.Log("No action selected! Please select an action first.");
            return;
        }

        // Check if the player can use this action (including action points)
        if (!selectedPlayerActions.CanUseAction(selectedAction))
        {
            Debug.Log($"Cannot use {selectedAction.actionName}! Not enough action points or other requirements not met.");
            return;
        }

        // Check if the target is within range
        if (!IsTargetWithinActionRange(clickedTile))
        {
            Debug.Log($"Target is out of range for {selectedAction.actionName}!");
            return;
        }

        Char targetCharacter = GetCharacterOnTile(clickedTile);

        // Store action points before using action
        int actionPointsBefore = selectedPlayer.charClass.currentActionPoints;

        // Use the action through CharacterActions (this will handle action point deduction)
        selectedPlayerActions.UseAction(selectedAction, clickedTile, targetCharacter);

        // Fire the event for external systems
        OnActionUsed?.Invoke(selectedPlayer, selectedAction, clickedTile);

        Debug.Log($"{selectedPlayer.name} used {selectedAction.actionName} on {clickedTile.coordinates}. AP: {actionPointsBefore} -> {selectedPlayer.charClass.currentActionPoints}");

        // Clear action selection after use
        selectedAction = null;

        // Destroy the preview hitbox since action is used
        if (SpawnAttack != null)
        {
            Destroy(SpawnAttack);
            SpawnAttack = null;
        }

        // Force UI update by calling an event that UI can listen to
        StartCoroutine(DelayedUIUpdate());

        // If no action points left, cancel selection
        if (selectedPlayer.charClass.currentActionPoints <= 0)
        {
            Debug.Log($"{selectedPlayer.name} has no action points left!");
            StartCoroutine(DelayedCancelSelection());
        }
    }
    private IEnumerator DelayedUIUpdate()
    {
        yield return new WaitForEndOfFrame();
        // Force another event to trigger UI refresh
        OnPlayerSelected?.Invoke(selectedPlayer);
    }
    private IEnumerator DelayedCancelSelection()
    {
        yield return new WaitForSeconds(1f); // Give player time to see the result
        CancelSelection();
    }

    private bool IsTargetWithinActionRange(HexTile targetTile)
    {
        if (selectedAction == null || selectedPlayer == null || targetTile == null)
            return false;

        if (selectedAction.range <= 0)
            return true; // No range limit

        // Calculate hex distance between player and target
        Vector2Int playerCoord = selectedPlayer.currentHex.coordinates;
        Vector2Int targetCoord = targetTile.coordinates;

        int distance = GetHexDistance(playerCoord, targetCoord);
        return distance <= selectedAction.range;
    }
    private int GetHexDistance(Vector2Int a, Vector2Int b)
    {
        Vector3 cubeA = AxialToCube(a);
        Vector3 cubeB = AxialToCube(b);

        return (int)((Mathf.Abs(cubeA.x - cubeB.x) + Mathf.Abs(cubeA.y - cubeB.y) + Mathf.Abs(cubeA.z - cubeB.z)) / 2);
    }
    private Vector3 AxialToCube(Vector2Int axial)
    {
        float x = axial.x;
        float z = axial.y;
        float y = -x - z;
        return new Vector3(x, y, z);
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
            //todo
            // selectedPlayerActions.UseItem(selectedItem, clickedTile, targetCharacter);
            OnItemUsed?.Invoke(selectedPlayer, selectedItem, clickedTile);

            // Clear item selection after use
            selectedItem = null;

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

        Debug.Log($"Special action at {clickedTile.coordinates} - implement your special logic here!");
    }

    private bool IsValidItemTarget(HexTile tile)
    {
        if (selectedItem == null || selectedPlayerActions == null) return false;

        try
        {
            List<HexTile> validTargets = selectedPlayerActions.GetValidTargets(selectedItem.actionEffect);
            return validTargets != null && validTargets.Contains(tile);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error checking valid item targets: {e.Message}");
            return false;
        }
    }

    private Char GetCharacterOnTile(HexTile tile)
    {
        if (tile == null) return null;

        try
        {
            Char[] allCharacters = FindObjectsOfType<Char>();
            foreach (Char character in allCharacters)
            {
                if (character != null && character.currentHex == tile)
                {
                    return character;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error finding character on tile: {e.Message}");
        }

        return null;
    }

    private void HandleClickOnCharacter(HexTile clickedTile, Char character)
    {
        // In move mode, only allow selection of player team characters
        if (currentActionType == ActionModes.Move)
        {
            if (selectedPlayer == character)
            {
                return;
            }
            SelectCharacter(character, clickedTile);
        }
        // In action/item modes, clicking on characters can be targeting them
        else if (currentActionType == ActionModes.Actions || currentActionType == ActionModes.Item)
        {
            SelectCharacter(character, clickedTile);
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

            if (selectedPlayer == character)
            {
                return;
            }
        }
        else if (currentActionType == ActionModes.None)
        {
            // In None mode, just select the character
            SelectCharacter(character, clickedTile);
        }
    }

    private void SelectCharacter(Char character, HexTile tile)
    {
        if (character == null || tile == null) return;

        try
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

            // Only auto-select based on current mode if we're not in None mode
            switch (currentActionType)
            {
                case ActionModes.Move:
                    ShowMovementRange();
                    break;
                case ActionModes.Actions:
                    // Don't auto-select action anymore
                    break;
                case ActionModes.Item:
                    // Don't auto-select item anymore
                    break;
                case ActionModes.Special:
                    // Don't auto-select special anymore
                    break;
                case ActionModes.None:
                    // Don't show any ranges in None mode
                    break;
            }

            // Set camera target
            if (CamMagger.instance != null)
            {
                CamMagger.instance.SetTarget(character.transform);
            }

            // Fire event
            OnPlayerSelected?.Invoke(character);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error selecting character: {e.Message}");
        }
    }

    private void UpdateRangeDisplays()
    {
        if (selectedPlayer == null) return;

        try
        {
            switch (currentActionType)
            {
                case ActionModes.Move:
                    if (selectedPlayer.charClass.movementSpeed > 0)
                    {
                        ShowMovementRange();
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
        catch (System.Exception e)
        {
            Debug.LogError($"Error updating range displays: {e.Message}");
        }
    }

    private void HandleClickOnEmptyTile(HexTile clickedTile)
    {
        if (!IsValidMoveTarget(clickedTile))
        {
            return;
        }

        try
        {
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
        catch (System.Exception e)
        {
            Debug.LogError($"Error handling empty tile click: {e.Message}");
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
        if (action == null)
        {
            Debug.LogWarning("Trying to select null action!");
            return;
        }

        selectedAction = action;
        selectedItem = null; // Clear item selection

        // Spawn hitbox immediately at player's position when action is selected
        SpawnActionHitbox(action);

        Debug.Log($"Selected action: {action.actionName}");
    }

    private void SpawnActionHitbox(ActionData action)
    {
        if (selectedPlayer == null || action.hitboxPrefab == null) return;

        try
        {
            // Destroy previous attack if exists
            if (SpawnAttack != null)
            {
                Destroy(SpawnAttack);
            }

            // Always spawn at player's position when action is selected
            Vector3 spawnPosition = selectedPlayer.transform.position;

            SpawnAttack = Instantiate(action.hitboxPrefab, spawnPosition, Quaternion.identity);
            AttackHitMainbox hitbox = SpawnAttack.GetComponentInChildren<AttackHitMainbox>();

            if (hitbox != null)
            {
                // Initialize hitbox with all parameters including width and target type
                hitbox.InitializeForPreview(
                    action.damage,
                    selectedPlayer.team,
                    action.hitboxLifetime,
                    action.Length,
                    action.Width,        // Pass the width parameter
                    action ,   // Pass the target type
                    new Vector2(selectedPlayer.transform.position.x, selectedPlayer.transform.position.y)
                );
            }

            Debug.Log($"Spawned action hitbox for {action.actionName} at player position with Width: {action.Width}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error spawning action hitbox: {e.Message}");
        }
    }

    public void SelectItem(ItemData item)
    {
        if (item == null)
        {
            Debug.LogWarning("Trying to select null item!");
            return;
        }

        selectedItem = item;
        selectedAction = null; // Clear action selection
        Debug.Log($"Selected item: {item.itemName}");
    }

    // Range Display Methods
    private void ShowMovementRange()
    {
        if (selectedPlayer == null) return;

        try
        {
            ClearMovementRange();
            List<HexTile> movementTiles = selectedPlayer.GetMovementRange();

            if (movementTiles != null)
            {
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
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error showing movement range: {e.Message}");
        }
    }
    private void ShowItemRange()
    {
        if (selectedPlayer == null || selectedItem == null || selectedPlayerActions == null) return;

        try
        {
            ClearActionRange();
            List<HexTile> validTargets = selectedPlayerActions.GetValidTargets(selectedItem.actionEffect);

            if (validTargets != null)
            {
                currentActionRange = new HashSet<HexTile>(validTargets);

                foreach (HexTile tile in currentActionRange)
                {
                    if (tile != null)
                    {
                        tile.SetMovementDestination(true); // Reuse this visual for item targets
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error showing item range: {e.Message}");
        }
    }

    private bool IsNeighborOfSelected(HexTile tile)
    {
        if (selectedTile == null || tile == null || mapMaker == null) return false;

        try
        {
            List<Vector2Int> neighborCoords = mapMaker.GetNeighbors(selectedTile.Coordinates);
            return neighborCoords != null && neighborCoords.Contains(tile.Coordinates);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error checking if tile is neighbor: {e.Message}");
            return false;
        }
    }

    private void UpdateMovementRangeDisplay()
    {
        if (selectedPlayer == null) return;

        ClearMovementRange();
        if (selectedPlayer.charClass.movementSpeed > 0)
        {
            ShowMovementRange();
        }
    }

    public void UpdateCharacterPositionDisplays()
    {
        try
        {
            if (mapMaker == null) return;

            Char[] allCharacters = FindObjectsOfType<Char>();

            foreach (var tile in mapMaker.GetAllTiles())
            {
                if (tile != null)
                {
                    tile.SetCharacterPresent(false);
                }
            }

            foreach (Char character in allCharacters)
            {
                if (character != null && character.currentHex != null)
                {
                    character.currentHex.SetCharacterPresent(true, character.team);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error updating character position displays: {e.Message}");
        }
    }

    // Cleanup Methods
    private void ClearMovementRange()
    {
        try
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
        catch (System.Exception e)
        {
            Debug.LogError($"Error clearing movement range: {e.Message}");
        }
    }

    private void ClearActionRange()
    {
        try
        {
            foreach (HexTile tile in currentActionRange)
            {
                if (tile != null)
                {
                    tile.SetMovementDestination(false);
                }
            }
            currentActionRange.Clear();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error clearing action range: {e.Message}");
        }
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
        try
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
            currentActionType = ActionModes.None; // Reset to None
            if(SpawnAttack != null)
                Destroy(SpawnAttack);
            if (CamMagger.instance != null)
            {
                CamMagger.instance.SetTarget(null);
            }

            OnSelectionCancelled?.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error cancelling selection: {e.Message}");
        }
    }

    public void SetActionMode(ActionModes mode)
    {
        try
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
        catch (System.Exception e)
        {
            Debug.LogError($"Error setting action mode: {e.Message}");
        }
    }


    public Char GetSelectedPlayer() => selectedPlayer;


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



