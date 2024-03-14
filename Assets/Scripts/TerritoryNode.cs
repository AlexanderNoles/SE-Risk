using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
public class TerritoryNode : INode
{
    public Territory territory;
    public List<INode> GetNeighbours()
    {
        List<Territory> territories = territory.GetNeighbours();
        List<INode> nodes = new List<INode>();
        foreach (Territory territory in territories)
        {
            if (territory.GetOwner() != this.territory.GetOwner()) { nodes.Add(new TerritoryNode().SetTerritory(territory)); }
        }
        return nodes;
    }

    public Vector3 GetPosition()
    {
        return territory.GetCentrePoint();
    }
    public TerritoryNode SetTerritory(Territory territory)
    {
        this.territory = territory;
        return this;
    }
}
