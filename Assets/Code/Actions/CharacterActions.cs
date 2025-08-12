using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterActions : MonoBehaviour
{
    [Header("Action Points")]
    public int maxActionPoints = 2;
    public int currentActionPoints = 2;

    [Header("Available Actions")]
    public List<ActionData> availableActions = new List<ActionData>();

    [Header("Inventory")]
    public List<ItemData> inventory = new List<ItemData>();

    private Char character;

    private void Awake()
    {
        character = GetComponent<Char>();
    }

    public bool CanUseAction(ActionData action)
    {
        return currentActionPoints >= action.actionPointCost &&
               !character.isMoving;
    }

    public bool CanUseItem(ItemData item)
    {
        return inventory.Contains(item) &&
               currentActionPoints >= item.actionEffect.actionPointCost &&
               !character.isMoving;
    }

    public void UseAction(ActionData action, HexTile targetTile, Char targetCharacter = null)
    {
        if (!CanUseAction(action)) return;

        currentActionPoints -= action.actionPointCost;
        ExecuteAction(action, targetTile, targetCharacter);
    }

    public void UseItem(ItemData item, HexTile targetTile, Char targetCharacter = null)
    {
        if (!CanUseItem(item)) return;

        currentActionPoints -= item.actionEffect.actionPointCost;
        ExecuteAction(item.actionEffect, targetTile, targetCharacter);

        if (item.isConsumable)
        {
            inventory.Remove(item);
        }
    }

    private void ExecuteAction(ActionData action, HexTile targetTile, Char targetCharacter)
    {
        // Handle different action types
        switch (action.actionType)
        {
            case ActionType.Attack:
                ExecuteAttack(action, targetCharacter);
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

    private void ExecuteAttack(ActionData action, Char target)
    {
        if (target != null && target.GetComponent<IDamable>() != null)
        {
            target.GetComponent<IDamable>().TakeDamage(action.damage);
            Debug.Log($"{character.name} attacks {target.name} for {action.damage} damage!");
        }
    }

    private void ExecuteHeal(ActionData action, Char target)
    {
        if (target != null)
        {
            target.Health = Mathf.Min(target.MaxHp, target.Health + action.healing);
            Debug.Log($"{character.name} heals {target.name} for {action.healing} HP!");
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
        currentActionPoints = maxActionPoints;
    }

    public List<HexTile> GetActionRange(ActionData action)
    {
        return character.GetTilesInRange(character.currentHex, action.range);
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

        switch (action.targetType)
        {
            case TargetType.Self:
                return characterOnTile == character;

            case TargetType.SingleTarget:
                if (characterOnTile == null) return false;

                if (characterOnTile.team == character.team)
                    return action.canTargetAllies;
                else
                    return action.canTargetEnemies;

            case TargetType.Area:
                return true; // Area effects can target any tile

            case TargetType.AllEnemies:
                return characterOnTile != null && characterOnTile.team != character.team;

            case TargetType.AllAllies:
                return characterOnTile != null && characterOnTile.team == character.team;

            default:
                return false;
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
