using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UILoadingInGame : Singleton<UILoadingInGame>
{
    [SerializeField] public GameObject holder;
    [SerializeField] public TextMeshProUGUI textLoading;

    [SerializeField] public Image IconGame;

    private Tween _textTween;
    private Sequence _iconGameSeq;
    private Vector3 _iconGameBaseScale;
    private Vector2 _iconGameBaseAnchoredPos;
    private Color _iconGameBaseColor;
    private bool _iconGameBaseCached;
    private Coroutine _dotsRoutine;
    private string _baseText;
    private const float DotsIntervalSeconds = 0.35f;
    private const int MaxDots = 3;

    private GameObject HolderOrSelf()
    {
        return holder != null ? holder : gameObject;
    }

    public void Show()
    {
        HolderOrSelf().SetActive(true);

        if (!_iconGameBaseCached && IconGame != null)
        {
            RectTransform baseRt = IconGame.rectTransform;
            _iconGameBaseScale = baseRt.localScale;
            _iconGameBaseAnchoredPos = baseRt.anchoredPosition;
            _iconGameBaseColor = IconGame.color;
            _iconGameBaseCached = true;
        }

        if (IconGame != null)
        {
            _iconGameSeq?.Kill();

            RectTransform rt = IconGame.rectTransform;
            rt.localScale = _iconGameBaseScale;
            rt.anchoredPosition = _iconGameBaseAnchoredPos;
            IconGame.color = _iconGameBaseColor;

            const float cycleDuration = 0.9f;
            const float floatY = 10f;
            float targetAlpha = Mathf.Clamp01(_iconGameBaseColor.a * 0.65f);

            _iconGameSeq = DOTween.Sequence().SetUpdate(true);
            _iconGameSeq.Append(rt.DOAnchorPos(_iconGameBaseAnchoredPos + new Vector2(0f, floatY), cycleDuration * 0.5f).SetEase(Ease.InOutSine));
            _iconGameSeq.Join(rt.DOScale(_iconGameBaseScale * 1.06f, cycleDuration * 0.5f).SetEase(Ease.InOutSine));
            _iconGameSeq.Join(IconGame.DOFade(targetAlpha, cycleDuration * 0.5f).SetEase(Ease.InOutSine));
            _iconGameSeq.Append(rt.DOAnchorPos(_iconGameBaseAnchoredPos, cycleDuration * 0.5f).SetEase(Ease.InOutSine));
            _iconGameSeq.Join(rt.DOScale(_iconGameBaseScale, cycleDuration * 0.5f).SetEase(Ease.InOutSine));
            _iconGameSeq.Join(IconGame.DOFade(_iconGameBaseColor.a, cycleDuration * 0.5f).SetEase(Ease.InOutSine));
            _iconGameSeq.SetLoops(-1, LoopType.Restart);
        }

        if (textLoading != null)
        {
            _baseText = "Voodoo";
            textLoading.text = _baseText;

            _textTween?.Kill();
            textLoading.alpha = 1f;
            _textTween = textLoading.DOFade(0.25f, 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);

            if (_dotsRoutine != null) StopCoroutine(_dotsRoutine);
            _dotsRoutine = StartCoroutine(AnimateDots());
        }
    }

    public void Hide()
    {
        if (_dotsRoutine != null)
        {
            StopCoroutine(_dotsRoutine);
            _dotsRoutine = null;
        }

        if (_textTween != null)
        {
            _textTween.Kill();
            _textTween = null;
        }

        if (_iconGameSeq != null)
        {
            _iconGameSeq.Kill();
            _iconGameSeq = null;
        }

        if (textLoading != null)
        {
            textLoading.alpha = 1f;
            if (!string.IsNullOrEmpty(_baseText)) textLoading.text = _baseText;
        }

        if (IconGame != null && _iconGameBaseCached)
        {
            IconGame.rectTransform.localScale = _iconGameBaseScale;
            IconGame.rectTransform.anchoredPosition = _iconGameBaseAnchoredPos;
            IconGame.color = _iconGameBaseColor;
        }
        HolderOrSelf().SetActive(false);
    }

    private IEnumerator AnimateDots()
    {
        int dots = 0;
        while (true)
        {
            if (textLoading != null)
            {
                string suffix = dots == 0 ? string.Empty : new string('.', dots);
                textLoading.text = _baseText + suffix;
            }

            dots = (dots + 1) % (MaxDots + 1);
            yield return new WaitForSecondsRealtime(DotsIntervalSeconds);
        }
    }
}
