using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

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
    public static void Flash(Color color, float flashDuration,GameObject go)
    {
        instance.flashColor = color;
        instance.flashDuration = flashDuration;
        instance.passedGameObject = go;
        instance.StartCoroutine(nameof(FlashFade));
    }
    public IEnumerator FlashFade()
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
