using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HexTile : MonoBehaviour
{
    [Header("Tile Properties")]
    public Vector2Int coordinates;
    public bool isWalkable = true;
    public int movementCost = 1;

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;


    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    public Color selectedColor = Color.green;
    public Color blockedColor = Color.red;

    private MapMaker gridManager;
    private bool isSelected = false;
    private bool isHovered = false;

    public void Initialize(Vector2Int coords, MapMaker manager)
    {
        coordinates = coords;
        gridManager = manager;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        UpdateVisual();
    }

    void UpdateVisual()
    {
        if (spriteRenderer == null) return;

        Color targetColor = normalColor;

        if (!isWalkable)
            targetColor = blockedColor;
        else if (isSelected)
            targetColor = selectedColor;
        else if (isHovered)
            targetColor = hoverColor;

        spriteRenderer.color = targetColor;
    }

    public void MousedOver()
    {
        if (!isWalkable) return;

        isHovered = true;
        UpdateVisual();

        // Optional: Show tile info
       // Debug.Log($"Hex Tile: {coordinates} - Cost: {movementCost}");
    }

    public void MouseExit()
    {
        isHovered = false;
        UpdateVisual();
    }

    public void Interact()
    {
        if (!isWalkable) return;

        ToggleSelection();
    }

    public void ToggleSelection()
    {
        isSelected = !isSelected;
        UpdateVisual();

        if (isSelected)
        {
            // Show neighbors or perform other selection logic
            var neighbors = gridManager.GetNeighbors(coordinates);
            //Debug.Log($"Selected hex {coordinates} has {neighbors.Count} neighbors");
        }
    }
    // Pathfinding helper methods
    public void SetWalkable(bool walkable)
    {
        isWalkable = walkable;
        UpdateVisual();
    }

    public void SetMovementCost(int cost)
    {
        movementCost = Mathf.Max(1, cost);
    }
    public float GetDistanceTo(HexTile other)
    {
        if (other == null) return float.MaxValue;
        return gridManager.GetDistance(coordinates, other.coordinates);
    }
}
