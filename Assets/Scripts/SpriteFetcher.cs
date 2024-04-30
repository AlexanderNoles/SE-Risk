using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using MonitorBreak.Bebug;
public class SpriteFetcher : MonoBehaviour
{
    //The name of the sprite that this sprite fetcher is fetching
    [SerializeField]
    public string path;
    [SerializeField]
    public string folder;
    public static string spriteDir = "/Sprites" ;

    public void Start()
    {
#if !UNITY_EDITOR
        SetGameObjectSpriteFromFile();
#endif
    }
    public Sprite GetSprite()
    {
        Texture2D spriteTexture = null;
        string fullPath = Application.dataPath+spriteDir + "/" + folder+"/"+path;
        if (File.Exists(fullPath))
        {
            byte[] data = File.ReadAllBytes(fullPath);
            spriteTexture = new Texture2D(1, 1);
            spriteTexture.LoadImage(data);
            Vector2 pivot;
            if (gameObject.layer == 5)
            {
                pivot = new Vector2(spriteTexture.width / 2, spriteTexture.height / 2);
            }
            else
            {
                pivot = new Vector2(0.5f, 0.5f);
            }
            Sprite sprite = Sprite.Create(spriteTexture, new Rect(0, 0, spriteTexture.width, spriteTexture.height),pivot);
            return sprite;
        }
        else
        {
            Console.Log(fullPath);
            return null;
        }
    }

    public void SetGameObjectSpriteFromFile()
    {
        Sprite sprite = GetSprite();
        SpriteRenderer sr = gameObject.GetComponent<SpriteRenderer>();
        Image ig = gameObject.GetComponent<Image>();
        if (sprite != null)
        {
            if(sr != null)
            {
                sr.sprite = sprite;
            }
            else
            {
                ig.sprite = sprite;
            }
        }
    }
}
