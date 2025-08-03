using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemySpriteData
{
    [Header("Basic Info")]
    public string enemyName = "Generated Enemy";
    public Sprite mainSprite; // Main enemy sprite - REQUIRED
    public Sprite deadSprite; // Optional death sprite
    public Sprite meleeAttackSprite; // Optional melee attack sprite
    public Sprite rangedAttackSprite; // Optional ranged attack sprite

    [Header("Generated Stats")]
    public int points = 10; // Spawn cost
    public int weight = 1; // Spawn weight

    [Header("Auto-Generation Settings")]
    [Range(0.5f, 3f)] public float sizeMultiplier = 1f; // Scale enemy size
    [Range(0.1f, 5f)] public float speedMultiplier = 1f; // Movement speed modifier
    [Range(0.1f, 5f)] public float healthMultiplier = 1f; // Health modifier
    [Range(0.1f, 5f)] public float damageMultiplier = 1f; // Damage modifier
    public bool preferRanged = false; // Combat preference
    public Color enemyTint = Color.white; // Color tint for enemy
}

[System.Serializable]
public class AutoEnemyPool
{
    public List<EnemySpriteData> enemySprites = new List<EnemySpriteData>();
    public int MinLevel;
    public int MaxLevel;

    [Header("Pool Generation Settings")]
    public Vector2 healthRange = new Vector2(50f, 150f);
    public Vector2 damageRange = new Vector2(10f, 30f);
    public Vector2 speedRange = new Vector2(2f, 5f);
    public Vector2 attackRangeRange = new Vector2(1.5f, 6f);
}

public class AutoEnemySpawner : MonoBehaviour
{
    public static AutoEnemySpawner instance { get; private set; }

    [Header("Enemy Sprite Pools")]
    public List<AutoEnemyPool> enemyPools = new List<AutoEnemyPool>();

    [Header("Spawning Settings")]
    public Transform Player;
    public float SpawnMaxDistance = 10f;
    public float SpawnMinDistance = 5f;
    public float SpawnRate = 1f;

    [Header("Points System")]
    public float CurrentPoints;
    public PlayerStats PlayerData;
    public float PointsPerLevel = 100f;
    public float PointsRegenerationRate = 10f;

    [Header("Auto-Generation Base Stats")]
    public GameObject baseEnemyPrefab; // Optional: base prefab to clone
    public LayerMask playerLayer = 1;
    public LayerMask obstacleLayer = -1;
    public string targetTag = "Player";

    [Header("Debug")]
    public bool ShowDebugInfo = true;
    public bool saveGeneratedEnemies = false; // Save generated prefabs to project

    private float nextSpawnTime = 0f;
    private float lastPointsUpdate = 0f;
    private Dictionary<EnemySpriteData, GameObject> generatedEnemyCache = new Dictionary<EnemySpriteData, GameObject>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        if (Player == null)
        {
            Player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
    }

    void Start()
    {
        UpdateAvailablePoints();
        lastPointsUpdate = Time.time;

        // Pre-generate all enemy prefabs
        PreGenerateEnemies();
    }

    void Update()
    {
        // Regenerate points over time
        if (Time.time >= lastPointsUpdate + 1f)
        {
            CurrentPoints += PointsRegenerationRate;
            CurrentPoints = Mathf.Min(CurrentPoints, GetMaxPoints());
            lastPointsUpdate = Time.time;
        }

        // Try to spawn enemies
        SpawnEnemy();

        if (ShowDebugInfo)
        {
            Debug.Log($"Current Points: {CurrentPoints}, Player Level: {PlayerData?.Level}");
        }
    }

    private void PreGenerateEnemies()
    {
        foreach (var pool in enemyPools)
        {
            foreach (var spriteData in pool.enemySprites)
            {
                if (spriteData.mainSprite != null && !generatedEnemyCache.ContainsKey(spriteData))
                {
                    GameObject generatedEnemy = GenerateEnemyFromSprite(spriteData, pool);
                    generatedEnemyCache[spriteData] = generatedEnemy;

                    if (ShowDebugInfo)
                    {
                        Debug.Log($"Pre-generated enemy: {spriteData.enemyName}");
                    }
                }
            }
        }
    }

    private GameObject GenerateEnemyFromSprite(EnemySpriteData spriteData, AutoEnemyPool pool)
    {
        // Create base enemy GameObject
        GameObject enemy = new GameObject(spriteData.enemyName);

        // Add essential components
        SetupBasicComponents(enemy, spriteData);

        // Add and configure Enemy script
        Enamy enemyScript = enemy.AddComponent<Enamy>();
        ConfigureEnemyStats(enemyScript, spriteData, pool);

        // Create attack prefabs
        CreateAttackPrefabs(enemyScript, spriteData);

        // Make it a prefab (inactive)
        enemy.SetActive(false);

        return enemy;
    }

