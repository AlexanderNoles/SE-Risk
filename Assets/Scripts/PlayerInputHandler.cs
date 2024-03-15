using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
    Camera m_Camera;
    Vector3 mousePosLastFrame;
    Territory currentTerritoryUnderMouse = null;
    static state currentState;
    static turnPhase currentPhase;
    float zoomTime = 0.2f;
    Vector3 startPos;
    Vector3 cameraMoveVector;
    float startSize;
    float endSize;
    float executionTime;
    const float defaultCameraSize = 5.4f;
    [SerializeField]
    UIManagement pools;
    [SerializeField]
    TroopTransporter troopTransporter;
    static PlayerInputHandler instance;
    Territory newTerritoryUnderMouse;
    Territory selectedTerritory = null;
    Territory toTerritory;
    public static void SetLocalPlayer(LocalPlayer player)
    {
        instance.localPlayer = player;
    }
    LocalPlayer localPlayer;
    enum state {MapView,Selected,Zooming}
    enum turnPhase {Deploying,Attacking,Fortifying,Waiting }
    public void Awake()
    {
        instance = this;
        m_Camera = Camera.main;
        currentPhase = turnPhase.Waiting;
        currentState = state.MapView;
    }
    public void Update()
    {
        if (currentPhase != turnPhase.Waiting)
        {
            if (currentState == state.MapView)
            {
                Vector3 mousePosThisFrame = m_Camera.ScreenToWorldPoint(Input.mousePosition);
                mousePosThisFrame.z = 0;
                if (mousePosThisFrame != mousePosLastFrame)
                {
                    //checks if the mouse has moved
                    mousePosLastFrame = mousePosThisFrame;
                    newTerritoryUnderMouse = Map.GetTerritoryUnderPosition(mousePosThisFrame);
                }
            }
            Territory overridenTerr = newTerritoryUnderMouse;
            if(selectedTerritory != null)
            {
                overridenTerr = selectedTerritory;
            }
            if (currentTerritoryUnderMouse != overridenTerr)
            {
                //checks if the territory under the mouse has changed and inflates and deflates territories accordingly
                if (currentTerritoryUnderMouse != null)
                {
                    currentTerritoryUnderMouse.Deflate();
                }
                if (overridenTerr != null)
                {
                    overridenTerr.Inflate();
                }
            }

            if (currentPhase==turnPhase.Deploying)
            {
                if(currentState == state.MapView) {
                   
                }
                if (currentTerritoryUnderMouse != null)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        if (localPlayer.GetTerritories().Contains(currentTerritoryUnderMouse) && selectedTerritory==null)
                        {
                            currentState = state.Zooming;
                            SelectTerritory();
                        }
                    }
                }
                if (localPlayer.GetTroopCount() == 0 && currentState != state.Zooming)
                {
                    currentTerritoryUnderMouse = null;
                    currentPhase = turnPhase.Waiting;
                    MatchManager.Attack();
                }
                else if (currentState == state.Selected)
                {
                    // if we're zoomed in on a territory
                    if (Input.GetKeyDown(KeyCode.Escape))
                    {
                        selectedTerritory = null;
                        troopTransporter.gameObject.SetActive(false);
                        currentState = state.Zooming;
                        DeselectTerritory();
                    }
                    else if (Input.GetKeyDown(KeyCode.Return))
                    {
                        selectedTerritory = null;
                        localPlayer.SetTroopCount(troopTransporter.FinaliseTerritoryTroopCounts());
                        currentState = state.Zooming;
                        DeselectTerritory();
                    }
                }
            }
            else if (currentPhase == turnPhase.Attacking)
            {
                if (currentTerritoryUnderMouse != null)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        if (selectedTerritory == null)
                        {
                            if (localPlayer.GetTerritories().Contains(currentTerritoryUnderMouse))
                            {
                                SelectTerritory();
                            }
                        }
                        else
                        {
                            if (selectedTerritory.GetNeighbours().Contains(newTerritoryUnderMouse) && newTerritoryUnderMouse.GetOwner()!=selectedTerritory.GetOwner())
                            {
                                if(Map.Attack(selectedTerritory, newTerritoryUnderMouse, selectedTerritory.GetCurrentTroops() - 1))
                                {
                                    toTerritory = newTerritoryUnderMouse;
                                    currentState = state.Zooming;
                                    newTerritoryUnderMouse.Inflate();
                                    SelectTerritory();
                                }
                            }
                        }

                    }
                }
                if (currentState == state.MapView)
                {
                if(selectedTerritory != null)
                {
                    if(Input.GetKeyDown(KeyCode.Escape)||selectedTerritory.GetCurrentTroops()==1)
                    {
                        selectedTerritory.Deflate();
                        selectedTerritory =null;
                    }
                }
                else
                {
                    if(Input.GetKeyDown(KeyCode.Return)|| Input.GetKeyDown(KeyCode.Space))
                    {
                        MatchManager.Fortify();
                    }
                }

                }
                else if (currentState == state.Selected)
                {
                    if (Input.GetKeyDown(KeyCode.Return))
                    {
                        troopTransporter.FinaliseTerritoryTroopCounts();
                        selectedTerritory.Deflate();
                        toTerritory.Deflate();
                        currentState = state.Zooming;
                        DeselectTerritory();
                    }
                }
            }
            else if (currentPhase == turnPhase.Fortifying)
            {
                if (currentTerritoryUnderMouse != null)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        if (selectedTerritory == null)
                        {
                            if (localPlayer.GetTerritories().Contains(currentTerritoryUnderMouse))
                            {
                                SelectTerritory();
                            }
                        }
                        else
                        {
                            if (newTerritoryUnderMouse!=null &&newTerritoryUnderMouse!=selectedTerritory &&newTerritoryUnderMouse.GetOwner() == selectedTerritory.GetOwner() && localPlayer.AreTerritoriesConnected(selectedTerritory, newTerritoryUnderMouse))
                            {
                                troopTransporter.SetupTroopTransporter(newTerritoryUnderMouse, selectedTerritory);
                                Map.SetActiveGreyPlane(true);
                                newTerritoryUnderMouse.Inflate();
                                toTerritory = newTerritoryUnderMouse;
                                currentState = state.Selected;
                            }
                        }

                    }
                }
                if (currentState == state.MapView)
                {
                    if (selectedTerritory != null)
                    {
                        if (Input.GetKeyDown(KeyCode.Escape) || selectedTerritory.GetCurrentTroops() == 1)
                        {
                            selectedTerritory.Deflate();
                            selectedTerritory = null;
                        }
                    }
                    else
                    {
                        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                        {
                            currentPhase = turnPhase.Waiting;
                            MatchManager.EndTurn();
                        }
                    }

                }
                else if (currentState == state.Selected)
                {
                    if (Input.GetKeyDown(KeyCode.Return))
                    {
                        troopTransporter.FinaliseTerritoryTroopCounts();
                        Map.SetActiveGreyPlane(false);
                        troopTransporter.gameObject.SetActive(false);
                        DeselectTerritory();
                        toTerritory.Deflate();
                        currentState = state.MapView;
                        MatchManager.EndTurn();
                    }
                    else if (Input.GetKeyDown(KeyCode.Escape))
                    {
                        Map.SetActiveGreyPlane(false);
                        selectedTerritory = null;
                        troopTransporter.gameObject.SetActive(false);
                        toTerritory.Deflate();
                        currentState = state.MapView;
                    }
                }
            }
            if (currentState == state.Zooming)
            {
                //if we're currently zooming into a territory
                if (executionTime < zoomTime)
                {
                    //calculates the percentage through the zoom we are and changes camera position and size to match
                    float deltaTime = Time.deltaTime;
                    executionTime += deltaTime;
                    float completionRate = (executionTime / zoomTime);
                    m_Camera.transform.position = startPos + (cameraMoveVector * completionRate);
                    m_Camera.orthographicSize = startSize - ((startSize - endSize) * completionRate);
                }
                else
                {
                    //once the zoom is done, ensure the final position is correct, then switch state
                    m_Camera.transform.position = startPos + cameraMoveVector;
                    m_Camera.orthographicSize = endSize;
                    if (m_Camera.orthographicSize == defaultCameraSize)
                    {
                        currentState = state.MapView;
                    }
                    else
                    {
                        currentState = state.Selected;
                        if (currentPhase==turnPhase.Deploying)
                        {
                            troopTransporter.SetupTroopTransporter(currentTerritoryUnderMouse, localPlayer.GetTroopCount());
                        }
                        else
                        {
                            troopTransporter.SetupTroopTransporter(toTerritory, selectedTerritory);
                        }
                    }

                }
            }
            if(currentState == state.MapView) { currentTerritoryUnderMouse = overridenTerr; }
        }
    }

    public void SelectTerritory()
    {
        //precomputes the vlaues needed for the zoom and moves the troop labels behind the grey plane
        selectedTerritory = currentTerritoryUnderMouse;
        if (currentState == state.Zooming)
        {
            Map.SetActiveGreyPlane(true);
            Vector3 extents = currentTerritoryUnderMouse.GetBounds().extents;
            float diagLength = Mathf.Sqrt(extents.x * extents.x + extents.y * extents.y);
            startPos = m_Camera.transform.position;
            cameraMoveVector = (currentTerritoryUnderMouse.GetCentrePoint() + Vector3.forward * -10) - m_Camera.transform.position;
            startSize = defaultCameraSize;
            endSize = diagLength * 2;
            executionTime = 0;
            pools.GetComponent<Canvas>().sortingOrder = 300;
        }
    }
    public void DeselectTerritory()
    {
        //same as SelectTerritory but in reverse
        selectedTerritory = null;
        if (currentState == state.Zooming)
        {
            Map.SetActiveGreyPlane(false);
            startPos = m_Camera.transform.position;
            cameraMoveVector = new Vector3(0, 0, -10) - startPos;
            startSize = m_Camera.orthographicSize;
            endSize = defaultCameraSize;
            executionTime = 0;
            pools.GetComponent<Canvas>().sortingOrder = 1200;
        }
    }
    public static void Deploy()
    {
        currentState = state.MapView;
        currentPhase = turnPhase.Deploying;
    }
    public static void Attack()
    {
        currentState = state.MapView;
        currentPhase = turnPhase.Attacking;
    }
    public static void Fortify()
    {
        currentState = state.MapView;
        currentPhase = turnPhase.Fortifying;
    }
}
