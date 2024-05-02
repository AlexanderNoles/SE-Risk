using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MonitorBreak;

/// <summary>
/// Basic network communicator to sync lobby settings before the game starts.
/// </summary>
public class PlayScreenNetworkDataCommunicator : NetworkBehaviour
{
    private static PlayScreenNetworkDataCommunicator instance;

    private void OnEnable()
    {
        instance = this;
    }

    private void OnDisable()
    {
        instance = null;
    }

    /// <summary>
    /// Mirror SyncVar. Number of network players.
    /// </summary>
    [SyncVar(hook = nameof(UpdatePlayUI))]
    public int numberOfPlayers = 0;

    /// <summary>
    /// Mirror SyncVar. Number of AI players.
    /// </summary>
    [SyncVar(hook = nameof(UpdatePlayUI))]
    public int numberOfAIPlayers = 0;

    /// <summary>
    /// Mirror SyncVar. Game mode.
    /// </summary>
    [SyncVar(hook = nameof(UpdatePlayUI))]
    public int mode = 0;

    /// <summary>
    /// Update the PlayOptionsManagement play UI. Used as callback for syncvar values updating.
    /// </summary>
    /// <param name="oldValue">Old sync var value.</param>
    /// <param name="newValue">New sync var value.</param>
    public static void UpdatePlayUI(int oldValue, int newValue)
    {
        //Contact play screen ui to update it
        PlayOptionsManagement.UpdatePlayerUIExternal(instance.numberOfAIPlayers, instance.numberOfPlayers, instance.mode);
    }

    /// <summary>
    /// Get the total number of players (AI + Network + local).
    /// </summary>
    /// <returns>Number of players.</returns>
    public static int GetTotalNumberOfPlayers()
    {
        return instance.numberOfPlayers + instance.numberOfAIPlayers;
    }

    /// <summary>
    /// Update instance number of network players.
    /// </summary>
    /// <param name="newNumber"></param>
    public static void UpdateNumberOPlayers(int newNumber)
    {
        instance.numberOfPlayers = newNumber;
    }

    /// <summary>
    /// Update instance number of AI players.
    /// </summary>
    /// <param name="newNumber">New number of AI players.</param>
    public static void UpdateNumberOfAIPlayers(int newNumber)
    {
        if (instance == null)
        {
            return;
        }

        instance.numberOfAIPlayers = newNumber;
    }

    /// <summary>
    /// Update instance game mode.
    /// </summary>
    /// <param name="newMode">The new mode.</param>
    public static void UpdateMode(int newMode)
    {
        if (instance == null)
        {
            return;
        }
        
        instance.mode = newMode;
    }
}
