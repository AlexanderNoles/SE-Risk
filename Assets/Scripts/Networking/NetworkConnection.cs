using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Player;

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

    public static bool ActuallyConnectedToServer()
    {
        return touchedServer;
    }

    public static void ResetTouchedServer()
    {
        touchedServer = false;
    }

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
            SceneManager.LoadScene(0);
        }
    }

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



    [ClientRpc]
    public void RpcStartGame(int sceneIndex)
    {
        NetworkManagement.MakePlayerObjectsNonDestroy();
        SceneManager.LoadScene(sceneIndex);
    }

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
    public static void ServerWinCheck(int current)
    {
        instance.CmdWinCheck(current);
    }

    [Command]
    public void CmdWinCheck(int current)
    {
        MatchManager.WinCheck(current);
    }

    public static void StartGameExitAcrossLobby(string colourName, string colourHex, int playerWonIndex)
    {
        instance.StartGameExitOnClient(colourName, colourHex, playerWonIndex);
    }

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
    public static void SwitchPlayerSetup()
    {
        instance.SwitchPlayerSetupOnServer();
    }

    [Command]
    public void SwitchPlayerSetupOnServer()
    {
        MatchManager.SwitchPlayerSetup();
    }

    public static void EndTurn()
    {
        instance.EndTurnOnServer(instance.clientLocalPlayer.GetIndex());
    }

    [Command]
    public void EndTurnOnServer(int playerIndex)
    {
        MatchManager.EndTurn(playerIndex);
    }

    //TROOP COUNT
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

    [Command]
    public void CmdUpdateChangedTerritoryTroopCount(int index, int newTroopCount)
    {
        UpdateAllClientsTroopCountOnTerritory(index, newTroopCount);
    }

    private static void UpdateAllClientsTroopCountOnTerritory(int index, int newTroopCount)
    {
        instance.RpcUpdateTroopCountOnClient(index, newTroopCount);
    }

    [ClientRpc]
    public void RpcUpdateTroopCountOnClient(int index, int newTroopCount)
    {
        //Set on client
        Map.SetTerritoryTroopCount(index, newTroopCount, false);
    }

    //OWNER
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

    [Command]
    public void CmdUpdateChangedTerritoryOwner(int index, int newOwner)
    {
        UpdateAllClientsOwnerOnTerritory(index, newOwner);
    }

    private static void UpdateAllClientsOwnerOnTerritory(int index, int newOwner)
    {
        instance.RpcUpdateOwnerOnClient(index, newOwner);
    }

    [ClientRpc]
    public void RpcUpdateOwnerOnClient(int index, int newOwner)
    {
        //Set on client
        Map.SetTerritoryOwner(index, newOwner, false);
    }

    //CAPITAL
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

    [Command]
    public void CmdUpdateChangedCapital(int index, int newOwner)
    {
        UpdateCapitalAllClients(index, newOwner);
    }

    private static void UpdateCapitalAllClients(int terrIndex, int newOwner)
    {
        instance.RpcUpdateCapitalOnClient(terrIndex, newOwner);
    }

    [ClientRpc]
    public void RpcUpdateCapitalOnClient(int index, int newOwner)
    {
        //Set on client
        Map.AddCapital(index, newOwner, false);
    }

    //DECK
    public static void InitDeckAcrossAllClients(int seed)
    {
        instance.RpcInitDeck(seed);
    }

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

    [Command]
    public void CmdUpdateCardTaken(int deckIndex, int newValue, int playerIndex)
    {
        UpdateCardTakenAcrossAllClients(deckIndex, newValue, playerIndex);
    }

    private static void UpdateCardTakenAcrossAllClients(int deckIndex, int newValue, int playerIndex)
    {
        instance.RpcUpdateCardTakenOnClients(deckIndex, newValue, playerIndex);
    }

    [ClientRpc]
    public void RpcUpdateCardTakenOnClients(int deckIndex, int newValue, int playerIndex)
    {
        //Set on client
        Deck.SetCardTaken(deckIndex, newValue, playerIndex, false);
    }

    //Turn number
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

    [Command]
    public void CmdUpdateTurnNumber(int newTurnNumber)
    {
        UpdateTurnNumberAcrossAllClients(newTurnNumber);
    }

    private static void UpdateTurnNumberAcrossAllClients(int newTurnNumber)
    {
        instance.RpcUpdateTurnNumberOnClients(newTurnNumber);
    }

    [ClientRpc]
    public void RpcUpdateTurnNumberOnClients(int newTurnNumber)
    {
        //Set on client
        MatchManager.SetTurnNumber(newTurnNumber, false);
    }

    //Sets turned in
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

    [Command]
    public void CmdUpdateSetsTurnedIn(int newNumber)
    {
        UpdateSetsTurnedInAcrossAllClients(newNumber);
    }

    private static void UpdateSetsTurnedInAcrossAllClients(int newNumber)
    {
        instance.RpcUpdateSetsTurnedInOnClients(newNumber);
    }

    [ClientRpc]
    public void RpcUpdateSetsTurnedInOnClients(int newNumber)
    {
        //Set on client
        Hand.SetSetsTurnedIn(newNumber, false);
    }

    //////  UI
    //Turn info text
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

    [Command]
    public void CmdSetTurnInfoText(string newText)
    {
        UpdateTurnInfoTextAcrossAllClients(newText);
    }

    private static void UpdateTurnInfoTextAcrossAllClients(string newText)
    {
        instance.RpcUpdateTurnInfoTextOnClients(newText);
    }

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

    [Command]
    public void CmdAddLineToRollOutput(string newText)
    {
        AddLineToRollOutputAllClients(newText);
    }

    private static void AddLineToRollOutputAllClients(string newText)
    {
        instance.RpcAddLineToRollOutputOnClients(newText);
    }

    [ClientRpc]
    public void RpcAddLineToRollOutputOnClients(string newText)
    {
        //Set on client
        UIManagement.AddLineToRollOutput(newText, false);
    }

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

    [Command]
    public void CmdRefreshRollOutput()
    {
        RefreshRollOutputAllClients();
    }

    private static void RefreshRollOutputAllClients()
    {
        instance.RpcRefreshRollOutputOnClients();
    }

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
    public static void SetupPlayerInfoHandlerOnClients(List<int> players)
    {
        instance.RpcSetupPlayerInfoHandler(players);
    }

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

    public static void UpdatePlayerInfoHandlerAcrossLobby()
    {
        if (NetworkManagement.GetClientState() == NetworkManagement.ClientState.Host)
        {
            UpdatePlayerInfoHandlerOnAllClients();
        }
        else
        {
            instance.CmdRefreshRollOutput();
        }
    }

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

    [Command]
    public void CmdUpdatePlayerTurnIndex(int newPlayerTurnIndex)
    {
        UpdatePlayerTurnIndexOnAllClients(newPlayerTurnIndex);
    }

    private static void UpdatePlayerTurnIndexOnAllClients(int newPlayerTurnIndex)
    {
        instance.RpcUpdateTurnIndexOnClient(newPlayerTurnIndex);
    }

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