    private void SetupBasicComponents(GameObject enemy, EnemySpriteData spriteData)
    {
        // Add SpriteRenderer
        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        sr.sprite = spriteData.mainSprite;
        sr.color = spriteData.enemyTint;

        // Scale the enemy
        enemy.transform.localScale = Vector3.one * spriteData.sizeMultiplier;

        // Add Collider2D
        CircleCollider2D collider = enemy.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;

        // Add Rigidbody2D
        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 0f;
        rb.drag = 5f;

        // Create fire point
        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(enemy.transform);
        firePoint.transform.localPosition = Vector3.right * 0.5f;
    }

    private void ConfigureEnemyStats(Enamy enemyScript, EnemySpriteData spriteData, AutoEnemyPool pool)
    {
        // Basic info
        enemyScript.enemyName = spriteData.enemyName;
        enemyScript.enemyIcon = spriteData.mainSprite;
        enemyScript.deadIcon = spriteData.deadSprite;
        enemyScript.meleeAttackSprite = spriteData.meleeAttackSprite;
        enemyScript.rangedAttackSprite = spriteData.rangedAttackSprite;
        enemyScript.spriteRenderer = enemyScript.GetComponent<SpriteRenderer>();
        enemyScript.expGiven = spriteData.points * 2; // Give exp based on cost

        // Generate stats based on sprite data and pool settings
        float baseHealth = Random.Range(pool.healthRange.x, pool.healthRange.y) * spriteData.healthMultiplier;
        float baseDamage = Random.Range(pool.damageRange.x, pool.damageRange.y) * spriteData.damageMultiplier;
        float baseSpeed = Random.Range(pool.speedRange.x, pool.speedRange.y) * spriteData.speedMultiplier;
        float baseRange = Random.Range(pool.attackRangeRange.x, pool.attackRangeRange.y);

        // Health system
        enemyScript.maxHealth = baseHealth;
        enemyScript.currentHealth = baseHealth;
        enemyScript.DamageReduction = Random.Range(0f, 10f); // 0-10% damage reduction

        // Combat ranges
        enemyScript.MeleeAttackRange = baseRange * 0.3f;
        enemyScript.Rangeattack = baseRange;

        // Attack settings
        enemyScript.MeleeAttackDamage = baseDamage * 1.2f; // Melee does more damage
        enemyScript.RangeAttackDamage = baseDamage;
        enemyScript.MeleeAttackCooldown = Random.Range(1f, 2.5f);
        enemyScript.RangeAttackCooldown = Random.Range(0.8f, 2f);

        // Melee area settings
        enemyScript.meleeAreaRadius = enemyScript.MeleeAttackRange * 1.2f;
        enemyScript.meleeAreaDuration = 0.5f;

        // Ranged settings
        enemyScript.projectileSpeed = Random.Range(8f, 15f);
        enemyScript.projectileLifetime = 3f;
        enemyScript.firePoint = enemyScript.transform.Find("FirePoint");

        // AI settings
        enemyScript.moveSpeed = baseSpeed;
        enemyScript.preferRangedCombat = spriteData.preferRanged;
        enemyScript.idealCombatDistance = spriteData.preferRanged ? baseRange * 0.7f : enemyScript.MeleeAttackRange;
        enemyScript.chaseRange = baseRange * 2f;
        enemyScript.stoppingDistance = 0.8f;
        enemyScript.retreatDistance = 1.5f;

        // Layer settings
        enemyScript.playerLayer = playerLayer;
        enemyScript.obstacleLayer = obstacleLayer;
        enemyScript.TargetName = targetTag;

        // Visual settings
        enemyScript.attackSpriteScale = spriteData.sizeMultiplier * 1.2f;
        enemyScript.rotateAttackSprite = true;
        enemyScript.meleeAttackColor = Color.red;
        enemyScript.rangedAttackColor = Color.blue;
    }

    private void CreateAttackPrefabs(Enamy enemyScript, EnemySpriteData spriteData)
    {
        // Create melee attack prefab if sprite is provided
        if (spriteData.meleeAttackSprite != null)
        {
            enemyScript.meleeAreaPrefab = CreateAttackPrefab(
                "MeleeAttack_" + spriteData.enemyName,
                spriteData.meleeAttackSprite,
                enemyScript.meleeAttackColor,
                enemyScript.attackSpriteScale
            );
        }

        // Create ranged attack prefab if sprite is provided
        if (spriteData.rangedAttackSprite != null)
        {
            enemyScript.projectilePrefab = CreateAttackPrefab(
                "RangedAttack_" + spriteData.enemyName,
                spriteData.rangedAttackSprite,
                enemyScript.rangedAttackColor,
                enemyScript.attackSpriteScale * 0.8f
            );
        }
    }

