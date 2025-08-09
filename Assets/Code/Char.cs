using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Team { none, player, enemy , neutral , other };
public class Char : MonoBehaviour
{
    [Header("Character Properties")]
    public string characterName = "Player";
    public Team team = Team.none;
    public int moveSpeed = 3;
    private float movementAnimationSpeed = 2f;

    public HexTile currentHex { get; private set; }
    public bool isMoving { get; private set; }
    public int remainingMoves { get; private set; }

    [Header("Visual")]
    public SpriteRenderer characterRenderer;



    private TMPro.TextMeshPro moveCounterText;
    private TMPro.TextMeshPro healthText;




    [Header("Action System")]
    public List<BaseAction> availableActions = new List<BaseAction>();
    public int maxActionPoints = 2;
    public int currentActionPoints;

    [Header("Resources")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float maxMana = 50f;
    public float currentMana;

    // Events
    public System.Action<Char> OnMovement;
    public System.Action<Char> TurnStart;
    public System.Action<Char> OnTurnEnd;
    public System.Action<Char> OnHealthChanged;
    public System.Action<Char> OnManaChanged;
    private void Start()
    {
        // Initialize character
        if (characterRenderer == null)
            characterRenderer = GetComponent<SpriteRenderer>();


        remainingMoves = moveSpeed;

        // Create move counter UI
        InitializeCharacter();
    }
    private void InitializeCharacter()
    {
        if (characterRenderer == null)
            characterRenderer = GetComponent<SpriteRenderer>();

        // Initialize resources
        currentHealth = maxHealth;
        currentMana = maxMana;
        currentActionPoints = maxActionPoints;
        remainingMoves = moveSpeed;

        // Create UI elements
        CreateMoveCounter();
        CreateHealthDisplay();

        // Initialize actions
        InitializeActions();
    }

    public void InitializeActions()
    {
        currentActionPoints = maxActionPoints;

        // Load default actions if none assigned
        if (availableActions.Count == 0)
        {
            LoadDefaultActions();
        }
    }
    private void LoadDefaultActions()
    {
      
       // Create basic attack
        AttackAction basicAttack = ScriptableObject.CreateInstance<AttackAction>();
        basicAttack.actionName = "Basic Attack";
        basicAttack.description = "A simple melee attack";
        basicAttack.damage = 15f;
        basicAttack.range = 1;
        basicAttack.actionPointCost = 1;
        basicAttack.actionType = ActionType.Attack;
        availableActions.Add(basicAttack);

        // Create defend action
        DefensiveAction defend = ScriptableObject.CreateInstance<DefensiveAction>();
        defend.actionName = "Defend";
        defend.description = "Reduce incoming damage";
        defend.damageReduction = 0.5f;
        defend.duration = 1;
        defend.actionPointCost = 1;
        defend.actionType = ActionType.Block;
        defend.requiresTarget = false;
        availableActions.Add(defend);

        // Create dodge action
        DefensiveAction dodge = ScriptableObject.CreateInstance<DefensiveAction>();
        dodge.actionName = "Dodge";
        dodge.description = "Increase evasion chance";
        dodge.damageReduction = 0.2f;
        dodge.increasesEvasion = true;
        dodge.evasionBonus = 0.3f;
        dodge.duration = 1;
        dodge.actionPointCost = 1;
        dodge.actionType = ActionType.Dodge;
        dodge.requiresTarget = false;
        availableActions.Add(dodge);

        // Create spell action (if character can cast)
        if (maxMana > 0)
        {
            SpellAction fireball = ScriptableObject.CreateInstance<SpellAction>();
            fireball.actionName = "Fireball";
            fireball.description = "Launch a fireball at target";
            fireball.manaCost = 15f;
            fireball.range = 3;
            fireball.actionPointCost = 1;
            fireball.actionType = ActionType.CastSpell;
            fireball.canTargetEnemies = true;
            fireball.spellEffect = SpellEffect.Damage;
            availableActions.Add(fireball);
        }
    }
    private void Update()
    {
        getTileOnGround();
        UpdateMoveCounter();
        UpdateHealthDisplay();
    }

    void CreateMoveCounter()
    {
        GameObject counterObj = new GameObject("MoveCounter2");
        counterObj.transform.SetParent(transform);
        counterObj.transform.localPosition = new Vector3(0, 0.8f, 0);

        moveCounterText = counterObj.AddComponent<TMPro.TextMeshPro>();
        moveCounterText.text = $"{remainingMoves}/{moveSpeed}";
        moveCounterText.fontSize = 2;
        moveCounterText.alignment = TMPro.TextAlignmentOptions.Center;
        moveCounterText.sortingOrder = 10;
    }
    void CreateHealthDisplay()
    {
        GameObject healthObj = new GameObject("HealthDisplay");
        healthObj.transform.SetParent(transform);
        healthObj.transform.localPosition = new Vector3(0, -0.8f, 0);

        healthText = healthObj.AddComponent<TMPro.TextMeshPro>();
        healthText.text = $"{currentHealth:F0}/{maxHealth:F0}";
        healthText.fontSize = 1.5f;
        healthText.alignment = TMPro.TextAlignmentOptions.Center;
        healthText.sortingOrder = 10;
        healthText.color = Color.red;
    }

    void UpdateMoveCounter()
    {
        if (moveCounterText != null)
        {
            moveCounterText.text = $"{remainingMoves}/{moveSpeed} | AP:{currentActionPoints}";

            if (remainingMoves == 0 && currentActionPoints == 0)
                moveCounterText.color = Color.red;
            else if (remainingMoves <= moveSpeed / 2 || currentActionPoints <= maxActionPoints / 2)
                moveCounterText.color = Color.yellow;
            else
                moveCounterText.color = Color.green;
        }
    }
    void UpdateHealthDisplay()
    {
        if (healthText != null)
        {
            healthText.text = $"{currentHealth:F0}/{maxHealth:F0}";

            float healthPercent = currentHealth / maxHealth;
            if (healthPercent > 0.6f)
                healthText.color = Color.green;
            else if (healthPercent > 0.3f)
                healthText.color = Color.yellow;
            else
                healthText.color = Color.red;
        }
    }

    void getTileOnGround()
    {
        if (currentHex == null)
        {
            MapMaker mapMaker = MapMaker.instance;
            if (mapMaker == null) return;
            Vector3 position = transform.position;
            Vector2Int hexCoords = mapMaker.WorldToHexPosition(position);
            if (mapMaker.hexTiles.TryGetValue(hexCoords, out HexTile tile))
            {
                currentHex = tile;
                currentHex.SetCurrentPlayer(this);
            }
        }
    }

 

    public void StartTurn()
    {
        remainingMoves = moveSpeed;
        currentActionPoints = maxActionPoints;

        // Regenerate some mana each turn
        currentMana = Mathf.Min(maxMana, currentMana + 5f);

        TurnStart?.Invoke(this);
        Debug.Log($"{characterName}'s turn started. Moves: {remainingMoves}, AP: {currentActionPoints}");

        StartCoroutine(TurnStartAnimation());
    }

    public void EndTurn()
    {
        remainingMoves = 0;
        currentActionPoints = 0;

        // Process any end-of-turn effects (like defensive buff countdown)
        DefensiveBuff buff = GetComponent<DefensiveBuff>();
        if (buff != null)
        {
            buff.DecrementTurn();
        }

        OnTurnEnd?.Invoke(this);
        Debug.Log($"{characterName}'s turn ended.");
    }

    public bool CanUseAction(BaseAction action)
    {
        if (action == null) return false;

        // Check action points
        if (currentActionPoints < action.actionPointCost) return false;

        // Check mana for spells
        if (action is SpellAction spell && currentMana < spell.manaCost) return false;

        return action.CanExecute(this);
    }

    public void SpendActionPoints(int cost)
    {
        currentActionPoints = Mathf.Max(0, currentActionPoints - cost);
    }

    public void SpendMana(float cost)
    {
        currentMana = Mathf.Max(0, currentMana - cost);
        OnManaChanged?.Invoke(this);
    }

    public void TakeDamage(float damage)
    {
        // Apply defensive buffs
        DefensiveBuff buff = GetComponent<DefensiveBuff>();
        if (buff != null)
        {
            float reduction = buff.GetDamageReduction();
            damage *= (1f - reduction);
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        OnHealthChanged?.Invoke(this);

        Debug.Log($"{characterName} takes {damage} damage! Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }

        // Visual damage effect
        StartCoroutine(DamageFlashEffect());
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(this);
        Debug.Log($"{characterName} heals for {amount}! Health: {currentHealth}/{maxHealth}");
    }

    private void Die()
    {
        Debug.Log($"{characterName} has died!");
        // Handle death logic here
        // Maybe remove from current hex, play death animation, etc.
        if (currentHex != null)
        {
            currentHex.SetCurrentPlayer(null);
        }
    }

    public bool MovePlayerToTile(HexTile targetTile)
    {
        if (isMoving || remainingMoves <= 0)
        {
            Debug.Log($"{characterName} cannot move: {(isMoving ? "Already moving" : "No moves remaining")}");
            return false;
        }

        if (targetTile == null || !targetTile.isWalkable || targetTile.hasChar)
        {
            Debug.Log($"{characterName} cannot move to target tile: Invalid destination");
            return false;
        }

        if (!IsAdjacent(currentHex, targetTile))
        {
            Debug.Log($"{characterName} cannot move to target tile: Not adjacent");
            return false;
        }

        if (targetTile.movementCost > remainingMoves)
        {
            Debug.Log($"{characterName} cannot move: Insufficient moves (need {targetTile.movementCost}, have {remainingMoves})");
            return false;
        }

        remainingMoves -= targetTile.movementCost;
        StartCoroutine(AnimateMovement(targetTile));
        return true;
    }

    private bool IsAdjacent(HexTile from, HexTile to)
    {
        if (from == null || to == null) return false;

        MapMaker mapMaker = MapMaker.instance;
        if (mapMaker == null) return false;

        List<Vector2Int> neighbors = mapMaker.GetNeighbors(from.coordinates);
        return neighbors.Contains(to.coordinates);
    }

    private IEnumerator AnimateMovement(HexTile targetTile)
    {
        isMoving = true;

        // Clear current hex
        if (currentHex != null)
        {
            currentHex.SetCurrentPlayer(null);
        }

        Vector3 startPos = transform.position;
        Vector3 endPos = targetTile.transform.position;
        float journeyTime = 1f / movementAnimationSpeed;
        float elapsedTime = 0;

        while (elapsedTime < journeyTime)
        {
            elapsedTime += Time.deltaTime;
            float fractionOfJourney = elapsedTime / journeyTime;
            fractionOfJourney = Mathf.SmoothStep(0, 1, fractionOfJourney);

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, fractionOfJourney);
            currentPos.y += Mathf.Sin(fractionOfJourney * Mathf.PI) * 0.2f;

            transform.position = currentPos;
            yield return null;
        }

        transform.position = endPos;
        currentHex = targetTile;
        currentHex.SetCurrentPlayer(this);

        isMoving = false;
        OnMovement?.Invoke(this);
    }

    public List<HexTile> GetMovementRange()
    {
        return GetTilesInRange(currentHex, remainingMoves);
    }

    private List<HexTile> GetTilesInRange(HexTile center, int range)
    {
        if (center == null || range <= 0) return new List<HexTile>();

        MapMaker mapMaker = MapMaker.instance;
        if (mapMaker == null) return new List<HexTile>();

        List<HexTile> tilesInRange = new List<HexTile>();
        Dictionary<HexTile, int> costSoFar = new Dictionary<HexTile, int>();
        Queue<HexTile> frontier = new Queue<HexTile>();

        frontier.Enqueue(center);
        costSoFar[center] = 0;

        while (frontier.Count > 0)
        {
            HexTile current = frontier.Dequeue();
            List<Vector2Int> neighbors = mapMaker.GetNeighbors(current.coordinates);

            foreach (Vector2Int neighborCoord in neighbors)
            {
                if (mapMaker.hexTiles.TryGetValue(neighborCoord, out HexTile neighbor))
                {
                    if (!neighbor.isWalkable || neighbor.hasChar) continue;

                    int newCost = costSoFar[current] + neighbor.movementCost;

                    if (newCost <= range && (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor]))
                    {
                        costSoFar[neighbor] = newCost;
                        frontier.Enqueue(neighbor);

                        if (!tilesInRange.Contains(neighbor))
                        {
                            tilesInRange.Add(neighbor);
                        }
                    }
                }
            }
        }

        return tilesInRange;
    }

    public bool CanMove()
    {
        return !isMoving && (remainingMoves > 0 || currentActionPoints > 0);
    }

    private IEnumerator TurnStartAnimation()
    {
        if (characterRenderer == null) yield break;

        float duration = 0.5f;
        float elapsed = 0;
        Color originalColor = characterRenderer.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float flash = Mathf.Sin(t * Mathf.PI * 2) * 0.5f + 0.5f;
            characterRenderer.color = Color.Lerp(originalColor, Color.white, flash * 0.5f);

            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.2f;
            transform.localScale = Vector3.one * scale;

            yield return null;
        }

        characterRenderer.color = originalColor;
        transform.localScale = Vector3.one;
    }

    private IEnumerator DamageFlashEffect()
    {
        if (characterRenderer == null) yield break;

        Color originalColor = characterRenderer.color;
        characterRenderer.color = Color.red;

        yield return new WaitForSeconds(0.1f);

        characterRenderer.color = originalColor;
    }
  
    // Update the existing StartTurn method to include action points
    public void StartTurnWithActions()
    {
        remainingMoves = moveSpeed;
        currentActionPoints = maxActionPoints;
        TurnStart?.Invoke(this);
        Debug.Log($"{characterName}'s turn started. Moves: {remainingMoves}, AP: {currentActionPoints}");

        StartCoroutine(TurnStartAnimation());
    }



   


 


 
}
