using UnityEngine;
using DG.Tweening;

public class Key : MonoBehaviour
{
    [SerializeField] private Vector3 _offset = new Vector3(0f, 0.4f, 0f);
    [SerializeField] private GameObject _trail;
    [SerializeField] private float _hideDelayAfterUnlock = 0.35f;
    [SerializeField] private float _flyDuration = 0.35f;
    [SerializeField] private Ease _flyEase = Ease.InQuad;
    [SerializeField] private float _scaleOutDuration = 0.2f;

    public Vector3 UnlockTargetPosition => transform.position;
    public bool IsFlying { get; private set; }

    public void SetAnchorPosition(Vector3 anchorPosition)
    {
        transform.position = anchorPosition + _offset;
    }

    public void Consume()
    {
        if (_hideDelayAfterUnlock <= 0f)
        {
            gameObject.SetActive(false);
            return;
        }

        DOVirtual.DelayedCall(_hideDelayAfterUnlock, () =>
        {
            if (this != null && gameObject != null)
                gameObject.SetActive(false);
        });
    }

    public void FlyTo(Transform target, System.Action onArrive = null)
    {
        transform.DOKill();
        transform.SetParent(null, true);
        IsFlying = true;
        if (_trail != null) _trail.SetActive(true);

        Vector3 targetPos = target != null ? target.position : transform.position;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOMove(targetPos, _flyDuration).SetEase(_flyEase));
        if (_scaleOutDuration > 0f)
        {
            float scaleStart = Mathf.Max(0f, _flyDuration - _scaleOutDuration);
            seq.Insert(scaleStart, transform.DOScale(Vector3.zero, _scaleOutDuration).SetEase(Ease.InQuad));
        }
        seq.OnComplete(() =>
        {
            IsFlying = false;
            if (_trail != null) _trail.SetActive(false);
            onArrive?.Invoke();
            if (this != null && gameObject != null)
                gameObject.SetActive(false);
        });
    }
}
