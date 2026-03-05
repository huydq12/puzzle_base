using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class UIPauseLose : UIElement
{
    public override bool ManualHide => true;
    public override bool DestroyOnHide => false;
    public override bool UseBehindPanel => false;

    [SerializeField] private RectTransform step1;
    [SerializeField] private RectTransform step2;
    [SerializeField] private Button btnClose;

    [SerializeField] private Button btnUseRainbow;

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

    private void CacheStep1Pos()
    {
        if (_cachedStep1Pos) return;
        if (step1 == null) return;
        _step1ShownPos = step1.anchoredPosition;
        _cachedStep1Pos = true;
    }
}
