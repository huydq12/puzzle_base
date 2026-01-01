using UnityEngine;
using System.Collections.Generic;
using Object = UnityEngine.Object;

public class Pool
{
    private Transform Holder;
    private GameObject Original;
    private List<GameObject> Active;
    private Queue<GameObject> Inactive;
    public string Tag { get; set; }
    public Pool(GameObject original)
    {
        Original = original;
        Active = new List<GameObject>();
        Inactive = new Queue<GameObject>();
        Holder = new GameObject().transform;
        Holder.name = Original.name + "_Holder";
    }
    public void Return(GameObject gameObject)
    {
        gameObject.transform.SetParent(Holder);
        gameObject.SetActive(false);
        Active.Remove(gameObject);
        Inactive.Enqueue(gameObject);
    }
    public void ReturnAll()
    {
        foreach (var prefab in Active)
        {
            if(prefab!=null)
            {
                prefab.transform.SetParent(Holder);
                prefab.SetActive(false);
                Inactive.Enqueue(prefab);
            }
        }
        Active.Clear();
    }
    public GameObject Get()
    {
        GameObject prefab;
        if (Inactive.Count > 0)
        {
            prefab = Inactive.Dequeue();
            prefab.transform.SetParent(null);
            prefab.SetActive(true);
        }
        else
        {
            prefab = Object.Instantiate(Original);
            prefab.name = Original.name;
        }
        Active.Add(prefab);
        return prefab;
    }
    public void Release()
    {
        ReturnAll();
        Object.Destroy(Holder.gameObject);
        Original = default;
        Inactive.Clear();
    }
}
public static class ObjectPool
{
    private static Dictionary<string, Pool> Pools = new Dictionary<string, Pool>();
    private static Pool GetPool(GameObject gameObject)
    {
        string key = gameObject.name;
        if (!Pools.ContainsKey(key))
            Pools.Add(key, new Pool(gameObject));
        return Pools[key];
    }
    public static T Get<T>(T original, string tag = default) where T : Component
    {
        return Get(original.gameObject, Vector3.zero, Quaternion.identity, null, tag).GetComponent<T>();
    }
    public static T Get<T>(T original, Transform parent, string tag = default) where T : Component
    {
        return Get(original.gameObject, Vector3.zero, Quaternion.identity, parent, tag).GetComponent<T>();
    }
    public static T Get<T>(T original, Vector3 position, Quaternion rotation, string tag = default) where T : Component
    {
        return Get(original.gameObject, position, rotation, null, tag).GetComponent<T>();
    }
    public static T Get<T>(T original, Vector3 position, Quaternion rotation, Transform parent, string tag = default) where T : Component
    {
        return Get(original.gameObject, position, rotation, parent, tag).GetComponent<T>();
    }
    public static GameObject Get(GameObject original, Vector3 position, Quaternion rotation, Transform parent, string tag = default)
    {
        Pool pool = GetPool(original);
        GameObject prefab = pool.Get();
        prefab.transform.position = position;
        prefab.transform.rotation = rotation;
        if (parent != null) prefab.transform.SetParent(parent);
        if (!string.IsNullOrWhiteSpace(tag)) pool.Tag = tag;
        return prefab;
    }
    public static GameObject Get(GameObject original, Vector3 position, Quaternion rotation, string tag = default)
    {
        return Get(original, position, rotation, null, tag);
    }
    public static GameObject Get(GameObject original, Transform parent, string tag = default)
    {
        return Get(original, Vector3.zero, Quaternion.identity, parent, tag);
    }
    public static GameObject Get(GameObject original, string tag = default)
    {
        return Get(original, Vector3.zero, Quaternion.identity, null, tag);
    }
    public static void Return(GameObject gameObject)
    {
        GetPool(gameObject).Return(gameObject);
    }
    public static void ReturnAll(GameObject gameObject)
    {
        string key = gameObject.name;
        if (Pools.ContainsKey(key))
            Pools[key].ReturnAll();
        else Return(gameObject);
    }
    public static void ReturnAll()
    {
        foreach (var pool in Pools)
            pool.Value.ReturnAll();
    }
    public static void ReturnAllTag(string tag)
    {
        foreach (KeyValuePair<string, Pool> pool in Pools)
        {
            if (string.IsNullOrWhiteSpace(pool.Value.Tag)) continue;
            if (pool.Value.Tag.Equals(tag)) pool.Value.ReturnAll();
        }
    }
    public static void Release(GameObject gameObject)
    {
        string key = gameObject.name;
        if (Pools.ContainsKey(key))
        {
            Pools[key].Release();
            Pools.Remove(key);
        }
        else Object.Destroy(gameObject);
    }
    public static void ReleaseAll()
    {
        foreach (var pool in Pools)
            pool.Value.Release();
        Pools.Clear();
    }
}