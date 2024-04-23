using System.Collections;
using System.Collections.Generic;
using UnityEngine;

static public class Deck 
{
    static List<Card> cards = new List<Card>();
    static public void CreateDeck()
    {
        List<Territory> territories = Map.GetTerritories();
        List<int> designCounts = new List<int>();
        int count = territories.Count;
        for(int i = 0; i < 3; i++)
        {
            designCounts.Add(0);
        }
        int index = 0;
        while (count > 0)
        {
            designCounts[index]++;
            index++;
            if (index > 2) { index = 0; }
            count--;
        }
        foreach(Territory t in territories)
        {
            bool chosen = false;
            int choice = 0;
            while(!chosen)
            {
                choice = Random.Range(0, 2);
                if (designCounts[choice] > 0)
                {
                    chosen = true;
                }
            }
            Card newCard = new Card(t,choice);
            cards.Add(newCard);
        }

    }

    public static Card Draw()
    {
        if (cards.Count == 0)
        {
            throw new System.Exception("Deck is empty!");
        }

        int count = Random.Range(0, cards.Count);
        Card card = cards[count];
        cards.Remove(card);
        return card;
    }

    public static void ReturnToDeck(Card card)
    {
        cards.Add(card);
    }
}
