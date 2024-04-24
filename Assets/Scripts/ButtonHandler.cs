using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

public class ButtonHandler : MonoBehaviour
{
    CardDisplayer cardDisplayer;
    LocalPlayer player;

    public void Start()
    {
        cardDisplayer = GetComponent<CardDisplayer>();
        player = FindObjectOfType<LocalPlayer>();
    }
    public void WhenClicked()
    {
        if (cardDisplayer.GetCardState() != -1)
        {
            if (cardDisplayer.GetCardState() == 0)
            {
                player.SetCardDisplayerHand();
                cardDisplayer.UpdateCardVisuals();
                cardDisplayer.ShowCards();
            }
            else{
                cardDisplayer.HideCards();
            }
        }
    }
}
