using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Char : MonoBehaviour
{
    [Header("Character Properties")]
    public string characterName = "Player";
    public int moveSpeed = 3; // Number of tiles this character can move per turn
    public float movementAnimationSpeed = 2f; // Speed of visual movement between tiles

    [Header("Current State")]
    public HexTile currentHex;
    public bool isMoving = false;
    public int remainingMoves = 0; // Moves left this turn

    [Header("Visual")]
    public SpriteRenderer characterRenderer;
    public Color characterColor = Color.white;

    [Header("UI")]
    public GameObject moveIndicatorPrefab; // Optional: UI element showing remaining moves
    private TMPro.TextMeshPro moveCounterText;

    // Events
    public System.Action<Char> OnMovementComplete;
    public System.Action<Char> OnTurnStart;
    public System.Action<Char> OnTurnEnd;

    // Movement queue for smooth animation
    private Queue<HexTile> movementQueue = new Queue<HexTile>();
    private Coroutine currentMovement;

    private void Start()
    {
        // Initialize character
        if (characterRenderer == null)
            characterRenderer = GetComponent<SpriteRenderer>();

        if (characterRenderer != null)
            characterRenderer.color = characterColor;

        remainingMoves = moveSpeed;

        // Create move counter UI
        CreateMoveCounter();
    }

    private void Update()
    {
        getTileOnGround();
        UpdateMoveCounter();
    }

    void CreateMoveCounter()
    {
        GameObject counterObj = new GameObject("MoveCounter");
        counterObj.transform.SetParent(transform);
        counterObj.transform.localPosition = new Vector3(0, 0.8f, 0);

        moveCounterText = counterObj.AddComponent<TMPro.TextMeshPro>();
        moveCounterText.text = $"{remainingMoves}/{moveSpeed}";
        moveCounterText.fontSize = 3;
        moveCounterText.alignment = TMPro.TextAlignmentOptions.Center;
        moveCounterText.sortingOrder = 10;
    }

    void UpdateMoveCounter()
    {
        if (moveCounterText != null)
        {
            moveCounterText.text = $"{remainingMoves}/{moveSpeed}";

            // Change color based on moves remaining
            if (remainingMoves == 0)
                moveCounterText.color = Color.red;
            else if (remainingMoves <= moveSpeed / 2)
                moveCounterText.color = Color.yellow;
            else
                moveCounterText.color = Color.green;
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

    /// <summary>
    /// Start this character's turn
    /// </summary>
    public void StartTurn()
    {
        remainingMoves = moveSpeed;
        OnTurnStart?.Invoke(this);
        Debug.Log($"{characterName}'s turn started. Moves available: {remainingMoves}");

        // Visual feedback
        StartCoroutine(TurnStartAnimation());
    }

    /// <summary>
    /// End this character's turn
    /// </summary>
    public void EndTurn()
    {
        remainingMoves = 0;
        OnTurnEnd?.Invoke(this);
        Debug.Log($"{characterName}'s turn ended.");
    }

    /// <summary>
    /// Move player to a specific tile (single move)
    /// </summary>
    public bool MovePlayerToTile(HexTile targetTile)
    {
        if (isMoving || remainingMoves <= 0)
        {
            Debug.Log($"{characterName} cannot move: {(isMoving ? "Already moving" : "No moves remaining")}");
            return false;
        }

        if (targetTile == null || !targetTile.isWalkable || targetTile.hasPlayer)
        {
            Debug.Log($"{characterName} cannot move to target tile: Invalid destination");
            return false;
        }

        //// Check if target is adjacent to current position
        if (!IsAdjacent(currentHex, targetTile))
        {
            Debug.Log($"{characterName} cannot move to target tile: Not adjacent");
            return false;
        }

        // Check if we have enough moves
        if (targetTile.movementCost > remainingMoves)
        {
            Debug.Log($"{characterName} cannot move: Insufficient moves (need {targetTile.movementCost}, have {remainingMoves})");
            return false;
        }

        // Deduct movement cost
        remainingMoves -= targetTile.movementCost;

        // Start movement animation
        StartCoroutine(AnimateMovement(targetTile));

        return true;
    }

   
    /// <summary>
    /// Check if two tiles are adjacent
    /// </summary>
    private bool IsAdjacent(HexTile from, HexTile to)
    {
        if (from == null || to == null) return false;

        MapMaker mapMaker = MapMaker.instance;
        if (mapMaker == null) return false;

        List<Vector2Int> neighbors = mapMaker.GetNeighbors(from.coordinates);
        return neighbors.Contains(to.coordinates);
    }

    /// <summary>
    /// Animate movement to a single tile
    /// </summary>
    private IEnumerator AnimateMovement(HexTile targetTile)
    {
        isMoving = true;

        // Update tile occupancy
        if (currentHex != null)
            currentHex.SetCurrentPlayer(null);

        Vector3 startPos = transform.position;
        Vector3 endPos = targetTile.transform.position;
        float journeyTime = 1f / movementAnimationSpeed;
        float elapsedTime = 0;

        while (elapsedTime < journeyTime)
        {
            elapsedTime += Time.deltaTime;
            float fractionOfJourney = elapsedTime / journeyTime;

            // Smooth movement curve with a slight bounce
            fractionOfJourney = Mathf.SmoothStep(0, 1, fractionOfJourney);

            // Add slight vertical bounce
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, fractionOfJourney);
            currentPos.y += Mathf.Sin(fractionOfJourney * Mathf.PI) * 0.2f;

            transform.position = currentPos;
            yield return null;
        }

        // Ensure final position is exact
        transform.position = endPos;
        currentHex = targetTile;
        currentHex.SetCurrentPlayer(this);

        isMoving = false;
        OnMovementComplete?.Invoke(this);
    }

 
    /// <summary>
    /// Get tiles within movement range
    /// </summary>
    public List<HexTile> GetMovementRange()
    {
        return GetTilesInRange(currentHex, remainingMoves);
    }

    /// <summary>
    /// Get all tiles within a certain range using Dijkstra's algorithm for accurate movement cost calculation
    /// </summary>
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
                    if (!neighbor.isWalkable || neighbor.hasPlayer) continue;

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

    /// <summary>
    /// Check if character can move
    /// </summary>
    public bool CanMove()
    {
        return !isMoving && remainingMoves > 0;
    }



    /// <summary>
    /// Visual feedback for turn start
    /// </summary>
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

            // Flash effect
            float flash = Mathf.Sin(t * Mathf.PI * 2) * 0.5f + 0.5f;
            characterRenderer.color = Color.Lerp(originalColor, Color.white, flash * 0.5f);

            // Scale pulse
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.2f;
            transform.localScale = Vector3.one * scale;

            yield return null;
        }

        characterRenderer.color = originalColor;
        transform.localScale = Vector3.one;
    }
}
