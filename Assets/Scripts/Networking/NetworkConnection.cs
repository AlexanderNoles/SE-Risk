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

        instance.RpcStartGame(0);
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
        //Set on server
        Map.SetTerritoryTroopCount(index, newTroopCount, false);
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
        //Set on server
        Map.SetTerritoryOwner(index, newOwner, false);
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
        //Set on server
        Map.AddCapital(index, newOwner, false);
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
        Deck.CreateDeck(seed);
    }

    public static void UpdateCardTakenAcrossLobby(int deckIndex, int newValue)
    {
        if (NetworkManagement.GetClientState() == NetworkManagement.ClientState.Host)
        {
            UpdateCardTakenAcrossAllClients(deckIndex, newValue);
        }
        else
        {
            instance.CmdUpdateCardTaken(deckIndex, newValue);
        }
    }

    [Command]
    public void CmdUpdateCardTaken(int deckIndex, int newValue)
    {
        //Set on server
        Deck.SetCardTaken(deckIndex, newValue, false);
        UpdateCardTakenAcrossAllClients(deckIndex, newValue);
    }

    private static void UpdateCardTakenAcrossAllClients(int deckIndex, int newValue)
    {
        instance.RpcUpdateCardTakenOnClients(deckIndex, newValue);
    }

    [ClientRpc]
    public void RpcUpdateCardTakenOnClients(int deckIndex, int newValue)
    {
        //Set on client
        Deck.SetCardTaken(deckIndex, newValue, false);
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
        //Set on server
        MatchManager.SetTurnNumber(newTurnNumber, false); 
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
        //Set on server
        Hand.SetSetsTurnedIn(newNumber, false);
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
        //Set on server
        UIManagement.SetText(newText, false);
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
        //Set on server
        UIManagement.AddLineToRollOutput(newText, false);
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
        //Set on server
        UIManagement.RefreshRollOutput(false);
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
}
