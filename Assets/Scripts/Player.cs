using MonitorBreak.Bebug;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Territory;

/// <summary>
/// The base implementation of a player. This contains all the methods needed for an AI player, in case a human player is ever unable to make a move on their turn
/// </summary>
public class Player : MonoBehaviour
{
    [SerializeField]
    PlayerColour colour;
    /// <summary>
    /// Returns the players colour as an integer value. As this is a unique in each game, it can be used as an identifer
    /// </summary>
    /// <returns></returns>
    public int GetIndex()
    {
        return (int)colour;
    }

    float turnDelay
    {
        get
        {
            if (SceneManager.GetActiveScene().buildIndex == 1) //Main Scene
            {
                return 0.1f;
            }
            else
            {
                return 0.001f;
            }
        }
    }
    private bool inTheMiddleOfAttack;
    private Coroutine attackCoroutine = null;
    public enum PlayerColour { Red, Blue, Green, Pink, Orange, Purple };

    public static Color GetColourBasedOnIndex(int index)
    {
        return playerColorToColor[(PlayerColour)index];
    }

    static Dictionary<PlayerColour, Color> playerColorToColor = new Dictionary<PlayerColour, Color> { { PlayerColour.Red, new Color(135 / 255f, 14 / 255f, 5 / 255f, 1f) }, { PlayerColour.Blue, new Color(1 / 255f, 1 / 255f, 99 / 255f, 1f) }, { PlayerColour.Orange, new Color(171 / 255f, 71 / 255f, 14 / 255f, 1f) }, { PlayerColour.Green, new Color(6 / 255f, 66 / 255f, 14 / 255f, 1f) }, { PlayerColour.Purple, new Color(92 / 255f, 14 / 255f, 171 / 255f, 1f) }, { PlayerColour.Pink, new Color(166 / 255f, 8 / 255f, 140 / 255f, 1f) } };
    protected int troopCount;
    protected List<Territory> territories;
    protected Hand hand;
    protected bool territoryTakenThisTurn;
    private bool hasBeenReset;
    protected bool turnReset;
    public bool KilledAPlayerThisTurn = false;
    private bool placingFirstTerritory = true;
    private int difficulty = 1;
    private int expanding = 0;
    List<Territory> interruptRoute;
    Territory toExpand;
    bool doAttack;
    bool doAttackExpansion;
    /// <summary>
    /// Resets a player to the state it will be in at the start of a game
    /// </summary>
    public virtual void ResetPlayer()
    {
        placingFirstTerritory = true;
        hand = new Hand(GetIndex());
        territoryTakenThisTurn = false;
        hasBeenReset = true;
        inTheMiddleOfAttack = false;
        territories = new List<Territory>();
        turnReset = false;
    }
    /// <summary>
    /// Starts the setup phase of this players turn
    /// </summary>
    /// <param name="territories">The territories that the player can place their troop onto</param>
    public virtual void Setup(List<Territory> territories)
    {
        turnReset = false;
        this.territories = territories;
        StartCoroutine(nameof(SetupWait), troopCount);
    }

    /// <summary>
    /// Evaluates where to place the players capital, or first troop
    /// </summary>
    /// <returns>The territory the player should place its capital on</returns>
    public Territory EvaluateCapitalPlacement()
    {
        ShuffleTerritoryList();
        Territory deployTerritory = territories[Random.Range(0, territories.Count)];
        //Finds the territory left with the fewest neighbouring territories as the easiest territory to defend
        foreach (Territory territory in territories)
        {
            if(territory.GetOwner() == -1)
            {
                if (deployTerritory.GetNeighbours().Count-Random.Range(0,difficulty) > territory.GetNeighbours().Count)
                {
                    deployTerritory = territory;
                }
            }
        }
        return deployTerritory;
    }

