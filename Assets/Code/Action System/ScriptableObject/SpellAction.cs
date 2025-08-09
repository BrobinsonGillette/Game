using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SpellEffect
{
    Damage,
    Heal,
    Buff,
    Debuff,
    Teleport,
    CreateBarrier,
    ElementalDamage
}

[CreateAssetMenu(fileName = "New Spell Action", menuName = "Actions/Spell Action")]
public class SpellAction : BaseAction
{
    [Header("Spell Properties")]
    public float manaCost = 10f;
    public bool isAreaOfEffect = false;
    public int aoeRadius = 1;
    public SpellEffect spellEffect;

    public override bool CanExecute(Char performer, HexTile targetTile = null)
    {
        // Check if performer has enough mana (you'll need to add mana to Char class)
        // For now, just check basic requirements
        if (requiresTarget && targetTile == null) return false;

        if (targetTile != null)
        {
            int distance = GetDistance(performer.currentHex, targetTile);
            if (distance > range) return false;
        }

        return true;
    }

    public override void Execute(Char performer, HexTile targetTile = null)
    {
        if (!CanExecute(performer, targetTile)) return;

        List<HexTile> affectedTiles = new List<HexTile>();

        if (isAreaOfEffect && targetTile != null)
        {
            affectedTiles = GetTilesInRange(targetTile, aoeRadius);
        }
        else if (targetTile != null)
        {
            affectedTiles.Add(targetTile);
        }

        foreach (HexTile tile in affectedTiles)
        {
            ApplySpellEffect(tile, performer);
        }

        Debug.Log($"{performer.characterName} casts {actionName}!");
    }

    public override List<HexTile> GetValidTargets(Char performer)
    {
        List<HexTile> validTargets = new List<HexTile>();
        MapMaker mapMaker = MapMaker.instance;

        if (mapMaker == null || performer.currentHex == null) return validTargets;

        List<HexTile> tilesInRange = GetTilesInRange(performer.currentHex, range);

        foreach (HexTile tile in tilesInRange)
        {
            bool isValid = false;

            if (canTargetEmptyTiles && !tile.hasChar)
                isValid = true;
            else if (tile.hasChar)
            {
                Char target = tile.CurrentPlayer;
                if (canTargetEnemies && target.team != performer.team)
                    isValid = true;
                else if (canTargetAllies && target.team == performer.team && target != performer)
                    isValid = true;
                else if (canTargetSelf && target == performer)
                    isValid = true;
            }

            if (isValid)
                validTargets.Add(tile);
        }

        return validTargets;
    }

    private void ApplySpellEffect(HexTile tile, Char caster)
    {
        // Apply spell effects based on spellEffect enum
        // This would be expanded based on your spell system
    }

    private int GetDistance(HexTile from, HexTile to)
    {
        if (from == null || to == null) return int.MaxValue;

        Vector2Int fromCoord = from.coordinates;
        Vector2Int toCoord = to.coordinates;

        return (Mathf.Abs(fromCoord.x - toCoord.x) +
                Mathf.Abs(fromCoord.x + fromCoord.y - toCoord.x - toCoord.y) +
                Mathf.Abs(fromCoord.y - toCoord.y)) / 2;
    }

    private List<HexTile> GetTilesInRange(HexTile center, int range)
    {
        List<HexTile> tiles = new List<HexTile>();
        MapMaker mapMaker = MapMaker.instance;

        for (int q = -range; q <= range; q++)
        {
            for (int r = Mathf.Max(-range, -q - range); r <= Mathf.Min(range, -q + range); r++)
            {
                Vector2Int coord = center.coordinates + new Vector2Int(q, r);
                HexTile tile = mapMaker.GetHexTile(coord);
                if (tile != null)
                {
                    tiles.Add(tile);
                }
            }
        }

        return tiles;
    }
}
