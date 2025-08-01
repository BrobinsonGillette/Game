using System;
using UnityEngine.InputSystem;
using UnityEngine;
using System.Collections;

[System.Serializable]
public class PlayerData
{
    public int level;
    public float currentExp;
    public float currentHealth;
    public float maxHealth;
    public int might;
    public int agility;
    public int intellect;
    public int endurance;
    public int luck;
    public int sin;
    public int deaths;
    public int availablePoints;
}
public enum weaponType { melee, ranged };
public class PlayerStats : MonoBehaviour, IDamable
{
    public static PlayerStats instance { get; private set; }

    [Header("Player Stats")]
    [SerializeField] private int might = 1;
    [SerializeField] private int agility = 1;
    [SerializeField] private int intellect = 1;
    [SerializeField] private int endurance = 1;
    [SerializeField] private int luck = 1;
    [SerializeField] private int sin = 0;
    [SerializeField] private int deaths = 0;

    [Header("Level System")]
    [SerializeField] private int level = 1;
    [SerializeField] private float currentExp = 0f;
    [SerializeField] private float maxExp = 100f;
    [SerializeField] private int availablePoints = 0;
    [SerializeField] private int pointsPerLevel = 5;

    [Header("Health System")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Movement Stats")]
    [SerializeField] private float baseSpeed = 5f;
    [SerializeField] private float baseSprint = 10f;

    [Header("Level Scaling")]
    [SerializeField] private float healthPerLevel = 20f;
    [SerializeField] private float speedPerLevel = 0.5f;
    [SerializeField] private float expMultiplier = 1.5f;
    [SerializeField] private float enduranceHealthBonus = 3f; // Health bonus per endurance point

    [Header("Melee Attack Settings")]
    public float MeleeAttackDamage = 10f;
    public float MeleeAttackCooldown = 1f;
    public float meleeAreaRadius = 1.5f; // Radius of damage area
    public float meleeAreaDuration = 0.5f; // How long damage area stays active
    public GameObject meleeAreaPrefab; // Optional visual prefab for damage area

    [Header("Ranged Attack Settings")]
    public float RangeAttackDamage = 10f;
    public float RangeAttackCooldown = 1f;
    public GameObject projectilePrefab; // Projectile prefab to shoot
    public Transform firePoint; // Where projectiles spawn from
    public float projectileSpeed = 10f;
    public float projectileLifetime;

    [Header("Attack Detection")]
    public LayerMask EnemyLayer = 1; // FIXED: Changed from EnamyLayer to EnemyLayer
    private weaponType currentWeapon = weaponType.melee;

    // Properties for stats
    public int MightStat => might;
    public int AgilityStat => agility;
    public int IntellectStat => intellect;
    public int EnduranceStat => endurance;
    public int LuckStat => luck;
    public int SinStat => sin;

    // Properties for level system
    public int Level => level;
    public float CurrentExp => currentExp;
    public float MaxExp => maxExp;
    public int AvailablePoints => availablePoints;

    // Private variables
    private float lastMeleeAttackTime;
    private float lastRangeAttackTime;
    bool takeDamage;


    // Properties for health
    public float Health
    {
        get => currentHealth;
        set => currentHealth = Mathf.Clamp(value, 0, maxHealth);
    }
    public float MaxHealth => maxHealth;

    // Properties for movement (affected by Agility)
    public float MoveSpeed => baseSpeed + (level * speedPerLevel) + (agility * 0.1f);
    public float SprintSpeed => baseSprint + (level * speedPerLevel) + (agility * 0.2f);

    // Other properties
    public int Deaths => deaths;

    // Events
    public event Action<float, float> OnHealthChanged; // current, max
    public event Action<int> OnLevelUp;
    public event Action<float, float> OnExpChanged; // current, max
    public event Action OnDeath;
    public event Action<int> OnStatChanged; // available points
    public event Action OnPlayerRespawn;
    public event Action OnMeleeAttack;
    public event Action OnRangedAttack;

    // References
    private PlayerMove playerMove;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePlayer();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        playerMove = GetComponent<PlayerMove>();

        // FIXED: Properly subscribe to input events
        if (playerMove != null && playerMove.inputSystem != null)
        {
            playerMove.inputSystem.Attack.action.performed += TryAttacking;
        }

        // Set up firePoint if not assigned
        if (firePoint == null)
        {
            firePoint = transform;
        }
    }

    private void OnDestroy()
    {
        // FIXED: Unsubscribe from events to prevent memory leaks
        if (playerMove != null && playerMove.inputSystem != null)
        {
            playerMove.inputSystem.Attack.action.performed -= TryAttacking;
        }
    }

    private void TryAttacking(InputAction.CallbackContext context) // FIXED: Method name
    {
        if(IsDead()) return;
        switch (currentWeapon)
        {
            case weaponType.melee:
                TryMeleeAttack();
                break;
            case weaponType.ranged:
                TryRangedAttack();
                break;
        }
    }

