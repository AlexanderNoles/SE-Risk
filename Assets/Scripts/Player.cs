using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField]
    PlayerColour colour;
    public enum PlayerColour {Red, Blue, Green, Pink, Orange, Purple};
    Dictionary<PlayerColour, Color> playerColorToColor = new Dictionary<PlayerColour, Color>{ { PlayerColour.Red, new Color(135/255f,14 / 255f, 5 / 255f, 1f) }, { PlayerColour.Blue, new Color(1 / 255f, 1 / 255f, 99 / 255f, 1f) }, { PlayerColour.Orange, new Color(171 / 255f, 71 / 255f, 14 / 255f, 1f) }, { PlayerColour.Green, new Color(6 / 255f, 66 / 255f, 14 / 255f, 1f)}, { PlayerColour.Purple, new Color(92 / 255f, 14 / 255f, 171 / 255f, 1f)}, { PlayerColour.Pink, new Color(166 / 255f, 8 / 255f, 140 / 255f, 1f)} };
    protected int troopCount;
    protected List<Territory> territories;
    public virtual bool Deploy(List<Territory> territories, int troopCount) 
    {
        this.territories = territories;
        while (troopCount > 0)
        {
            int deployCount = Random.Range(1, troopCount + 1);
            Territory deployTerriory = territories[Random.Range(0, territories.Count)];
            deployTerriory.SetCurrentTroops(deployCount + deployTerriory.GetCurrentTroops());
            troopCount -= deployCount;
        }
        MatchManager.Attack();
        return true;
    }
    public virtual bool Attack()
    {
        for(int i = 0; i<territories.Count;i++)
        {
            Territory territory = territories[i];//just like among us!!!!
            if (territory.GetCurrentTroops() > 1)
            {
                foreach (Territory neighbour in territory.GetNeighbours())
                {
                    if (neighbour.GetOwner() != territory.GetOwner())
                    {
                        if (Random.Range(0, 2) == 0)
                        {
                            Map.Attack(territory,neighbour,territory.GetCurrentTroops()-1);
                        }
                    }
                }
            }
        }
        MatchManager.EndTurn();
        return true; 
    }
    public Color GetColor()
    {
        return playerColorToColor[colour];
    }
    public PlayerColour GetPlayerColour() { return colour; }
    public List<Territory> GetTerritories() {  return territories; }
    public void AddTerritory(Territory territory)
    {
        territories.Add(territory);
    }
}