    private GameObject CreateAttackPrefab(string name, Sprite sprite, Color color, float scale)
    {
        GameObject attackPrefab = new GameObject(name);

        // Add SpriteRenderer
        SpriteRenderer sr = attackPrefab.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = 10;

        // Add Collider
        CircleCollider2D collider = attackPrefab.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;

        // Scale
        attackPrefab.transform.localScale = Vector3.one * scale;

        // Make inactive
        attackPrefab.SetActive(false);

        return attackPrefab;
    }

    public void SpawnEnemy()
    {
        if (Time.time >= nextSpawnTime && PlayerData != null)
        {
            nextSpawnTime = Time.time + SpawnRate;

            AutoEnemyPool selectedPool = SelectEnemyPool();
            if (selectedPool != null)
            {
                EnemySpriteData selectedSpriteData = SelectEnemySpriteData(selectedPool);
                if (selectedSpriteData != null && generatedEnemyCache.ContainsKey(selectedSpriteData))
                {
                    Vector3 spawnPosition = GetRandomSpawnPosition();
                    GameObject enemyPrefab = generatedEnemyCache[selectedSpriteData];
                    GameObject spawnedEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
                    spawnedEnemy.SetActive(true);

                    // Deduct points
                    CurrentPoints -= selectedSpriteData.points;

                    if (ShowDebugInfo)
                    {
                        Debug.Log($"Spawned auto-generated {selectedSpriteData.enemyName} at {spawnPosition} for {selectedSpriteData.points} points");
                    }
                }
            }
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        float distance = Random.Range(SpawnMinDistance, SpawnMaxDistance);
        Vector3 spawnPosition = Player.position + Random.onUnitSphere * distance;
        spawnPosition.y = 0;
        return spawnPosition;
    }

    private EnemySpriteData SelectEnemySpriteData(AutoEnemyPool selectedPool)
    {
        if (selectedPool.enemySprites == null || selectedPool.enemySprites.Count == 0)
            return null;

        // Get all affordable enemies with sprites
        List<EnemySpriteData> affordableEnemies = new List<EnemySpriteData>();
        foreach (var spriteData in selectedPool.enemySprites)
        {
            if (spriteData.mainSprite != null && CurrentPoints >= spriteData.points)
            {
                affordableEnemies.Add(spriteData);
            }
        }

        if (affordableEnemies.Count == 0)
            return null;

        // Weighted random selection
        return SelectWeightedRandom(affordableEnemies);
    }

    private EnemySpriteData SelectWeightedRandom(List<EnemySpriteData> enemies)
    {
        float totalWeight = 0f;
        foreach (var enemy in enemies)
        {
            totalWeight += enemy.weight;
        }

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var enemy in enemies)
        {
            currentWeight += enemy.weight;
            if (randomValue <= currentWeight)
            {
                return enemy;
            }
        }

        return enemies[0];
    }

    private AutoEnemyPool SelectEnemyPool()
    {
        if (PlayerData == null || enemyPools == null || enemyPools.Count == 0)
            return null;

        foreach (var pool in enemyPools)
        {
            if (PlayerData.Level >= pool.MinLevel && PlayerData.Level <= pool.MaxLevel)
            {
                return pool;
            }
        }
        return null;
    }

    public void UpdateAvailablePoints()
    {
        if (PlayerData != null)
        {
            CurrentPoints = GetMaxPoints();
        }
    }

    public float GetMaxPoints()
    {
        return PlayerData != null ? PlayerData.Level * PointsPerLevel : 0f;
    }

    public void AddPoints(float points)
    {
        CurrentPoints = Mathf.Min(CurrentPoints + points, GetMaxPoints());
    }

    // Utility method to add new sprite data at runtime
    public void AddEnemySpriteData(EnemySpriteData spriteData, int poolIndex = 0)
    {
        if (poolIndex >= 0 && poolIndex < enemyPools.Count)
        {
            enemyPools[poolIndex].enemySprites.Add(spriteData);

            // Generate the enemy immediately
            if (spriteData.mainSprite != null && !generatedEnemyCache.ContainsKey(spriteData))
            {
                GameObject generatedEnemy = GenerateEnemyFromSprite(spriteData, enemyPools[poolIndex]);
                generatedEnemyCache[spriteData] = generatedEnemy;

                Debug.Log($"Added new enemy type: {spriteData.enemyName}");
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (Player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(Player.position, SpawnMinDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(Player.position, SpawnMaxDistance);
        }
    }
}