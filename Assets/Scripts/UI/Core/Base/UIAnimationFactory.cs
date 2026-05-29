using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class UIAnimationFactory
{
    public static Sequence CreateShow(BaseUIElement element, UIAnimType animType)
    {
        var rt = element.transform as RectTransform;
        Sequence seq = DOTween.Sequence();

        switch (animType)
        {
            case UIAnimType.None:
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
                return null;

            case UIAnimType.Fade:
                UIFadeUtil.DOFade(rt, 1f, 0.3f);
                break;

            case UIAnimType.SlideLeft:
                rt.anchoredPosition = new Vector2(-Screen.width, 0);
                seq.Append(rt.DOAnchorPosX(0, 0.3f).SetEase(Ease.OutCubic));
                break;

            case UIAnimType.SlideRight:
                rt.anchoredPosition = new Vector2(Screen.width, 0);
                seq.Append(rt.DOAnchorPosX(0, 0.3f).SetEase(Ease.OutCubic));
                break;

            case UIAnimType.ScaleUp:
                rt.localScale = Vector3.zero;
                seq.Append(rt.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
                break;

            case UIAnimType.Popup:
                rt.localScale = new Vector3(0.8f, 0.8f, 1);
                seq.Append(rt.DOScale(1f, 0.25f).SetEase(Ease.OutBack));
                break;
        }

        return seq;
    }

    public static Sequence CreateHide(BaseUIElement element, UIAnimType animType)
    {
        var rt = element.transform as RectTransform;
        Sequence seq = DOTween.Sequence();

        switch (animType)
        {
            case UIAnimType.None:
                rt.localScale = Vector3.zero;
                return null;

            case UIAnimType.Fade:
                UIFadeUtil.DOFade(rt, 0f, 0.3f);
                break;

            case UIAnimType.SlideLeft:
                seq.Append(rt.DOAnchorPosX(-Screen.width, 0.3f));
                break;

            case UIAnimType.SlideRight:
                seq.Append(rt.DOAnchorPosX(Screen.width, 0.3f));
                break;

            case UIAnimType.ScaleUp:
                seq.Append(rt.DOScale(0f, 0.2f));
                break;

            case UIAnimType.Popup:
                seq.Append(rt.DOScale(0.8f, 0.2f));
                break;
        }

        return seq;
    }
}

public static class UIFadeUtil
{
    private static readonly Dictionary<Graphic, float> _alphaCache = new();
    private static readonly Dictionary<Transform, Sequence> _sequenceCache = new();

    public static Sequence DOFade(Transform root, float endValue, float duration)
    {
        if (root == null) return null;

        if (_sequenceCache.TryGetValue(root, out var oldSeq))
        {
            if (oldSeq.IsActive())
                oldSeq.Kill();
        }

        var graphics = root.GetComponentsInChildren<Graphic>(true);

        Sequence seq = DOTween.Sequence();
        seq.SetAutoKill(false);

        foreach (var g in graphics)
        {
            if (g == null) continue;

            g.DOKill();

            if (!_alphaCache.TryGetValue(g, out float originalAlpha))
            {
                originalAlpha = g.color.a;
                _alphaCache[g] = originalAlpha;
            }

            float targetAlpha = originalAlpha * endValue;

            seq.Join(g.DOFade(targetAlpha, duration));
        }

        _sequenceCache[root] = seq;

        return seq;
    }

    public static void Restore(Transform root)
    {
        if (root == null) return;

        var graphics = root.GetComponentsInChildren<Graphic>(true);

        foreach (var g in graphics)
        {
            if (g == null) continue;

            g.DOKill();

            if (_alphaCache.TryGetValue(g, out float originalAlpha))
            {
                var c = g.color;
                c.a = originalAlpha;
                g.color = c;
            }
        }
    }

    public static void ClearCache(Transform root)
    {
        if (root == null) return;

        if (_sequenceCache.TryGetValue(root, out var seq))
        {
            if (seq.IsActive())
                seq.Kill();

            _sequenceCache.Remove(root);
        }

        var graphics = root.GetComponentsInChildren<Graphic>(true);

        foreach (var g in graphics)
        {
            if (g != null)
                _alphaCache.Remove(g);
        }
    }
}