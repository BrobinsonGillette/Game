using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Chest", menuName = "TilesSet/Chest Interactable")]
public class ChestInteractable : Interactable
{
    public GameObject[] treasureItems;
    public int goldReward = 50;

    public override void OnInteract(Vector3Int tilePosition, GameObject player)
    {
        Debug.Log("Opening chest!");

        // Give gold
        GiveGold(player, goldReward);

        // Spawn treasure
        if (treasureItems.Length > 0)
        {
            SpawnItems(tilePosition, treasureItems);
        }

        base.OnInteract(tilePosition, player);
    }

    void GiveGold(GameObject player, int amount)
    {
        // Implement your gold/currency system
        Debug.Log($"Player received {amount} gold!");
    }
}
