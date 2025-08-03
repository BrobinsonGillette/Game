using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;


public class Enamy : MonoBehaviour, IDamable
{

    [Header("Enemy Basic Info")]
    public string enemyName = "Enemy";

    [Header("Sprite Configuration - Auto-Generated")]
    public Sprite enemyIcon;
    public Sprite deadIcon;
    public Sprite meleeAttackSprite;
    public Sprite rangedAttackSprite;
    [SerializeField] public SpriteRenderer spriteRenderer; // Made public for auto-setup

    [Header("Attack Visual Settings - Auto-Generated")]
    public Color meleeAttackColor = Color.red;
    public Color rangedAttackColor = Color.blue;
    public float attackSpriteScale = 1.5f;
    public bool rotateAttackSprite = true;

    public int expGiven = 10;

    [Header("Health System - Auto-Generated")]
    public float currentHealth = 100f;
    public float maxHealth = 100f;
    public float DamageReduction = 1f;

    [Header("Combat Ranges - Auto-Generated")]
    public float Rangeattack = 5f;
    public float MeleeAttackRange = 2f;

    [Header("Melee Attack Settings - Auto-Generated")]
    public float MeleeAttackDamage = 10f;
    public float MeleeAttackCooldown = 1f;
    public float meleeAreaRadius = 1.5f;
    public float meleeAreaDuration = 0.5f;
    public GameObject meleeAreaPrefab; // Auto-created from sprite

    [Header("Ranged Attack Settings - Auto-Generated")]
    public float RangeAttackDamage = 10f;
    public float RangeAttackCooldown = 1f;
    public GameObject projectilePrefab; // Auto-created from sprite
    public Transform firePoint; // Auto-created
    public float projectileSpeed = 10f;
    public float projectileLifetime = 3f;

    [Header("Attack Detection - Auto-Generated")]
    public LayerMask playerLayer = 1;

    [Header("AI Navigation - Auto-Generated")]
    public float moveSpeed = 3f;
    public float stoppingDistance = 1.5f;
    public float retreatDistance = 1f;
    public float pathUpdateRate = 0.2f;
    public LayerMask obstacleLayer = -1;
    public float raycastDistance = 0.6f;

    [Header("AI Behavior - Auto-Generated")]
    public bool preferRangedCombat = true;
    public float idealCombatDistance = 4f;
    public float chaseRange = 10f;
    public float loseTargetTime = 5f;
    public float avoidanceForce = 5f;
    public int pathfindingSteps = 8;

    // Components
    private Rigidbody2D rb2d;

    // Private variables
    public Transform player;
    public String TargetName = "Player";
    private float lastMeleeAttackTime;
    private float lastRangeAttackTime;
    private bool isAttacking = false;
    private float lastPathUpdate;
    private float targetLostTimer;
    private bool hasLineOfSight;
    private bool hasGivenExp = false;
    bool takeDamage = false;

    // Movement and pathfinding
    private Vector2 currentTarget;
    private bool isMoving;

    // Combat preference calculation
    private float optimalCombatRange;

    // Events
    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;
    public event Action OnMeleeAttack;
    public event Action OnRangedAttack;

    private void Start()
    {
        InitializeEnemy();
    }

    private void InitializeEnemy()
    {
        // Initialize health
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Set up sprite (should already be configured by spawner)
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Find target
        findtarget();

        // Set up fire point (should be created by spawner)
        if (firePoint == null)
        {
            // Create fire point if not assigned
            GameObject firePointObj = new GameObject("FirePoint");
            firePointObj.transform.SetParent(transform);
            firePointObj.transform.localPosition = Vector3.right * 0.5f;
            firePoint = firePointObj.transform;
        }

        // Set up physics
        SetupRigidbody2D();

        // Calculate combat preferences
        CalculateOptimalCombatRange();

        currentTarget = transform.position;

        if (ShowDebugInfo())
        {
            Debug.Log($"Initialized auto-generated enemy: {enemyName}");
        }
        spriteRenderer.sprite = enemyIcon;
    }

    private bool ShowDebugInfo()
    {
        return AutoEnemySpawner.instance != null && AutoEnemySpawner.instance.ShowDebugInfo;
    }

