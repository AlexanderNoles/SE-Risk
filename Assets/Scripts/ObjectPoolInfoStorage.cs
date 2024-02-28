using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolInfoStorage : MonoBehaviour
{
    [HideInInspector]
    public MultiObjectPool originPool;
    public MultiObjectPool.ObjectFromPool<MonoBehaviour> info;

    public bool automaticallyReturn = false;
    public float timeTillAutoReturn;

    private void OnEnable()
    {
        StopAllCoroutines();
        if (automaticallyReturn)
        {
            StartCoroutine(nameof(AutoReturn));
        }
    }

    private IEnumerator AutoReturn()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(timeTillAutoReturn);
        ReturnSelf();
    }

    public void ReturnSelf()
    {
        if (originPool == null)
        {
            return;
        }

        MultiObjectPool tempPool = originPool;
        originPool = null;

        tempPool.ReturnObject(info);
    }
}
