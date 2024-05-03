using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Territory;
using MonitorBreak.Bebug;

/// <summary>
/// <c>Map</c> represents the logical game map
/// </summary>
public class Map : MonoBehaviour
{
    public static int GetNumberOfTerritoriesForAPlayerIndex(int index)
    {
        int count = 0;
        foreach (Territory territory in instance.territories)
        {
            if (territory.GetOwner() == index)
            {
                count++;
            }
        }

        return count;
    }

    public static List<int> GetAlivePlayers()
    {
        List<int> indexList = new List<int>();

        foreach (Territory territory in instance.territories)
        {
            if (!indexList.Contains(territory.GetOwner()))
            {
                indexList.Add(territory.GetOwner());
            }
        }

        return indexList;
    }


    [SerializeField]
    List<Territory> territories = new List<Territory>();
    /// <summary>
    /// Sets a particular territories troop count 
    /// </summary>
    /// <param name="index">The index of the territory to set</param>
    /// <param name="newCount">The new troop count</param>
    /// <param name="makeRequest">Make a request to the server to update this value across lobby?</param>
    public static void SetTerritoryTroopCount(int index, int newCount, bool makeRequest)
    {
        instance.territories[index].SetCurrentTroops(newCount, makeRequest);
    }
    /// <summary>
    /// Get a territory from the map based on the index
    /// </summary>
    /// <param name="index">The territories index</param>
    /// <returns>The target territory, if it doesn't exist return null</returns>
    public static Territory GetTerritory(int index)
    {
        if (instance != null && instance.territories.Count > index)
        {
            return instance.territories[index];
        }

        return null;
    }

    /// <summary>
    /// Sets a particular territories owner
    /// </summary>
    /// <param name="index">The index of the territory to set</param>
    /// <param name="newOwnerIndex">The new owner</param>
    /// <param name="makeRequest">Make a request to the server to update this value across lobby?</param>
    public static void SetTerritoryOwner(int index, int newOwnerIndex, bool makeRequest)
    {
        instance.territories[index].SetOwner(newOwnerIndex, makeRequest);
    }

    private static HashSet<(int, int)> capitals = new HashSet<(int, int)>();
    /// <summary>
    /// Adds a new capital to a specified territory on the map
    /// </summary>
    /// <param name="territory">The territory to add the capital onto</param>
    /// <param name="owner">The player who owns this capital</param>
    public static void AddCapital(int territory, int owner, bool makeARequest = true)
    {
        //Update locally
        capitals.Add((territory, owner));

        //Spawn ui element signify this territory is a capital
        Color capitalColor = Player.GetColourBasedOnIndex(owner);
        capitalColor *= 1.25f;
        capitalColor.a = 1f;

        UIManagement.Spawn<Image>(Map.GetTerritory(territory).GetUIOffset(), 1).component.color = capitalColor;

        if (makeARequest && NetworkManagement.GetClientState() != NetworkManagement.ClientState.Offline)
        {
            NetworkConnection.UpdateCapitalAcrossLobby(territory, owner);
        }
    }
    /// <summary>
    /// Checks to see if a particular player holds every capital on the board
    /// </summary>
    /// <param name="target">The ID of the player we want to check</param>
    /// <returns>True if the specified player has every capital, else false</returns>
    public static bool DoesPlayerHoldAllCapitals(int target)
    {
        foreach ((int, int) capital in capitals)
        {
            //If they don't own this capital
            if (Map.GetTerritory(capital.Item1).GetOwner() != target)
            {
                //Quit early
                return false;
            }
        }

        return true;
    }


    public static Dictionary<Territory.Continent, List<Territory>> continents = new Dictionary<Territory.Continent, List<Territory>>(); 
    static Map instance;

    public enum AttackResult
    {
        Won,
        Lost,
        Cancelled //Can only be triggered by players
    }

