using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
//using Newtonsoft.Json;
using Sirenix.Utilities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class Common
{
    public static Vector3 With(this Vector3 v, float? x = null, float? y = null, float? z = null)
    {
        v.x = x != null ? (float)x : v.x;
        v.y = y != null ? (float)y : v.y;
        v.z = z != null ? (float)z : v.z;
        return v;
    }
    public static Vector2 With(this Vector2 v, float? x = null, float? y = null)
    {
        v.x = x != null ? (float)x : v.x;
        v.y = y != null ? (float)y : v.y;
        return v;
    }
    public static Color With(this Color color, float? a)
    {
        color.a = a != null ? (float)a : color.a;
        return color;
    }
    public static IEnumerator DelayActionToEndOfFrame(Action callback)
    {
        yield return new WaitForEndOfFrame();
        callback?.Invoke();
    }
    public static IEnumerator DelayActionToNextFrame(Action callback)
    {
        yield return null;
        callback?.Invoke();
    }
    public static IEnumerator DelayActionUntil(Func<bool> condition, Action callback)
    {
        yield return new WaitUntil(condition);
        callback?.Invoke();
    }
    public static IEnumerator DelayAction(float inteval, Action callback)
    {
        if (inteval > 0)
        {
            yield return new WaitForSeconds(inteval);
            callback?.Invoke();
        }
        else
        {
            callback?.Invoke();
        }
    }
 
    public static void RefreshRecursive(this RectTransform rect)
    {
        if (rect == null || !rect.gameObject.activeSelf)
            return;

        // Gọi đệ quy cho toàn bộ con
        foreach (Transform child in rect)
        {
            if (child is RectTransform rectChild)
            {
                RefreshRecursive(rectChild);
            }
        }

        // Refresh LayoutGroup nếu có
        var layoutGroup = rect.GetComponent<LayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.SetLayoutHorizontal();
            layoutGroup.SetLayoutVertical();
        }

        // Refresh ContentSizeFitter nếu có
        var contentSizeFitter = rect.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }
    }
    public static string FormatTime(float totalSeconds)
    {
        int minutes = Mathf.FloorToInt(totalSeconds / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);
        return $"{minutes}:{seconds:D2}";
    }

    public static T GetRandomEnumValue<T>(params T[] excluded) where T : Enum
    {
        T[] allValues = (T[])Enum.GetValues(typeof(T));
        T[] filtered = allValues
            .Where(v => !excluded.Contains(v))
            .ToArray();

        if (filtered.Length == 0)
            throw new InvalidOperationException("No enum values left after exclusion.");

        return filtered[UnityEngine.Random.Range(0, filtered.Length)];
    }
    public static int GetLayerByName(string layerName)
    {
        return LayerMask.NameToLayer(layerName);
    }
    public static Vector3 AnchorPosition(this Transform transform)
    {
        float oldSize = 5;
        float sizeChange = Camera.main.orthographicSize - oldSize;
        return transform.position.With(z: transform.position.z - sizeChange);
    }
    public static IEnumerator WaitForAnimatorState(Animator animator, string stateName)
    {
        yield return null;

        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        while (stateInfo.IsName(stateName) && stateInfo.normalizedTime < 1f)
        {
            yield return null;
            stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        }
    }
    public static bool IsPointerOverObject(string name)
    {
        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            if (result.gameObject.name == name)
                return true;
        }
        return false;
    }
    /*
        public static string ToJson<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public static T FromJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    */
    public static bool IsPointerOverUI()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);

        eventDataCurrentPosition.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

        return results.Count > 0 && results[0].gameObject.TryGetComponent<RectTransform>(out _);
    }
    /*
        public static T DeepCopy<T>(T obj)
        {
            string json = JsonConvert.SerializeObject(obj, Formatting.None,
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    PreserveReferencesHandling = PreserveReferencesHandling.None
                });

            return JsonConvert.DeserializeObject<T>(json,
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                });
    */
#if UNITY_EDITOR
    public static Color GetColorForEnumEditor(ObjectColor value)
    {
        string hex = "#FFFFFF";

        switch (value)
        {
            case ObjectColor.Red:
                hex = "#ff3232d2";
                break;

            case ObjectColor.Green:
                hex = "#b8ff67ff";
                break;

            case ObjectColor.Pink:
                hex = "#F29ADF";
                break;

            case ObjectColor.Purple:
                hex = "#9E7DEE";
                break;

            case ObjectColor.Cyan:
                hex = "#79D6F5";
                break;

            case ObjectColor.Blue:
                hex = "#0059E6";
                break;

            case ObjectColor.Yellow:
                hex = "#F5D67A";
                break;

            case ObjectColor.Orange:
                hex = "#F5A57A";
                break;

            case ObjectColor.Brown:
                hex = "#813d3dff";
                break;

            case ObjectColor.Teal:
                hex = "#6bc5b6ff";
                break;
        }

        if (UnityEngine.ColorUtility.TryParseHtmlString(hex, out var color))
            return color;

        return Color.white;
    }

    public static ObjectColor DrawObjectColor(ObjectColor value, GUIContent label)
    {
        Color textColor = GetColorForEnumEditor(value);
        GUIStyle style = new GUIStyle(UnityEditor.EditorStyles.popup)
        {
            normal = { textColor = textColor },
            fontStyle = FontStyle.Bold
        };

        return (ObjectColor)UnityEditor.EditorGUILayout.EnumPopup(label, value, style);
    }
    public static ObjectColor DrawObjectColor(ObjectColor value, GUIContent label, params GUILayoutOption[] options)
    {
        Color textColor = GetColorForEnumEditor(value);
        GUIStyle style = new GUIStyle(UnityEditor.EditorStyles.popup)
        {
            normal = { textColor = textColor },
            fontStyle = FontStyle.Bold
        };

        return (ObjectColor)UnityEditor.EditorGUILayout.EnumPopup(label, value, style, options);
    }

    public static ObjectColor DrawObjectColor(Rect rect, ObjectColor value)
    {
        // Lấy màu cho text
        Color textColor = GetColorForEnumEditor(value);

        // Tạo style với màu text
        GUIStyle style = new GUIStyle(UnityEditor.EditorStyles.popup)
        {
            normal = { textColor = textColor },
            fontStyle = FontStyle.Bold
        };
        return (ObjectColor)UnityEditor.EditorGUI.EnumPopup(rect, value, style);
    }
#endif
}