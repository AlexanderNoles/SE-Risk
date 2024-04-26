using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
/// <summary>
/// Controls the win screen UI. involves outputting the winner to the screen.
/// </summary>
public class WinScreenControl : MonoBehaviour
{
    /// <summary>
    /// The text to output to.
    /// </summary>
    public TextMeshProUGUI winnerText;

    private void OnEnable()
    {
        MatchManager.GameWonInfo info = MatchManager.GetGameWonInfo();

        winnerText.text = "GAME WON BY <color=" + info.winnerColor + ">" + info.winnerName + "</color>";
    }
}