    /// <summary>
    /// Randomises the order that territories appear in the territory list. This means that in cases where there are multiple equally good moves, the AI will not always pick the best one
    /// </summary>
    public void ShuffleTerritoryList()
    {
        List<Territory> shuffledTerritories = new List<Territory>();
        for (int i = 0; i < territories.Count; i++)
        {
            int rndIndex = Random.Range(0, territories.Count);
            shuffledTerritories.Add(territories[rndIndex]);
            territories.Remove(territories[rndIndex]);
        }
        territories = shuffledTerritories;
    }
    /// <summary>
    /// Evaluates the next territory for the AI player to place its troop on in the setup phase, when there are still unclaimed territories
    /// </summary>
    /// <returns></returns>
    public Territory EvaluateNextTerritory()
    {
        List<Territory> ownedTerritories = Map.GetTerritoriesOwnedByPlayer(GetIndex());
        Territory.Continent continent = Map.GetContinentClosestToCaptured(GetIndex());
        ShuffleTerritoryList();
        //Finds territories neighbouring ones we own on the same continent, in an attempt to capture the continent
        foreach (Territory territory in ownedTerritories)
        {
                foreach (Territory neighbor in territory.GetNeighbours())
                {
                    if (neighbor.GetContinent() == continent && neighbor.GetOwner() == -1)
                    {
                        return neighbor;
                    }
                }
        }
        //If we cant take a territory on the same continent, try to find the closest territory to our border
        int minLengthBetweenTerritories = 1000;
        Territory nextTerritory = territories[0];
        foreach (Territory territory in territories)
        {
                foreach (Territory owned in ownedTerritories)
                {
                    int routeLength = RouteBetweenTerritories(owned, territory).Count;
                    if (routeLength < minLengthBetweenTerritories)
                    {
                        minLengthBetweenTerritories = routeLength;
                        nextTerritory = territory;
                    }
                    return territory;
                }
        }
        return nextTerritory;
    }
    /// <summary>
    /// This function evaluates where the player should play their setup troop, once all territories have already been claimed
    /// </summary>
    /// <returns></returns>
    public Territory EvaluateNextTroopPlacement()
    {
        Territory minTroopTerritory = territories[0];
        ShuffleTerritoryList();
        //Looks for our "border" (territories neighbouring enemies) and places troops evenly along it
        foreach (Territory territory in territories)
        {
                foreach (Territory neighbor in territory.GetNeighbours())
                {
                if (neighbor.GetOwner() != minTroopTerritory.GetOwner() && territory.GetCurrentTroops() < minTroopTerritory.GetCurrentTroops() + Random.Range(0, difficulty));
                    {
                        minTroopTerritory= territory;
                    }
                }
        }
        return minTroopTerritory;
    }
    /// <summary>
    /// The coroutine that plays the setup phase of the turn for an AI player
    /// </summary>
    /// <returns></returns>
    private IEnumerator SetupWait()
    {
        yield return new WaitForSecondsRealtime(turnDelay);
        Territory deployTerritory;
        if (placingFirstTerritory)
        {
            deployTerritory = EvaluateCapitalPlacement();
            placingFirstTerritory = false;
        }
        else if (!AllTerritoriesClaimed())
        {
            deployTerritory = EvaluateNextTerritory();
        }
        else
        {
            deployTerritory = EvaluateNextTroopPlacement();
        }
        deployTerritory.SetCurrentTroops(1 + deployTerritory.GetCurrentTroops());
        deployTerritory.SetOwner(GetIndex());
        MatchManager.SwitchPlayerSetup();
    }

    /// <summary>
    /// Checks to see if every territory has already been claimed
    /// </summary>
    /// <returns>True if all territories have an owner, else false</returns>
    private bool AllTerritoriesClaimed()
    {
        bool allTerritoriesClaimed= true;
        foreach(Territory territory in territories)
        {
            if (territory.GetOwner() == -1)
            {
                allTerritoriesClaimed = false;
                break;
            }
        }
        return allTerritoriesClaimed;
    }

    /// <summary>
    /// Starts the setup phase of the turn for the first troop to be deployed (the capital)
    /// </summary>
    /// <param name="territories"></param>
    public virtual void ClaimCapital(List<Territory> territories)
    {
        this.territories = territories;
        StartCoroutine(nameof(ClaimWait), troopCount);
    }

    /// <summary>
    /// The coroutine that executes the setup phase of an AI players turn for the capital deployment
    /// </summary>
    /// <returns></returns>
    private IEnumerator ClaimWait()
    {
        yield return new WaitForSecondsRealtime(turnDelay);
        Territory capital = EvaluateCapitalPlacement();
        capital.SetCurrentTroops(1);
        capital.SetOwner(GetIndex());

        Map.AddCapital(capital.GetIndexInMap(), GetIndex());

        MatchManager.SwitchPlayerSetup();
    }

