using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManagement : MonoBehaviour
{
    private static MenuManagement instance;

    public enum Menu
    {
        Main,
        Play,
        Options
    }

    public static Menu currentMenu = Menu.Main;
    private static Menu intendedMenu = Menu.Main;
    public List<GameObject> menuObjects = new List<GameObject>();

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        //In start to not run into conflicts with instance identification in transition control
        LoadMenu(Menu.Main, false);
        TransitionControl.HideMaskedTextForNextTransition();
        TransitionControl.RunTransition(TransitionControl.Transitions.SwipeOut);
    }

    public void LoadMainMenuButton()
    {
        AudioManagement.PlaySound("ButtonPress");
        LoadMenu(Menu.Main);
    }

    public void LoadPlayMenuButton()
    {
        AudioManagement.PlaySound("ButtonPress");
        LoadMenu(Menu.Play);
    }

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

    public void LoadOptionsButton()
    {
        AudioManagement.PlaySound("ButtonPress");
        LoadMenu(Menu.Options);
    }

    public static void LoadMenu(Menu menu, bool doTransition = true)
    {
        if (SceneManager.GetActiveScene().buildIndex != 1)
        {
            //Load menu scene
            SceneManager.LoadScene(1);
        }

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
