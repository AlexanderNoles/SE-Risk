using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class CardDisplayer : MonoBehaviour
{
    Card[] cards;
    [SerializeField]
    List<Sprite> designSprites = new List<Sprite>();
    GameObject[] gameObjects = new GameObject[6];
    private bool showing = false;
    private bool hiding = false;
    private float showTime=1.0f;
    private float executionTime;
    private Vector3 startPos = new Vector3(-10000, 100, 0);
    [SerializeField]
    AnimationCurve ShowCurve;
    [SerializeField]
    AnimationCurve HideCurve;
    public void Start()
    {
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
    }

    public void Update()
    {
        if (showing||hiding)
        {
            float deltaTime = Time.deltaTime;
            executionTime += deltaTime;
            float completionRate = executionTime / showTime;

            for (int i =0;i< gameObjects.Length; i++)
            {
                GameObject go = gameObjects[i];
                if (executionTime < showTime)
                {
                    Vector3 endPos = Vector3.right * ((318 * (i))-(startPos.x+796));
                    if (showing) { go.transform.localPosition = startPos + (endPos * ShowCurve.Evaluate(completionRate)); }
                    else { go.transform.localPosition = (startPos + endPos) - (endPos * HideCurve.Evaluate(completionRate));}
                }
                else
                {
                    if (hiding) { go.SetActive(false); }
                    if (i == 5)
                    {
                        if (showing) { showing = false; }
                        else { hiding = false; }
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

    public void SetHand(Card[] cards)
    {
        this.cards = cards;
        for (int i = cards.Length-1; i < 6-cards.Length; i++)
        {
            Card card = new Card(null,3);
            this.cards[i] = card;
        }
    }
    [ContextMenu("Show Cards")]
    public void ShowCards()
    {
        foreach (GameObject go in gameObjects)
        {
            go.SetActive(true);
        }
        showing = true;
        executionTime = 0;
    }
    [ContextMenu("Show One Card")]
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
    [ContextMenu("HideCards")]
    public void HideCards()
    {
        hiding = true;
        executionTime = 0;
    }
}
