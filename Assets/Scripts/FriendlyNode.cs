using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

/// <summary>
/// INode implementation used to pass data to the Pathfinding namespace. Used to navigate territory map.
/// </summary>
public class FriendlyNode : INode
{
    /// <summary>
    /// The target territory of this node
    /// </summary>
    public Territory territory;

    /// <summary>
    /// Used in the pathfinding algorithm to determine what nodes are next to each other.
    /// </summary>
    /// <returns>A list of all the neighbour nodes.</returns>
    public List<INode> GetNeighbours()
    {
        List<Territory> territories = territory.GetNeighbours();
        List<INode> nodes = new List<INode>();

        //Here the node neighbours is just every neighbour the territory has on the map
        foreach (Territory territory in territories)
        {
            if (territory.GetOwner() == this.territory.GetOwner()) { nodes.Add(new FriendlyNode().SetTerritory(territory)); }
        }
        return nodes;
    }

    /// <summary>
    /// Gets the position of the node, this is just the centre point of the territory. Used by the pathfinding algorithm to calculate heuristics.
    /// </summary>
    /// <returns>The world space position of the centre point of the territory.</returns>
    public Vector3 GetPosition()
    {
        return territory.GetCentrePoint();
    }

    /// <summary>
    /// Set the target territoy for this node.
    /// </summary>
    /// <param name="territory">The new target territory.</param>
    /// <returns>Returns this object, now with a new target territory. This is not a copy of the object.</returns>
    public FriendlyNode SetTerritory(Territory territory)
    {
        this.territory = territory;
        return this;
    }
}
