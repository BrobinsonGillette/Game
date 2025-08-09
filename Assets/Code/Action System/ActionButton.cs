using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ActionButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Components")]
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI actionNameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI cooldownText;
    [SerializeField] private Image backgroundImage;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Color disabledColor = Color.gray;
    [SerializeField] private Color selectedColor = Color.green;

    [Header("Dynamic Button Settings")]
    [SerializeField] private bool isDynamicButton = false;
    [SerializeField] private Button removeButton;
    [SerializeField] private GameObject removeButtonPrefab;

    private BaseAction action;
    private ActionUI actionUI;
    private bool isAvailable = true;
    private bool isSelected = false;

    // Static reference for managing dynamic buttons
    private static List<ActionButton> dynamicButtons = new List<ActionButton>();

    public void Initialize(BaseAction actionData, ActionUI ui, bool isDynamic = false)
    {
        action = actionData;
        actionUI = ui;
        isDynamicButton = isDynamic;

        SetupComponents();
        SetupDynamicFeatures();
        UpdateVisuals();

        if (button != null)
            button.onClick.AddListener(OnButtonClick);

        // Add to dynamic buttons list if it's a dynamic button
        if (isDynamicButton && !dynamicButtons.Contains(this))
            dynamicButtons.Add(this);
    }

    private void SetupComponents()
    {
        if (button == null)
            button = GetComponent<Button>();
        if (actionNameText == null)
            actionNameText = transform.Find("ActionName")?.GetComponent<TextMeshProUGUI>();
        if (costText == null)
            costText = transform.Find("Cost")?.GetComponent<TextMeshProUGUI>();
        if (cooldownText == null)
            cooldownText = transform.Find("Cooldown")?.GetComponent<TextMeshProUGUI>();
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
    }

    private void SetupDynamicFeatures()
    {
        if (isDynamicButton)
        {
            // Create remove button if it doesn't exist
            if (removeButton == null && removeButtonPrefab != null)
            {
                GameObject removeButtonObj = Instantiate(removeButtonPrefab, transform);
                removeButton = removeButtonObj.GetComponent<Button>();
            }

            // If no prefab provided, create a simple remove button
            if (removeButton == null)
            {
                CreateDefaultRemoveButton();
            }

            if (removeButton != null)
            {
                removeButton.onClick.AddListener(RemoveThisButton);
                // Hide remove button by default, show on hover
                removeButton.gameObject.SetActive(false);
            }
        }
    }

    private void CreateDefaultRemoveButton()
    {
        // Create a simple X button
        GameObject removeButtonObj = new GameObject("RemoveButton");
        removeButtonObj.transform.SetParent(transform);

        RectTransform rectTransform = removeButtonObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(20, 20);
        rectTransform.anchoredPosition = new Vector2(10, 10);
        rectTransform.anchorMin = new Vector2(1, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(1, 1);

        Image buttonImage = removeButtonObj.AddComponent<Image>();
        buttonImage.color = Color.red;

        removeButton = removeButtonObj.AddComponent<Button>();

        // Add X text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(removeButtonObj.transform);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.sizeDelta = Vector2.zero;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI removeText = textObj.AddComponent<TextMeshProUGUI>();
        removeText.text = "×";
        removeText.fontSize = 14;
        removeText.color = Color.white;
        removeText.alignment = TextAlignmentOptions.Center;
    }

    private void UpdateVisuals()
    {
        if (action == null) return;

        // Set action name
        if (actionNameText != null)
            actionNameText.text = action.actionName;

        // Set cost
        if (costText != null)
            costText.text = action.actionPointCost.ToString();

        // Set cooldown (you'll need to implement cooldown tracking)
        if (cooldownText != null)
        {
            // cooldownText.text = action.currentCooldown > 0 ? action.currentCooldown.ToString() : "";
            cooldownText.text = ""; // Placeholder
        }

        // Set availability
        UpdateAvailability();
    }

    private void UpdateAvailability()
    {
        // Check if action can be used (you'll need to implement this logic)
        isAvailable = true; // Placeholder

        if (button != null)
            button.interactable = isAvailable;

        UpdateButtonColor();
    }

    private void UpdateButtonColor()
    {
        if (backgroundImage != null)
        {
            Color targetColor = normalColor;

            if (!isAvailable)
                targetColor = disabledColor;
            else if (isSelected)
                targetColor = selectedColor;

            backgroundImage.color = targetColor;
        }
    }

    private void OnButtonClick()
    {
        if (action != null && actionUI != null && isAvailable)
        {
            actionUI.SelectAction(action);

            // Update selection state
            SetSelected(true);

            // Visual feedback
            StartCoroutine(ClickAnimation());
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateButtonColor();
    }

    private IEnumerator ClickAnimation()
    {
        Vector3 originalScale = transform.localScale;

        // Scale down
        float duration = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(originalScale, originalScale * 0.9f, t);
            yield return null;
        }

        // Scale back up
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(originalScale * 0.9f, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (backgroundImage != null && isAvailable && !isSelected)
            backgroundImage.color = hoverColor;

        // Show remove button for dynamic buttons
        if (isDynamicButton && removeButton != null)
            removeButton.gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (backgroundImage != null && isAvailable && !isSelected)
            backgroundImage.color = normalColor;

        // Hide remove button for dynamic buttons
        if (isDynamicButton && removeButton != null)
            removeButton.gameObject.SetActive(false);
    }

    // Dynamic button management methods
    public void RemoveThisButton()
    {
        if (isDynamicButton)
        {
            dynamicButtons.Remove(this);

            // Notify ActionUI if needed
            if (actionUI != null)
                actionUI.OnActionButtonRemoved(this);

            Destroy(gameObject);
        }
    }

    public static void AddDynamicButton(BaseAction actionData, ActionUI actionUI, Transform parent, GameObject buttonPrefab)
    {
        if (buttonPrefab != null && parent != null)
        {
            GameObject newButtonObj = Instantiate(buttonPrefab, parent);
            ActionButton newButton = newButtonObj.GetComponent<ActionButton>();

            if (newButton != null)
            {
                newButton.Initialize(actionData, actionUI, true);
            }
        }
    }

    public static void RemoveAllDynamicButtons()
    {
        for (int i = dynamicButtons.Count - 1; i >= 0; i--)
        {
            if (dynamicButtons[i] != null)
                dynamicButtons[i].RemoveThisButton();
        }
        dynamicButtons.Clear();
    }

    public static int GetDynamicButtonCount()
    {
        return dynamicButtons.Count;
    }

    public static List<ActionButton> GetDynamicButtons()
    {
        return new List<ActionButton>(dynamicButtons);
    }

    // Property accessors
    public BaseAction Action => action;
    public bool IsDynamicButton => isDynamicButton;
    public bool IsAvailable => isAvailable;
    public bool IsSelected => isSelected;

    private void OnDestroy()
    {
        // Clean up from static list
        if (dynamicButtons.Contains(this))
            dynamicButtons.Remove(this);
    }
}
