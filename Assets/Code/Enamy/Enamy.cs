using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Playables;
using UnityEngine;
using UnityEngine.AI;


public class Enamy : MonoBehaviour, IDamable
{
    [Header("Enemy Basic Info")]
    public string enemyName = "Enemy";
    public Sprite enemyIcon;
    [SerializeField] SpriteRenderer spriteRenderer;
    public int expGiven = 10;

    [Header("Health System")]
    public float currentHealth = 100f;
    public float maxHealth = 100f;
    public float DamageReduction = 1f;

    [Header("Combat Ranges")]
    public float Rangeattack = 5f; // Range for ranged attack
    public float MeleeAttackRange = 2f; // Range for melee attack

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

    [Header("Attack Detection")]
    public LayerMask playerLayer = 1; // What layer the player is on

    [Header("AI Navigation - Tilemap")]
    public float moveSpeed = 3f;
    public float stoppingDistance = 1.5f;
    public float retreatDistance = 1f;
    public float pathUpdateRate = 0.2f;
    public LayerMask obstacleLayer = -1; // What layers block movement
    public float raycastDistance = 0.6f; // Distance to check for obstacles

    [Header("AI Behavior")]
    public bool preferRangedCombat = true;
    public float idealCombatDistance = 4f;
    public float chaseRange = 10f;
    public float loseTargetTime = 5f;
    public float avoidanceForce = 5f; // How strong obstacle avoidance is
    public int pathfindingSteps = 8; // Number of directions to try when pathfinding

    // Components
    private Rigidbody2D rb2d;

    // Private variables
    private Transform player;
    private float lastMeleeAttackTime;
    private float lastRangeAttackTime;
    private bool isAttacking = false;
    private float lastPathUpdate;
    private float targetLostTimer;
    private bool hasLineOfSight;

    // Movement and pathfinding
    private Vector2 currentTarget;
    private Vector2 currentVelocity;
    private List<Vector2> currentPath;
    private int currentPathIndex;
    private bool isMoving;

    // Combat preference calculation
    private float optimalCombatRange;
    private float maxDamageWeaponRange;

    // Pathfinding directions (8-directional movement)
    private static readonly Vector2[] directions = {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right,
        new Vector2(1, 1).normalized, new Vector2(1, -1).normalized,
        new Vector2(-1, 1).normalized, new Vector2(-1, -1).normalized
    };

    // Events
    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;
    public event Action OnMeleeAttack;
    public event Action OnRangedAttack;

    private void Start()
    {
        // Initialize health
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        spriteRenderer.sprite = enemyIcon;

        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        if (firePoint == null)
        {
            firePoint = transform;
        }

        SetupRigidbody2D();
        CalculateOptimalCombatRange();

        currentTarget = transform.position;
    }

    private void SetupRigidbody2D()
    {
        rb2d = GetComponent<Rigidbody2D>();
        if (rb2d == null)
        {
            rb2d = gameObject.AddComponent<Rigidbody2D>();
        }

        rb2d.freezeRotation = true;
        rb2d.gravityScale = 0f; // No gravity for top-down movement
        rb2d.drag = 5f; // Add some drag for smoother movement
    }

    private void CalculateOptimalCombatRange()
    {
        float meleeDPS = MeleeAttackDamage / MeleeAttackCooldown;
        float rangedDPS = RangeAttackDamage / RangeAttackCooldown;

        if (meleeDPS > rangedDPS)
        {
            maxDamageWeaponRange = MeleeAttackRange;
            optimalCombatRange = MeleeAttackRange * 0.8f;
            preferRangedCombat = false;
        }
        else
        {
            maxDamageWeaponRange = Rangeattack;
            optimalCombatRange = Rangeattack * 0.7f;
            preferRangedCombat = true;
        }

        if (idealCombatDistance > 0)
        {
            optimalCombatRange = idealCombatDistance;
        }

        Debug.Log($"{enemyName}: Melee DPS: {meleeDPS:F1}, Ranged DPS: {rangedDPS:F1}, Optimal Range: {optimalCombatRange:F1}");
    }

