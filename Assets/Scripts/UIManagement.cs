using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManagement : MonoBehaviour
{
    private static MultiObjectPool pools;

    private void Awake()
    {
        pools = GetComponent<MultiObjectPool>();
    }

    public static MultiObjectPool.ObjectFromPool<T> Spawn<T>(Vector3 pos, int poolIndex)
    {
        return pools.SpawnObject<T>(poolIndex, pos);
    }
}
