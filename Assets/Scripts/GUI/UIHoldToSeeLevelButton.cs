using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoldToSeeLevelButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private CanvasGroup[] hideTargets;
    [SerializeField] private float fadeDuration = 0.12f;
    [SerializeField] private float hiddenAlpha = 0f;
    [SerializeField] private float shownAlpha = 1f;

    private bool _isHolding;

    public void OnPointerDown(PointerEventData eventData)
    {
        _isHolding = true;
        SetTargetsAlpha(hiddenAlpha);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isHolding = false;
        SetTargetsAlpha(shownAlpha);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_isHolding) return;
        _isHolding = false;
        SetTargetsAlpha(shownAlpha);
    }

    private void OnDisable()
    {
        _isHolding = false;
        SetTargetsAlphaImmediate(shownAlpha);
    }

    private void SetTargetsAlpha(float alpha)
    {
        if (hideTargets == null || hideTargets.Length == 0) return;
        for (int i = 0; i < hideTargets.Length; i++)
        {
            CanvasGroup group = hideTargets[i];
            if (group == null) continue;
            group.DOKill(false);
            group.DOFade(alpha, fadeDuration).SetEase(Ease.OutQuad);
        }
    }

    private void SetTargetsAlphaImmediate(float alpha)
    {
        if (hideTargets == null || hideTargets.Length == 0) return;
        for (int i = 0; i < hideTargets.Length; i++)
        {
            CanvasGroup group = hideTargets[i];
            if (group == null) continue;
            group.DOKill(false);
            group.alpha = alpha;
        }
    }
}

