using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MonitorBreak;
using UnityEngine.UI;
using UnityEngine.Events;

[IntializeAtRuntime("TransitionCanvas")]
public class TransitionControl : MonoBehaviour
{
    private static TransitionControl instance;
    public static UnityEvent onTransitionOver = new UnityEvent();
    public Image swipeImage;
    public RectTransform maskedUI;
    public AnimationCurve swipeCurve;

    private static bool swipeIn;
    private static float swipeT;

    public GameObject raycastBlocker;

    public enum Transitions
    {
        SwipeIn,
        SwipeOut
    }

    private void Awake()
    {
        instance = this;
    }

    public static void RunTransition(Transitions transitions)
    {
        instance.raycastBlocker.SetActive(true);
        if (transitions == Transitions.SwipeIn)
        {
            swipeIn = true;
            swipeT = 1.0f;
        }
        else if (transitions == Transitions.SwipeOut)
        {
            swipeIn = false;
            swipeT = 1.0f;
        }
    }

    public static void HideMaskedTextForNextTransition()
    {
        instance.maskedUI.gameObject.SetActive(false);
        onTransitionOver.AddListener(ShowMaskText);
    }

    public static void ShowMaskText()
    {
        instance.maskedUI.gameObject.SetActive(true);
        onTransitionOver.RemoveListener(ShowMaskText);
    }

    private void Update()
    {
        if (swipeT > 0.0f)
        {
            swipeT -= Time.deltaTime * 2.0f;
            if (swipeIn)
            {
                swipeImage.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(0, -800, swipeCurve.Evaluate(swipeT)), 0);
            }
            else
            {
                swipeImage.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(800, 0, swipeCurve.Evaluate(swipeT)), 0);
            }

            maskedUI.transform.position = transform.position;

            if (swipeT <= 0.0f)
            {
                EndTransition();
            }
        }
    }

    private void EndTransition()
    {
        //Turn off raycast blocker before calling on transition over
        //in case a class wants to immediately call another transiton after this one completes
        raycastBlocker.SetActive(false);
        onTransitionOver.Invoke();
    }
}
