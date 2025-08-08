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
    public Color blockedColor = Color.black;
    public Color playerPositionColor = Color.blue;
    public Color movementTargetColor = Color.cyan;

    public bool isSelected { get; private set; }
    private bool isHovered = false;
    public bool hasPlayer { get; private set; }
    private bool isMovementTarget = false;

    public Char CurrentPlayer { get; private set; }

    public void Initialize(Vector2Int coords, MapMaker manager)
    {
        coordinates = coords;
 

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
        else if (hasPlayer)
            targetColor = playerPositionColor;
        else if (isMovementTarget)
            targetColor = movementTargetColor;
        else if (isHovered)
            targetColor = hoverColor;

        spriteRenderer.color = targetColor;
    }

    public void MousedOver()
    {
        if (!isWalkable) return;

        isHovered = true;
        UpdateVisual();
    }

    public void MouseExit()
    {
        isHovered = false;
        UpdateVisual();
    }

    public void Interact()
    {
        if (!isWalkable) return;

 
        isSelected = true;
        UpdateVisual();
    }
    public void DeSelect()
    {
        if (!isWalkable) return;


        isSelected = false;
        UpdateVisual();
    }
    public void SetCurrentPlayer(Char player)
    {
        if (player == null)
        {
            CurrentPlayer = null;
            hasPlayer = false;
            UpdateVisual();
            return;
        }

        if (CurrentPlayer == null || CurrentPlayer == player)
        {
             player.transform.position = transform.position;
            hasPlayer = true;
            CurrentPlayer = player;
            UpdateVisual();
            return;
        }
   
    }
 
}
