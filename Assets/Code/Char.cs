using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Char : MonoBehaviour
{
    public HexTile currentHex;

    
    private void Update()
    {
        getTileOnGround();
    }
    void getTileOnGround()
    {
        if (currentHex == null)
        {
            MapMaker mapMaker = MapMaker.instance;
            if (mapMaker == null) return;
            Vector3 position = transform.position;
            Vector2Int hexCoords = mapMaker.WorldToHexPosition(position);
            if (mapMaker.hexTiles.TryGetValue(hexCoords, out HexTile tile))
            {
                currentHex = tile;
                currentHex.SetCurrentPlayer(this);
            }
        }
    }
    public void MovePlayerToTile(HexTile targetTile)
    {
        if (currentHex != null)
        {
            currentHex.SetCurrentPlayer(null);
        }
        if(targetTile == null || !targetTile.isWalkable || targetTile.hasPlayer)
        {
            return;
        }
        currentHex = targetTile;
        currentHex.SetCurrentPlayer(this);
        transform.position = targetTile.transform.position;
    }
}
