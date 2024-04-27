using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkConnection : NetworkBehaviour
{
    private static NetworkConnection instance;
    public bool isOnHost = false;
    private static bool touchedServer = false;

    public static bool ActuallyConnectedToServer()
    {
        return touchedServer;
    }

    public static void ResetTouchedServer()
    {
        touchedServer = false;
    }

    private void Awake()
    {
        instance = this;
    }

    public override void OnStartClient()
    {
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
        if (!isLocalPlayer && isOnHost)
        {
            Debug.Log("Connection Lost");

            PlayOptionsManagement.NotifyHostOfLostConnection();
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
            PlayOptionsManagement.NewHostSetup();
        }
    }
}
