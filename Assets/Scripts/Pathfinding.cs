using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Pathfinding
{
    public interface INode
    {
        public Vector3 GetPosition();
        public List<INode> GetNeighbours();
    }

    public static class AStar
    {
        private class AStarNode
        {
            public INode node;
            public float g, h, f;

            public AStarNode(INode node, AStarNode previousNode, float g, INode endNode)
            {
                this.node = node;
                this.g = g;
                h = Distance(node, endNode);
                f = h + g;

                this.previousNode = previousNode;
            }

            public AStarNode previousNode;
        }

        private static float Distance(INode a, INode b)
        {
            return Mathf.Abs(a.GetPosition().x - b.GetPosition().x) + Mathf.Abs(a.GetPosition().z - b.GetPosition().z);
        }

        public static List<INode> FindPath(INode startNode, INode endNode, bool circular)
        {
            List<INode> toReturn = new List<INode>();

            {
                //Reverse the end and start nodes so we don't have to reverse the final path when we move back from the end
                INode temp = startNode;
                startNode = endNode;
                endNode = temp;
            }
                List<AStarNode> closed = new List<AStarNode>();
                List<AStarNode> frontier = new List<AStarNode>(){
                new AStarNode(startNode, null, 0, endNode)
                };
            

            while (frontier.Count > 0)
            {
                AStarNode current = GetNextNode(frontier);

                if (current.node.GetPosition() == endNode.GetPosition()) 
                {
                    //We have found our path, we can just get a reverse path
                    while (current != null)
                    {
                        toReturn.Add(current.node);
                        current = current.previousNode;
                    }

                    break;
                }

                frontier.Remove(current);
                closed.Add(current);

                foreach (INode neighbour in current.node.GetNeighbours())
                {
                    //Add all the neighbours to the frontier
                    if (Contains(closed, neighbour))
                    {
                        continue;
                    }

                    AStarNode newNode = new AStarNode(
                        neighbour,
                        current,
                        current.g + Distance(current.node, neighbour),
                        endNode);

                    if (Contains(frontier, neighbour, out AStarNode outNode))
                    {
                        if (newNode.g > outNode.g)
                        {
                            continue;
                        }
                    }
                    if(!(circular && closed.Count == 1 && newNode.node.GetPosition() == endNode.GetPosition()))
                    {
                        frontier.Add(newNode);
                    }
                }
            }

            return toReturn;
        }

        private static AStarNode GetNextNode(List<AStarNode> nodes)
        {
            AStarNode toReturn = nodes[0];

            foreach (AStarNode node in nodes)
            {
                if (toReturn.f > node.f)
                {
                    toReturn = node;
                }
            }

            return toReturn;
        }

        private static bool Contains(List<AStarNode> nodes, INode checkNode, out AStarNode outNode)
        {
            outNode = null;

            foreach (AStarNode node in nodes)
            {
                if (node.node.GetPosition() == checkNode.GetPosition())
                {
                    outNode = node;
                    return true;
                }
            }

            return false;
        }

        private static bool Contains(List<AStarNode> nodes, INode checkNode)
        {
            foreach (AStarNode node in nodes)
            {
                if (node.node.GetPosition() == checkNode.GetPosition())
                {
                    return true;
                }
            }

            return false;
        }
    }
}
