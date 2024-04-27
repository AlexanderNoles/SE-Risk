using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MonitorBreak;
using UnityEngine.UI;
using UnityEngine.Events;

[IntializeAtRuntime("TransitionCanvas")]
/// <summary>
/// <c>TransitionControl</c> handles all screen transitions. It uses <c>IntializeAtRuntime</c> to load and instantiate the prefab containing the transition UI from Resources. Access point is RunTransition.
/// </summary>
public class TransitionControl : MonoBehaviour
{
    private static TransitionControl instance;
    /// <summary>
    /// Start event run on transition end.
    /// </summary>
    public static UnityEvent onTransitionOver = new UnityEvent();
    /// <summary>
    /// UI Reference. Setup in insepector.
    /// </summary>
    public Image swipeImage;
    /// <summary>
    /// UI Reference. Setup in insepector.
    /// </summary> 
    public RectTransform maskedUI;
    /// <summary>
    /// Animation Curve representing easing function for swipe transition. Setup in insepector.
    /// </summary>
    public AnimationCurve swipeCurve;

    private static bool swipeIn;
    private static float swipeT;

    private static float animationSpeed = 1.0f;

    /// <summary>
    /// UI Reference. Setup in insepector. Used to block button inputs when transition is playing. 
    /// </summary>
    public GameObject raycastBlocker;

    /// <summary>
    /// Enum representing all possible transitions.
    /// </summary>
    public enum Transitions
    {
        SwipeIn,
        SwipeOut
    }

    private void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// Runs a transition when called. 
    /// </summary>
    /// <param name="transitions">The transition to be called.</param>
    public static void RunTransition(Transitions transitions, float transitionSpeed = 1.0f)
    {
        animationSpeed = transitionSpeed;
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

    /// <summary>
    /// Hide the masked UI for the next transition. This will make the transition effect look like just a black screen.
    /// </summary>
    public static void HideMaskedTextForNextTransition()
    {
        instance.maskedUI.gameObject.SetActive(false);
        onTransitionOver.AddListener(ShowMaskText);
    }

    /// <summary>
    /// Show the masked UI, if it is hidden. Run automatically on transition end if masked UI was hidden for this transition.
    /// </summary>
    public static void ShowMaskText()
    {
        instance.maskedUI.gameObject.SetActive(true);
        onTransitionOver.RemoveListener(ShowMaskText);
    }

    private void Update()
    {
        if (swipeT > 0.0f)
        {
            swipeT -= Time.deltaTime * 2.0f * animationSpeed;
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
