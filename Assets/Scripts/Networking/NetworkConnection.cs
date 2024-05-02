using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Player;

/// <summary>
/// Handles the connection of a client to a host and vice versa. Mirror demands certain functions are public (Commands, Rpcs, etc.) so many functions here are public when they arguably shouldn't be. Main access points are always the static functions and that is really all that should be being called outside of this class.
/// </summary>
public class NetworkConnection : NetworkBehaviour
{
    private static NetworkConnection instance;
    private LocalPlayer clientLocalPlayer;

    private void SetLocalPlayer()
    {
        clientLocalPlayer = FindObjectOfType<LocalPlayer>();
    }


    public bool isOnHost = false;
    public static uint networkID;
    private static bool touchedServer = false;
    private static bool intentionallyDisconnecting;

    /// <summary>
    /// Did we ever actually connect to the server? Returns false if we tried to connect and couldn't for whatever reason.
    /// </summary>
    /// <returns>true or false</returns>
    public static bool ActuallyConnectedToServer()
    {
        return touchedServer;
    }

    /// <summary>
    /// Reset the static touched server bool as we don't want to carry a positive result across lobbies.
    /// </summary>
    public static void ResetTouchedServer()
    {
        touchedServer = false;
    }

    /// <summary>
    /// Handles a connecting client. If local, set static network ID, update if server actually touched and if lobby is full. If can connect update play UI.
    /// </summary>
    public override void OnStartClient()
    {
        if (isLocalPlayer)
        {
            networkID = GetComponent<NetworkIdentity>().netId;
            intentionallyDisconnecting = false;
        }

        NetworkManagement.AddPlayerObject(GetComponent<NetworkIdentity>());
        if (!isLocalPlayer || isOnHost)
        {
            return;
        }
        else
        {
            instance = this;
            Debug.Log("CLIENT CONNECTED: " + PlayScreenNetworkDataCommunicator.GetTotalNumberOfPlayers() + " in lobby");
            touchedServer = true;

            //Check if the host has any avaliable slots
            //if not then disconnect
            if (PlayScreenNetworkDataCommunicator.GetTotalNumberOfPlayers() > 5)
            {
                //Go offline immediately
                //Notify the playoptions management so it doesn't run the swipe out transition
                //as the join button is already doing those transitions
                NetworkManagement.ResetPlayerObjects();
                PlayOptionsManagement.DontRunDisconnectTransitions();
                NetworkManagement.UpdateClientNetworkState(NetworkManagement.ClientState.Offline);
            }
            else
            {
                //Update our local play screen ui
                //a.k.a remove ability to add AI and change mode
                //and add number of AI set by host
                PlayScreenNetworkDataCommunicator.UpdatePlayUI(-1, -1);
            }
        }
    }

    /// <summary>
    /// Handles a client disconnecting. If this happens mid game all players are kicked back to the main menu and lobby is disconnected.
    /// </summary>
    public override void OnStopClient()
    {
        if (isLocalPlayer)
        {
            instance = null;
            NetworkManagement.ResetPlayerObjects();
        }

        if (!isLocalPlayer)
        {
            if(isOnHost)
            {
                Debug.Log("Connection Lost");

                PlayOptionsManagement.NotifyHostOfLostConnection();
            }

            NetworkManagement.RemovePlayerObject(GetComponent<NetworkIdentity>());
        }

        //Someone disconnected mid game so we return to the title screen
        if (SceneManager.GetActiveScene().buildIndex == 1 && !intentionallyDisconnecting) //Play scene
        {
            MenuManagement.SetDefaultMenu(MenuManagement.Menu.Main);
            NetworkManagement.UpdateClientNetworkState(NetworkManagement.ClientState.Offline);
            SceneManager.LoadScene(0);
        }
    }

    /// <summary>
    /// Handles starting the server. Setup host UI is local and if not notify the host of the new connection.
    /// </summary>
    public override void OnStartServer()
    {
        if (isOnHost)
        {
            return;
        }

        //This means this runs only on inital connection
        isOnHost = true;

        if (!isLocalPlayer)
        {
            Debug.Log("Connection added!");

            PlayOptionsManagement.NotifyHostOfNewConnection();
        }
        else
        {
            touchedServer = true;
            instance = this;
            PlayOptionsManagement.NewHostSetup();
        }
    }

