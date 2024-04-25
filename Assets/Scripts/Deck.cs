using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <c>Deck</c> is a static class that contains a list of Cards. It has various methods to interact with the list of cards.
/// </summary>
public class Deck 
{
    static List<Card> cards = new List<Card>();

    /// <summary>
    /// <c>CreateDeck</c> intializes and creates a new deck based of the current territories on the current Map instance.
    /// </summary>
    public static void CreateDeck()
    {
        cards.Clear();
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
                choice = Random.Range(0, 3); //change back to 3 when done testing
                if (designCounts[choice] > 0)
                {
                    chosen = true;
                }
            }
            Card newCard = new Card(t,choice);
            cards.Add(newCard);
        }

    }

    /// <summary>
    /// <c>Draw</c> removes a Card from the deck and returns it
    /// </summary>
    /// <returns>The drawn Card</returns>
    /// <exception cref="System.Exception">Thrown when the deck is empty. With a max of 6 players, as long as the game is running correctly, this should never happen.</exception>
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

    /// <summary>
    /// <c>ReturnToDeck</c> returns a Card to the deck.
    /// </summary>
    /// <param name="card">The Card being returned</param>
    public static void ReturnToDeck(Card card)
    {
        cards.Add(card);
    }
}
