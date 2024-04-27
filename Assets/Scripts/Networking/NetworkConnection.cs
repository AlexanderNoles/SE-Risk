using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkConnection : NetworkBehaviour
{
    private NetworkIdentity identity;
    private bool isHost = false;

    private void Awake()
    {
        identity = GetComponent<NetworkIdentity>();
    }

    public override void OnStartClient()
    {
        if (isHost)
        {
            return;
        }
        Debug.Log("CLIENT CONNECTED");

        //Check if the host has any avliable slots
        //if so request to be added
    }

    public override void OnStartServer()
    {
        Debug.Log("HOST STARTED");
        isHost = true;
    }
}