    /// <summary>
    /// Static function to tell clients to start the match.
    /// </summary>
    /// <exception cref="System.Exception">Thrown if run by client.</exception>
    public static void StartGameServerCommand()
    {
        if (instance == null)
        {
            throw new System.Exception("Shouldn't be running this. Likely a client.");
        }

        instance.RpcStartGame(1);
    }


    //COMMUNICATION
    //Has scene loaded yet?

    private PlayerColour colourToSet;
    private List<int> inbetween;
    private List<Territory> targetTerritories;

    private void SetTargetTerritories()
    {
        targetTerritories = new List<Territory>();

        foreach (int index in inbetween)
        {
            targetTerritories.Add(Map.GetTerritory(index));
        }
    }


    private bool ShouldWait()
    {
        return clientLocalPlayer == null;
    }

    private IEnumerator Wait()
    {
        yield return new WaitForEndOfFrame();
        SetLocalPlayer();
    }


    /// <summary>
    /// Tell all clients to start game.
    /// </summary>
    /// <param name="sceneIndex">Scene to load, typically 1.</param>
    [ClientRpc]
    public void RpcStartGame(int sceneIndex)
    {
        NetworkManagement.MakePlayerObjectsNonDestroy();
        SceneManager.LoadScene(sceneIndex);
    }

    /// <summary>
    /// Notify a specific client that their player has been created server side.
    /// </summary>
    /// <param name="target">Target client connection.</param>
    /// <param name="colour">Client's new PlayerColour.</param>
    [TargetRpc]
    public void RpcNotifyTarget(NetworkConnectionToClient target, PlayerColour colour)
    {
        colourToSet = colour;
        StartCoroutine(nameof(WaitBeforeLocalPlayerSetup));
    }

    private IEnumerator WaitBeforeLocalPlayerSetup()
    {
        //We do this because the host will load and run start before the client loads their scene
        //We should just be waiting for one frame but we wait for the amount needed just in case of ping and whatnot (we are on lan but still)
        while (ShouldWait())
        {
            yield return Wait();
        }

        //Actually setup local player
        clientLocalPlayer.SetColor(colourToSet);
    }

    /// <summary>
    /// Tell a specific client to reset their local player object.
    /// </summary>
    /// <param name="target">Target client connection.</param>
    [TargetRpc]
    public void RpcResetPlayer(NetworkConnectionToClient target)
    {
        StartCoroutine(nameof(WaitBeforeResetPlayer));
    }

    private IEnumerator WaitBeforeResetPlayer()
    {
        //We do this because the host will load and run start before the client loads their scene
        //We should just be waiting for one frame but we wait for the amount needed just in case of ping and whatnot (we are on lan but still)
        while (ShouldWait())
        {
            yield return Wait();
        }

        //Actually setup local player
        clientLocalPlayer.ResetPlayer();
    }

    /// <summary>
    /// Tell a specific client to claim a capital territory. Used only in conquest mode.
    /// </summary>
    /// <param name="target">Target client connection.</param>
    /// <param name="territoryIndexes">Territories they can take.</param>
    [TargetRpc]
    public void RpcClaimCapital(NetworkConnectionToClient target, List<int> territoryIndexes)
    {
        inbetween = territoryIndexes;
        StartCoroutine(nameof(WaitBeforeClaimCapital));
    }

    private IEnumerator WaitBeforeClaimCapital()
    {
        //We do this because the host will load and run start before the client loads their scene
        //We should just be waiting for one frame but we wait for the amount needed just in case of ping and whatnot (we are on lan but still)
        while (ShouldWait())
        {
            yield return Wait();
        }

        SetTargetTerritories();
        clientLocalPlayer.ClaimCapital(targetTerritories);
    }

    /// <summary>
    /// Tell a specific client to run their setup phase.
    /// </summary>
    /// <param name="target">Target client connection.</param>
    /// <param name="territoryIndexes">Territories they can take/add troops too.</param>
    [TargetRpc]
    public void RpcSetup(NetworkConnectionToClient target, List<int> territoryIndexes)
    {
        inbetween = territoryIndexes;
        StartCoroutine(nameof(WaitBeforeSetup));
    }

    private IEnumerator WaitBeforeSetup()
    {
        //We do this because the host will load and run start before the client loads their scene
        //We should just be waiting for one frame but we wait for the amount needed just in case of ping and whatnot (we are on lan but still)
        while (ShouldWait())
        {
            yield return Wait();
        }

        SetTargetTerritories();
        clientLocalPlayer.Setup(targetTerritories);
    }

