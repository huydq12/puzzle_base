using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Sirenix.OdinInspector;

public class UIPopup : UIElement
{
    public override bool ManualHide => true;
    public override bool DestroyOnHide => false;
    public override bool UseBehindPanel => false;

    [Header("Animation Settings")]
    [SerializeField] private float scaleDuration = 0.3f;
    [SerializeField] private float scaleFrom = 0f;
    [SerializeField] private float scaleTo = 1f;

    [SerializeField] CanvasGroup _canvasGroup;

    [SerializeField] Button btn_close;
    public RectTransform Logo;
    public RectTransform Title;

    protected virtual void Start()
    {
        //        btn_close.onClick.AddListener(Hide);
    }

    public override void Show()
    {
        base.Show();

        // Fade in animation
        _canvasGroup.alpha = 0;
        _canvasGroup.DOFade(1, scaleDuration);

        // Scale animation
        holder.transform.localScale = Vector3.one * scaleFrom;
        holder.transform.DOScale(scaleTo, scaleDuration)
            .SetEase(Ease.OutBack).OnComplete(() =>
            {
                Logo.DOPunchScale(Vector3.one * 0.15f, 0.4f, 1, 0.5f);
                Title.DOPunchScale(Vector3.one * 0.15f, 0.4f, 1, 0.5f);
            });

    }
    public override void Hide()
    {
        // Fade out animation
        _canvasGroup.DOFade(0, scaleDuration);

        // Scale animation
        holder.transform.DOScale(scaleFrom, scaleDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                base.Hide();
            });
    }
}