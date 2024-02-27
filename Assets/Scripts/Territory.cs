using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Territory : MonoBehaviour
{
    object owner;
    int currentTroops;
    [SerializeField]
    List<Vector3> borderPoints;
    

    public bool CheckIfMouseIsInside (Vector3 mousePos)
    {
        return PolygonUtility.DoesPointExistInsidePolygon(borderPoints, mousePos);
    }
   
    public object GetOwner () { return owner; }
    public void SetOwner (object owner) { this.owner = owner;}
    public int GetCurrentTroops() { return currentTroops; }
    public void SetCurrentTroops(int currentTroops) { this.currentTroops = currentTroops;}
    public List<Vector3> GetBorderPoints () {  return borderPoints; }
    public void SetBorderPoints(List<Vector3> newPoints) {  borderPoints = newPoints;
        for (int i = 0; i < borderPoints.Count - 1; i++)
        {
            Debug.DrawLine(borderPoints[i], borderPoints[i+1], Color.green, 10000);
        }
    }  

}
