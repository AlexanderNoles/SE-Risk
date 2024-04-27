using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// <c>PlayOptionsManagement</c> manages the play options (mode, number of AI playes) for a given match. Allows their modification through the play screen, which it controls.
/// </summary>
public class PlayOptionsManagement : MonoBehaviour
{
    /// <summary>
    /// Stores all the PlayOptions needed for a given match.
    /// </summary>
    public struct PlayOptions
    {
        public enum Mode
        {
            Normal,
            Conquest
        }

        public Mode mode;
        public int numberOfAIPlayers;

        /// <summary>
        /// Standard PlayOptions constructor.
        /// </summary>
        /// <param name="mode">Intial Game Mode.</param>
        /// <param name="numAIPlayers">Intial number of AI players.</param>
        public PlayOptions(Mode mode, int numAIPlayers)
        {
            this.mode = mode;
            numberOfAIPlayers = numAIPlayers;
        }

        /// <summary>
        /// Get the total number of players.
        /// </summary>
        /// <returns>Currently returns the number of AI players + 1 (for the host player).</returns>
        public int TotalNumberOfPlayers()
        {
            //Always assume 1 as that is the pc this is running on
            return 1 + numberOfAIPlayers;
        }
    }

    //If you run from just the play scene these default settings will be used
    private static PlayOptions playOptions = new PlayOptions(PlayOptions.Mode.Normal, 5);

    /// <summary>
    /// UI object refrence for the selected mode outline, setup in inspector.
    /// </summary>
    public RectTransform selectionOutline;
    /// <summary>
    /// UI object refrence, for the normal mode button, setup in inspector.
    /// </summary>
    public Image normalImage;
    /// <summary>
    /// UI object refrence, for the conquest mode button, setup in inspector.
    /// </summary>
    public Image conquestImage;
    /// <summary>
    /// Animation Curve ditacting the ease function for the on mode button click effect, setup in inspector.
    /// </summary>
    public AnimationCurve buttonFeedbackAnimationCurve;

    private float buttonT;
    private Image targetImageForAnimation;

    [Header("Start Button")]
    /// <summary>
    /// UI object refrence, for the start game button, setup in inspector.
    /// </summary>
    public Button startButton;
    /// <summary>
    /// UI object refrence, for the start game button, setup in inspector.
    /// </summary>
    public GameObject cantStartButton;

    [Header("Player List")]
    /// <summary>
    /// UI object refrence, for the extra player slots, setup in inspector.
    /// </summary>
    public List<RectTransform> extraPlayerSlots = new List<RectTransform>();
    /// <summary>
    /// UI object refrence, for the add AI player to game button, setup in inspector.
    /// </summary>
    public RectTransform addSlotButton;

    [Header("Networking")]
    /// <summary>
    /// UI object refrence,for the go offline button, setup in inspector.
    /// </summary>
    public GameObject goOfflineButton;
    /// <summary>
    /// UI object refrence,for the start host button, setup in inspector.
    /// </summary>
    public GameObject startHostButton;
    /// <summary>
    /// UI object refrence,for the go offline button, setup in inspector.
    /// </summary>
    public GameObject startClientButton;

    /// <summary>
    /// Static function that returns true if current set mode is Conquest mode.
    /// </summary>
    /// <returns>Is current mode Conquest?</returns>
    public static bool IsConquestMode()
    {
        return playOptions.mode == PlayOptions.Mode.Conquest;
    }

    /// <summary>
    /// Static function that returns true if current set mode is Normal mode.
    /// </summary>
    /// <returns>Is current mode Normal?</returns>
    public static bool IsNormalMode()
    {
        return playOptions.mode == PlayOptions.Mode.Normal;
    }

    /// <summary>
    /// Static function that returns the current number of AI players.
    /// </summary>
    /// <returns>Current number of AI players as int.</returns>
    public static int GetNumberOfAIPlayers()
    {
        return playOptions.numberOfAIPlayers;
    }

    /// <summary>
    /// Static function that returns the total number of players.
    /// </summary>
    /// <returns>Total number of players as int.</returns>
    public static int GetTotalNumberOfPlayers()
    {
        return playOptions.TotalNumberOfPlayers();
    }

