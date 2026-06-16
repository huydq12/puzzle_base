using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;

public class UIManager : Singleton<UIManager>
{
    #region Inspector

    [Header("UI Root")]
    public Transform cScreen;
    public Transform cPopup;
    public Transform cNotify;
    public Transform cOverlap;

    #endregion

    #region Overlay

    [Header("Overlay")]
    private UIOverlay _overlayInstance;

    #endregion

    #region Controller Maps

    private readonly Dictionary<Type, IUIController> _baseControllerMap = new();
    private readonly Dictionary<Type, IUIController> _resolvedControllerCache = new();

    private readonly Dictionary<Type, IUIController> _onSceneControllerMap = new();
    private readonly Dictionary<Type, IUIController> _resolvedOnSceneCache = new();

    #endregion

    #region Constants

    private const string SCREEN_RESOURCES_PATH = "Prefabs/UI/Screen/";
    private const string POPUP_RESOURCES_PATH = "Prefabs/UI/Popup/";
    private const string NOTIFY_RESOURCES_PATH = "Prefabs/UI/Notify/";
    private const string OVERLAP_RESOURCES_PATH = "Prefabs/UI/Overlay/";

    #endregion

    private List<Type> _priorityType = new List<Type>() { typeof(BaseScreen), typeof(BasePopup), typeof(BaseNotify), typeof(BaseOverlay) };

