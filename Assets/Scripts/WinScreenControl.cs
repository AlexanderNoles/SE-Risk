using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WinScreenControl : MonoBehaviour
{
    public TextMeshProUGUI winnerText;

    private void OnEnable()
    {
        MatchManager.GameWonInfo info = MatchManager.GetGameWonInfo();

        winnerText.text = "GAME WON BY <color=" + info.winnerColor + ">" + info.winnerName + "</color>";
    }
}
