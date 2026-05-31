using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UITutorial : BaseScreen
{
    public override bool ManualHide => true;
    public override bool DestroyOnHide => false;
    public override bool UseBehindPanel => false;
    
    [Header("Config (optional)")]
    [SerializeField] private TutorialPopupConfig _config;

    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _title;
    [SerializeField] private TextMeshProUGUI _description;
    [SerializeField] private Button _btnClose;

    private bool _useOverrideContent;
    private Sprite _overrideIcon;
    private string _overrideTitle;
    private string _overrideDescription;

    [SerializeField] private float _slideDuration = 0.25f;
    [SerializeField] private float _slideOffsetY = 200f;

    private RectTransform _holderRect;
    private Vector2 _holderShownPos;
    private bool _holderPosCached;
    private Tween _slideTween;

    private TutorialPopupConfig Config => _config != null ? _config : TutorialPopupService.Config;

    void Start()
    {
        if (_btnClose != null)
        {
            _btnClose.onClick.AddListener(Hide);
        }
    }

    public override void BeforeShow()
    {
        base.BeforeShow();
        RefreshView();

        CacheHolderPos();
        _slideTween?.Kill();
        if (_holderRect != null)
        {
            _holderRect.anchoredPosition = _holderShownPos + new Vector2(0f, -_slideOffsetY);
        }
        if (_holderRect != null)
        {
            _slideTween = _holderRect.DOAnchorPos(_holderShownPos, _slideDuration).SetEase(Ease.OutCubic);
        }
    }

    public override void BeforeHide()
    {
        base.BeforeHide();
        _useOverrideContent = false;

        CacheHolderPos();
        _slideTween?.Kill();
        if (_holderRect == null) return;

        _slideTween = _holderRect.DOAnchorPos(_holderShownPos + new Vector2(0f, -_slideOffsetY), _slideDuration)
            .SetEase(Ease.InCubic)
            .OnComplete(() => _slideTween = null);
    }

    private void CacheHolderPos()
    {
        if (_holderPosCached) return;
        if (holder == null) return;
        _holderRect = holder.GetComponent<RectTransform>();
        if (_holderRect == null) return;
        _holderShownPos = _holderRect.anchoredPosition;
        _holderPosCached = true;
    }

    public void ShowBoosterTutorial(Sprite icon, string title, string description)
    {
        _useOverrideContent = true;
        _overrideIcon = icon;
        _overrideTitle = title;
        _overrideDescription = description;
        UIManager.Instance.ShowUI<UITutorial>(null, true, false, UIAnimType.None);
    }

    private void RefreshView()
    {
        if (_useOverrideContent)
        {
            if (_icon != null)
            {
                _icon.sprite = _overrideIcon;
                _icon.enabled = _overrideIcon != null;
            }
            if (_title != null) _title.text = _overrideTitle ?? string.Empty;
            if (_description != null) _description.text = _overrideDescription ?? string.Empty;
            return;
        }

        if (TutorialManager.Instance == null) return;

        var cfg = Config;
        if (cfg != null)
        {
            int level = Mathf.Max(1, GameManagerInGame.Instance.CurrentLevel);
            var entry = cfg.GetEntry(level);
            if (entry != null)
            {
                ApplyTutorialContent(entry.icon, entry.title, entry.description);
                return;
            }
        }

	        ApplyTutorialContent(null, string.Empty, string.Empty);
	    }

    private void ApplyTutorialContent(Sprite icon, string title, string description)
    {
        if (_icon != null)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;
        }
        if (_title != null) _title.text = title ?? string.Empty;
        if (_description != null) _description.text = description ?? string.Empty;
    }
}
