using MonitorBreak.Bebug;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Territory;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class Player : MonoBehaviour
{
    [SerializeField]
    PlayerColour colour;

    public int GetIndex()
    {
        return (int)colour;
    }

    float turnDelay
    {
        get
        {
            if (SceneManager.GetActiveScene().buildIndex == 0) //Main Scene
            {
                return 0.01f;
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

    public virtual void ResetPlayer()
    {
        placingFirstTerritory = true;
        hand = new Hand();
        territoryTakenThisTurn = false;
        hasBeenReset = true;
        inTheMiddleOfAttack = false;
        territories = new List<Territory>();
        turnReset = false;
    }

    public virtual void Setup(List<Territory> territories)
    {
        turnReset = false;
        this.territories = territories;
        StartCoroutine(nameof(SetupWait), troopCount);
    }

    public Territory EvaluateCapitalPlacement()
    {
        ShuffleTerritoryList();
        Territory deployTerritory = territories[Random.Range(0, territories.Count)];
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
    public Territory EvaluateNextTerritory()
    {
        List<Territory> ownedTerritories = Map.GetTerritoriesOwnedByPlayer(GetIndex());
        Territory.Continent continent = Map.GetContinentClosestToCaptured(GetIndex());
        ShuffleTerritoryList();
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
        foreach (Territory territory in territories)
        {
            if (territory.GetContinent() == continent && territory.GetOwner() == -1)
            {
                return territory;
            }
        }
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

    public Territory EvaluateNextTroopPlacement()
    {
        Territory minTroopTerritory = territories[0];
        ShuffleTerritoryList();
        foreach (Territory territory in territories)
        {
                foreach (Territory neighbor in territory.GetNeighbours())
                {
                    if (neighbor.GetOwner() != minTroopTerritory.GetOwner() && territory.GetCurrentTroops()<minTroopTerritory.GetCurrentTroops())
                    {
                        minTroopTerritory= territory;
                    }
                }
        }
        return minTroopTerritory;
    }
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
    public virtual void ClaimCapital(List<Territory> territories)
    {
        this.territories = territories;
        StartCoroutine(nameof(ClaimWait), troopCount);
    }

    private IEnumerator ClaimWait()
    {
        yield return new WaitForSecondsRealtime(turnDelay);
        Territory capital = EvaluateCapitalPlacement();
        capital.SetCurrentTroops(1);
        capital.SetOwner(GetIndex());

        Map.AddCapital(capital, this);

        MatchManager.SwitchPlayerSetup();
    }

    private Territory EvaluateDeploy()
    {
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
    private IEnumerator DeployWait(int troopCount)
    {
            yield return new WaitForSecondsRealtime(turnDelay);
            Territory deployTerriory = EvaluateDeploy();
            if (deployTerriory != null)
            {
                deployTerriory.SetCurrentTroops(troopCount + deployTerriory.GetCurrentTroops());
                MatchManager.Attack();
            }
            else
            {
                MatchManager.WinCheck(this);
            }
    }
    public virtual bool Attack()
    {
        //This means the coroutine is only reset when has been reset is set to true in the middle of it running
        hasBeenReset = false;
        attackCoroutine = StartCoroutine(nameof(AttackWait));
        return true;
    }

    private IEnumerator AttackWait()
    {
            Debug.Log("hi");
            if (doAttack)
            {
                bool routeFailed = false;
                for (int i = 0; i < interruptRoute.Count - 1; i++)
                {
                    if (routeFailed == true)
                    {
                        break;
                    }
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
                foreach (Territory neighbour in enemyNeighbours)
                {
                    //Fix this
                    if (toExpand == null || neighbour == null || this == null || toExpand.GetCurrentTroops() < neighbour.GetCurrentTroops())
                    {
                        break;
                    }
                    while (toExpand.GetCurrentTroops() > 1 && neighbour.GetOwner() != GetIndex())
                    {
                        onePlayerAlive = MatchManager.OnePlayerAlive(this);
                        if (onePlayerAlive || toExpand == null || neighbour == null || this == null)
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
                MatchManager.Fortify();
            }
        }
    private Territory EvaluateExpansion(out bool attempt)
    {
        int currentMinNeighbours = 10000;
        Territory currentTerritoryToExpand = null;
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

        if (currentTerritoryToExpand != null && currentTerritoryToExpand.GetCurrentTroops() > currentMinNeighbours - Random.Range(0, difficulty)*2)
        {
            attempt = true;
        }

        return currentTerritoryToExpand;
    }
    private List<Territory> EvaluateInterruptAttack(out bool attempt)
    {
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

   
    public int GetMaxAttackingDice(Territory target)
    {
        //Must have one more army than the number of dice we want to roll, clamped to range 1...3
        return Mathf.Clamp(target.GetCurrentTroops()-1, 1, 3);
    }

    public virtual int GetAttackingDice(Territory attacker)
    {
        return GetMaxAttackingDice(attacker);
    }

    public int GetMaxDefendingDice(Territory target)
    {
        //Random Range function is max exclusive, so we add 1 to the current troops
        //This is accounted for in the dice ui, if this is changed to not used Random.Range, make sure to update that code as well!
        return Mathf.Clamp(target.GetCurrentTroops() + 1, 2, 3);
    }

    public virtual int GetDefendingDice(Territory defender)
    {
        //Return any value between 1 and 2 inclusive, if we have more than 1 troop
        if (defender.GetCurrentTroops() > 1)
        {
            return Random.Range(1, GetMaxDefendingDice(defender));
        }

        return 1;
    }

    public virtual void OnAttackEnd(Map.AttackResult attackResult, Territory attacker, Territory defender, int attackerDiceCount)
    {
        if (attackResult == Map.AttackResult.Won)
        {
            if (expanding == 0)
            {
                defender.SetCurrentTroops(attacker.GetCurrentTroops() - 1);
                attacker.SetCurrentTroops(1);
            }
            else
            {
                int troopsToMove = attacker.GetCurrentTroops() / expanding ;
                Debug.Log(troopsToMove);
                Debug.Log(attacker.GetCurrentTroops());
                Debug.Log(attackerDiceCount);
                defender.SetCurrentTroops(troopsToMove > attackerDiceCount ? troopsToMove : attackerDiceCount);
                attacker.SetCurrentTroops(attacker.GetCurrentTroops() - defender.GetCurrentTroops());
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

    private Territory EvaluateFortify()
    {
        Territory maxLockedTerritory = null;
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
            if(locked && (maxLockedTerritory == null||maxLockedTerritory.GetCurrentTroops() < territory.GetCurrentTroops()) && AreTerritoriesConnected(territory,toExpand))
            {
                maxLockedTerritory = territory;
            }
        }
        return maxLockedTerritory;
    }
    public virtual void Fortify()
    {
        MatchManager.WinCheck(this);
        if (!MatchManager.OnePlayerAlive(this))
        {
            StartCoroutine(nameof(FortifyWait));
        }
    }
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
        MatchManager.EndTurn();
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
    public bool AreTerritoriesConnected(Territory startTerritory, Territory endTerritory)
    {
        FriendlyNode startNode = new FriendlyNode().SetTerritory(startTerritory);
        FriendlyNode endNode = new FriendlyNode().SetTerritory(endTerritory);
        return Pathfinding.AStar.FindPath(startNode, endNode, false).Count > 0;
    }
    public List<Pathfinding.INode> RouteBetweenTerritories(Territory startTerritory, Territory endTerritory)
    {
        EnemyNode startNode = new EnemyNode().SetTerritory(startTerritory).SetOwner(GetIndex());
        EnemyNode endNode = new EnemyNode().SetTerritory(endTerritory).SetOwner(endTerritory.GetOwner());
        return Pathfinding.AStar.FindPath(startNode, endNode, false);
    }
    public virtual void OnTurnEnd()
    {
        if (territoryTakenThisTurn && hand.Count() < 6)
        {
            hand.AddCard(Deck.Draw());
        }
    }

    public bool IsDead()
    {
        return !MatchManager.InSetup() && territories.Count <= 0;
    }

    public void Killed(Player killed)
    {
        Hand killedHand = killed.GetHand();
        for (int i = 0; i < killedHand.Count(); i++)
        {
            hand.AddCard(killedHand.GetCard(i));
        }

        PlayerInfoHandler.UpdateInfo();
        KilledAPlayerThisTurn = true;
        //if (MatchManager.OnePlayerAlive(this))
        //{
        //    doAttackExpansion = false;
        //}

    }

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
}
