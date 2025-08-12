using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public enum ActionType
{
    Attack,
    Heal,
    Buff,
    Debuff,
    Special,
    Item
}

[System.Serializable]
public enum TargetType
{
    Self,
    SingleTarget,
    Area,
    AllEnemies,
    AllAllies
}

[CreateAssetMenu(fileName = "New Action", menuName = "Game/Action Data")]
public class ActionData : ScriptableObject
{
    [Header("Basic Info")]
    public string actionName;
    public string description;
    public ActionType actionType;
    public TargetType targetType;
    public Animations animation;

    [Header("Range & Targeting")]
    public int range = 1;
    public int areaOfEffect = 0; // 0 = single target, 1+ = radius

    [Header("Costs")]
    public int actionPointCost = 1;
    public int manaCost = 0;

    [Header("Effects")]
    public float damage = 0f;
    public float healing = 0f;
    public int duration = 0; // For buffs/debuffs

    [Header("Special Properties")]
    public bool requiresLineOfSight = true;
    public bool canTargetSelf = false;
    public bool canTargetAllies = true;
    public bool canTargetEnemies = true;
}

// ItemData.cs - For consumable items
[CreateAssetMenu(fileName = "New Item", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Item Info")]
    public string itemName;
    public string description;
    public Sprite icon;

    [Header("Usage")]
    public ActionData actionEffect; // What happens when used
    public bool isConsumable = true;
    public int stackSize = 1;
}

