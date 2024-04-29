using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// <c>CardDisplayer</c> handles the visual showing of cards to the local player, as well as interaction with those cards
/// </summary>
public class CardDisplayer : MonoBehaviour
{
    LocalPlayer player;
    Card[] cards;
    [SerializeField]
    List<Sprite> designSprites = new List<Sprite>();
    GameObject[] gameObjects = new GameObject[10];
    List<Card> selected = new List<Card>();
    private bool showing = false;
    private bool hiding = false;
    private float showTime = 1.0f;
    private float executionTime;
    private Vector3 startPos = new Vector3(-10000, 100, 0);
    [SerializeField]
    AnimationCurve ShowCurve;
    [SerializeField]
    AnimationCurve HideCurve;
    [SerializeField]
    TextMeshProUGUI currentTurnInText;
    bool cardsOnScreen;
    bool AbleToTurnInCards;
    Camera m_Camera;
    bool slidingSelected = false;
    bool slidingTen = false;
    Vector3[] endPositions = new Vector3[10];
    bool tenCardsOnScreen = false;
    public void Start()
    {
        AbleToTurnInCards = false;
        player = FindObjectOfType<LocalPlayer>();

        m_Camera = Camera.main;
        cards = new Card[10];
        for(int i=0; i<cards.Count(); i++) 
        {
            Card card = new Card(-1, 3);
            cards[i] = card;
        }
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            gameObjects[i] = gameObject.transform.GetChild(i).gameObject;
        }
        cardsOnScreen = false;
    }

    public void Update()
    {
        UpdateTurnInText();
        if (showing || hiding)
        {
            float deltaTime = Time.deltaTime;
            executionTime += deltaTime;
            float completionRate = executionTime / showTime;

            for (int i = 0; i < (slidingTen?10:6); i++)
            {
                GameObject go = gameObjects[i];
                if (executionTime < showTime)
                {
                    if (showing) { go.transform.localPosition = startPos + (endPositions[i] * ShowCurve.Evaluate(completionRate)); }
                    else { go.transform.localPosition = (startPos + endPositions[i]) - (endPositions[i] * HideCurve.Evaluate(completionRate)); }
                }
                else
                {
                    if (hiding) {go.SetActive(false); }
                    if (i == (slidingTen?9:5))
                    {
                        if (showing)
                        {
                            showing = false; cardsOnScreen = true;
                        }
                        else { hiding = false; cardsOnScreen = false; }
                        slidingTen = false;
                    }
                }
            }
        }
        else if (slidingSelected)
        {
            float deltaTime = Time.deltaTime;
            executionTime += deltaTime;
            float completionRate = executionTime / showTime;

            for (int i = 0; i < 10; i++)
            {
                if (selected.Contains(cards[i]))
                {
                    GameObject go = gameObjects[i];
                    if (executionTime < showTime)
                    {
                        go.transform.localPosition = new Vector3(endPositions[i].x + startPos.x, startPos.y + (5000 * HideCurve.Evaluate(completionRate)), 0);
                    }
                    else
                    {
                        go.GetComponent<Image>().color = Color.black;
                        go.SetActive(false);
                    }
                }
                if (executionTime >= showTime)
                {
                    if (i == 9 && tenCardsOnScreen && player.GetHand().Count() < 5)
                    {
                        HideCards(true);
                    }
                }
            }
            if (executionTime >= showTime)
            {
                slidingSelected = false;
                selected.Clear();
            }
        }
    }
    /// <summary>
    /// Updates the visual assets of the cards to match the cards currently in the card displayer
    /// </summary>
    public void UpdateCardVisuals() 
    {
        for (int i=0;i<cards.Length;i++)
        {
            GameObject go = gameObjects[i];
            if (cards[i].GetDesign() != Card.cardDesign.Empty)
            {
                go.GetComponent<Image>().sprite = null;
                TextMeshProUGUI text = go.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>();
                Image design = go.transform.GetChild(2).gameObject.GetComponent<Image>();
                Image territoryImage = go.transform.GetChild(3).gameObject.GetComponent<Image>();
                Image cardBody = go.transform.GetChild(0).gameObject.GetComponent<Image>();
                if (cards[i].GetDesign() == Card.cardDesign.WildCard)
                {
                    cardBody.gameObject.SetActive(true);
                    design.gameObject.SetActive(false);
                    text.gameObject.SetActive(false);
                    territoryImage.gameObject.SetActive(false);
                    cardBody.sprite = design.sprite = designSprites[4];
                }
                else
                {
                    cardBody.sprite = null;
                    text.SetText(Map.GetTerritory(cards[i].GetTerritory()).name);
                    territoryImage.sprite = Map.GetTerritory(cards[i].GetTerritory()).getCardSprite();
                    design.sprite = designSprites[(int)cards[i].GetDesign()];
                    foreach (Transform child in go.GetComponentInChildren<Transform>())
                    {
                        child.gameObject.SetActive(true);
                    }
                }
            }
            else if(cards[i].GetDesign() == Card.cardDesign.Empty)
            {
                foreach (Transform child in go.GetComponentInChildren<Transform>())
                {
                    child.gameObject.SetActive(false);
                }
                go.GetComponent<Image>().sprite = designSprites[3];
            }
        }
    }

    /// <summary>
    /// Sets just the first card in the displayer
    /// </summary>
    /// <param name="newCard">The card to be displayer in slot 1</param>
    public void SetCard(Card newCard)
    {
        cards[0] = newCard;
    }

    /// <summary>
    /// Updates the full set of cards to match the cards in a passed hand
    /// </summary>
    /// <param name="cards">The hand to match to</param>
    public void SetHand(Hand cards)
    {
        for (int i = 0; i < this.cards.Count(); i++)
        {
            if (i < cards.Count())
            {
                this.cards[i] = cards.GetCard(i);
            }
            else
            {
                Card card = new Card(-1, 3);
                this.cards[i] = card;
            }
        }
    }
    /// <summary>
    /// Makes the cards visible to the player and moves them on screen
    /// </summary>
    /// <param name="slidingTen">Set true to show all 10 cards, false to display 6</param>
    public void ShowCards(bool slidingTen)
    {
        this.slidingTen = slidingTen;
        tenCardsOnScreen = slidingTen;
        for(int i  = 0; i < (slidingTen?10:6); i++)
        {
            GameObject go = gameObjects[i];
            go.SetActive(true);
        }
        showing = true;
        executionTime = 0;
        CalculateEndPositions();
    }
    /// <summary>
    /// Makes just the first card in the displayer visible to the player
    /// </summary>
    public void ShowOneCard()
    {
        for(int i  = 1; i < gameObjects.Length; i++) 
        {
            gameObjects[i].SetActive(false);
        }
        gameObjects[0].SetActive(true);
        showing = true;
        executionTime = 0;
        CalculateEndPositions();
    }
    /// <summary>
    /// Hides all cards on the screen
    /// </summary>
    /// <param name="slidingTen">True if there are 10 cards on screen, else false</param>
    public void HideCards(bool slidingTen)
    {
        hiding = true;
        executionTime = 0;
        this.slidingTen = slidingTen;
        tenCardsOnScreen = false;
        for (int i = 0;i < gameObjects.Length; i++)
        {
            if (selected.Contains<Card>(cards[i]))
            {
                gameObjects[i].GetComponent<Image>().color = Color.black;
                selected.Remove(cards[i]);
            }
        }
        CalculateEndPositions();
    }
    /// <summary>
    /// Returns the current state of the cards on screen
    /// </summary>
    /// <returns>1 if cards are on screen, 0 if they are not, -1 if the cards are in a different state, such as moving from being on screen to off screen</returns>
    public int GetCardState()
    {
        //returns an integer signialing to current state of the on screen cards
        if (showing || hiding) { return -1; }
        else if (!cardsOnScreen) { return 0; }
        else { return 1; }
    }
    /// <summary>
    /// Handles the functions performed when a card is clicked, such as selecting that card, or turning it in if 3 are selected
    /// </summary>
    /// <param name="index">The index of the card that has been clicked</param>
    public void OnCardClick(int index)
    {
        if (cardsOnScreen && AbleToTurnInCards&& cards[index].GetDesign()!=Card.cardDesign.Empty)
        {
            if ((player.GetTurnReset() && !tenCardsOnScreen))
            {
                foreach(GameObject go in gameObjects)
                {
                    Flasher.Flash(Color.red,1f,go);
                }
            }
            else
            {
                AudioManagement.PlaySound("Card Select");
                if (selected.Contains<Card>(cards[index]))
                {
                    gameObjects[index].GetComponent<Image>().color = Color.black;
                    selected.Remove(cards[index]);
                }
                else
                {
                    gameObjects[index].GetComponent<Image>().color = Color.green;
                    selected.Add(cards[index]);
                }
                if (selected.Count == 3)
                {
                    if (Hand.IsArrayAValidSet(selected))
                    {
                        slidingSelected = true;
                        executionTime = 0;
                        CalculateEndPositions();
                        foreach (Card card in selected)
                        {
                            player.GetHand().RemoveCard(card);
                        }
                        player.SetTroopCount(player.GetTroopCount() + Hand.NumberOfTroopsForSet(player.GetIndex(), selected));
                        Hand.IncrementTurnInCount();
                    }
                    else
                    {
                        for (int i = 0; i < gameObjects.Length; i++)
                        {
                            if (selected.Contains(cards[i]))
                            {
                                GameObject go = gameObjects[i];
                                go.GetComponent<Image>().color = Color.black;
                                selected.Remove(cards[i]);
                                Flasher.Flash(Color.red, 1f, go);
                            }
                        }
                    }
                }
            }
        }
    }
    /// <summary>
    /// Sets isAbleToTurnInCards
    /// </summary>
    /// <param name="isAbleToTurnInCards">The new value of isAbleToTurnInCards</param>
    public void SetAbleToTurnInCards(bool isAbleToTurnInCards)
    {
        AbleToTurnInCards = isAbleToTurnInCards;
    }

    /// <summary>
    /// Handles the function of the on screen card menu button, such as displaying the current held cards when pressed
    /// </summary>
    public void ToggleCardMenuButton()
    {
        if (GetCardState() != -1 && tenCardsOnScreen==false)
        {
            if (GetCardState() == 0)
            {
                SetHand(player.GetHand());
                UpdateCardVisuals();
                ShowCards(false);
            }
            else
            {
                HideCards(false);
            }
        }
    }
    /// <summary>
    /// Returns whether or not there are currently cards being displayed on screen
    /// </summary>
    /// <returns>True when cards are on screen, else false</returns>
    public bool GetCardsOnScreen()
    {
        return cardsOnScreen;
    }

    /// <summary>
    /// Calculates the positions that each card will have on screen, when displayed
    /// </summary>
    private void CalculateEndPositions()
    {
        for (int i = 0; i < (slidingTen ? 10 : 6); i++)
        {
            Vector3 endPos = Vector3.right * ((318 * (i < 6 ? i : i - 4)) - (startPos.x + 796));
            if (slidingTen)
            {
                if (i >= 2 && i < 6)
                {
                    endPos += Vector3.up * 150;
                }
                else if (i >= 6 && i < 10)
                {
                    endPos += Vector3.up * -370;
                }
            }
            endPositions[i] = endPos;   
        }
    }
    /// <summary>
    /// Updates the value displayed on the troops for card turn in counter
    /// </summary>
    public void UpdateTurnInText()
    {
        currentTurnInText.text = Hand.CalculateSetWorth().ToString();
    }
}
