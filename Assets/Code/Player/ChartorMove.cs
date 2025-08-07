using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChartorMove : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Current Position")]
    public Vector2Int currentHexPosition;

    private MapMaker mapMaker;
    private bool isMoving = false;
    private bool isInMovementMode = false;
    private Coroutine currentMoveCoroutine;
    private List<HexTile> highlightedTiles = new List<HexTile>();

    // Events
    public System.Action<Vector2Int> OnMoveStarted;
    public System.Action<Vector2Int> OnMoveCompleted;
    public System.Action OnMovementModeActivated;
    public System.Action OnMovementModeDeactivated;

    void Start()
    {
        mapMaker = MapMaker.instance;
        if (mapMaker == null)
        {
            Debug.LogError("ChartorMove: MapMaker not found!");
            return;
        }

        // Set initial position to the world position equivalent of currentHexPosition
        UpdateWorldPosition();
    }

    void Update()
    {
        // Exit movement mode if right-click or escape is pressed
        if (isInMovementMode && (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)))
        {
            DeactivateMovementMode();
        }
    }

    /// <summary>
    /// Activate movement mode - player can now select where to move
    /// </summary>
    public void ActivateMovementMode()
    {
        if (isMoving)
        {
            Debug.Log("ChartorMove: Cannot activate movement mode while moving");
            return;
        }

        isInMovementMode = true;
        OnMovementModeActivated?.Invoke();

        // Highlight neighboring tiles as movement options
        HighlightMovementOptions();

        Debug.Log("ChartorMove: Movement mode activated! Click on a neighboring tile to move there.");
    }

    /// <summary>
    /// Deactivate movement mode
    /// </summary>
    public void DeactivateMovementMode()
    {
        if (!isInMovementMode) return;

        isInMovementMode = false;
        OnMovementModeDeactivated?.Invoke();

        // Clear highlighted tiles
        ClearHighlightedTiles();

        Debug.Log("ChartorMove: Movement mode deactivated.");
    }

    /// <summary>
    /// Highlight tiles that the player can move to
    /// </summary>
    private void HighlightMovementOptions()
    {
        if (mapMaker == null) return;

        ClearHighlightedTiles();

        // Get neighboring tiles
        var neighbors = mapMaker.GetNeighbors(currentHexPosition);
        foreach (var neighborCoord in neighbors)
        {
            HexTile neighborTile = mapMaker.GetHexTile(neighborCoord);
            if (neighborTile != null && neighborTile.isWalkable)
            {
                neighborTile.SetAsMovementTarget(true);
                highlightedTiles.Add(neighborTile);
            }
        }
    }

    /// <summary>
    /// Clear all highlighted movement option tiles
    /// </summary>
    private void ClearHighlightedTiles()
    {
        foreach (var tile in highlightedTiles)
        {
            if (tile != null)
            {
                tile.SetAsMovementTarget(false);
            }
        }
        highlightedTiles.Clear();
    }

    /// <summary>
    /// Attempt to move to a target tile (only works if in movement mode)
    /// </summary>
    public bool TryMoveToTile(Vector2Int targetHexCoord)
    {
        if (!isInMovementMode)
        {
            Debug.Log("ChartorMove: Not in movement mode, cannot move");
            return false;
        }

        // Check if target is a valid neighbor
        var neighbors = mapMaker.GetNeighbors(currentHexPosition);
        if (!neighbors.Contains(targetHexCoord))
        {
            Debug.Log("ChartorMove: Target tile is not adjacent to current position");
            return false;
        }

        // Check if target tile exists and is walkable
        HexTile targetTile = mapMaker.GetHexTile(targetHexCoord);
        if (targetTile == null || !targetTile.isWalkable)
        {
            Debug.Log($"ChartorMove: Cannot move to {targetHexCoord} - tile not walkable or doesn't exist");
            return false;
        }

        // Deactivate movement mode and start moving
        DeactivateMovementMode();
        MoveToHex(targetHexCoord);
        return true;
    }

    /// <summary>
    /// Move the player to the specified hex coordinate
    /// </summary>
    private void MoveToHex(Vector2Int targetHexCoord)
    {
        if (mapMaker == null)
        {
            Debug.LogError("ChartorMove: MapMaker not available!");
            return;
        }

        // Don't move if already at target position
        if (currentHexPosition == targetHexCoord)
        {
            Debug.Log("ChartorMove: Already at target position");
            return;
        }

        // Don't start new movement if already moving
        if (isMoving)
        {
            Debug.Log("ChartorMove: Already moving, ignoring move request");
            return;
        }

        // Start movement
        if (currentMoveCoroutine != null)
        {
            StopCoroutine(currentMoveCoroutine);
        }

        currentMoveCoroutine = StartCoroutine(MoveCoroutine(targetHexCoord));
    }

    /// <summary>
    /// Coroutine that handles the smooth movement animation
    /// </summary>
    private IEnumerator MoveCoroutine(Vector2Int targetHexCoord)
    {
        isMoving = true;
        OnMoveStarted?.Invoke(targetHexCoord);

        Vector3 startWorldPos = transform.position;
        Vector3 targetWorldPos = mapMaker.HexToWorldPosition(targetHexCoord) - mapMaker.GridCenter;

        float elapsedTime = 0f;
        float moveDuration = 1f / moveSpeed;

        while (elapsedTime < moveDuration)
        {
            float t = elapsedTime / moveDuration;
            float curveValue = moveCurve.Evaluate(t);

            transform.position = Vector3.Lerp(startWorldPos, targetWorldPos, curveValue);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure we end up exactly at the target position
        transform.position = targetWorldPos;
        currentHexPosition = targetHexCoord;

        isMoving = false;
        OnMoveCompleted?.Invoke(targetHexCoord);
    }

    /// <summary>
    /// Update the world position based on current hex position
    /// </summary>
    public void UpdateWorldPosition()
    {
        if (mapMaker != null)
        {
            Vector3 worldPos = mapMaker.HexToWorldPosition(currentHexPosition) - mapMaker.GridCenter;
            transform.position = worldPos;
        }
    }

    /// <summary>
    /// Set the player's hex position without animation
    /// </summary>
    public void SetHexPosition(Vector2Int hexCoord)
    {
        currentHexPosition = hexCoord;
        UpdateWorldPosition();
    }

    /// <summary>
    /// Check if the player can move to a specific hex coordinate
    /// </summary>
    public bool CanMoveTo(Vector2Int hexCoord)
    {
        if (mapMaker == null) return false;

        HexTile tile = mapMaker.GetHexTile(hexCoord);
        return tile != null && tile.isWalkable && !isMoving;
    }

    /// <summary>
    /// Get the current hex coordinate the player is on
    /// </summary>
    public Vector2Int GetCurrentHexPosition()
    {
        return currentHexPosition;
    }


    public bool IsMoving()
    {
       return isMoving;
    }
}
