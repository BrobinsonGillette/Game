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



[CreateAssetMenu(fileName = "New Action", menuName = "Game/Action Data")]
public class ActionData : ScriptableObject
{
    [Header("Basic Info")]
    public string actionName;
    public string description;
    public ActionType actionType;
    public Animations animation;

    [Header("Range & Targeting")]
    public int range = 1;
    public int Width = 1;
    public int Length = 1;


    [Header("Costs")]
    public int actionPointCost = 1;
    public int manaCost = 0;

    [Header("Effects")]
    public float damage = 0f;
    public float healing = 0f;


    [Header("Hitbox/VFX")]
    public GameObject hitboxPrefab; // The hitbox prefab to spawn
    public float hitboxLifetime = 1f; // How long the hitbox stays active


    [Header("Special Properties")]
    public bool CanTargetMultipleTargets = false;
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