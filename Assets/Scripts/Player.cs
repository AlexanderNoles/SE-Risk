using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField]
    PlayerColour colour;
    const float turnDelay = 0.01f;
    public enum PlayerColour {Red, Blue, Green, Pink, Orange, Purple};
    Dictionary<PlayerColour, Color> playerColorToColor = new Dictionary<PlayerColour, Color>{ { PlayerColour.Red, new Color(135/255f,14 / 255f, 5 / 255f, 1f) }, { PlayerColour.Blue, new Color(1 / 255f, 1 / 255f, 99 / 255f, 1f) }, { PlayerColour.Orange, new Color(171 / 255f, 71 / 255f, 14 / 255f, 1f) }, { PlayerColour.Green, new Color(6 / 255f, 66 / 255f, 14 / 255f, 1f)}, { PlayerColour.Purple, new Color(92 / 255f, 14 / 255f, 171 / 255f, 1f)}, { PlayerColour.Pink, new Color(166 / 255f, 8 / 255f, 140 / 255f, 1f)} };
    protected int troopCount;
    protected List<Territory> territories;
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
        StartCoroutine(nameof(AttackWait));
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
                                yield return new WaitForSecondsRealtime(turnDelay);
                                if(Map.Attack(territory, neighbour, territory.GetCurrentTroops() - 1)) {
                                    neighbour.SetCurrentTroops(territory.GetCurrentTroops() + neighbour.GetCurrentTroops() - 1);
                                    territory.SetCurrentTroops(1);
                                    break; };
                            }
                        }
                    }
                }
            }
        }
        MatchManager.Fortify();
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
