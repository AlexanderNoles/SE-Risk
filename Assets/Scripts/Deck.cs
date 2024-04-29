using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// <c>Deck</c> is a static class that contains a list of Cards. It has various methods to interact with the list of cards.
/// </summary>
public class Deck 
{
    static List<Card> cards = new List<Card>();
    static List<int> cardsTaken = new List<int>();

    /// <summary>
    /// <c>CreateDeck</c> intializes and creates a new deck based of the current territories on the current Map instance.
    /// </summary>
    public static void CreateDeck(int seed)
    {
        System.Random rand = new System.Random(seed);

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
        int territoryCount = Map.GetTerritories().Count;

        for (int i = 0; i < territoryCount; i++)
        {
            bool chosen = false;
            int choice = 0;
            while (!chosen)
            {
                choice = rand.Next(0,3);
                if (designCounts[choice] > 0)
                {
                    chosen = true;
                }
            }
            Card newCard = new Card(i, choice);
            cards.Add(newCard);
        }


        for(int i = 0;i < 10;i++)
        {
            cards.Add(new Card());
        }

        //Copy deck to original deck
        foreach (Card card in cards)
        {
            cardsTaken.Add(0);
        }
    }

    /// <summary>
    /// <c>Draw</c> removes a Card from the deck and returns it
    /// </summary>
    /// <returns>The drawn Card</returns>
    /// <exception cref="System.Exception">Thrown when the deck is empty. With a max of 6 players, as long as the game is running correctly, this should never happen.</exception>
    public static Card Draw()
    {
        Card toReturn;
        //Need to remove card from our end and then tell the server that
        //First we take the card but don't remove it
        //This is so a client can remove it when it gets a callback from the server
        int index = -1;
        int loopProtection = cards.Count;
        do
        {
            if (loopProtection == 0)
            {
                throw new Exception("Deck is empty!");
            }

            index = UnityEngine.Random.Range(0, cards.Count);
            toReturn = cards[index];

            loopProtection--;
        }
        while (index == -1 || cardsTaken[index] == 1);

        SetCardTaken(index, 1);

        return toReturn;
    }

    public static void SetCardTaken(int index, int newValue, bool makeRequest = true)
    {
        cardsTaken[index] = newValue;

        if (makeRequest && NetworkManagement.GetClientState() != NetworkManagement.ClientState.Offline)
        {
            NetworkConnection.UpdateCardTakenAcrossLobby(index, newValue);
        }
    }

    /// <summary>
    /// <c>ReturnToDeck</c> returns a Card to the deck.
    /// </summary>
    /// <param name="card">The Card being returned</param>
    public static void ReturnToDeck(Card card)
    {
        SetCardTaken(cards.IndexOf(card), 0);
    }
}