    private void Awake()
    {
        playOptions = new PlayOptions(PlayOptions.Mode.Normal, 0);
        SetPlayModeNormal(false);
        UpdateCloseConnectionButton(false, false);
        UpdatePlayerUI();
    }

    private void OnEnable()
    {
        NetworkManagement.onClientDisconnect.AddListener(OnDisconnect);
    }

    private void OnDisable()
    {
        NetworkManagement.onClientDisconnect.RemoveListener(OnDisconnect);
    }

    private void Update()
    {
        //Mode buttons, on selected effect
        if (buttonT > 0.0f)
        {
            buttonT -= Time.deltaTime * 5.0f;

            targetImageForAnimation.rectTransform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.1f, buttonFeedbackAnimationCurve.Evaluate(buttonT));
            selectionOutline.localScale = targetImageForAnimation.rectTransform.localScale; //Make selection outline match current selected button
        }
    }

    /// <summary>
    /// Set the current game mode to Normal, used by UI buttons and code in Awake.
    /// </summary>
    /// <param name="playSound">Should sound be played when this function is run? Used to give feedback on button press.</param>
    public void SetPlayModeNormal(bool playSound = true)
    {
        if (playSound)
        {
            AudioManagement.PlaySound("ButtonPress");
        }

        playOptions.mode = PlayOptions.Mode.Normal;
        UpdateModeUI(normalImage, conquestImage);
    }

    /// <summary>
    /// Set the current game mode to Conquest, used by UI buttons.
    /// </summary>
    /// <param name="playSound">Should sound be played when this function is run? Used to give feedback on button press.</param>
    public void SetPlayModeConquest(bool playSound = true)
    {
        if (playSound)
        {
            AudioManagement.PlaySound("ButtonPress");
        }

        playOptions.mode = PlayOptions.Mode.Conquest;
        UpdateModeUI(conquestImage, normalImage);
    }

    private void UpdateModeUI(Image selectedImage, Image nonSelectedImage)
    {
        if (targetImageForAnimation != null)
        {
            //Reset local scale, in case we are in the middle of the on selected growth animation
            targetImageForAnimation.rectTransform.localScale = Vector3.one;
        }
        targetImageForAnimation = selectedImage;
        //Set effect value to 1
        buttonT = 1.0f;

        selectedImage.color = Color.white;
        selectionOutline.anchoredPosition = selectedImage.rectTransform.anchoredPosition;

        nonSelectedImage.color = Color.grey;
    }

    /// <summary>
    /// Add an AI player. Used by UI buttons.
    /// </summary>
    public void AddAIPlayer()
    {
        AudioManagement.PlaySound("ButtonPress");
        playOptions.numberOfAIPlayers++;
        UpdatePlayerUI();
    }

    /// <summary>
    /// Remove an AI player. Used by UI buttons.
    /// </summary>
    public void RemoveAIPlayer()
    {
        AudioManagement.PlaySound("ButtonPress");
        playOptions.numberOfAIPlayers--;
        UpdatePlayerUI();
    }

    /// <summary>
    /// Play a sound to give feedback on start game refused. Used by UI buttons.
    /// </summary>
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

    ///NETWORK STUFF
    public void StartHostButton()
    {
        NetworkManagement.UpdateClientNetworkState(NetworkManagement.ClientState.Host);
        UpdateCloseConnectionButton(true);
    }

    public void StartClientButton()
    {
        NetworkManagement.UpdateClientNetworkState(NetworkManagement.ClientState.Client);
        UpdateCloseConnectionButton(true);
    }

    public void OnDisconnect()
    {
        UpdateCloseConnectionButton(false, false);
    }

    public void GoOfflineButton()
    {
        NetworkManagement.UpdateClientNetworkState(NetworkManagement.ClientState.Offline);
        AudioManagement.PlaySound("ButtonPress");
    }

    private void UpdateCloseConnectionButton(bool active, bool playSound = true)
    {
        if (playSound)
        {
            AudioManagement.PlaySound("ButtonPress");
        }

        goOfflineButton.SetActive(active);
        startHostButton.SetActive(!active);
        startClientButton.SetActive(!active);
    }
}
