using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class HexTile : MonoBehaviour
{

    [Header("Tile Properties")]
    public Vector2Int coordinates;
    public bool isWalkable = true;
    public int movementCost = 1;

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;
    public SpriteRenderer highlightRenderer;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    public Color selectedColor = Color.green;
    public Color playerPositionColor = Color.blue;
    public Color enemyPositionColor = Color.red;
    public Color movementRangeColor = new Color(0.5f, 0.8f, 1f, 0.6f);
    public Color movementDestinationColor = Color.gray;
    public Color attackTargetColor = new Color(1f, 0.3f, 0.3f, 0.8f); // Red for attack targets
    public Color supportTargetColor = new Color(0.3f, 0.3f, 1f, 0.8f); // Blue for support targets

    [Header("Animation")]
    public float pulseSpeed = 2f;
    public float pulseIntensity = 0.3f;

    // Visual state only - no game logic
    private bool isSelected = false;
    private bool isHovered = false;
    private bool hasCharacter = false;
    private bool inMovementRange = false;
    private bool isMovementDestination = false;
    private bool isAttackTarget = false;
    private bool isSupportTarget = false;
    private Team currentCharacterTeam = Team.none;

    private Coroutine pulseCoroutine;

    // Events for input handling only
    public Action OnInteract;
    public Action OnHover;
    public Action OnDeselect;

    private void Awake()
    {
        SetupHighlightRenderer();
    }

    private void SetupHighlightRenderer()
    {
        if (highlightRenderer == null)
        {
            GameObject highlightObj = new GameObject("Highlight");
            highlightObj.transform.SetParent(transform);
            highlightObj.transform.localPosition = Vector3.zero;
            highlightObj.transform.localScale = Vector3.one * 1.1f;

            highlightRenderer = highlightObj.AddComponent<SpriteRenderer>();
            highlightRenderer.sprite = spriteRenderer.sprite;
            highlightRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
            highlightRenderer.color = new Color(1, 1, 1, 0);
        }
    }

    public void Initialize(Vector2Int coords, MapMaker manager)
    {
        coordinates = coords;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        UpdateVisual();
    }

    #region Visual Display Methods

    public void SetHovered(bool hovered)
    {
        if (isHovered == hovered) return;

        isHovered = hovered;
        UpdateVisual();

        // Scale effect for hover
        transform.localScale = hovered ? Vector3.one * 1.05f : Vector3.one;

        if (hovered)
            OnHover?.Invoke();
    }

    public void SetSelected(bool selected)
    {
        if (isSelected == selected) return;

        isSelected = selected;

        if (selected)
            StartPulseEffect();
        else
            StopPulseEffect();

        UpdateVisual();

        if (!selected)
            OnDeselect?.Invoke();
    }

    public void SetCharacterPresent(bool present, Team team = Team.none)
    {
        hasCharacter = present;
        currentCharacterTeam = team;
        UpdateVisual();
    }

    public void SetMovementRange(bool inRange)
    {
        inMovementRange = inRange;
        UpdateVisual();
    }

    public void SetMovementDestination(bool isDestination)
    {
        isMovementDestination = isDestination;
        UpdateVisual();
    }
    public void SetAttackTarget(bool isTarget)
    {
        isAttackTarget = isTarget;
        UpdateVisual();
    }

    public void SetSupportTarget(bool isTarget)
    {
        isSupportTarget = isTarget;
        UpdateVisual();
    }
    public void ClearAllHighlights()
    {
        SetSelected(false);
        SetHovered(false);
        SetMovementRange(false);
        SetMovementDestination(false);
        SetAttackTarget(false);
        SetSupportTarget(false);
    }

    private void UpdateVisual()
    {
        if (spriteRenderer == null) return;

        Color targetColor = GetCurrentColor();
        Color highlightColor = GetHighlightColor();

        spriteRenderer.color = targetColor;

        if (highlightRenderer != null)
        {
            highlightRenderer.color = highlightColor;
        }
    }

    private Color GetCurrentColor()
    {
        // Priority order for color display
        if (isHovered)
            return hoverColor;

        if (hasCharacter)
        {
            return currentCharacterTeam switch
            {
                Team.player => playerPositionColor,
                Team.enemy => enemyPositionColor,
                _ => normalColor
            };
        }

        if (isSelected)
            return selectedColor;

        if (isMovementDestination)
            return movementDestinationColor;

        if (inMovementRange)
            return movementRangeColor;

        return normalColor;
    }

    private Color GetHighlightColor()
    {
        Color highlight = Color.clear;

        if (isHovered)
        {
            highlight = hoverColor;
            highlight.a = 0.2f;
        }
        else if (isAttackTarget)
        {
            highlight = attackTargetColor;
            highlight.a = 0.5f;
        }
        else if (isSupportTarget)
        {
            highlight = supportTargetColor;
            highlight.a = 0.5f;
        }
        else if (inMovementRange)
        {
            highlight = movementRangeColor;
            highlight.a = 0.4f;
        }
        else if (isMovementDestination)
        {
            highlight = movementDestinationColor;
            highlight.a = 0.4f;
        }

        return highlight;
    }

    #endregion

    #region Input Handling (Mouse Events)

    public void MousedOver()
    {
        if (!isWalkable) return;
        SetHovered(true);
    }

    public void MouseExit()
    {
        SetHovered(false);
    }

    public void Interact()
    {
        if (!isWalkable) return;

        OnInteract?.Invoke();
        StartCoroutine(ClickAnimation());
    }

    #endregion

    #region Animation Effects

    private void StartPulseEffect()
    {
        StopPulseEffect();
        pulseCoroutine = StartCoroutine(PulseEffect());
    }

    private void StopPulseEffect()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        if (highlightRenderer != null)
        {
            highlightRenderer.transform.localScale = Vector3.one * 1.1f;
        }
    }

    private IEnumerator PulseEffect()
    {
        while (isSelected)
        {
            float scale = 1.1f + Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
            if (highlightRenderer != null)
            {
                highlightRenderer.transform.localScale = Vector3.one * scale;

                Color col = highlightRenderer.color;
                col.a = 0.3f + Mathf.Sin(Time.time * pulseSpeed) * 0.2f;
                highlightRenderer.color = col;
            }
            yield return null;
        }
    }

    private IEnumerator ClickAnimation()
    {
        float duration = 0.2f;
        float elapsed = 0;
        Vector3 originalScale = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.2f;
            transform.localScale = originalScale * scale;

            yield return null;
        }

        transform.localScale = originalScale;
    }

    #endregion

    #region Public Properties for Game Logic

    public bool HasCharacter => hasCharacter;
    public bool IsWalkable => isWalkable;
    public int MovementCost => movementCost;
    public Vector2Int Coordinates => coordinates;

    #endregion

}
