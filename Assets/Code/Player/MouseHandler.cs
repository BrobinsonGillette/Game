using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MouseHandler : MonoBehaviour
{
    private MapMaker mapMaker;
    private HexTile currentHoveredTile = null;
    private HexTile clickedTile = null;

    void Start()
    {
        mapMaker = MapMaker.instance;
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
        if(clickedTile != this.clickedTile)
        {
            if(this.clickedTile != null)
            {
                this.clickedTile.DeSelect();
            }

            this.clickedTile = clickedTile;

            if (this.clickedTile != null)
            {
                this.clickedTile.Interact();
            }
        }
    }

  
}


