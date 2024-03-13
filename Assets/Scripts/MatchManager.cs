using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static Territory;

public class MatchManager : MonoBehaviour
{
    TurnState state;
    [SerializeField]
    List<Player> playerList;
    int currentTurnIndex;
    List<Territory> currentPlayerTerritories;
    int troopCount;
    static MatchManager instance;
    enum TurnState { Deploying, Attacking, Fortifying };
    public void Awake()
    {
        instance = this;
    }
    public void Start()
    {
        foreach (Player player in playerList)
        {
            for (int i = 0; i < 7; i++)
            {
                Territory territory = Map.GetTerritories()[Random.Range(0, Map.GetTerritories().Count)];
                if(territory.GetOwner() == null)
                {
                    territory.SetOwner(player);
                }
                else { i--;}
            }
        }
        Deploy();
    }
    public static void Deploy()
    {
        instance.currentPlayerTerritories = Map.TerritoriesOwnedByPlayer(instance.playerList[instance.currentTurnIndex],out int troopCount);
        if(instance.currentPlayerTerritories.Count == 0)
        {
            instance.playerList.Remove(instance.playerList[instance.currentTurnIndex]);
            instance.currentTurnIndex--;
            instance.SwitchPlayer();
        }
        instance.troopCount = troopCount;
        instance.playerList[instance.currentTurnIndex].Deploy(instance.currentPlayerTerritories,troopCount);
    }
    public static void Attack()
    {
        instance.playerList[instance.currentTurnIndex].Attack();
    }
    public static void Fortify()
    {

    }
    public void SwitchPlayer() { if (currentTurnIndex == playerList.Count - 1|| currentTurnIndex<0) { currentTurnIndex = 0; } else { currentTurnIndex++; } }
    public List<Territory> GetCurrentPlayerTerritories() {  return currentPlayerTerritories; }
    public static void EndTurn()
    {
        instance.SwitchPlayer();
        Deploy();
    }
}
