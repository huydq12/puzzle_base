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

public class UIMenuBar : UIElement
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

    public System.Action<HomeTab> OnTabChanged;


    private void Start()
    {
        btn_Shop.onClick.AddListener(() => SwitchToTab(HomeTab.Shop));
        btn_Home.onClick.AddListener(() => SwitchToTab(HomeTab.Home));
        btn_Ranking.onClick.AddListener(() => SwitchToTab(HomeTab.Ranking));

        StartCoroutine(InitializeMenuBar());
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

        float canvasWidth = 0f;
        if (GameUI.Instance != null) canvasWidth = GameUI.Instance.CanvasWidth;

        if (canvasWidth <= 0f)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                var canvasRect = canvas.GetComponent<RectTransform>();
                if (canvasRect != null) canvasWidth = canvasRect.rect.width;
            }
        }

        if (canvasWidth <= 0f) return;
        rectChooseTab.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, canvasWidth / 3f);
    }

    public void SwitchToTab(HomeTab tab)
    {
        if (currentTab == tab) return;

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
        RectTransform target = null;
        switch (tab)
        {
            case HomeTab.Shop:
                if (btn_Shop != null) target = btn_Shop.GetComponent<RectTransform>();
                break;
            case HomeTab.Home:
                if (btn_Home != null) target = btn_Home.GetComponent<RectTransform>();
                break;
            case HomeTab.Ranking:
                if (btn_Ranking != null) target = btn_Ranking.GetComponent<RectTransform>();
                break;
        }

        var parentRect = rectChooseTab.parent as RectTransform;
        if (target == null || parentRect == null) return rectChooseTab.localPosition.x;

        var canvas = GetComponentInParent<Canvas>();
        float targetCenterX = RectTransformUtility.CalculateRelativeRectTransformBounds(parentRect, target).center.x;
        float chooseTabCenterX = RectTransformUtility.CalculateRelativeRectTransformBounds(parentRect, rectChooseTab).center.x;
        float pivotToChooseCenterX = chooseTabCenterX - rectChooseTab.localPosition.x;
        float x = targetCenterX - pivotToChooseCenterX;

        x += GetChooseTabOffset(tab);

        if (canvas != null)
            x = RectTransformUtility.PixelAdjustPoint(new Vector2(x, 0f), parentRect, canvas).x;

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
        GameObject fromHolder = GetHolderByTab(fromTab);
        GameObject toHolder = GetHolderByTab(toTab);

        if (fromTab != HomeTab.Home && fromHolder != null)
        {
            RectTransform fromRect = fromHolder.GetComponent<RectTransform>();
            fromRect.DOKill();
            
            float exitOffset = fromTab == HomeTab.Shop ? -slideDistance : slideDistance;
            fromRect.DOAnchorPosX(exitOffset, animationDuration)
                .SetEase(Ease.InOutSine)
                .OnComplete(() =>
                {
                    HideTabUI(fromTab);
                    fromRect.anchoredPosition = Vector2.zero;
                });
        }

        if (toTab != HomeTab.Home && toHolder != null)
        {
            ShowTabUI(toTab);
            
            RectTransform toRect = toHolder.GetComponent<RectTransform>();
            toRect.DOKill();
            
            float enterOffset = toTab == HomeTab.Shop ? -slideDistance : slideDistance;
            toRect.anchoredPosition = new Vector2(enterOffset, 0);

            toRect.DOAnchorPosX(0, animationDuration)
                .SetEase(Ease.InOutSine);
        }
    }

    private void ShowTabUI(HomeTab tab)
    {
        Hide();
        switch (tab)
        {
            case HomeTab.Shop:
                GameUI.Instance.Get<UIShop>().Show();
                break;
            case HomeTab.Ranking:
                GameUI.Instance.Get<UIPopupRank>().Show();
                break;
        }
        Show();
    }

    private void HideTabUI(HomeTab tab)
    {
        switch (tab)
        {
            case HomeTab.Shop:
                GameUI.Instance.Get<UIShop>().Hide();
                break;
            case HomeTab.Ranking:
                GameUI.Instance.Get<UIPopupRank>().Hide();
                break;
        }
    }

    private GameObject GetHolderByTab(HomeTab tab)
    {
        switch (tab)
        {
            case HomeTab.Shop:
                return GameUI.Instance.Get<UIShop>()?.holder;
            case HomeTab.Home:
                return GameUI.Instance.Get<UIHome>()?.holder;
            case HomeTab.Ranking:
                return GameUI.Instance.Get<UIPopupRank>()?.holder;
            default:
                return null;
        }
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
