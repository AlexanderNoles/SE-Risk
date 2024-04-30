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
    static PlayerInfoHandler instance;

    public static bool Initialized()
    {
        return instance != null;
    }

    static List<int> originalPlayers = new List<int>();
    static Dictionary<int, int> indexToHandCounts = new Dictionary<int, int>();
    static int previousPlayerTurnIndex;
    static int currentPlayerTurnIndex = -1;
    static float effectT;

    private const float atRestXPos = -25;
    private const float activeXPos = -120;

    public static void SetCurrentPlayerTurnIndex(int newIndex, bool makeRequest = true)
    {
        if (!makeRequest || NetworkManagement.GetClientState() == NetworkManagement.ClientState.Offline)
        {
            if (previousPlayerTurnIndex != -1)
            {
                actualInfos[previousPlayerTurnIndex].anchoredPosition = new Vector2(atRestXPos, actualInfos[previousPlayerTurnIndex].anchoredPosition.y);
            }

            previousPlayerTurnIndex = currentPlayerTurnIndex;
            currentPlayerTurnIndex = newIndex;

            if (previousPlayerTurnIndex != -1)
            {
                actualInfos[previousPlayerTurnIndex].anchoredPosition = new Vector2(activeXPos, actualInfos[previousPlayerTurnIndex].anchoredPosition.y);
            }

            actualInfos[currentPlayerTurnIndex].anchoredPosition = new Vector2(atRestXPos, actualInfos[currentPlayerTurnIndex].anchoredPosition.y);

            effectT = 1.0f;
        }
        else
        {
            //Send request to update on all clients
            //We do it this way so previousPlayerTurnIndex doesn't get screwed up
            NetworkConnection.UpdateCurrentPlayerTurnIndexAcrossLobby(newIndex);
        }
    }

    private void Awake()
    {
        GetUIElements();
        currentPlayerTurnIndex = -1;
        instance = this;
    }

    private void Update()
    {
        if (effectT > 0.0f)
        {
            effectT -= Time.deltaTime * 5.0f;

            if (previousPlayerTurnIndex != -1)
            {
                actualInfos[previousPlayerTurnIndex].anchoredPosition = new Vector2(Mathf.Lerp(atRestXPos, activeXPos, effectT), actualInfos[previousPlayerTurnIndex].anchoredPosition.y);
            }

            actualInfos[currentPlayerTurnIndex].anchoredPosition = new Vector2(Mathf.Lerp(activeXPos, atRestXPos, effectT), actualInfos[currentPlayerTurnIndex].anchoredPosition.y);
        }
    }

    /// <summary>
    /// Sets the list of players to display through the UI
    /// </summary>
    /// <param name="players">The list of players to display</param>
    public static void SetPlayers(List<int> players, bool makeRequest = true)
    {
        if (makeRequest && NetworkManagement.GetClientState() != NetworkManagement.ClientState.Offline)
        {
            //Tell the clients to set up their players
            NetworkConnection.SetupPlayerInfoHandlerOnClients(players);
        }
        else
        {
            //Actually Apply
            originalPlayers = players;
            instance.UpdateColours();
            instance.ResetInfos();

            indexToHandCounts = new Dictionary<int, int>();

            foreach (int player in originalPlayers)
            {
                indexToHandCounts[player] = 0;
            }

            UpdateInfo(true);
        }
    }

    private void GetUIElements()
    {
        actualInfos = new List<RectTransform>();
        infoFronts = new List<Image>();
        textBackers = new List<Image>();
        infoTexts = new List<TextMeshProUGUI>();
        crosses = new List<GameObject>();
        backers = new List<GameObject>();

        for (int i = 0; i < 6; i++)
        {
            Transform info = transform.GetChild(i);
            actualInfos.Add(info as RectTransform);

            //Reset position
            actualInfos[i].anchoredPosition = new Vector2(atRestXPos, actualInfos[i].anchoredPosition.y);

            backers.Add(info.gameObject);
            infoFronts.Add(info.GetChild(0).GetComponent<Image>());
            textBackers.Add(infoFronts[^1].transform.GetChild(1).GetComponent<Image>());
            infoTexts.Add(textBackers[^1].transform.GetChild(0).GetComponent<TextMeshProUGUI>());
            crosses.Add(info.GetChild(1).gameObject);
            crosses[^1].SetActive(false);
        }
    }

    static List<RectTransform> actualInfos;
    static List<Image> infoFronts;
    static List<Image> textBackers;
    static List<TextMeshProUGUI> infoTexts;
    static List<GameObject> crosses;
    static List<GameObject> backers;
    public void UpdateColours()
    {
        for (int i = 0; i<6; i++)
        {
            if (i < originalPlayers.Count)
            {
                infoFronts[i].color = Player.GetColourBasedOnIndex(originalPlayers[i]);
                textBackers[i].color = Player.GetColourBasedOnIndex(originalPlayers[i]);
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

    public static void UpdateHandCounts(Dictionary<int, int> playerIndexToHandCounts)
    {
        indexToHandCounts = playerIndexToHandCounts;
    }

    public static void UpdateInfo(bool inSetup = false)
    {
        List<int> alivePlayers = Map.GetAlivePlayers();

        for (int i = 0; i < originalPlayers.Count; i++)
        {
            if (!alivePlayers.Contains(originalPlayers[i]) && !inSetup)
            {
                crosses[i].SetActive(true);
                infoTexts[i].text = "";
            }
            else if (!crosses[i].activeSelf)
            {
                if (indexToHandCounts.ContainsKey(originalPlayers[i]))
                {
                    infoTexts[i].text = indexToHandCounts[originalPlayers[i]].ToString();
                }
                else
                {
                    infoTexts[i].text = "0";
                }
            }
        }
    }
}
