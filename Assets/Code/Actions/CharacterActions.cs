using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterActions : MonoBehaviour
{
    public Char character;


    public bool CanUseAction(ActionData action)
    {
        return character.charClass.currentActionPoints >= action.actionPointCost &&
               !character.isMoving;
    }

    public bool CanUseItem(ItemData item)
    {
        return character.charClass.inventory.Contains(item) &&
               character.charClass.currentActionPoints >= item.actionEffect.actionPointCost &&
               !character.isMoving;
    }

    public void UseAction(ActionData action, HexTile targetTile, Char targetCharacter = null)
    {
        if (!CanUseAction(action)) return;

        character.charClass.currentActionPoints -= action.actionPointCost;
        ExecuteAction(action, targetTile, targetCharacter);
    }
    //todo fix
    //public void UseItem(ItemData item, HexTile targetTile, Char targetCharacter = null)
    //{
    //    if (!CanUseItem(item)) return;

    //    currentActionPoints -= item.actionEffect.actionPointCost;
    //    ExecuteAction(item.actionEffect, targetTile, targetCharacter);

    //    if (item.isConsumable)
    //    {
    //        inventory.Remove(item);
    //    }
    //}

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

            case ActionType.Buff:
            case ActionType.Debuff:
                ExecuteBuff(action, targetCharacter);
                break;

            case ActionType.Special:
                ExecuteSpecial(action, targetTile, targetCharacter);
                break;

            case ActionType.Item:
                ExecuteItem(action, targetCharacter);
                break;
        }

        // Play animation based on action type
        PlayActionAnimation(action.actionType);
    }

    private void ExecuteAttack(ActionData action, HexTile targetTile, Char targetCharacter)
    {
        // For the new workflow, we don't spawn hitbox here anymore
        // The hitbox is already spawned when action is selected
        // Instead, we activate existing hitboxes or trigger damage directly

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
            // Find all AttackHitbox components in the scene
            AttackHitMainbox[] allHitboxes = FindObjectsOfType<AttackHitMainbox>();

            foreach (AttackHitMainbox hitbox in allHitboxes)
            {
                // Check if this hitbox belongs to our character and matches the action
                if (hitbox != null && hitbox.OwnerTeam == character.team && !hitbox.IsActivated)
                {
                    // Activate the hitbox for actual damage
                    hitbox.ActivateForDamage();
                    Debug.Log($"{character.name} activated attack hitbox!");
                    break; // Only activate one hitbox per action
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error activating existing hitboxes: {e.Message}");
        }
    }

    private void ExecuteHeal(ActionData action, Char target)
    {
        if (target != null)
        {
            target.charClass.Health = Mathf.Min(target.charClass.MaxHp, target.charClass.Health + action.healing);
        }
    }

    private void ExecuteBuff(ActionData action, Char target)
    {
        // Simplified buff system - you can expand this
        Debug.Log($"{character.name} applies {action.actionName} to {target.name}!");
        // Add your buff/debuff logic here
    }

    private void ExecuteSpecial(ActionData action, HexTile targetTile, Char targetCharacter)
    {
        Debug.Log($"{character.name} uses special action: {action.actionName}!");
        // Add your special action logic here
    }

    private void ExecuteItem(ActionData action, Char target)
    {
        Debug.Log($"{character.name} uses item with effect: {action.actionName}!");
        // Item effects are handled the same as regular actions
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
        character.charClass.currentActionPoints = character.charClass.maxActionPoints;
    }

    public List<HexTile> GetActionRange(ActionData action)
    {
        return character.GetTilesInRange(character.currentHex, action.Length);
    }

    public List<HexTile> GetValidTargets(ActionData action)
    {
        List<HexTile> validTargets = new List<HexTile>();
        List<HexTile> tilesInRange = GetActionRange(action);

        foreach (HexTile tile in tilesInRange)
        {
            if (IsValidTarget(action, tile))
            {
                validTargets.Add(tile);
            }
        }

        return validTargets;
    }

    private bool IsValidTarget(ActionData action, HexTile tile)
    {
        Char characterOnTile = GetCharacterOnTile(tile);


        if (action.CanTargetMultipleTargets)
        {
            return true; // Area effects can target any tile
        }
        else
        {
            if (characterOnTile == null) return false;

            if (characterOnTile.team == character.team)
                return action.canTargetAllies;
            else
                return action.canTargetEnemies;
        }
    }

    private Char GetCharacterOnTile(HexTile tile)
    {
        Char[] allCharacters = FindObjectsOfType<Char>();
        foreach (Char character in allCharacters)
        {
            if (character.currentHex == tile)
            {
                return character;
            }
        }
        return null;
    }
}
