using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DunonExit", menuName = "TilesSet/Dungon DunonExit")]
public class DunonExit : Interactable
{

    public string dungeonName = "Dungeon";
    [Header("Player Positioning")]
    public Vector3 playerOffset = new Vector3(0, -1, 0); // Offset to place player (default: 1 unit above)

    public override void OnInteract(Vector3Int tilePosition, GameObject player)
    {
      
            Debug.Log($"Exiting dungeon: {dungeonName}");
            SceneLoader.Instance.UnloadScene(dungeonName);
            SceneLoader.Instance.ShowScene("Map");

            // Teleport player above the exit door
            TeleportPlayer(player, tilePosition);
        
    }

    private void TeleportPlayer(GameObject player, Vector3Int tilePosition)
    {
        if (player != null)
        {
            // Convert tile position to world position
            Vector3 worldPosition = new Vector3(tilePosition.x + 0.5f, tilePosition.y + 0.5f, 0);

            // Apply the offset (usually above the door)
            Vector3 targetPosition = worldPosition + playerOffset;

            // Teleport the player
            player.transform.position = targetPosition;

            Debug.Log($"Teleported player to {targetPosition}");
        }
    }


}
