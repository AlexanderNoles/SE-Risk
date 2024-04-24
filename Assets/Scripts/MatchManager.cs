using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static MatchManager;

public class MatchManager : MonoBehaviour
{
    private static bool gameOver;
    public struct GameWonInfo
    {
        public string winnerName;
        public string winnerColor;
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


    public void Awake()
    {
        gameOver = false;
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

        //Assertion
        if (playerList.Count < 3)
        {
            //Less than 3 players in lobby, this shouldn't occur unless there has been some issue with the play menu
            //as it shouldn't let you run the game until at least 3 players are in
            throw new System.Exception("Less than 3 players in lobby");
        }

        troopDeployCount = StartingTroopCounts[playerList.Count];
        UpdateInfoTextSetup(troopDeployCount);
        turnNumber = 1;
        Deck.CreateDeck();

        capitalsPlaced = 0;
        Setup();
    }
    public static void Setup()
    {
        if (PlayOptionsManagement.IsConquestMode() && !Map.IsSimulated() && capitalsPlaced < instance.playerList.Count)
        {
            instance.currentPlayerTerritories = Map.GetUnclaimedTerritories(instance.playerList[instance.currentTurnIndex], out List<Territory> playerTerritories);

            UpdateInfoTextDefault("Capital Placement");

            instance.playerList[instance.currentTurnIndex].ClaimCapital(instance.currentPlayerTerritories);
            capitalsPlaced += 1;
        }
        else if (instance.troopDeployCount > 0)
        {
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
            EndTurn(true);
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
    public void SwitchPlayer() 
    {
        if (currentTurnIndex == playerList.Count - 1|| currentTurnIndex<0) 
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
        if (Map.IsSimulated())
        {
            return;
        }

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
                    //This Player has won!
                    gameOver = true;
                }
            }
        }  

        if (gameOver)
        {
            //Create game won info, to be used by game won screen
            gameWonInfo = new GameWonInfo();
            if (current != null)
            {
                gameWonInfo.winnerName = current.GetColorName();
                gameWonInfo.winnerColor = current.GetColorName().ToLower();
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
}
