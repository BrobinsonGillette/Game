using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Door", menuName = "TilesSet/Door Interactable")]
public class DoorInteractable : Interactable
{
    public string requiredKey = "";
    public string lockedMessage = "This door is locked.";
    public GameObject openDoorTile;

    public override void OnInteract(Vector3Int tilePosition, GameObject player)
    {
        if (!string.IsNullOrEmpty(requiredKey))
        {
            if (PlayerHasKey(player, requiredKey))
            {
                OpenDoor(tilePosition);
                base.OnInteract(tilePosition, player);
            }
            else
            {
                ShowDialogue(lockedMessage);
            }
        }
        else
        {
            OpenDoor(tilePosition);
            base.OnInteract(tilePosition, player);
        }
    }

    bool PlayerHasKey(GameObject player, string keyName)
    {
        // Implement your inventory system check
        return true; // Placeholder
    }

    void OpenDoor(Vector3Int position)
    {
        if (openDoorTile != null)
        {
            // Replace the door tile with an open door
            // You'll need to implement tile replacement in your tilemap manager
        }
    }
}
