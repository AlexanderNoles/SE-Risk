using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// <c>MultiObjectPool</c> a generic object pooling solution meant to reduce garbage collector spikes in unity. Contains multiple object pools rather than just one so it can contain defined sets of different objects.
/// </summary>
public class MultiObjectPool : MonoBehaviour
{
    [Tooltip("Automatically Generate Objects On Start")]
    /// <summary>
    /// Generate the objects in the pools on Start. If false objects must be pre-generated.
    /// </summary>
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

    /// <summary>
    /// Add a set of new pools to the MultiObjectPool at Runtime.
    /// </summary>
    /// <param name="numbers">The number of objects for each pool.</param>
    /// <param name="baseObjects">The actual objects that will be in the pools. Used as bases to instantiate more.</param>
    public void AddNewPools(List<int> numbers, List<GameObject> baseObjects)
    {
        for (int i = 0; i < numbers.Count; i++)
        {
            AddNewPool(numbers[i], baseObjects[i]);
        }
    }

    /// <summary>
    /// Add a single new pool to the MultiObjectPool at Runtime.
    /// </summary>
    /// <param name="numberOfObjects">Number of objects that should be in the new pool.</param>
    /// <param name="baseObject">The base object all other objets will be instantiated off of.</param>
    /// <param name="name">Name of the new pool. If blank, pool will be assigned a number instead.</param>
    /// <returns>The index of the new pool</returns>
    /// <exception cref="System.Exception">Thrown when function is called too early and MultiObjectPool has not been setup yet.</exception>
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
    /// <summary>
    /// A serializable class containg all the information about a given pool.
    /// </summary>
    public class ObjectPool
    {
        [InspectorName("Pool Name")]
        /// <summary>
        /// The name of the Pool.
        /// </summary>
        public string name;
        /// <summary>
        /// The base object, objects in the pool are generated based on this.
        /// </summary>
        public GameObject baseObject;
        /// <summary>
        /// The original number of objects in the pool when it is generated.
        /// </summary>
        public int baseNumberOfObjects;
        [Tooltip("Generate extra objects when the pool is empty but an object is required")]
        /// <summary>
        /// Should a new object be generated if one is request when the pool is empty?
        /// </summary>
        public bool generateAsNeeded = false;
        /// <summary>
        /// Stack of all objects currently in the pool.
        /// </summary>
        public Stack<Transform> activeObjects;
        private Transform actualTransform;
        /// <summary>
        /// Automatically set the parent of an object when it is returned to the pool? Useful for UI elements.
        /// </summary>
        public bool automaticallySetParent = true;

        /// <summary>
        /// Standard ObjectPool constructor
        /// </summary>
        /// <param name="baseNumberOfObjects">Original number of objects in the new pool.</param>
        /// <param name="baseObject">Object that should fill the new pool.</param>
        /// <param name="name">Name of the new pool.</param>
        /// <param name="actualTransform">Parent of the new pool. The transform the objects are attached too.</param>
        public ObjectPool(int baseNumberOfObjects, GameObject baseObject, string name, Transform actualTransform)
        {
            this.baseNumberOfObjects = baseNumberOfObjects;
            this.baseObject = baseObject;
            this.name = name;

            Setup(actualTransform, true);
        }

        /// <summary>
        /// Setup the pool. Run automatically from constructor.
        /// </summary>
        /// <param name="actualTransform">New parent of the pool.</param>
        /// <param name="generateObjects">Actual generate objects when creating the new pool? Or just leave it empty?</param>
        /// <exception cref="System.Exception">Thrown when the pool has not been given a proper name.</exception>
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

        /// <summary>
        /// Add an object to the pool.
        /// </summary>
        /// <returns>The Transform of the newly instantiated object.</returns>
        public Transform GenerateObject()
        {
            GameObject currentObject = Instantiate(baseObject, actualTransform);

            Transform transform = currentObject.transform;
            activeObjects.Push(transform);
            return transform;
        }

