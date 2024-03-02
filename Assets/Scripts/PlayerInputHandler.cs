using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
    private Camera m_Camera;
    private Vector3 mousePosLastFrame;
    private Territory currentTerritoryUnderMouse = null;
    private state currentState;
    private float zoomTime = 0.2f;
    Vector3 startPos;
    Vector3 cameraMoveVector;
    float startSize;
    float endSize;
    float executionTime;
    const float defaultCameraSize = 7;
    [SerializeField]
    UIManagement pools;
    [SerializeField]
    TroopTransporter troopTransporter;
    private enum state {MapView,Selected,Zooming}
    public void Awake()
    {
        m_Camera = Camera.main;
        currentState = state.MapView;
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
            else if (currentTerritoryUnderMouse != null)
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
                troopTransporter.gameObject.SetActive(false);
                DeselectTerritory();
            }
            else if (Input.GetKeyDown(KeyCode.Return))
            {
                troopTransporter.FinaliseTerritoryTroopCounts();
                DeselectTerritory();
            }
        }


        else if (currentState==state.Zooming) 
        {
            if (executionTime < zoomTime)
            {
                float deltaTime = Time.deltaTime;
                executionTime += deltaTime;
                float completionRate = (executionTime / zoomTime);
                m_Camera.transform.position = startPos + (cameraMoveVector * completionRate);
                m_Camera.orthographicSize = startSize - ((startSize-endSize) * completionRate);
            }
            else
            {
                m_Camera.transform.position = startPos + cameraMoveVector;
                m_Camera.orthographicSize = endSize;
                if (m_Camera.orthographicSize == defaultCameraSize)
                {
                    currentState = state.MapView;
                }
                else 
                {
                    currentState = state.Selected;
                    troopTransporter.SetupTroopTransporter(currentTerritoryUnderMouse,999);
                }
                
            }
        }
    }

    public void SelectTerritory()
    {
       currentState = state.Zooming;
       Map.SetActiveGreyPlane(true);
       Vector3 extents = currentTerritoryUnderMouse.GetBounds().extents;
       float diagLength = Mathf.Sqrt(extents.x * extents.x + extents.y * extents.y);
       startPos = m_Camera.transform.position;
       cameraMoveVector = (currentTerritoryUnderMouse.GetCentrePoint() + Vector3.forward * -10) - m_Camera.transform.position;
        startSize = defaultCameraSize;
       endSize = diagLength * 2;
       executionTime = 0;
        pools.GetComponent<Canvas>().sortingOrder = 0;
    }
    public void DeselectTerritory()
    {
        currentState = state.Zooming;
        Map.SetActiveGreyPlane(false);
        startPos = m_Camera.transform.position;
        cameraMoveVector = new Vector3(0, 0, -10) - startPos;
        startSize = m_Camera.orthographicSize;
        endSize = defaultCameraSize;
        executionTime = 0;
        pools.GetComponent<Canvas>().sortingOrder = 2;
    }
}
