using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CubeEffect : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private float _travelDuration = 0.35f;
    [SerializeField] private Ease _travelEase = Ease.InQuad;
    [SerializeField] private float _scaleOutDuration = 0.15f;

    public float TravelDuration
    {
        get => _travelDuration;
        set => _travelDuration = Mathf.Max(0.01f, value);
    }

    public float ScaleOutDuration
    {
        get => _scaleOutDuration;
        set => _scaleOutDuration = Mathf.Max(0f, value);
    }

    public Ease TravelEase
    {
        get => _travelEase;
        set => _travelEase = value;
    }

    public void Play(Vector3 from, Vector3 to, ObjectColor color)
    {
        transform.DOKill();
        transform.position = from;

        if (meshRenderer != null && Board.Instance != null)
        {
            Material mat = Board.Instance.ColorConfig.GetCubeColor(color);
            if (mat != null)
                meshRenderer.sharedMaterial = mat;
        }

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOMove(to, _travelDuration).SetEase(_travelEase));
        if (_scaleOutDuration > 0f)
        {
            float scaleStart = Mathf.Max(0f, _travelDuration - _scaleOutDuration);
            seq.Insert(scaleStart, transform.DOScale(Vector3.zero, _scaleOutDuration).SetEase(Ease.InQuad));
        }
        seq.OnComplete(() =>
        {
            if (this != null && gameObject != null)
                Destroy(gameObject);
        });
    }
}
