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
    public SpriteRenderer highlightRenderer; // Additional renderer for highlights

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    public Color selectedColor = Color.green;
    public Color playerPositionColor = Color.blue;
    public Color EnemyPositionColor = Color.red;
    public Color movementRangeColor = new Color(0.5f, 0.8f, 1f, 0.6f); // Light blue with transparency
    public Color MovementDestinationColor = Color.gray;


    [Header("Animation")]
    public float pulseSpeed = 2f;
    public float pulseIntensity = 0.3f;

    public bool isSelected { get; private set; }
    private bool isHovered = false;
    public bool hasChar { get; private set; }
    private bool inMovementRange = false;
    private bool isInMovementRange = false;
    private Coroutine pulseCoroutine;

    public Char CurrentPlayer { get; private set; }

    private void Awake()
    {
        // Create highlight renderer if it doesn't exist
        if (highlightRenderer == null)
        {
            GameObject highlightObj = new GameObject("Highlight");
            highlightObj.transform.SetParent(transform);
            highlightObj.transform.localPosition = Vector3.zero;
            highlightObj.transform.localScale = Vector3.one * 1.1f; // Slightly larger

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

    void UpdateVisual()
    {
        if (spriteRenderer == null) return;

        Color targetColor = normalColor;
        Color highlightColor = Color.clear;
        if (isHovered)
        {
            targetColor = hoverColor;
            highlightColor = hoverColor;
            highlightColor.a = 0.2f;
        }
        else if (hasChar)
        {
            if(CurrentPlayer.team == Team.enemy)
            {
                targetColor = EnemyPositionColor;
            }
            if(CurrentPlayer.team == Team.player)
            {
                targetColor = playerPositionColor;
            }

        }
        else if (isSelected)
        {
            targetColor = selectedColor;
            StartPulseEffect();
        }else if (inMovementRange)
        {
            targetColor = movementRangeColor;
            highlightColor = movementRangeColor;
            highlightColor.a = 0.4f;
        }
        else if (isInMovementRange)
        {
            targetColor = MovementDestinationColor;
            highlightColor = MovementDestinationColor;
            highlightColor.a = 0.4f;
        }
        
        spriteRenderer.color = targetColor;

        if (highlightRenderer != null)
        {
            highlightRenderer.color = highlightColor;
        }
    }

    public void MousedOver()
    {
        if (!isWalkable) return;

        isHovered = true;
        UpdateVisual();

        // Scale up slightly for hover effect
        transform.localScale = Vector3.one * 1.05f;
    }

    public void MouseExit()
    {
        isHovered = false;
        UpdateVisual();

        // Reset scale
        transform.localScale = Vector3.one;
    }

    public void Interact()
    {
        if (!isWalkable) return;

        isSelected = true;
        UpdateVisual();

        // Play click animation
        StartCoroutine(ClickAnimation());
        StartCoroutine(ClearAnimation());
    }

    public void DeSelect()
    {
        if (!isWalkable) return;

        isSelected = false;
        StopPulseEffect();
        UpdateVisual();
    }
    public void SetMovementRange(bool inRange)
    {
        inMovementRange = inRange;
        UpdateVisual();

        if (inRange)
        {
            StartCoroutine(AppearAnimation());
            StartCoroutine(ClearAnimation());
        }
    }
    public void SetInMovementRange(bool inRange)
    {
        isInMovementRange = inRange;
        UpdateVisual();

        if (inRange)
        {
            StartCoroutine(AppearAnimation());
            StartCoroutine(ClearAnimation());
        }
    }

    public void SetOnPath(bool onPath)
    {
        UpdateVisual();
    }

    public void SetCurrentPlayer(Char player)
    {
        if (player == null)
        {
            CurrentPlayer = null;
            hasChar = false;
            UpdateVisual();
            return;
        }

        if (CurrentPlayer == null || CurrentPlayer == player)
        {
            player.transform.position = transform.position;
            hasChar = true;
            CurrentPlayer = player;
            UpdateVisual();
            return;
        }
    }

    private void StartPulseEffect()
    {
        if (pulseCoroutine != null)
            StopCoroutine(pulseCoroutine);

        pulseCoroutine = StartCoroutine(PulseEffect());
        StartCoroutine(ClearAnimation());
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

                // Also pulse the highlight alpha
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

            // Quick scale bounce
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.2f;
            transform.localScale = originalScale * scale;

            yield return null;
        }

        transform.localScale = originalScale;
    }

    private IEnumerator AppearAnimation()
    {
        if (highlightRenderer == null) yield break;

        float duration = 0.3f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Fade in and scale up
            Color col = highlightRenderer.color;
            col.a = Mathf.Lerp(0, 0.4f, t);
            highlightRenderer.color = col;

            float scale = Mathf.Lerp(0.8f, 1.1f, Mathf.SmoothStep(0, 1, t));
            highlightRenderer.transform.localScale = Vector3.one * scale;

            yield return null;
        }
    }

    public IEnumerator ClearAnimation()
    {
        if (highlightRenderer == null) yield break;

        float duration = 0.3f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Fade out and scale down
            Color col = highlightRenderer.color;
            col.a = Mathf.Lerp(0.4f, 0, t);
            highlightRenderer.color = col;

            float scale = Mathf.Lerp(1.1f, 0.8f, Mathf.SmoothStep(0, 1, t));
            highlightRenderer.transform.localScale = Vector3.one * scale;

            yield return null;
        }
    }


}
