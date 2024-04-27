using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MonitorBreak;
using Mirror;
using UnityEngine.Events;

[IntializeAtRuntime("NetworkManager")]
public class NetworkManagement : NetworkManager
{
    private const string HostAddressKey = "HostAddress";
    private static NetworkManagement instance;

    public enum ClientState
    {
        Offline,
        Host,
        Client
    }

    private static ClientState currentState = ClientState.Offline;
    public static UnityEvent onClientDisconnect = new UnityEvent();

    public override void Awake()
    {
        base.Awake();
        instance = this;
    }

    public static void UpdateClientNetworkState(ClientState newState)
    {
        //First stop whatever state we are in
        if (instance.isNetworkActive)
        {
            //Assumes we are client or host not server
            if (NetworkClient.activeHost)
            {
                //If we are host stop host
                instance.StopHost();
            }
            else
            {
                //If we are client stop client
                instance.StopClient();
            }
        }

        if (newState == ClientState.Host)
        {
            instance.StartHost();
        }
        else if (newState == ClientState.Client) 
        {
            instance.StartClient();
        }
        else if (newState == ClientState.Offline)
        {
            //Need to account for in middle of game
        }

        currentState = newState;
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        onClientDisconnect.Invoke();
    }

}
