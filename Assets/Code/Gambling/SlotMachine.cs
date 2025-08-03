
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


[System.Serializable]
public class SlotPrize
{
    public PrizeType prizeType;
    public WeponClass weapon; // For weapon prizes
    public int expAmount; // For EXP prizes
    public int healthAmount; // For health prizes
    public Sprite prizeIcon; // Visual representation
    public string prizeName;
    public Color prizeColor = Color.white;
}

[System.Serializable]
public class SlotSymbol
{
    public Sprite symbolSprite;
    public PrizeType prizeType;
    public float weight = 1f; // Higher weight = more common
}

public class SlotMachine : MonoBehaviour
{
    [Header("Slot Machine Settings")]
    public float spinDuration = 1.5f; // CHANGED: Reduced from 3f to 1.5f
    public float reelSpeed = 10f;

    [Header("Rarity Weights (Lower = Rarer)")]
    [Range(0.1f, 100f)] public float trashWeight = 45f;
    [Range(0.1f, 100f)] public float commonWeight = 25f;
    [Range(0.1f, 100f)] public float uncommonWeight = 15f;
    [Range(0.1f, 100f)] public float rareWeight = 8f;
    [Range(0.1f, 100f)] public float epicWeight = 4f;
    [Range(0.1f, 100f)] public float legendaryWeight = 2f;
    [Range(0.1f, 100f)] public float mythicWeight = 0.8f;
    [Range(0.1f, 100f)] public float exoticWeight = 0.15f;
    [Range(0.1f, 100f)] public float ultimateWeight = 0.04f;
    [Range(0.1f, 100f)] public float superUltimateWeight = 0.008f;
    [Range(0.1f, 100f)] public float godlikeWeight = 0.002f;

    [Header("Prize Pools")]
    public List<SlotPrize> weaponPrizes = new List<SlotPrize>();
    public List<SlotPrize> expPrizes = new List<SlotPrize>();
    public List<SlotPrize> healthPrizes = new List<SlotPrize>();

    [Header("UI References")]
    public GameObject slotMachinePanel;
    public Image[] reelImages = new Image[3]; // 3 reels
    public Button spinButton;
    public TextMeshProUGUI resultText;

    public GameObject prizeDisplayPanel;
    public Image prizeIcon;
    public TextMeshProUGUI prizeNameText;
    public TextMeshProUGUI prizeDescriptionText;
    public Button claimPrizeButton;
    public Button closeButton;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip spinSound;
    public AudioClip winSound;
    public AudioClip loseSound;

    private bool isSpinning = false;
    private SlotPrize currentPrize;
    private PlayerStats playerStats;
    private WeaponInventory weaponInventory;
    private List<SlotSymbol> allSymbols = new List<SlotSymbol>();
    private Coroutine spinCoroutine;

    private void Start()
    {
        InitializeSlotMachine();
        SetupUI();
        CreateSymbolPool();
    }

    private void InitializeSlotMachine()
    {
        playerStats = PlayerStats.instance;
        weaponInventory = FindObjectOfType<WeaponInventory>();

        if (slotMachinePanel != null)
            slotMachinePanel.SetActive(false);

        if (prizeDisplayPanel != null)
            prizeDisplayPanel.SetActive(false);
    }

