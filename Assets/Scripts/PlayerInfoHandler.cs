using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the UI elements that display information about the games players
/// </summary>
public class PlayerInfoHandler : MonoBehaviour
{
    static List<Player> players = new List<Player>();

    private void Awake()
    {
        GetUIElements();
    }
    /// <summary>
    /// Sets the list of players to display through the UI
    /// </summary>
    /// <param name="newPlayers">The list of players to display</param>
    public void SetPlayers(List<Player> newPlayers)
    {
        players = newPlayers;
        UpdateColours();
        ResetInfos();

        UpdateInfo();
    }

    private void GetUIElements()
    {
        infoFronts = new List<Image>();
        textBackers = new List<Image>();
        infoTexts = new List<TextMeshProUGUI>();
        crosses = new List<GameObject>();
        backers = new List<GameObject>();

        for (int i = 0; i < 6; i++)
        {
            Transform info = transform.GetChild(i);
            backers.Add(info.gameObject);
            infoFronts.Add(info.GetChild(0).GetComponent<Image>());
            textBackers.Add(infoFronts[^1].transform.GetChild(1).GetComponent<Image>());
            infoTexts.Add(textBackers[^1].transform.GetChild(0).GetComponent<TextMeshProUGUI>());
            crosses.Add(info.GetChild(1).gameObject);
            crosses[^1].SetActive(false);
        }
    }

    static List<Image> infoFronts;
    static List<Image> textBackers;
    static List<TextMeshProUGUI> infoTexts;
    static List<GameObject> crosses;
    static List<GameObject> backers;
    public void UpdateColours()
    {
        for (int i = 0; i<6; i++)
        {
            if (i < players.Count)
            {
                infoFronts[i].color = players[i].GetColor();
                textBackers[i].color = players[i].GetColor();
            }
            else
            {
                backers[i].gameObject.SetActive(false);
            }
        }
    }

    private void ResetInfos()
    {
        foreach (GameObject cross in crosses)
        {
            cross.SetActive(false);
        }
    }

    public static void UpdateInfo()
    {
        int j = 0;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].IsDead())
            {
                crosses[j].SetActive(true);
            }
            else if(!crosses[j].activeSelf)
            {
                infoTexts[j].text = players[i].GetHand().Count().ToString();
            }
            else
            {
                i--;
            }
            j++;
        }
    }
}
