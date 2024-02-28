using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
    private Camera m_Camera;
    private Vector3 mousePosLastFrame;
    private Territory currentTerritoryUnderMouse = null;
    public void Awake()
    {
        m_Camera = Camera.main;
    }
    public void Update()
    {
        Vector3 mousePosThisFrame = m_Camera.ScreenToWorldPoint(Input.mousePosition);
        mousePosThisFrame.z = 0;
        if(mousePosThisFrame != mousePosLastFrame)
        {
            mousePosLastFrame = mousePosThisFrame;
            currentTerritoryUnderMouse = Map.GetTerritoryUnderPosition(mousePosThisFrame);
            Debug.Log(currentTerritoryUnderMouse.name);

        }
        Debug.DrawRay(mousePosThisFrame, Vector3.up, Color.green);
    }
}
