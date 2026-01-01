using DG.Tweening;
using Sirenix.OdinInspector;
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
    public UnityEvent OnClick;
    [SerializeField] private bool _activeAnimate;
    [SerializeField, ShowIf(nameof(_activeAnimate))] private float _bounceScale = 1.2f;
    [SerializeField, ShowIf(nameof(_activeAnimate))] private float _bounceDuration = 0.2f;
    private RectTransform _rectTransform;
    private Vector3 _defaultScale;
    private ButtonState _state;
    private Sequence _clickSequence;
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
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (_button == null || _button.interactable)
        {
            OnButtonClickAnimate();
            OnClick?.Invoke();
            SetState(ButtonState.Click);
        }
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
