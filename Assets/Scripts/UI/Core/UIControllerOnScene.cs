using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIControllerOnScene<T> : IUIController where T : BaseUIElement
{
    private readonly Dictionary<Type, T> elements = new();

    private T currentElement;

    public BaseUIElement CurrentElement => currentElement;

    // ========================= REGISTER =========================
    public void AddElement<Y>(Type elementType, Y element) where Y : BaseUIElement
    {
        if (element == null) return;
        if (!typeof(T).IsAssignableFrom(elementType)) return;

        if (!elements.ContainsKey(elementType))
        {
            elements.Add(elementType, element as T);
        }
    }

    public BaseUIElement GetExistElement(Type elementType)
    {
        elements.TryGetValue(elementType, out var element);
        return element;
    }

    // ========================= SHOW =========================
    public async UniTask Show(Type elementType, object data, bool forceShowData, bool hideCurrent, Action onStartShowAction = null)
    {
        if (!elements.TryGetValue(elementType, out var element))
        {
            Debug.LogWarning($"[UIOnScene] Not found: {elementType.Name}");
            return;
        }

        bool shouldShow = forceShowData || element.IsHide;
        bool isManualScreen = element is BaseScreen baseScreen && baseScreen.ManualHide;

        if (!isManualScreen)
        {
            currentElement = element;
        }

        onStartShowAction?.Invoke();

        if (shouldShow)
        {
            element.SetObjectData(data);
            await element.ShowAsync();
        }
    }

    // ========================= HIDE =========================
    public async UniTask Hide(Type elementType, Action onStartHideAction = null)
    {
        if (!elements.TryGetValue(elementType, out var element))
            return;

        onStartHideAction?.Invoke();

        await element.HideAsync();

        bool isManualScreen = element is BaseScreen baseScreen && baseScreen.ManualHide;
        if (isManualScreen)
        {
            return;
        }

        if (currentElement == element)
        {
            currentElement = null;
        }
    }

    // ========================= HIDE ALL =========================
    public async UniTask HideAll()
    {
        List<UniTask> tasks = new();

        foreach (var e in elements.Values)
        {
            if (e != null && !e.IsHide)
            {
                tasks.Add(e.HideAsync());
            }
        }

        currentElement = null;

        await UniTask.WhenAll(tasks);
    }

    // ========================= OVERLAY =========================
    public bool HasAnyOverlay()
    {
        return currentElement != null
            && !currentElement.IsHide
            && currentElement.hasOverlay;
    }

    public bool HasAnyOverlayInStack()
    {
        return false;
    }

    public List<BaseUIElement> GetAllElements()
    {
        List<BaseUIElement> lists = new();
        foreach(var item in elements.Values)
        {
            lists.Add(item);
        }

        return lists;
    }

    public BaseUIElement GetFirstOverlayElement()
    {
        return null;
    }
}
