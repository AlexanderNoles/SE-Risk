using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        rect.position = -direction.normalized * (Time.time * speed);

        target.uvRect = rect;
    }
}