    private void findtarget()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(TargetName);
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    private void SetupRigidbody2D()
    {
        rb2d = GetComponent<Rigidbody2D>();
        if (rb2d == null)
        {
            rb2d = gameObject.AddComponent<Rigidbody2D>();
        }

        rb2d.freezeRotation = true;
        rb2d.gravityScale = 0f;
        rb2d.drag = 5f;
    }

    private void CalculateOptimalCombatRange()
    {
        float meleeDPS = MeleeAttackDamage / MeleeAttackCooldown;
        float rangedDPS = RangeAttackDamage / RangeAttackCooldown;

        if (meleeDPS > rangedDPS && !preferRangedCombat)
        {
            optimalCombatRange = MeleeAttackRange * 0.8f;
        }
        else
        {
            optimalCombatRange = Rangeattack * 0.7f;
        }

        if (idealCombatDistance > 0)
        {
            optimalCombatRange = idealCombatDistance;
        }

        if (ShowDebugInfo())
        {
            Debug.Log($"{enemyName}: Melee DPS: {meleeDPS:F1}, Ranged DPS: {rangedDPS:F1}, Optimal Range: {optimalCombatRange:F1}");
        }
    }

    private void Update()
    {
        if (IsDead && !hasGivenExp)
        {
            Die();
            return;
        }

        if (player != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            if (distanceToPlayer <= chaseRange)
            {
                targetLostTimer = 0f;
                UpdateAI(distanceToPlayer);
            }
            else
            {
                targetLostTimer += Time.deltaTime;
                if (targetLostTimer >= loseTargetTime)
                {
                    StopMovement();
                }
            }
        }
        else
        {
            findtarget();
        }
    }

    private void FixedUpdate()
    {
        if (isMoving && !isAttacking && !IsDead)
        {
            MoveTowardsTarget();
        }
    }

    private void UpdateAI(float distanceToPlayer)
    {
        hasLineOfSight = CheckLineOfSight();

        if (!isAttacking && !IsDead)
        {
            UpdateMovement(distanceToPlayer);
            UpdateCombat(distanceToPlayer);
        }
    }

    private void UpdateMovement(float distanceToPlayer)
    {
        if (Time.time - lastPathUpdate < pathUpdateRate) return;
        lastPathUpdate = Time.time;

        Vector2 targetPosition = player.position;

        if (preferRangedCombat || !hasLineOfSight)
        {
            if (distanceToPlayer > optimalCombatRange + 1f)
            {
                targetPosition = GetPositionTowardsPlayer(optimalCombatRange);
            }
            else if (distanceToPlayer < optimalCombatRange - 1f)
            {
                targetPosition = GetRetreatPosition();
            }
            else
            {
                targetPosition = GetStrafePosition();
            }
        }
        else
        {
            if (distanceToPlayer > MeleeAttackRange)
            {
                targetPosition = GetPositionTowardsPlayer(MeleeAttackRange * 0.5f);
            }
        }

        SetMovementTarget(targetPosition);
    }

    private void SetMovementTarget(Vector2 target)
    {
        currentTarget = target;
        isMoving = true;
    }

    private void StopMovement()
    {
        isMoving = false;
        if (rb2d != null)
        {
            rb2d.velocity = Vector2.zero;
        }
    }

    private void MoveTowardsTarget()
    {
        Vector2 currentPos = transform.position;
        Vector2 directionToTarget = (currentTarget - currentPos).normalized;
        float distanceToTarget = Vector2.Distance(currentPos, currentTarget);

        if (distanceToTarget <= stoppingDistance)
        {
            StopMovement();
            return;
        }

        Vector2 desiredVelocity = directionToTarget * moveSpeed;
        Vector2 avoidanceForceVec = CalculateAvoidanceForce(currentPos, directionToTarget);

        Vector2 finalDirection = (desiredVelocity + avoidanceForceVec).normalized;
        rb2d.velocity = finalDirection * moveSpeed;
    }

