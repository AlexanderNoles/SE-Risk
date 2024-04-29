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

    /// <summary>
    /// Takes player ID and returns the player object from that
    /// </summary>
    /// <param name="target">The ID of the player we want</param>
    /// <returns>The player object corresponding to that ID</returns>
    public static Player GetPlayerFromIndex(int target)
    {
        foreach (Player player in instance.playerList)
        {
            if (player.GetIndex() == target)
            {
                return player;
            }
        }

        return null;
    }

    int currentTurnIndex = 0;
    public static void SetCurrentTurnIndex(int index)
    {
        instance.currentTurnIndex = index;
    }

    static int turnNumber;

    public static void SetTurnNumber(int newTurnNumber, bool makeRequest = true)
    {
        //Update locally
        turnNumber = newTurnNumber;

        if (makeRequest && NetworkManagement.GetClientState() != NetworkManagement.ClientState.Offline)
        {
            NetworkConnection.UpdateTurnNumberAcrossLobby(newTurnNumber);
        }
    }

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
    /// <summary>
    /// Resets the game entirely, so resets all parts of the game like territories and the map and then brings us but to the start of the deploy phase
    /// </summary>
    /// <param name="initialReset"></param>
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

        if (NetworkManagement.GetClientState() != NetworkManagement.ClientState.Client)
        {
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
            SetTurnNumber(1);
            int seed = UnityEngine.Random.Range(0, 1000);
            Deck.CreateDeck(seed);

            if (NetworkManagement.GetClientState() != NetworkManagement.ClientState.Offline)
            {
                NetworkConnection.InitDeckAcrossAllClients(seed);
            }

            capitalsPlaced = 0;
            Setup(0, false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void Start()
    {
        if (NetworkManagement.GetClientState() != NetworkManagement.ClientState.Client)
        {
            //start at one because of local player/host
            int colourIndex = 1;
            uint targetNetID = 3; //First client id
            //Create the number of neccesary network players
            for (int i = 1; i < PlayOptionsManagement.GetNumberOfNetworkPlayers(); i++)
            {
                //when creating the network player we need to notify the corresponding client
                //so they know what colour they are, all the other colours etc.
                NetworkPlayer newNP = Instantiate(Resources.Load("NetworkPlayer") as GameObject).GetComponent<NetworkPlayer>();

                newNP.SetColor((Player.PlayerColour)colourIndex);
                colourIndex++;

                //Notify client/Setup net player
                newNP.NotifyClient(targetNetID);
                targetNetID++;

                playerList.Add(newNP);
            }


            for (int i = 0; i < PlayOptionsManagement.GetNumberOfAIPlayers(); i++)
            {
                //Should load this from options
                Player newAI = Instantiate(Resources.Load("AIPlayer") as GameObject).GetComponent<AIPlayer>();

                newAI.SetColor((Player.PlayerColour)colourIndex);
                colourIndex++;

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
        }

        ResetGame(true);
    }
    /// <summary>
    /// Runs the setup phase of a players turn, allowing them to place one troop on an owned or unoccupied territory
    /// </summary>
    public static void Setup(int currentPlayer, bool requireValidation = true)
    {
        if (requireValidation && currentPlayer != instance.playerList[instance.currentTurnIndex].GetIndex())
        {
            return;
        }


        if (PlayOptionsManagement.IsConquestMode()&& capitalsPlaced < instance.playerList.Count)
        {
            instance.currentPlayerTerritories = Map.GetUnclaimedTerritories(instance.playerList[instance.currentTurnIndex].GetIndex(), out List<Territory> playerTerritories);

            UpdateInfoTextDefault("Capital Placement");

            instance.playerList[instance.currentTurnIndex].ClaimCapital(instance.currentPlayerTerritories);
            capitalsPlaced += 1;
        }
        else if (instance.troopDeployCount > 0)
        {
            MonitorBreak.Bebug.Console.Log("Normal Setup");
            instance.currentPlayerTerritories = Map.GetUnclaimedTerritories(instance.playerList[instance.currentTurnIndex].GetIndex(), out List<Territory> playerTerritories);

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
            SetTurnNumber(1);
            inSetup = false;
            Deploy(0, false);
            return;
        }
    }
    /// <summary>
    /// Runs the deploy phase of a players turn, allowing them to place some number of troops on territories they own
    /// </summary>
    public static void Deploy(int playerMakingRequest, bool requireValidation = false)
    {
        if (requireValidation && instance.playerList[instance.currentTurnIndex].GetIndex() != playerMakingRequest)
        {
            return;
        }

        state = TurnState.Deploying;
        instance.currentPlayerTerritories = Map.TerritoriesOwnedByPlayerWorth(instance.playerList[instance.currentTurnIndex].GetIndex(),out int troopCount);
        if(instance.currentPlayerTerritories.Count == 0)
        {
            PlayerInfoHandler.UpdateInfo();
            instance.playerList.Remove(instance.playerList[instance.currentTurnIndex]);
            instance.currentTurnIndex--;
            EndTurn(0, false, true);
            return;
        }
        UpdateInfoTextDefault("Deploy");
        instance.troopCount = troopCount;
        instance.playerList[instance.currentTurnIndex].Deploy(instance.currentPlayerTerritories,troopCount);
    }
    /// <summary>
    /// Runs the attack phase of a players turn, allowing them to request attacks from territories they own to territories they dont
    /// </summary>
    public static void Attack(int playerMakingRequest, bool requireValidation = false)
    {
        if (requireValidation && instance.playerList[instance.currentTurnIndex].GetIndex() != playerMakingRequest)
        {
            return;
        }

        state = TurnState.Attacking;
        UpdateInfoTextDefault("Attack");
        instance.playerList[instance.currentTurnIndex].Attack();
    }
    /// <summary>
    /// Runs the fortify phase of a players turn, allowing them to move troops from one territory they own to one other territory they also own
    /// </summary>
    public static void Fortify(int playerMakingRequest, bool requireValidation = false)
    {
        if (requireValidation && instance.playerList[instance.currentTurnIndex].GetIndex() != playerMakingRequest)
        {
            return;
        }

        state = TurnState.Fortifying;
        UpdateInfoTextDefault("Fortify");
        instance.playerList[instance.currentTurnIndex].Fortify();
    }

    /// <summary>
    /// Switches the current turn index to be the player whos turn it is next
    /// </summary>
    public void SwitchPlayer() 
    {
        if (playerList.Count <= 1)
        {
            return;
        }
        else if (currentTurnIndex == playerList.Count - 1|| currentTurnIndex<0) 
        { 
            currentTurnIndex = 0;
            SetTurnNumber(turnNumber + 1); 
        } 
        else 
        { 
            currentTurnIndex++; 
        } 
    }
    public List<Territory> GetCurrentPlayerTerritories() {  return currentPlayerTerritories; }
    /// <summary>
    /// Ends a players turn by drawing a card and then switching to the next player
    /// </summary>
    /// <param name="playerRemoved"></param>
    public static void EndTurn(int playerMakingRequest, bool requireValidation = true, bool playerRemoved = false)
    {
        if (NetworkManagement.GetClientState() == NetworkManagement.ClientState.Client)
        {
            //Make a request to the server
            //To end our turn there
            NetworkConnection.EndTurn();
        }
        else
        {
            if (requireValidation && playerMakingRequest != instance.playerList[instance.currentTurnIndex].GetIndex())
            {
                throw new Exception("Turn end not valid!");
            }

            if (!playerRemoved)
            {
                instance.playerList[instance.currentTurnIndex].OnTurnEnd();
                PlayerInfoHandler.UpdateInfo();
            }

            instance.SwitchPlayer();
            Deploy(0, false);
        }
    }

    /// <summary>
    /// Switches the player in the setup phase, to ensure the counter tracks accordingly
    /// </summary>
    public static void SwitchPlayerSetup()
    {
        if (NetworkManagement.GetClientState() == NetworkManagement.ClientState.Client)
        {
            //Make a request to the server instead
            NetworkConnection.SwitchPlayerSetup();
        }
        else
        {
            if (instance.currentTurnIndex == instance.playerList.Count - 1)
            {
                instance.troopDeployCount--;
            }
            instance.SwitchPlayer();
            Setup(0, false);
        }
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
    /// <summary>
    /// Checks to see if the passed player has won the game, if they have, the game is ended and we transition to the win screen
    /// </summary>
    /// <param name="current">The player we are checking to see if they have won</param>
    public static void WinCheck(Player current)
    {
        //We get the current player as an argument in case of a break in some other part of the code
        //that would cause a player to attack not on their turn (or more likely the code doesn't think it is their turn) 

        if (current != null)
        {
            if (PlayOptionsManagement.IsConquestMode())
            {
                //Check if the current player has all the capitals in their possesion 
                if (Map.DoesPlayerHoldAllCapitals(current.GetIndex()))
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
    /// <summary>
    /// Stops the transition effect and switches to the menu scene
    /// </summary>
    public static void OnOutTransitionOver()
    {
        TransitionControl.onTransitionOver.RemoveListener(OnOutTransitionOver);
        MenuManagement.SetDefaultMenu(MenuManagement.Menu.WinScreen);
        SceneManager.LoadScene(1);
    }
    /// <summary>
    /// Updates the turn info text to match the current player and turn phase
    /// </summary>
    /// <param name="turnPhase"></param>
    public static void UpdateInfoTextDefault(string turnPhase)
    {
        UIManagement.SetText($"{instance.playerList[instance.currentTurnIndex].GetColorName()} Turn {turnNumber} : {turnPhase}");
    }
    /// <summary>
    /// Updates the turn info text to display the number of troops left to set up
    /// </summary>
    /// <param name="count"></param>
    public static void UpdateInfoTextSetup(int count)
    {
        UIManagement.SetText($"Current Troops: {count}");
    }

    public static List<Player> GetPlayers()
    {
        return instance.playerList;
    }

    /// <summary>
    /// Checks to see if the passed player is the only living player
    /// </summary>
    /// <param name="current"></param>
    /// <returns></returns>
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