    /// <summary>
    /// Tell a specific client to run their deploy phase.
    /// </summary>
    /// <param name="target">Target client connection.</param>
    /// <param name="territoryIndexes">Territories they can add troops too.</param>
    /// <param name="troopCount">Number of troops they can add.</param>
    [TargetRpc]
    public void RpcDeploy(NetworkConnectionToClient target, List<int> territoryIndexes, int troopCount)
    {
        inbetween = territoryIndexes;
        StartCoroutine(nameof(WaitBeforeDeploy), troopCount);
    }

    private IEnumerator WaitBeforeDeploy(int troopCount)
    {
        //We do this because the host will load and run start before the client loads their scene
        //We should just be waiting for one frame but we wait for the amount needed just in case of ping and whatnot (we are on lan but still)
        while (ShouldWait())
        {
            yield return Wait();
        }

        SetTargetTerritories();
        clientLocalPlayer.Deploy(targetTerritories, troopCount);
    }

    /// <summary>
    /// Tell a specific client to run their attack phase.
    /// </summary>
    /// <param name="target">Target client connection.</param>
    [TargetRpc]
    public void RpcAttack(NetworkConnectionToClient target)
    {
        StartCoroutine(nameof(WaitBeforeAttack));
    }

    private IEnumerator WaitBeforeAttack()
    {
        //We do this because the host will load and run start before the client loads their scene
        //We should just be waiting for one frame but we wait for the amount needed just in case of ping and whatnot (we are on lan but still)
        while (ShouldWait())
        {
            yield return Wait();
        }

        clientLocalPlayer.Attack();
    }

    /// <summary>
    /// Tell a specific client to run their fortify phase.
    /// </summary>
    /// <param name="target">Target client connection.</param>
    [TargetRpc]
    public void RpcFortify(NetworkConnectionToClient target)
    {
        StartCoroutine(nameof(WaitBeforeFortify));
    }

    private IEnumerator WaitBeforeFortify()
    {
        //We do this because the host will load and run start before the client loads their scene
        //We should just be waiting for one frame but we wait for the amount needed just in case of ping and whatnot (we are on lan but still)
        while (ShouldWait())
        {
            yield return Wait();
        }

        clientLocalPlayer.Fortify();
    }

    /// <summary>
    /// Tell a specific client to run their turn has eneded.
    /// </summary>
    /// <param name="target">Target client connection.</param>
    [TargetRpc]
    public void RpcOnTurnEnd(NetworkConnectionToClient target)
    {
        StartCoroutine(nameof(WaitBeforeOnTurnEnd));
    }

    private IEnumerator WaitBeforeOnTurnEnd()
    {
        //We do this because the host will load and run start before the client loads their scene
        //We should just be waiting for one frame but we wait for the amount needed just in case of ping and whatnot (we are on lan but still)
        while (ShouldWait())
        {
            yield return Wait();
        }

        clientLocalPlayer.OnTurnEnd();
    }

    /// <summary>
    /// Tell a specifc client they have been killed.
    /// </summary>
    /// <param name="target">Target client connection.</param>
    [TargetRpc]
    public void RpcOnKilled(NetworkConnectionToClient target)
    {
        StartCoroutine(nameof(WaitBeforeKilled));
    }

    private IEnumerator WaitBeforeKilled()
    {
        //We do this because the host will load and run start before the client loads their scene
        //We should just be waiting for one frame but we wait for the amount needed just in case of ping and whatnot (we are on lan but still)
        while (ShouldWait())
        {
            yield return Wait();
        }

        clientLocalPlayer.OnKilled();
    }

    /// <summary>
    /// Tell a specifc client they have killed another player.
    /// </summary>
    /// <param name="target">Target client connection.</param>
    /// <param name="cardCount">The amount of cards that player has.</param>
    [TargetRpc]
    public void RpcOnKillOtherPlayer(NetworkConnectionToClient target, int cardCount)
    {
        StartCoroutine(nameof(WaitBeforeKilledOtherPlayer), cardCount);
    }

    private IEnumerator WaitBeforeKilledOtherPlayer(int cardCount)
    {
        //We do this because the host will load and run start before the client loads their scene
        //We should just be waiting for one frame but we wait for the amount needed just in case of ping and whatnot (we are on lan but still)
        while (ShouldWait())
        {
            yield return Wait();
        }

        clientLocalPlayer.Killed(cardCount);
    }

