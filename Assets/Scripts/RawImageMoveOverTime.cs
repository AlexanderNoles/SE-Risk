using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// <c>RawImageMoveOverTime</c> is a monobehaviour that is added to gameobjects. It will then apply a move over time effect to the first RawImage it finds on said gameobject.
/// </summary>
public class RawImageMoveOverTime : MonoBehaviour
{
    public float speed = 1f;
    public Vector2 direction = Vector2.right;
    private RawImage target;

    void Start()
    {
        target = GetComponent<RawImage>();
    }

    void Update()
    {
        Rect rect = target.uvRect;

        //Negative direction as position here represents offset
        rect.position = -direction.normalized * (Time.time * speed);

        target.uvRect = rect;
    }
}
