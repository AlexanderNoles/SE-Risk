using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayOptionsManagement : MonoBehaviour
{
    public struct PlayOptions
    {
        public enum Mode
        {
            Normal,
            Conquest
        }

        public Mode mode;
        public int numberOfAIPlayers;

        public PlayOptions(Mode mode, int numAIPlayers)
        {
            this.mode = mode;
            numberOfAIPlayers = numAIPlayers;
        }

        public int TotalNumberOfPlayers()
        {
            //Always assume 1 as that is the pc this is running on
            return 1 + numberOfAIPlayers;
        }
    }

    //If you run from just the play scene these default settings will be used
    private static PlayOptions playOptions = new PlayOptions(PlayOptions.Mode.Normal, 5);
    public RectTransform selectionOutline;
    public Image normalImage;
    public Image conquestImage;
    public AnimationCurve buttonFeedbackAnimationCurve;

    private float buttonT;
    private Image targetImageForAnimation;

    [Header("Start Button")]
    public Button startButton;
    public GameObject cantStartButton;

    [Header("Player List")]
    public List<RectTransform> extraPlayerSlots = new List<RectTransform>();
    public RectTransform addSlotButton;

    public static bool IsConquestMode()
    {
        return playOptions.mode == PlayOptions.Mode.Conquest;
    }

    public static bool IsNormalMode()
    {
        return playOptions.mode == PlayOptions.Mode.Normal;
    }

    public static int GetNumberOfAIPlayers()
    {
        return playOptions.numberOfAIPlayers;
    }

    public static int GetTotalNumberOfPlayers()
    {
        return playOptions.TotalNumberOfPlayers();
    }

    private void Awake()
    {
        playOptions = new PlayOptions(PlayOptions.Mode.Normal, 0);
        SetPlayModeNormal();
        UpdatePlayerUI();
    }

    private void Update()
    {
        if (buttonT > 0.0f)
        {
            buttonT -= Time.deltaTime * 5.0f;

            targetImageForAnimation.rectTransform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.1f, buttonFeedbackAnimationCurve.Evaluate(buttonT));
            selectionOutline.localScale = targetImageForAnimation.rectTransform.localScale;
        }
    }

    public void SetPlayModeNormal()
    {
        AudioManagement.PlaySound("ButtonPress");
        playOptions.mode = PlayOptions.Mode.Normal;
        UpdateModeUI(normalImage, conquestImage);
    }

    public void SetPlayModeConquest()
    {
        AudioManagement.PlaySound("ButtonPress");
        playOptions.mode = PlayOptions.Mode.Conquest;
        UpdateModeUI(conquestImage, normalImage);
    }

    private void UpdateModeUI(Image selectedImage, Image nonSelectedImage)
    {
        if (targetImageForAnimation != null)
        {
            targetImageForAnimation.rectTransform.localScale = Vector3.one;
        }
        targetImageForAnimation = selectedImage;
        buttonT = 1.0f;

        selectedImage.color = Color.white;
        selectionOutline.anchoredPosition = selectedImage.rectTransform.anchoredPosition;

        nonSelectedImage.color = Color.grey;
    }

    public void AddAIPlayer()
    {
        AudioManagement.PlaySound("ButtonPress");
        playOptions.numberOfAIPlayers++;
        UpdatePlayerUI();
    }

    public void RemoveAIPlayer()
    {
        AudioManagement.PlaySound("ButtonPress");
        playOptions.numberOfAIPlayers--;
        UpdatePlayerUI();
    }

    public void CantStartGame()
    {
        AudioManagement.PlaySound("Refuse");
    }

    private void UpdatePlayerUI()
    {
        startButton.interactable = GetTotalNumberOfPlayers() >= 3;
        cantStartButton.SetActive(!startButton.interactable);

        bool setAddButtonActive = false;
        float yPos = -195;

        for (int i = 0; i < extraPlayerSlots.Count; i++)
        {
            yPos -= 120;
            Vector2 newPos = new Vector2(0, yPos);

            if (i < playOptions.numberOfAIPlayers)
            {
                extraPlayerSlots[i].gameObject.SetActive(true);
                extraPlayerSlots[i].anchoredPosition = newPos;
            }
            else 
            {
                if (i == playOptions.numberOfAIPlayers)
                {
                    setAddButtonActive = true;
                    addSlotButton.anchoredPosition = new Vector2(0, yPos);
                }

                extraPlayerSlots[i].gameObject.SetActive(false);
            } 
        }

        addSlotButton.gameObject.SetActive(setAddButtonActive);
    }
}
