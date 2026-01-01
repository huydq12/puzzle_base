using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class Slot : MonoBehaviour
{
    [ReadOnly] public ButtonCircle ButtonInSlot;
    [SerializeField] private SpriteRenderer _warning;
    [SerializeField] private float _fadeDuration = 0.5f;

    private Tween _warningTween;
    private bool _isWarning;

    public bool IsOccupied => ButtonInSlot != null;
    public bool IsWarning
    {
        get { return _isWarning; }
        set
        {
            if (_isWarning == value) return;
            _isWarning = value;

            if (_isWarning)
            {
                StartWarningAnimation();
            }
            else
            {
                StopWarningAnimation();
            }
        }
    }

    private void StartWarningAnimation()
    {
        _warning.enabled = true;
        _warningTween?.Kill();

        Color color = _warning.color;
        color.a = 1f;
        _warning.color = color;

        _warningTween = _warning.DOFade(0f, _fadeDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    private void StopWarningAnimation()
    {
        _warningTween?.Kill();
        _warningTween = null;
        _warning.enabled = false;

        Color color = _warning.color;
        color.a = 1f;
        _warning.color = color;
    }
}
