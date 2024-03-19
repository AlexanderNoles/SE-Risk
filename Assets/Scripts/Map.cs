using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Map : MonoBehaviour
{
    [SerializeField]
    List<Territory> territories = new List<Territory>();
    Dictionary<Territory.Continent, List<Territory>> continents = new Dictionary<Territory.Continent, List<Territory>>(); 
    public GameObject greyPlane;
    static Map instance;

    public void Awake()
    {
        SetupMap();
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
    public static bool Attack(Territory attacker, Territory defender, int attackingTroops)
    {
        List<int> attackingRolls = new List<int>();
        List<int> defendingRolls = new List<int>();
        bool taken = false;
        for (int i = 0; i < attackingTroops && i<3; i++)
        {
            attackingRolls.Add(Random.Range(1, 7));
        }
        for (int i = 0; i < defender.GetCurrentTroops() && i<2 && i<attackingRolls.Count; i++)
        {
            defendingRolls.Add(Random.Range(1, 7));
        }
        attackingRolls.Sort();
        attackingRolls.Reverse();
        defendingRolls.Sort();
        defendingRolls.Reverse();
        for (int i = 0; i < defendingRolls.Count; i++)
        {
            if (attackingRolls[i] > defendingRolls[i])
            {
                defender.SetCurrentTroops(defender.GetCurrentTroops()-1);
            }
            else
            {
                attacker.SetCurrentTroops(attacker.GetCurrentTroops() - 1);
            }
        }
        if(defender.GetCurrentTroops() <= 0)
        {
            defender.SetOwner(attacker.GetOwner());
            attacker.GetOwner().AddTerritory(defender);
            defender.SetCurrentTroops(attacker.GetCurrentTroops()-1>=3?3:attacker.GetCurrentTroops()-1);
            attacker.SetCurrentTroops(attacker.GetCurrentTroops() - defender.GetCurrentTroops());
            taken = true;
        }
        return taken;
    }
    public static List<Territory> GetTerritories() { return instance.territories; }
    public static List<Territory> TerritoriesOwnedByPlayer(Player player, out int troopCount)
    {
        List<Territory> ownedTerritories = new List<Territory>();
        troopCount = 0;
        foreach(Territory.Continent continent in instance.continents.Keys)
        {
            bool continentOwned = true;
            foreach(Territory territory in instance.continents[continent]) 
            {
                if (territory.GetOwner()==player) { ownedTerritories.Add(territory); }
                else { continentOwned = false; }
            }
            if( continentOwned ) { troopCount += Territory.continentValues[continent]; }
        }
        int territoryWorth = (int)Mathf.Floor(ownedTerritories.Count / 3);
        if (territoryWorth < 3) { troopCount += 3; }
        else {  troopCount += territoryWorth; }
        return ownedTerritories;
    }
    public static List<Territory> GetUnclaimedTerritories(Player player, out List<Territory> playerTerritories )
    {
        List<Territory> unownedTerritories = new List<Territory>();
        playerTerritories = new List<Territory>();
        foreach (Territory territory in instance.territories)
        {
            if (territory.GetOwner() == null)
            {
                unownedTerritories.Add(territory);
            }
            else if (territory.GetOwner() == player)
            {
                playerTerritories.Add(territory);
            }
        }
        return unownedTerritories;
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
        territories = new List<Territory>();
        continents = new Dictionary<Territory.Continent, List<Territory>>();
        instance = this;
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent<Territory>(out Territory territory))
            {
                territories.Add(territory);
                if (!continents.ContainsKey(territory.GetContinent()))
                {
                    continents[territory.GetContinent()] = new List<Territory>();
                }
                continents[territory.GetContinent()].Add(territory);
            }
        }
    }

}
