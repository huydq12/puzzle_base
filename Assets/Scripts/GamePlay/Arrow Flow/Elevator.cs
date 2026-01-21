using DG.Tweening;
using System;
using UnityEngine;

public class Elevator : MonoBehaviour
{
    [SerializeField] private Transform leftGate;
    [SerializeField] private Transform rightGate;
    [SerializeField] private float openDuration = 0.35f;
    [SerializeField] private float gateOpenDistance = 0.4f;
    [SerializeField] private bool scaleGateOpenDistance = true;
    [SerializeField] private float gateOpenDistanceScale = 1f;
    [SerializeField] private float spawnDelayAfterOpen = 0.2f;
    [SerializeField] private float holdOpenDuration = 0.25f;
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

    public void ActivateAndDisappear(Action onOpened = null)
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

        float openDistance = gateOpenDistance;
        if (scaleGateOpenDistance)
            openDistance *= Mathf.Max(0.01f, transform.localScale.x) * Mathf.Max(0.01f, gateOpenDistanceScale);

        if (leftGate != null)
            seq.Join(leftGate.DOLocalMoveX(_leftGateStart.x - openDistance, openDuration).SetEase(Ease.OutQuad));
        if (rightGate != null)
            seq.Join(rightGate.DOLocalMoveX(_rightGateStart.x + openDistance, openDuration).SetEase(Ease.OutQuad));

        if (spawnDelayAfterOpen > 0f)
            seq.AppendInterval(spawnDelayAfterOpen);
        if (onOpened != null)
            seq.AppendCallback(() => onOpened());

        if (holdOpenDuration > 0f)
            seq.AppendInterval(holdOpenDuration);
	        seq.AppendCallback(() =>
	        {
	            Destroy(gameObject);
	        });
	    }
	}
