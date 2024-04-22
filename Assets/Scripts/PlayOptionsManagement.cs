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

        public PlayOptions(Mode mode)
        {
            this.mode = mode;
            numberOfAIPlayers = 0;
        }
    }

    private static PlayOptions playOptions = new PlayOptions(PlayOptions.Mode.Normal);
    public RectTransform selectionOutline;
    public Image normalImage;
    public Image conquestImage;
    public AnimationCurve buttonFeedbackAnimationCurve;

    private float buttonT;
    private Image targetImageForAnimation;

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

    private void Awake()
    {
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
        playOptions.mode = PlayOptions.Mode.Normal;
        UpdateModeUI(normalImage, conquestImage);
    }

    public void SetPlayModeConquest()
    {
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
        playOptions.numberOfAIPlayers++;
        UpdatePlayerUI();
    }

    public void RemoveAIPlayer()
    {
        playOptions.numberOfAIPlayers--;
        UpdatePlayerUI();
    }

    private void UpdatePlayerUI()
    {
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
