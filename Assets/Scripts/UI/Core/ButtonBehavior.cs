using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum ButtonState
{
    Click,
    Default
}

[DisallowMultipleComponent]
public class ButtonBehavior : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private Image _frame;
    [SerializeField] private Sprite _clickSprite;
    [SerializeField] private Sprite _defaultSprite;
    [SerializeField] private SFXType _soundClick;

    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private bool _activeTextScale;
    [SerializeField, ShowIf(nameof(_activeTextScale))] private float _textBounceDuration = 0.2f;
    [SerializeField] private GameObject _activeObject;
    [SerializeField] private GameObject _deactiveObject;

    public UnityEvent OnClick;
    [SerializeField] private bool _activeAnimate;
    [SerializeField, ShowIf(nameof(_activeAnimate))] private float _bounceScale = 1.2f;
    [SerializeField, ShowIf(nameof(_activeAnimate))] private float _bounceDuration = 0.2f;

    private RectTransform _rectTransform;
    private Vector3 _defaultScale;
    private ButtonState _state;
    private Sequence _clickSequence;
    private Tween _textScaleTween;
    private Button _button;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _button = GetComponent<Button>();
        _defaultScale = _rectTransform.localScale;

        if (_defaultSprite == null && _frame != null)
        {
            _defaultSprite = _frame.sprite;
        }

        if (_activeTextScale && _text != null)
        {
            var isSelected = _frame != null && _clickSprite != null && _frame.sprite == _clickSprite;
            _state = isSelected ? ButtonState.Click : ButtonState.Default;
            _text.rectTransform.localScale = isSelected ? Vector3.one : Vector3.zero;
        }

        RefreshStateObjects();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_button == null || _button.interactable)
        {
            OnButtonClickAnimate();
            SetState(ButtonState.Click);
            OnClick?.Invoke();
        }
    }

    public void SetSelected(bool isSelected, bool instant = false)
    {
        SetState(isSelected ? ButtonState.Click : ButtonState.Default);

        if (!_activeTextScale || _text == null)
        {
            return;
        }

        Vector3 targetScale = isSelected ? Vector3.one : Vector3.zero;

        _textScaleTween?.Kill();

        if (instant)
        {
            _text.rectTransform.localScale = targetScale;
            return;
        }

        _textScaleTween = _text.rectTransform
            .DOScale(targetScale, _textBounceDuration)
            .SetEase(Ease.OutQuad);
    }

    private void SetState(ButtonState state)
    {
        _state = state;
        if (_frame != null)
        {
            _frame.sprite = state switch
            {
                ButtonState.Click when _clickSprite != null => _clickSprite,
                ButtonState.Default when _defaultSprite != null => _defaultSprite,
                _ => _frame.sprite
            };
        }

        RefreshStateObjects();
    }

    private void RefreshStateObjects()
    {
        bool isSelected = _state == ButtonState.Click;

        if (_activeObject != null)
        {
            _activeObject.SetActive(isSelected);
        }

        if (_deactiveObject != null)
        {
            _deactiveObject.SetActive(!isSelected);
        }
    }

    private void OnButtonClickAnimate()
    {
        if (_clickSequence != null)
        {
            return;
        }

        if (!_activeAnimate)
        {
            AudioManager.Instance.PlaySFX(_soundClick);
            return;
        }

        _clickSequence = DOTween.Sequence();
        _clickSequence.Append(_rectTransform.DOScale(_bounceScale, _bounceDuration / 2).SetEase(Ease.OutQuad));
        _clickSequence.AppendCallback(() => { AudioManager.Instance.PlaySFX(_soundClick); });
        _clickSequence.Append(_rectTransform.DOScale(_defaultScale, _bounceDuration / 2).SetEase(Ease.InQuad));
        _clickSequence.OnComplete(() => _clickSequence = null);
    }
}
