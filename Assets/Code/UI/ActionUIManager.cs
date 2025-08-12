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

    private void Start()
    {
        mouseHandler = MouseHandler.instance;

        if (mouseHandler != null)
        {
            mouseHandler.OnPlayerSelected += OnPlayerSelected;
            mouseHandler.OnSelectionCancelled += OnSelectionCancelled;
        }

        SetupModeButtons();
        HideAllPanels();
    }

    private void SetupModeButtons()
    {
        if (moveButton != null)
            moveButton.onClick.AddListener(() => SetActionMode(ActionModes.Move));

        if (actionsButton != null)
            actionsButton.onClick.AddListener(() => SetActionMode(ActionModes.Actions));

        if (itemsButton != null)
            itemsButton.onClick.AddListener(() => SetActionMode(ActionModes.Item));

        if (specialButton != null)
            specialButton.onClick.AddListener(() => SetActionMode(ActionModes.Special));
    }

    private void SetActionMode(ActionModes mode)
    {
        if (mouseHandler != null)
        {
            mouseHandler.SetActionMode(mode);
        }

        UpdateUI();
    }

    private void OnPlayerSelected(Char selectedCharacter)
    {
        UpdateCharacterInfo(selectedCharacter);
        UpdateActionButtons(selectedCharacter);
        UpdateItemButtons(selectedCharacter);
        UpdateUI();
    }

    private void OnSelectionCancelled()
    {
        HideAllPanels();
        ClearCharacterInfo();
    }

    private void UpdateCharacterInfo(Char character)
    {
        if (characterNameText != null)
            characterNameText.text = character.name;

        if (healthText != null)
            healthText.text = $"HP: {character.Health:F0}/{character.MaxHp:F0}";

        CharacterActions characterActions = character.GetComponent<CharacterActions>();
        if (characterActions != null && actionPointsText != null)
        {
            actionPointsText.text = $"AP: {characterActions.currentActionPoints}/{characterActions.maxActionPoints}";
        }
    }

    private void ClearCharacterInfo()
    {
        if (characterNameText != null)
            characterNameText.text = "";

        if (healthText != null)
            healthText.text = "";

        if (actionPointsText != null)
            actionPointsText.text = "";
    }

    private void UpdateActionButtons(Char character)
    {
        ClearActionButtons();

        CharacterActions characterActions = character.GetComponent<CharacterActions>();
        if (characterActions == null) return;

        foreach (ActionData action in characterActions.availableActions)
        {
            CreateActionButton(action, characterActions);
        }
    }

    private void CreateActionButton(ActionData action, CharacterActions characterActions)
    {
        if (actionButtonPrefab == null || actionButtonParent == null) return;

        GameObject buttonObj = Instantiate(actionButtonPrefab, actionButtonParent);
        Button button = buttonObj.GetComponent<Button>();

        // Setup button text
        TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = $"{action.actionName}\nAP: {action.actionPointCost}";
        }

        // Setup button functionality
        button.onClick.AddListener(() => SelectAction(action));

        // Disable button if can't use action
        button.interactable = characterActions.CanUseAction(action);

        actionButtons.Add(button);
    }

    private void UpdateItemButtons(Char character)
    {
        ClearItemButtons();

        CharacterActions characterActions = character.GetComponent<CharacterActions>();
        if (characterActions == null) return;

        foreach (ItemData item in characterActions.inventory)
        {
            CreateItemButton(item, characterActions);
        }
    }

    private void CreateItemButton(ItemData item, CharacterActions characterActions)
    {
        if (itemButtonPrefab == null || itemButtonParent == null) return;

        GameObject buttonObj = Instantiate(itemButtonPrefab, itemButtonParent);
        Button button = buttonObj.GetComponent<Button>();

        // Setup button text
        TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = $"{item.itemName}\nAP: {item.actionEffect.actionPointCost}";
        }

        // Setup button functionality
        button.onClick.AddListener(() => SelectItem(item));

        // Disable button if can't use item
        button.interactable = characterActions.CanUseItem(item);

        itemButtons.Add(button);
    }

    private void SelectAction(ActionData action)
    {
        if (mouseHandler != null)
        {
            mouseHandler.SelectAction(action);
            mouseHandler.SetActionMode(ActionModes.Actions);
        }

        Debug.Log($"Selected action: {action.actionName}");
    }

    private void SelectItem(ItemData item)
    {
        if (mouseHandler != null)
        {
            mouseHandler.SelectItem(item);
            mouseHandler.SetActionMode(ActionModes.Item);
        }

        Debug.Log($"Selected item: {item.itemName}");
    }

    private void UpdateUI()
    {
        if (mouseHandler == null) return;

        // Hide all panels first
        HideAllPanels();

        // Show appropriate panel based on current mode
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

        // Update button states
        UpdateModeButtonStates();
    }

    private void UpdateModeButtonStates()
    {
        if (mouseHandler == null) return;

        // Reset all button colors/states
        ResetButtonState(moveButton);
        ResetButtonState(actionsButton);
        ResetButtonState(itemsButton);
        ResetButtonState(specialButton);

        // Highlight current mode button
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

    private void ResetButtonState(Button button)
    {
        if (button == null) return;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        button.colors = colors;
    }

    private void HighlightButton(Button button)
    {
        if (button == null) return;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.yellow;
        button.colors = colors;
    }

    private void HideAllPanels()
    {
        if (actionPanel != null)
            actionPanel.SetActive(false);

        if (itemPanel != null)
            itemPanel.SetActive(false);
    }

    private void ClearActionButtons()
    {
        foreach (Button button in actionButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }
        actionButtons.Clear();
    }

    private void ClearItemButtons()
    {
        foreach (Button button in itemButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }
        itemButtons.Clear();
    }

    private void OnDestroy()
    {
        if (mouseHandler != null)
        {
            mouseHandler.OnPlayerSelected -= OnPlayerSelected;
            mouseHandler.OnSelectionCancelled -= OnSelectionCancelled;
        }
    }
}
