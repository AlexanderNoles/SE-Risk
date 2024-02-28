using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;

public class Territory : MonoBehaviour
{
    object owner;
    int currentTroops;
    [SerializeField]
    List<Vector3> borderPoints;
    [SerializeField]
    Bounds bounds = new Bounds();
    [SerializeField]
    Vector3 centrePoint;
    const float inflationRatio = 1.1f;
    private SpriteRenderer spriteRenderer;
    private TextMeshProUGUI troopLabel;


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
   
    public void Inflate()
    {
        transform.localScale = Vector3.one*inflationRatio;
        Vector3 newCentre = centrePoint * inflationRatio;
        Vector3 difference= newCentre-centrePoint;
        Debug.Log(difference);
        transform.localPosition = -difference;
        spriteRenderer.sortingOrder = 1000;
    }

    public void Deflate()
    {
        transform.localScale = Vector3.one;
        transform.localPosition = Vector3.zero;
        spriteRenderer.sortingOrder = 0;
    }

    public Bounds GetBounds() { return bounds; }
    public Vector3 GetCentrePoint() { return centrePoint; }
    public void SetCentrePoint(Vector3 centrePoint) { this.centrePoint = centrePoint; }
    public object GetOwner () { return owner; }
    public void SetOwner (object owner) { this.owner = owner;}
    public int GetCurrentTroops() { return currentTroops; }
    public void SetCurrentTroops(int currentTroops) 
    { 
        this.currentTroops = currentTroops;
        troopLabel.text = currentTroops.ToString();
    }
    public List<Vector3> GetBorderPoints () {  return borderPoints; }
    public void SetBorderPoints(List<Vector3> newPoints) 
    {
        borderPoints = newPoints;
        CalculateBounds();
    }

    public void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        troopLabel = UIManagement.Spawn<TextMeshProUGUI>(centrePoint, 0).component;
        SetCurrentTroops(0);
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < borderPoints.Count - 1; i++)
        {
            Gizmos.color = Color.Lerp(Color.green, Color.red, ((float)i / borderPoints.Count));
            Gizmos.DrawLine(borderPoints[i], borderPoints[i+1]);
        }
        Gizmos.DrawLine(Vector3.zero,centrePoint);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
