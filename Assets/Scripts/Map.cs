using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Map : MonoBehaviour
{
    List<Territory> territories = new List<Territory>();
    public GameObject greyPlane;
    static Map instance;
    public void Awake()
    {
    //As the program commences the map creates a list of all territories on it
        instance = this;
        foreach(Transform child in transform)
        {
            if (child.TryGetComponent<Territory>(out Territory territory))
                territories.Add(territory);
        }
    }
    public static Territory GetTerritoryUnderPosition(Vector3 pos)
    {
        foreach(Territory currentTerritory in instance.territories) 
        {
            if (currentTerritory.CheckIfPosIsInside(pos))
            {
                return currentTerritory;
            }
        }
        return null;
    }
    public static void SetActiveGreyPlane(bool active)
    {
        instance.greyPlane.SetActive(active);
    }
    [ContextMenu("Update Neighbours")]
    public void UpdatesNeighbours()
    //Provides a rough estimation for neighbours that is around 80% accurate,
    //neighbours should be pruned by hand afterwards to ensure they are correct
    //and to connect land-sea routes
    {
        for (int i = 0; i < territories.Count; i++)
        {
            territories[i].SetNeighbours(new List<Territory>());
            for (int j = 0; j < territories.Count; j++)
            {
                Debug.Log("works");
                if (i != j)
                {
                    
                    if (territories[i].GetBounds().Intersects(territories[j].GetBounds()))
                    {
                        territories[i].GetNeighbours().Add(territories[j]);
                    }
                }
            }
        }
    }
    [ContextMenu("Setup Map")]
    public void SetupMap()
    //Adds all territories to the map as a context menu option so map operations can be performed when the program is not running
    {
        instance = this;
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent<Territory>(out Territory territory))
                territories.Add(territory);
        }
    }

}