    /// <summary>
    /// Evaluates the best territory for the AI player to deploy onto
    /// </summary>
    /// <returns>The best territory to deploy onto</returns>
    private Territory EvaluateDeploy()
    {
        //Precomputes what territory we will attack from, and places all our troops on that territory
        toExpand = EvaluateExpansion(out doAttackExpansion);
        interruptRoute = EvaluateInterruptAttack(out doAttack);
        if (doAttack)
        {
            return interruptRoute[0];
        }
        else if (toExpand!=null)
        {
            return toExpand;
        }
        else
        {
            return null;
        }
    }
    /// <summary>
    /// Starts the deploy phase for an AI player. This includes turning in cards, if it can
    /// </summary>
    /// <param name="territories"></param>
    /// <param name="troopCount"></param>
    /// <returns></returns>
    public virtual bool Deploy(List<Territory> territories, int troopCount)
    {
        KilledAPlayerThisTurn = false;
        //Card check
        do
        {
            if (hand.FindValidSet(out List<Card> validSet, true))
            {
                    troopCount += Hand.NumberOfTroopsForSet(GetIndex(), validSet);
                Hand.IncrementTurnInCount();
            }
        } while (hand.Count() >= 5);
        //Normal process
        territoryTakenThisTurn = false;
        this.territories = territories;
        StartCoroutine(nameof(DeployWait), troopCount);
        return true;
    }
    /// <summary>
    /// The coroutine that simulates the deploy phase of the AI players turn
    /// </summary>
    /// <param name="troopCount"></param>
    /// <returns></returns>
    private IEnumerator DeployWait(int troopCount)
    {
            yield return new WaitForSecondsRealtime(turnDelay);
            Territory deployTerriory = EvaluateDeploy();
            if (deployTerriory != null)
            {
                deployTerriory.SetCurrentTroops(troopCount + deployTerriory.GetCurrentTroops());
                MatchManager.Attack(GetIndex());
            }
            else
            {
                MatchManager.WinCheck(GetIndex());
            }
    }
    /// <summary>
    /// Starts the attack phase of the AI players turn
    /// </summary>
    public virtual void Attack()
    {
        //This means the coroutine is only reset when has been reset is set to true in the middle of it running
        hasBeenReset = false;
        attackCoroutine = StartCoroutine(nameof(AttackWait));
    }

