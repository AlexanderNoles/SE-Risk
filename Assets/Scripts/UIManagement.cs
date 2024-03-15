using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class UIManagement : MonoBehaviour
{
    static UIManagement instance;
    public TextMeshProUGUI turnInfoText;
    private static MultiObjectPool pools;

    private void Awake()
    {
        instance = this;
        pools = GetComponent<MultiObjectPool>();
    }

    public static MultiObjectPool.ObjectFromPool<T> Spawn<T>(Vector3 pos, int poolIndex)
    {
        return pools.SpawnObject<T>(poolIndex, pos);
    }
    public static void SetText(string text)
    {
        instance.turnInfoText.text = text;
    }
}