    private void InitializePlayer()
    {
        // Calculate max health based on endurance
        RecalculateMaxHealth();
        currentHealth = maxHealth;
        UpdateMaxExp();

        // Invoke initial events
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnExpChanged?.Invoke(currentExp, maxExp);
        OnStatChanged?.Invoke(availablePoints);
    }

    private void TryMeleeAttack()
    {
        if (Time.time >= lastMeleeAttackTime + MeleeAttackCooldown)
        {
            StartCoroutine(PerformMeleeAttack());
            lastMeleeAttackTime = Time.time;
        }
    }

    private void TryRangedAttack()
    {
        if (Time.time >= lastRangeAttackTime + RangeAttackCooldown)
        {
            PerformRangedAttack();
            lastRangeAttackTime = Time.time;
        }
    }

    private IEnumerator PerformMeleeAttack()
    {
        OnMeleeAttack?.Invoke();

        Vector3 attackPosition = transform.position;

        GameObject damageArea = null;
        if (meleeAreaPrefab != null)
        {
            damageArea = Instantiate(meleeAreaPrefab, attackPosition, Quaternion.identity);
            BasicProjectile basicProjectile = damageArea.AddComponent<BasicProjectile>();
            basicProjectile.Initialize(0, 0, meleeAreaDuration);
        }



        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(attackPosition, meleeAreaRadius, EnemyLayer); // FIXED: EnemyLayer
        foreach (Collider2D hitCollider in hitColliders)
        {
            IDamable damageTarget = hitCollider.GetComponent<IDamable>();
         
            if (damageTarget != null)
            {
                damageArea.transform.position = hitCollider.GetComponent<Collider2D>().bounds.center;
                damageTarget.TakeDamage(MeleeAttackDamage);
                Debug.Log($"Player dealt {MeleeAttackDamage} melee damage to {hitCollider.name}");
            }
            else
            {
                Debug.Log($"No IDamable component found on {hitCollider.name}");
            }
        }
     

        yield return new WaitForSeconds(meleeAreaDuration);

        if (damageArea != null)
        {
            Destroy(damageArea);
        }

    }

    private void PerformRangedAttack()
    {
        OnRangedAttack?.Invoke();

        if (projectilePrefab != null && firePoint != null) // FIXED: Added firePoint null check
        {
            // FIXED: Improved mouse position to world position conversion
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f; // Ensure z is 0 for 2D

            Vector2 direction = ((Vector2)mousePos - (Vector2)firePoint.position).normalized;

            // Calculate 2D rotation angle
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            // Spawn projectile
            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, rotation);

            // Fallback to BasicProjectile
            BasicProjectile basicProjectile = projectile.GetComponent<BasicProjectile>();
            if (basicProjectile == null)
            {
                basicProjectile = projectile.AddComponent<BasicProjectile>();
            }

            // Add Rigidbody2D if needed
            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = projectile.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
            }

