using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Hand
{
    static int setsTurnedIn;

    public Hand()
    {
        list = new List<Card>();
    }

    public void AddCard(Card card)
    {
        list.Add(card);
    }

    public void RemoveCard(Card card) 
    {
        list.Remove(card);

        Deck.ReturnToDeck(card);
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
        setsTurnedIn++;
    }
    List<Card> list;
    public static int NumberOfTroopsForSet(Player player, List<Card> set)
    {
        PlayerInfoHandler.UpdateInfo();
        foreach(Card card in set)
        {
            if(card.GetTerritory().GetOwner() == player)
            {
                card.GetTerritory().SetCurrentTroops(card.GetTerritory().GetCurrentTroops()+2);
                break;
            }
        }
        return CalculateSetWorth();

    }
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
        else
        {
            return false;
        }
    }

    public static void ResetSetNumberWorth()
    {
        setsTurnedIn = 0;
    }

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
