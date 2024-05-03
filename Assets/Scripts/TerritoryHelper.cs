using Pathfinding;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Editor only class used to pre-generate data about territories.
/// </summary>
public class TerritoryHelper : MonoBehaviour
{
#if UNITY_EDITOR
    public Territory territory;
    private Texture2D texture = null;
    public Sprite spriteOverride = null;
    private float pixelsPerUnit;
    private int halfWidth;
    private int halfHeight;
    public bool UpdateTexture()
    {
        Sprite sprite = spriteOverride;
        if(sprite == null)
        {
            sprite = GetComponent<SpriteRenderer>().sprite;
        }
        pixelsPerUnit = sprite.pixelsPerUnit;
        if(texture = sprite.texture)
        {
            halfHeight = texture.height / 2;
            halfWidth = texture.width / 2;
            return true;
        }
        else
        {
            return false;
        }
    }
    public bool FindBlackPixel(out Vector2 pos)
    {
        //used to find the start pixel for our A* pathfinding 
        if (UpdateTexture())
        {
            Color[] colour = texture.GetPixels();
            for (int i = 0; i < colour.Length; i++)
            {
                //searches the texture for a valid white pixel
                if (colour[i] == Color.white)
                {
                    int pixelIndex = i;
                    while (colour[pixelIndex]== Color.white)
                    {
                        //looks vertically from the white pixel till it finds a black pixel
                        pixelIndex += texture.width;
                    }
                    pos = IntArrayToVector2(pixelIndex);
                    return true;

                }
            }
        }
        pos = Vector2.zero;
        return false;
    }
    [ContextMenu("Update Border Points")]
    public void UpdateBorderPoints()
    {
        //converts the texture into a list of vectors that indicate a polygon with the shape and position of the territory
        if (FindBlackPixel(out Vector2 pos))
        {
            //finds the start pixel and returns its position
            TextureNode startNode = new TextureNode();
            startNode.SetPosition(pos);
            startNode.SetTexture(texture);
            TextureNode endNode = new TextureNode();
            endNode.SetPosition(startNode.GetEndNode()); // uses the start node to find the end node
            endNode.SetTexture(texture);
            List<INode> path = AStar.FindPath(startNode, endNode, true); // finds a clockwise path from the start node to the end node
            path.Add(startNode); // adds the start node to the end of the list to close the circular path
            territory.SetCentrePoint(CalculateCentrePoint(path));
            path = RemoveUselessBorderPoints(path);
            List<Vector3> vectorPath = ConvertNodePositionsToBorderPoints(path);
            territory.SetBorderPoints(vectorPath);
        }
    }

    public Vector3 CalculateCentrePoint(List<INode> path)
    {
        //averages the positions of the border points to find the central point
        Vector3 centrePoint = Vector3.zero;
        for(int i=0; i<path.Count; i++)
        {
            centrePoint += path[i].GetPosition();
        }
        centrePoint/=path.Count;
        return ConvertNodePositionToBorderPoint(centrePoint,transform.position);
    }
    public List<INode> RemoveUselessBorderPoints(List<INode> path)
    {
        //finds points that lie on lines between two other points are removes them, as they add no detail to our border
        for(int i = 0; i < path.Count-2; i++) 
        {
            Vector3 point1 = path[i].GetPosition();
            Vector3 point2 = path[i+2].GetPosition();
            Vector3 difference = point2 - point1;
            if(difference.x != 1 && difference.x != -1 && difference.y != -1 && difference.y != 1)
            {
                path.Remove(path[i + 1]);
                i--;
            }
        }
        return path;
    }
    //The next 3 functions all convert between various different data types used in our pathfinding and border creation algorithms
    public Vector2 IntArrayToVector2(int pos)
    {  
        Vector2 vector = new Vector2();
        float height = Mathf.Floor(pos/texture.width);
        float width = pos-(height*texture.width);
        vector.x = width;
        vector.y = height;
        return vector;
    }
    
    public List<Vector3> ConvertNodePositionsToBorderPoints(List<INode> path)
    {
        Vector3 objectPos = transform.position;
        List<Vector3> borderPoints = new List<Vector3>();
        foreach (INode node in path)
        {
            borderPoints.Add(ConvertNodePositionToBorderPoint(node.GetPosition(),objectPos));
        }
        return borderPoints;
    }
    public Vector3 ConvertNodePositionToBorderPoint(Vector3 pos, Vector3 objectPos)
    {
  
        pos.x -= halfWidth;
        pos.y -= halfHeight;
        pos /= pixelsPerUnit;
        pos += objectPos;
        return pos;
    }
    [ContextMenu("Find white pixel")]
    public void FindWhitePixel()
    {
        //iterates through the texture till it finds the first solid white pixel. Only used for testing.
        UpdateTexture();

        Color[] colour = texture.GetPixels();
        for (int i = 0; i < colour.Length; i++)
        {
            if (colour[i] == Color.white)
            {
                return;
            }
        }
    }
    [ContextMenu("Find black pixel")]
    public void FindBlackPixel()
    {
        //A version of find black pixel that can be run from the context menu, used for testing
        if (UpdateTexture())
        {
            Color[] colour = texture.GetPixels();
            for (int i = 0; i < colour.Length; i++)
            {
                if (colour[i] == Color.white)
                {
                    int pixelIndex = i;
                    while (colour[pixelIndex] == Color.white)
                    {
                        pixelIndex += texture.width;
                    }
                    return;

                }
            }
        }
    }
#endif
}