            rb.velocity = direction * projectileSpeed;
            basicProjectile.Initialize(RangeAttackDamage, EnemyLayer, projectileLifetime); 
        }
    }

    private void RecalculateMaxHealth()
    {
        float baseHealthForLevel = 10f + ((level - 1) * healthPerLevel);
        float enduranceBonus = endurance * enduranceHealthBonus;
        maxHealth = baseHealthForLevel + enduranceBonus;
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0 || currentHealth <= 0 && !takeDamage) return;

        // Calculate damage reduction: 1% reduction for every 2 points in endurance + agility combined
        float totalDefensiveStats = endurance + agility;
        float damageReductionPercent = (totalDefensiveStats / 2f) * 0.01f; // 1% per 2 points
        float damageReduction = 1f - damageReductionPercent;

        float actualDamage = damage * damageReduction;

        currentHealth = currentHealth - actualDamage;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        StartCoroutine(takeDamge());
    }
    private IEnumerator takeDamge()
    {
        takeDamage = true;
        PlayerMove.instance.spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.4f);
        PlayerMove.instance.spriteRenderer.color = Color.white;
        takeDamage = false;
    }


    public void Heal(float amount)
    {
        if (amount <= 0 || currentHealth >= maxHealth) return;

        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        Debug.Log($"Player healed {amount:F1}. Health: {currentHealth:F1}/{maxHealth:F1}");
    }

    public void GainExp(float amount)
    {
        if (amount <= 0) return;

        // Luck can provide small EXP bonus
        float bonusMultiplier = 1f + (luck * 0.02f); // 2% per luck point
        float actualExp = amount * bonusMultiplier;

        currentExp += actualExp;
        OnExpChanged?.Invoke(currentExp, maxExp);

        Debug.Log($"Gained {actualExp:F1} EXP (base: {amount:F1}). Current: {currentExp:F1}/{maxExp:F1}");

        // Check for level up
        while (currentExp >= maxExp)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentExp -= maxExp;
        level++;
        availablePoints += pointsPerLevel;
        // Store old values for comparison
        float oldMaxHealth = maxHealth;

        // Recalculate max health
        RecalculateMaxHealth();

        // Heal player on level up (but don't exceed new max health)
        currentHealth = Mathf.Min(currentHealth + healthPerLevel, maxHealth);

        UpdateMaxExp();

        // Invoke events
        OnLevelUp?.Invoke(level);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnExpChanged?.Invoke(currentExp, maxExp);
        OnStatChanged?.Invoke(availablePoints);

        Debug.Log($"Level Up! Now level {level}. Health increased from {oldMaxHealth:F1} to {maxHealth:F1}. Available points: {availablePoints}");
    }

    private void UpdateMaxExp()
    {
        maxExp = Mathf.Floor(100f * Mathf.Pow(expMultiplier, level - 1));
    }

    public enum StatType
    {
        Might = 1,
        Agility = 2,
        Intellect = 3,
        Endurance = 4,
        Luck = 5,
        Sin = 6
    }

    public bool CanUpgradeStat()
    {
        return availablePoints > 0;
    }

    public bool DecreaseState(StatType statType)
    {
        float oldMaxHealth = maxHealth;
        bool healthChanged = false;

        switch (statType)
        {
            case StatType.Might:
                might--;
                break;
            case StatType.Agility:
                agility--;
                break;
            case StatType.Intellect:
                intellect--;
                break;
            case StatType.Endurance:
                endurance--;
                RecalculateMaxHealth();
                healthChanged = true;
                break;
            case StatType.Luck:
                luck--;
                break;
            case StatType.Sin:
                sin--;
                break;
            default:
                Debug.LogError($"Invalid stat type: {statType}");
                return false;
        }

        availablePoints++;
        OnStatChanged?.Invoke(availablePoints);

        if (healthChanged)
        {
            // Increase current health proportionally when max health increases
            float healthIncrease = maxHealth - oldMaxHealth;
            currentHealth += healthIncrease;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        Debug.Log($"Upgraded {statType} to {GetStatValue(statType)}. Available points: {availablePoints}");
        return true;
    }

    public bool IncreceState(StatType statType)
    {
        if (!CanUpgradeStat())
        {
            return false;
        }

        float oldMaxHealth = maxHealth;
        bool healthChanged = false;

        switch (statType)
        {
            case StatType.Might:
                might++;
                break;
            case StatType.Agility:
                agility++;
                break;
            case StatType.Intellect:
                intellect++;
                break;
            case StatType.Endurance:
                endurance++;
                RecalculateMaxHealth();
                healthChanged = true;
                break;
            case StatType.Luck:
                luck++;
                break;
            case StatType.Sin:
                sin++;
                break;
            default:
                Debug.LogError($"Invalid stat type: {statType}");
                return false;
        }

        availablePoints--;
        OnStatChanged?.Invoke(availablePoints);

        if (healthChanged)
        {
            // Increase current health proportionally when max health increases
            float healthIncrease = maxHealth - oldMaxHealth;
            currentHealth += healthIncrease;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        Debug.Log($"Upgraded {statType} to {GetStatValue(statType)}. Available points: {availablePoints}");
        return true;
    }

    public int GetStatValue(StatType statType)
    {
        return statType switch
        {
            StatType.Might => might,
            StatType.Agility => agility,
            StatType.Intellect => intellect,
            StatType.Endurance => endurance,
            StatType.Luck => luck,
            StatType.Sin => sin,
            _ => 0
        };
    }

    public void Die()
    {
        deaths++;
        Debug.Log($"Player died! Total deaths: {deaths}");
        OnDeath?.Invoke();
    }

    public void Respawn()
    {
        if (!IsDead()) return; // FIXED: Logic was inverted
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnPlayerRespawn?.Invoke();
        Debug.Log("Player respawned with full health!");
    }

    // Utility methods
    public float GetHealthPercentage() => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    public float GetExpPercentage() => maxExp > 0 ? currentExp / maxExp : 0f;
    public bool IsDead() => currentHealth <= 0;

    // Critical chance based on Luck (for other systems to use)
    public float GetCriticalChance() => (luck * 0.5f);

    // Save/Load methods
    public PlayerData GetSaveData()
    {
        return new PlayerData
        {
            level = this.level,
            currentExp = this.currentExp,
            currentHealth = this.currentHealth,
            maxHealth = this.maxHealth,
            might = this.might,
            agility = this.agility,
            intellect = this.intellect,
            endurance = this.endurance,
            luck = this.luck,
            sin = this.sin,
            deaths = this.deaths,
            availablePoints = this.availablePoints
        };
    }

    public void LoadSaveData(PlayerData data)
    {
        level = data.level;
        currentExp = data.currentExp;
        currentHealth = data.currentHealth;
        maxHealth = data.maxHealth;
        might = data.might;
        agility = data.agility;
        intellect = data.intellect;
        endurance = data.endurance;
        luck = data.luck;
        sin = data.sin;
        deaths = data.deaths;
        availablePoints = data.availablePoints;

        UpdateMaxExp();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnExpChanged?.Invoke(currentExp, maxExp);
        OnStatChanged?.Invoke(availablePoints);
    }


    public void SetWeaponType(weaponType newWeapon)
    {
        currentWeapon = newWeapon;
        Debug.Log($"Weapon switched to: {currentWeapon}");
    }


}