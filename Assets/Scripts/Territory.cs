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
    [SerializeField]
    Bounds bounds = new Bounds();


    public bool CheckIfPosIsInside (Vector3 pos)
    {
        if (bounds.Contains(pos))
        {
            return PolygonUtility.DoesPointExistInsidePolygon(borderPoints, pos);
        }
        else
        {
            return false;
        }
    }

    public void CalculateBounds()
    {
        bounds = new Bounds();
        bounds.center = borderPoints[0];
        foreach(Vector3 point in borderPoints)
        {
            bounds.Encapsulate(point);
        }
        bounds.Expand(new Vector3(0, 0, 1));
    }
   
    public object GetOwner () { return owner; }
    public void SetOwner (object owner) { this.owner = owner;}
    public int GetCurrentTroops() { return currentTroops; }
    public void SetCurrentTroops(int currentTroops) { this.currentTroops = currentTroops;}
    public List<Vector3> GetBorderPoints () {  return borderPoints; }
    public void SetBorderPoints(List<Vector3> newPoints) 
    {
        borderPoints = newPoints;
        CalculateBounds();
    }



    private void OnDrawGizmos()
    {
        for (int i = 0; i < borderPoints.Count - 1; i++)
        {
            Gizmos.color = Color.Lerp(Color.green, Color.red, ((float)i / borderPoints.Count));
            Gizmos.DrawLine(borderPoints[i], borderPoints[i+1]);
        }
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