    /// <summary>
    /// Simulates the attack phase on an AI players turn
    /// </summary>
    /// <returns></returns>
    private IEnumerator AttackWait()
    {
        //if we have the troops to attempt the attack route
            if (doAttack)
            {
                bool routeFailed = false;
                for (int i = 0; i < interruptRoute.Count - 1; i++)
                {
                    if (routeFailed == true)
                    {
                        break;
                    }
                    //attempt to attack along the plotted route
                    while (interruptRoute[i + 1].GetOwner() != GetIndex())
                    {
                        if (interruptRoute[i].GetCurrentTroops() <= 1)
                        {
                            routeFailed = true;
                            break;
                        }
                        if (hasBeenReset)
                        {
                            yield break;
                        }
                        else if (!inTheMiddleOfAttack)
                        {
                            yield return new WaitForSecondsRealtime(turnDelay);
                            inTheMiddleOfAttack = true;
                            Map.RequestAttack(interruptRoute[i], interruptRoute[i + 1]);
                        }
                        else
                        {
                            yield return new WaitForEndOfFrame();
                        }
                    }
                }
            }
            bool onePlayerAlive = false;
            //if we have a territory to expand our borders from (increase the number of territories that do not border enemy territories)
            if (toExpand != null && doAttackExpansion && !onePlayerAlive)
            {
                List<Territory> enemyNeighbours = new List<Territory>();
                foreach (Territory neighbour in toExpand.GetNeighbours())
                {
                    if (neighbour.GetOwner() != GetIndex())
                    {
                        enemyNeighbours.Add(neighbour);
                    }
                }
                expanding = enemyNeighbours.Count + 1;
            //try to take all neighbours
                foreach (Territory neighbour in enemyNeighbours)
                {
                    if (toExpand.GetCurrentTroops() < neighbour.GetCurrentTroops())
                    {
                        break;
                    }
                    while (toExpand.GetCurrentTroops() > 1 && neighbour.GetOwner() != GetIndex())
                    {
                        onePlayerAlive = MatchManager.OnePlayerAlive(this);
                        if (onePlayerAlive)
                        {
                            break;
                        }

                        if (!inTheMiddleOfAttack && !MatchManager.IsGameOver() && !hasBeenReset)
                        {
                            inTheMiddleOfAttack = true;
                            Map.RequestAttack(toExpand, neighbour);
                            yield return new WaitForSecondsRealtime(turnDelay);
                        }
                        else
                        {
                            yield return new WaitForEndOfFrame();
                        }
                    }
                }
                toExpand = EvaluateExpansion(out doAttackExpansion);
                expanding = 0;
            }
            if (!hasBeenReset)
            {
                MatchManager.Fortify(GetIndex());
            }
        }
    /// <summary>
    /// Evaluates the territory that the player can most easily expand its borders from 
    /// </summary>
    /// <param name="attempt">Whether or not the player has enough troops to expand from this territory</param>
    /// <returns>The territory that the player can most easily exapand its borders from</returns>
    private Territory EvaluateExpansion(out bool attempt)
    {
        int currentMinNeighbours = 10000;
        Territory currentTerritoryToExpand = null;
        //Finds the territory with the fewest troops neighbouring it, not necessarily the fewest enemy territories
        foreach (Territory territory in territories)
        {
            int enemyNeighbourCount = 0;
            List<Territory> enemyNeighbours = new List<Territory>();
            foreach(Territory neighbour in territory.GetNeighbours())
            {
                if (neighbour.GetOwner()!= territory.GetOwner())
                {
                    enemyNeighbourCount+=neighbour.GetCurrentTroops();
                }
            }
            if (enemyNeighbourCount < currentMinNeighbours && enemyNeighbourCount!=0)
            {
                currentTerritoryToExpand = territory;
                currentMinNeighbours=enemyNeighbourCount;
            }
        }

        attempt = false;
        //decides if we attempt the attack
        if (currentTerritoryToExpand != null && currentTerritoryToExpand.GetCurrentTroops() > currentMinNeighbours - Random.Range(0, difficulty)*2)
        {
            attempt = true;
        }

        return currentTerritoryToExpand;
    }
    /// <summary>
    /// Evalutes if a player is control of a continent, and the best route to break control of that continent
    /// </summary>
    /// <param name="attempt">Whether or not the player has the troops required to attempt this route</param>
    /// <returns>The list of territories to take to break a players control over a contienent</returns>
    private List<Territory> EvaluateInterruptAttack(out bool attempt)
    {
        //finds the continents owned completely by an enemy
        List<Continent> continentsOwnedByEnemies = new List<Continent>();
        foreach(Continent continent in System.Enum.GetValues(typeof(Continent)))
        {
            int owner = Map.GetContinentOwner(continent);
            if (owner != -1 && owner != GetIndex())
            {
                continentsOwnedByEnemies.Add(continent);
            }
        }
        int minTroopRequirement=10000;
        List<Territory> bestRoute = new List<Territory>();
        //Finds the shortest route to break enemy control of each continent
        foreach(Continent continent in continentsOwnedByEnemies)
        {
            foreach(Territory endTerritory in Map.continents[continent])
            {
                foreach(Territory startTerritory in territories)
                {
                    int routeCost =0;
                    List<Territory> thisRoute = new List<Territory>();
                    List<Pathfinding.INode> newRoute = RouteBetweenTerritories(startTerritory, endTerritory);
                    foreach (EnemyNode node in newRoute)
                    {
                        thisRoute.Add(node.territory);
                        if (node.territory.GetOwner() != GetIndex())
                        {
                            routeCost += (node.territory.GetCurrentTroops()*2)+1;
                        }
                    }
                    if (newRoute.Count>0 && routeCost < minTroopRequirement - Random.Range(0,difficulty))
                    {
                        minTroopRequirement = routeCost;
                        bestRoute = thisRoute;
                    }
                }
            }
        }
        //determines if we attempt the attack
        if (bestRoute.Count>0 && bestRoute[0].GetCurrentTroops() > minTroopRequirement - Random.Range(0, difficulty))
        {
            attempt = true;
        }
        else
        {
            attempt = false;
        }
        return bestRoute;   
    }

