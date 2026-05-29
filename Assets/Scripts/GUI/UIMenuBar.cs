using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public enum HomeTab
{
    Shop,
    Home,
    Ranking,
    None
}

public class UIMenuBar : BaseScreen
{
    public override bool ManualHide => true;
    public override bool DestroyOnHide => false;
    public override bool UseBehindPanel => false;

    [Header("Tab Buttons")]
    [SerializeField] private Button btn_Shop;
    [SerializeField] private Button btn_Home;
    [SerializeField] private Button btn_Ranking;

    [SerializeField] private RectTransform rectChooseTab;

    [Header("Tab Icons - On/Off States")]
    [SerializeField] private GameObject img_Shop_on;
    [SerializeField] private GameObject img_Shop_off;
    [SerializeField] private GameObject img_Home_on;
    [SerializeField] private GameObject img_Home_off;
    [SerializeField] private GameObject img_Ranking_on;
    [SerializeField] private GameObject img_Ranking_off;

    [Header("Tab Icons - Scale Animation")]
    [SerializeField] private GameObject img_Icon_Shop;
    [SerializeField] private GameObject img_Icon_Home;
    [SerializeField] private GameObject img_Icon_Ranking;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.25f;
    [SerializeField] private float slideDistance = 350f;
    [SerializeField] private float iconScaleNormal = 0.58f;
    [SerializeField] private float iconScaleSelected = 1f;
    [SerializeField] private float iconScaleDuration = 0.2f;

    [SerializeField] private float chooseTabOffsetShop = -5f;
    [SerializeField] private float chooseTabOffsetHome = -2.7f;
    [SerializeField] private float chooseTabOffsetRanking = 0f;

    private HomeTab currentTab = HomeTab.None;
    private readonly Dictionary<HomeTab, BaseUIElement> _tabElements = new();
    private readonly Dictionary<HomeTab, RectTransform> _tabRects = new();
    private readonly Dictionary<HomeTab, CanvasGroup> _tabCanvasGroups = new();
    private RectTransform _btnShopRect;
    private RectTransform _btnHomeRect;
    private RectTransform _btnRankingRect;
    private RectTransform _chooseParentRect;
    private Canvas _canvas;
    private RectTransform _canvasRect;

    public System.Action<HomeTab> OnTabChanged;


    private void Start()
    {
        CacheStaticRefs();

        if (btn_Shop != null) btn_Shop.onClick.AddListener(() => SwitchToTab(HomeTab.Shop));
        if (btn_Home != null) btn_Home.onClick.AddListener(() => SwitchToTab(HomeTab.Home));
        if (btn_Ranking != null) btn_Ranking.onClick.AddListener(() => SwitchToTab(HomeTab.Ranking));

        StartCoroutine(InitializeMenuBar());
    }

    private void OnDisable()
    {
        KillTabTweens(HomeTab.Shop);
        KillTabTweens(HomeTab.Home);
        KillTabTweens(HomeTab.Ranking);
        rectChooseTab?.DOKill();
    }