    //Win check
    /// <summary>
    /// Static inbetween function to run the win check on the server.
    /// </summary>
    /// <param name="current">The player who could've won.</param>
    public static void ServerWinCheck(int current)
    {
        instance.CmdWinCheck(current);
    }

    /// <summary>
    /// Command function to run the win check on the server.
    /// </summary>
    /// <param name="current">The player who could've won.</param>
    [Command]
    public void CmdWinCheck(int current)
    {
        MatchManager.WinCheck(current);
    }

    /// <summary>
    /// Static inbetween function to start game exit across lobby.
    /// </summary>
    /// <param name="colourName">Winner name.</param>
    /// <param name="colourHex">Winner colour as hex.</param>
    /// <param name="playerWonIndex">Winner's player index.</param>
    public static void StartGameExitAcrossLobby(string colourName, string colourHex, int playerWonIndex)
    {
        instance.StartGameExitOnClient(colourName, colourHex, playerWonIndex);
    }

    /// <summary>
    /// Tell all clients to exit game.
    /// </summary>
    /// <param name="colourName">Winner name.</param>
    /// <param name="colourHex">Winner colour as hex.</param>
    /// <param name="playerWonIndex">Winner's player index.</param>
    [ClientRpc]
    public void StartGameExitOnClient(string colourName, string colourHex, int playerWonIndex)
    {
        //We do this so the disconnect handler doesn't kick in
        intentionallyDisconnecting = true;

        if (NetworkManagement.GetClientState() != NetworkManagement.ClientState.Host)
        {
            //Construct game won info
            MatchManager.CreateGameWonInfo(colourName, colourHex, playerWonIndex);
        }

        MatchManager.StartExitTransition();
    }

    //Setup
    /// <summary>
    /// Switch current player setup.
    /// </summary>
    public static void SwitchPlayerSetup()
    {
        instance.SwitchPlayerSetupOnServer();
    }

    /// <summary>
    /// Actual Command to switch player setup on server.
    /// </summary>
    [Command]
    public void SwitchPlayerSetupOnServer()
    {
        MatchManager.SwitchPlayerSetup();
    }

    /// <summary>
    /// End turn on server.
    /// </summary>
    public static void EndTurn()
    {
        instance.EndTurnOnServer(instance.clientLocalPlayer.GetIndex());
    }

    /// <summary>
    /// Actual Command to end turn on server.
    /// </summary>
    [Command]
    public void EndTurnOnServer(int playerIndex)
    {
        MatchManager.EndTurn(playerIndex);
    }

    //TROOP COUNT
    /// <summary>
    /// Update the troop count of a territory across the lobby.
    /// </summary>
    /// <param name="territoryIndex">Target territory index in map.</param>
    /// <param name="newTroopCount">Updated value.</param>
    public static void UpdateTerritoryTroopCountAcrossLobby(int territoryIndex, int newTroopCount)
    {
        if (NetworkManagement.GetClientState() == NetworkManagement.ClientState.Host)
        {
            //Just run on all clients
            UpdateAllClientsTroopCountOnTerritory(territoryIndex, newTroopCount);
        }
        else
        {
            //Send to server
            instance.CmdUpdateChangedTerritoryTroopCount(territoryIndex, newTroopCount);
        }
    }

    /// <summary>
    /// Actual Command to update troop count.
    /// </summary>
    /// <param name="index">Territory index.</param>
    /// <param name="newTroopCount">Updated value.</param>
    [Command]
    public void CmdUpdateChangedTerritoryTroopCount(int index, int newTroopCount)
    {
        UpdateAllClientsTroopCountOnTerritory(index, newTroopCount);
    }

    private static void UpdateAllClientsTroopCountOnTerritory(int index, int newTroopCount)
    {
        instance.RpcUpdateTroopCountOnClient(index, newTroopCount);
    }

    /// <summary>
    /// Tell all clients to update troop count. This includes the client who changed it originally.
    /// </summary>
    /// <param name="index">Territory index.</param>
    /// <param name="newTroopCount">Updated value.</param>
    [ClientRpc]
    public void RpcUpdateTroopCountOnClient(int index, int newTroopCount)
    {
        //Set on client
        Map.SetTerritoryTroopCount(index, newTroopCount, false);
    }

