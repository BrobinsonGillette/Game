using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Defensive Action", menuName = "Actions/Defensive Action")]
public class DefensiveAction : BaseAction
{
    [Header("Defensive Properties")]
    public float damageReduction = 0.5f;
    public int duration = 1; // Turns
    public bool increasesEvasion = false;
    public float evasionBonus = 0.2f;

    public override bool CanExecute(Char performer, HexTile targetTile = null)
    {
        // Defensive actions can usually always be performed on self
        return true;
    }

    public override void Execute(Char performer, HexTile targetTile = null)
    {
        // Apply defensive buff to performer
        DefensiveBuff buff = performer.GetComponent<DefensiveBuff>();
        if (buff == null)
        {
            buff = performer.gameObject.AddComponent<DefensiveBuff>();
        }

        buff.ApplyDefensiveBuff(damageReduction, duration, increasesEvasion, evasionBonus);
    }

    public override List<HexTile> GetValidTargets(Char performer)
    {
        // Defensive actions typically target self
        return new List<HexTile> { performer.currentHex };
    }
}
