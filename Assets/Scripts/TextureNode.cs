using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
/// <summary>
/// INode implementation used to pass data to the Pathfinding namespace. Used to navigate certain coloured pixels of a texture node. Used to assist with generating accurate polygonal representations of sprites.
/// </summary>
public class TextureNode : INode
{
    //The class for our nodes in the A* path finding algorithm
    Vector3 position;
    Texture2D texture;
    static Vector3[] pomniOffsets =
    {
        new Vector3(-1,-1),new Vector3(-1,0),new Vector3(-1,1),
        new Vector3(1,1),new Vector3(1,0),new Vector3(1,-1),
        new Vector3(0,-1),new Vector3(0,1),
    };
    static Vector3[] orthOffsets =
    {
       new Vector3(-1,0),new Vector3(1,0),
       new Vector3(0,-1),new Vector3(0,1)
    };
    static Vector3[] endOffsets =
    {
        new Vector3(0,1),new Vector3(-1,0)
    };

    /// <summary>
    /// Gets the neighbouring pixels aslong as they pass the neccesary colour checks.
    /// </summary>
    /// <returns>The list of neighbours.</returns>
    public List<INode> GetNeighbours()
    {
        //Returns a list of all orthoganoly neighbouring black pixels that are omnidirectionally neighbouring a white pixel
        List<INode> returnList = new List<INode>();
        foreach (Vector3 vector in orthOffsets)
        {
            Vector3 pixelPos = new Vector3(position.x + vector.x, position.y + vector.y);
            Color colour = texture.GetPixel((int)pixelPos.x, (int)pixelPos.y);
            if (colour != Color.white && colour != Color.clear)
            {
                if (CheckNeighbouringWhiteNodes(pixelPos))
                {
                    TextureNode newNode = new TextureNode();
                    newNode.texture = texture;
                    newNode.position = pixelPos;
                    returnList.Add(newNode);
                }
            }
        }
        return returnList;
    }
    /// <summary>
    /// Gets the end node based on the target texture, this is the pixel that will be pathfinded to.
    /// </summary>
    /// <returns>The generated end position. If none is found Vector3.zero is returned.</returns>
    public Vector3 GetEndNode()
    {
        //looks for a valid black pixel to the left of or above the start pixel, this is what we'll find a path to
        foreach (Vector3 vector in endOffsets)
        {
            Vector3 pixelPos = new Vector3(position.x + vector.x, position.y + vector.y);
            Color colour = texture.GetPixel((int)pixelPos.x, (int)pixelPos.y);
            if (colour != Color.white && colour != Color.clear)
            {
                if (CheckNeighbouringWhiteNodes(pixelPos))
                {
                    return pixelPos;
                }
            }
        }
        return Vector3.zero;
    }
    /// <summary>
    /// Checks if any neighbouring (in all orthagonal directions) are white, this helps with colour filtering in GetNeighbours().
    /// </summary>
    /// <param name="pos"></param>
    /// <returns>true or false</returns>
    public bool CheckNeighbouringWhiteNodes(Vector3 pos)
    {
        //looks at the neighbouring pixels in all 8 directions and returns true if any of those pixels are white
        foreach (Vector3 vector in pomniOffsets)
        {
            Vector3 pixelPos = new Vector3(pos.x + vector.x, pos.y + vector.y);
            if (texture.GetPixel((int)pixelPos.x, (int)pixelPos.y) == Color.white)
            {
                return true;
            }
        }
        return false;
    }
    /// <summary>
    /// Get the position of the node.
    /// </summary>
    /// <returns>The position as a Vector3.</returns>
    public Vector3 GetPosition()
    {
        return position;
    }

    /// <summary>
    /// Set the position of the node.
    /// </summary>
    /// <param name="position">The new position.</param>
    public void SetPosition(Vector3 position)
    {
        this.position = position;
    }
    /// <summary>
    /// Set the texture of the node.
    /// </summary>
    /// <param name="texture">The new texture</param>
    public void SetTexture(Texture2D texture)
    {
        this.texture = texture;
    }
}
