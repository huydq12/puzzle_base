using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UITutorialBotter :UIElement
{
    public override bool ManualHide => true;
    public override bool DestroyOnHide => false;
    public override bool UseBehindPanel => false;
    
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _title;
    [SerializeField] private TextMeshProUGUI _description;

    [SerializeField] private float autoHideSeconds = 3f;
    private Coroutine autoHideRoutine;

    [SerializeField] private float _slideDuration = 0.25f;
    [SerializeField] private float _slideOffsetY = 200f;

    private RectTransform _holderRect;
    private Vector2 _holderShownPos;
    private bool _holderPosCached;
    private Tween _slideTween;

    public override void Show()
    {
        CacheHolderPos();
        _slideTween?.Kill();
        if (_holderRect != null)
        {
            _holderRect.anchoredPosition = _holderShownPos + new Vector2(0f, -_slideOffsetY);
        }
        base.Show();

        if (_holderRect != null)
        {
            _slideTween = _holderRect.DOAnchorPos(_holderShownPos, _slideDuration).SetEase(Ease.OutCubic);
        }
        StartAutoHide();
    }

    public override void Hide()
    {
        StopAutoHide();

        CacheHolderPos();
        _slideTween?.Kill();
        if (_holderRect == null)
        {
            base.Hide();
            return;
        }

        GameUI.Instance.Unsubmit(this);
        _slideTween = _holderRect.DOAnchorPos(_holderShownPos + new Vector2(0f, -_slideOffsetY), _slideDuration)
            .SetEase(Ease.InCubic)
            .OnComplete(() => base.Hide());
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

    private void StartAutoHide()
    {
        StopAutoHide();
        autoHideRoutine = StartCoroutine(AutoHideRoutine());
    }

    private void StopAutoHide()
    {
        if (autoHideRoutine == null) return;
        StopCoroutine(autoHideRoutine);
        autoHideRoutine = null;
    }

    private IEnumerator AutoHideRoutine()
    {
        yield return new WaitForSeconds(autoHideSeconds);
        Hide();
    }

    public void ShowForBooster(int boosterType)
    {
        var cfg = BoosterUnlockService.Config;
        if (cfg == null)
        {
            ShowBoosterTutorial(null, string.Empty, string.Empty);
            return;
        }

        var entry = cfg.GetEntry(boosterType);
        if (entry == null)
        {
            ShowBoosterTutorial(null, string.Empty, string.Empty);
            return;
        }

        ShowBoosterTutorial(entry.tutorialIcon, entry.tutorialTitle, entry.tutorialDescription);
    }

    public void ShowBoosterTutorial(Sprite icon, string title, string description)
    {
        if (_icon != null)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;
        }

        if (_title != null)
        {
            _title.text = title;
        }

        if (_description != null)
        {
            _description.text = description;
        }

        Show();
    }
}
