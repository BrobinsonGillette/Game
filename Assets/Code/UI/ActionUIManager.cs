using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionUIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject actionPanel;
    public GameObject itemPanel;
    public Button moveButton;
    public Button actionsButton;
    public Button itemsButton;
    public Button specialButton;

    [Header("Action UI")]
    public Transform actionButtonParent;
    public GameObject actionButtonPrefab;

    [Header("Item UI")]
    public Transform itemButtonParent;
    public GameObject itemButtonPrefab;

    [Header("Character Info")]
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI actionPointsText;
    public TextMeshProUGUI healthText;

    private MouseHandler mouseHandler;
    private List<Button> actionButtons = new List<Button>();
    private List<Button> itemButtons = new List<Button>();
    private Char currentSelectedCharacter;

    // Update frequency for UI refresh
    private float uiUpdateInterval = 0.1f;
    private float lastUpdateTime;

    private void Start()
    {
        try
        {
            mouseHandler = MouseHandler.instance;

            if (mouseHandler != null)
            {
                mouseHandler.OnPlayerSelected += OnPlayerSelected;
                mouseHandler.OnSelectionCancelled += OnSelectionCancelled;
                mouseHandler.OnActionUsed += OnActionUsed; // Subscribe to action used event
                mouseHandler.OnPlayerMoved += OnPlayerMoved; // Subscribe to movement event
            }
            else
            {
                Debug.LogWarning("MouseHandler instance not found during ActionUIManager initialization");
            }

            SetupModeButtons();
            HideAllPanels();
            HideModeButtons();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error initializing ActionUIManager: {e.Message}");
        }
    }

    private void Update()
    {
        // Periodically update UI to catch any changes
        if (Time.time - lastUpdateTime > uiUpdateInterval && currentSelectedCharacter != null)
        {
            RefreshCharacterInfo();
            RefreshButtonStates();
            lastUpdateTime = Time.time;
        }
    }

    // New event handlers for action and movement
    private void OnActionUsed(Char character, ActionData action, HexTile tile)
    {
        if (character == currentSelectedCharacter)
        {
            RefreshUI();
        }
    }

    private void OnPlayerMoved(Char character, HexTile tile)
    {
        if (character == currentSelectedCharacter)
        {
            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        if (currentSelectedCharacter != null)
        {
            RefreshCharacterInfo();
            RefreshButtonStates();
        }
    }

    private void RefreshCharacterInfo()
    {
        if (currentSelectedCharacter != null)
        {
            UpdateCharacterInfo(currentSelectedCharacter);
        }
    }

    private void RefreshButtonStates()
    {
        if (currentSelectedCharacter == null) return;

        CharacterActions characterActions = currentSelectedCharacter.GetComponent<CharacterActions>();
        if (characterActions == null) return;

        // Update action button states
        for (int i = 0; i < actionButtons.Count && i < characterActions.character.charClass.availableActions.Count; i++)
        {
            ActionData action = characterActions.character.charClass.availableActions[i];
            Button button = actionButtons[i];

            if (button != null && action != null)
            {
                button.interactable = characterActions.CanUseAction(action);

                // Update ActionButton component if it exists
                ActionButton actionButton = button.GetComponent<ActionButton>();
                if (actionButton != null)
                {
                    actionButton.SetInteractable(characterActions.CanUseAction(action));
                }
            }
        }

        // Update item button states
        for (int i = 0; i < itemButtons.Count && i < characterActions.character.charClass.inventory.Count; i++)
        {
            ItemData item = characterActions.character.charClass.inventory[i];
            Button button = itemButtons[i];

            if (button != null && item != null)
            {
                button.interactable = characterActions.CanUseItem(item);
            }
        }
    }

    private void SetupModeButtons()
    {
        try
        {
            if (moveButton != null)
                moveButton.onClick.AddListener(() => SafeSetActionMode(ActionModes.Move));

            if (actionsButton != null)
                actionsButton.onClick.AddListener(() => SafeSetActionMode(ActionModes.Actions));

            if (itemsButton != null)
                itemsButton.onClick.AddListener(() => SafeSetActionMode(ActionModes.Item));

            if (specialButton != null)
                specialButton.onClick.AddListener(() => SafeSetActionMode(ActionModes.Special));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error setting up mode buttons: {e.Message}");
        }
    }

    private void SafeSetActionMode(ActionModes mode)
    {
        try
        {
            if (mouseHandler != null)
            {
                mouseHandler.SetActionMode(mode);
                UpdateUI();
            }
            else
            {
                Debug.LogWarning("MouseHandler is null when trying to set action mode");
                mouseHandler = MouseHandler.instance;
                if (mouseHandler != null)
                {
                    mouseHandler.SetActionMode(mode);
                    UpdateUI();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error setting action mode to {mode}: {e.Message}");
        }
    }

    private void OnPlayerSelected(Char selectedCharacter)
    {
        try
        {
            if (selectedCharacter == null)
            {
                Debug.LogWarning("Selected character is null");
                return;
            }

            currentSelectedCharacter = selectedCharacter;
            UpdateCharacterInfo(selectedCharacter);
            UpdateActionButtons(selectedCharacter);
            UpdateItemButtons(selectedCharacter);
            ShowModeButtons();
            UpdateUI();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error handling player selection: {e.Message}");
        }
    }

    private void OnSelectionCancelled()
    {
        try
        {
            currentSelectedCharacter = null;
            HideAllPanels();
            HideModeButtons();
            ClearCharacterInfo();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error handling selection cancellation: {e.Message}");
        }
    }

    private void ShowModeButtons()
    {
        try
        {
            if (moveButton != null)
                moveButton.gameObject.SetActive(true);
            if (actionsButton != null)
                actionsButton.gameObject.SetActive(true);
            if (itemsButton != null)
                itemsButton.gameObject.SetActive(true);
            if (specialButton != null)
                specialButton.gameObject.SetActive(true);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error showing mode buttons: {e.Message}");
        }
    }

    private void HideModeButtons()
    {
        try
        {
            if (moveButton != null)
                moveButton.gameObject.SetActive(false);
            if (actionsButton != null)
                actionsButton.gameObject.SetActive(false);
            if (itemsButton != null)
                itemsButton.gameObject.SetActive(false);
            if (specialButton != null)
                specialButton.gameObject.SetActive(false);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error hiding mode buttons: {e.Message}");
        }
    }

    private void UpdateCharacterInfo(Char character)
    {
        try
        {
            if (character == null) return;

            if (characterNameText != null)
                characterNameText.text = character.name ?? "Unknown";

            if (healthText != null)
                healthText.text = $"HP: {character.Health:F0}/{character.charClass.MaxHp:F0}";

            // Always update action points display
            if (actionPointsText != null)
            {
                actionPointsText.text = $"AP: {character.CurrentActionPoints}/{character.charClass.maxActionPoints}";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error updating character info: {e.Message}");
        }
    }

    private void ClearCharacterInfo()
    {
        try
        {
            if (characterNameText != null)
                characterNameText.text = "";

            if (healthText != null)
                healthText.text = "";

            if (actionPointsText != null)
                actionPointsText.text = "";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error clearing character info: {e.Message}");
        }
    }

    private void UpdateActionButtons(Char character)
    {
        try
        {
            ClearActionButtons();

            if (character == null) return;

            CharacterActions characterActions = character.GetComponent<CharacterActions>();
            if (characterActions == null || characterActions.character.charClass.availableActions == null) return;

            foreach (ActionData action in characterActions.character.charClass.availableActions)
            {
                if (action != null)
                {
                    CreateActionButton(action, characterActions);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error updating action buttons: {e.Message}");
        }
    }

    private void CreateActionButton(ActionData action, CharacterActions characterActions)
    {
        try
        {
            if (actionButtonPrefab == null || actionButtonParent == null || action == null || characterActions == null)
            {
                Debug.LogWarning("Missing references for creating action button");
                return;
            }

            GameObject buttonObj = Instantiate(actionButtonPrefab, actionButtonParent);
            if (buttonObj == null) return;

            ActionButton actionButton = buttonObj.GetComponent<ActionButton>();
            if (actionButton != null)
            {
                actionButton.Setup(action, SafeSelectAction);
                actionButton.SetInteractable(characterActions.CanUseAction(action));

                Button button = actionButton.button;
                if (button != null)
                {
                    actionButtons.Add(button);
                }
            }
            else
            {
                Button button = buttonObj.GetComponent<Button>();
                if (button == null)
                {
                    Debug.LogWarning("Action button prefab doesn't have a Button component");
                    Destroy(buttonObj);
                    return;
                }

                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = $"{action.actionName ?? "Unknown"}\nAP: {action.actionPointCost}";
                }

                button.onClick.AddListener(() => SafeSelectAction(action));
                button.interactable = characterActions.CanUseAction(action);
                actionButtons.Add(button);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating action button: {e.Message}");
        }
    }

    private void UpdateItemButtons(Char character)
    {
        try
        {
            ClearItemButtons();

            if (character == null) return;

            CharacterActions characterActions = character.GetComponent<CharacterActions>();
            if (characterActions == null || characterActions.character.charClass.inventory == null) return;

            foreach (ItemData item in characterActions.character.charClass.inventory)
            {
                if (item != null)
                {
                    CreateItemButton(item, characterActions);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error updating item buttons: {e.Message}");
        }
    }

    private void CreateItemButton(ItemData item, CharacterActions characterActions)
    {
        try
        {
            if (itemButtonPrefab == null || itemButtonParent == null || item == null || characterActions == null)
            {
                Debug.LogWarning("Missing references for creating item button");
                return;
            }

            GameObject buttonObj = Instantiate(itemButtonPrefab, itemButtonParent);
            if (buttonObj == null) return;

            Button button = buttonObj.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogWarning("Item button prefab doesn't have a Button component");
                Destroy(buttonObj);
                return;
            }

            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                string itemName = item.itemName ?? "Unknown Item";
                int actionCost = item.actionEffect?.actionPointCost ?? 0;
                buttonText.text = $"{itemName}\nAP: {actionCost}";
            }

            button.onClick.AddListener(() => SafeSelectItem(item));
            button.interactable = characterActions.CanUseItem(item);
            itemButtons.Add(button);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating item button: {e.Message}");
        }
    }

    private void SafeSelectAction(ActionData action)
    {
        try
        {
            if (action == null)
            {
                Debug.LogWarning("Trying to select null action");
                return;
            }

            if (mouseHandler != null)
            {
                mouseHandler.SelectAction(action);
                mouseHandler.SetActionMode(ActionModes.Actions);
                Debug.Log($"Selected action: {action.actionName}");
                UpdateUI();
            }
            else
            {
                Debug.LogWarning("MouseHandler is null when trying to select action");
                mouseHandler = MouseHandler.instance;
                if (mouseHandler != null)
                {
                    mouseHandler.SelectAction(action);
                    mouseHandler.SetActionMode(ActionModes.Actions);
                    Debug.Log($"Selected action: {action.actionName}");
                    UpdateUI();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error selecting action: {e.Message}");
        }
    }

    private void SafeSelectItem(ItemData item)
    {
        try
        {
            if (item == null)
            {
                Debug.LogWarning("Trying to select null item");
                return;
            }

            if (mouseHandler != null)
            {
                mouseHandler.SelectItem(item);
                mouseHandler.SetActionMode(ActionModes.Item);
                Debug.Log($"Selected item: {item.itemName}");
                RefreshUI(); // Immediately refresh UI
            }
            else
            {
                Debug.LogWarning("MouseHandler is null when trying to select item");
                mouseHandler = MouseHandler.instance;
                if (mouseHandler != null)
                {
                    mouseHandler.SelectItem(item);
                    mouseHandler.SetActionMode(ActionModes.Item);
                    Debug.Log($"Selected item: {item.itemName}");
                    RefreshUI();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error selecting item: {e.Message}");
        }
    }

    private void UpdateUI()
    {
        try
        {
            if (mouseHandler == null)
            {
                mouseHandler = MouseHandler.instance;
                if (mouseHandler == null)
                {
                    Debug.LogWarning("MouseHandler still null in UpdateUI");
                    return;
                }
            }

            HideAllPanels();

            if (mouseHandler.GetSelectedPlayer() != null && mouseHandler.currentActionType != ActionModes.None)
            {
                switch (mouseHandler.currentActionType)
                {
                    case ActionModes.Actions:
                        if (actionPanel != null)
                            actionPanel.SetActive(true);
                        break;

                    case ActionModes.Item:
                        if (itemPanel != null)
                            itemPanel.SetActive(true);
                        break;
                }
            }

            UpdateModeButtonStates();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error updating UI: {e.Message}");
        }
    }

    private void UpdateModeButtonStates()
    {
        try
        {
            if (mouseHandler == null) return;

            ResetButtonState(moveButton);
            ResetButtonState(actionsButton);
            ResetButtonState(itemsButton);
            ResetButtonState(specialButton);

            switch (mouseHandler.currentActionType)
            {
                case ActionModes.Move:
                    HighlightButton(moveButton);
                    break;
                case ActionModes.Actions:
                    HighlightButton(actionsButton);
                    break;
                case ActionModes.Item:
                    HighlightButton(itemsButton);
                    break;
                case ActionModes.Special:
                    HighlightButton(specialButton);
                    break;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error updating mode button states: {e.Message}");
        }
    }

    private void ResetButtonState(Button button)
    {
        try
        {
            if (button == null) return;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            button.colors = colors;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error resetting button state: {e.Message}");
        }
    }

    private void HighlightButton(Button button)
    {
        try
        {
            if (button == null) return;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.yellow;
            button.colors = colors;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error highlighting button: {e.Message}");
        }
    }

    private void HideAllPanels()
    {
        try
        {
            if (actionPanel != null)
                actionPanel.SetActive(false);

            if (itemPanel != null)
                itemPanel.SetActive(false);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error hiding panels: {e.Message}");
        }
    }

    private void ClearActionButtons()
    {
        try
        {
            foreach (Button button in actionButtons)
            {
                if (button != null && button.gameObject != null)
                    Destroy(button.gameObject);
            }
            actionButtons.Clear();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error clearing action buttons: {e.Message}");
        }
    }

    private void ClearItemButtons()
    {
        try
        {
            foreach (Button button in itemButtons)
            {
                if (button != null && button.gameObject != null)
                    Destroy(button.gameObject);
            }
            itemButtons.Clear();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error clearing item buttons: {e.Message}");
        }
    }

    private void OnDestroy()
    {
        try
        {
            if (mouseHandler != null)
            {
                mouseHandler.OnPlayerSelected -= OnPlayerSelected;
                mouseHandler.OnSelectionCancelled -= OnSelectionCancelled;
                mouseHandler.OnActionUsed -= OnActionUsed;
                mouseHandler.OnPlayerMoved -= OnPlayerMoved;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during ActionUIManager destruction: {e.Message}");
        }
    }
}
