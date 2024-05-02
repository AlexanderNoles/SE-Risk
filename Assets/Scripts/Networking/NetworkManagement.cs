using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MonitorBreak;
using Mirror;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages overall network activity, mainly the current state of the client which is then checked by other scripts.
/// </summary>
[IntializeAtRuntime("NetworkManager")]
public class NetworkManagement : NetworkManager
{
    private static NetworkManagement instance;

    /// <summary>
    /// Enum representing the current state of the client.
    /// </summary>
    public enum ClientState
    {
        Offline,
        Host,
        Client
    }

    private static ClientState currentState = ClientState.Offline;
    /// <summary>
    /// Get local client's current state.
    /// </summary>
    /// <returns>ClientState enum.</returns>
    public static ClientState GetClientState()
    {
        return currentState;
    }

    /// <summary>
    /// Even run when local client disconnects.
    /// </summary>
    public static UnityEvent onClientDisconnect = new UnityEvent();

    private static List<NetworkIdentity> playerObjects = new List<NetworkIdentity>();

    /// <summary>
    /// Add a player object to our tracked player objects.
    /// </summary>
    /// <param name="newPlayer">The player to add.</param>
    public static void AddPlayerObject(NetworkIdentity newPlayer)
    {
        playerObjects.Add(newPlayer);
    }

    /// <summary>
    /// Remove a player object from our tracked player objects.
    /// </summary>
    /// <param name="player">The player to remove.</param>
    public static void RemovePlayerObject(NetworkIdentity player)
    {
        playerObjects.Remove(player);
    }

    /// <summary>
    /// Fully reset our tracked player objects.
    /// </summary>
    public static void ResetPlayerObjects()
    {
        playerObjects.Clear();
    }

    /// <summary>
    /// Get a player object based on a netID.
    /// </summary>
    /// <param name="netID">The target netID.</param>
    /// <returns>The player's network identity.</returns>
    public static NetworkIdentity GetSpeificPlayerIdentity(uint netID)
    {
        foreach (NetworkIdentity player in playerObjects)
        {
            if (player.netId == netID)
            {
                return player;
            }
        }


        return null;
    }

    /// <summary>
    /// Make player objects not destroyed by scene loads.
    /// </summary>
    public static void MakePlayerObjectsNonDestroy()
    {
        foreach (NetworkIdentity player in playerObjects)
        {
            DontDestroyOnLoad(player);
        }
    }

    /// <summary>
    /// Standard unity message, forced public by Mirror.
    /// </summary>
    public override void Awake()
    {
        base.Awake();
        instance = this;
    }

    /// <summary>
    /// Update the local client's network state.
    /// </summary>
    /// <param name="newState">The new state.</param>
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

    /// <summary>
    /// Function run on local client disconnect.
    /// </summary>
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
