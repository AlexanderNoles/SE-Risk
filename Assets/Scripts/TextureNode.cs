using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
public class TextureNode : INode
{
    Vector3 position;
    Texture2D texture;
    static Vector3[] pomniOffsets =
    {
        new Vector3(-1,-1),new Vector3(-1,0),new Vector3(-1,1),
        new Vector3(1,1),new Vector3(1,0),new Vector3(1,-1),
        new Vector3(0,-1),new Vector3(0,1)
    };
    static Vector3[] orthOffsets =
    {
        new Vector3(-1,0),new Vector3(1,0),
        new Vector3(0,-1),new Vector3(0,1)
    };
    static Vector3[] endOffsets =
    {
        new Vector3(0,1),new Vector3(-1,-1),
        new Vector3(-1,0),new Vector3(-1,1)
    };
    public List<INode> GetNeighbours()
    {
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
    public Vector3 GetEndNode()
    {
        
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
    public bool CheckNeighbouringWhiteNodes(Vector3 pos)
    {
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
    public Vector3 GetPosition()
    {
        return position;
    }
    public void SetPosition(Vector3 position)
    {
        this.position = position;
    }
    public void SetTexture(Texture2D texture)
    {
        this.texture = texture;
    }
}