    public void Start()
    {
        SetupMap();
    }
    /// <summary>
    /// Gets the territory that encapsulates the position on the board
    /// </summary>
    /// <param name="pos">The position to check</param>
    /// <returns>Returns the territory that the passed position is in if it exists, else it returns false</returns>
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
    /// <summary>
    /// Runs the necessary steps before commencing an attack, such as ensuring it is a valid attack and getting the attacking dice count
    /// </summary>
    /// <param name="attacker">The territory the attack is coming from</param>
    /// <param name="defender">The territory being attacked</param>
    public static void RequestAttack(Territory attacker, Territory defender)
    {
        //! We don't currently account for the attacker and defender both being players but on different machines
        //will do that when it is needed

        //Need this later
        //After attack has finished we need to notify the attacker that they have won or lost
        //No need to notify defender (at least the actual game object) with current game structure
        int attackingPlayer = attacker.GetOwner();

        if (attackingPlayer == -1) 
        {
            return;
        }

        //If the attacker and defender are both ai skip over ui step
        //If not we need to wait (open dice roll menu) and come back to it later
        //Dice roll menu will then run attack when it is needed
        bool attackerIsLocalPlayer = attackingPlayer == PlayerInputHandler.GetLocalPlayerIndex();

        if (attackingPlayer == defender.GetOwner()) 
        {
            throw new System.Exception("LocalPlayer is attacking itself!");
        }
        else if (attackerIsLocalPlayer)
        {
            //Activate dice roll menu
            DiceRollMenu.Activate(attacker, defender);
        }
        else
        {
            //Just straight run attack
            Attack(attacker, defender, MatchManager.GetPlayerFromIndex(attackingPlayer).GetAttackingDice(attacker));
        }
    }
    /// <summary>
    /// Calculates the result of an attack and sets troops and territories the match that result
    /// </summary>
    /// <param name="attacker">The territory the attack is coming from</param>
    /// <param name="defender">The territory being attacked</param>
    /// <param name="attackingDice">The number of dice the attacker is using</param>
    /// <param name="defendingDice">The number of dice the defender is using</param>
    public static void Attack(Territory attacker, Territory defender, int attackingDice)
    {
        //Get max possible defending dice based on territory
        int defendingDice = Player.GetMaxDefendingDice(defender);


        List<int> attackingRolls = new List<int>();
        List<int> defendingRolls = new List<int>();
        for (int i = 0; i < attackingDice; i++)
        {
            attackingRolls.Add(Random.Range(1, 7));
        }
        for (int i = 0; i < defendingDice; i++)
        {
            defendingRolls.Add(Random.Range(1, 7));
        }
        attackingRolls.Sort();
        attackingRolls.Reverse();
        defendingRolls.Sort();
        defendingRolls.Reverse();

        for (int i = 0; i < defendingRolls.Count && i < attackingRolls.Count; i++)
        {
            string newOutputLine = "(" + attackingRolls[i] + " vs " + defendingRolls[i] + ")";
            if (attackingRolls[i] > defendingRolls[i])
            {
                defender.SetCurrentTroops(defender.GetCurrentTroops() - 1);
            }
            else
            {
                attacker.SetCurrentTroops(attacker.GetCurrentTroops() - 1);
            }
            UIManagement.AddLineToRollOutput(newOutputLine);
        }

        AttackResult attackResult = defender.GetCurrentTroops() <= 0 ? AttackResult.Won : AttackResult.Lost;

        Player attackerPlayer = MatchManager.GetPlayerFromIndex(attacker.GetOwner());

        if (defender.GetCurrentTroops() <= 0)
        {
            int defenderIndex = defender.GetOwner();
            defender.SetOwner(attacker.GetOwner());
            attackerPlayer.AddTerritory(defender);
            UIManagement.AddLineToRollOutput("Territory Taken!");

            List<int> alivePlayers = GetAlivePlayers();
            if (!alivePlayers.Contains(defenderIndex))
            {
                attackerPlayer.Killed(PlayerInfoHandler.CardCountForIndex(defender.GetOwner()));
            }
        }
        else
        {
            UIManagement.AddLineToRollOutput("Territory Defended!");
        }

        //Notify the attacker
        //owner will be null if game was just won
        //This is why the win check is placed above this function call
        if (attacker.GetOwner() != -1)
        {
            attackerPlayer.OnAttackEnd(attackResult, attacker, defender,attackingDice);
        }

        //Refresh UI
        UIManagement.RefreshRollOutput();
    }

    /// <summary>
    /// Checks to see if this is a real game, or a simulated game as part of a menu
    /// </summary>
    /// <returns>True if its a simulated game, else false</returns>
    public static bool IsSimulated()  
    {
        return SceneManager.GetActiveScene().buildIndex == 0; //Menu Scene
    }
    public static List<Territory> GetTerritories() { return instance.territories; }
    public static List<Territory> TerritoriesOwnedByPlayerWorth(int player, out int troopCount)
    {
        List<Territory> ownedTerritories = new List<Territory>();
        troopCount = 0;
        foreach(Territory.Continent continent in Map.continents.Keys)
        {
            bool continentOwned = true;
            foreach(Territory territory in Map.continents[continent]) 
            {
                if (territory.GetOwner() == player) { ownedTerritories.Add(territory); }
                else { continentOwned = false; }
            }
            if( continentOwned ) { troopCount += Territory.continentValues[continent]; }
        }
        int territoryWorth = (int)Mathf.Floor(ownedTerritories.Count / 3);
        if (territoryWorth < 3) { troopCount += 3; }
        else {  troopCount += territoryWorth; }
        return ownedTerritories;
    }