    private Vector2 CalculateAvoidanceForce(Vector2 currentPos, Vector2 desiredDirection)
    {
        Vector2 avoidanceForceVec = Vector2.zero;

        for (int i = 0; i < pathfindingSteps; i++)
        {
            float angle = (360f / pathfindingSteps) * i;
            Vector2 checkDirection = Quaternion.Euler(0, 0, angle) * Vector2.up;

            RaycastHit2D hit = Physics2D.Raycast(currentPos, checkDirection, raycastDistance, obstacleLayer);

            if (hit.collider != null)
            {
                Vector2 awayFromObstacle = (currentPos - hit.point).normalized;
                float distance = hit.distance;
                float forceStrength = avoidanceForce * (raycastDistance - distance) / raycastDistance;

                avoidanceForceVec += awayFromObstacle * forceStrength;
            }
        }

        RaycastHit2D directHit = Physics2D.Raycast(currentPos, desiredDirection, raycastDistance, obstacleLayer);
        if (directHit.collider != null)
        {
            Vector2 perpendicular = Vector2.Perpendicular(desiredDirection);

            RaycastHit2D leftHit = Physics2D.Raycast(currentPos, perpendicular, raycastDistance, obstacleLayer);
            RaycastHit2D rightHit = Physics2D.Raycast(currentPos, -perpendicular, raycastDistance, obstacleLayer);

            if (leftHit.collider == null && rightHit.collider != null)
            {
                avoidanceForceVec += perpendicular * avoidanceForce;
            }
            else if (rightHit.collider == null && leftHit.collider != null)
            {
                avoidanceForceVec += -perpendicular * avoidanceForce;
            }
            else if (leftHit.collider == null && rightHit.collider == null)
            {
                avoidanceForceVec += perpendicular * avoidanceForce * (Mathf.Sin(Time.time) > 0 ? 1 : -1);
            }
        }

        return avoidanceForceVec;
    }

