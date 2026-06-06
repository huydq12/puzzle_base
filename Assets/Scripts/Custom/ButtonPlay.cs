using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using Sirenix.OdinInspector;
using System;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Button))]
public class ButtonPlay : MonoBehaviour, IPointerClickHandler
{
    public enum TweenType
    {
        None,
        ScalePunch,
        ScaleUp,
        RotatePunch,
        MoveY
    }

    public enum ColorEffectType
    {
        None,
        Darken,
        Lighten,
        Tint
    }

    [Header("Condition (Injected)")]
    public bool defaultInteractable = true;
    public UnityEvent onInvalidClick;

    // Runtime-only injected condition.
    public Func<bool> CanClick;

    [Header("Tween Settings")]
    public TweenType tweenType = TweenType.ScalePunch;
    public float duration = 0.2f;
    public float strength = 0.2f;

    [Header("Sound")]
    [SerializeField] private SFXType _soundClick = SFXType.UIClick;

    [Header("Sprite State")]
    [SerializeField] private Sprite _clickSprite;
    [SerializeField] private Sprite _defaultSprite;

    [Header("ButtonBehavior Animate")]
    [SerializeField] private bool _activeAnimate;
    [SerializeField, ShowIf(nameof(_activeAnimate))] private float _bounceScale = 1.2f;
    [SerializeField, ShowIf(nameof(_activeAnimate))] private float _bounceDuration = 0.2f;

    [Header("Color Effect")]
    public ColorEffectType colorEffect = ColorEffectType.Darken;
    public Color tintColor = Color.gray;
    public float colorDuration = 0.1f;
    [Range(0, 1)] public float disabledMultiplier = 0.6f; // Độ tối khi bị disable

    [Header("Target (Auto Detect)")]
    [SerializeField] private Button _button;

    [Header("Event")]
    public UnityEvent onClick;

    private Vector3 _originalScale;
    private Vector3 _originalPos;
    private Quaternion _originalRot;
    private Color _originalColor;

    private Sequence _sequence;
    private bool _isInitialized;
    private RectTransform _rectTransform;

    // ================= AUTO DETECT =================

    private void Reset()
    {
        AutoDetect();
    }

    private void OnValidate()
    {
        AutoDetect();
        ValidateEditorTweenMode();
    }

    private void ValidateEditorTweenMode()
    {
        if (Application.isPlaying) return;

        if (_activeAnimate)
        {
            tweenType = TweenType.None;
        }
        else if (tweenType == TweenType.None)
        {
            tweenType = TweenType.ScalePunch;
        }
    }

    private void AutoDetect()
    {
        if (_button == null)
            _button = GetComponent<Button>();

        if (_rectTransform == null)
            _rectTransform = _button != null ? _button.transform as RectTransform : GetComponent<RectTransform>();
    }

    // ================= LIFECYCLE =================

    private void Awake()
    {
        // Chỉ lưu màu gốc khi game bắt đầu chạy
        if (Application.isPlaying)
        {
            CacheOriginal();
        }
    }

    private void OnEnable()
    {
        // 🔥 FIX QUAN TRỌNG: Chặn không cho đổi màu trong Edit Mode
        if (!Application.isPlaying)
        {

            return;
        }

        if (!_isInitialized)
        {
            CacheOriginal();
        }

        // Cập nhật lại trạng thái màu sắc khi object được bật lại
        ApplyInteractableState(false);
    }

    private void OnDisable()
    {
        if (!Application.isPlaying) return;

        KillTweens();
        ResetState();
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying) return;

