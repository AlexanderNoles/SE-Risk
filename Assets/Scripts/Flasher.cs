using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Flasher : MonoBehaviour
{
    Image flashBox;
    Color flashColor;
    float flashDuration;
    public void Flash(Color color, float flashDuration)
    {
        flashColor = color;
        this.flashDuration = flashDuration;
        StartCoroutine(nameof(FlashFade));
    }
    public IEnumerator FlashFade()
    {
        Debug.Log("HI");
        GameObject go = new GameObject();
        go.transform.parent = this.transform.parent;
        go.transform.position = this.transform.position;
        go.transform.rotation = this.transform.rotation;
        RectTransform transform = (RectTransform)gameObject.transform;
        go.transform.localScale = new Vector3(transform.rect.width*transform.localScale.x / 100, transform.rect.height * transform.localScale.y / 100, 1);
        go.transform.position += Vector3.forward*5;
        flashBox = go.AddComponent<Image>();
        go.layer = 5;
        flashBox.color = flashColor;
        flashBox.canvasRenderer.SetAlpha(0f);
        flashBox.CrossFadeAlpha(1,flashDuration/2,false);
        yield return new WaitForSeconds(flashDuration/4);
        AudioManagement.PlaySound("Refuse");
        yield return new WaitForSeconds(flashDuration / 4);
        flashBox.CrossFadeAlpha(0f, flashDuration / 2, false);
        yield return new WaitForSeconds(flashDuration / 2);
        Destroy(go);
    }

}
