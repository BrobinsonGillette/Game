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
    public Button decreaseButton; // For respec functionality if needed

    private int currentValue;

    public void UpdateDisplay(int value, bool canUpgrade)
    {
        currentValue = value;
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

    [Header("UI Panels")]
    public GameObject statsPanel;
    public GameObject pointsPanel;
    public GameObject levelUpPanel;
    public GameObject healthBarPanel;
    public GameObject expBarPanel;

    [Header("Health UI")]
    public Slider healthSlider;
    public TextMeshProUGUI healthText;


    [Header("Experience UI")]
    public Slider expSlider;
    public TextMeshProUGUI[] expText;
    public TextMeshProUGUI[] levelText;

    [Header("Stats UI")]
    [SerializeField] private StatUI[] statUi;
    public TextMeshProUGUI availablePointsText;
    public Button levelUpButton;
    public Button closeStatsButton;

    [Header("Level Up UI")]
    [SerializeField] private StatUI[] levelUpStatUi;
    public TextMeshProUGUI levelUpText;
    public GameObject levelUpNotification;

    [Header("Player Info")]
    public TextMeshProUGUI deathCountText;

    private PlayerStats playerStats;
    private Coroutine levelUpCoroutine;

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
        InitializeUI();
        SetupEventListeners();
        SetupButtonListeners();
    }

    private void InitializeUI()
    {
        // Get reference to PlayerStats
        playerStats = PlayerStats.instance;

        if (playerStats == null)
        {
            Debug.LogError("PlayerStats instance not found! Make sure PlayerStats is initialized before UIManager.");
            return;
        }

        // Initialize UI panels
        if (statsPanel != null)
            statsPanel.SetActive(false);

        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);

        if (pointsPanel != null)
            pointsPanel.SetActive(false);

        if (levelUpNotification != null)
            levelUpNotification.SetActive(false);

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
        // Setup stat upgrade buttons for level up UI
        if (levelUpStatUi != null)
        {
            for (int i = 0; i < levelUpStatUi.Length; i++)
            {
                if (levelUpStatUi[i]?.increaseButton != null)
                {
                    int index = i; // Capture for closure
                    levelUpStatUi[i].increaseButton.onClick.AddListener(() => IncreseStat(levelUpStatUi[index].statType));
                    levelUpStatUi[i].decreaseButton.onClick.AddListener(() => DecreaseStat(levelUpStatUi[index].statType));
                }
            }
        }


        // Setup panel buttons
        if (levelUpButton != null)
            levelUpButton.onClick.AddListener(OpenLevelUpPanel);

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
            deathCountText.text = $"Deaths: {playerStats.Deaths}";
        }
    }
    private void DecreaseStat(PlayerStats.StatType statType)
    {
        if (playerStats != null)
        {
            bool success = playerStats.DecreaseState(statType);
            if (success)
            {
                // Hide level up button if no more points available
                if (playerStats.AvailablePoints <= 0)
                {
                    ShowLevelUpButton(false);
                    if (levelUpNotification != null)
                        levelUpNotification.SetActive(false);
                }
            }
        }
    }
    private void IncreseStat(PlayerStats.StatType statType)
    {
        if (playerStats != null)
        {
            bool success = playerStats.IncreceState(statType);
            if (success)
            {
                // Hide level up button if no more points available
                if (playerStats.AvailablePoints <= 0)
                {
                    ShowLevelUpButton(false);
                    if (levelUpNotification != null)
                        levelUpNotification.SetActive(false);
                }
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
        if (statsPanel != null && pointsPanel != null)
        {
            pointsPanel.SetActive(true);
            statsPanel.SetActive(true);
            UpdateStatsUI(); // Refresh the display
        }
    }

    public void OpenLevelUpPanel()
    {
        if (levelUpPanel != null && pointsPanel != null)
        {
            // Close stats panel if it's open, but keep points panel
            if (statsPanel != null)
                statsPanel.SetActive(false);

            pointsPanel.SetActive(true);
            levelUpPanel.SetActive(true);
            UpdateStatsUI(); // Refresh the display
        }
    }

    public void CloseStatsPanel()
    {
        if (statsPanel != null)
        {
            statsPanel.SetActive(false);
        }

        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(false);
        }

        if (pointsPanel != null)
        {
            pointsPanel.SetActive(false);
        }
    }

    private void OnPlayerDeath()
    {
        // Update death count display
        if (deathCountText != null && playerStats != null)
        {
            deathCountText.text = $"Deaths: {playerStats.Deaths}";
        }

        Debug.Log("Player died - UI updated");
    }

    private void OnPlayerRespawn()
    {
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


        PlayerMove.instance.inputSystem.Inventory.action.performed -= OpenInventory;
 

        // Clean up coroutines
        if (levelUpCoroutine != null)
        {
            StopCoroutine(levelUpCoroutine);
        }

        // Clean up button listeners
        if (levelUpButton != null)
            levelUpButton.onClick.RemoveAllListeners();

        if (closeStatsButton != null)
            closeStatsButton.onClick.RemoveAllListeners();

        // Clean up stat UI button listeners
        if (levelUpStatUi != null)
        {
            foreach (var statUI in levelUpStatUi)
            {
                if (statUI?.increaseButton != null)
                {
                    statUI.increaseButton.onClick.RemoveAllListeners();
                }
            }
        }

        if (statUi != null)
        {
            foreach (var statUI in statUi)
            {
                if (statUI?.increaseButton != null)
                {
                    statUI.increaseButton.onClick.RemoveAllListeners();
                }
            }
        }
    }

  
}