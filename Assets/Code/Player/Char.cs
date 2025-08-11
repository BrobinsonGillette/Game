using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Team { none, player, enemy , neutral , other };
public class Char : MonoBehaviour
{
    [Header("Character Properties")]
    public Team team = Team.none;
    private float movementAnimationSpeed = 2f;

    public SpriteRenderer characterRenderer;
    public HexTile currentHex { get; private set; }
    public bool isMoving { get; private set; }


    [Header("Visual")]
    public CharClasses charClass;


    // Events
    public System.Action<Char> OnMovement;
    public System.Action<Char> TurnStart;
    public System.Action<Char> OnTurnEnd;
    public System.Action<Char> OnHealthChanged;
    public System.Action<Char> OnManaChanged;
    private void Start()
    {

        // Create move counter UI
        charClass.InitializeCharacter(characterRenderer,this.transform);
    }


    private void Update()
    {
        getTileOnGround();
        charClass.UpdateMoveCounter();
        charClass.UpdateHealthDisplay();
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
        charClass.remainingMoves = charClass.stats.moveSpeed;
        charClass.currentActionPoints = charClass.stats.maxActionPoints;

        // Regenerate some mana each turn
        charClass.currentMana = Mathf.Min(charClass.stats.maxMana, charClass.currentMana + 5f);

        TurnStart?.Invoke(this);
        StartCoroutine(TurnStartAnimation());
    }

    public void EndTurn()
    {
        charClass.remainingMoves = 0;
        charClass.currentActionPoints = 0;

        // Process any end-of-turn effects (like defensive buff countdown)
        DefensiveBuff buff = GetComponent<DefensiveBuff>();
        if (buff != null)
        {
            buff.DecrementTurn();
        }

        OnTurnEnd?.Invoke(this);
    }

    public bool CanUseAction(BaseAction action)
    {
        if (action == null) return false;

        // Check action points
        if (charClass.currentActionPoints < action.actionPointCost) return false;

        // Check mana for spells
        if (action is SpellAction spell && charClass.currentMana < spell.manaCost) return false;

        return action.CanExecute(this);
    }

    public void SpendActionPoints(int cost)
    {
        charClass.currentActionPoints = Mathf.Max(0, charClass.currentActionPoints - cost);
    }

    public void SpendMana(float cost)
    {
        charClass.currentMana = Mathf.Max(0, charClass.currentMana - cost);
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

        charClass.currentHealth = Mathf.Max(0, charClass.currentHealth - damage);
        OnHealthChanged?.Invoke(this);

        if (charClass.currentHealth <= 0)
        {
            Die();
        }

        // Visual damage effect
        StartCoroutine(DamageFlashEffect());
    }

    public void Heal(float amount)
    {
        charClass.currentHealth = Mathf.Min(charClass.stats.maxHealth, charClass.currentHealth + amount);
        OnHealthChanged?.Invoke(this);
    }

    private void Die()
    {
        // Handle death logic here
        // Maybe remove from current hex, play death animation, etc.
        if (currentHex != null)
        {
            currentHex.SetCurrentPlayer(null);
        }
    }

    public bool MovePlayerToTile(HexTile targetTile)
    {
        if (isMoving ||charClass.remainingMoves <= 0)
        {
            return false;
        }

        if (targetTile == null || !targetTile.isWalkable || targetTile.hasChar)
        {
            return false;
        }

        if (!IsAdjacent(currentHex, targetTile))
        {
            return false;
        }

        if (targetTile.movementCost > charClass.remainingMoves)
        {
            return false;
        }

        charClass.remainingMoves -= targetTile.movementCost;
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
        return GetTilesInRange(currentHex, charClass.remainingMoves);
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
        return !isMoving && (charClass.remainingMoves > 0 || charClass.currentActionPoints > 0);
    }

    private IEnumerator TurnStartAnimation()
    {

        float duration = 0.5f;
        float elapsed = 0;
        Color originalColor =characterRenderer.color;

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
  




   


 


 
}