    private void Update()
    {
        if (player != null && !IsDead())
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
    }

    private void FixedUpdate()
    {
        if (isMoving && !isAttacking)
        {
            MoveTowardsTarget();
        }
    }

    private void UpdateAI(float distanceToPlayer)
    {
        hasLineOfSight = CheckLineOfSight();

        if (!isAttacking)
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

        // Determine desired positioning
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
        rb2d.velocity = Vector2.zero;
    }

    private void MoveTowardsTarget()
    {
        Vector2 currentPos = transform.position;
        Vector2 directionToTarget = (currentTarget - currentPos).normalized;
        float distanceToTarget = Vector2.Distance(currentPos, currentTarget);

        // Stop if close enough to target
        if (distanceToTarget <= stoppingDistance)
        {
            StopMovement();
            return;
        }

        // Check for obstacles in the direct path
        Vector2 desiredVelocity = directionToTarget * moveSpeed;
        Vector2 avoidanceForceVec = CalculateAvoidanceForce(currentPos, directionToTarget);

        // Combine desired movement with obstacle avoidance
        Vector2 finalDirection = (desiredVelocity + avoidanceForceVec).normalized;

        // Apply movement
        rb2d.velocity = finalDirection * moveSpeed;
    }

    private Vector2 CalculateAvoidanceForce(Vector2 currentPos, Vector2 desiredDirection)
    {
        Vector2 avoidanceForceVec = Vector2.zero;

        // Check multiple directions for obstacles
        for (int i = 0; i < pathfindingSteps; i++)
        {
            float angle = (360f / pathfindingSteps) * i;
            Vector2 checkDirection = Quaternion.Euler(0, 0, angle) * Vector2.up;

            RaycastHit2D hit = Physics2D.Raycast(currentPos, checkDirection, raycastDistance, obstacleLayer);

            if (hit.collider != null)
            {
                // Calculate repulsion force away from obstacle
                Vector2 awayFromObstacle = (currentPos - hit.point).normalized;
                float distance = hit.distance;
                float forceStrength = avoidanceForce * (raycastDistance - distance) / raycastDistance;

                avoidanceForceVec += awayFromObstacle * forceStrength;
            }
        }

        // Also check the desired direction specifically
        RaycastHit2D directHit = Physics2D.Raycast(currentPos, desiredDirection, raycastDistance, obstacleLayer);
        if (directHit.collider != null)
        {
            // Strong avoidance for direct obstacles
            Vector2 perpendicular = Vector2.Perpendicular(desiredDirection);

            // Choose left or right based on which is more clear
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
                // Both sides clear, choose randomly or based on previous movement
                avoidanceForceVec += perpendicular * avoidanceForce * (Mathf.Sin(Time.time) > 0 ? 1 : -1);
            }
        }

