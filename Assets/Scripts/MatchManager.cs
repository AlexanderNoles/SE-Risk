using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using static MatchManager;

public class MatchManager : MonoBehaviour
{
    private static bool gameOver;

    public static bool IsGameOver()
    {
        return gameOver;
    }

    public struct GameWonInfo
    {
        public string winnerName;
        public string winnerColor;

        public bool localPlayerWon;

        public GameWonInfo(bool lpw)
        {
            localPlayerWon = lpw;
            winnerName = "";
            winnerColor = "";
        }
    }

    private static GameWonInfo gameWonInfo;

    public static GameWonInfo GetGameWonInfo()
    {
        return gameWonInfo;
    }

    static TurnState state;
    public static TurnState GetTurnState()
    {
        return state;
    }

    private List<Player> originalPlayers;
    [SerializeField]
    List<Player> playerList;
    int currentTurnIndex;
    int turnNumber;
    List<Territory> currentPlayerTerritories;
    int troopCount;
    static MatchManager instance;
    public enum TurnState { Deploying, Attacking, Fortifying };

    private static int capitalsPlaced;
    private Dictionary<int, int> StartingTroopCounts = new Dictionary<int, int>{{3,35}, { 4, 30 } , { 5, 25 } , { 6, 20 } };
    int troopDeployCount;
    public static int GetCurrentTroopDeployCount()
    {
        return instance.troopDeployCount;
    }

    public PlayerInfoHandler infoHandler;

    private static bool inSetup = false;
    public static bool InSetup()
    {
        return inSetup;
    }

    public void Awake()
    {
        instance = this;
    }

    public void ResetGame(bool initialReset)
    {
        gameOver = false;
        state = TurnState.Deploying;
        if (!initialReset)
        {
            Map.ResetInstanceMap();
            //Setup players list again from original players
            playerList.Clear();
            foreach (Player player in originalPlayers)
            {
                playerList.Add(player);
            }
        }

        //Reset players
        foreach (Player player in playerList)
        {
            player.ResetPlayer();
        }

        inSetup = true;

        if (infoHandler != null)
        {
            infoHandler.SetPlayers(playerList);
        }

        //Reset match
        troopDeployCount = StartingTroopCounts[playerList.Count];
        UpdateInfoTextSetup(troopDeployCount);
        turnNumber = 1;
        Deck.CreateDeck();

        capitalsPlaced = 0;
        Setup();
    }

    public void Start()
    {
        //Create the number of neccesary A.I, but only in the actual game
            for (int i = 0; i < PlayOptionsManagement.GetNumberOfAIPlayers(); i++)
            {
                //Should load this from options
                Player newAI = Instantiate(Resources.Load("AIPlayer") as GameObject).GetComponent<Player>();

                newAI.SetColor((Player.PlayerColour)(i));

                playerList.Add(newAI);
            }

        //Assertion
        if (playerList.Count < 3)
        {
            //Less than 3 players in lobby, this shouldn't occur unless there has been some issue with the play menu
            //as it shouldn't let you run the game until at least 3 players are in
            throw new System.Exception("Less than 3 players in lobby");
        }

        originalPlayers = new List<Player>();
        //Setup original players list as copy
        foreach (Player player in playerList)
        {
            originalPlayers.Add(player);
        }

        ResetGame(true);
    }
    public static void Setup()
    {
        if (PlayOptionsManagement.IsConquestMode()&& capitalsPlaced < instance.playerList.Count)
        {
            instance.currentPlayerTerritories = Map.GetUnclaimedTerritories(instance.playerList[instance.currentTurnIndex], out List<Territory> playerTerritories);

            UpdateInfoTextDefault("Capital Placement");

            instance.playerList[instance.currentTurnIndex].ClaimCapital(instance.currentPlayerTerritories);
            capitalsPlaced += 1;
        }
        else if (instance.troopDeployCount > 0)
        {
            MonitorBreak.Bebug.Console.Log("Normal Setup");
            instance.currentPlayerTerritories = Map.GetUnclaimedTerritories(instance.playerList[instance.currentTurnIndex], out List<Territory> playerTerritories);

            UpdateInfoTextSetup(instance.troopDeployCount);

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
            MonitorBreak.Bebug.Console.Log("Setup finished");
            instance.turnNumber = 1;
            inSetup = false;
            Deploy();
            return;
        }
    }
    public static void Deploy()
    {
        Debug.Log("deploy");
        state = TurnState.Deploying;
        instance.currentPlayerTerritories = Map.TerritoriesOwnedByPlayerWorth(instance.playerList[instance.currentTurnIndex],out int troopCount);
        if(instance.currentPlayerTerritories.Count == 0)
        {
            PlayerInfoHandler.UpdateInfo();
            instance.playerList.Remove(instance.playerList[instance.currentTurnIndex]);
            instance.currentTurnIndex--;
            EndTurn(true);
            return;
        }
        UpdateInfoTextDefault("Deploy");
        instance.troopCount = troopCount;
        instance.playerList[instance.currentTurnIndex].Deploy(instance.currentPlayerTerritories,troopCount);
    }
    public static void Attack()
    {
        Debug.Log("attack");
        state = TurnState.Attacking;
        UpdateInfoTextDefault("Attack");
        instance.playerList[instance.currentTurnIndex].Attack();
    }
    public static void Fortify()
    {
        Debug.Log("fortify");
        state = TurnState.Fortifying;
        UpdateInfoTextDefault("Fortify");
        instance.playerList[instance.currentTurnIndex].Fortify();
    }
    public void SwitchPlayer() 
    {
        Debug.Log("switched player");
        if (playerList.Count <= 1)
        {
            return;
        }
        else if (currentTurnIndex == playerList.Count - 1|| currentTurnIndex<0) 
        { 
            currentTurnIndex = 0;
            turnNumber++; 
        } 
        else 
        { 
            currentTurnIndex++; 
        } 
    }
    public List<Territory> GetCurrentPlayerTerritories() {  return currentPlayerTerritories; }
    public static void EndTurn(bool playerRemoved = false)
    {
        if (!playerRemoved)
        {
            instance.playerList[instance.currentTurnIndex].OnTurnEnd();
            PlayerInfoHandler.UpdateInfo();
        }

        instance.SwitchPlayer();
        Deploy();
    }
    public static void SwitchPlayerSetup()
    {
        if (instance.currentTurnIndex == instance.playerList.Count - 1)
        {
            instance.troopDeployCount--;
        }
        instance.SwitchPlayer();
        Setup();
    }

