using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Inherits from player, essentially a set of wrapper functions for NetworkConnection functions.
/// </summary>
public class NetworkPlayer : Player
{
    private uint netID = 0;
    private NetworkConnection personelConnectionObject;
    private NetworkConnectionToClient connectionToClient;

    /// <summary>
    /// Notify the target client they have been initilized and setup this network player on host.
    /// </summary>
    /// <param name="clientNetID">The target network ID.</param>
    /// <exception cref="System.Exception">Thrown if player with that ID does not exist.</exception>
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

    public override void ResetPlayer()
    {
        personelConnectionObject.RpcResetPlayer(connectionToClient);
    }

    public override void ClaimCapital(List<Territory> territories)
    {
        personelConnectionObject.RpcClaimCapital(connectionToClient, GetInbetween(territories));
    }

    public override void Setup(List<Territory> territories)
    {
        personelConnectionObject.RpcSetup(connectionToClient, GetInbetween(territories));
    }

    public override bool Deploy(List<Territory> territories, int troopCount)
    {
        personelConnectionObject.RpcDeploy(connectionToClient, GetInbetween(territories), troopCount);

        return true;
    }

    public override void Attack()
    {
        personelConnectionObject.RpcAttack(connectionToClient);
    }

    public override void Fortify()
    {
        personelConnectionObject.RpcFortify(connectionToClient);
    }

    public override void OnTurnEnd()
    {
        personelConnectionObject.RpcOnTurnEnd(connectionToClient);
    }

    public override void OnKilled()
    {
        personelConnectionObject.RpcOnKilled(connectionToClient);
    }

    public override void Killed(int numberOfCardsTaken)
    {
        personelConnectionObject.RpcOnKillOtherPlayer(connectionToClient, numberOfCardsTaken);
    }

    private List<int> GetInbetween(List<Territory> territories)
    {
        List<int> inBetween = new List<int>();
        foreach (Territory territory in territories)
        {
            inBetween.Add(territory.GetIndexInMap());
        }

        return inBetween;
    }
}