    /// <summary>
    /// Returns a list of territories owned by a particular player
    /// </summary>
    /// <param name="player">The player whos owned territories we want</param>
    /// <returns>The list of territories owned by the passed player </returns>
    public static List<Territory> GetTerritoriesOwnedByPlayer(int player)
    {
        List<Territory> ownedTerritories = new List<Territory>();
            foreach (Territory territory in instance.territories)
            {
                if (territory.GetOwner() == player) { ownedTerritories.Add(territory); }
            }
        return ownedTerritories;
    }
    /// <summary>
    /// A helper method for AI setup, returns the continent that is closest to being fully claimed that the passed player owns a territory in
    /// </summary>
    /// <param name="player">The player that must own a territory in the returned continent</param>
    /// <returns>The continent closest to capture by the passed player</returns>
    public static Continent GetContinentClosestToCaptured(int player)
    {
        bool playerHasGround = false;
        bool continentFullyOwned = true;
        int territoriesLeft = 0;
        int bestTerritoriesLeft = 1000;
        Continent bestContinent = Continent.Asia;
        foreach(Continent continent in Map.continents.Keys)
        {
            foreach (Territory territory in Map.continents[continent])
            {
                if (territory.GetOwner() == -1) { continentFullyOwned = false; }
                if (territory.GetOwner() != player) { territoriesLeft++; }
                else
                {
                    playerHasGround = true;
                }
            }
            if ((territoriesLeft < bestTerritoriesLeft) && !continentFullyOwned && playerHasGround)
            {
                bestTerritoriesLeft = territoriesLeft;
                bestContinent = continent;
            }
            territoriesLeft = 0;
            continentFullyOwned = false;
            playerHasGround = false;
        }
        return bestContinent;
    }
    /// <summary>
    /// Checks to see if the given contient is owned by a player
    /// </summary>
    /// <param name="continent">The continent to be check for ownership</param>
    /// <returns>The player ID of the owner if the continent is owned by a player, else -1</returns>
    public static int GetContinentOwner(Continent continent)
    {
        int owner = Map.continents[continent][0].GetOwner();
        foreach (Territory territory in Map.continents[continent])
        {
            if(territory.GetOwner() != owner)
            {
                return -1;
            }
        }
        return owner;
    }
    /// <summary>
    /// Returns a list of territories with no owner
    /// </summary>
    /// <param name="player">The player whos territories we are looking for</param>
    /// <param name="playerTerritories">An out parameter that holds a list of territories owned by the input player</param>
    /// <returns></returns>
    public static List<Territory> GetUnclaimedTerritories(int player, out List<Territory> playerTerritories )
    {
        List<Territory> unownedTerritories = new List<Territory>();
        playerTerritories = new List<Territory>();
        foreach (Territory territory in instance.territories)
        {
            if (territory.GetOwner() == -1)
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

    /// <summary>
    /// Resets the map to its starting state
    /// </summary>
    public static void ResetInstanceMap()
    {
        instance.SetupMap();
    }

    /// <summary>
    /// Adds all territories to the map as well as creating the continents dictionary and clearing capitals
    /// </summary>
    public void SetupMap()
    {
        //Load territory name overriding
        Dictionary<int, string> nameDict = MapEditor.LoadTerritoryNamesList();
        //Load territory extra neighbours
        Dictionary<int, List<int>> extraNeighboursDict = MapEditor.LoadExtraNeighboursList();

        if (PlayOptionsManagement.IsConquestMode())
        {
            capitals.Clear();
        }
        territories = new List<Territory>(); 
        continents = new Dictionary<Territory.Continent, List<Territory>>();
        instance = this;
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent<Territory>(out Territory territory))
            {
                territories.Add(territory);
                territory.ResetTerritory(territories.Count-1);

                if (nameDict.TryGetValue(territories.Count-1, out string newTerrOverName))
                {
                    territory.SetName(newTerrOverName);
                }

                if (!continents.ContainsKey(territory.GetContinent()))
                {
                    continents[territory.GetContinent()] = new List<Territory>();
                }
                continents[territory.GetContinent()].Add(territory);
            }
        }

        //We do this after so we can get the actual territory objects
        for (int i = 0; i < territories.Count; i++)
        {
            if (extraNeighboursDict.ContainsKey(i))
            {
                territories[i].SetExtraNeighbours(extraNeighboursDict[i]);
            }
        }
    }

}
