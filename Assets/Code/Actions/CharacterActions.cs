using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterActions : MonoBehaviour
{
    public Char character;


    public bool CanUseAction(ActionData action)
    {
        return character.CurrentActionPoints >= action.actionPointCost &&
               !character.isMoving;
    }



    public void UseAction(ActionData action, HexTile targetTile, Char targetCharacter = null)
    {
        if (!CanUseAction(action)) return;

        character.CurrentActionPoints -= action.actionPointCost;
        ExecuteAction(action, targetTile, targetCharacter);
    }
    private void ExecuteAction(ActionData action, HexTile targetTile, Char targetCharacter)
    {
        // Handle different action types
        switch (action.actionType)
        {
            case ActionType.Attack:
                ExecuteAttack(action, targetTile, targetCharacter);
                break;

            case ActionType.Heal:
                ExecuteHeal(action, targetCharacter);
                break;

        }

        // Play animation based on action type
        PlayActionAnimation(action.actionType);
    }

    private void ExecuteAttack(ActionData action, HexTile targetTile, Char targetCharacter)
    {
 
        if (action.hitboxPrefab != null)
        {
            // Find existing hitboxes spawned by this character and activate them
            ActivateExistingHitboxes(action, targetTile, targetCharacter);
        }
        else
        {
            // Fallback to direct damage if no hitbox prefab and we have a specific target
            if (targetCharacter != null && targetCharacter.GetComponent<IDamage>() != null)
            {
                targetCharacter.GetComponent<IDamage>().TakeDamage(action.damage);
                Debug.Log($"{character.name} attacks {targetCharacter.name} for {action.damage} damage!");
            }
        }
    }

    private void ActivateExistingHitboxes(ActionData action, HexTile targetTile, Char targetCharacter)
    {
        try
        {
            AttackHitMainbox[] allHitboxes = FindObjectsOfType<AttackHitMainbox>();
            foreach (AttackHitMainbox hitbox in allHitboxes)
            {
           
                if (hitbox != null && !hitbox.IsActivated)
                {
                    hitbox.ActivateForDamage();
                    
                    break; 
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.Log(ex.Message);
        }
    }

    private void ExecuteHeal(ActionData action, Char target)
    {
        if (target != null)
        {
            target.Health = Mathf.Min(target.charClass.MaxHp, target.Health + action.healing);
        }
    }


    private void PlayActionAnimation(ActionType actionType)
    {
        // You can expand this to play different animations
        switch (actionType)
        {
            case ActionType.Attack:
                character.PlayAttackAnimation();
                break;
            case ActionType.Heal:
                character.PlayHealAnimation();
                break;
                // Add more cases as needed
        }
    }

    public void RestoreActionPoints()
    {
        character.CurrentActionPoints = character.charClass.maxActionPoints;
    }

    public List<HexTile> GetActionRange(ActionData action)
    {
        return character.GetTilesInRange(character.currentHex, action.Length);
    }

 

  

}
