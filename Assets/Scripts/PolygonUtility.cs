using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PolygonUtility
{
    public static bool DoLineSegmentsIntersect(Vector2 line1point1, Vector2 line1point2, Vector2 line2point1, Vector2 line2point2)
    {
        //This function was written by user BAST42 on the unity forums
        Vector2 a = line1point2 - line1point1;
        Vector2 b = line2point1 - line2point2;
        Vector2 c = line1point1 - line2point1;

        float alphaNumerator = b.y * c.x - b.x * c.y;
        float betaNumerator = a.x * c.y - a.y * c.x;
        float denominator = a.y * b.x - a.x * b.y;

        if (denominator == 0)
        {
            return false;
        }
        else if (denominator > 0)
        {
            if (alphaNumerator < 0 || alphaNumerator > denominator || betaNumerator < 0 || betaNumerator > denominator)
            {
                return false;
            }
        }
        else if (alphaNumerator > 0 || alphaNumerator < denominator || betaNumerator > 0 || betaNumerator < denominator)
        {
            return false;
        }

        return true;
    }

    public static bool DoLineSegmentsIntersect(Vector3 line1point1, Vector3 line1point2, Vector3 line2point1, Vector3 line2point2)
    {
        //This function was written by user BAST42 on the unity forums
        bool toReturn = DoLineSegmentsIntersect(
            new Vector2(line1point1.x, line1point1.y),
            new Vector2(line1point2.x, line1point2.y),
            new Vector2(line2point1.x, line2point1.y),
            new Vector2(line2point2.x, line2point2.y)
            );

        return toReturn;
    }

    public static bool DoesPointExistInsidePolygon(List<Vector3> polygon, Vector3 worldSpacePos)
    {
        Vector3 endPoint = worldSpacePos + (Vector3.left * 10000.0f);
        int totalIntersects = 0;
        for(int i = 0; i < polygon.Count-1; i++)
        {
            if (DoLineSegmentsIntersect(endPoint, worldSpacePos, polygon[i], polygon[i + 1]))
            {
                totalIntersects++;
            }
        }
        return totalIntersects % 2 != 0;

    }
}
