using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static Territory;

public class MatchManager : MonoBehaviour
{
    static TurnState state;
    public static TurnState GetTurnState()
    {
        return state;
    }

    [SerializeField]
    List<Player> playerList;
    int currentTurnIndex;
    int turnNumber;
    List<Territory> currentPlayerTerritories;
    int troopCount;
    static MatchManager instance;
    public enum TurnState { Deploying, Attacking, Fortifying };
    private Dictionary<int, int> StartingTroopCounts = new Dictionary<int, int>{{3,35}, { 4, 30 } , { 5, 25 } , { 6, 20 } };
    int troopDeployCount;
    public static int GetCurrentTroopDeployCount()
    {
        return instance.troopDeployCount;
    }
    public void Awake()
    {
        state = TurnState.Deploying;
        instance = this;
    }
    public void Start()
    {
        //Create the number of neccesary A.I, but only in the actual game
        if (!Map.IsSimulated())
        {
            for (int i = 0; i < PlayOptionsManagement.GetNumberOfAIPlayers(); i++)
            {
                //Should load this from options
                Player newAI = Instantiate(Resources.Load("AIPlayer") as GameObject).GetComponent<Player>();

                newAI.SetColor((Player.PlayerColour)(i));

                playerList.Add(newAI);
            }
        }

        troopDeployCount = StartingTroopCounts[playerList.Count];
        UpdateInfoTextSetup(troopDeployCount);
        turnNumber = 1;
        Deck.CreateDeck();

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
        state = TurnState.Deploying;
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
        state = TurnState.Attacking;
        UpdateInfoTextDefault("Attack");
        instance.playerList[instance.currentTurnIndex].Attack();
    }
    public static void Fortify()
    {
        state = TurnState.Fortifying;
        UpdateInfoTextDefault("Fortify");
        instance.playerList[instance.currentTurnIndex].Fortify();
    }
    public void SwitchPlayer() { if (currentTurnIndex == playerList.Count - 1|| currentTurnIndex<0) { currentTurnIndex = 0;turnNumber++; } else { currentTurnIndex++; } }
    public List<Territory> GetCurrentPlayerTerritories() {  return currentPlayerTerritories; }
    public static void EndTurn()
    {
        instance.playerList[instance.currentTurnIndex].OnTurnEnd();

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
