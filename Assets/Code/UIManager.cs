using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[System.Serializable]
public class StatUI
{
    [Header("Stat Info")]
    public PlayerStats.StatType statType;

    [Header("UI Elements")]
    public TextMeshProUGUI statValueText;
    public Button increaseButton;
    public Button decreaseButton;

    public void UpdateDisplay(int value, bool canUpgrade)
    {
        if (statValueText != null)
        {
            statValueText.text = value.ToString();
        }

        if (increaseButton != null)
        {
            increaseButton.interactable = canUpgrade;
        }
    }
}

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }


    private GameObject statsPanel;
    private GameObject StatspointsPanel;
    private GameObject healthBarPanel;
    private GameObject expBarPanel;


    private Slider healthSlider;
    private TextMeshProUGUI healthText;


    private Slider expSlider;
    private TextMeshProUGUI[] expText;
    private TextMeshProUGUI[] levelText;


   [SerializeField]  private StatUI[] statUi;
    private TextMeshProUGUI availablePointsText;
    private Button levelUpButton;
    private Button closeStatsButton;


    [SerializeField] private StatUI[] levelUpStatUi;
    private TextMeshProUGUI levelUpText;
    private GameObject levelUpNotification;

    private TextMeshProUGUI deathCountText;
    private GameObject deathCountPanel;

    private PlayerStats playerStats;
    private Coroutine levelUpCoroutine;
    private Canvas mainCanvas;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        CreateCompleteUI();
        InitializeUI();
        SetupEventListeners();
        SetupButtonListeners();
        Debug.Log($"Created {statUi?.Length} regular stat UIs");
        Debug.Log($"Created {levelUpStatUi?.Length} level up stat UIs");

        // Check if buttons exist
        if (levelUpStatUi != null)
        {
            for (int i = 0; i < levelUpStatUi.Length; i++)
            {
                Debug.Log($"Level up stat {i} ({levelUpStatUi[i]?.statType}): " +
                         $"Increase button = {(levelUpStatUi[i]?.increaseButton != null ? "OK" : "NULL")}, " +
                         $"Decrease button = {(levelUpStatUi[i]?.decreaseButton != null ? "OK" : "NULL")}");
            }
        }
    }

    private void CreateCompleteUI()
    {
        // Find or create main canvas
        mainCanvas = FindObjectOfType<Canvas>();
        if (mainCanvas == null)
        {
            CreateMainCanvas();
        }

        // Create all UI panels and elements
        CreateHealthUI();
        CreateExpUI();
        CreateStatsPanel();
        CreateLevelUpNotification();
        CreateDeathUI();
    }

    private void CreateMainCanvas()
    {
        GameObject canvasObj = new GameObject("Main Canvas");
        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainCanvas.sortingOrder = 0;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Create EventSystem if it doesn't exist
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    private void CreateHealthUI()
    {
        // Create health bar panel
        healthBarPanel = CreatePanel("Health Bar Panel", mainCanvas.transform);
        SetRectTransform(healthBarPanel.GetComponent<RectTransform>(),
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(160.9f, -28.7f), new Vector2(320, 60));

        // Create health slider
        GameObject healthSliderObj = CreateSlider("Health Slider", healthBarPanel.transform);
        healthSlider = healthSliderObj.GetComponent<Slider>();
        SetRectTransform(healthSliderObj.GetComponent<RectTransform>(),
            Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -30));

        // Style health slider
        StyleSlider(healthSlider, new Color(0.8f, 0.2f, 0.2f), new Color(0.3f, 0.3f, 0.3f));

        // Create health text
        GameObject healthTextObj = CreateText("Health Text", healthBarPanel.transform, "100/100");
        healthText = healthTextObj.GetComponent<TextMeshProUGUI>();
        SetRectTransform(healthTextObj.GetComponent<RectTransform>(),
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 29.9f), new Vector2(0, 25));
        healthText.alignment = TextAlignmentOptions.Center;
        healthText.fontSize = 16;
    }

    private void CreateExpUI()
    {
        // Create exp bar panel
        expBarPanel = CreatePanel("Exp Bar Panel", mainCanvas.transform);
        SetRectTransform(expBarPanel.GetComponent<RectTransform>(),
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(160.9f, -90), new Vector2(320, 60));

        // Create exp slider
        GameObject expSliderObj = CreateSlider("Exp Slider", expBarPanel.transform);
        expSlider = expSliderObj.GetComponent<Slider>();
        SetRectTransform(expSliderObj.GetComponent<RectTransform>(),
            Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -30));

        // Style exp slider
        StyleSlider(expSlider, new Color(0.2f, 0.8f, 0.2f), new Color(0.3f, 0.3f, 0.3f));

        // Create exp text array
        expText = new TextMeshProUGUI[1];
        GameObject expTextObj = CreateText("Exp Text", expBarPanel.transform, "0/100 EXP");
        expText[0] = expTextObj.GetComponent<TextMeshProUGUI>();
        SetRectTransform(expTextObj.GetComponent<RectTransform>(),
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 29.7f), new Vector2(0, 25));
        expText[0].alignment = TextAlignmentOptions.Center;
        expText[0].fontSize = 16;

        // Create level text array
        levelText = new TextMeshProUGUI[1];
        GameObject levelTextObj = CreateText("Level Text", expBarPanel.transform, "Level 1");
        levelText[0] = levelTextObj.GetComponent<TextMeshProUGUI>();
        SetRectTransform(levelTextObj.GetComponent<RectTransform>(),
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, 25));
        levelText[0].alignment = TextAlignmentOptions.Center;
        levelText[0].fontSize = 18;
        levelText[0].fontStyle = FontStyles.Bold;
    }

    private void CreateStatsPanel()
    {
        // Create main stats panel (hidden by default)
        statsPanel = CreatePanel("Stats Panel", mainCanvas.transform);
        SetRectTransform(statsPanel.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 500));
        statsPanel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        statsPanel.SetActive(false);

        // Create points panel
        StatspointsPanel = CreatePanel("Points Panel", statsPanel.transform);
        SetRectTransform(StatspointsPanel.GetComponent<RectTransform>(),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        StatspointsPanel.GetComponent<Image>().color = Color.clear;

        // Create title
        CreateText("Stats Title", StatspointsPanel.transform, "CHARACTER STATS", 24, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRectTransform(StatspointsPanel.transform.GetChild(StatspointsPanel.transform.childCount - 1).GetComponent<RectTransform>(),
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -50), new Vector2(-20, 40));

        // Create available points text
        GameObject availablePointsObj = CreateText("Available Points", StatspointsPanel.transform, "Available Points: 0");
        availablePointsText = availablePointsObj.GetComponent<TextMeshProUGUI>();
        SetRectTransform(availablePointsObj.GetComponent<RectTransform>(),
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -90), new Vector2(-20, 30));
        availablePointsText.alignment = TextAlignmentOptions.Center;
        availablePointsText.fontSize = 18;

        // Create stat UIs for all stat types
        CreateStatUIs();

        // Create close button
        GameObject closeButtonObj = CreateButton("Close Stats", StatspointsPanel.transform, "Close");
        closeStatsButton = closeButtonObj.GetComponent<Button>();
        SetRectTransform(closeButtonObj.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 36.6f), new Vector2(100, 20.65f));

        // Create level up button (hidden by default)
        GameObject levelUpButtonObj = CreateButton("Level Up Button", StatspointsPanel.transform, "Level Up!");
        levelUpButton = levelUpButtonObj.GetComponent<Button>();
        SetRectTransform(levelUpButtonObj.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(120, 40));
        StyleButton(levelUpButton, new Color(0.2f, 0.8f, 0.2f), Color.white);
        levelUpButton.interactable= false;
        levelUpButton.gameObject.SetActive(false);
    }

    private void CreateStatUIs()
    {
        // Get all stat types
        var statTypes = System.Enum.GetValues(typeof(PlayerStats.StatType));
        statUi = new StatUI[statTypes.Length];

        float startY = -130f;
        float spacing = 60f;

        for (int i = 0; i < statTypes.Length; i++)
        {
            PlayerStats.StatType statType = (PlayerStats.StatType)statTypes.GetValue(i);
            statUi[i] = CreateStatUI(statType, StatspointsPanel.transform, new Vector2(0, startY - (i * spacing)));
        }
    }
    private StatUI CreateStatUI(PlayerStats.StatType statType, Transform parent, Vector2 position)
    {
        StatUI statUI = new StatUI();
        statUI.statType = statType;

        // Create stat container
        GameObject statContainer = new GameObject($"{statType} Stat");
        statContainer.transform.SetParent(parent, false);
        RectTransform containerRect = statContainer.AddComponent<RectTransform>();
        SetRectTransform(containerRect, new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(10, position.y), new Vector2(-20, 50));

        // Create stat label
        GameObject labelObj = CreateText($"{statType} Label", statContainer.transform, statType.ToString());
        SetRectTransform(labelObj.GetComponent<RectTransform>(),
            Vector2.zero, new Vector2(0.4f, 1), Vector2.zero, Vector2.zero);
        labelObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

        // Create stat value text
        GameObject valueObj = CreateText($"{statType} Value", statContainer.transform, "1");
        statUI.statValueText = valueObj.GetComponent<TextMeshProUGUI>();
        SetRectTransform(valueObj.GetComponent<RectTransform>(),
            new Vector2(0.4f, 0), new Vector2(0.6f, 1), Vector2.zero, Vector2.zero);
        statUI.statValueText.alignment = TextAlignmentOptions.Center;
        statUI.statValueText.fontSize = 18;
        statUI.statValueText.fontStyle = FontStyles.Bold;

        // ADD BUTTONS TO REGULAR STAT UI TOO (if you want them in the stats panel)
        // Create increase button
        GameObject increaseObj = CreateButton($"{statType} Increase", statContainer.transform, "+");
        statUI.increaseButton = increaseObj.GetComponent<Button>();
        SetRectTransform(increaseObj.GetComponent<RectTransform>(),
            new Vector2(0.7f, 0), new Vector2(0.85f, 1), Vector2.zero, Vector2.zero);
        StyleButton(statUI.increaseButton, new Color(0.2f, 0.8f, 0.2f), Color.white);

        // Create decrease button
        GameObject decreaseObj = CreateButton($"{statType} Decrease", statContainer.transform, "-");
        statUI.decreaseButton = decreaseObj.GetComponent<Button>();
        SetRectTransform(decreaseObj.GetComponent<RectTransform>(),
            new Vector2(0.85f, 0), new Vector2(1f, 1), Vector2.zero, Vector2.zero);
        StyleButton(statUI.decreaseButton, new Color(0.8f, 0.2f, 0.2f), Color.white);

        return statUI;
    }
 
    private void CreateLevelUpNotification()
    {
        // Create level up notification (hidden by default)
        levelUpNotification = CreatePanel("Level Up Notification", mainCanvas.transform);
        SetRectTransform(levelUpNotification.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), Vector2.zero, new Vector2(300, 100));
        levelUpNotification.GetComponent<Image>().color = new Color(1f, 0.8f, 0f, 0.9f);
        levelUpNotification.SetActive(false);

        // Create level up text
        GameObject levelUpTextObj = CreateText("Level Up Text", levelUpNotification.transform, "LEVEL UP!\nLevel 2");
        levelUpText = levelUpTextObj.GetComponent<TextMeshProUGUI>();
        SetRectTransform(levelUpTextObj.GetComponent<RectTransform>(),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        levelUpText.alignment = TextAlignmentOptions.Center;
        levelUpText.fontSize = 20;
        levelUpText.fontStyle = FontStyles.Bold;
        levelUpText.color = Color.black;
    }

    private void CreateDeathUI()
    {
        // Create death count text (positioned at bottom center)
        GameObject deathCountObj = CreateText("Death Count", mainCanvas.transform, "");
        deathCountText = deathCountObj.GetComponent<TextMeshProUGUI>();
        SetRectTransform(deathCountObj.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 100));
        deathCountText.alignment = TextAlignmentOptions.Center;
        deathCountText.fontSize = 24;
        deathCountText.fontStyle = FontStyles.Bold;
        deathCountText.color = Color.red;
        deathCountPanel = deathCountObj;
    }

    // Utility methods for creating UI elements
    private GameObject CreatePanel(string name, Transform parent)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        return panel;
    }

    private GameObject CreateText(string name, Transform parent, string text, float fontSize = 16, FontStyles fontStyle = FontStyles.Normal, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        TextMeshProUGUI textComp = textObj.AddComponent<TextMeshProUGUI>();

        textComp.text = text;
        textComp.fontSize = fontSize;
        textComp.fontStyle = fontStyle;
        textComp.alignment = alignment;
        textComp.color = Color.white;

        return textObj;
    }

    private GameObject CreateButton(string name, Transform parent, string buttonText)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        Image image = buttonObj.AddComponent<Image>();
        Button button = buttonObj.AddComponent<Button>();

        image.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        button.targetGraphic = image;

        // Create button text
        GameObject textObj = CreateText("Text", buttonObj.transform, buttonText, 16, FontStyles.Normal, TextAlignmentOptions.Center);
        SetRectTransform(textObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        textObj.GetComponent<TextMeshProUGUI>().color = Color.white;

        return buttonObj;
    }
 
    private GameObject CreateSlider(string name, Transform parent)
    {
        GameObject sliderObj = new GameObject(name);
        sliderObj.transform.SetParent(parent, false);

        RectTransform rect = sliderObj.AddComponent<RectTransform>();
        Slider slider = sliderObj.AddComponent<Slider>();

        // Create background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRect = background.AddComponent<RectTransform>();
        Image bgImage = background.AddComponent<Image>();
        SetRectTransform(bgRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        bgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        // Create fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        SetRectTransform(fillAreaRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Create fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        Image fillImage = fill.AddComponent<Image>();
        SetRectTransform(fillRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        fillImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);

        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;

        return sliderObj;
    }

    private void SetRectTransform(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private void StyleButton(Button button, Color normalColor, Color textColor)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = normalColor * 1.2f;
        colors.pressedColor = normalColor * 0.8f;
        colors.selectedColor = normalColor;
        button.colors = colors;

        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.color = textColor;
        }
    }

    private void StyleSlider(Slider slider, Color fillColor, Color backgroundColor)
    {
        Image fillImage = slider.fillRect?.GetComponent<Image>();
        if (fillImage != null)
        {
            fillImage.color = fillColor;
        }

        Image bgImage = slider.transform.Find("Background")?.GetComponent<Image>();
        if (bgImage != null)
        {
            bgImage.color = backgroundColor;
        }
    }

    // Rest of the original methods remain the same...
    private void InitializeUI()
    {
        // Get reference to PlayerStats
        playerStats = PlayerStats.instance;

        if (playerStats == null)
        {
            Debug.Log("PlayerStats instance not found! Make sure PlayerStats is initialized before UIManager.");
            return;
        }

        // Setup initial UI values
        UpdateHealthUI(playerStats.Health, playerStats.MaxHealth);
        UpdateExpUI(playerStats.CurrentExp, playerStats.MaxExp);
        UpdateStatsUI();
        UpdatePlayerInfoUI();
        PlayerMove.instance.inputSystem.Inventory.action.performed += OpenInventory;
    }

    private void OpenInventory(InputAction.CallbackContext context)
    {
        if (statsPanel != null)
        {
            if (statsPanel.activeSelf)
            {
                CloseStatsPanel();
            }
            else
            {
                OpenStatsPanel();
            }
        }
    }

    private void SetupEventListeners()
    {
        if (playerStats == null) return;

        // Subscribe to PlayerStats events
        playerStats.OnHealthChanged += UpdateHealthUI;
        playerStats.OnExpChanged += UpdateExpUI;
        playerStats.OnLevelUp += OnPlayerLevelUp;
        playerStats.OnStatChanged += OnStatPointsChanged;
        playerStats.OnDeath += OnPlayerDeath;
        playerStats.OnPlayerRespawn += OnPlayerRespawn;
    }

    private void SetupButtonListeners()
    {
      // Setup stat upgrade buttons for REGULAR stats UI
        if (statUi != null)
        {
            for (int i = 0; i < statUi.Length; i++)
            {
                if (statUi[i]?.increaseButton != null)
                {
                    int index = i; // Capture for closure
                    statUi[i].increaseButton.onClick.AddListener(() => IncreaseStat(statUi[index].statType));
                }

                if (statUi[i]?.decreaseButton != null)
                {
                    int index = i; // Capture for closure
                    statUi[i].decreaseButton.onClick.AddListener(() => DecreaseStat(statUi[index].statType));
                }
            }
        }

        // Setup stat upgrade buttons for level up UI
        if (levelUpStatUi != null)
        {
            for (int i = 0; i < levelUpStatUi.Length; i++)
            {
                if (levelUpStatUi[i]?.increaseButton != null)
                {
                    int index = i; // Capture for closure
                    levelUpStatUi[i].increaseButton.onClick.AddListener(() => IncreaseStat(levelUpStatUi[index].statType));
                }

                if (levelUpStatUi[i]?.decreaseButton != null)
                {
                    int index = i; // Capture for closure
                    levelUpStatUi[i].decreaseButton.onClick.AddListener(() => DecreaseStat(levelUpStatUi[index].statType));
                }
            }
        }

        if (closeStatsButton != null)
            closeStatsButton.onClick.AddListener(CloseStatsPanel);
    }

    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{currentHealth:F0}/{maxHealth:F0}";
        }
    }

    private void UpdateExpUI(float currentExp, float maxExp)
    {
        if (expSlider != null)
        {
            expSlider.maxValue = maxExp;
            expSlider.value = currentExp;
        }

        if (expText != null)
        {
            foreach (var expText in expText)
            {
                if (expText != null)
                {
                    expText.text = $"{currentExp:F0}/{maxExp:F0} EXP";
                }
            }
        }

        if (levelText != null && playerStats != null)
        {
            foreach (var levelText in levelText)
            {
                if (levelText != null)
                {
                    levelText.text = $"Level {playerStats.Level}";
                }
            }
        }
    }

    private void OnPlayerLevelUp(int newLevel)
    {
        // Show level up notification and button
        ShowLevelUpNotification(newLevel);
        ShowLevelUpButton(true);

        // Update stats UI to reflect new available points
        UpdateStatsUI();
    }

    private void ShowLevelUpNotification(int newLevel)
    {
        if (levelUpNotification != null && levelUpText != null)
        {
            levelUpText.text = $"LEVEL UP!\nLevel {newLevel}";

            if (levelUpCoroutine != null)
            {
                StopCoroutine(levelUpCoroutine);
            }

            levelUpNotification.SetActive(true);
            levelUpCoroutine = StartCoroutine(HideLevelUpNotificationAfterDelay(3f));
        }
    }

    private IEnumerator HideLevelUpNotificationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (levelUpNotification != null)
        {
            levelUpNotification.SetActive(false);
        }
        levelUpCoroutine = null;
    }

    private void OnStatPointsChanged(int availablePoints)
    {
        UpdateStatsUI();
    }

    private void UpdateStatsUI()
    {
        if (playerStats == null) return;

        bool canUpgrade = playerStats.CanUpgradeStat();

        // Update available points display
        if (availablePointsText != null)
        {
            availablePointsText.text = $"Available Points: {playerStats.AvailablePoints}";
        }

        // Update regular stat UI
        if (statUi != null)
        {
            foreach (var statUI in statUi)
            {
                if (statUI != null)
                {
                    int statValue = playerStats.GetStatValue(statUI.statType);
                    statUI.UpdateDisplay(statValue, canUpgrade);
                }
            }
        }

        // Update level up stat UI
        if (levelUpStatUi != null)
        {
            foreach (var statUI in levelUpStatUi)
            {
                if (statUI != null)
                {
                    int statValue = playerStats.GetStatValue(statUI.statType);
                    statUI.UpdateDisplay(statValue, canUpgrade);
                }
            }
        }
    }

    private void UpdatePlayerInfoUI()
    {
        if (playerStats == null) return;

        if (deathCountText != null)
        {
            deathCountText.text = $"";
        }
    }

    private void DecreaseStat(PlayerStats.StatType statType)
    {
        if (playerStats != null)
        {
            // Fix the method name - should be DecreaseState or DecreaseStat depending on PlayerStats implementation
            bool success = playerStats.DecreaseState(statType); // Check if this method exists in PlayerStats
            if (success)
            {
                Debug.Log($"Decreased {statType} stat. Available points: {playerStats.AvailablePoints}");

                // Update UI to show the level up button again if points are available
                if (playerStats.AvailablePoints > 0)
                {
                    ShowLevelUpButton(true);
                }
            }
            else
            {
                Debug.Log($"Failed to decrease {statType} stat. Available points: {playerStats.AvailablePoints}");
            }
        }
    }
    private void IncreaseStat(PlayerStats.StatType statType)
    {
        if (playerStats != null)
        {
            // Fix the method name - should be IncreaseState or IncreaseStat depending on PlayerStats implementation
            bool success = playerStats.IncreaseState(statType); // or IncreaseState if that's the correct name
            if (success)
            {
                Debug.Log($"Increased {statType} stat. Available points: {playerStats.AvailablePoints}");

                // Hide level up button if no more points available
                if (playerStats.AvailablePoints <= 0)
                {
                    ShowLevelUpButton(false);
                    if (levelUpNotification != null)
                        levelUpNotification.SetActive(false);
                }
            }
            else
            {
                Debug.Log($"Failed to increase {statType} stat. Available points: {playerStats.AvailablePoints}");
            }
        }
    }

    public void ShowLevelUpButton(bool show)
    {
        if (levelUpButton != null)
        {
            levelUpButton.gameObject.SetActive(show);
        }
    }

    public void OpenStatsPanel()
    {
        if (statsPanel != null && StatspointsPanel != null)
        {
            StatspointsPanel.SetActive(true);
            statsPanel.SetActive(true);
            deathCountPanel.SetActive(false); // Hide death count text when stats panel is open
            UpdateStatsUI(); // Refresh the display
        }
    }



    public void CloseStatsPanel()
    {
        if (statsPanel != null)
        {
            statsPanel.SetActive(false);
        }

        if (StatspointsPanel != null)
        {
            StatspointsPanel.SetActive(false);
        }
        deathCountPanel.SetActive(true); // Hide death count text when stats panel is open
    }

    private void OnPlayerDeath()
    {
        // Update death count display
        if (deathCountText != null && playerStats != null)
        {
            deathCountText.text = $"YOU Died! Deaths: {playerStats.Deaths} Press enter to loop";
        }

        Debug.Log("Player died - UI updated");
    }

    private void OnPlayerRespawn()
    {
        deathCountText.text = "";
        Debug.Log("Player respawned - UI updated");
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= UpdateHealthUI;
            playerStats.OnExpChanged -= UpdateExpUI;
            playerStats.OnLevelUp -= OnPlayerLevelUp;
            playerStats.OnStatChanged -= OnStatPointsChanged;
            playerStats.OnDeath -= OnPlayerDeath;
            playerStats.OnPlayerRespawn -= OnPlayerRespawn;
        }

        if (PlayerMove.instance?.inputSystem?.Inventory.action != null)
        {
            PlayerMove.instance.inputSystem.Inventory.action.performed -= OpenInventory;
        }

        // Clean up coroutines
        if (levelUpCoroutine != null)
        {
            StopCoroutine(levelUpCoroutine);
        }


        if (closeStatsButton != null)
            closeStatsButton.onClick.RemoveAllListeners();

        // Clean up regular stat UI button listeners
        if (statUi != null)
        {
            foreach (var statUI in statUi)
            {
                if (statUI?.increaseButton != null)
                {
                    statUI.increaseButton.onClick.RemoveAllListeners();
                }
                if (statUI?.decreaseButton != null)
                {
                    statUI.decreaseButton.onClick.RemoveAllListeners();
                }
            }
        }

        // Clean up level up stat UI button listeners
        if (levelUpStatUi != null)
        {
            foreach (var statUI in levelUpStatUi)
            {
                if (statUI?.increaseButton != null)
                {
                    statUI.increaseButton.onClick.RemoveAllListeners();
                }
                if (statUI?.decreaseButton != null)
                {
                    statUI.decreaseButton.onClick.RemoveAllListeners();
                }
            }
        }
    }
}