        /// <summary>
        /// Pop an object in the pool from the top of the stack.
        /// </summary>
        /// <returns>The Transform of the object or null if none are avaliable.</returns>
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

        /// <summary>
        /// Return an object to the pool, pushing it onto the stack.
        /// </summary>
        /// <param name="_object">The object being returned.</param>
        public void ReturnObject(Transform _object)
        {
            if (automaticallySetParent)
            {
                _object.SetParent(actualTransform);
            }
            _object.gameObject.SetActive(false);

            activeObjects.Push(_object);
        }

        /// <summary>
        /// Gets Component from all the objects currently in the pool. 
        /// </summary>
        /// <typeparam name="T">The Component to get.</typeparam>
        /// <returns>A Dictionary with structure [key: Object Transform, value: The requested component]</returns>
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

        /// <summary>
        /// Get all objects in the pool as an array.
        /// </summary>
        /// <returns>The objects in the pool as an array.</returns>
        public Transform[] GetActiveObjectsAsArray()
        {
            return activeObjects.ToArray();
        }
    }

    /// <summary>
    /// All pools in this MultiObjectPool. Displayed as a serialized list in the inspector for easy editing.
    /// </summary>
    public List<ObjectPool> pools = new List<ObjectPool>();

    /// <summary>
    /// Get the index of a pool based on the name.
    /// </summary>
    /// <param name="poolName">The name of the pool that is being searched for.</param>
    /// <returns>The index of the pool.</returns>
    /// <exception cref="System.Exception">Thrown when a pool with that name does not exist.</exception>
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

    /// <summary>
    /// Get an object from a pool based on the name. Non-generic.
    /// </summary>
    /// <param name="poolName">The name of the target pool.</param>
    /// <returns>An Object, in the form of ObjectFromPool.</returns>
    public ObjectFromPool<MonoBehaviour> GetObject(string poolName)
    {
        return GetObject<MonoBehaviour>(poolName);
    }

    /// <summary>
    /// Get an object from a pool based on the name. Allows you to specify a component to grab.
    /// </summary>
    /// <param name="poolName">The name of the target pool.</param>
    /// <returns>An Object, in the form of ObjectFromPool.</returns>
    public ObjectFromPool<T> GetObject<T>(string poolName)
    {
        return GetObject<T>(GetPoolIndex(poolName));
    }

    /// <summary>
    /// The majority return type of the MultiObjectPool functions. Contains all the data about an object taken from a pool.
    /// </summary>
    /// <typeparam name="T">The component that was retrieved from the object.</typeparam>
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

    /// <summary>
    /// Get an object from a pool based on the index. Non-generic.
    /// </summary>
    /// <param name="poolIndex">The index of the target pool.</param>
    /// <returns>An Object, in the form of ObjectFromPool.</returns>
    public ObjectFromPool<MonoBehaviour> GetObject(int poolIndex)
    {
        return GetObject<MonoBehaviour>(poolIndex);
    }

    /// <summary>
    /// Get an object from a pool based on the index. Allows you to specify a component to grab.
    /// </summary>
    /// <param name="poolIndex">The index of the target pool.</param>
    /// <returns>An Object, in the form of ObjectFromPool.</returns>
    /// <exception cref="System.Exception">Thrown when the target pool is empty.</exception>
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
    /// Get an object from a pool based on the index. Unlike GetObject this will automatically update the information on the object, set it active and put it at a position.
    /// </summary>
    /// <typeparam name="T">The type of the component to grab.</typeparam>
    /// <param name="poolIndex">The index of the target pool.</param>
    /// <param name="position">The target position.</param>
    /// <returns>An Object, in the form of ObjectFromPool.</returns>
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

