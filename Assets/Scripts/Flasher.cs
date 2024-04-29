using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles flashing UI Elements particular colours
/// </summary>
public class Flasher : MonoBehaviour
{
    Image flashBox;
    Color flashColor;
    float flashDuration;
    GameObject passedGameObject;
    static Flasher instance;

    public void Start()
    {
        instance = this;
    }
    /// <summary>
    /// Starts the passed game object flashing a specified colour for the given duration
    /// </summary>
    /// <param name="color">The colour to flash</param>
    /// <param name="flashDuration">The total duration of the flash</param>
    /// <param name="go">The UI element to flash</param>
    public static void Flash(Color color, float flashDuration,GameObject go)
    {
        instance.flashColor = color;
        instance.flashDuration = flashDuration;
        instance.passedGameObject = go;
        instance.StartCoroutine(nameof(FlashFade));
    }
    /// <summary>
    /// The Coroutine that handles the flashing of the game object
    /// </summary>
    /// <returns></returns>
    private IEnumerator FlashFade()
    {
        GameObject go = new GameObject();
        go.transform.parent = passedGameObject.transform.parent;
        go.transform.position = passedGameObject.transform.position;
        go.transform.rotation = passedGameObject.transform.rotation;

        RectTransform transform = (RectTransform)passedGameObject.transform;
        go.transform.localScale = new Vector3(transform.rect.width*transform.localScale.x / 100, transform.rect.height * transform.localScale.y / 100, 1);
        go.transform.position += Vector3.forward*5;
        go.layer = 5;

        flashBox = go.AddComponent<Image>();
        flashBox.color = flashColor;
        flashBox.canvasRenderer.SetAlpha(1f);

        flashBox.CrossFadeAlpha(0,flashDuration,false);
        AudioManagement.PlaySound("Refuse");
        yield return new WaitForSeconds(flashDuration);

        Destroy(go);
    }

}
