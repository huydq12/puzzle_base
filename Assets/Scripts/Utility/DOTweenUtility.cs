using DG.Tweening;
using UnityEngine;

public static class DOTweenUtility
{
    public static Tween ScaleFrom(this Transform target, float fromScale, float toScale, float duration, Ease ease = Ease.OutBack)
    {
        if (target == null) return null;

        target.localScale = Vector3.one * fromScale;
        return target.DOScale(Vector3.one * toScale, duration).SetEase(ease);
    }

    public static Tween ScaleLoop(this Transform target, float toScale = 1.1f, float duration = 0.5f)
    {
        if (target == null) return null;

        return target.DOScale(Vector3.one * toScale, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }
}
