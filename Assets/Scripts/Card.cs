using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// <c>Card</c> is the class that implements the games territory cards
/// </summary>
public class Card
{
    public enum cardDesign {Troop, Cavalry, Artillery,WildCard,Empty}
    int territoryIndex;
    cardDesign design;

    /// <summary>
    /// Creates a card with a specified territory associated with it
    /// </summary>
    /// <param name="territory">The territory the card corresponds to</param>
    /// <param name="design">The design on the card</param>
    public Card(int territoryIndex, int design)
    {
        this.territoryIndex = territoryIndex;
        switch (design)
        {
            case 0: this.design=cardDesign.Troop; break;
            case 1: this.design = cardDesign.Cavalry; break;
            case 2: this.design = cardDesign.Artillery; break;
            case 3: this.design = cardDesign.Empty; break;
        }
    }
    /// <summary>
    /// Creates a card with no territory associated with it, aka a wild card
    /// </summary>
    public Card()
    {
        design = cardDesign.WildCard;
    }
    /// <summary>
    /// Returns the territory associated with this card
    /// </summary>
    /// <returns>The territory associated with this card</returns>
    public int GetTerritory()
    {
        return territoryIndex;
    }
    /// <summary>
    /// Returns the design on this card
    /// </summary>
    /// <returns>The design on this card</returns>
    public cardDesign GetDesign()
    {
        return design;
    }
}