    //OWNER
    /// <summary>
    /// Update the owner of a territory across the lobby.
    /// </summary>
    /// <param name="territoryIndex">Target territory index in map.</param>
    /// <param name="newOwner">Updated value.</param>
    public static void UpdateTerritoryOwnerAcrossLobby(int territoryIndex, int newOwner)
    {
        if (NetworkManagement.GetClientState() == NetworkManagement.ClientState.Host)
        {
            //Just run on all clients as we are the host
            UpdateAllClientsOwnerOnTerritory(territoryIndex, newOwner);
        }
        else
        {
            //Send to server
            instance.CmdUpdateChangedTerritoryOwner(territoryIndex, newOwner);
        }
    }

    /// <summary>
    /// Actual Command to update owner.
    /// </summary>
    /// <param name="index">Territory index.</param>
    /// <param name="newOwner">Updated value.</param>
    [Command]
    public void CmdUpdateChangedTerritoryOwner(int index, int newOwner)
    {
        UpdateAllClientsOwnerOnTerritory(index, newOwner);
    }

    private static void UpdateAllClientsOwnerOnTerritory(int index, int newOwner)
    {
        Map.SetTerritoryOwner(index, newOwner, false);
        instance.RpcUpdateOwnerOnClient(index, newOwner);
    }

    /// <summary>
    /// Tell all clients to update owner. This includes the client who changed it originally.
    /// </summary>
    /// <param name="index">Territory index.</param>
    /// <param name="newOwner">Updated value.</param>
    [ClientRpc]
    public void RpcUpdateOwnerOnClient(int index, int newOwner)
    {
        //Set on client
        Map.SetTerritoryOwner(index, newOwner, false);
    }

    //CAPITAL
    /// <summary>
    /// Update a player capital across lobby.
    /// </summary>
    /// <param name="territoryIndex">Target territory index in map.</param>
    /// <param name="newOwner">Updated value.</param>
    public static void UpdateCapitalAcrossLobby(int territoryIndex, int newOwner)
    {
        if (NetworkManagement.GetClientState() == NetworkManagement.ClientState.Host)
        {
            UpdateCapitalAllClients(territoryIndex, newOwner);
        }
        else
        {
            instance.CmdUpdateChangedCapital(territoryIndex, newOwner);
        }
    }

    /// <summary>
    /// Actual Command to update capital.
    /// </summary>
    /// <param name="index">Territory index.</param>
    /// <param name="newOwner">Updated value.</param>
    [Command]
    public void CmdUpdateChangedCapital(int index, int newOwner)
    {
        UpdateCapitalAllClients(index, newOwner);
    }

    private static void UpdateCapitalAllClients(int terrIndex, int newOwner)
    {
        instance.RpcUpdateCapitalOnClient(terrIndex, newOwner);
    }

    /// <summary>
    /// Tell all clients to update capital owner. This includes the client who changed it originally.
    /// </summary>
    /// <param name="index">Territory index.</param>
    /// <param name="newOwner">Updated value.</param>
    [ClientRpc]
    public void RpcUpdateCapitalOnClient(int index, int newOwner)
    {
        //Set on client
        Map.AddCapital(index, newOwner, false);
    }

    //DECK
    /// <summary>
    /// Create the deck across lobby.
    /// </summary>
    /// <param name="seed">Consistent RNG seed used to generate deck.</param>
    public static void InitDeckAcrossAllClients(int seed)
    {
        instance.RpcInitDeck(seed);
    }

    /// <summary>
    /// Tell all clients to init their deck.
    /// </summary>
    /// <param name="seed">Consistent RNG seed used to generate deck.</param>
    [ClientRpc]
    public void RpcInitDeck(int seed)
    {
        StartCoroutine(nameof(WaitTillInitDeck), seed);
    }

    private IEnumerator WaitTillInitDeck(int seed)
    {
        while (ShouldWait())
        {
            yield return Wait();
        }

        Deck.CreateDeck(seed);
    }

    /// <summary>
    /// Remove/Add a card from/to the deck across lobby
    /// </summary>
    /// <param name="deckIndex">The card index in deck.</param>
    /// <param name="newValue">New card taken value (0 for not taken, 1 for taken)</param>
    /// <param name="playerIndex">The player who made the request.</param>
    public static void UpdateCardTakenAcrossLobby(int deckIndex, int newValue, int playerIndex)
    {
        if (NetworkManagement.GetClientState() == NetworkManagement.ClientState.Host)
        {
            UpdateCardTakenAcrossAllClients(deckIndex, newValue, playerIndex);
        }
        else
        {
            instance.CmdUpdateCardTaken(deckIndex, newValue, playerIndex);
        }
    }

