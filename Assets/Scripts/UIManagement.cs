using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
/// <summary>
/// Manages code side communication with non bespoke UI. This includes managing storage of generic UI elements, for example troop count UI. 
/// </summary>
public class UIManagement : MonoBehaviour
{
    static UIManagement instance;
    /// <summary>
    /// UI reference. Setup in inspector. Displays information about the current turn. 
    /// </summary>
    public TextMeshProUGUI turnInfoText;
    /// <summary>
    /// UI reference. Setup in inspector. Gray plane that sits behind other UI elements.
    /// </summary>
    public GameObject greyPlane;
    /// <summary>
    /// UI reference. Setup in inspector. Displays the output of dice rolls.
    /// </summary>
    public TextMeshProUGUI rollOutput;
    /// <summary>
    /// UI reference. Setup in inspector. Roll output text effect.
    /// </summary>
    public Image textFadeOut;
    private float fadeOutT;
    private float buildUp;
    private static int newLinesAdded = 0;
    /// <summary>
    /// UI reference. Setup in inspector. Easing curve for the roll output text effect.
    /// </summary>
    public AnimationCurve fadeOutCurve;

    private const int maxLengthOfOutput = 65;
    private static List<string> rollOutputLines;

    private static MultiObjectPool pools;

    private void Awake()
    {
        Debug.Log("hi");
        instance = this;
        rollOutputLines = new List<string>();
        RefreshRollOutput();
        pools = GetComponent<MultiObjectPool>();
    }

    /// <summary>
    /// Spawns a generic UI object at a given position from a specific Object Pool.  
    /// </summary>
    /// <typeparam name="T">Component to retrieve from the spawned object.</typeparam>
    /// <param name="pos">Position to spawn the object at.</param>
    /// <param name="poolIndex">The object pool to take the object from.</param>
    /// <returns></returns>
    public static MultiObjectPool.ObjectFromPool<T> Spawn<T>(Vector3 pos, int poolIndex)
    {
        return pools.SpawnObject<T>(poolIndex, pos);
    }

    /// <summary>
    /// Set the text of the turn info text object.
    /// </summary>
    /// <param name="text">The new text to display.</param>
    public static void SetText(string text)
    {
        if (!Map.IsSimulated())
        {
            instance.turnInfoText.text = text;
        }
    }

    /// <summary>
    /// Set the gray backing plane active or inactive.
    /// </summary>
    /// <param name="active">Active or inactive?</param>
    public static void SetActiveGreyPlane(bool active)
    {
        instance.greyPlane.SetActive(active);
    }

    /// <summary>
    /// Add a line to the roll output text.
    /// </summary>
    /// <param name="line">The text to add.</param>
    public static void AddLineToRollOutput(string line)
    {
        if (Map.IsSimulated()) 
        {
            return;
        }

        rollOutputLines.Insert(0, line);
        newLinesAdded++;

        if (rollOutputLines.Count > maxLengthOfOutput)
        {
            rollOutputLines.RemoveAt(maxLengthOfOutput);
        }
    }

    /// <summary>
    /// Apply the new output text to the actual roll output UI object.
    /// </summary>
    public static void RefreshRollOutput()
    {
        if (Map.IsSimulated())
        {
            return;
        }

        //Construct output string
        string finalOutputString = "";

        foreach (string line in rollOutputLines)
        {
            finalOutputString += line + "\n";
        }

        //Set to output
        instance.rollOutput.text = finalOutputString;
        instance.buildUp += 1.0f;
        instance.fadeOutT = 1.0f;

        instance.textFadeOut.rectTransform.sizeDelta = new Vector2(265, newLinesAdded * 29);
        instance.textFadeOut.rectTransform.anchoredPosition = new Vector2(0.0f, newLinesAdded * -14.5f);

        newLinesAdded = 0;
    }

    private void Update()
    {
        if (fadeOutT > 0.0f)
        {
            fadeOutT -= Time.deltaTime * Mathf.Clamp(buildUp, 1.0f, 100.0f);
            textFadeOut.color = Color.Lerp(Color.clear, Color.black, fadeOutCurve.Evaluate(fadeOutT));
        }

        if (buildUp > 0.0f)
        {
            buildUp = Mathf.Lerp(buildUp, 0.0f, Time.deltaTime);
        }
    }
}