    private bool CheckLineOfSight()
    {
        if (player == null)
        {
            findtarget();
            return false;
        }

        Vector2 directionToPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, distanceToPlayer - 0.1f, obstacleLayer);
        return hit.collider == null;
    }

    private Vector2 GetPositionTowardsPlayer(float desiredDistance)
    {
        if (player == null)
        {
            findtarget();
            return Vector2.zero;
        }
        Vector2 directionToPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
        return (Vector2)player.position - directionToPlayer * desiredDistance;
    }

    private Vector2 GetRetreatPosition()
    {
        Vector2 directionFromPlayer = ((Vector2)transform.position - (Vector2)player.position).normalized;
        return (Vector2)transform.position + directionFromPlayer * retreatDistance;
    }

    private Vector2 GetStrafePosition()
    {
        if (player == null)
        {
            findtarget();
            return Vector2.zero;
        }
        Vector2 directionToPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
        Vector2 perpendicular = Vector2.Perpendicular(directionToPlayer);

        float strafeDirection = Mathf.Sin(Time.time * 0.5f) > 0 ? 1f : -1f;
        return (Vector2)transform.position + perpendicular * strafeDirection * 2f;
    }

    private void UpdateCombat(float distanceToPlayer)
    {
        if (distanceToPlayer <= Rangeattack && !IsDead)
        {
            TryRangedAttack();
        }

        if (distanceToPlayer <= MeleeAttackRange && !IsDead)
        {
            TryMeleeAttack();
        }
    }

    private void TryMeleeAttack()
    {
        if (Time.time >= lastMeleeAttackTime + MeleeAttackCooldown && !IsDead)
        {
            StartCoroutine(PerformMeleeAttack());
            lastMeleeAttackTime = Time.time;
        }
    }

    private void TryRangedAttack()
    {
        if (Time.time >= lastRangeAttackTime + RangeAttackCooldown && !IsDead)
        {
            PerformRangedAttack();
            lastRangeAttackTime = Time.time;
        }
    }

    private IEnumerator PerformMeleeAttack()
    {
        isAttacking = true;
        OnMeleeAttack?.Invoke();

        StopMovement();

        Vector3 attackPosition = transform.position;
        GameObject damageArea = null;

        // Create or instantiate melee attack visual
        if (meleeAreaPrefab != null && !IsDead)
        {
            damageArea = Instantiate(meleeAreaPrefab, attackPosition, Quaternion.identity);
            damageArea.SetActive(true);
            yield return ChangeSprite(meleeAttackSprite, 0.2f);
            // Rotate toward player if enabled
            if (rotateAttackSprite && player != null)
            {
                Vector2 direction = (player.position - transform.position).normalized;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                damageArea.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }

            // Set up projectile component for lifetime management
            BasicProjectile projectile = damageArea.GetComponent<BasicProjectile>();
            if (projectile == null)
            {
                projectile = damageArea.AddComponent<BasicProjectile>();
            }
            projectile.Initialize(0, 0, meleeAreaDuration);

            // Deal damage to nearby enemies
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(attackPosition, meleeAreaRadius, playerLayer);
            foreach (Collider2D hitCollider in hitColliders)
            {
                IDamable damageTarget = hitCollider.GetComponent<IDamable>();
                if (damageTarget != null)
                {
                    damageTarget.TakeDamage(MeleeAttackDamage);
                    if (ShowDebugInfo())
                    {
                        Debug.Log($"{enemyName} dealt {MeleeAttackDamage} melee damage to {hitCollider.name}");
                    }
                }
            }

            yield return new WaitForSeconds(meleeAreaDuration);

            if (damageArea != null)
            {
                Destroy(damageArea);
            }
        }

        isAttacking = false;
    }

    private void PerformRangedAttack()
    {
        OnRangedAttack?.Invoke();
        StartCoroutine(ChangeSprite(rangedAttackSprite, 0.2f));
        if (projectilePrefab != null && player != null && !IsDead)
        {
            Vector2 direction = ((Vector2)player.position - (Vector2)firePoint.position).normalized;

            // Calculate rotation
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion rotation = rotateAttackSprite ? Quaternion.AngleAxis(angle, Vector3.forward) : Quaternion.identity;

            // Spawn projectile
            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, rotation);
            projectile.SetActive(true);

            // Set up BasicProjectile component
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
            basicProjectile.Initialize(RangeAttackDamage, playerLayer, projectileLifetime);

            if (ShowDebugInfo())
            {
                Debug.Log($"{enemyName} fired projectile at {player.name}");
            }
        }
    }
    private IEnumerator ChangeSprite(Sprite newSprite, float duration)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = newSprite;
            yield return new WaitForSeconds(duration);
            spriteRenderer.sprite = enemyIcon; // Reset to original icon
        }
    }
    // Health and damage system
    public float Health
    {
        get => currentHealth;
        set => currentHealth = Mathf.Clamp(value, 0, maxHealth);
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0 || currentHealth <= 0 && !takeDamage) return;

        float actualDamage = damage;
        float damageReduction = 1f - (DamageReduction * 0.01f);
        actualDamage *= damageReduction;

        currentHealth = currentHealth - actualDamage;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (ShowDebugInfo())
        {
            Debug.Log($"{enemyName} took {actualDamage:F1} damage (reduced from {damage:F1}). Health: {currentHealth:F1}/{maxHealth:F1}");
        }

        if (IsDead)
        {
            Die();
        }
        StartCoroutine(takeDamge());
    }

    private IEnumerator takeDamge()
    {
        takeDamage = true;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
        }
        yield return new WaitForSeconds(0.4f);
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
        takeDamage = false;
    }

    public bool IsDead => currentHealth <= 0;

    private void Die()
    {
        if (deadIcon != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = deadIcon;
        }

        if (PlayerStats.instance != null && !hasGivenExp)
        {
            hasGivenExp = true;
            PlayerStats.instance.GainExp(expGiven);
            if (ShowDebugInfo())
            {
                Debug.Log($"{enemyName} gave {expGiven} exp to player");
            }
        }

        OnDeath?.Invoke();
        Destroy(gameObject, 1.5f);
    }

    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        // Combat ranges
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, MeleeAttackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, Rangeattack);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, optimalCombatRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, meleeAreaRadius);

        // Line of sight
        if (player != null && Application.isPlaying)
        {
            Gizmos.color = hasLineOfSight ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, player.position);
        }

        // Current target
        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(currentTarget, 0.5f);
            Gizmos.DrawLine(transform.position, currentTarget);
        }

        // Obstacle detection rays
        if (Application.isPlaying)
        {
            Gizmos.color = Color.white;
            for (int i = 0; i < pathfindingSteps; i++)
            {
                float angle = (360f / pathfindingSteps) * i;
                Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.up;
                Gizmos.DrawRay(transform.position, direction * raycastDistance);
            }
        }
    }

}