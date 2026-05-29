using System;
using System.Collections;
using UnityEngine;

public class ResoucesLoadAsync<T> : ILoadAsync<T> where T : UnityEngine.Object
{
    public IEnumerator LoadAsync(string path, Action<T> onComplete)
    {
        ResourceRequest request = Resources.LoadAsync<T>(path);
        yield return request;
        onComplete?.Invoke(request.asset as T);
    }

    public IEnumerator LoadAllAsync(string path, Action<T[]> onComplete)
    {
        T[] assets = Resources.LoadAll<T>(path);
        onComplete?.Invoke(assets);
        yield break;
    }

    public void UnloadAsset(T asset)
    {
        Resources.UnloadAsset(asset);
    }
}
