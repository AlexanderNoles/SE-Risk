using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MultiObjectPool : MonoBehaviour
{
    [Tooltip("Automatically Generate Objects On Start")]
    public bool GenerateOnStart = false;
    private Transform selfTransform;

    private void Awake()
    {
        selfTransform = transform;
        foreach (ObjectPool pool in pools)
        {
            pool.Setup(selfTransform, GenerateOnStart);
        }
    }

    public void AddNewPools(List<int> numbers, List<GameObject> baseObjects)
    {
        for (int i = 0; i < numbers.Count; i++)
        {
            AddNewPool(numbers[i], baseObjects[i]);
        }
    }

    public int AddNewPool(int numberOfObjects, GameObject baseObject, string name = "")
    {
        if (selfTransform == null)
        {
            throw new System.Exception("Function run too early! Run in Start instead.");
        }

        int currentNumberOfPools = pools.Count;
        if (name == "")
        {
            name = currentNumberOfPools.ToString();
        }

        pools.Add(new ObjectPool(numberOfObjects, baseObject, name, selfTransform));

        return currentNumberOfPools;
    }

    [System.Serializable]
    public class ObjectPool
    {
        [InspectorName("Pool Name")]
        public string name;
        public GameObject baseObject;
        public int baseNumberOfObjects;
        [Tooltip("Generate extra objects when the pool is empty but an object is required")]
        public bool generateAsNeeded = false;
        public Stack<Transform> activeObjects;
        private Transform actualTransform;
        public bool automaticallySetParent = true;

        public ObjectPool(int baseNumberOfObjects, GameObject baseObject, string name, Transform actualTransform)
        {
            this.baseNumberOfObjects = baseNumberOfObjects;
            this.baseObject = baseObject;
            this.name = name;

            Setup(actualTransform, true);
        }

        public void Setup(Transform actualTransform, bool generateObjects)
        {
            this.actualTransform = actualTransform;

            if (name == "")
            {
                throw new System.Exception("This pool doesn't have a name!");
            }

            if (!generateObjects)
            {
                return;
            }

            activeObjects = new Stack<Transform>();
            baseObject.SetActive(false); //Set inactive so that on enable doesn't run when the new objects are instantiated

            for (int i = 0; i < baseNumberOfObjects; i++)
            {
                GenerateObject();
            }

            baseObject.SetActive(true); //Set active because this effects the base prefab. This means it would show up as hidden in editor
        }

        public Transform GenerateObject()
        {
            GameObject currentObject = Instantiate(baseObject, actualTransform);

            Transform transform = currentObject.transform;
            activeObjects.Push(transform);
            return transform;
        }

        public Transform GetObject()
        {
            if (activeObjects.Count == 0)
            {
                if (generateAsNeeded)
                {
                    GenerateObject();
                }
                else
                {
                    return null;
                }
            }

            return activeObjects.Pop();
        }

        public void ReturnObject(Transform _object)
        {
            if (automaticallySetParent)
            {
                _object.SetParent(actualTransform);
            }
            _object.gameObject.SetActive(false);

            activeObjects.Push(_object);
        }

        public Dictionary<Transform, T> GetComponentsOnActiveObjects<T>()
        {
            Dictionary<Transform, T> toReturn = new Dictionary<Transform, T>();
            Transform[] allObjects = GetActiveObjectsAsArray();

            foreach (Transform _obj in allObjects)
            {
                toReturn.Add(_obj, _obj.GetComponent<T>());
            }

            return toReturn;
        }

        public Transform[] GetActiveObjectsAsArray()
        {
            return activeObjects.ToArray();
        }
    }

    public List<ObjectPool> pools = new List<ObjectPool>();

    public int GetPoolIndex(string poolName)
    {
        int numOfPools = pools.Count;

        for (int i = 0; i < numOfPools; i++)
        {
            if (pools[i].name == poolName)
            {
                return i;
            }
        }

        throw new System.Exception("No pool with that name in multi pool");
    }

    public ObjectFromPool<MonoBehaviour> GetObject(string poolName)
    {
        return GetObject<MonoBehaviour>(poolName);
    }

    public ObjectFromPool<T> GetObject<T>(string poolName)
    {
        return GetObject<T>(GetPoolIndex(poolName));
    }

    public struct ObjectFromPool<T>
    {
        public int poolIndex;
        public Transform transform;
        public T component;

        public ObjectFromPool(int poolIndex, Transform transform, T component)
        {
            this.poolIndex = poolIndex;
            this.transform = transform;
            this.component = component;
        }
    }

    public ObjectFromPool<MonoBehaviour> GetObject(int poolIndex)
    {
        return GetObject<MonoBehaviour>(poolIndex);
    }

    public ObjectFromPool<T> GetObject<T>(int poolIndex)
    {
        Transform newObject = pools[poolIndex].GetObject();
        if (newObject == null)
        {
            throw new System.Exception("No Objects left in pool!");
        }
        else
        {
            newObject.TryGetComponent(out T component);

            return new ObjectFromPool<T>(poolIndex, newObject, component);
        }
    }


    /// <summary>
    /// Unlike GetObject this will automatically update the information on the object and set it active
    /// </summary>
    public ObjectFromPool<T> SpawnObject<T>(int poolIndex, Vector3 position)
    {
        //Return a blank if there is no object
        ObjectFromPool<T> toReturn = new ObjectFromPool<T>();

        Transform newObject = pools[poolIndex].GetObject();
        if (newObject != null)
        {
            newObject.position = position;
            newObject.gameObject.SetActive(true);

            //Setup toReturn
            toReturn.poolIndex = poolIndex;
            toReturn.transform = newObject;

            if (typeof(T) != typeof(MonoBehaviour))
            {
                newObject.TryGetComponent(out T component);
                toReturn.component = component;
            }

            try
            {
                ObjectPoolInfoStorage objectPoolInfoStorage = newObject.GetComponent<ObjectPoolInfoStorage>();
                objectPoolInfoStorage.info = new ObjectFromPool<MonoBehaviour>(poolIndex, newObject, null);
                objectPoolInfoStorage.originPool = this;
            }
            catch (System.NullReferenceException)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"WARNING: There is no {nameof(ObjectPoolInfoStorage)} attached to the new object. This means it cannot return itself to the object pool!");
#endif
            }
        }

        return toReturn;
    }

    public ObjectFromPool<T> SpawnObject<T>(Enum enumEntry, Vector3 position)
    {
        return SpawnObject<T>(Convert.ToInt32(enumEntry), position);
    }

    public ObjectFromPool<MonoBehaviour> SpawnObject(int poolIndex, Vector3 position)
    {
        return SpawnObject<MonoBehaviour>(poolIndex, position);
    }

    public ObjectFromPool<MonoBehaviour> SpawnObject(Enum enumEntry, Vector3 position)
    {
        return SpawnObject<MonoBehaviour>(enumEntry, position);
    }

    public void ReturnObject(string poolName, Transform _object)
    {
        ReturnObject(GetPoolIndex(poolName), _object);
    }

    public void ReturnObject(ObjectFromPool<MonoBehaviour> objectFromPool)
    {
        pools[objectFromPool.poolIndex].ReturnObject(objectFromPool.transform);
    }

    public void ReturnObject(int poolIndex, Transform _object)
    {
        pools[poolIndex].ReturnObject(_object);
    }

    public void IncreasePoolCapacity(string poolName, int amount = 1)
    {
        IncreasePoolCapacity(GetPoolIndex(poolName), amount);
    }

    public void IncreasePoolCapacity(int poolIndex, int amount = 1)
    {
        for (int i = 0; i < amount; i++)
        {
            pools[poolIndex].GenerateObject();
        }
    }

    public Dictionary<Transform, T> GetComponentsOnAllActiveObjects<T>(int poolIndex)
    {
        return pools[poolIndex].GetComponentsOnActiveObjects<T>();
    }
}