   /// <summary>
   /// Returns the max amount of dice a territory is able to attack with, based off their troop count
   /// </summary>
   /// <param name="target">The territory we want to get the max attacking dice for</param>
   /// <returns>The maximum number of dice the passed territory can attack with </returns>
    public int GetMaxAttackingDice(Territory target)
    {
        return Mathf.Clamp(target.GetCurrentTroops()-1, 1, 3);
    }
    /// <summary>
    /// Returns the amount of dice the AI player will attack with
    /// </summary>
    /// <param name="target">The territory we want to get the attacking dice for</param>
    /// <returns>The number of dice the passed territory will attack with </returns>
    public virtual int GetAttackingDice(Territory attacker)
    {
        return GetMaxAttackingDice(attacker); //As it is optimal, the AI will always attack with the maximum number of dice
    }

    /// <summary>
    /// Returns the max amount of dice a territory is able to defend with, based off their troop count
    /// </summary>
    /// <param name="target">The territory we want to get the max defending dice for</param>
    /// <returns>The maximum number of dice the passed territory can defend with </returns>
    public static int GetMaxDefendingDice(Territory target)
    {
        return Mathf.Clamp(target.GetCurrentTroops(),1, 2);
    }

    /// <summary>
    ///  Runs when an attack finishes to move troops if the territory was taken, and gain the defenders cards if you eliminated them
    /// </summary>
    /// <param name="attackResult">The result of the attack (whether or not the defender was taken)</param>
    /// <param name="attacker">The territory attacking the defender</param>
    /// <param name="defender">The territory defending itself from the attacker</param>
    /// <param name="attackerDiceCount">The number of dice the attacker was attacking with</param>
    public virtual void OnAttackEnd(Map.AttackResult attackResult, Territory attacker, Territory defender, int attackerDiceCount)
    {
        if (attackResult == Map.AttackResult.Won)
        {
            //if we're expanding our borders, place an even amount on each territory
            if (expanding == 0)
            {
                int troopsToMove = attacker.GetCurrentTroops() - 1;
                defender.SetCurrentTroops(troopsToMove > attackerDiceCount ? troopsToMove : attackerDiceCount);
                attacker.SetCurrentTroops(1);
            }
            //else dump all troops on taken territory
            else
            {
                int troopsToMove = attacker.GetCurrentTroops() / expanding ;
                defender.SetCurrentTroops(troopsToMove > attackerDiceCount ? troopsToMove : attackerDiceCount);
                attacker.SetCurrentTroops(attacker.GetCurrentTroops() - defender.GetCurrentTroops());
            }
            if (attacker.GetCurrentTroops() < 1)
            {
                attacker.SetCurrentTroops(1);
                defender.SetCurrentTroops(defender.GetCurrentTroops() - 1);
            }
            territoryTakenThisTurn = true;
        }
        if (hand.Count() >= 6 && KilledAPlayerThisTurn)
        {
            ContinueTurn();
        }

        //Allow attacking again
        //Eithier on this turn or the next
        inTheMiddleOfAttack = false;
    }
    /// <summary>
    /// Evaluates the territory that is the most beneficial to take troops away from in the fortify phase
    /// </summary>
    /// <returns></returns>
    private Territory EvaluateFortify()
    {
        Territory maxLockedTerritory = null;
        //find the territory with no enemy neighbours with the most troops on it
        foreach (Territory territory in territories)
        {
            bool locked = true;
            foreach (Territory neighbour in territory.GetNeighbours())
            {
                if (neighbour.GetOwner() != territory.GetOwner())
                {
                    locked = false;
                    break;
                }
            }
            if(locked && (maxLockedTerritory == null||maxLockedTerritory.GetCurrentTroops() < territory.GetCurrentTroops() - Random.Range(0, difficulty)) && AreTerritoriesConnected(territory,toExpand))
            {
                maxLockedTerritory = territory;
            }
        }
        return maxLockedTerritory;
    }
    /// <summary>
    /// Starts the fortifying phase of the AI players turn
    /// </summary>
    public virtual void Fortify()
    {
        MatchManager.WinCheck(GetIndex());
        if (!MatchManager.OnePlayerAlive(this))
        {
            StartCoroutine(nameof(FortifyWait));
        }
    }
    /// <summary>
    /// The coroutine that simulates the fortify phase of the AI players turn
    /// </summary>
    /// <returns></returns>
    private IEnumerator FortifyWait()
    {
        yield return new WaitForSecondsRealtime(turnDelay);
            Territory territory = EvaluateFortify();
            if (territory != null && territory.GetCurrentTroops() > 1)
            {
                toExpand.SetCurrentTroops(territory.GetCurrentTroops() + toExpand.GetCurrentTroops() - 1);
                territory.SetCurrentTroops(1);
            }
        turnReset = false;
        MatchManager.EndTurn(GetIndex());
    }
    public void SetColor(PlayerColour colour)
    {
        this.colour = colour;
    }
    public Color GetColor()
    {
        return playerColorToColor[colour];
    }
    public string GetColorName()
    {
        return colour.ToString();
    }
    public PlayerColour GetPlayerColour() { return colour; }
    public List<Territory> GetTerritories() { return territories; }
    public void AddTerritory(Territory territory)
    {
        territories.Add(territory);
    }

