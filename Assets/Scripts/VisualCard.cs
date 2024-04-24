using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualCard : MonoBehaviour
{
    [SerializeField]
    int index;
    CardDisplayer cardDisplayer;

    public void Start()
    {
        cardDisplayer = FindObjectOfType<CardDisplayer>();
    }

    public void OnClick()
    {
        cardDisplayer.OnCardClick(index);
    }
}
