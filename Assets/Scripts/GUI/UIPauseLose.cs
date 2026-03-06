using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;

public class UIPauseLose : UIElement
{
    public override bool ManualHide => true;
    public override bool DestroyOnHide => false;
    public override bool UseBehindPanel => false;

    [SerializeField] private RectTransform step1;
    [SerializeField] private RectTransform step2;

    [SerializeField] private TextMeshProUGUI txt_coin;
    
    [SerializeField] private Button btnClose;

    [SerializeField] private Button btnUseRainbow;

    [SerializeField] private Button btnUseRainbowBuyCoin;

    [Header("Use Rainbow (Buy with coin)")]
    [SerializeField] private int useRainbowCoinPrice = 200;
    [SerializeField] private string notEnoughGoldToast = "Not enough gold";

    [Header("Animation")]
    [SerializeField] private float step1MoveFromOffsetY = 800f;
    [SerializeField] private float step1MoveDuration = 0.35f;
    [SerializeField] private Ease step1MoveEase = Ease.OutCubic;

    [SerializeField] private float step2ShowDuration = 0.2f;
    [SerializeField] private float step2ScaleFrom = 0.85f;
    [SerializeField] private Ease step2ShowEase = Ease.OutBack;

    private Vector2 _step1ShownPos;
    private bool _cachedStep1Pos;

    private Tween _step1Tween;
    private Tween _step2Tween;

    public override void Show()
    {
        CacheStep1Pos();
        RefreshCoin();

        KillTweensInHierarchy(false);
        _step1Tween?.Kill();
        _step2Tween?.Kill();

        if (step1 != null)
        {
            step1.gameObject.SetActive(true);
            step1.anchoredPosition = _step1ShownPos + new Vector2(0f, step1MoveFromOffsetY);
        }

        CanvasGroup step2Group = step2 != null ? step2.GetComponent<CanvasGroup>() : null;
        if (step2 != null)
        {
            step2.gameObject.SetActive(false);
            step2.localScale = Vector3.one * step2ScaleFrom;
        }
        if (step2Group != null) step2Group.alpha = 0f;

        base.Show();

        if (step1 == null)
        {
            ShowStep2(step2Group);
            return;
        }

        _step1Tween = step1.DOAnchorPos(_step1ShownPos, step1MoveDuration)
            .SetEase(step1MoveEase)
            .OnComplete(() => ShowStep2(step2Group));
    }

    private void ShowStep2(CanvasGroup step2Group)
    {
        if (step2 == null) return;

        if (step1 != null) step1.gameObject.SetActive(false);
        step2.gameObject.SetActive(true);

        if (step2Group != null)
            _step2Tween = step2Group.DOFade(1f, step2ShowDuration).SetEase(Ease.OutQuad);

        step2.localScale = Vector3.one * step2ScaleFrom;
        step2.DOScale(1f, step2ShowDuration).SetEase(step2ShowEase);
    }

    public override void Hide()
    {
        KillTweensInHierarchy(false);
        _step1Tween?.Kill();
        _step2Tween?.Kill();
        base.Hide();

        GameUI.Instance.Get<UILose>().Show();
    }

    private void Start()
    {
        if (btnClose != null)
            btnClose.onClick.AddListener(Hide);

        if (btnUseRainbow != null)
            btnUseRainbow.onClick.AddListener(UseRainbow);

        if (btnUseRainbowBuyCoin != null)
            btnUseRainbowBuyCoin.onClick.AddListener(UseRainbowBuyCoin);
    }

    private void UseRainbow()
    {
        if (Board.Instance != null)
        {
            Board.Instance.SpawnLoseRainbowShooter();
        }

        if (GameManagerInGame.Instance != null)
        {
            GameManagerInGame.Instance.SetState(GameStateInGame.Playing);
        }

        KillTweensInHierarchy(false);
        _step1Tween?.Kill();
        _step2Tween?.Kill();

        // Hide this UI without chaining to the standard lose popup.
        base.Hide();
    }

    private void UseRainbowBuyCoin()
    {
        if (useRainbowCoinPrice > 0)
        {
            bool spent = InventoryManager.Instance != null && InventoryManager.Instance.SpendCoin(useRainbowCoinPrice);
            if (!spent)
            {
                var toast = GameUI.Instance != null ? GameUI.Instance.Get<UINotification>() : null;
                if (toast != null)
                    toast.ShowToast(notEnoughGoldToast);
                RefreshCoin();
                return;
            }
        }

        RefreshCoin();
        UseRainbow();
    }

    private void RefreshCoin()
    {
        if (txt_coin == null) return;

        int coin = 0;
        if (InventoryManager.Instance != null)
            coin = InventoryManager.Instance.GetCoin();
        else if (GameManagerInGame.Instance != null && GameManagerInGame.Instance.userData != null)
            coin = GameManagerInGame.Instance.userData.playerCash;
        else if (GameManager.Instance != null && GameManager.Instance.userData != null)
            coin = GameManager.Instance.userData.playerCash;

        txt_coin.text = coin.ToString();
    }

    private void CacheStep1Pos()
    {
        if (_cachedStep1Pos) return;
        if (step1 == null) return;
        _step1ShownPos = step1.anchoredPosition;
        _cachedStep1Pos = true;
    }
}
