using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
    private Camera m_Camera;
    private Vector3 mousePosLastFrame;
    private Territory currentTerritoryUnderMouse = null;
    private state currentState;
    private enum state {MapView,Selected}
    public void Awake()
    {
        m_Camera = Camera.main;
    }
    public void Update()
    {
        if (currentState == state.MapView)
        {
            Vector3 mousePosThisFrame = m_Camera.ScreenToWorldPoint(Input.mousePosition);
            mousePosThisFrame.z = 0;
            if (mousePosThisFrame != mousePosLastFrame)
            {
                mousePosLastFrame = mousePosThisFrame;
                Territory hoveredTerritory = Map.GetTerritoryUnderPosition(mousePosThisFrame);

                if (currentTerritoryUnderMouse != hoveredTerritory)
                {
                    if (currentTerritoryUnderMouse != null)
                    {
                        currentTerritoryUnderMouse.Deflate();
                    }
                    if (hoveredTerritory != null)
                    {
                        hoveredTerritory.Inflate();
                    }
                }
                currentTerritoryUnderMouse = hoveredTerritory;
            }
            if (currentTerritoryUnderMouse != null)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    SelectTerritory();
                }
            }
        }
        else if (currentState==state.Selected)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                DeselectTerritory();
            }
        }
    }

    public void SelectTerritory()
    {
        currentState = state.Selected;
        Map.SetActiveGreyPlane(true);
        m_Camera.transform.position = currentTerritoryUnderMouse.GetCentrePoint() + Vector3.forward*-10;
        Vector3 extents = currentTerritoryUnderMouse.GetBounds().extents;
        float diagLength = Mathf.Sqrt(extents.x*extents.x + extents.y*extents.y);
        m_Camera.orthographicSize = diagLength*2;
    }
    public void DeselectTerritory()
    {
        currentState = state.MapView;
        Map.SetActiveGreyPlane(false);
        m_Camera.orthographicSize = 7;
    }
}
