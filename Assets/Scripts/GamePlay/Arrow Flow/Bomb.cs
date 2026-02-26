using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using TMPro;

public class Bomb : MonoBehaviour
{
    private const string StateIdle = "Empty";
    private const string StateTimer = "Time";
    private const string StateExplode = "Destroy";

    [SerializeField] private ParticleSystem _explodeParticleSystem;
    [SerializeField] private float _explodePunchScale = 0.25f;
    [SerializeField] private float _explodePunchDuration = 0.6f;
    [SerializeField] private int _explodePunchVibrato = 8;
    [SerializeField] private float _explodePunchElasticity = 0.6f;
    [SerializeField] private float _explodeShakeDuration = 0.6f;
    [SerializeField] private float _explodeShakeStrength = 0.05f;

    [SerializeField] private TextMeshPro _counterTimeText;
    [SerializeField] private Vector3 _counterTextOffset = new Vector3(0f, 0.4f, 0f);
    [SerializeField] private GameObject _explodeObject;

    private Tween _explodeTween;

    public void SetVisible(bool visible)
    {
        if (gameObject.activeSelf != visible)
            gameObject.SetActive(visible);

        if (visible)
            PlayIdle();
    }

    public void SetRemainingSeconds(float seconds)
    {
        if (_counterTimeText == null) return;
        int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
        int minutes = total / 60;
        int secs = total % 60;
        _counterTimeText.text = $"{minutes}:{secs:00}";
    }

    public void SetAnchorPosition(Vector3 anchorPosition)
    {
        transform.position = anchorPosition;
        if (_counterTimeText != null)
            _counterTimeText.transform.position = anchorPosition + _counterTextOffset;
    }

    public void PlayIdle()
    {
       
    }

    public void PlayTimer()
    {
       
    }

    public void PlayExplosion()
    {
        if (_explodeTween != null && _explodeTween.IsActive())
            _explodeTween.Kill();

        transform.DOKill();

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOPunchScale(Vector3.one * _explodePunchScale, _explodePunchDuration, _explodePunchVibrato, _explodePunchElasticity));
        if (_explodeShakeDuration > 0f && _explodeShakeStrength > 0f)
            seq.Join(transform.DOShakePosition(_explodeShakeDuration, _explodeShakeStrength, 10, 90f, false, true)).OnComplete(() => { _explodeObject.SetActive(false); _explodeParticleSystem.Play(); });
        _explodeTween = seq;
    }
}
