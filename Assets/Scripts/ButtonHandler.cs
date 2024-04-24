using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

public class ButtonHandler : MonoBehaviour
{
    CardDisplayer cardDisplayer;
    LocalPlayer player;
    PlayerInputHandler inputHandler;
    public void Start()
    {
        cardDisplayer = GetComponent<CardDisplayer>();
        player = FindObjectOfType<LocalPlayer>();
        inputHandler = FindObjectOfType<PlayerInputHandler>();
    }
    public void WhenClicked()
    {
        if (cardDisplayer.GetCardState() != -1)
        {
            inputHandler.ToggleCardView();
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
