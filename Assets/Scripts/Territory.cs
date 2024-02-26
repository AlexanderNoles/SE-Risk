using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Territory
{
    object owner;
    int currentTroops;
    [SerializeField]
    List<Vector3> borderPoints;
    

    public bool CheckIfMouseIsInside (Vector3 mousePos)
    {
        return PolygonUtility.DoesPointExistInsidePolygon(borderPoints, mousePos);
    }


}
