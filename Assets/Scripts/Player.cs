using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class Player : MonoBehaviour
{
    [SerializeField]
    PlayerColour colour;
    const float turnDelay = 0.1f;
    private bool inTheMiddleOfAttack;
    private Coroutine attackCoroutine = null;
    public enum PlayerColour {Red, Blue, Green, Pink, Orange, Purple};
    Dictionary<PlayerColour, Color> playerColorToColor = new Dictionary<PlayerColour, Color>{ { PlayerColour.Red, new Color(135/255f,14 / 255f, 5 / 255f, 1f) }, { PlayerColour.Blue, new Color(1 / 255f, 1 / 255f, 99 / 255f, 1f) }, { PlayerColour.Orange, new Color(171 / 255f, 71 / 255f, 14 / 255f, 1f) }, { PlayerColour.Green, new Color(6 / 255f, 66 / 255f, 14 / 255f, 1f)}, { PlayerColour.Purple, new Color(92 / 255f, 14 / 255f, 171 / 255f, 1f)}, { PlayerColour.Pink, new Color(166 / 255f, 8 / 255f, 140 / 255f, 1f)} };
    protected int troopCount;
    protected List<Territory> territories;
    public virtual void Setup(List<Territory> territories)
    {
        this.territories = territories;
        StartCoroutine(nameof(SetupWait), troopCount);
    }
    private IEnumerator SetupWait()
    {
        yield return new WaitForSecondsRealtime(turnDelay);
        Territory deployTerriory = territories[Random.Range(0, territories.Count)];
        deployTerriory.SetCurrentTroops(1 + deployTerriory.GetCurrentTroops());
        deployTerriory.SetOwner(this);
        MatchManager.SwitchPlayerSetup();
    }
    public virtual bool Deploy(List<Territory> territories, int troopCount) 
    {
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
                                if (!inTheMiddleOfAttack)
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

        MatchManager.Fortify();
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
        if(defender.GetCurrentTroops() > 1)
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
                    if (endNode!= territory && AreTerritoriesConnected(territory, endNode) && Random.Range(0, 2) == 0)
                    {
                        endNode.SetCurrentTroops(territory.GetCurrentTroops() + endNode.GetCurrentTroops() - 1);
                        territory.SetCurrentTroops(1);
                        territoryFound = true;
                        break;
                    }
                }
            }
        }
        MatchManager.EndTurn();
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
    public List<Territory> GetTerritories() {  return territories; }
    public void AddTerritory(Territory territory)
    {
        territories.Add(territory);
    }
    public bool AreTerritoriesConnected(Territory startTerritory, Territory endTerritory)
    {
        TerritoryNode startNode = new TerritoryNode().SetTerritory(startTerritory);
        TerritoryNode endNode = new TerritoryNode().SetTerritory(endTerritory);
        return Pathfinding.AStar.FindPath(startNode, endNode,false).Count>0;
    }
}
