using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Represents the hand of a given player. Includes a list of their cards.
/// </summary>
public class Hand
{
    static int setsTurnedIn;

    public static void SetSetsTurnedIn(int newValue, bool makeRequest = true)
    {
        setsTurnedIn = newValue;

        if (makeRequest && NetworkManagement.GetClientState() != NetworkManagement.ClientState.Offline)
        {
            NetworkConnection.UpdateSetsTurnedInAcrossLobby(newValue);
        }
    }

    List<Card> list;
    private int playerIndex;

    public Hand(int playerIndex)
    {
        list = new List<Card>();
        this.playerIndex = playerIndex;
    }

    public void AddCard(Card card)
    {
        list.Add(card);
    }

    public void RemoveCard(Card card) 
    {
        list.Remove(card);

        Deck.ReturnToDeck(card, playerIndex);
    }

    public void RemoveAll()
    {
        foreach (Card card in list)
        {
            Deck.ReturnToDeck(card, playerIndex);
        }

        list.Clear();
    }

    public Card GetCard(int index)
    {
        return list[index];
    }

    public Card GetLastCard()
    {
        return list[^1];
    }

    public int Count()
    {
        return list.Count;
    }

    public static void IncrementTurnInCount()
    {
        SetSetsTurnedIn(setsTurnedIn + 1);
    }
    /// <summary>
    /// Returns the number of troops a player gets from the set they are turning in
    /// </summary>
    /// <param name="player">The player turning in a set</param>
    /// <param name="set">The set being turned in</param>
    /// <returns>The number of troops the player can get</returns>
    public static int NumberOfTroopsForSet(int player, List<Card> set)
    {
        foreach(Card card in set)
        {
            if(card.GetDesign()!=Card.cardDesign.WildCard&& Map.GetTerritory(card.GetTerritory()).GetOwner() == player)
            {
                Map.GetTerritory(card.GetTerritory()).SetCurrentTroops(Map.GetTerritory(card.GetTerritory()).GetCurrentTroops()+2);
                break;
            }
        }
        return CalculateSetWorth();

    }
    /// <summary>
    /// Checks to see if this hand has a valid set of cards in it
    /// </summary>
    /// <param name="validSet">Output variable of the valid set, empty if none exists</param>
    /// <param name="autoRemoveListFromHand">Whether or not to remove the list </param>
    /// <returns>True if this hand has a valid set, false elsewise </returns>
    public bool FindValidSet(out List<Card> validSet, bool autoRemoveListFromHand = false)
    {
        for (int i = 0; i < list.Count; i++)
        {
            for (int j = 0; j < list.Count; j++)
            {
                for(int k = 0; k < list.Count; k++)
                {
                    List<Card> set = new List<Card>()
                    {
                        list[i],
                        list[j],
                        list[k]
                    };

                    if (i!=j&&i!=k&&j!=k&&IsArrayAValidSet(set))
                    {
                        validSet = set;

                        if (autoRemoveListFromHand)
                        {
                            foreach (Card card in set)
                            {
                                RemoveCard(card);
                            }
                        }

                        return true;
                    }
                }
            }
        }

        validSet = new List<Card>();
        return false;
    }
    /// <summary>
    /// Checks to see if a given set of cards is valid to turn in
    /// </summary>
    /// <param name="cardArray">The card array to check validity for</param>
    /// <returns>Whether or not the array is valid</returns>
    public static bool IsArrayAValidSet(List<Card> cardArray)
    {
        if (cardArray.Count < 3) { return false; }

        if (cardArray[0].GetDesign() != cardArray[1].GetDesign() && cardArray[0].GetDesign() != cardArray[2].GetDesign() && cardArray[1].GetDesign() != cardArray[2].GetDesign())
        {
            return true;
        }
        else if (cardArray[0].GetDesign() == cardArray[1].GetDesign() && cardArray[0].GetDesign() == cardArray[2].GetDesign())
        {
            return true;
        }
        else if ((cardArray[0].GetDesign() == Card.cardDesign.WildCard)||
                (cardArray[1].GetDesign() == Card.cardDesign.WildCard)||
                (cardArray[2].GetDesign() == Card.cardDesign.WildCard))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public static void ResetSetNumberWorth()
    {
        SetSetsTurnedIn(0);
    }

    /// <summary>
    /// Calculates the number of troops you get for turning in cards
    /// </summary>
    /// <returns>The number of troops you get for turning in</returns>
    public static int CalculateSetWorth()
    {
        if(setsTurnedIn < 6) 
        {
            return 4+(2*setsTurnedIn);
        }
        else
        {
            return 15+(5*(setsTurnedIn-6));
        }
    }

    public void DebugHand()
    {
        foreach(Card card in list)
        {
            Debug.Log(card.GetDesign());
        }
    }
}
