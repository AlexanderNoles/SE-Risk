using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;

public class Territory : MonoBehaviour
{
    Player owner;
    int currentTroops;
    [SerializeField]
    List<Vector3> borderPoints;
    [SerializeField]
    Bounds bounds = new Bounds();
    [SerializeField]
    Vector3 centrePoint;
    [SerializeField]
    Vector3 textOffset = new Vector3(0, 0, 0);
    [SerializeField]
    List<Territory> neighbours = new List<Territory>();
    [SerializeField]
    Continent continent;
    public enum Continent { Africa, South_America, North_America, Europe, Asia, Australia }
    public static Dictionary<Continent, int> continentValues = new Dictionary<Continent,int>() { { Continent.Africa, 3 }, { Continent.Europe, 5 }, { Continent.Asia, 7 }, { Continent.North_America, 5 }, { Continent.Australia, 2 }, { Continent.South_America, 2 } };
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
        //calculates the minimum bounding box that encapsulates all the points of the polygon
        bounds = new Bounds();
        bounds.center = borderPoints[0];
        foreach(Vector3 point in borderPoints)
        {
            bounds.Encapsulate(point);
        }
        bounds.Expand(new Vector3(0, 0, 1)); //expands the bounding box so that it exists in 3 dimensions
    }
   
    public void Inflate()
    {
        //increases the size of the territory slightly and brings it to the front of the sorting order
        transform.localScale = Vector3.one*inflationRatio;
        Vector3 newCentre = centrePoint * inflationRatio;
        Vector3 difference= newCentre-centrePoint;
        transform.localPosition = -difference;
        spriteRenderer.sortingOrder = 1000;
    }

    public void Deflate()
    {
        //reverses the inflate function
        transform.localScale = Vector3.one;
        transform.localPosition = Vector3.zero;
        spriteRenderer.sortingOrder = 0;
    }

    public Bounds GetBounds() { return bounds; }
    public Vector3 GetCentrePoint() { return centrePoint; }
    public void SetCentrePoint(Vector3 centrePoint) { this.centrePoint = centrePoint; }
    public Player GetOwner () { return owner; }
    public void SetOwner (Player owner) 
    { 
        this.owner = owner; 
        spriteRenderer.color = owner.GetColor(); 
    }
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
    public List<Territory> GetNeighbours() { return neighbours; }
    public void SetNeighbours(List<Territory> neighbours) { this.neighbours = neighbours; }
    public Continent GetContinent() { return continent; }
    public void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    public void Start()
    {
        //spawns a troop label for each territory and sets it to display 0
        troopLabel = UIManagement.Spawn<TextMeshProUGUI>(centrePoint + textOffset, 0).component;
        SetCurrentTroops(0); 
    }



    private void OnDrawGizmos()
    {
        for (int i = 0; i < borderPoints.Count - 1; i++)
        {
            Gizmos.color = Color.Lerp(Color.green, Color.red, ((float)i / borderPoints.Count));
            Gizmos.DrawLine(borderPoints[i], borderPoints[i+1]);
        }
        Gizmos.DrawLine(Vector3.zero, centrePoint);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
