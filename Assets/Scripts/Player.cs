using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Territory;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class Player : MonoBehaviour
{
    [SerializeField]
    PlayerColour colour;
    float turnDelay
    {
        get
        {
            if (SceneManager.GetActiveScene().buildIndex == 0) //Main Scene
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
    Dictionary<PlayerColour, Color> playerColorToColor = new Dictionary<PlayerColour, Color> { { PlayerColour.Red, new Color(135 / 255f, 14 / 255f, 5 / 255f, 1f) }, { PlayerColour.Blue, new Color(1 / 255f, 1 / 255f, 99 / 255f, 1f) }, { PlayerColour.Orange, new Color(171 / 255f, 71 / 255f, 14 / 255f, 1f) }, { PlayerColour.Green, new Color(6 / 255f, 66 / 255f, 14 / 255f, 1f) }, { PlayerColour.Purple, new Color(92 / 255f, 14 / 255f, 171 / 255f, 1f) }, { PlayerColour.Pink, new Color(166 / 255f, 8 / 255f, 140 / 255f, 1f) } };
    protected int troopCount;
    protected List<Territory> territories;
    protected Hand hand;
    protected bool territoryTakenThisTurn;
    private bool hasBeenReset;
    protected bool turnReset;
    public bool KilledAPlayerThisTurn = false;
    private bool placingFirstTerritory = true;
    private int difficulty = 1;
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
            if(territory.GetOwner() == null)
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
        List<Territory> ownedTerritories = Map.GetTerritoriesOwnedByPlayer(this);
        Territory.Continent continent = Map.GetContinentClosestToCaptured(this);
        ShuffleTerritoryList();
        foreach (Territory territory in ownedTerritories)
        {
                foreach (Territory neighbor in territory.GetNeighbours())
                {
                    if (neighbor.GetContinent() == continent && neighbor.GetOwner() == null)
                    {
                        return neighbor;
                    }
                }
        }
        foreach (Territory territory in territories)
        {
            if (territory.GetContinent() == continent && territory.GetOwner() == null)
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
                    int routeLength = LengthOfRouteBetweenTerritories(owned, territory);
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
        deployTerritory.SetOwner(this);
        MatchManager.SwitchPlayerSetup();
    }

    private bool AllTerritoriesClaimed()
    {
        bool allTerritoriesClaimed= true;
        foreach(Territory territory in territories)
        {
            if (territory.GetOwner() == null)
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
        capital.SetOwner(this);

        Map.AddCapital(capital, this);

        MatchManager.SwitchPlayerSetup();
    }

    public virtual bool Deploy(List<Territory> territories, int troopCount)
    {
        KilledAPlayerThisTurn = false;
        //Card check
        do
        {
            if (hand.FindValidSet(out List<Card> validSet, true))
            {
                if (!Map.IsSimulated())
                {
                    troopCount += Hand.NumberOfTroopsForSet(this, validSet);
                }

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
        while (troopCount > 0)
        {
            yield return new WaitForSecondsRealtime(turnDelay);
            int deployCount = Random.Range(1, troopCount + 1);
            Territory deployTerriory = territories[Random.Range(0, territories.Count)];
            deployTerriory.SetCurrentTroops(deployCount + deployTerriory.GetCurrentTroops());
            troopCount -= deployCount;
        }
        MatchManager.Attack();
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
        for (int i = 0; i < territories.Count; i++)
        {
            Territory territory = territories[i];
            if (territory.GetCurrentTroops() > 1)
            {
                foreach (Territory neighbour in territory.GetNeighbours())
                {
                    if (neighbour.GetOwner() != territory.GetOwner())
                    {
                        if (Random.Range(0, 2) == 0)
                        {
                            while (territory.GetCurrentTroops() > 1)
                            {
                                if (hasBeenReset)
                                {
                                    yield break;
                                }
                                else if (!inTheMiddleOfAttack)
                                {
                                    yield return new WaitForSecondsRealtime(turnDelay);
                                    inTheMiddleOfAttack = true;
                                    Map.RequestAttack(territory, neighbour);
                                }
                                else
                                {
                                    yield return new WaitForEndOfFrame();
                                }
                            }
                        }
                    }
                }
            }
        }

        if (!hasBeenReset)
        {
            MatchManager.Fortify();
        }
    }

    public int GetMaxAttackingDice(Territory target)
    {
        //Must have one more army than the number of dice we want to roll, clamped to range 1...3
        return Mathf.Clamp(target.GetCurrentTroops(), 2, 4);
    }

    public virtual int GetAttackingDice(Territory attacker)
    {
        return Random.Range(1, GetMaxAttackingDice(attacker));
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

    public virtual void OnAttackEnd(Map.AttackResult attackResult, Territory attacker, Territory defender)
    {
        if (attackResult == Map.AttackResult.Won)
        {
            //Temp measure, just move all troops
            defender.SetCurrentTroops(attacker.GetCurrentTroops() + defender.GetCurrentTroops() - 1);
            attacker.SetCurrentTroops(1);
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


    public virtual void Fortify()
    {
        StartCoroutine(nameof(FortifyWait));
    }
    private IEnumerator FortifyWait()
    {
        yield return new WaitForSecondsRealtime(turnDelay);
        bool territoryFound = false;
        for (int i = 0; i < territories.Count && !territoryFound; i++)
        {
            Territory territory = territories[i];
            if (territory.GetCurrentTroops() > 1 && Random.Range(0, 2) == 0)
            {
                foreach (Territory endNode in territories)
                {
                    if (endNode != territory && AreTerritoriesConnected(territory, endNode) && Random.Range(0, 2) == 0)
                    {
                        endNode.SetCurrentTroops(territory.GetCurrentTroops() + endNode.GetCurrentTroops() - 1);
                        territory.SetCurrentTroops(1);
                        territoryFound = true;
                        break;
                    }
                }
            }
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
        TerritoryNode startNode = new TerritoryNode().SetTerritory(startTerritory);
        TerritoryNode endNode = new TerritoryNode().SetTerritory(endTerritory);
        return Pathfinding.AStar.FindPath(startNode, endNode, false).Count > 0;
    }
    public int LengthOfRouteBetweenTerritories(Territory startTerritory, Territory endTerritory)
    {
        TerritoryNode startNode = new TerritoryNode().SetTerritory(startTerritory);
        TerritoryNode endNode = new TerritoryNode().SetTerritory(endTerritory);
        return Pathfinding.AStar.FindPath(startNode, endNode, false).Count;
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
    }

    public void ContinueTurn()
    {
        turnReset = true;
        Deploy(territories, 0);
    }

    public bool GetTurnReset()
    {
        return turnReset;
    }
}
