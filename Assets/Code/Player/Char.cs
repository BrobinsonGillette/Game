using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.GraphView.GraphView;

public enum Team { none, player, enemy , neutral , other };
public class Char : AnimatorBrain, IDamable
{
    [Header("Character Properties")]
    public Team team = Team.none;
    public int movementSpeed;
    private float movementAnimationSpeed = 1f;
    public HexTile currentHex { get; private set; }
    public bool isMoving { get; private set; }
    public float MaxHp;
    public float Health { get ; set  ; }
    private bool movingLeft;
    private Vector2 CurrentPosition;
    SpriteRenderer spriteRenderer;
    private void Awake()
    {
        spriteRenderer= GetComponent<SpriteRenderer>();
        CurrentPosition = transform.position;
        Initialize(GetComponent<Animator>().layerCount, Animations.Idle, GetComponent<Animator>(), DefaultAnimation);
    }
    private void Update()
    {
        getTileOnGround();
        if(movingLeft)
        {
            spriteRenderer.flipX = false;
        }
        else
        {
            spriteRenderer.flipX = true;
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
                currentHex.SetCharacterPresent(true, team);
            }
        }
    }

    public bool MovePlayerToTile(HexTile targetTile)
    {
        if (isMoving || movementSpeed <= 0)
        {
            return false;
        }

        if (targetTile == null || !targetTile.isWalkable || targetTile.HasCharacter)
        {
            return false;
        }

        if (!IsAdjacent(currentHex, targetTile))
        {
            return false;
        }

        if (targetTile.movementCost > movementSpeed)
        {
            return false;
        }

        movementSpeed -= targetTile.movementCost;
        if(CurrentPosition.x <= targetTile.transform.position.x)
        {
            movingLeft = true;
        }
        else
        {
            movingLeft = false;
        }
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
        Play(Animations.Walk, 0, false, false);
        // Clear current hex - both logically and visually
        if (currentHex != null)
        {
            currentHex.SetCharacterPresent(false); // Clear visual
                                                   // Note: We don't clear the currentHex reference until we arrive
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
            //currentPos.y += Mathf.Sin(fractionOfJourney * Mathf.PI) * 0.2f;

            transform.position = currentPos;
            yield return null;
        }

        // Finalize movement
        transform.position = endPos;
        currentHex = targetTile;

        // Update visual display
        targetTile.SetCharacterPresent(true, team);
        Play(Animations.Idle, 0, false, false);
        CurrentPosition = transform.position;
        isMoving = false;
        // Update MouseHandler's character position displays
        if (MouseHandler.instance != null)
        {
            // This will refresh all character position displays
            MouseHandler.instance.UpdateCharacterPositionDisplays();
        }
    }

    public List<HexTile> GetMovementRange()
    {
        return GetTilesInRange(currentHex, movementSpeed);
    }

    public List<HexTile> GetTilesInRange(HexTile center, int range)
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
                    if (!neighbor.isWalkable || neighbor.HasCharacter) continue;

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
        return !isMoving && (movementSpeed > 0);
    }
    void DefaultAnimation(int layer) 
    {
        if (isMoving)
        {
            Play(Animations.Walk, layer, false, false);
        }
        else
        {
            Play(Animations.Idle, layer, false, false);
        }
    }

    public void TakeDamage(float damage)
    {
        Health -= damage;
        Play(Animations.Hurt, 0, true, true);
        if (Health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Play(Animations.Death, 0, true, true);
    }

    public void PlayAttackAnimation()
    {
        Play(Animations.ATTACK_1, 0, false, false);
    }

    public void PlayHealAnimation()
    {
       Debug.Log("PlayHealAnimation");
       Play(Animations.ATTACK_2, 0, false, false);
    }
}
