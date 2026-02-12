using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UILoadingInGame : Singleton<UILoadingInGame>
{
    [SerializeField] public GameObject holder;
    [SerializeField] public TextMeshProUGUI textLoading;

    [SerializeField] public Image playerIcon1;
    [SerializeField] public Image playerIcon2;

    private Tween _textTween;
    private Sequence _icon1Seq;
    private Sequence _icon2Seq;
    private Vector2 _icon1BasePos;
    private Vector2 _icon2BasePos;
    private Vector3 _icon1BaseScale;
    private Vector3 _icon2BaseScale;
    private Quaternion _icon1BaseRot;
    private Quaternion _icon2BaseRot;
    private bool _iconBasesCached;
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

        if (!_iconBasesCached)
        {
            if (playerIcon1 != null)
            {
                _icon1BasePos = playerIcon1.rectTransform.anchoredPosition;
                _icon1BaseScale = playerIcon1.rectTransform.localScale;
                _icon1BaseRot = playerIcon1.rectTransform.localRotation;
            }

            if (playerIcon2 != null)
            {
                _icon2BasePos = playerIcon2.rectTransform.anchoredPosition;
                _icon2BaseScale = playerIcon2.rectTransform.localScale;
                _icon2BaseRot = playerIcon2.rectTransform.localRotation;
            }

            _iconBasesCached = true;
        }

        if (playerIcon1 != null)
        {
            _icon1Seq?.Kill();
            var rt = playerIcon1.rectTransform;
            rt.anchoredPosition = _icon1BasePos;
            rt.localScale = _icon1BaseScale;
            rt.localRotation = _icon1BaseRot;

            _icon1Seq = DOTween.Sequence().SetUpdate(true);
            _icon1Seq.Append(rt.DOAnchorPos(_icon1BasePos + new Vector2(-14f, 6f), 0.06f).SetEase(Ease.OutQuad));
            _icon1Seq.Join(rt.DOPunchRotation(new Vector3(0f, 0f, 10f), 0.12f, 14, 0.9f));
            _icon1Seq.Join(rt.DOPunchScale(_icon1BaseScale * 0.08f, 0.12f, 10, 0.9f));
            _icon1Seq.Append(rt.DOAnchorPos(_icon1BasePos, 0.09f).SetEase(Ease.InQuad));
            _icon1Seq.AppendInterval(0.06f);
            _icon1Seq.SetLoops(-1, LoopType.Restart);
        }

        if (playerIcon2 != null)
        {
            _icon2Seq?.Kill();
            var rt = playerIcon2.rectTransform;
            rt.anchoredPosition = _icon2BasePos;
            rt.localScale = _icon2BaseScale;
            rt.localRotation = _icon2BaseRot;

            _icon2Seq = DOTween.Sequence().SetUpdate(true);
            _icon2Seq.Append(rt.DOAnchorPos(_icon2BasePos + new Vector2(-10f, 4f), 0.06f).SetEase(Ease.OutQuad));
            _icon2Seq.Join(rt.DOPunchRotation(new Vector3(0f, 0f, -8f), 0.12f, 12, 0.9f));
            _icon2Seq.Join(rt.DOPunchScale(_icon2BaseScale * 0.06f, 0.12f, 10, 0.9f));
            _icon2Seq.Append(rt.DOAnchorPos(_icon2BasePos, 0.09f).SetEase(Ease.InQuad));
            _icon2Seq.AppendInterval(0.08f);
            _icon2Seq.SetLoops(-1, LoopType.Restart);
        }

        if (textLoading != null)
        {
            _baseText = "LOADING DATA";
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

        if (_icon1Seq != null)
        {
            _icon1Seq.Kill();
            _icon1Seq = null;
        }

        if (_icon2Seq != null)
        {
            _icon2Seq.Kill();
            _icon2Seq = null;
        }

        if (textLoading != null)
        {
            textLoading.alpha = 1f;
            if (!string.IsNullOrEmpty(_baseText)) textLoading.text = _baseText;
        }

        if (playerIcon1 != null)
        {
            playerIcon1.rectTransform.anchoredPosition = _icon1BasePos;
            playerIcon1.rectTransform.localScale = _icon1BaseScale;
            playerIcon1.rectTransform.localRotation = _icon1BaseRot;
        }

        if (playerIcon2 != null)
        {
            playerIcon2.rectTransform.anchoredPosition = _icon2BasePos;
            playerIcon2.rectTransform.localScale = _icon2BaseScale;
            playerIcon2.rectTransform.localRotation = _icon2BaseRot;
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