    /// <summary>
    /// Actual command to update card taken across lobby.
    /// </summary>
    /// <param name="deckIndex">The card index in deck.</param>
    /// <param name="newValue">New card taken value (0 for not taken, 1 for taken)</param>
    /// <param name="playerIndex">The player who made the request.</param>
    [Command]
    public void CmdUpdateCardTaken(int deckIndex, int newValue, int playerIndex)
    {
        UpdateCardTakenAcrossAllClients(deckIndex, newValue, playerIndex);
    }

    private static void UpdateCardTakenAcrossAllClients(int deckIndex, int newValue, int playerIndex)
    {
        instance.RpcUpdateCardTakenOnClients(deckIndex, newValue, playerIndex);
    }

    /// <summary>
    /// Tell all clients to update card taken value. This includes the client who changed it originally.
    /// </summary>
    /// <param name="deckIndex">Deck index.</param>
    /// <param name="newValue">Updated value.</param>
    /// <param name="playerIndex">Player who made request.</param>
    [ClientRpc]
    public void RpcUpdateCardTakenOnClients(int deckIndex, int newValue, int playerIndex)
    {
        //Set on client
        Deck.SetCardTaken(deckIndex, newValue, playerIndex, false);
    }

    //Turn number
    /// <summary>
    /// Update turn index across lobby.
    /// </summary>
    /// <param name="newTurnNumber">New turn number.</param>
    public static void UpdateTurnNumberAcrossLobby(int newTurnNumber)
    {
        if (NetworkManagement.GetClientState() == NetworkManagement.ClientState.Host)
        {
            UpdateTurnNumberAcrossAllClients(newTurnNumber);
        }
        else
        {
            instance.CmdUpdateTurnNumber(newTurnNumber);
        }
    }

    /// <summary>
    /// Actual Command to update turn number across lobby.
    /// </summary>
    /// <param name="newTurnNumber">New value.</param>
    [Command]
    public void CmdUpdateTurnNumber(int newTurnNumber)
    {
        UpdateTurnNumberAcrossAllClients(newTurnNumber);
    }

    private static void UpdateTurnNumberAcrossAllClients(int newTurnNumber)
    {
        instance.RpcUpdateTurnNumberOnClients(newTurnNumber);
    }

    /// <summary>
    /// Tell all clients to update turn number value. This includes the client who changed it originally.
    /// </summary>
    /// <param name="newTurnNumber">New value.</param>
    [ClientRpc]
    public void RpcUpdateTurnNumberOnClients(int newTurnNumber)
    {
        //Set on client
        MatchManager.SetTurnNumber(newTurnNumber, false);
    }

    //Sets turned in
    /// <summary>
    /// Update sets turned in across lobby.
    /// </summary>
    /// <param name="newNumber">New number of sets turned in.</param>
    public static void UpdateSetsTurnedInAcrossLobby(int newNumber)
    {
        if (NetworkManagement.GetClientState() == NetworkManagement.ClientState.Host)
        {
            UpdateSetsTurnedInAcrossAllClients(newNumber);
        }
        else
        {
            instance.CmdUpdateSetsTurnedIn(newNumber);
        }
    }

    /// <summary>
    /// Actual Command to update sets turned in across lobby.
    /// </summary>
    /// <param name="newNumber">New number of sets turned in.</param>
    [Command]
    public void CmdUpdateSetsTurnedIn(int newNumber)
    {
        UpdateSetsTurnedInAcrossAllClients(newNumber);
    }

    private static void UpdateSetsTurnedInAcrossAllClients(int newNumber)
    {
        instance.RpcUpdateSetsTurnedInOnClients(newNumber);
    }

    /// <summary>
    /// Tell all clients to update number of sets turned in. This includes the client who changed it originally.
    /// </summary>
    /// <param name="newNumber">New number of sets turned in.</param>
    [ClientRpc]
    public void RpcUpdateSetsTurnedInOnClients(int newNumber)
    {
        //Set on client
        Hand.SetSetsTurnedIn(newNumber, false);
    }

