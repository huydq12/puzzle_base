using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : Component
{
    private static T Current;
    public static T Instance
    {
        get
        {
            if (Current == null) Current = FindFirstObjectByType<T>();
            if (Current == null) Current = new GameObject(typeof(T).Name).AddComponent<T>();
            return Current;
        }
    }
    private void ClearInstance()
    {
        if (Current == this) Current = null;
    }
    protected virtual void Awake()
    {
        if (Current == null) Current = this as T;
        else if (Current != this) Destroy(gameObject);
    }
    protected virtual void OnDestroy() => ClearInstance();
    protected virtual void OnApplicationQuit() => ClearInstance();
}