    private void Update()
    {
        //Testing code
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            gameOver = true;
            WinCheck(playerList[0]);
        }
    }

    public static void WinCheck(Player current)
    {
        //We get the current player as an argument in case of a break in some other part of the code
        //that would cause a player to attack not on their turn (or more likely the code doesn't think it is their turn) 

        if (current != null)
        {
            if (PlayOptionsManagement.IsConquestMode())
            {
                //Check if the current player has all the capitals in their possesion 
                if (Map.DoesPlayerHoldAllCapitals(current))
                {
                    gameOver = true;
                }
            }
            else
            {
                //Check if only one player is left, this is pretty easy
                //We can't check if only one player is left in the list
                //as they are removed during THEIR deploy phase
                //So we need to simply check if the amount of territories the current player holds is
                //equal to all the territories

                if (current.GetTerritories().Count == Map.GetTerritories().Count)
                {
                    Debug.Log("won");
                    //This Player has won!
                    gameOver = true;
                }
            }
        }  

        if (gameOver)
        {
                //Create game won info, to be used by game won screen
                gameWonInfo = new GameWonInfo(!instance.playerList[0].IsDead());
                if (current != null)
                {
                    gameWonInfo.winnerName = current.GetColorName();

                    string winnerColour = "#" + current.GetColor().ToHexString();
                    
                    gameWonInfo.winnerColor = winnerColour;
                }
                else
                {
                    gameWonInfo.winnerName = "None";
                    gameWonInfo.winnerColor = "white";
                }

                //Load win screen menu
                TransitionControl.onTransitionOver.AddListener(OnOutTransitionOver);
                TransitionControl.RunTransition(TransitionControl.Transitions.SwipeIn);
        }
    }

    public static void OnOutTransitionOver()
    {
        TransitionControl.onTransitionOver.RemoveListener(OnOutTransitionOver);
        MenuManagement.SetDefaultMenu(MenuManagement.Menu.WinScreen);
        SceneManager.LoadScene(1);
    }

    public static void UpdateInfoTextDefault(string turnPhase)
    {
        UIManagement.SetText($"{instance.playerList[instance.currentTurnIndex].GetColorName()} Turn {instance.turnNumber} : {turnPhase}");
    }
    public static void UpdateInfoTextSetup(int count)
    {
        UIManagement.SetText($"Current Troops: {count}");
    }

    public static List<Player> GetPlayers()
    {
        return instance.playerList;
    }

    public static bool OnePlayerAlive(Player current)
    {
        if (current.GetTerritories().Count == Map.GetTerritories().Count)
        {
            Debug.Log("working");
            return true;
        }
        return false;
    }
}
