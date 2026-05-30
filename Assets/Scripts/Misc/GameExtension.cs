using System;
using System.Collections;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using UnityEngine;

public static class GameExtension
{
    static Dictionary<float, WaitForSeconds> Yields = new Dictionary<float, WaitForSeconds>();
    static Dictionary<float, WaitForSecondsRealtime> CustomYields = new Dictionary<float, WaitForSecondsRealtime>();
    public static Coroutine Schedule(this MonoBehaviour monoBehaviour, float seconds, bool unscaledTime, Action task)
    {
        return monoBehaviour.StartCoroutine(DoTask(seconds, unscaledTime, task));
    }
    public static Coroutine Schedule(this MonoBehaviour monoBehaviour, float seconds, Action task)
    {
        return monoBehaviour.StartCoroutine(DoTask(seconds, false, task));
    }
    public static WaitForSeconds GetWaitForSeconds(float seconds)
    {
        if (!Yields.ContainsKey(seconds))
            Yields.Add(seconds, new WaitForSeconds(seconds));
        return Yields[seconds];
    }
    public static WaitForSecondsRealtime GetWaitForSecondsRealtime(float seconds)
    {
        if (!CustomYields.ContainsKey(seconds))
            CustomYields.Add(seconds, new WaitForSecondsRealtime(seconds));
        return CustomYields[seconds];
    }
    private static IEnumerator DoTask(float seconds, bool unscaledTime, Action task)
    {
        if (unscaledTime) yield return GetWaitForSecondsRealtime(seconds);
        else yield return GetWaitForSeconds(seconds);
        task?.Invoke();
    }
    public static void Release<T>(this List<T> list) where T : Component
    {
        foreach (var item in list)
            item.SetActive(false);
    }
    public static T Get<T>(this List<T> list, T prefab, Transform parent) where T : Component
    {
        foreach (var item in list)
        {
            if (item.gameObject.activeSelf) continue;
            item.SetParent(parent);
            item.SetActive(true);
            return item;
        }
        var clone = Object.Instantiate(prefab);
        clone.name = prefab.name;
        clone.SetParent(parent);
        list.Add(clone);
        return clone;
    }
    public static void SetActive(this Component component, bool value)
    {
        component.gameObject.SetActive(value);
    }
    public static void SetParent(this Component component, Transform parent)
    {
        component.transform.SetParent(parent);
        component.transform.localScale = Vector3.one;
        component.transform.localPosition = Vector3.zero;
    }
    public static Vector3 RotateAroundPivot(this Vector3 vector3, Vector3 pivot, Vector3 angle)
    {
        return Quaternion.Euler(angle) * (vector3 - pivot) + pivot;
    }
    public static void CullingMaskToggle(this Camera camera, string layerName)
    {
        camera.cullingMask ^= 1 << LayerMask.NameToLayer(layerName);
    }
    public static void CullingMaskEnable(this Camera camera, string layerName)
    {
        camera.cullingMask |= 1 << LayerMask.NameToLayer(layerName);
    }
    public static void CullingMaskDisable(this Camera camera, string layerName)
    {
        camera.cullingMask &= ~(1 << LayerMask.NameToLayer(layerName));
    }
    public static int GetValue(this LayerMask layerMask)
    {
        if (layerMask.value == 0) return 0;
        LayerMask localMask = layerMask;
        int result = 1;
        while (true)
        {
            localMask = localMask >> 1;
            if ((localMask & 1) == 1) break;
            else result++;
        }
        return result;
    }
    public static void Shuffle<T>(this IList<T> list)
    {
        for (int i = 0; i < list.Count - 1; i++)
        {
            int index = Random.Range(0, list.Count);
            var temp = list[i];
            list[i] = list[index];
            list[index] = temp;
        }
    }
}