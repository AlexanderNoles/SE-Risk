using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Component placed on the individual card UI elements.
/// </summary>
public class VisualCard : MonoBehaviour
{
    [SerializeField]
    int index;
    CardDisplayer cardDisplayer;

    private void Start()
    {
        cardDisplayer = FindObjectOfType<CardDisplayer>();
    }

    /// <summary>
    /// Button function. runs on card click function on CardDisplayer.
    /// </summary>
    public void OnClick()
    {
        cardDisplayer.OnCardClick(index);
    }
}
