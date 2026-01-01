using System.Collections.Generic;
using UnityEngine;

public class ReturnToMyPool : MonoBehaviour
{
    public MyPool pool;

    public void OnDisable()
    {
        pool.AddToPool(gameObject);
    }
}

public class MyPool
{
    private Stack<GameObject> stack = new Stack<GameObject>();
    private HashSet<GameObject> inPool = new HashSet<GameObject>();
    private GameObject baseObj;
    private GameObject tmp;
    private ReturnToMyPool returnPool;

    public MyPool(GameObject baseObj)
    {
        this.baseObj = baseObj;
    }

    public GameObject Get()
    {
        while (stack.Count > 0)
        {
            tmp = stack.Pop();
            if (tmp != null)
            {
                inPool.Remove(tmp);
                tmp.SetActive(true);
                return tmp;
            } else
            {
                Debug.LogWarning($"game object with key {baseObj.name} has been destroyed!");
            }
        }
        tmp = GameObject.Instantiate(baseObj);
        returnPool = tmp.AddComponent<ReturnToMyPool>();
        returnPool.pool = this;
        return tmp;
    }

    public void AddToPool(GameObject obj)
    {
        if (obj == null) return;
        if (inPool.Add(obj) == false) return;
        stack.Push(obj);
    }
}

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance;
    private Dictionary<GameObject, MyPool> dicPools = new Dictionary<GameObject, MyPool>();
    GameObject tmp;
    private void Awake()
    {
        Instance = this;
    }
    public GameObject Get(GameObject obj)
    {
        if (dicPools.ContainsKey(obj) == false)
        {
            dicPools.Add(obj, new MyPool(obj));
        }
        return dicPools[obj].Get();
    }
    public GameObject Get(GameObject obj, Vector3 position)
    {
        tmp = Get(obj);
        tmp.transform.position = position;
        return tmp;
    }
    public T Get<T>(T obj) where T : Component
    {
        tmp = Get(obj.gameObject);
        if (tmp == null) return default;
        return tmp.GetComponent<T>();
    }
    public T Get<T>(T obj, Vector3 position) where T : Component
    {
        tmp = Get(obj.gameObject);
        if (tmp == null) return default;
        tmp.transform.position = position;
        return tmp.GetComponent<T>();
    }
}