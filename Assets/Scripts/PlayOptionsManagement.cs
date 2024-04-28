using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// <c>PlayOptionsManagement</c> manages the play options (mode, number of AI playes) for a given match. Allows their modification through the play screen, which it controls.
/// </summary>
public class PlayOptionsManagement : MonoBehaviour
{
    private static PlayOptionsManagement instance;

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
        public int numberOfNetworkPlayers;

        /// <summary>
        /// Standard PlayOptions constructor.
        /// </summary>
        /// <param name="mode">Intial Game Mode.</param>
        /// <param name="numAIPlayers">Intial number of AI players.</param>
        public PlayOptions(Mode mode, int numAIPlayers)
        {
            this.mode = mode;
            numberOfAIPlayers = numAIPlayers;
            numberOfNetworkPlayers = 1; //The host pc
        }

        /// <summary>
        /// Get the total number of players.
        /// </summary>
        /// <returns>Currently returns the number of AI players + 1 (for the host player).</returns>
        public int TotalNumberOfPlayers()
        {
            return numberOfAIPlayers + numberOfNetworkPlayers;
        }
    }

    //If you run from just the play scene these default settings will be used
    private static PlayOptions playOptions = new PlayOptions(PlayOptions.Mode.Normal, 5);

    public GameObject modeRaycastBlocker;
            
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
    private List<(Image, TextMeshProUGUI, GameObject)> playerSlotRefrences = new List<(Image, TextMeshProUGUI, GameObject)>();
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
    /// UI object refrence,for the start client button, setup in inspector.
    /// </summary>
    public GameObject startClientButton;
    /// <summary>
    /// UI object refrence,for the start client button, setup in inspector.
    /// </summary>
    public TextMeshProUGUI menuTitle;

    public RawImage checkerBacking;
    public Color lanLobbyColour;

    private void SetLobbySchemeActive(bool active)
    {
        if (active)
        {
            menuTitle.text = "LAN Lobby";
            checkerBacking.color = lanLobbyColour;
        }
        else
        {
            menuTitle.text = "Play";
            checkerBacking.color = new Color(1,1,1, 0.129f);
        }
    }

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
    /// Static function that returns the current number of network players.
    /// </summary>
    /// <returns>Current number of network players as int.</returns>
    public static int GetNumberOfNetworkPlayers()
    {
        return playOptions.numberOfNetworkPlayers;
    }

    /// <summary>
    /// Static function that returns the total number of players.
    /// </summary>
    /// <returns>Total number of players as int.</returns>
    public static int GetTotalNumberOfPlayers()
    {
        return playOptions.TotalNumberOfPlayers();
    }

    private static bool dontRunDisconnectTransitions = false;
    public static void DontRunDisconnectTransitions()
    {
        dontRunDisconnectTransitions = true;
    }

    private const float transitionSpeed = 1.0f;

    private void Awake()
    {
        instance = this;
        playOptions = new PlayOptions(PlayOptions.Mode.Normal, 0);
        SetLobbySchemeActive(false);

        foreach (RectTransform extraSlot in extraPlayerSlots)
        {
            playerSlotRefrences.Add((
                extraSlot.GetComponent<Image>(),
                extraSlot.GetChild(0).GetComponent<TextMeshProUGUI>(),
                extraSlot.GetChild(1).gameObject
                ));
        }


        SetPlayModeNormal(false);
        UpdateCloseConnectionButton(false);
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

        NetworkDataCommunicator.UpdateMode(0);
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

        NetworkDataCommunicator.UpdateMode(1);
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

        if (NetworkManagement.GetClientState() == NetworkManagement.ClientState.Offline)
        {
            UpdatePlayerUI();
        }
        else
        {
            NetworkDataCommunicator.UpdateNumberOfAIPlayers(playOptions.numberOfAIPlayers);
        }
    }

    /// <summary>
    /// Remove an AI player. Used by UI buttons.
    /// </summary>
    public void RemoveAIPlayer()
    {
        AudioManagement.PlaySound("ButtonPress");
        playOptions.numberOfAIPlayers--;

        if (NetworkManagement.GetClientState() == NetworkManagement.ClientState.Offline)
        {
            UpdatePlayerUI();
        }
        else
        {
            NetworkDataCommunicator.UpdateNumberOfAIPlayers(playOptions.numberOfAIPlayers);
        }
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
        if (NetworkManagement.GetClientState() != NetworkManagement.ClientState.Client)
        {
            startButton.gameObject.SetActive(true);

            startButton.interactable = GetTotalNumberOfPlayers() >= 3;
            cantStartButton.SetActive(!startButton.interactable);
        }
        else
        {
            startButton.gameObject.SetActive(false);
            cantStartButton.SetActive(false);
        }

        bool setAddButtonActive = false;
        float yPos = -85;

        for (int i = 0; i < extraPlayerSlots.Count; i++)
        {
            yPos -= 120;
            Vector2 newPos = new Vector2(0, yPos);

            if (i < playOptions.numberOfNetworkPlayers)
            {
                extraPlayerSlots[i].gameObject.SetActive(true);
                extraPlayerSlots[i].anchoredPosition = newPos;

                string labelText = "LAN player";
                Color color = Color.white;
                color.a = 0.8f;

                if (i == 0)
                {
                    if (NetworkManagement.GetClientState() == NetworkManagement.ClientState.Offline)
                    {
                        labelText = "You";
                    }
                    else if (NetworkManagement.GetClientState() == NetworkManagement.ClientState.Host)
                    {
                        labelText = "You (host)";
                    }
                    else
                    {
                        labelText = "Host";
                    }
                }
                else if (i == 1 && NetworkManagement.GetClientState() == NetworkManagement.ClientState.Client)
                {
                    //Temp, all clients see first player as them
                    labelText = "You";
                }

                playerSlotRefrences[i].Item1.color = color;
                playerSlotRefrences[i].Item2.text = labelText;
                playerSlotRefrences[i].Item3.SetActive(false);
            }
            else if (i-playOptions.numberOfNetworkPlayers < playOptions.numberOfAIPlayers)
            {
                extraPlayerSlots[i].gameObject.SetActive(true);
                extraPlayerSlots[i].anchoredPosition = newPos;

                playerSlotRefrences[i].Item1.color = new Color(0.9f, 0.9f, 0.9f, 0.8f);
                playerSlotRefrences[i].Item2.text = "AI PLAYER";
                playerSlotRefrences[i].Item3.SetActive(NetworkManagement.GetClientState() != NetworkManagement.ClientState.Client);
            }
            else 
            {
                if (i == GetTotalNumberOfPlayers())
                {
                    setAddButtonActive = NetworkManagement.GetClientState() != NetworkManagement.ClientState.Client;
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
        AudioManagement.PlaySound("ButtonPress");

        TransitionControl.onTransitionOver.AddListener(ActuallySwitchToHost);
        TransitionControl.RunTransition(TransitionControl.Transitions.SwipeIn, transitionSpeed);
    }

    private void ActuallySwitchToHost()
    {
        SetLobbySchemeActive(true);

        TransitionControl.onTransitionOver.RemoveListener(ActuallySwitchToHost);
        TransitionControl.RunTransition(TransitionControl.Transitions.SwipeOut, transitionSpeed);
        NetworkManagement.UpdateClientNetworkState(NetworkManagement.ClientState.Host);
        UpdateCloseConnectionButton(true);
    }

    public static void NewHostSetup()
    {
        NetworkDataCommunicator.UpdateNumberOfAIPlayers(playOptions.numberOfAIPlayers);
        //1 representing the host
        NetworkDataCommunicator.UpdateNumberOPlayers(1);

        NetworkDataCommunicator.UpdateMode((int)playOptions.mode);

        //Force update UI
        //It would update automatically if the network synced number of ai and network players was different everytime
        //but they could not be
        instance.UpdatePlayerUI();
    }

    public static void NotifyHostOfNewConnection()
    {
        instance.OnNewConnection();
    }

    private void OnNewConnection()
    {
        playOptions.numberOfNetworkPlayers++;
        NetworkDataCommunicator.UpdateNumberOPlayers(playOptions.numberOfNetworkPlayers);
    }

    public static void NotifyHostOfLostConnection()
    {
        if (instance == null)
        {
            return;
        }

        instance.OnLostConnection();
    }

    private void OnLostConnection()
    {
        playOptions.numberOfNetworkPlayers--;
        NetworkDataCommunicator.UpdateNumberOPlayers(playOptions.numberOfNetworkPlayers);
    }

    public void StartClientButton()
    {
        AudioManagement.PlaySound("ButtonPress");
        modeRaycastBlocker.SetActive(true);
        TransitionControl.onTransitionOver.AddListener(ActuallyStartClient);
        TransitionControl.RunTransition(TransitionControl.Transitions.SwipeIn, transitionSpeed);
    }

    private void ActuallyStartClient()
    {
        NetworkConnection.ResetTouchedServer();
        SetLobbySchemeActive(true);
        TransitionControl.onTransitionOver.RemoveListener(ActuallyStartClient);
        TransitionControl.RunTransition(TransitionControl.Transitions.SwipeOut, transitionSpeed);
        NetworkManagement.UpdateClientNetworkState(NetworkManagement.ClientState.Client);
        UpdateCloseConnectionButton(true);
    }

    public static void ForceOnDisconnetToRun()
    {
        if (instance == null)
        {
            return;
        }

        instance.OnDisconnect();
    }

    public void OnDisconnect()
    {
        if (!NetworkConnection.ActuallyConnectedToServer())
        {
            DontRunDisconnectTransitions();
        }
        else
        {
            NetworkConnection.ResetTouchedServer();
        }

        if (dontRunDisconnectTransitions)
        {
            ActuallyDisconnect();
            return;
        }

        TransitionControl.onTransitionOver.AddListener(ActuallyDisconnect);
        TransitionControl.RunTransition(TransitionControl.Transitions.SwipeIn, transitionSpeed);
    }

    private void ActuallyDisconnect()
    {
        SetLobbySchemeActive(false);
        modeRaycastBlocker.SetActive(false);
        UpdateCloseConnectionButton(false);
        if (dontRunDisconnectTransitions)
        {
            dontRunDisconnectTransitions = false;
        }
        else
        {
            TransitionControl.onTransitionOver.RemoveListener(ActuallyDisconnect);
            TransitionControl.RunTransition(TransitionControl.Transitions.SwipeOut, transitionSpeed);
        }
    }

    public void GoOfflineButton()
    {
        AudioManagement.PlaySound("ButtonPress");
        NetworkManagement.UpdateClientNetworkState(NetworkManagement.ClientState.Offline);
    }

    private void UpdateCloseConnectionButton(bool active)
    {
        if (!active)
        {
            //No longer online
            playOptions.numberOfNetworkPlayers = 1;
            playOptions.numberOfAIPlayers = 0;

            SetPlayModeNormal(false);
            UpdatePlayerUI();
        }

        goOfflineButton.SetActive(active);
        startHostButton.SetActive(!active);
        startClientButton.SetActive(!active);
    }

    public void LeavePlayScreenButton()
    {
        GoOfflineButton();
        MenuManagement.LoadMenu(MenuManagement.Menu.Main);
    }

    public static void UpdatePlayerUIExternal(int numberOfAI, int numberOfNonAI, int mode)
    {
        playOptions.numberOfAIPlayers = numberOfAI;
        playOptions.numberOfNetworkPlayers = numberOfNonAI;
        instance.UpdatePlayerUI();

        if (NetworkManagement.GetClientState() != NetworkManagement.ClientState.Host)
        {
            if (mode == (int)playOptions.mode)
            {
                //Dont update mode if we don't have too
                //This prevents the button animation from playing
                return;
            }

            if (mode == 0)
            {
                instance.SetPlayModeNormal(false);
            }
            else
            {
                instance.SetPlayModeConquest(false);
            }
        }
    }
}
