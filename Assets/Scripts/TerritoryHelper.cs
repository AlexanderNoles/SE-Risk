using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
public class TerritoryHelper : MonoBehaviour
{
    public Territory territory;
    private Texture2D texture = null;
    public Sprite spriteOverride = null;
    private float pixelsPerUnit;
    public bool UpdateTexture()
    {
        Sprite sprite = spriteOverride;
        if(sprite == null)
        {
            sprite = GetComponent<SpriteRenderer>().sprite;
        }
        pixelsPerUnit = sprite.pixelsPerUnit;
        return texture = sprite.texture;

    }
    public bool FindBlackPixel(out Vector2 pos)
    {
        if (UpdateTexture())
        {
            Color[] colour = texture.GetPixels();
            for (int i = 0; i < colour.Length; i++)
            {
                if (colour[i] == Color.white)
                {
                    int pixelIndex = i;
                    while (colour[pixelIndex]== Color.white)
                    {
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
        if (FindBlackPixel(out Vector2 pos))
        {
            TextureNode startNode = new TextureNode();
            startNode.SetPosition(pos);
            startNode.SetTexture(texture);
            TextureNode endNode = new TextureNode();
            endNode.SetPosition(startNode.GetEndNode());
            endNode.SetTexture(texture);
            List<INode> path = AStar.FindPath(startNode, endNode, true);
            path.Add(startNode);
            path = RemoveUselessBorderPoints(path);
            List<Vector3> vectorPath = ConvertNodePositionsToBorderPoints(path);
            territory.SetBorderPoints(vectorPath);
        }
    }
    public List<INode> RemoveUselessBorderPoints(List<INode> path)
    {
        for(int i = 0; i < path.Count-2; i++) 
        {
            Vector3 point1 = path[i].GetPosition();
            Vector3 point2 = path[i+2].GetPosition();
            Vector3 difference = point2 - point1;
            //Just to clarify, If I'm understanding this correctly
            //this check will remove a point if the direction from point[i] to point[i+2] is the same as the direction from
            //point[i] to point[i+1] right?
            //Essentially removing long strips of points?
            if(difference.x != 1 && difference.x != -1 && difference.y != -1 && difference.y != 1)
            {
                path.Remove(path[i + 1]);
                i--;
            }
        }
        return path;
    }
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
        int halfWidth = texture.width / 2;
        int halfHeight = texture.height / 2;
        Vector3 objectPos = transform.position;
        List<Vector3> borderPoints = new List<Vector3>();
        foreach (INode node in path)
        {
            Vector3 newPos = node.GetPosition();
            newPos.x -= halfWidth;
            newPos.y -= halfHeight;
            newPos /= pixelsPerUnit;
            newPos += objectPos;
            borderPoints.Add(newPos);
        }
        return borderPoints;
    }
    [ContextMenu("Find white pixel")]
    public void FindWhitePixel()
    {
        UpdateTexture();

        Color[] colour = texture.GetPixels();
        for (int i = 0; i < colour.Length; i++)
        {
            if (colour[i] == Color.white)
            {
                Debug.Log(IntArrayToVector2(i));
                return;
            }
        }
    }
    [ContextMenu("Find black pixel")]
    public void FindBlackPixel()
    {
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
                    Debug.Log(IntArrayToVector2(pixelIndex));
                    return;

                }
            }
        }
    }
}
#endif