        KillTweens();
    }

    private void KillTweens()
    {
        Graphic buttonGraphic = GetButtonGraphic();

        _sequence?.Kill();
        _sequence = null;
        _rectTransform?.DOKill();
        buttonGraphic?.DOKill();
    }

    private void CacheOriginal()
    {
        AutoDetect();

        if (_rectTransform == null) return;

        _originalScale = _rectTransform.localScale;
        _originalPos = _rectTransform.localPosition;
        _originalRot = _rectTransform.localRotation;

        Graphic buttonGraphic = GetButtonGraphic();
        Image buttonImage = GetButtonImage();

        if (buttonGraphic != null)
            _originalColor = buttonGraphic.color;

        if (_defaultSprite == null && buttonImage != null)
            _defaultSprite = buttonImage.sprite;

        _isInitialized = true;
    }

    // ================= API =================

    /// <summary>
    /// Bật/Tắt nút. Nếu tắt, nút sẽ tối đi và không thể click.
    /// </summary>
    public void SetInteractable(bool value, bool animated = true)
    {
        AutoDetect();

        if (_button != null)
        {
            _button.interactable = value;
        }

        ApplyInteractableState(animated);
    }

    public void SetCondition(Func<bool> condition)
    {
        CanClick = condition;
    }

    public void SetSprite(Sprite sprite)
    {
        Image buttonImage = GetButtonImage();

        if (buttonImage != null)
        {
            buttonImage.sprite = sprite;
        }
    }

    private void ApplyInteractableState(bool animated)
    {
        if (!_isInitialized)
        {
            CacheOriginal();
        }

        Graphic buttonGraphic = GetButtonGraphic();
        if (buttonGraphic == null) return;

        Color targetCol = IsButtonInteractable() ? _originalColor : GetDisabledColor();

        buttonGraphic.DOKill();

        if (animated && Application.isPlaying)
        {
            buttonGraphic.DOColor(targetCol, colorDuration).SetUpdate(true); // ✅ Chạy được cả khi TimeScale = 0
        }
        else
        {
            buttonGraphic.color = targetCol;
        }
    }

    private Color GetDisabledColor()
    {
        return new Color(
            _originalColor.r * disabledMultiplier,
            _originalColor.g * disabledMultiplier,
            _originalColor.b * disabledMultiplier,
            _originalColor.a
        );
    }

    // ================= CLICK =================

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!Application.isPlaying) return; // 🔥 Fix: Không cho click trong chế độ Edit Mode của Editor

        // ❌ Check internal interactable state
        if (!IsButtonInteractable()) return;

        // ❌ Spam guard
        if (_sequence != null && _sequence.IsActive() && _sequence.IsPlaying())
            return;

        // ❌ Condition check (dynamic)
        if (!IsValidClick())
        {
            onInvalidClick?.Invoke();
            return;
        }

        // ✅ Valid
        PlayAll();
    }

    private bool IsValidClick()
    {
        if (CanClick != null)
            return CanClick.Invoke();

        return defaultInteractable;
    }

    private bool IsButtonInteractable()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
        }

        return _button != null && _button.interactable;
    }

    // ================= MAIN TWEEN =================

    private void PlayAll()
    {
        if (_rectTransform == null)
        {
            PlayClickSound();
            SetClickSprite();
            onClick?.Invoke();
            return;
        }

        _sequence?.Kill();
        // 🔥 Fix: Đảm bảo UI button hoạt động bình thường khi Pause game (Time.timeScale = 0)
        _sequence = DOTween.Sequence().SetUpdate(true);
        _sequence.AppendCallback(PlayClickSound);
        _sequence.AppendCallback(SetClickSprite);

        // ===== TWEEN =====
        if (_activeAnimate)
        {
            _rectTransform.localScale = _originalScale;
            _sequence.Append(_rectTransform.DOScale(_originalScale * _bounceScale, _bounceDuration / 2f).SetEase(Ease.OutQuad));
            _sequence.Append(_rectTransform.DOScale(_originalScale, _bounceDuration / 2f).SetEase(Ease.InQuad));
        }
        else
        {
            switch (tweenType)
            {
                case TweenType.ScalePunch:
                    _sequence.Append(_rectTransform.DOPunchScale(Vector3.one * strength, duration, 10, 1));
                    break;

                case TweenType.ScaleUp:
                    _rectTransform.localScale = _originalScale;
                    _sequence.Append(
                        _rectTransform.DOScale(_originalScale * (1f + strength), duration)
                            .SetLoops(2, LoopType.Yoyo)
                    );
                    break;

                case TweenType.RotatePunch:
                    _sequence.Append(
                        _rectTransform.DOPunchRotation(Vector3.forward * strength * 50f, duration, 10, 1)
                    );
                    break;

                case TweenType.MoveY:
                    _rectTransform.localPosition = _originalPos;
                    _sequence.Append(
                        _rectTransform.DOLocalMoveY(_originalPos.y + strength * 100f, duration)
                            .SetLoops(2, LoopType.Yoyo)
                    );
                    break;
            }
        }

        // ===== COLOR CLICK EFFECT =====
        Graphic buttonGraphic = GetButtonGraphic();
        if (buttonGraphic != null && colorEffect != ColorEffectType.None)
        {
            Color effectColor = _originalColor;

            switch (colorEffect)
            {
                case ColorEffectType.Darken:
                    effectColor = new Color(_originalColor.r * 0.7f, _originalColor.g * 0.7f, _originalColor.b * 0.7f, _originalColor.a);
                    break;
                case ColorEffectType.Lighten:
                    // 🔥 Fix: Phải Clamp để màu không vượt quá 1, tránh lỗi over-exposure HDR
                    effectColor = new Color(
                        Mathf.Clamp01(_originalColor.r * 1.3f),
                        Mathf.Clamp01(_originalColor.g * 1.3f),
                        Mathf.Clamp01(_originalColor.b * 1.3f),
                        _originalColor.a
                    );
                    break;
                case ColorEffectType.Tint:
                    effectColor = new Color(tintColor.r, tintColor.g, tintColor.b, _originalColor.a);
                    break;
            }

            _sequence.Join(
                buttonGraphic.DOColor(effectColor, colorDuration).SetLoops(2, LoopType.Yoyo)
            );
        }

        // ===== CALLBACK =====
        _sequence.OnComplete(() =>
        {
            _sequence = null;
            ResetState();

            // 🔥 Fix: Kiểm tra GameObject còn tồn tại hay không trước khi Invoke
            // (Trường hợp logic của game tắt nút đi trước khi chạy xong animation)
            if (this != null && gameObject.activeInHierarchy)
            {
                onClick?.Invoke();
            }
        });

        _sequence.OnKill(() => _sequence = null);
    }

    private void PlayClickSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(_soundClick);
        }
    }

    private void SetClickSprite()
    {
        Image buttonImage = GetButtonImage();

        if (buttonImage != null && _clickSprite != null)
        {
            buttonImage.sprite = _clickSprite;
        }
    }

    private Graphic GetButtonGraphic()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
        }

        return _button != null ? _button.targetGraphic : null;
    }

    private Image GetButtonImage()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
        }

        return _button != null ? _button.image : null;
    }

    // ================= RESET =================

    private void ResetState()
    {
        if (_rectTransform == null) return;

        _rectTransform.localScale = _originalScale;
        _rectTransform.localPosition = _originalPos;
        _rectTransform.localRotation = _originalRot;

        Graphic buttonGraphic = GetButtonGraphic();
        if (buttonGraphic != null)
        {
            // Trở về màu dựa trên trạng thái interactable hiện tại
            buttonGraphic.color = IsButtonInteractable() ? _originalColor : GetDisabledColor();
        }

        Image buttonImage = GetButtonImage();
        if (buttonImage != null && _defaultSprite != null)
        {
            buttonImage.sprite = _defaultSprite;
        }
    }
}
