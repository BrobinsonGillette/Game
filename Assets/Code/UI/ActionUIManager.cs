using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionUIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject actionPanel;
    public Button moveButton;
    public Button actionsButton;



    [Header("Action UI")]
    public Transform actionButtonParent;
    public GameObject actionButtonPrefab;


    [Header("Character Info")]
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI actionPointsText;
    public TextMeshProUGUI healthText;

    private MouseHandler mouseHandler;
    private List<Button> actionButtons = new List<Button>();
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

   
    }

    private void SetupModeButtons()
    {
        try
        {
            if (moveButton != null)
                moveButton.onClick.AddListener(() => SafeSetActionMode(ActionModes.Move));

            if (actionsButton != null)
                actionsButton.onClick.AddListener(() => SafeSetActionMode(ActionModes.Actions));

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
                    UpdateUI();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error selecting action: {e.Message}");
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
            switch (mouseHandler.currentActionType)
            {
                case ActionModes.Move:
                    HighlightButton(moveButton);
                    break;
                case ActionModes.Actions:
                    HighlightButton(actionsButton);
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