    //////  UI
    //Turn info text
    /// <summary>
    /// Update turn info text (top right of screen) across lobby.
    /// </summary>
    /// <param name="newText">The new text to display.</param>
    public static void UpdateTurnInfoTextAcrossLobby(string newText)
    {
        if (NetworkManagement.GetClientState() == NetworkManagement.ClientState.Host)
        {
            UpdateTurnInfoTextAcrossAllClients(newText);
        }
        else
        {
            instance.CmdSetTurnInfoText(newText);
        }
    }

    /// <summary>
    /// Actual Command to update turn info text across lobby.
    /// </summary>
    /// <param name="newText">The new text to display.</param>
    [Command]
    public void CmdSetTurnInfoText(string newText)
    {
        UpdateTurnInfoTextAcrossAllClients(newText);
    }

    private static void UpdateTurnInfoTextAcrossAllClients(string newText)
    {
        instance.RpcUpdateTurnInfoTextOnClients(newText);
    }

    /// <summary>
    /// Tell all clients to update turn info text.
    /// </summary>
    /// <param name="newText">The new text to display.</param>
    [ClientRpc]
    public void RpcUpdateTurnInfoTextOnClients(string newText)
    {
        //Set on client
        StartCoroutine(nameof(WaitTillCanSetText), newText);
    }

    private IEnumerator WaitTillCanSetText(string newText)
    {
        while (!UIManagement.Initialized())
        {
            yield return new WaitForEndOfFrame();
        }

        UIManagement.SetText(newText, false);
    }

    //Roll output
    /// <summary>
    /// Add a line to the roll output across the lobby.
    /// </summary>
    /// <param name="newText">The new text to add.</param>
    public static void AddLineToRollOutputAcrossLobby(string newText)
    {
        if (NetworkManagement.GetClientState() == NetworkManagement.ClientState.Host)
        {
            AddLineToRollOutputAllClients(newText);
        }
        else
        {
            instance.CmdAddLineToRollOutput(newText);
        }
    }

    /// <summary>
    /// Actual Command to add line to roll output.
    /// </summary>
    /// <param name="newText">The new text to add.</param>
    [Command]
    public void CmdAddLineToRollOutput(string newText)
    {
        AddLineToRollOutputAllClients(newText);
    }

    private static void AddLineToRollOutputAllClients(string newText)
    {
        instance.RpcAddLineToRollOutputOnClients(newText);
    }

    /// <summary>
    /// Tell clients to add line to roll output. This include the one that made the request originally
    /// </summary>
    /// <param name="newText">The new text to add.</param>
    [ClientRpc]
    public void RpcAddLineToRollOutputOnClients(string newText)
    {
        //Set on client
        UIManagement.AddLineToRollOutput(newText, false);
    }

    /// <summary>
    /// Refresh (actually display) the roll output across the lobby.
    /// </summary>
    public static void RefreshRollOutputAcrossLobby()
    {
        if (NetworkManagement.GetClientState() == NetworkManagement.ClientState.Host)
        {
            RefreshRollOutputAllClients();
        }
        else
        {
            instance.CmdRefreshRollOutput();
        }
    }

    /// <summary>
    /// Actual Command to refresh roll output.
    /// </summary>
    [Command]
    public void CmdRefreshRollOutput()
    {
        RefreshRollOutputAllClients();
    }

    private static void RefreshRollOutputAllClients()
    {
        instance.RpcRefreshRollOutputOnClients();
    }

    /// <summary>
    /// Tell all clients to refresh their roll output. This includes the one that sent the request originally.
    /// </summary>
    [ClientRpc]
    public void RpcRefreshRollOutputOnClients()
    {
        //Set on client
        StartCoroutine(nameof(WaitTillCanRefreshRollOutput));
    }

    private IEnumerator WaitTillCanRefreshRollOutput()
    {
        while (!UIManagement.Initialized())
        {
            yield return new WaitForEndOfFrame();
        }

        UIManagement.RefreshRollOutput(false);
    }

    //Player info handler
    /// <summary>
    /// Init player info handler on all clients.
    /// </summary>
    /// <param name="players"></param>
    public static void SetupPlayerInfoHandlerOnClients(List<int> players)
    {
        instance.RpcSetupPlayerInfoHandler(players);
    }

    /// <summary>
    /// Tell all clients to init their player info handler. This includes the one that sent the request originally.
    /// </summary>
    [ClientRpc]
    public void RpcSetupPlayerInfoHandler(List<int> players)
    {
        StartCoroutine(nameof(WaitTillCanSetupPlayerInfoHandler), players);
    }