    /// <summary>
    /// Wrapper for <c>SpawnObject</c> that converts an enum to an index.
    /// </summary>
    /// <typeparam name="T">The type of the component to grab.</typeparam>
    /// <param name="enumEntry">The enum to convert to an int.</param>
    /// <param name="position">The target position.</param>
    /// <returns>An Object, in the form of ObjectFromPool.</returns>
    public ObjectFromPool<T> SpawnObject<T>(Enum enumEntry, Vector3 position)
    {
        return SpawnObject<T>(Convert.ToInt32(enumEntry), position);
    }

    /// <summary>
    /// Wrapper for <c>SpawnObject</c>. Non-Generic.
    /// </summary>
    /// <param name="poolIndex">The index of the target pool.</param>
    /// <param name="position">The target position.</param>
    /// <returns>An Object, in the form of ObjectFromPool.</returns>
    public ObjectFromPool<MonoBehaviour> SpawnObject(int poolIndex, Vector3 position)
    {
        return SpawnObject<MonoBehaviour>(poolIndex, position);
    }

    /// <summary>
    /// Wrapper for <c>SpawnObject</c> that converts an enum to an index. Non-generic.
    /// </summary>
    /// <param name="enumEntry">The enum to convert to an int.</param>
    /// <param name="position">The target position.</param>
    /// <returns>An Object, in the form of ObjectFromPool.</returns>
    public ObjectFromPool<MonoBehaviour> SpawnObject(Enum enumEntry, Vector3 position)
    {
        return SpawnObject<MonoBehaviour>(enumEntry, position);
    }

    /// <summary>
    /// Returns an object to a pool of a given name.
    /// </summary>
    /// <param name="poolName">Target pool.</param>
    /// <param name="_object">The object being returned.</param>
    public void ReturnObject(string poolName, Transform _object)
    {
        ReturnObject(GetPoolIndex(poolName), _object);
    }

    /// <summary>
    /// Returns an object to a pool. Pool is pulled automatically from the ObjectFromPool object.
    /// </summary>
    /// <param name="objectFromPool">The object being returned.</param>
    public void ReturnObject(ObjectFromPool<MonoBehaviour> objectFromPool)
    {
        pools[objectFromPool.poolIndex].ReturnObject(objectFromPool.transform);
    }

    /// <summary>
    /// Returns an object to a pool of a given index.
    /// </summary>
    /// <param name="poolIndex">Target pool.</param>
    /// <param name="_object">The object being returned.</param>
    public void ReturnObject(int poolIndex, Transform _object)
    {
        pools[poolIndex].ReturnObject(_object);
    }

    /// <summary>
    /// Increases pool capacity. Useful when you don't want a pool to just keep automatically generating new objects when asked to provide one but need an intially indeterminate number.
    /// </summary>
    /// <param name="poolName">Name of the target pool.</param>
    /// <param name="amount">Amount to increase the pool by.</param>
    public void IncreasePoolCapacity(string poolName, int amount = 1)
    {
        IncreasePoolCapacity(GetPoolIndex(poolName), amount);
    }

    /// <summary>
    /// Increases pool capacity. Useful when you don't want a pool to just keep automatically generating new objects when asked to provide one but need an intially indeterminate number.
    /// </summary>
    /// <param name="poolIndex">Index of the target pool.</param>
    /// <param name="amount">Amount to increase the pool by.</param>
    public void IncreasePoolCapacity(int poolIndex, int amount = 1)
    {
        for (int i = 0; i < amount; i++)
        {
            pools[poolIndex].GenerateObject();
        }
    }

    /// <summary>
    /// Get a component from every object in a pool of a given index.
    /// </summary>
    /// <typeparam name="T">The component to grab.</typeparam>
    /// <param name="poolIndex">The index of the target pool.</param>
    /// <returns>A Dictionary with structure [key: Object Transform, value: The requested component]</returns>
    public Dictionary<Transform, T> GetComponentsOnAllActiveObjects<T>(int poolIndex)
    {
        return pools[poolIndex].GetComponentsOnActiveObjects<T>();
    }
}
