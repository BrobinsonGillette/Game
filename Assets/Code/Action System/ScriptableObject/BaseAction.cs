using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ActionType
{
    Move,
    Attack,
    Dodge,
    Block,
    CastSpell,
    Special
}
[System.Serializable]
public abstract class BaseAction : ScriptableObject
{
    [Header("Basic Properties")]
    public string actionName;
    public string description;
    public ActionType actionType;
    public int actionPointCost = 1;
    public int cooldown = 0;
    public int range = 1;

    [Header("Requirements")]
    public bool requiresTarget = true;
    public bool canTargetEnemies = true;
    public bool canTargetAllies = false;
    public bool canTargetSelf = false;
    public bool canTargetEmptyTiles = false;

    [Header("Audio/Visual")]
    public AudioClip soundEffect;
    public GameObject visualEffect;

    public abstract bool CanExecute(Char performer, HexTile targetTile = null);
    public abstract void Execute(Char performer, HexTile targetTile = null);
    public abstract List<HexTile> GetValidTargets(Char performer);
}

