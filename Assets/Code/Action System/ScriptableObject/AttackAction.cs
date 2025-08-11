using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Attack Action", menuName = "Actions/Attack Action")]
public class AttackAction : BaseAction
{
    [Header("Attack Properties")]
    public float damage = 10f;
    public float critChance = 0.1f;
    public float critMultiplier = 2f;
    public bool isRanged = false;
    public bool canAttackDiagonally = true;

    public override bool CanExecute(Char performer, HexTile targetTile = null)
    {
        if (targetTile == null || !targetTile.hasChar) return false;

        Char target = targetTile.CurrentPlayer;
        if (target == null || target.team == performer.team) return false;

        // Check range
        int distance = GetDistance(performer.currentHex, targetTile);
        return distance <= range;
    }

    public override void Execute(Char performer, HexTile targetTile = null)
    {
        if (!CanExecute(performer, targetTile)) return;

        Char target = targetTile.CurrentPlayer;
        IDamable damageTarget = target.GetComponent<IDamable>();

        if (damageTarget != null)
        {
            float finalDamage = damage;

            // Calculate crit
            if (Random.value < critChance)
            {
                finalDamage *= critMultiplier;
                Debug.Log($"Critical hit! {finalDamage} damage!");
            }

            damageTarget.TakeDamage(finalDamage);
        }
    }

    public override List<HexTile> GetValidTargets(Char performer)
    {
        List<HexTile> validTargets = new List<HexTile>();
        MapMaker mapMaker = MapMaker.instance;

        if (mapMaker == null || performer.currentHex == null) return validTargets;

        // Get all tiles in range
        List<HexTile> tilesInRange = GetTilesInRange(performer.currentHex, range);

        foreach (HexTile tile in tilesInRange)
        {
            if (CanExecute(performer, tile))
            {
                validTargets.Add(tile);
            }
        }

        return validTargets;
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