    private IEnumerator WaitTillCanSetupPlayerInfoHandler(List<int> players)
    {
        while (!PlayerInfoHandler.Initialized())
        {
            yield return new WaitForEndOfFrame();
        }

        PlayerInfoHandler.SetPlayers(players, false);
    }

    /// <summary>
    /// Update the player info handler across the lobby with new information.
    /// </summary>
    public static void UpdatePlayerInfoHandlerAcrossLobby()
    {
        if (NetworkManagement.GetClientState() == NetworkManagement.ClientState.Host)
        {
            UpdatePlayerInfoHandlerOnAllClients();
        }
        else
        {
            instance.CmdUpdatePlayerInfoHandler();
        }
    }

    /// <summary>
    /// Actual Command to update the player info handler.
    /// </summary>
    [Command]
    public void CmdUpdatePlayerInfoHandler()
    {
        UpdatePlayerInfoHandlerOnAllClients();
    }

    private static void UpdatePlayerInfoHandlerOnAllClients()
    {
        //Get the current number of cards in each person's hand (as kept track of by the server)
        Dictionary<int, int> indexToCardCount = Deck.GetPlayerCardCounts();

        //Convert to passable format
        List<int> playerIndexes = new List<int>();
        List<int> playerCardCounts = new List<int>();

        foreach (int key in indexToCardCount.Keys)
        {
            playerIndexes.Add(key);
            playerCardCounts.Add(indexToCardCount[key]);
        }

        instance.RpcUpdatePlayerInfoHandler(playerIndexes, playerCardCounts);
    }

    /// <summary>
    /// Tell clients to update player info handler. This includes the one that originally made the request.
    /// </summary>
    /// <param name="playerIndexes">Player indexes. Their index is their colour in the PlayerColour enum.</param>
    /// <param name="playerCardCounts">Each player's remaining card count.</param>
    [ClientRpc]
    public void RpcUpdatePlayerInfoHandler(List<int> playerIndexes, List<int> playerCardCounts)
    {
        //Convert passed data back into usable data
        Dictionary<int, int> outputITDC = new Dictionary<int, int>();

        for (int i = 0; i < playerIndexes.Count; i++)
        {
            outputITDC[playerIndexes[i]] = playerCardCounts[i];
        }

        PlayerInfoHandler.UpdateHandCounts(outputITDC);
        PlayerInfoHandler.UpdateInfo();
    }

    /// <summary>
    /// Update player turn index across lobby.
    /// </summary>
    /// <param name="newPlayerTurnIndex">The updated value.</param>
    public static void UpdateCurrentPlayerTurnIndexAcrossLobby(int newPlayerTurnIndex)
    {
        if (NetworkManagement.GetClientState() == NetworkManagement.ClientState.Host)
        {
            UpdatePlayerTurnIndexOnAllClients(newPlayerTurnIndex);
        }
        else
        {
            instance.CmdUpdatePlayerTurnIndex(newPlayerTurnIndex);
        }
    }

    /// <summary>
    /// Actual Command to update player turn index.
    /// </summary>
    /// <param name="newPlayerTurnIndex">The updated value.</param>
    [Command]
    public void CmdUpdatePlayerTurnIndex(int newPlayerTurnIndex)
    {
        UpdatePlayerTurnIndexOnAllClients(newPlayerTurnIndex);
    }

    private static void UpdatePlayerTurnIndexOnAllClients(int newPlayerTurnIndex)
    {
        instance.RpcUpdateTurnIndexOnClient(newPlayerTurnIndex);
    }

    /// <summary>
    /// Tell clients to update turn index. This includes the one that made the request originally.
    /// </summary>
    /// <param name="newPlayerTurnIndex">The updated value.</param>
    [ClientRpc]
    public void RpcUpdateTurnIndexOnClient(int newPlayerTurnIndex)
    {
        StartCoroutine(nameof(WaitUntilCanUpdateTurnIndexOnClient),newPlayerTurnIndex);
    }

    private IEnumerator WaitUntilCanUpdateTurnIndexOnClient(int newPlayerTurnIndex)
    {
        while (!PlayerInfoHandler.Initialized())
        {
            yield return new WaitForEndOfFrame();
        }

        //Update on clients
        PlayerInfoHandler.SetCurrentPlayerTurnIndex(newPlayerTurnIndex, false);
    }
}
