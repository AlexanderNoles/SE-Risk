using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MonitorBreak;

public class NetworkDataCommunicator : NetworkBehaviour
{
    private static NetworkDataCommunicator instance;

    private void OnEnable()
    {
        instance = this;
    }

    private void OnDisable()
    {
        instance = null;
    }

    [SyncVar(hook = nameof(UpdatePlayUI))]
    public int numberOfPlayers = 0;

    [SyncVar(hook = nameof(UpdatePlayUI))]
    public int numberOfAIPlayers = 0;

    [SyncVar(hook = nameof(UpdatePlayUI))]
    public int mode = 0;


    public static void UpdatePlayUI(int oldValue, int newValue)
    {
        //Contact play screen ui to update it
        PlayOptionsManagement.UpdatePlayerUIExternal(instance.numberOfAIPlayers, instance.numberOfPlayers, instance.mode);
    }

    public static int GetTotalNumberOfPlayers()
    {
        return instance.numberOfPlayers + instance.numberOfAIPlayers;
    }

    public static void UpdateNumberOPlayers(int newNumber)
    {
        instance.numberOfPlayers = newNumber;
    }

    public static void UpdateNumberOfAIPlayers(int newNumber)
    {
        if (instance == null)
        {
            return;
        }

        instance.numberOfAIPlayers = newNumber;
    }

    public static void UpdateMode(int newMode)
    {
        if (instance == null)
        {
            return;
        }
        
        instance.mode = newMode;
    }
}
