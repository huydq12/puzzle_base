using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.EventSystems;
#endif

public class GameUI : Singleton<GameUI>
{
    private List<UIElement> ActiveElements;
    private Dictionary<Type, UIElement> Elements;
    private RectTransform FrontPanel, BehindPanel;
    private readonly string FolderPath = "GameUI/";

    public float CanvasWidth ;
    public float CanvasHeight ;


    public int ActiveCount => ActiveElements.Count;
    protected override void Awake()
    {
        base.Awake();
        ActiveElements = new List<UIElement>();
        Elements = new Dictionary<Type, UIElement>();
        CreateFrontAndBehindPanel();

        var canvas = GetComponent<Canvas>();
        var canvasRect = canvas.GetComponent<RectTransform>();
        CanvasWidth  = canvasRect.rect.width;
        CanvasHeight = canvasRect.rect.height;
        Debug.Log($"Canvas size: {CanvasWidth} x {CanvasHeight}");
    }
    public void HideAll()
    {
        //foreach (UIElement element in ActiveElements)
        //    element.Hide();

        while (ActiveElements.Count > 0)
            ActiveElements[0].Hide();
    }
    public void HideOnTop()
    {
        int activeCount = ActiveCount;
        if (activeCount == 0) return;
        UIElement element = ActiveElements[--activeCount];
        if (!element.ManualHide) element.Hide();
    }
    public void Block(bool value)
    {
        FrontPanel.gameObject.SetActive(value);
        if (value) FrontPanel.SetAsLastSibling();
        else FrontPanel.SetAsFirstSibling();
    }
    public Element Get<Element>() where Element : UIElement
    {
        Type type = typeof(Element);
        if (Elements.ContainsKey(type)) return Elements[type] as Element;
        Element prefab = Resources.Load<Element>(FolderPath + type.Name);
        Element element = prefab != null ? Instantiate(prefab) : default;
        if (element != null) SetParent(element.transform, transform);
        return element;
    }
    public void Submit(UIElement element)
    {
        if (ActiveElements.Contains(element)) return;
        ActiveElements.Add(element);
        element.transform.SetAsLastSibling();
        UpdateBehindPanelSibling();
    }
    public void Unsubmit(UIElement element)
    {
        if (ActiveElements.Remove(element)) UpdateBehindPanelSibling();
    }
    public void Register(UIElement element)
    {
        Type type = element.GetType();
        if (Elements.ContainsKey(type)) return;
        Elements.Add(type, element);
    }
    public void Unregister(UIElement element)
    {
        Elements.Remove(element.GetType());
    }
    private void UpdateBehindPanelSibling()
    {
        BehindPanel.SetParent(transform);
        for (int i = ActiveCount - 1; i >= 0; i--)
        {
            if (!ActiveElements[i].UseBehindPanel) continue;
            BehindPanel.gameObject.SetActive(true);
            SetParent(BehindPanel, ActiveElements[i].transform);
            BehindPanel.SetAsFirstSibling();
            return;
        }
        BehindPanel.gameObject.SetActive(false);
    }
    private void CreateFrontAndBehindPanel()
    {
        FrontPanel = CreatePanel("FrontPanel", new Color(1, 1, 1, 0));
        BehindPanel = CreatePanel("BehindPanel", new Color(0, 0, 0, 0.9725f));
        Button button = BehindPanel.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(HideOnTop);
    }
    private void SetParent(Transform target, Transform parent)
    {
        target.SetParent(parent, false);
        target.localScale = Vector3.one;
        target.localPosition = Vector3.zero;
    }
    private RectTransform CreatePanel(string name, Color color)
    {
        GameObject panelObject = new GameObject(name);
        panelObject.AddComponent<Image>().color = color;
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        SetParent(panelRect, transform);
        panelRect.anchorMax = Vector2.one;
        panelRect.anchorMin = Vector2.zero;
        panelRect.offsetMax = Vector2.one * 2;
        panelRect.offsetMin = Vector2.one * -2;
        panelObject.SetActive(false);
        return panelRect;
    }
#if UNITY_EDITOR
    [MenuItem("GameObject/UI/GameUI", priority = 0)]
    private static void CreateContext()
    {
        GameObject canvasObject = new GameObject("MainCanvas");
        canvasObject.layer = 5;
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 10;
        CanvasScaler canvasScaler = canvasObject.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(720, 1280);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        canvasObject.AddComponent<GraphicRaycaster>();
        canvasObject.AddComponent<GameUI>();
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem != null) return;
        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
        Selection.activeObject = canvasObject;
    }
#endif
}