    public float CanvasWidth
    {
        get
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
            var rect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
            return rect != null ? rect.rect.width : 0f;
        }
    }

    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();

        var goLoader = new ResoucesLoadAsync<GameObject>();

        // Base controller (spawn từ Resources)
        _baseControllerMap[typeof(BaseScreen)] = new UIController<BaseScreen>(cScreen, SCREEN_RESOURCES_PATH, UIType.Screen, goLoader);
        _baseControllerMap[typeof(BasePopup)] = new UIController<BasePopup>(cPopup, POPUP_RESOURCES_PATH, UIType.Popup, goLoader);
        _baseControllerMap[typeof(BaseNotify)] = new UIController<BaseNotify>(cNotify, NOTIFY_RESOURCES_PATH, UIType.Notify, goLoader);
        _baseControllerMap[typeof(BaseOverlay)] = new UIController<BaseOverlay>(cOverlap, OVERLAP_RESOURCES_PATH, UIType.Overlap, goLoader);

        // On-scene controller
        _onSceneControllerMap[typeof(BaseScreen)] = new UIControllerOnScene<BaseScreen>();
        _onSceneControllerMap[typeof(BasePopup)] = new UIControllerOnScene<BasePopup>();
        _onSceneControllerMap[typeof(BaseNotify)] = new UIControllerOnScene<BaseNotify>();
        _onSceneControllerMap[typeof(BaseOverlay)] = new UIControllerOnScene<BaseOverlay>();

        RegisterSceneUIElements();
        CreateOverlay();
    }

    #endregion

    #region Show UI

    public void ShowUI<T>(
        object data = null,
        bool forceShowData = true,
        bool hideCurrent = true,
        UIAnimType animType = UIAnimType.Default,
        Action action = null
    ) where T : BaseUIElement
    {
        ShowUIAsync<T>(data, forceShowData, hideCurrent, animType, action).Forget();
    }

    public void ShowUI<T>(
        bool hideCurrent,
        UIAnimType animType = UIAnimType.Default,
        Action action = null
    ) where T : BaseUIElement
    {
        ShowUIAsync<T>(null, true, hideCurrent, animType, action).Forget();
    }

    public async UniTask ShowUIAsync<T>(
        object data = null,
        bool forceShowData = true,
        bool hideCurrent = true,
        UIAnimType animType = UIAnimType.Default,
        Action action = null
    ) where T : BaseUIElement
    {
        var type = typeof(T);
        //animType = ResolveDefault(animType, type);

        // Ưu tiên UI có sẵn trong scene
        if (TryGetOnSceneController(type, out var onSceneController) &&
            onSceneController.GetExistElement(type) != null)
        {
            await ShowWithAnimAsync(onSceneController, type, animType, data, forceShowData, hideCurrent, action);
            return;
        }

        // fallback sang controller load prefab
        if (TryGetController(type, out var controller))
        {
            await ShowWithAnimAsync(controller, type, animType, data, forceShowData, hideCurrent, action);
        }
    }

    #endregion

    #region Hide UI

    public void HideUI<T>(UIAnimType animType = UIAnimType.Default, Action action = null)
        where T : BaseUIElement
    {
        HideUIAsync<T>(animType, action).Forget();
    }

    public void HideUI(Type type, UIAnimType animType = UIAnimType.Default, Action action = null)
    {
        HideUIAsync(type, animType, action).Forget();
    }

    public async UniTask HideUIAsync<T>(UIAnimType animType = UIAnimType.Default, Action action = null)
        where T : BaseUIElement
    {
        await HideUIAsync(typeof(T), animType, action);
    }

    public async UniTask HideUIAsync(Type type, UIAnimType animType = UIAnimType.Default, Action action = null)
    {
        if (TryGetOnSceneController(type, out var onSceneController) &&
            onSceneController.GetExistElement(type) != null)
        {
            await HideWithAnimAsync(onSceneController, type, animType, action);
            return;
        }

        if (TryGetController(type, out var controller))
        {
            await HideWithAnimAsync(controller, type, animType, action);
        }
    }

    #endregion

    #region Get UI

    public T Get<T>() where T : BaseUIElement
    {
        var existing = GetExistUI<T>();
        if (existing != null) return existing;

        return CreateUIImmediate<T>();
    }

    public T GetExistUI<T>() where T : BaseUIElement
    {
        var type = typeof(T);

        if (TryGetOnSceneController(type, out var onSceneController))
        {
            var element = onSceneController.GetExistElement(type);
            if (element != null) return element as T;
        }

        if (TryGetController(type, out var controller))
        {
            return controller.GetExistElement(type) as T;
        }

        return null;
    }

    #endregion

    private T CreateUIImmediate<T>() where T : BaseUIElement
    {
        Type type = typeof(T);
        if (!TryResolveCreateInfo(type, out Transform container, out string resourcePath, out UIType uiType))
            return null;

        if (container == null)
        {
            Debug.LogError($"[UIManager] Cannot create {type.Name}: missing UI container.");
            return null;
        }

        GameObject prefab = Resources.Load<GameObject>(resourcePath + type.Name);
        if (prefab == null)
        {
            prefab = FindPrefabByComponent<T>();
        }

        if (prefab == null)
        {
            Debug.LogError($"[UIManager] Cannot create {type.Name}: prefab not found at Resources/{resourcePath}{type.Name}.");
            return null;
        }

        GameObject instance = Instantiate(prefab, container);
        instance.transform.localScale = Vector3.one;
        instance.transform.localPosition = Vector3.zero;

#if UNITY_EDITOR
        instance.name = $"{uiType.ToString().ToUpper()}_{type.Name}";
#endif

        T element = instance.GetComponent<T>();
        if (element == null)
        {
            Destroy(instance);
            return null;
        }

        element.UIRegisterType = UIRegisterType.OnSpawn;
        element.Init();
        element.SetUIType(uiType);

        if (TryGetController(type, out var controller))
        {
            controller.AddElement(type, element);
        }

        return element;
    }

    private GameObject FindPrefabByComponent<T>() where T : BaseUIElement
    {
        GameObject[] prefabs = Resources.LoadAll<GameObject>("");
        foreach (GameObject prefab in prefabs)
        {
            if (prefab == null) continue;
            if (prefab.GetComponent<T>() != null)
                return prefab;
        }

        return null;
    }

    private bool TryResolveCreateInfo(Type type, out Transform container, out string resourcePath, out UIType uiType)
    {
        if (IsType<BaseScreen>(type))
        {
            container = cScreen;
            resourcePath = SCREEN_RESOURCES_PATH;
            uiType = UIType.Screen;
            return true;
        }

        if (IsType<BasePopup>(type))
        {
            container = cPopup;
            resourcePath = POPUP_RESOURCES_PATH;
            uiType = UIType.Popup;
            return true;
        }

        if (IsType<BaseNotify>(type))
        {
            container = cNotify;
            resourcePath = NOTIFY_RESOURCES_PATH;
            uiType = UIType.Notify;
            return true;
        }

        if (IsType<BaseOverlay>(type))
        {
            container = cOverlap;
            resourcePath = OVERLAP_RESOURCES_PATH;
            uiType = UIType.Overlap;
            return true;
        }

        container = null;
        resourcePath = null;
        uiType = UIType.Unknow;
        return false;
    }

    #region Register

    public void RegisterUI(BaseUIElement element)
    {
        var type = element.GetType();

        if (TryGetOnSceneController(type, out var controller))
        {
            controller.AddElement(type, element);
        }
    }

    private void RegisterSceneUIElements()
    {
        BaseUIElement[] elements = Resources.FindObjectsOfTypeAll<BaseUIElement>();
        foreach (BaseUIElement element in elements)
        {
            if (element == null) continue;
            if (!element.gameObject.scene.IsValid()) continue;
            if (element.UIRegisterType != UIRegisterType.None) continue;

            if (!TryResolveCreateInfo(element.GetType(), out _, out _, out UIType uiType))
                continue;

            element.SetUIType(uiType);
            RegisterUI(element);
            element.UIRegisterType = UIRegisterType.OnScene;
        }
    }

    #endregion

    #region Controller Resolve

    private bool TryGetController<T>(out IUIController controller) where T : BaseUIElement
    {
        return TryGetController(typeof(T), out controller);
    }

    private bool TryGetController(Type type, out IUIController controller)
    {
        if (_resolvedControllerCache.TryGetValue(type, out controller))
            return true;

        if (IsType<BaseScreen>(type))
            controller = _baseControllerMap[typeof(BaseScreen)];
        else if (IsType<BasePopup>(type))
            controller = _baseControllerMap[typeof(BasePopup)];
        else if (IsType<BaseNotify>(type))
            controller = _baseControllerMap[typeof(BaseNotify)];
        else if (IsType<BaseOverlay>(type))
            controller = _baseControllerMap[typeof(BaseOverlay)];
        else
        {
            controller = null;
            return false;
        }

        _resolvedControllerCache[type] = controller;
        return true;
    }

    private bool TryGetOnSceneController(Type type, out IUIController controller)
    {
        if (_resolvedOnSceneCache.TryGetValue(type, out controller))
            return true;

        if (IsType<BaseScreen>(type))
            controller = _onSceneControllerMap[typeof(BaseScreen)];
        else if (IsType<BasePopup>(type))
            controller = _onSceneControllerMap[typeof(BasePopup)];
        else if (IsType<BaseNotify>(type))
            controller = _onSceneControllerMap[typeof(BaseNotify)];
        else if (IsType<BaseOverlay>(type))
            controller = _onSceneControllerMap[typeof(BaseOverlay)];
        else
        {
            controller = null;
            return false;
        }

        _resolvedOnSceneCache[type] = controller;
        return true;
    }

    private bool IsType<T>(Type type)
    {
        return type == typeof(T) || type.IsSubclassOf(typeof(T));
    }

    #endregion

    #region Show/Hide Core

    private async UniTask ShowWithAnimAsync(
        IUIController controller,
        Type type,
        UIAnimType animType,
        object data,
        bool forceShowData,
        bool hideCurrent,
        Action action
    )
    {
        var prev = controller.CurrentElement;
        bool prevHasOverlay = prev != null && prev.hasOverlay;

        Action onStartShow = () =>
        {
            var next = controller.CurrentElement;
            if (next == null) return;

            animType = ResolveDefault(animType, next);

            next.SetAnim(animType);

            if (next.hasOverlay && _overlayInstance != null)
            {
                var config = next.GetOverlayConfig();

                if (!prevHasOverlay)
                    _overlayInstance.Show(config, next.transform);
                else
                    _overlayInstance.ApplyConfig(config);
            }

            action?.Invoke();
        };

        await controller.Show(type, data, forceShowData, hideCurrent, onStartShow);
    }

    private async UniTask HideWithAnimAsync(
        IUIController controller,
        Type type,
        UIAnimType animType,
        Action action
    )
    {
        var element = controller.GetExistElement(type);
        if (element == null) return;

        animType = ResolveDefault(animType, element);

        element.SetAnim(animType);

        await controller.Hide(type, action);

        if (element != null && !element.hasOverlay)
        {
            //_overlayInstance.Hide();
            return;
        }

        var next = controller.CurrentElement;
        bool nextHasOverlay = next != null && next.hasOverlay;
        bool stillHasOverlay = controller.HasAnyOverlayInStack();

        if (stillHasOverlay && nextHasOverlay && _overlayInstance != null)
        {
            _overlayInstance.ApplyConfig(next.GetOverlayConfig());
        }
        else
        {
            IUIController nextController = GetNextPriorityController(type);

            if (nextController == null)
            {
                _overlayInstance?.Hide();
            }
            else
            {
                var nextElement = nextController.GetFirstOverlayElement();

                if (nextElement == null)
                {
                    _overlayInstance?.Hide();
                    return;
                }

                UIOverlayConfig uIOverlayConfig = nextElement.uIOverlayConfig;

                _overlayInstance?.ApplyConfig(uIOverlayConfig, nextElement.transform);
            }
        }
    }

    #endregion

    #region Utils

    public void HideAll(UIType uiType)
    {
        HideAllBaseAsync(uiType).Forget();
    }

    public void HideAll()
    {
        HideAllBaseAsync(UIType.Screen).Forget();
        HideAllBaseAsync(UIType.Popup).Forget();
        HideAllBaseAsync(UIType.Notify).Forget();
        HideAllBaseAsync(UIType.Overlap).Forget();
        HideAllOnSceneAsync(UIType.Screen).Forget();
        HideAllOnSceneAsync(UIType.Popup).Forget();
        HideAllOnSceneAsync(UIType.Notify).Forget();
        HideAllOnSceneAsync(UIType.Overlap).Forget();
    }

    public void ToggleAllContainers()
    {
        SetAllContainersActive(!AreAllContainersActive());
    }

    public async UniTask HideAllBaseAsync(UIType uiType)
    {
        switch (uiType)
        {
            case UIType.Screen:
                await _baseControllerMap[typeof(BaseScreen)].HideAll();
                break;

            case UIType.Popup:
                await _baseControllerMap[typeof(BasePopup)].HideAll();
                break;

            case UIType.Notify:
                await _baseControllerMap[typeof(BaseNotify)].HideAll();
                break;

            case UIType.Overlap:
                await _baseControllerMap[typeof(BaseOverlay)].HideAll();
                break;
        }
    }    
    
    public async UniTask HideAllOnSceneAsync(UIType uiType)
    {
        switch (uiType)
        {
            case UIType.Screen:
                await _onSceneControllerMap[typeof(BaseScreen)].HideAll();
                break;

            case UIType.Popup:
                await _onSceneControllerMap[typeof(BasePopup)].HideAll();
                break;

            case UIType.Notify:
                await _onSceneControllerMap[typeof(BaseNotify)].HideAll();
                break;

            case UIType.Overlap:
                await _onSceneControllerMap[typeof(BaseOverlay)].HideAll();
                break;
        }
    }

    public UIAnimType ResolveDefault(UIAnimType animType, BaseUIElement baseUIElement)
    {
        if(animType == UIAnimType.Default)
        {
            if(baseUIElement.animType == UIAnimType.Default)
            {
                if (IsType<BasePopup>(baseUIElement.GetType()))
                    return UIAnimType.SlideRight;
            }

            return baseUIElement.animType;
        }

        if (animType != UIAnimType.Default)
            return animType;


        return UIAnimType.None;
    }

    #endregion

    private void SetAllContainersActive(bool isActive)
    {
        SetContainerActive(cScreen, isActive);
        SetContainerActive(cPopup, isActive);
        SetContainerActive(cNotify, isActive);
        SetContainerActive(cOverlap, isActive);
    }

    private bool AreAllContainersActive()
    {
        return IsContainerActive(cScreen)
            && IsContainerActive(cPopup)
            && IsContainerActive(cNotify)
            && IsContainerActive(cOverlap);
    }

    private void SetContainerActive(Transform container, bool isActive)
    {
        if (container == null)
            return;

        container.gameObject.SetActive(isActive);
    }

    private bool IsContainerActive(Transform container)
    {
        return container != null && container.gameObject.activeSelf;
    }

    #region Overlay

    private void CreateOverlay()
    {
        if (cPopup == null)
        {
            return;
        }

        var go = new GameObject("UI_Overlay");

        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(cPopup, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        go.AddComponent<CanvasRenderer>();

        _overlayInstance = go.AddComponent<UIOverlay>();
        _overlayInstance.Init(cPopup);

        go.transform.SetAsFirstSibling();
    }

    public void SetCustomOverlayPosition(Transform target, UIOverlayConfig uIOverlayConfig, float duration = 0)
    {
        _overlayInstance?.Show(uIOverlayConfig, target, duration);
    }

    public void HideOverlay(float duration = 0.25f)
    {
        _overlayInstance?.Hide(duration);
    }

    public void ShowOverlay(IUIController controller)
    {
        var next = controller.CurrentElement;
        var config = next.GetOverlayConfig();
        _overlayInstance.Show(config, next.transform);
    }

    #endregion

    private IUIController GetNextPriorityController(Type type)
    {
        IUIController controller = null;
        Type currentType = type;

        if (IsType<BaseScreen>(type))
        {
            controller = _onSceneControllerMap[typeof(BasePopup)];
            currentType = typeof(BasePopup);
        }

        else if (IsType<BasePopup>(type))
        {
            controller = _onSceneControllerMap[typeof(BaseNotify)];
            currentType = typeof(BaseNotify);
        }
        else if (IsType<BaseNotify>(type))
        {
            controller = _onSceneControllerMap[typeof(BaseOverlay)];
            currentType = typeof(BaseOverlay);  
        }
        else if (IsType<BaseOverlay>(type))
        {
            controller = null;
            currentType = null;
        }


        if(controller != null && currentType != null && controller.GetFirstOverlayElement() == null)
        {
            return GetNextPriorityController(currentType);
        }

        return controller;
    }

    #region Current UI

    public BaseScreen CurScreen => _baseControllerMap[typeof(BaseScreen)].CurrentElement as BaseScreen;
    public BasePopup CurPopup => _baseControllerMap[typeof(BasePopup)].CurrentElement as BasePopup;
    public BaseNotify CurNotify => _baseControllerMap[typeof(BaseNotify)].CurrentElement as BaseNotify;
    public BaseOverlay CurOverlap => _baseControllerMap[typeof(BaseOverlay)].CurrentElement as BaseOverlay;

    public bool HasActivePopup
    {
        get
        {
            if (CurPopup != null && !CurPopup.IsHide)
            {
                return true;
            }

            if (_onSceneControllerMap.TryGetValue(typeof(BasePopup), out IUIController popupController))
            {
                BaseUIElement currentPopup = popupController.CurrentElement;
                return currentPopup != null && !currentPopup.IsHide;
            }

            return false;
        }
    }

    #endregion
}

public enum UIType
{
    Unknow,
    Screen,
    Popup,
    Notify,
    Overlap
}
