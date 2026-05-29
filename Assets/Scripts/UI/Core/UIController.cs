using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class UIController<T> : IUIController where T : BaseUIElement
{
    private readonly Dictionary<Type, T> elements = new();
    private readonly HashSet<Type> _loadingSet = new();
    private readonly Dictionary<Type, List<Action<T>>> _pendingCallbacks = new();

    private class UIStackItem
    {
        public T element;
        public object data;

        public UIStackItem(T element, object data)
        {
            this.element = element;
            this.data = data;
        }
    }

    private readonly Stack<UIStackItem> _stack = new();

    private readonly StringBuilder pathBuilder = new();
    private readonly StringBuilder nameBuilder = new();

    private T currentElement;
    private Transform container;
    private string resourcePath;
    private UIType uiType;
    private readonly ILoadAsync<GameObject> loader;

    public UIController(Transform container, string resourcePath, UIType uiType, ILoadAsync<GameObject> loader)
    {
        this.container = container;
        this.resourcePath = resourcePath;
        this.uiType = uiType;
        this.loader = loader;
    }

    public BaseUIElement CurrentElement => currentElement;

    // ========================= SHOW =========================
    public async UniTask Show(Type elementType, object data = null, bool forceShowData = false, bool hideCurrent = true, Action onStartShowAction = null)
    {
        if (!typeof(T).IsAssignableFrom(elementType))
        {
            throw new ArgumentException($"{elementType.Name} is not assignable to {typeof(T).Name}");
        }

        T result = null;

        // ===== HANDLE CURRENT =====
        if (currentElement != null)
        {
            var curType = currentElement.GetType();

            if (curType == elementType)
            {
                result = currentElement;
            }
            else if (!currentElement.IsHide)
            {
                _stack.Push(new UIStackItem(currentElement, data));

                if(hideCurrent) await currentElement.HideAsync();
            }
        }

        if (result == null)
        {
            result = await GetOrLoadElement(elementType);
        }

        bool isShow = false;

        if (result != null)
        {
            if (forceShowData)
            {
                isShow = true;
            }
            else if (result.IsHide)
            {
                isShow = true;
            }
        }
        currentElement = result;

        currentElement.transform.SetAsLastSibling();

        onStartShowAction?.Invoke();

        if (isShow)
        {
            result.SetObjectData(data);
            await result.ShowAsync();     
        }
    }

    // ========================= LOAD =========================
    private async UniTask<T> GetOrLoadElement(Type elementType)
    {
        if (elements.TryGetValue(elementType, out var element))
        {
            return element;
        }

        // ===== đang load =====
        if (_loadingSet.Contains(elementType))
        {
            var tcs = new UniTaskCompletionSource<T>();

            _pendingCallbacks[elementType].Add(e => tcs.TrySetResult(e));

            return await tcs.Task;
        }

        _loadingSet.Add(elementType);
        _pendingCallbacks[elementType] = new();

        T newElement = await GetNewElement(elementType);

        if (newElement != null && !elements.ContainsKey(elementType))
        {
            elements.Add(elementType, newElement);
        }

        _loadingSet.Remove(elementType);

        elements.TryGetValue(elementType, out var loaded);

        var callbacks = _pendingCallbacks[elementType];
        _pendingCallbacks.Remove(elementType);

        foreach (var cb in callbacks)
            cb?.Invoke(loaded);

        return loaded;
    }

    private async UniTask<T> GetNewElement(Type elementType)
    {
        string nameElement = elementType.Name;
        GameObject pfElement = null;

        pathBuilder.Clear();
        pathBuilder.Append(resourcePath).Append(nameElement);

        var tcs = new UniTaskCompletionSource<GameObject>();

        await loader.LoadAsync(pathBuilder.ToString(), go => tcs.TrySetResult(go));

        pfElement = await tcs.Task;

        if (pfElement == null || !pfElement.GetComponent(elementType))
        {
            throw new MissingReferenceException($"Cannot find prefab for {nameElement} of type {uiType}");
        }

        GameObject ob = UnityEngine.Object.Instantiate(pfElement, container);

        ob.transform.localScale = Vector3.one;
        ob.transform.localPosition = Vector3.zero;

#if UNITY_EDITOR
        nameBuilder.Clear();
        nameBuilder.Append(uiType.ToString().ToUpper()).Append("_").Append(nameElement);
        ob.name = nameBuilder.ToString();
#endif

        T elementScr = ob.GetComponent(elementType) as T;

        elementScr.UIRegisterType = UIRegisterType.OnSpawn;
        elementScr.Init();
        elementScr.SetUIType(uiType);

        return elementScr;
    }

    public BaseUIElement GetExistElement(Type elementType)
    {
        if (!typeof(T).IsAssignableFrom(elementType))
        {
            throw new ArgumentException($"{elementType.Name} is not assignable to {typeof(T).Name}");
        }

        elements.TryGetValue(elementType, out var element);
        return element;
    }

    // ========================= HIDE =========================
    public async UniTask Hide(Type elementType, Action onStartHideAction = null)
    {
        if (elements.TryGetValue(elementType, out T elementToHide))
        {
            onStartHideAction?.Invoke();
            await elementToHide.HideAsync();

            if (currentElement == elementToHide)
            {
                if (_stack.Count > 0)
                {
                    var stackItem = _stack.Pop();

                    currentElement = stackItem.element;

                    if (currentElement != null)
                    {
                        currentElement.transform.SetAsLastSibling();
                        if (currentElement.IsHide)
                        {
                            currentElement.SetObjectData(stackItem.data);
                            currentElement.ShowAsync().Forget();
                        }
                    }
                }
                else
                {
                    currentElement = null;
                }
            }
        }
    }

    // ========================= HIDE ALL =========================
    public async UniTask HideAll()
    {
        List<UniTask> tasks = new();
        foreach (var item in elements)
        {
            if (item.Value != null && !item.Value.IsHide)
            {
                tasks.Add(item.Value.HideAsync());
            }
        }

        _stack.Clear();
        currentElement = null;

        await UniTask.WhenAll(tasks);
    }

    public void AddElement<Y>(Type elementType, Y element) where Y : BaseUIElement
    {
        if (element == null) return;
        if (elements.ContainsKey(elementType)) return;

        elements.Add(elementType, element as T);
    }

    public void Remove(Type elementType)
    {
        if (elements.ContainsKey(elementType))
        {
            UnityEngine.Object.Destroy(elements[elementType].gameObject);
            elements.Remove(elementType);
        }
    }

    // ========================= OVERLAY CHECK =========================
    public bool HasAnyOverlayInStack()
    {
        // check current
        if (currentElement != null && !currentElement.IsHide && currentElement.hasOverlay)
            return true;

        if (_stack.Count == 0) return false;

        foreach (var item in _stack)
        {
            if (item.element != null && !item.element.IsHide && item.element.hasOverlay)
                return true;
        }

        return false;
    }

    public List<BaseUIElement> GetAllElements()
    {
        List<BaseUIElement> lists = new();
        foreach (var item in elements.Values)
        {
            lists.Add(item);
        }

        return lists;
    }

    public BaseUIElement GetFirstOverlayElement()
    {
        BaseUIElement baseUIElement = null;

        foreach (var item in _stack)
        {
            if (item.element != null && !item.element.IsHide && item.element.hasOverlay)
                baseUIElement = item.element;
        }

        return baseUIElement;
    }
}
