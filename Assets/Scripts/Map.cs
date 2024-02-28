using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Map : MonoBehaviour
{
    List<Territory> territories = new List<Territory>();
    static Map instance;
    public void Awake()
    {  
        instance = this;
        foreach(Transform child in transform)
        {
            if (child.TryGetComponent<Territory>(out Territory territory))
                territories.Add(territory);
        }
        Debug.Log(territories.Count);
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
}
