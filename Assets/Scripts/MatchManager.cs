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
    int turnNumber;
    List<Territory> currentPlayerTerritories;
    int troopCount;
    static MatchManager instance;
    enum TurnState { Deploying, Attacking, Fortifying };
    private Dictionary<int, int> StartingTroopCounts = new Dictionary<int, int>{{3,35}, { 4, 30 } , { 5, 25 } , { 6, 20 } };
    int troopDeployCount;
    public static int GetCurrentTroopDeployCount()
    {
        return instance.troopDeployCount;
    }
    public void Awake()
    {
        instance = this;
    }
    public void Start()
    {
        troopDeployCount = StartingTroopCounts[playerList.Count];
        UpdateInfoTextSetup(troopDeployCount);
        turnNumber = 1;
        Setup();
    }
    public static void Setup()
    {
        if (instance.troopDeployCount > 0)
        {
            instance.currentPlayerTerritories = Map.GetUnclaimedTerritories(instance.playerList[instance.currentTurnIndex], out List<Territory> playerTerritories);
            if (instance.currentPlayerTerritories.Count != 0)
            {
                instance.playerList[instance.currentTurnIndex].Setup(instance.currentPlayerTerritories);
            }
            else
            {
                instance.playerList[instance.currentTurnIndex].Setup(playerTerritories);
            }
        }
        else
        {
            instance.turnNumber = 1;
            Deploy();
            return;
        }
    }
    public static void Deploy()
    {
        instance.currentPlayerTerritories = Map.TerritoriesOwnedByPlayer(instance.playerList[instance.currentTurnIndex],out int troopCount);
        if(instance.currentPlayerTerritories.Count == 0)
        {
            instance.playerList.Remove(instance.playerList[instance.currentTurnIndex]);
            instance.currentTurnIndex--;
            EndTurn();
            return;
        }
        UpdateInfoTextDefault("Deploy");
        instance.troopCount = troopCount;
        instance.playerList[instance.currentTurnIndex].Deploy(instance.currentPlayerTerritories,troopCount);
    }
    public static void Attack()
    {
        UpdateInfoTextDefault("Attack");
        instance.playerList[instance.currentTurnIndex].Attack();
    }
    public static void Fortify()
    {
        UpdateInfoTextDefault("Fortify");
        instance.playerList[instance.currentTurnIndex].Fortify();
    }
    public void SwitchPlayer() { if (currentTurnIndex == playerList.Count - 1|| currentTurnIndex<0) { currentTurnIndex = 0;turnNumber++; } else { currentTurnIndex++; } }
    public List<Territory> GetCurrentPlayerTerritories() {  return currentPlayerTerritories; }
    public static void EndTurn()
    {
        instance.SwitchPlayer();
        Deploy();
    }
    public static void SwitchPlayerSetup()
    {
        if (instance.currentTurnIndex == instance.playerList.Count - 1)
        {
            instance.troopDeployCount--;
            UpdateInfoTextSetup(instance.troopDeployCount);
        }
        Debug.Log(instance.troopDeployCount);
        instance.SwitchPlayer();
        Setup();
    }
    public static void UpdateInfoTextDefault(string turnPhase)
    {
        UIManagement.SetText($"{instance.playerList[instance.currentTurnIndex].GetColorName()} Turn {instance.turnNumber} : {turnPhase}");
    }
    public static void UpdateInfoTextSetup(int count)
    {
        UIManagement.SetText($"Current Troops: {count}");
    }
}