    private IEnumerator InitializeMenuBar()
    {
        yield return null;
        UpdateChooseTabWidth();
        SetChooseTabImmediate(HomeTab.Home);
        SwitchToTab(HomeTab.Home);
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled) return;
        UpdateChooseTabWidth();
        SetChooseTabImmediate(currentTab == HomeTab.None ? HomeTab.Home : currentTab);
    }

    private void UpdateChooseTabWidth()
    {
        if (rectChooseTab == null) return;

        float canvasWidth = _canvasRect != null ? _canvasRect.rect.width : 0f;

        if (canvasWidth <= 0f)
        {
            if (UIManager.Instance != null) canvasWidth = UIManager.Instance.CanvasWidth;
        }

        if (canvasWidth <= 0f) return;
        rectChooseTab.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, canvasWidth / 3f);
    }

    public void SwitchToTab(HomeTab tab)
    {
        if (currentTab == tab) return;
        CacheTabElement(tab);
        CacheTabElement(currentTab);

        AnimateChooseTab(tab);
        AnimateIconScale(currentTab, tab);
        AnimateTabTransition(currentTab, tab);

        currentTab = tab;
        OnTabChanged?.Invoke(tab);
    }

    private void AnimateChooseTab(HomeTab tab)
    {
        if (rectChooseTab == null) return;
        float targetX = GetChooseTabTargetLocalX(tab);

        rectChooseTab.DOKill();
        rectChooseTab.DOLocalMoveX(targetX, animationDuration).SetEase(Ease.InOutSine);
    }

    private void SetChooseTabImmediate(HomeTab tab)
    {
        if (rectChooseTab == null) return;
        float targetX = GetChooseTabTargetLocalX(tab);
        var p = rectChooseTab.localPosition;
        rectChooseTab.localPosition = new Vector3(targetX, p.y, p.z);
    }

    private float GetChooseTabTargetLocalX(HomeTab tab)
    {
        RectTransform target = GetButtonRect(tab);
        if (target == null || _chooseParentRect == null) return rectChooseTab.localPosition.x;

        float targetCenterX = RectTransformUtility.CalculateRelativeRectTransformBounds(_chooseParentRect, target).center.x;
        float chooseTabCenterX = RectTransformUtility.CalculateRelativeRectTransformBounds(_chooseParentRect, rectChooseTab).center.x;
        float pivotToChooseCenterX = chooseTabCenterX - rectChooseTab.localPosition.x;
        float x = targetCenterX - pivotToChooseCenterX;

        x += GetChooseTabOffset(tab);

        if (_canvas != null)
            x = RectTransformUtility.PixelAdjustPoint(new Vector2(x, 0f), _chooseParentRect, _canvas).x;

        return x;
    }

    private float GetChooseTabOffset(HomeTab tab)
    {
        switch (tab)
        {
            case HomeTab.Shop:
                return chooseTabOffsetShop;
            case HomeTab.Home:
                return chooseTabOffsetHome;
            case HomeTab.Ranking:
                return chooseTabOffsetRanking;
            default:
                return 0f;
        }
    }


    private void AnimateTabTransition(HomeTab fromTab, HomeTab toTab)
    {
        RectTransform fromRect = GetTabRect(fromTab);
        RectTransform toRect = GetTabRect(toTab);

        if (fromTab != HomeTab.None && fromTab != HomeTab.Home && fromRect != null)
        {
            KillTabTweens(fromTab);
            
            float exitOffset = fromTab == HomeTab.Shop ? -slideDistance : slideDistance;
            fromRect.DOAnchorPosX(exitOffset, animationDuration)
                .SetEase(Ease.InOutSine)
                .OnComplete(() =>
                {
                    HideTabUI(fromTab);
                    fromRect.anchoredPosition = Vector2.zero;
                });
        }

        if (toTab != HomeTab.Home && toRect != null)
        {
            ShowTabUI(toTab);
            KillTabTweens(toTab);

            float enterOffset = toTab == HomeTab.Shop ? -slideDistance : slideDistance;
            toRect.anchoredPosition = new Vector2(enterOffset, 0);

            toRect.DOAnchorPosX(0, animationDuration)
                .SetEase(Ease.InOutSine);
        }
    }

    private void ShowTabUI(HomeTab tab)
    {
        BaseUIElement element = CacheTabElement(tab);
        ResetTabVisualState(tab);

        switch (tab)
        {
            case HomeTab.Shop:
                element?.Show();
                break;
            case HomeTab.Ranking:
                element?.Show();
                break;
        }
        BringToFront();
    }

    private void HideTabUI(HomeTab tab)
    {
        BaseUIElement element = CacheTabElement(tab);
        ResetTabVisualState(tab);

        switch (tab)
        {
            case HomeTab.Shop:
                element?.Hide();
                break;
            case HomeTab.Ranking:
                element?.Hide();
                break;
        }
    }

    private RectTransform GetTabRect(HomeTab tab)
    {
        if (_tabRects.TryGetValue(tab, out RectTransform rect) && rect != null)
            return rect;

        BaseUIElement element = CacheTabElement(tab);
        GameObject tabHolder = element != null ? element.holder : null;
        rect = tabHolder != null ? tabHolder.GetComponent<RectTransform>() : null;
        if (rect != null) _tabRects[tab] = rect;
        return rect;
    }

    private BaseUIElement CacheTabElement(HomeTab tab)
    {
        if (tab == HomeTab.None) return null;
        if (_tabElements.TryGetValue(tab, out BaseUIElement element) && element != null)
        {
            PrepareTabElementForMenu(element);
            return element;
        }

        if (UIManager.Instance == null) return null;

        element = tab switch
        {
            HomeTab.Shop => UIManager.Instance.Get<UIShop>(),
            HomeTab.Home => UIManager.Instance.Get<UIHome>(),
            HomeTab.Ranking => UIManager.Instance.Get<UIRank>(),
            _ => null
        };

        if (element != null)
        {
            _tabElements[tab] = element;
            PrepareTabElementForMenu(element);
        }

        return element;
    }

    private void PrepareTabElementForMenu(BaseUIElement element)
    {
        if (element == null) return;
        element.SetAnim(UIAnimType.None);
    }

    private void BringToFront()
    {
        transform.SetAsLastSibling();
        if (holder != null) holder.transform.SetAsLastSibling();
    }

    private void CacheStaticRefs()
    {
        _btnShopRect = btn_Shop != null ? btn_Shop.GetComponent<RectTransform>() : null;
        _btnHomeRect = btn_Home != null ? btn_Home.GetComponent<RectTransform>() : null;
        _btnRankingRect = btn_Ranking != null ? btn_Ranking.GetComponent<RectTransform>() : null;
        _chooseParentRect = rectChooseTab != null ? rectChooseTab.parent as RectTransform : null;
        _canvas = GetComponentInParent<Canvas>();
        _canvasRect = _canvas != null ? _canvas.GetComponent<RectTransform>() : null;
    }

    private RectTransform GetButtonRect(HomeTab tab)
    {
        return tab switch
        {
            HomeTab.Shop => _btnShopRect,
            HomeTab.Home => _btnHomeRect,
            HomeTab.Ranking => _btnRankingRect,
            _ => null
        };
    }

    private void KillTabTweens(HomeTab tab)
    {
        if (_tabRects.TryGetValue(tab, out RectTransform rect) && rect != null)
            rect.DOKill();

        if (_tabCanvasGroups.TryGetValue(tab, out CanvasGroup group) && group != null)
            group.DOKill();
    }

    private void ResetTabVisualState(HomeTab tab)
    {
        BaseUIElement element = CacheTabElement(tab);
        Transform root = element != null && element.holder != null ? element.holder.transform : null;
        if (root == null) return;

        CanvasGroup group = GetTabCanvasGroup(tab);
        if (group != null)
        {
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        UIFadeUtil.Restore(root);
    }

    private CanvasGroup GetTabCanvasGroup(HomeTab tab)
    {
        if (_tabCanvasGroups.TryGetValue(tab, out CanvasGroup group))
            return group;

        BaseUIElement element = CacheTabElement(tab);
        if (element == null || element.holder == null) return null;

        group = element.holder.GetComponent<CanvasGroup>();
        _tabCanvasGroups[tab] = group;
        return group;
    }

    private void AnimateIconScale(HomeTab fromTab, HomeTab toTab)
    {
        GameObject fromIcon = GetIconByTab(fromTab);
        if (fromIcon != null)
        {
            // Dừng tween cũ để tránh giật khi spam click
            fromIcon.transform.DOKill();

            // Cho icon cũ thu nhỏ lại mượt mà
            fromIcon.transform.DOScale(iconScaleNormal, iconScaleDuration)
                .SetEase(Ease.InOutSine);
        }

        GameObject toIcon = GetIconByTab(toTab);
        if (toIcon != null)
        {
            // Dừng tween cũ và tạo hiệu ứng zoom-in rõ ràng cho tab được chọn
            toIcon.transform.DOKill();

            float startScale   = iconScaleSelected * 0.85f; // nhỏ hơn một chút
            float overshoot    = iconScaleSelected * 1.08f;  // nảy nhẹ

            // Bắt đầu từ scale nhỏ để cảm giác chuyển tab rõ ràng hơn
            toIcon.transform.localScale = Vector3.one * startScale;

            Sequence seq = DOTween.Sequence();
            seq.Append(toIcon.transform.DOScale(overshoot, iconScaleDuration * 0.55f)
                    .SetEase(Ease.OutBack))
               .Append(toIcon.transform.DOScale(iconScaleSelected, iconScaleDuration * 0.45f)
                    .SetEase(Ease.OutSine));
        }
    }

    private GameObject GetIconByTab(HomeTab tab)
    {
        switch (tab)
        {
            case HomeTab.Shop:
                return img_Icon_Shop;
            case HomeTab.Home:
                return img_Icon_Home;
            case HomeTab.Ranking:
                return img_Icon_Ranking;
            default:
                return null;
        }
    }

    public HomeTab GetCurrentTab()
    {
        return currentTab;
    }
}
