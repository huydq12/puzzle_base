using System;
using System.Collections;

public interface ILoadAsync<T> where T : UnityEngine.Object
{
    IEnumerator LoadAsync(string path, Action<T> onComplete);
    IEnumerator LoadAllAsync(string path, Action<T[]> onComplete);
    void UnloadAsset(T asset);
}
