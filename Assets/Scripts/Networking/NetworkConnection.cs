using Mirror;
using System.Collections;
using System.Collections.Generic;
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
        Debug.Log(clientLocalPlayer);
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

    public static void UpdateAllClientsTroopCountOnTerritory(int index, int newTroopCount)
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

    public static void UpdateAllClientsOwnerOnTerritory(int index, int newOwner)
    {
        instance.RpcUpdateOwnerOnClient(index, newOwner);
    }

    [ClientRpc]
    public void RpcUpdateOwnerOnClient(int index, int newOwner)
    {
        //Set on client
        Map.SetTerritoryOwner(index, newOwner, false);
    }
}
