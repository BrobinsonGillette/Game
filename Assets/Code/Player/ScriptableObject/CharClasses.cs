using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
[System.Serializable]
public class CharacterStats
{
    [Header("Basic Stats")]
    public float maxHealth = 100f;
    public float maxMana = 50f;
    public int moveSpeed = 3;
    public int maxActionPoints = 2;

    [Header("Combat Stats")]
    public float baseDamage = 10f;
    public float armor = 0f;
    public float criticalChance = 0.1f;
    public float criticalMultiplier = 2f;

    [Header("Regeneration")]
    public float healthRegenPerTurn = 2f;
    public float manaRegenPerTurn = 5f;
}

[System.Serializable]
public class UISettings
{
    [Header("UI Positioning")]
    public Vector3 moveCounterOffset = new Vector3(0, 0.8f, 0);
    public Vector3 healthDisplayOffset = new Vector3(0, -0.8f, 0);
    public Vector3 nameDisplayOffset = new Vector3(0, 1.2f, 0);

    [Header("UI Styling")]
    public float moveCounterFontSize = 2f;
    public float healthDisplayFontSize = 1.5f;
    public float nameDisplayFontSize = 1.8f;
    public int uiSortingOrder = 10;

    [Header("Color Settings")]
    public Color healthyColor = Color.green;
    public Color warnColor = Color.yellow;
    public Color dangerColor = Color.red;
    public Color nameColor = Color.white;
}
public abstract class CharClasses : ScriptableObject
{
    [Header("Basic Information")]
    public string characterName = "Player";
    public string description = "";
    public Sprite characterSprite;

    [Header("Character Stats")]
    public CharacterStats stats = new CharacterStats();

    [Header("UI Settings")]
    public UISettings uiSettings = new UISettings();

    [Header("Actions & Abilities")]
    public BaseAction[] defaultActions;
    public List<BaseAction> availableActions = new List<BaseAction>();

    [Header("Special Traits")]
    public string[] traits;
    public bool canFly = false;
    public bool canSwim = false;
    public bool hasNightVision = false;

    // Runtime values
    [System.NonSerialized] public float currentHealth;
    [System.NonSerialized] public float currentMana;
    [System.NonSerialized] public int currentActionPoints;
    [System.NonSerialized] public int remainingMoves;

    // UI Components
    private TextMeshPro moveCounterText;
    private TextMeshPro healthText;
    private TextMeshPro nameText;

    private Transform characterTransform;

    // Events for easy extension
    public System.Action<CharClasses> OnStatsChanged;
    public System.Action<CharClasses> OnActionUsed;

    public virtual void InitializeCharacter(SpriteRenderer characterRenderer ,Transform characterPrefab)
    {
        characterTransform = characterPrefab;

        if (characterRenderer != null && characterSprite != null)
        {
            characterRenderer.sprite = characterSprite;
        }

        // Initialize runtime values
        ResetToFullStats();

        // Create UI
        CreateUI();

        // Initialize actions
        InitializeActions();

        // Apply any special initialization
        ApplySpecialTraits();
    }

    public void ResetToFullStats()
    {
        currentHealth = stats.maxHealth;
        currentMana = stats.maxMana;
        currentActionPoints = stats.maxActionPoints;
        remainingMoves = stats.moveSpeed;
    }

    private void CreateUI()
    {
        CreateMoveCounter();
        CreateHealthDisplay();
        CreateNameDisplay();
    }

    private void CreateMoveCounter()
    {
        GameObject counterObj = new GameObject("MoveCounter");
        counterObj.transform.SetParent(characterTransform);
        counterObj.transform.localPosition = uiSettings.moveCounterOffset;

        moveCounterText = counterObj.AddComponent<TextMeshPro>();
        moveCounterText.fontSize = uiSettings.moveCounterFontSize;
        moveCounterText.alignment = TextAlignmentOptions.Center;
        moveCounterText.sortingOrder = uiSettings.uiSortingOrder;

        UpdateMoveCounter();
    }

    private void CreateHealthDisplay()
    {
        GameObject healthObj = new GameObject("HealthDisplay");
        healthObj.transform.SetParent(characterTransform);
        healthObj.transform.localPosition = uiSettings.healthDisplayOffset;

        healthText = healthObj.AddComponent<TextMeshPro>();
        healthText.fontSize = uiSettings.healthDisplayFontSize;
        healthText.alignment = TextAlignmentOptions.Center;
        healthText.sortingOrder = uiSettings.uiSortingOrder;

        UpdateHealthDisplay();
    }

    private void CreateNameDisplay()
    {
        GameObject nameObj = new GameObject("NameDisplay");
        nameObj.transform.SetParent(characterTransform);
        nameObj.transform.localPosition = uiSettings.nameDisplayOffset;

        nameText = nameObj.AddComponent<TextMeshPro>();
        nameText.text = characterName;
        nameText.fontSize = uiSettings.nameDisplayFontSize;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.sortingOrder = uiSettings.uiSortingOrder;
        nameText.color = uiSettings.nameColor;
    }

