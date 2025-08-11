using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionUI : MonoBehaviour
{
    public static ActionUI instance;

    [Header("UI References")]
    [SerializeField]private GameObject actionPanelPrefab;
    [SerializeField] private GameObject actionPanel;
    [SerializeField] private Transform actionButtonParent;
    [SerializeField] private GameObject actionButtonPrefab;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI actionPointsText;
    [SerializeField] private Button endTurnButton;

    [Header("Settings")]
    [SerializeField] private float panelAnimationSpeed = 0.3f;

    // Current state
    private Char currentCharacter;
    private BaseAction selectedAction;
    private List<ActionButton> actionButtons = new List<ActionButton>();
    private List<HexTile> validTargets = new List<HexTile>();
    private bool isTargeting = false;

    // Events
    public System.Action<BaseAction> OnActionSelected;
    public System.Action OnActionCancelled;
    public System.Action<BaseAction, HexTile> OnActionExecuted;
    public bool IsTargeting => isTargeting;
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

    private void Start()
    {
        if (actionPanel != null)
            actionPanel.SetActive(false);

        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(EndTurn);

        // Subscribe to mouse handler events
        if (MouseHandler.instance != null)
        {
            MouseHandler.instance.OnPlayerSelected += ShowActionsForCharacter;
            MouseHandler.instance.OnSelectionCancelled += HideActionPanel;
        }
    }

    public void ShowActionsForCharacter(Char character)
    {
        if (character == null) return;

        currentCharacter = character;
        UpdateCharacterInfo();
        CreateActionButtons();
        ShowPanel();
    }

    private void UpdateCharacterInfo()
    {
        if (characterNameText != null && currentCharacter != null)
        {
            characterNameText.text = currentCharacter.charClass.characterName;
        }

        if (actionPointsText != null && currentCharacter != null)
        {
            // You'll need to add action points to the Char class
            actionPointsText.text = $"AP: {currentCharacter.charClass.remainingMoves}"; // Temporary using moves
        }
    }

    private void CreateActionButtons()
    {
        ClearActionButtons();

        if (currentCharacter == null || currentCharacter.charClass.availableActions == null) return;

        foreach (BaseAction action in currentCharacter.charClass.availableActions)
        {
            if (action == null) continue;

            GameObject buttonObj = Instantiate(actionButtonPrefab, actionButtonParent);
            ActionButton actionButton = buttonObj.GetComponent<ActionButton>();

            if (actionButton == null)
                actionButton = buttonObj.AddComponent<ActionButton>();

            actionButton.Initialize(action, this);
            actionButtons.Add(actionButton);
        }
    }

    private void ClearActionButtons()
    {
        foreach (ActionButton button in actionButtons)
        {
            if (button != null && button.gameObject != null)
                Destroy(button.gameObject);
        }
        actionButtons.Clear();
    }

    public void SelectAction(BaseAction action)
    {
        selectedAction = action;

        if (action.requiresTarget)
        {
            StartTargeting();
        }
        else
        {
            ExecuteAction(null);
        }

        OnActionSelected?.Invoke(action);
    }

    private void StartTargeting()
    {
        isTargeting = true;
        validTargets = selectedAction.GetValidTargets(currentCharacter);

        // Highlight valid targets
        foreach (HexTile tile in validTargets)
        {
            if (tile != null)
            {
                tile.OnInteract += () => tile.SetMovementRange(false,true);
                tile.Interact();
            }
        }

        Debug.Log($"Select target for {selectedAction.actionName}. Valid targets: {validTargets.Count}");
    }

    private void StopTargeting()
    {
        isTargeting = false;

        // Clear target highlights
        foreach (HexTile tile in validTargets)
        {
            if (tile != null)
            {
                tile.OnInteract += () => tile.SetMovementRange(false, false);
                tile.Interact();
            }
        }

        validTargets.Clear();
        selectedAction = null;
    }

    public void OnTileClicked(HexTile tile)
    {
        if (!isTargeting || selectedAction == null) return;

        if (validTargets.Contains(tile))
        {
            ExecuteAction(tile);
        }
        else
        {
            Debug.Log("Invalid target!");
            CancelAction();
        }
    }

    private void ExecuteAction(HexTile targetTile)
    {
        if (selectedAction == null || currentCharacter == null) return;

        if (selectedAction.CanExecute(currentCharacter, targetTile))
        {
            selectedAction.Execute(currentCharacter, targetTile);

            // Deduct action points (you'll need to implement this in Char class)
            // currentCharacter.SpendActionPoints(selectedAction.actionPointCost);

            OnActionExecuted?.Invoke(selectedAction, targetTile);

            StopTargeting();
            UpdateCharacterInfo();

            // Check if character can still act
            if (!currentCharacter.CanMove()) // Temporary check
            {
                HideActionPanel();
            }
        }
        else
        {
            Debug.Log("Cannot execute action!");
            CancelAction();
        }
    }

    public void CancelAction()
    {
        StopTargeting();
        OnActionCancelled?.Invoke();
    }

    private void EndTurn()
    {
        if (currentCharacter != null)
        {
            currentCharacter.EndTurn();
        }
        HideActionPanel();
    }

    private void ShowPanel()
    {
        if (actionPanel != null)
        {
            actionPanel.SetActive(true);
            StartCoroutine(AnimatePanel(true));
        }
    }

    public void HideActionPanel()
    {
        StopTargeting();
        StartCoroutine(AnimatePanel(false));
    }

    private IEnumerator AnimatePanel(bool show)
    {
        if (actionPanel == null) yield break;

        CanvasGroup canvasGroup = actionPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = actionPanel.AddComponent<CanvasGroup>();

        float startAlpha = show ? 0f : 1f;
        float endAlpha = show ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < panelAnimationSpeed)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / panelAnimationSpeed;

            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);

            // Scale animation
            float scale = show ? Mathf.Lerp(0.8f, 1f, t) : Mathf.Lerp(1f, 0.8f, t);
            actionPanel.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        canvasGroup.alpha = endAlpha;
        actionPanel.transform.localScale = Vector3.one;

        if (!show)
            actionPanel.SetActive(false);
    }

    private void Update()
    {
        // Handle ESC key to cancel targeting
        if (isTargeting && Input.GetKeyDown(KeyCode.Escape))
        {
            CancelAction();
        }
    }

    private void OnDestroy()
    {
        if (MouseHandler.instance != null)
        {
            MouseHandler.instance.OnPlayerSelected -= ShowActionsForCharacter;
            MouseHandler.instance.OnSelectionCancelled -= HideActionPanel;
        }
    }

    public void OnActionButtonRemoved(ActionButton actionButton)
    {
        if (actionButtons.Contains(actionButton))
        {
            actionButtons.Remove(actionButton);
            Destroy(actionButton.gameObject);
        }
    }
}
