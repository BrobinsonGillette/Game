using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefensiveBuff : MonoBehaviour
{
    private float damageReduction = 0f;
    private bool hasEvasionBonus = false;
    private float evasionBonus = 0f;
    private int remainingTurns = 0;

    public void ApplyDefensiveBuff(float reduction, int duration, bool evasion, float evasionValue)
    {
        damageReduction = reduction;
        remainingTurns = duration;
        hasEvasionBonus = evasion;
        evasionBonus = evasionValue;

        // Visual feedback
        StartCoroutine(DefensiveVisualEffect());
    }

    public float GetDamageReduction()
    {
        return remainingTurns > 0 ? damageReduction : 0f;
    }

    public float GetEvasionBonus()
    {
        return (remainingTurns > 0 && hasEvasionBonus) ? evasionBonus : 0f;
    }

    public void DecrementTurn()
    {
        if (remainingTurns > 0)
        {
            remainingTurns--;
            if (remainingTurns <= 0)
            {
                Debug.Log($"{name} defensive buff expired!");
            }
        }
    }

    private IEnumerator DefensiveVisualEffect()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null) yield break;

        Color originalColor = renderer.color;
        Color buffColor = Color.blue;

        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            renderer.color = Color.Lerp(buffColor, originalColor, t);
            yield return null;
        }

        renderer.color = originalColor;
    }
}
