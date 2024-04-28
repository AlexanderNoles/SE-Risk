using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MonitorBreak;
using Mirror;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[IntializeAtRuntime("NetworkManager")]
public class NetworkManagement : NetworkManager
{
    private static NetworkManagement instance;

    public enum ClientState
    {
        Offline,
        Host,
        Client
    }

    private static ClientState currentState = ClientState.Offline;
    public static ClientState GetClientState()
    {
        return currentState;
    }

    public static UnityEvent onClientDisconnect = new UnityEvent();

    private static List<GameObject> playerObjects = new List<GameObject>();

    public static void AddPlayerObject(GameObject newPlayer)
    {
        playerObjects.Add(newPlayer);
    }

    public static void RemovePlayerObject(GameObject player)
    {
        playerObjects.Remove(player);
    }

    public static void ResetPlayerObjects()
    {
        playerObjects.Clear();
    }

    public static void MakePlayerObjectsNonDestroy()
    {
        foreach (GameObject player in playerObjects)
        {
            DontDestroyOnLoad(player);
        }
    }

    public override void Awake()
    {
        base.Awake();
        instance = this;
    }

    public static void UpdateClientNetworkState(ClientState newState)
    {
        currentState = newState;

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
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
    }

    public override void OnClientDisconnect()
    {
        if(GetClientState() != ClientState.Offline)
        {
            UpdateClientNetworkState(ClientState.Offline);
            return;
        }

        base.OnClientDisconnect();
        onClientDisconnect.Invoke();
    }
}