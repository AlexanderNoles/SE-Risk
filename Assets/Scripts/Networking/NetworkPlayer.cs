using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkPlayer : Player
{
    private uint netID = 0;
    private NetworkConnection personelConnectionObject;
    private NetworkConnectionToClient connectionToClient;

    public void NotifyClient(uint clientNetID)
    {
        netID = clientNetID;
        NetworkIdentity identity = NetworkManagement.GetSpeificPlayerIdentity(netID);
        personelConnectionObject = identity.GetComponent<NetworkConnection>();

        if (identity != null)
        {
            connectionToClient = identity.connectionToClient;
            personelConnectionObject.RpcNotifyTarget(connectionToClient, GetPlayerColour());
        }
        else
        {
            throw new System.Exception("Player with that netID does not exists!");
        }
    }

    public override void ClaimCapital(List<Territory> territories)
    {
        List<int> inBetween = new List<int>();
        foreach (Territory territory in territories)
        {
            inBetween.Add(territory.GetIndexInMap());
        }

        personelConnectionObject.RpcClaimCapital(connectionToClient, inBetween);
    }
}
