using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// <c>MenuManagement</c> manages the menu scene. It acts as a convient way to load each menu, main entry point is LoadMenu.
/// </summary>
public class MenuManagement : MonoBehaviour
{
    private static MenuManagement instance;

    /// <summary>
    /// Menu enum describing each possible menu that can be loaded. When adding a new menu this must be updated.
    /// </summary>
    public enum Menu
    {
        Main,
        Play,
        Options,
        WinScreen
    }

    private static Menu defaultMenu = Menu.Main;

    /// <summary>
    /// Set the default menu to be loaded when the menu scene is loaded. The standard default menu is Menu.Main.
    /// </summary>
    /// <param name="menu">The new default menu</param>
    public static void SetDefaultMenu(Menu menu)
    {
        defaultMenu = menu;
    }

    /// <summary>
    /// The current loaded menu.
    /// </summary>
    public static Menu currentMenu = Menu.Main;
    private static Menu intendedMenu = Menu.Main;
    /// <summary>
    /// Serialized list of menu objects displayed in the inspector. Indexed by converting Menu enum to an int, as such each object correlates to an entry in said enum.
    /// </summary>
    public List<GameObject> menuObjects = new List<GameObject>();

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        //In start to not run into conflicts with instance identification in transition control
        LoadMenu(defaultMenu, false);

        if (defaultMenu == Menu.Main)
        {
            TransitionControl.HideMaskedTextForNextTransition();
        }

        TransitionControl.RunTransition(TransitionControl.Transitions.SwipeOut);
    }

    /// <summary>
    /// Button method, used in inspector.
    /// </summary>
    public void LoadMainMenuButton()
    {
        AudioManagement.PlaySound("ButtonPress");
        LoadMenu(Menu.Main);
    }
    /// <summary>
    /// Button method, used in inspector.
    /// </summary>
    public void LoadPlayMenuButton()
    {
        AudioManagement.PlaySound("ButtonPress");
        LoadMenu(Menu.Play);
    }
    /// <summary>
    /// Button method, used in inspector.
    /// </summary>
    public void LoadMainSceneButton()
    {
        AudioManagement.PlaySound("ButtonPress");
        TransitionControl.onTransitionOver.AddListener(ActuallyLoadMainScene);
        TransitionControl.RunTransition(TransitionControl.Transitions.SwipeIn);
    }

    private void ActuallyLoadMainScene()
    {
        TransitionControl.onTransitionOver.RemoveListener(ActuallyLoadMainScene);
        SceneManager.LoadScene(0);
        TransitionControl.RunTransition(TransitionControl.Transitions.SwipeOut);
    }
    /// <summary>
    /// Button method, used in inspector.
    /// </summary>
    public void LoadOptionsButton()
    {
        AudioManagement.PlaySound("ButtonPress");
        LoadMenu(Menu.Options);
    }

    /// <summary>
    /// <c>LoadMenu</c> loads a specific menu. Plays transitions automatically to make loading look smoother.
    /// </summary>
    /// <param name="menu">Menu to be loaded</param>
    /// <param name="doTransition">Play the standard exit transition (Transitions.SwipeIn)?</param>
    public static void LoadMenu(Menu menu, bool doTransition = true)
    {
        intendedMenu = menu;

        if (doTransition)
        {
            //Setup callback
            TransitionControl.onTransitionOver.AddListener(ActuallyLoad);
            //Run transition out
            TransitionControl.RunTransition(TransitionControl.Transitions.SwipeIn);
        }
        else
        {
            //We do this when the game starts when we don't have anything do transition from
            ActuallyLoad();
        }
    }

    private static void ActuallyLoad()
    {
        //Set current menu inactive
        instance.menuObjects[(int)currentMenu].SetActive(false);
        //Set new menu active
        instance.menuObjects[(int)intendedMenu].SetActive(true);

        currentMenu = intendedMenu;

        //Remove transition listener
        TransitionControl.onTransitionOver.RemoveListener(ActuallyLoad);

        //Run transition in
        TransitionControl.RunTransition(TransitionControl.Transitions.SwipeOut);
    }
}
