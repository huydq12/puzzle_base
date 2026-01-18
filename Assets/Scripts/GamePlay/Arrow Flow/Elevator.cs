using DG.Tweening;
using UnityEngine;

public class Elevator : MonoBehaviour
{
    [SerializeField] private Transform leftGate;
    [SerializeField] private Transform rightGate;
    [SerializeField] private float openDuration = 0.35f;
    [SerializeField] private float gateOpenDistance = 0.4f;
    [SerializeField] private ParticleSystem vfx;
    [SerializeField] private Transform pointSpawn;

    private Vector3 _leftGateStart;
    private Vector3 _rightGateStart;
    private bool _hasActivated;

    private void Awake()
    {
        if (leftGate != null) _leftGateStart = leftGate.localPosition;
        if (rightGate != null) _rightGateStart = rightGate.localPosition;
    }

    private void OnEnable()
    {
        _hasActivated = false;
        if (leftGate != null) leftGate.localPosition = _leftGateStart;
        if (rightGate != null) rightGate.localPosition = _rightGateStart;
    }

    public void ActivateAndDisappear()
    {
        if (_hasActivated) return;
        _hasActivated = true;

        if (vfx != null)
        {
            if (pointSpawn != null)
                vfx.transform.position = pointSpawn.position;
            vfx.Play(true);
        }

        Sequence seq = DOTween.Sequence();

        if (leftGate != null)
            seq.Join(leftGate.DOLocalMoveX(_leftGateStart.x - gateOpenDistance, openDuration).SetEase(Ease.OutQuad));
        if (rightGate != null)
            seq.Join(rightGate.DOLocalMoveX(_rightGateStart.x + gateOpenDistance, openDuration).SetEase(Ease.OutQuad));

        seq.AppendInterval(0.15f);
        seq.AppendCallback(() =>
        {
            gameObject.SetActive(false);
        });
    }
}
