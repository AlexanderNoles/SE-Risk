using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <c>PolygonUtility</c> is a class that contains various static functions, utilized to help deal with 2D polygonal math.
/// </summary>
public class PolygonUtility
{
    /// <summary>
    /// Calculates if two line segements in 2D space intersect based on their end points.
    /// </summary>
    /// <param name="line1point1">The first point of line segment 1.</param>
    /// <param name="line1point2">The second point of line segment 1.</param>
    /// <param name="line2point1">The first point of line segment 2.</param>
    /// <param name="line2point2">The second point of line segment 2.</param>
    /// <returns>true or false</returns>
    public static bool DoLineSegmentsIntersect(Vector2 line1point1, Vector2 line1point2, Vector2 line2point1, Vector2 line2point2)
    {
        //This function was adapted from one written by user BAST42 on the unity forums
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

    /// <summary>
    /// Wrapper for DoLineSegementsIntersect that converts 3D points to 2D space.
    /// </summary>
    /// <param name="line1point1">The first point of line segment 1.</param>
    /// <param name="line1point2">The second point of line segment 1.</param>
    /// <param name="line2point1">The first point of line segment 2.</param>
    /// <param name="line2point2">The second point of line segment 2.</param>
    /// <returns>true or false</returns>
    public static bool DoLineSegmentsIntersect(Vector3 line1point1, Vector3 line1point2, Vector3 line2point1, Vector3 line2point2)
    {
        //This function was adapted from one written by user BAST42 on the unity forums
        bool toReturn = DoLineSegmentsIntersect(
            new Vector2(line1point1.x, line1point1.y),
            new Vector2(line1point2.x, line1point2.y),
            new Vector2(line2point1.x, line2point1.y),
            new Vector2(line2point2.x, line2point2.y)
            );

        return toReturn;
    }

    /// <summary>
    /// Calculates if a 3D world space position exists within a polygon. All positions z values are automatically converted to zero, essentially putting them in 2D space.
    /// </summary>
    /// <param name="polygon">The target polygon. Positions should be in world space.</param>
    /// <param name="worldSpacePos">The position to check. Should be in world space.</param>
    /// <returns>true or false</returns>
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