    private void SetupUI()
    {
        if (spinButton != null)
            spinButton.onClick.AddListener(OnSpinButtonPressed);

        if (claimPrizeButton != null)
            claimPrizeButton.onClick.AddListener(ClaimPrize);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseSlotMachine);
    }

    private void CreateSymbolPool()
    {
        allSymbols.Clear();

        // Create symbols for each prize type with their weights
        AddSymbolsForPrizeType(PrizeType.Trash, trashWeight);
        AddSymbolsForPrizeType(PrizeType.Common, commonWeight);
        AddSymbolsForPrizeType(PrizeType.Uncommon, uncommonWeight);
        AddSymbolsForPrizeType(PrizeType.Rare, rareWeight);
        AddSymbolsForPrizeType(PrizeType.Epic, epicWeight);
        AddSymbolsForPrizeType(PrizeType.Legendary, legendaryWeight);
        AddSymbolsForPrizeType(PrizeType.Mythic, mythicWeight);
        AddSymbolsForPrizeType(PrizeType.Exotic, exoticWeight);
        AddSymbolsForPrizeType(PrizeType.Ultimate, ultimateWeight);
        AddSymbolsForPrizeType(PrizeType.SuperUltimate, superUltimateWeight);
        AddSymbolsForPrizeType(PrizeType.Godlike, godlikeWeight);
    }

    private void AddSymbolsForPrizeType(PrizeType prizeType, float weight)
    {
        // Get a representative prize for this type to use its icon
        SlotPrize representativePrize = GetRandomPrizeOfType(prizeType);
        if (representativePrize != null)
        {
            SlotSymbol symbol = new SlotSymbol
            {
                symbolSprite = representativePrize.prizeIcon,
                prizeType = prizeType,
                weight = weight
            };
            allSymbols.Add(symbol);
        }
    }

    public void OpenSlotMachine()
    {
        if (slotMachinePanel != null)
        {
            slotMachinePanel.SetActive(true);
            ResetReels();
        }
    }

    public void CloseSlotMachine()
    {
        if (slotMachinePanel != null)
            slotMachinePanel.SetActive(false);

        if (prizeDisplayPanel != null)
            prizeDisplayPanel.SetActive(false);

        if (spinCoroutine != null)
        {
            StopCoroutine(spinCoroutine);
            isSpinning = false;
        }
    }

    private void OnSpinButtonPressed()
    {
        if (isSpinning) return;

        // Check if player can afford spin (implement your currency system)
        if (!CanAffordSpin())
        {
            if (resultText != null)
                resultText.text = "Not enough currency!";
            return;
        }

        // Deduct cost (implement your currency system)
        DeductSpinCost();

        StartSpin();
    }

    private bool CanAffordSpin()
    {
        // Implement your currency check here
        // For now, always return true (free spins)
        return true;
    }

    private void DeductSpinCost()
    {
        // Implement currency deduction here
        // For example: playerStats.SpendCurrency(spinCost);
    }

    private void StartSpin()
    {
        if (isSpinning) return;

        isSpinning = true;
        spinButton.interactable = false;

        if (resultText != null)
            resultText.text = "Spinning...";

        if (audioSource != null && spinSound != null)
            audioSource.PlayOneShot(spinSound);

        spinCoroutine = StartCoroutine(SpinAnimation());
    }

    private IEnumerator SpinAnimation()
    {
        float elapsedTime = 0f;
        List<int> finalSymbols = new List<int>();

        // Determine final result
        for (int i = 0; i < 3; i++)
        {
            finalSymbols.Add(GetWeightedRandomSymbolIndex());
        }

        // OPTIMIZED: Much faster spinning with variable speed per reel
        float[] reelStopTimes = { spinDuration * 0.6f, spinDuration * 0.8f, spinDuration }; // Stagger reel stops
        bool[] reelsStopped = { false, false, false };

        while (elapsedTime < spinDuration)
        {
            for (int i = 0; i < reelImages.Length; i++)
            {
                if (reelImages[i] != null)
                {
                    // Stop reel if it's time, otherwise show random symbols
                    if (elapsedTime >= reelStopTimes[i] && !reelsStopped[i])
                    {
                        // Stop this reel with final symbol
                        reelImages[i].sprite = allSymbols[finalSymbols[i]].symbolSprite;
                        reelImages[i].color = GetPrizeTypeColor(allSymbols[finalSymbols[i]].prizeType);
                        reelsStopped[i] = true;
                    }
                    else if (!reelsStopped[i])
                    {
                        // Show random symbols during spin
                        int randomIndex = Random.Range(0, allSymbols.Count);
                        reelImages[i].sprite = allSymbols[randomIndex].symbolSprite;
                        reelImages[i].color = GetPrizeTypeColor(allSymbols[randomIndex].prizeType);
                    }
                }
            }

            elapsedTime += Time.deltaTime;

            // MUCH FASTER: Reduced wait time significantly
            yield return new WaitForSeconds(0.02f); // Changed from 0.05f to 0.02f
        }

        // Ensure all reels show final result
        for (int i = 0; i < reelImages.Length; i++)
        {
            if (reelImages[i] != null && finalSymbols[i] < allSymbols.Count)
            {
                reelImages[i].sprite = allSymbols[finalSymbols[i]].symbolSprite;
                reelImages[i].color = GetPrizeTypeColor(allSymbols[finalSymbols[i]].prizeType);
            }
        }

        // Check for win
        ProcessSpinResult(finalSymbols);

        isSpinning = false;
        spinButton.interactable = true;
    }

    private int GetWeightedRandomSymbolIndex()
    {
        float totalWeight = 0f;
        for (int i = 0; i < allSymbols.Count; i++)
        {
            totalWeight += allSymbols[i].weight;
        }

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        for (int i = 0; i < allSymbols.Count; i++)
        {
            currentWeight += allSymbols[i].weight;
            if (randomValue <= currentWeight)
            {
                return i;
            }
        }

        return 0; // Fallback
    }

    private void ProcessSpinResult(List<int> symbolIndices)
    {
        // Check for matching symbols (3 of a kind = win)
        if (symbolIndices[0] == symbolIndices[1] && symbolIndices[1] == symbolIndices[2])
        {
            // Perfect match - best prize of that type
            PrizeType wonType = allSymbols[symbolIndices[0]].prizeType;
            currentPrize = GetRandomPrizeOfType(wonType);
            ShowWinResult();
        }
        else if (symbolIndices[0] == symbolIndices[1] || symbolIndices[1] == symbolIndices[2] || symbolIndices[0] == symbolIndices[2])
        {
            // Two matching - lesser prize
            PrizeType wonType = GetMatchingPrizeType(symbolIndices);
            currentPrize = GetRandomPrizeOfType(wonType);
            // Reduce prize value for partial match
            if (currentPrize != null)
            {
                currentPrize.expAmount = Mathf.Max(1, currentPrize.expAmount / 2);
                currentPrize.healthAmount = Mathf.Max(1, currentPrize.healthAmount / 2);
            }
            ShowWinResult();
        }
        else
        {
            // No match
            ShowLoseResult();
        }
    }

    private PrizeType GetMatchingPrizeType(List<int> symbolIndices)
    {
        if (symbolIndices[0] == symbolIndices[1])
            return allSymbols[symbolIndices[0]].prizeType;
        else if (symbolIndices[1] == symbolIndices[2])
            return allSymbols[symbolIndices[1]].prizeType;
        else
            return allSymbols[symbolIndices[0]].prizeType;
    }

    private SlotPrize GetRandomPrizeOfType(PrizeType prizeType)
    {
        List<SlotPrize> availablePrizes = new List<SlotPrize>();

        // Collect all prizes of the specified type
        availablePrizes.AddRange(weaponPrizes.FindAll(p => p.prizeType == prizeType));
        availablePrizes.AddRange(expPrizes.FindAll(p => p.prizeType == prizeType));
        availablePrizes.AddRange(healthPrizes.FindAll(p => p.prizeType == prizeType));

        if (availablePrizes.Count > 0)
        {
            return availablePrizes[Random.Range(0, availablePrizes.Count)];
        }

        // Fallback - create a basic prize
        return CreateFallbackPrize(prizeType);
    }

    private SlotPrize CreateFallbackPrize(PrizeType prizeType)
    {
        SlotPrize fallback = new SlotPrize();
        fallback.prizeType = prizeType;
        fallback.prizeColor = GetPrizeTypeColor(prizeType);

        int multiplier = GetPrizeTypeMultiplier(prizeType);

        // Randomly choose between EXP and health
        if (Random.value > 0.5f)
        {
            fallback.expAmount = 10 * multiplier;
            fallback.prizeName = $"{fallback.expAmount} EXP";
        }
        else
        {
            fallback.healthAmount = 5 * multiplier;
            fallback.prizeName = $"{fallback.healthAmount} Health";
        }

        return fallback;
    }

    private int GetPrizeTypeMultiplier(PrizeType prizeType)
    {
        switch (prizeType)
        {
            case PrizeType.Trash: return 1;
            case PrizeType.Common: return 2;
            case PrizeType.Uncommon: return 3;
            case PrizeType.Rare: return 5;
            case PrizeType.Epic: return 8;
            case PrizeType.Legendary: return 15;
            case PrizeType.Mythic: return 25;
            case PrizeType.Exotic: return 40;
            case PrizeType.Ultimate: return 70;
            case PrizeType.SuperUltimate: return 120;
            case PrizeType.Godlike: return 200;
            default: return 1;
        }
    }

    private Color GetPrizeTypeColor(PrizeType prizeType)
    {
        switch (prizeType)
        {
            case PrizeType.Trash: return Color.gray;
            case PrizeType.Common: return Color.white;
            case PrizeType.Uncommon: return Color.green;
            case PrizeType.Rare: return Color.blue;
            case PrizeType.Epic: return Color.magenta;
            case PrizeType.Legendary: return Color.yellow;
            case PrizeType.Mythic: return new Color(1f, 0.5f, 0f); // Orange
            case PrizeType.Exotic: return Color.cyan;
            case PrizeType.Ultimate: return new Color(1f, 0f, 1f); // Pink
            case PrizeType.SuperUltimate: return new Color(0.5f, 0f, 1f); // Purple
            case PrizeType.Godlike: return new Color(1f, 0.8f, 0f); // Gold
            default: return Color.white;
        }
    }

    private void ShowWinResult()
    {
        if (audioSource != null && winSound != null)
            audioSource.PlayOneShot(winSound);

        if (resultText != null)
            resultText.text = "YOU WON!";

        DisplayPrize();
    }

    private void ShowLoseResult()
    {
        if (audioSource != null && loseSound != null)
            audioSource.PlayOneShot(loseSound);

        if (resultText != null)
            resultText.text = "Try again!";
    }

    private void DisplayPrize()
    {
        if (prizeDisplayPanel != null && currentPrize != null)
        {
            prizeDisplayPanel.SetActive(true);

            if (prizeIcon != null)
            {
                prizeIcon.sprite = currentPrize.prizeIcon;
                prizeIcon.color = currentPrize.prizeColor;
            }

            if (prizeNameText != null)
                prizeNameText.text = currentPrize.prizeName;

            if (prizeDescriptionText != null)
            {
                string description = "";
                if (currentPrize.weapon != null)
                    description = $"Weapon: {currentPrize.weapon.name}";
                else if (currentPrize.expAmount > 0)
                    description = $"Gain {currentPrize.expAmount} Experience Points";
                else if (currentPrize.healthAmount > 0)
                    description = $"Restore {currentPrize.healthAmount} Health";

                prizeDescriptionText.text = description;
            }
        }
    }

    private void ClaimPrize()
    {
        if (currentPrize == null) return;

        bool claimed = false;

        // Award weapon
        if (currentPrize.weapon != null && weaponInventory != null)
        {
            claimed = weaponInventory.TryAddWeapon(currentPrize.weapon);
            if (claimed)
                Debug.Log($"Added weapon {currentPrize.weapon.name} to inventory!");
            else
                Debug.Log("Inventory full! Weapon converted to EXP.");
        }

        // Award EXP
        if (currentPrize.expAmount > 0 && playerStats != null)
        {
            playerStats.GainExp(currentPrize.expAmount);
            claimed = true;
            Debug.Log($"Gained {currentPrize.expAmount} EXP!");
        }

        // Award Health
        if (currentPrize.healthAmount > 0 && playerStats != null)
        {
            playerStats.Heal(currentPrize.healthAmount);
            claimed = true;
            Debug.Log($"Restored {currentPrize.healthAmount} health!");
        }

        if (claimed)
        {
            if (resultText != null)
                resultText.text = "Prize claimed!";
        }

        // Hide prize display
        if (prizeDisplayPanel != null)
            prizeDisplayPanel.SetActive(false);

        currentPrize = null;
    }

    private void ResetReels()
    {
        for (int i = 0; i < reelImages.Length; i++)
        {
            if (reelImages[i] != null && allSymbols.Count > 0)
            {
                reelImages[i].sprite = allSymbols[0].symbolSprite;
                reelImages[i].color = Color.white;
            }
        }

        if (resultText != null)
            resultText.text = "Good Luck!";
    }

    private void OnDestroy()
    {
        if (spinButton != null)
            spinButton.onClick.RemoveAllListeners();

        if (claimPrizeButton != null)
            claimPrizeButton.onClick.RemoveAllListeners();

        if (closeButton != null)
            closeButton.onClick.RemoveAllListeners();
    }
}