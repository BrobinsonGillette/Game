using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexMouseHandler : MonoBehaviour
{
    private MapMaker mapMaker;
    private HexTile currentHoveredTile = null;
    private HexTile lastHoveredTile = null;

    void Start()
    {
        mapMaker = FindObjectOfType<MapMaker>();
        if (mapMaker == null)
        {
            Debug.LogError("MapMaker not found! HexMouseHandler needs MapMaker to work.");
        }
    }

    void Update()
    {
        HandleMouseInput();
    }

    void HandleMouseInput()
    {
        if (mapMaker == null || CamMagger.instance == null) return;

        // Get world mouse position from CamMagger
        Vector3 worldMousePos = CamMagger.instance.WorldMousePosition;

        // Convert world position to hex coordinates
        Vector2Int hexCoord = mapMaker.WorldToHexPosition(worldMousePos);

        // Get the hex tile at those coordinates
        HexTile hoveredTile = mapMaker.GetHexTile(hexCoord);

        // Handle hover logic
        HandleHover(hoveredTile);

        // Handle click interactions
        if (Input.GetMouseButtonDown(0)) // Left mouse button
        {
            HandleClick(hoveredTile);
        }
    }

    void HandleHover(HexTile newHoveredTile)
    {
        // If we're hovering over a different tile
        if (newHoveredTile != currentHoveredTile)
        {
            // Call MouseExit on the previously hovered tile
            if (currentHoveredTile != null)
            {
                currentHoveredTile.MouseExit();
            }

            // Update current hovered tile
            lastHoveredTile = currentHoveredTile;
            currentHoveredTile = newHoveredTile;

            // Call MousedOver on the new tile
            if (currentHoveredTile != null)
            {
                currentHoveredTile.MousedOver();
            }
        }
    }

    void HandleClick(HexTile clickedTile)
    {
        if (clickedTile != null)
        {
            clickedTile.Interact();
        }
    }

    // Optional: Handle right-click or other mouse buttons
    void HandleRightClick(HexTile clickedTile)
    {
        if (clickedTile != null)
        {
            // You can add custom right-click behavior here
            Debug.Log($"Right-clicked on hex tile: {clickedTile.coordinates}");
        }
    }

    // Call this when the mouse leaves the game area entirely
    public void OnMouseLeaveGameArea()
    {
        if (currentHoveredTile != null)
        {
            currentHoveredTile.MouseExit();
            currentHoveredTile = null;
        }
    }
}

// Alternative approach: Add this to your MapMaker.cs instead
public class MapMakerMouseExtension : MonoBehaviour
{
    private MapMaker mapMaker;
    private HexTile currentHoveredTile = null;

    void Start()
    {
        mapMaker = GetComponent<MapMaker>();
    }

    void Update()
    {
        if (mapMaker == null || CamMagger.instance == null) return;

        // Get world mouse position
        Vector3 worldMousePos = CamMagger.instance.WorldMousePosition;

        // Convert to hex coordinates
        Vector2Int hexCoord = mapMaker.WorldToHexPosition(worldMousePos);

        // Get the tile
        HexTile hoveredTile = mapMaker.GetHexTile(hexCoord);

        // Handle hover state changes
        if (hoveredTile != currentHoveredTile)
        {
            // Exit previous tile
            if (currentHoveredTile != null)
            {
                currentHoveredTile.MouseExit();
            }

            // Enter new tile
            currentHoveredTile = hoveredTile;
            if (currentHoveredTile != null)
            {
                currentHoveredTile.MousedOver();
            }
        }

        // Handle clicks
        if (Input.GetMouseButtonDown(0) && currentHoveredTile != null)
        {
            currentHoveredTile.Interact();
        }
    }
}
