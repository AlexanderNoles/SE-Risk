using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestClass : MonoBehaviour
{
    public List<Vector3> polygon;

    public void Update()
    {
        Debug.Log(PolygonUtility.DoesPointExistInsidePolygon(polygon, transform.position));
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        for(int i = 0; i < polygon.Count-1; i++)
        {
            Gizmos.DrawLine(polygon[i], polygon[i+1]);
        }
    }
}
