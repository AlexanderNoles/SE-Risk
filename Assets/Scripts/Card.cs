using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Card
{
    public enum cardDesign {Troop, Cavalry, Artillery,WildCard,Empty}
    Territory territory;
    cardDesign design;
    public Card(Territory territory, int design)
    {
        this.territory = territory;
        switch (design)
        {
            case 0: this.design=cardDesign.Troop; break;
            case 1: this.design = cardDesign.Cavalry; break;
            case 2: this.design = cardDesign.Artillery; break;
            case 3: this.design = cardDesign.Empty; break;
        }
    }
    public Card()
    {
        design = cardDesign.WildCard;
    }

    public Territory GetTerritory()
    {
        return territory;
    }

    public cardDesign GetDesign()
    {
        return design;
    }
    
}
