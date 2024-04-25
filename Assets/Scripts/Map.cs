using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Map : MonoBehaviour
{
    [SerializeField]
    List<Territory> territories = new List<Territory>();

    private static HashSet<(Territory, Player)> capitals = new HashSet<(Territory, Player)>();

    public static void AddCapital(Territory territory, Player owner)
    {
        if (IsSimulated())
        {
            return;
        }


        capitals.Add((territory, owner));

        //Spawn ui element signify this territory is a capital
        Color capitalColor = owner.GetColor();
        capitalColor *= 1.25f;
        capitalColor.a = 1f;

        UIManagement.Spawn<Image>(territory.GetUIOffset(), 1).component.color = capitalColor;
    }

    public static bool DoesPlayerHoldAllCapitals(Player target)
    {
        foreach ((Territory, Player) capital in capitals)
        {
            //If they don't own this capital
            if (capital.Item1.GetOwner() != target)
            {
                //Quit early
                return false;
            }
        }

        return true;
    }


    Dictionary<Territory.Continent, List<Territory>> continents = new Dictionary<Territory.Continent, List<Territory>>(); 
    static Map instance;
    public enum AttackResult
    {
        Won,
        Lost,
        Cancelled //Can only be triggered by players
    }

    public static bool IsSimulated()
    {
        return SceneManager.GetActiveScene().buildIndex == 1; //Menu Scene
    }

    public void Start()
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

    public static void RequestAttack(Territory attacker, Territory defender)
    {
        //! We don't currently account for the attacker and defender both being players but on different machines
        //will do that when it is needed

        //Need this later
        //After attack has finished we need to notify the attacker that they have won or lost
        //No need to notify defender (at least the actual game object) with current game structure
        Player attackingPlayer = attacker.GetOwner();
        //Need to pass this to ask them how many troops they want to defend with
        Player defenderPlayer = defender.GetOwner();

        if (attackingPlayer == null || defenderPlayer == null) 
        {
            throw new System.Exception("Player is null!");
        }

        //If the attacker and defender are both ai skip over ui step
        //If not we need to wait (open dice roll menu) and come back to it later
        //Dice roll menu will then run attack when it is needed
        bool attackerIsLocalPlayer = attackingPlayer is LocalPlayer;
        bool defenderIsLocalPlayer = defenderPlayer is LocalPlayer;

        if (attackerIsLocalPlayer && defenderIsLocalPlayer) 
        {
            Debug.LogError("LocalPlayer is attacking itself!");
            Application.Quit();
        }
        else if (attackerIsLocalPlayer || defenderIsLocalPlayer)
        {
            //Activate dice roll menu
            DiceRollMenu.Activate(attacker, defender, attackerIsLocalPlayer);
        }
        else
        {
            //Just straight run attack
            Attack(attacker, defender, attackingPlayer.GetAttackingDice(attacker), defenderPlayer.GetDefendingDice(defender));
        }
    }

    public static void Attack(Territory attacker, Territory defender, int attackingDice, int defendingDice)
    {
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

        if (defender.GetCurrentTroops() <= 0)
        {
            Player oldOwner = defender.GetOwner();
            defender.SetOwner(attacker.GetOwner());
            attacker.GetOwner().AddTerritory(defender);
            oldOwner.RemoveTerritory(defender);
            defender.SetCurrentTroops(attacker.GetCurrentTroops() - 1 >= 3 ? 3 : attacker.GetCurrentTroops() - 1);
            attacker.SetCurrentTroops(attacker.GetCurrentTroops() - defender.GetCurrentTroops());
            UIManagement.AddLineToRollOutput("Territory Taken!");
            if (oldOwner.IsDead())
            {
                attacker.GetOwner().Killed(oldOwner);
            }
        }
        else
        {
            UIManagement.AddLineToRollOutput("Territory Defended!");
        }

        //Send a message to the match manager 
        //So it can check if the game is now over
        MatchManager.WinCheck(attacker.GetOwner());

        //Notify the attacker
        //owner will be null if game was just won
        //This is why the win check is placed above this function call
        if (attacker.GetOwner() != null)
        {
            attacker.GetOwner().OnAttackEnd(attackResult, attacker, defender);
        }

        //Refresh UI
        UIManagement.RefreshRollOutput();
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


    public static void ResetInstanceMap()
    {
        instance.SetupMap();
    }

    public void SetupMap()
    {
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
                territory.ResetTerritory(false);

                if (!continents.ContainsKey(territory.GetContinent()))
                {
                    continents[territory.GetContinent()] = new List<Territory>();
                }
                continents[territory.GetContinent()].Add(territory);
            }
        }
    }

}
