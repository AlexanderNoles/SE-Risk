using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkConnection : NetworkBehaviour
{
    private static NetworkConnection instance;
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

        NetworkManagement.AddPlayerObject(gameObject);
        if (!isLocalPlayer || isOnHost)
        {
            return;
        }
        else
        {
            Debug.Log("CLIENT CONNECTED: " + NetworkDataCommunicator.GetTotalNumberOfPlayers() + " in lobby");
            touchedServer = true;

            //Check if the host has any avaliable slots
            //if not then disconnect
            if (NetworkDataCommunicator.GetTotalNumberOfPlayers() > 5)
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
                NetworkDataCommunicator.UpdatePlayUI(-1, -1);
            }
        }
    }

    public override void OnStopClient()
    {
        if (isLocalPlayer)
        {
            if (isOnHost)
            {
                instance = null;
            }
            NetworkManagement.ResetPlayerObjects();
        }

        if (!isLocalPlayer)
        {
            if(isOnHost)
            {
                Debug.Log("Connection Lost");

                PlayOptionsManagement.NotifyHostOfLostConnection();
            }

            NetworkManagement.RemovePlayerObject(gameObject);
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
    [ClientRpc]
    public void RpcStartGame(int sceneIndex)
    {
        NetworkManagement.MakePlayerObjectsNonDestroy();
        SceneManager.LoadScene(sceneIndex);
    }
}
