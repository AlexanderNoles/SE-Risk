using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <c>ObjectPoolInfoStorage</c> is attached to a base object passed to an object pool. It can then be used by that object to return itself to its original pool.
/// </summary>
public class ObjectPoolInfoStorage : MonoBehaviour
{
    [HideInInspector]
    /// <summary>
    /// The pool this object originated from. Automatically populated by MultiObjectPool and hidden in inspector. 
    /// </summary>
    public MultiObjectPool originPool;
    /// <summary>
    /// Info describing this object.
    /// </summary>
    public MultiObjectPool.ObjectFromPool<MonoBehaviour> info;

    /// <summary>
    /// Automatically return this object to its original pool after some amount of time? Useful for temporary effects.
    /// </summary>
    public bool automaticallyReturn = false;
    /// <summary>
    /// The time until this object automatically returns (if automaticallyReturn is set to true), measured from the moment it is taken out of its original pool.
    /// </summary>
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

    /// <summary>
    /// Return this object to its original pool.
    /// </summary>
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
