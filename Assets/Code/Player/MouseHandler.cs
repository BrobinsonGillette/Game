using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MouseHandler : MonoBehaviour
{
    private MapMaker mapMaker;
    private HexTile currentHoveredTile = null;
    private HexTile ClickedTile = null;
    private Char SectedPlayer;
    void Start()
    {
        mapMaker = MapMaker.instance;
        if (mapMaker == null)
        {
            Debug.Log("MapMaker not found! HexMouseHandler needs MapMaker to work.");
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

        if(clickedTile != ClickedTile)
        {
            if(ClickedTile != null)
            {
                ClickedTile.DeSelect();
            }

            ClickedTile = clickedTile;
            if (ClickedTile.hasPlayer && SectedPlayer == null)
            {
                handleClickOnChar(clickedTile);
            }
            else
            {
                if (ClickedTile != null && SectedPlayer == null)
                {
                    ClickedTile.Interact();
                }
                else
                {
                    SectedPlayer.MovePlayerToTile(clickedTile);
                    ClickedTile.DeSelect();
                    SectedPlayer = null;
                }
            }
        }
    }
    void handleClickOnChar(HexTile clickedTile)
    {
        if (clickedTile == null) return;
        // If we already have a selected player, deselect them
        if (SectedPlayer != null)
        {
            ClickedTile.DeSelect();
            SectedPlayer = null;
        }
        // Select the clicked player
        Char clickedChar = clickedTile.CurrentPlayer;
        if (clickedChar != null)
        {
            SectedPlayer = clickedChar;
            ClickedTile.Interact();
        }
    }

  
}


