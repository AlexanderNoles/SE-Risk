using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class CardDisplayer : MonoBehaviour
{
    LocalPlayer player;
    PlayerInputHandler inputHandler;
    Card[] cards;
    [SerializeField]
    List<Sprite> designSprites = new List<Sprite>();
    GameObject[] gameObjects = new GameObject[6];
    List<Card> selected = new List<Card>();
    private bool showing = false;
    private bool hiding = false;
    private float showTime=1.0f;
    private float executionTime;
    private Vector3 startPos = new Vector3(-10000, 100, 0);
    [SerializeField]
    AnimationCurve ShowCurve;
    [SerializeField]
    AnimationCurve HideCurve;
    bool cardsOnScreen;
    bool AbleToTurnInCards;
    Camera m_Camera;
    bool slidingSelected = false;
    float timer;
    bool timing;
    public void Start()
    {
        AbleToTurnInCards = false;
        player = FindObjectOfType<LocalPlayer>();
        inputHandler = FindObjectOfType<PlayerInputHandler>();

        m_Camera = Camera.main;
        cards = new Card[6];
        for(int i=0; i<6; i++) 
        {
            Card card = new Card(null, 3);
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
        if (showing || hiding)
        {
            float deltaTime = Time.deltaTime;
            executionTime += deltaTime;
            float completionRate = executionTime / showTime;

            for (int i = 0; i < gameObjects.Length; i++)
            {
                GameObject go = gameObjects[i];
                if (executionTime < showTime)
                {
                    Vector3 endPos = Vector3.right * ((318 * (i)) - (startPos.x + 796));
                    if (showing) { go.transform.localPosition = startPos + (endPos * ShowCurve.Evaluate(completionRate)); }
                    else { go.transform.localPosition = (startPos + endPos) - (endPos * HideCurve.Evaluate(completionRate)); }
                }
                else
                {
                    if (hiding) { go.SetActive(false); }
                    if (i == 5)
                    {
                        if (showing)
                        {
                            showing = false; cardsOnScreen = true;
                        }
                        else { hiding = false; cardsOnScreen = false; }
                    }
                }
            }
        }
        else if (slidingSelected)
        {
            float deltaTime = Time.deltaTime;
            executionTime += deltaTime;
            float completionRate = executionTime / showTime;

            for (int i = 0; i < gameObjects.Length; i++)
            {
                if (selected.Contains(cards[i]))
                {
                    GameObject go = gameObjects[i];
                    if (executionTime < showTime)
                    {
                        Vector3 endPos = go.transform.position;
                        endPos.y = 5000;
                        go.transform.localPosition = new Vector3(endPos.x, (endPos.y * HideCurve.Evaluate(completionRate)), 0);
                    }
                    else
                    {
                        go.SetActive(false);
                        slidingSelected = false;
                    }
                }
            }
            if (executionTime >= showTime)
            {
                selected.Clear();
            }
            if (timing)
            {
                timer += Time.deltaTime;
                if (timer > 1)
                {
                    timing = false;
                    timer = 0;
                    for (int i = 0; i < gameObjects.Length; i++)
                    {
                        GameObject go = gameObjects[i];
                        go.GetComponent<Image>().color = Color.black;
                    }
                }
            }
        }
    }
    public void UpdateCardVisuals() 
    {
        for (int i=0;i<6;i++)
        {
            GameObject go = gameObjects[i];
            if (cards[i].GetDesign() != Card.cardDesign.Empty)
            {
                go.GetComponent<Image>().sprite = null;
                TextMeshProUGUI text = go.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>();
                Image design = go.transform.GetChild(2).gameObject.GetComponent<Image>();
                Image territoryImage = go.transform.GetChild(3).gameObject.GetComponent<Image>();
                text.SetText(cards[i].GetTerritory().name);
                territoryImage.sprite = cards[i].GetTerritory().getCardSprite();
                design.sprite = designSprites[(int)cards[i].GetDesign()];
                foreach (Transform child in go.GetComponentInChildren<Transform>())
                {
                    child.gameObject.SetActive(true);
                }
            }
            else
            {
                foreach (Transform child in go.GetComponentInChildren<Transform>())
                {
                    child.gameObject.SetActive(false);
                }
                go.GetComponent<Image>().sprite = designSprites[3];
            }
        }
    }

    public void SetCard(Card newCard)
    {
        cards[0] = newCard;
    }

    public void SetHand(Hand cards)
    {
        for (int i = 0; i < 6; i++)
        {
            if (i < cards.Count())
            {
                this.cards[i] = cards.GetCard(i);
            }
            else
            {
                Card card = new Card(null, 3);
                this.cards[i] = card;
            }
        }
    }
    public void ShowCards()
    {
        foreach (GameObject go in gameObjects)
        {
            go.SetActive(true);
        }
        showing = true;
        executionTime = 0;
    }
    public void ShowOneCard()
    {
        for(int i  = 1; i < gameObjects.Length; i++) 
        {
            gameObjects[i].SetActive(false);
        }
        gameObjects[0].SetActive(true);
        showing = true;
        executionTime = 0;
    }
    public void HideCards()
    {
        hiding = true;
        executionTime = 0;
        for(int i = 0;i < gameObjects.Length; i++)
        {
            if (selected.Contains<Card>(cards[i]))
            {
                gameObjects[i].GetComponent<Image>().color = Color.black;
                selected.Remove(cards[i]);
            }
        }
    }

    public int GetCardState()
    {
        //returns an integer signialing to current state of the on screen cards
        if (showing || hiding) { return -1; }
        else if (!cardsOnScreen) { return 0; }
        else { return 1; }
    }
    public void OnCardClick(int index)
    {
        if (cardsOnScreen && AbleToTurnInCards&& cards[index].GetDesign()!=Card.cardDesign.Empty)
        {
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
                    player.SetTroopCount(player.GetTroopCount()+Hand.NumberOfTroopsForSet(player,selected));
                    foreach(Card card in selected)
                    {
                        player.GetHand().RemoveCard(card);
                    }
                }
                else
                {
                    for (int i = 0; i < gameObjects.Length; i++)
                    {
                        if (selected.Contains(cards[i]))
                        {
                            GameObject go = gameObjects[i];
                            go.GetComponent<Image>().color = Color.red;
                            selected.Remove(cards[i]);
                            timing = true;
                        }
                    }
                }
            }
        }
    }
    public void SetAbleToTurnInCards(bool isAbleToTurnInCards)
    {
        AbleToTurnInCards = isAbleToTurnInCards;
    }


    public void ToggleCardMenuButton()
    {
        if (GetCardState() != -1)
        {
            if (GetCardState() == 0)
            {
                player.SetCardDisplayerHand();
                UpdateCardVisuals();
                ShowCards();
            }
            else
            {
                HideCards();
            }
        }
    }

    public bool GetCardsOnScreen()
    {
        return cardsOnScreen;
    }

}
