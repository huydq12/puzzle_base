using DG.Tweening;
using System;
using UnityEngine;

public class Elevator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer leftGate;
    [SerializeField] private SpriteRenderer rightGate;
    [SerializeField] private Vector2 leftGateSize;
    [SerializeField] private Vector2 rightGateSize;
    [SerializeField] private Transform border;
    [SerializeField] private float openDuration = 0.35f;
    [SerializeField] private float spawnDelayAfterOpen = 0.2f;
    [SerializeField] private float holdOpenDuration = 0.25f;
    [SerializeField] private ParticleSystem vfx;
    [SerializeField] private Transform pointSpawn;

    private Vector3 _leftGateStart;
    private Vector3 _rightGateStart;
    private Vector2 _leftGateSizeStart;
    private Vector2 _rightGateSizeStart;
    private bool _hasActivated;

    private void Awake()
    {
        if (leftGate != null) _leftGateStart = leftGate.transform.localPosition;
        if (rightGate != null) _rightGateStart = rightGate.transform.localPosition;
        ApplyGateVisuals();
        CacheGateSizes();
    }

    private void OnEnable()
    {
        _hasActivated = false;
        border.gameObject.SetActive(true);
        if (leftGate != null) leftGate.transform.localPosition = _leftGateStart;
        if (rightGate != null) rightGate.transform.localPosition = _rightGateStart;
        if (leftGate != null) leftGate.size = _leftGateSizeStart;
        if (rightGate != null) rightGate.size = _rightGateSizeStart;
    }

    private void ApplyGateVisuals()
    {
        if (leftGate != null)
        {
            leftGate.drawMode = SpriteDrawMode.Tiled;
            if (leftGateSize == Vector2.zero)
                leftGateSize = leftGate.size;
            leftGate.size = leftGateSize;
        }

        if (rightGate != null)
        {
            rightGate.drawMode = SpriteDrawMode.Tiled;
            if (rightGateSize == Vector2.zero)
                rightGateSize = rightGate.size;
            rightGate.size = rightGateSize;
        }
    }

    private void CacheGateSizes()
    {
        if (leftGate != null) _leftGateSizeStart = leftGate.size;
        if (rightGate != null) _rightGateSizeStart = rightGate.size;
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

        border.gameObject.SetActive(false);

        if (leftGate != null)
        {
            Vector2 leftOpenSize = new Vector2(0f, _leftGateSizeStart.y);
            seq.Join(DOTween.To(() => leftGate.size, value => leftGate.size = value, leftOpenSize, openDuration)
                .SetEase(Ease.OutQuad));
        }
        if (rightGate != null)
        {
            Vector2 rightOpenSize = new Vector2(0f, _rightGateSizeStart.y);
            seq.Join(DOTween.To(() => rightGate.size, value => rightGate.size = value, rightOpenSize, openDuration)
                .SetEase(Ease.OutQuad));
        }

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
