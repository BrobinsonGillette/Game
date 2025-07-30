using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LocationEntrance
{
    public Vector3Int position;
    public LocationType locationType;
    public string locationName;
}

public enum LocationType
{
    Town,
    Dungeon,
    Cave,
    Castle
}

// Location Manager for premade tilemaps
public class LocationManager : MonoBehaviour
{
    private DynamicTilemapSystem mainWorldSystem;
    private Vector3 returnPosition;

    public void Initialize(DynamicTilemapSystem worldSystem, Vector3 returnPos)
    {
        mainWorldSystem = worldSystem;
        returnPosition = returnPos;
    }

    private void Update()
    {
        // Press ESC or walk to exit to return to main world
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnToMainWorld();
        }
    }

    public void ReturnToMainWorld()
    {
        if (mainWorldSystem != null)
        {
            mainWorldSystem.ReturnToMainWorld(returnPosition);
            Destroy(gameObject);
        }
    }
}