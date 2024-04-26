using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Controls the win screen UI. involves outputting the winner to the screen.
/// </summary>
public class WinScreenControl : MonoBehaviour
{
    /// <summary>
    /// The text to output to.
    /// </summary>
    public TextMeshProUGUI winnerText;
    /// <summary>
    /// The title text. Outputs you won or you lost.
    /// </summary>
    public TextMeshProUGUI title;
    /// <summary>
    /// The backing behind all the text.
    /// </summary>
    public RawImage backing;

    private void OnEnable()
    {
        MatchManager.GameWonInfo info = MatchManager.GetGameWonInfo();

        winnerText.text = "GAME WON BY <color=" + info.winnerColor + ">" + info.winnerName + "</color>";
        title.text = info.localPlayerWon ? "YOU WON!" : "YOU LOST!";
        backing.color = info.localPlayerWon ? 
            new Color(0.764151f, 0.7370255f, 0.4000979f) : 
            new Color(0.4f, 0.4681f, 0.7647f);
    }
}
