using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManagement : MonoBehaviour
{
    static UIManagement instance;
    public TextMeshProUGUI turnInfoText;
    public GameObject greyPlane;
    public TextMeshProUGUI rollOutput;
    public Image textFadeOut;
    private float fadeOutT;
    private static int newLinesAdded = 0;
    public AnimationCurve fadeOutCurve;

    private const int maxLengthOfOutput = 65;
    private static List<string> rollOutputLines;

    private static MultiObjectPool pools;

    private void Awake()
    {
        instance = this;
        rollOutputLines = new List<string>();
        RefreshRollOutput();
        pools = GetComponent<MultiObjectPool>();
    }

    public static MultiObjectPool.ObjectFromPool<T> Spawn<T>(Vector3 pos, int poolIndex)
    {
        return pools.SpawnObject<T>(poolIndex, pos);
    }
    public static void SetText(string text)
    {
        instance.turnInfoText.text = text;
    }

    public static void SetActiveGreyPlane(bool active)
    {
        instance.greyPlane.SetActive(active);
    }

    public static void AddLineToRollOutput(string line)
    {
        rollOutputLines.Insert(0, line);
        newLinesAdded++;

        if (rollOutputLines.Count > maxLengthOfOutput)
        {
            rollOutputLines.RemoveAt(maxLengthOfOutput);
        }
    }

    public static void RefreshRollOutput()
    {
        //Construct output string
        string finalOutputString = "";

        foreach (string line in rollOutputLines)
        {
            finalOutputString += line + "\n";
        }

        //Set to output
        instance.rollOutput.text = finalOutputString;
        instance.fadeOutT = 1.0f;

        instance.textFadeOut.rectTransform.sizeDelta = new Vector2(265, newLinesAdded * 29);
        instance.textFadeOut.rectTransform.anchoredPosition = new Vector2(0.0f, newLinesAdded * -14.5f);

        newLinesAdded = 0;
    }

    private void Update()
    {
        if (fadeOutT > 0.0f)
        {
            fadeOutT -= Time.deltaTime;
            textFadeOut.color = Color.Lerp(Color.clear, Color.black, fadeOutCurve.Evaluate(fadeOutT));
        }
    }
}