    public void RemoveTerritory(Territory territory)
    {
        territories.Remove(territory);
    }

    public Hand GetHand()
    {
        return hand;
    }
    /// <summary>
    /// Determines if two territories are connected via a route of allied territories
    /// </summary>
    /// <param name="startTerritory">The territory we are looking for a path from</param>
    /// <param name="endTerritory">The territory we are looking for a path to</param>
    /// <returns>True if the two territories are connected by a path of allied territories, else false</returns>
    public bool AreTerritoriesConnected(Territory startTerritory, Territory endTerritory)
    {
        FriendlyNode startNode = new FriendlyNode().SetTerritory(startTerritory);
        FriendlyNode endNode = new FriendlyNode().SetTerritory(endTerritory);
        return Pathfinding.AStar.FindPath(startNode, endNode, false).Count > 0;
    }
    /// <summary>
    /// Finds a route between a two territories, only through enemy territories
    /// </summary>
    /// <param name="startTerritory">The friendly territory we are routing from</param>
    /// <param name="endTerritory">The enemy territory we are routing to</param>
    /// <returns>A list of nodes representing the route between these two territories. The list will be empty if no route exists</returns>
    public List<Pathfinding.INode> RouteBetweenTerritories(Territory startTerritory, Territory endTerritory)
    {
        EnemyNode startNode = new EnemyNode().SetTerritory(startTerritory).SetOwner(GetIndex());
        EnemyNode endNode = new EnemyNode().SetTerritory(endTerritory).SetOwner(endTerritory.GetOwner());
        return Pathfinding.AStar.FindPath(startNode, endNode, false);
    }

    /// <summary>
    /// Draws a card if the player has taken a territory this turn
    /// </summary>
    public virtual void OnTurnEnd()
    {
        if (territoryTakenThisTurn && hand.Count() < 6)
        {
            hand.AddCard(Deck.Draw(GetIndex()));
        }
    }
    /// <summary>
    /// Checks to see if the player is eliminated from the game
    /// </summary>
    /// <returns>True if the player has no territories, else returns false</returns>
    public bool IsDead()
    {
        return !MatchManager.InSetup() && territories.Count <= 0;
    }

    /// <summary>
    /// Takes a players hand
    /// </summary>
    /// <param name="killed">The player whos hand you are taking</param>
    public void Killed(int numberOfCardsTaken)
    {
        //This is not really in line with rules
        //means we just get the same amount of cards they had (essentially drawing as many cards as they had)
        for (int i = 0; i < numberOfCardsTaken; i++)
        {
            hand.AddCard(Deck.Draw(GetIndex()));
        }

        KilledAPlayerThisTurn = true;
    }

    /// <summary>
    /// Runs when the player turns in cards during the attack phase. Allows that player to deploy those troops
    /// </summary>
    public void ContinueTurn()
    {
        //doesnt work for Ai plauyers
        turnReset = true;
        Deploy(territories, 0);
    }

    public bool GetTurnReset()
    {
        return turnReset;
    }

    public virtual void OnKilled()
    {
        //Return all cards in hand
        hand.RemoveAll();
    }
}
