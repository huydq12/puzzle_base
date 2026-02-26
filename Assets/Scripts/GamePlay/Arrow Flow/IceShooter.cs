using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class IceShooter : MonoBehaviour
{
    [SerializeField] private List<ParticleSystem> _effect;
    [SerializeField] private TextMeshPro _textCount;

    public int Counter { get; private set; }

    private void Awake()
    {
        RefreshVisuals();
    }

    public void SetCounter(int counter)
    {
        Counter = Mathf.Max(0, counter);
        RefreshVisuals();
        if (Counter > 0)
            PlayAllEffects();
    }

    public void Consume(int amount)
    {
        if (Counter <= 0) return;
        int clamped = Mathf.Max(0, amount);
        if (clamped <= 0) return;

        int before = Counter;
        Counter = Mathf.Max(0, Counter - clamped);
        if (Counter != before)
        {
            RefreshVisuals();
            if (Counter > 0)
                PlayAllEffects();
        }
    }

    private void RefreshVisuals()
    {
        bool show = Counter > 0;
        if (gameObject.activeSelf != show)
            gameObject.SetActive(show);

        if (_textCount != null)
        {
            _textCount.gameObject.SetActive(show);
            if (show)
                _textCount.text = Counter.ToString();
        }

        if (!show)
            StopAllEffects();
    }

    private void StopAllEffects()
    {
        if (_effect == null) return;
        for (int i = 0; i < _effect.Count; i++)
        {
            ParticleSystem ps = _effect[i];
            if (ps == null) continue;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void PlayAllEffects()
    {
        if (_effect == null || _effect.Count == 0) return;
        for (int i = 0; i < _effect.Count; i++)
        {
            ParticleSystem ps = _effect[i];
            if (ps == null) continue;
            if (!ps.gameObject.activeSelf)
                ps.gameObject.SetActive(true);
            ps.Play(true);
        }
    }
}