        return avoidanceForceVec;
    }

    private bool CheckLineOfSight()
    {
        if (player == null) return false;

        Vector2 directionToPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Use a slightly smaller distance to avoid edge cases
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, distanceToPlayer - 0.1f, obstacleLayer);

        bool hasLOS = hit.collider == null;

        // Debug line of sight
        Debug.Log($"{enemyName} LOS check: {hasLOS}, Hit: {(hit.collider != null ? hit.collider.name : "none")}");

        return hasLOS;
    }

    private Vector2 GetPositionTowardsPlayer(float desiredDistance)
    {
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
        Vector2 directionToPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
        Vector2 perpendicular = Vector2.Perpendicular(directionToPlayer);

        float strafeDirection = Mathf.Sin(Time.time * 0.5f) > 0 ? 1f : -1f;
        return (Vector2)transform.position + perpendicular * strafeDirection * 2f;
    }

    private void UpdateCombat(float distanceToPlayer)
    {
        Debug.Log($"{enemyName}: Distance={distanceToPlayer:F2}, Ranged={Rangeattack:F2}, Melee={MeleeAttackRange:F2}, LOS={hasLineOfSight}, PreferRanged={preferRangedCombat}");

        // SIMPLE TEST VERSION - Just try ranged attack if in range
        if (distanceToPlayer <= Rangeattack)
        {
            TryRangedAttack();
        }

        // Also try melee if very close
        if (distanceToPlayer <= MeleeAttackRange)
        {
            TryMeleeAttack();
        }
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
        Debug.Log($"{enemyName} trying ranged attack - Cooldown ready: {Time.time >= lastRangeAttackTime + RangeAttackCooldown}");

        if (Time.time >= lastRangeAttackTime + RangeAttackCooldown)
        {
            Debug.Log($"{enemyName} FIRING ranged attack!");
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
        if (meleeAreaPrefab != null)
        {
            damageArea = Instantiate(meleeAreaPrefab, attackPosition, Quaternion.identity);
        }

        yield return new WaitForSeconds(0.1f);

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(attackPosition, meleeAreaRadius, playerLayer);
        foreach (Collider2D hitCollider in hitColliders)
        {
            IDamable damageTarget = hitCollider.GetComponent<IDamable>();
            if (damageTarget != null)
            {
                damageTarget.TakeDamage(MeleeAttackDamage);
                Debug.Log($"{enemyName} dealt {MeleeAttackDamage} melee damage to {hitCollider.name}");
            }
        }

        yield return new WaitForSeconds(meleeAreaDuration - 0.1f);

        if (damageArea != null)
        {
            Destroy(damageArea);
        }

        isAttacking = false;
    }

    private void PerformRangedAttack()
    {
        OnRangedAttack?.Invoke();

        if (projectilePrefab != null && player != null)
        {
            Vector2 direction = ((Vector2)player.position - (Vector2)firePoint.position).normalized;

            // Calculate 2D rotation angle
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            // Spawn projectile
            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, rotation);

            // Try to initialize with EnemyProjectile script first
            EnemyProjectile projectileScript = projectile.GetComponent<EnemyProjectile>();
            if (projectileScript != null)
            {
                projectileScript.Initialize(direction, projectileSpeed, RangeAttackDamage, playerLayer);
                Debug.Log($"{enemyName} fired EnemyProjectile at {player.name} (LOS: {hasLineOfSight})");
            }
            else
            {
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
                basicProjectile.Initialize(RangeAttackDamage, playerLayer);

                Debug.Log($"{enemyName} fired BasicProjectile at {player.name} (LOS: {hasLineOfSight})");
            }
        }
        else
        {
            if (projectilePrefab == null)
                Debug.LogWarning($"{enemyName} projectilePrefab is null!");
            if (player == null)
                Debug.LogWarning($"{enemyName} player reference is null!");
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
        if (damage <= 0 || currentHealth <= 0) return;

        float actualDamage = damage;
        float damageReduction = 1f - (DamageReduction * 0.01f);
        actualDamage *= damageReduction;

        currentHealth = Mathf.Clamp(currentHealth - actualDamage, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        Debug.Log($"{enemyName} took {actualDamage:F1} damage (reduced from {damage:F1}). Health: {currentHealth:F1}/{maxHealth:F1}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public bool IsDead() => currentHealth <= 0;

    private void Die()
    {
        if (IsDead()) return;

        if (PlayerStats.instance != null)
        {
            PlayerStats.instance.GainExp(expGiven);
        }

        OnDeath?.Invoke();
        Debug.Log($"{enemyName} died and gave {expGiven} experience!");
        Destroy(gameObject, 0.5f);
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

    // Public methods
    public void SetTarget(Transform newTarget)
    {
        player = newTarget;
        targetLostTimer = 0f;
    }

    public void ForceAttack()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= MeleeAttackRange)
        {
            TryMeleeAttack();
        }
        else if (distanceToPlayer <= Rangeattack)
        {
            TryRangedAttack();
        }
    }

    public float GetOptimalCombatRange()
    {
        return optimalCombatRange;
    }

    public bool IsInOptimalRange()
    {
        if (player == null) return false;

        float distance = Vector2.Distance(transform.position, player.position);
        return Mathf.Abs(distance - optimalCombatRange) <= 1f;
    }
}