    public void UpdateMoveCounter()
    {
        if (moveCounterText == null) return;

        moveCounterText.text = $"M:{remainingMoves}/{stats.moveSpeed} | AP:{currentActionPoints}";

        // Color coding based on remaining resources
        if (remainingMoves == 0 && currentActionPoints == 0)
            moveCounterText.color = uiSettings.dangerColor;
        else if (remainingMoves <= stats.moveSpeed / 2 || currentActionPoints <= stats.maxActionPoints / 2)
            moveCounterText.color = uiSettings.warnColor;
        else
            moveCounterText.color = uiSettings.healthyColor;
    }

    public void UpdateHealthDisplay()
    {
        if (healthText == null) return;

        healthText.text = $"HP:{currentHealth:F0}/{stats.maxHealth:F0}\nMP:{currentMana:F0}/{stats.maxMana:F0}";

        float healthPercent = currentHealth / stats.maxHealth;
        if (healthPercent > 0.6f)
            healthText.color = uiSettings.healthyColor;
        else if (healthPercent > 0.3f)
            healthText.color = uiSettings.warnColor;
        else
            healthText.color = uiSettings.dangerColor;
    }

    public virtual void InitializeActions()
    {
        availableActions.Clear();

        // Add default actions
        if (defaultActions != null)
        {
            foreach (var action in defaultActions)
            {
                if (action != null)
                {
                    availableActions.Add(action);
                }
            }
        }

        // Add class-specific actions
        AddClassSpecificActions();
    }

    // Override this in derived classes to add specific actions
    protected virtual void AddClassSpecificActions()
    {
        // Base implementation does nothing
        // Override in specific character classes like Warrior, Mage, etc.
    }

    // Override this in derived classes for special behaviors
    protected virtual void ApplySpecialTraits()
    {
        // Base implementation does nothing
        // Override to apply traits like flight, night vision, etc.
    }

    public virtual void StartTurn()
    {
        remainingMoves = stats.moveSpeed;
        currentActionPoints = stats.maxActionPoints;

        // Apply regeneration
        RegenerateResources();

        UpdateDisplays();
        OnStatsChanged?.Invoke(this);
    }

    public virtual void EndTurn()
    {
        remainingMoves = 0;
        currentActionPoints = 0;

        UpdateDisplays();
        OnStatsChanged?.Invoke(this);
    }

    protected virtual void RegenerateResources()
    {
        currentHealth = Mathf.Min(stats.maxHealth, currentHealth + stats.healthRegenPerTurn);
        currentMana = Mathf.Min(stats.maxMana, currentMana + stats.manaRegenPerTurn);
    }

    public void UpdateDisplays()
    {
        UpdateMoveCounter();
        UpdateHealthDisplay();
    }

    // Utility methods for easier character management
    public bool CanUseAction(BaseAction action)
    {
        if (action == null) return false;
        if (currentActionPoints < action.actionPointCost) return false;

        if (action is SpellAction spell && currentMana < spell.manaCost)
            return false;

        return true;
    }

    public void SpendActionPoints(int cost)
    {
        currentActionPoints = Mathf.Max(0, currentActionPoints - cost);
        UpdateDisplays();
        OnStatsChanged?.Invoke(this);
    }

    public void SpendMana(float cost)
    {
        currentMana = Mathf.Max(0, currentMana - cost);
        UpdateDisplays();
        OnStatsChanged?.Invoke(this);
    }

    public float TakeDamage(float damage)
    {
        // Apply armor reduction
        float actualDamage = Mathf.Max(1f, damage - stats.armor);

        currentHealth = Mathf.Max(0, currentHealth - actualDamage);
        UpdateDisplays();
        OnStatsChanged?.Invoke(this);

        return actualDamage;
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(stats.maxHealth, currentHealth + amount);
        UpdateDisplays();
        OnStatsChanged?.Invoke(this);
    }

    // Check if character has a specific trait
    public bool HasTrait(string traitName)
    {
        if (traits == null) return false;

        foreach (string trait in traits)
        {
            if (trait.Equals(traitName, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    // Get movement cost modifier based on terrain and traits
    public virtual int GetMovementCost(HexTile tile)
    {
        int baseCost = tile.movementCost;

        // Apply trait modifiers
        if (canFly) return 1; // Flying ignores terrain
        if (canSwim && tile.isWater) return baseCost / 2; // Swimming reduces water cost

        return baseCost;
    }

    // Easy way to create stat modifiers
    [System.Serializable]
    public class StatModifier
    {
        public string name;
        public float healthMultiplier = 1f;
        public float manaMultiplier = 1f;
        public int moveSpeedBonus = 0;
        public int actionPointBonus = 0;
        public float damageMultiplier = 1f;
        public float armorBonus = 0f;
    }

    public void ApplyStatModifier(StatModifier modifier)
    {
        stats.maxHealth *= modifier.healthMultiplier;
        stats.maxMana *= modifier.manaMultiplier;
        stats.moveSpeed += modifier.moveSpeedBonus;
        stats.maxActionPoints += modifier.actionPointBonus;
        stats.baseDamage *= modifier.damageMultiplier;
        stats.armor += modifier.armorBonus;

        // Update current values if needed
        currentHealth = Mathf.Min(currentHealth, stats.maxHealth);
        currentMana = Mathf.Min(currentMana, stats.maxMana);

        UpdateDisplays();
        OnStatsChanged?.Invoke(this);
    }
}
