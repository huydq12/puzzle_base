using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class Shuffle : MonoBehaviour
{
    [SerializeField] private Vector3 offset;
    [SerializeField] private Animator _animator;

    [SerializeField] private ParticleSystem _effect;

    private Action _onHit;

    public IEnumerator Hit(Transform target, Action onHit = null)
    {
        if (target == null) yield break;

        _onHit = onHit;
        transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        transform.position = target.position + offset;
        _animator.Play("ShooterShuffle_");
        yield return new WaitForSeconds(GetAnimationLength("ShooterShuffle_"));
        Destroy(gameObject);
    }

    public void OnHitEvent()
    {
        _onHit?.Invoke();
        _effect.Play();
    }

    private float GetAnimationLength(string animName)
    {
        AnimationClip[] clips = _animator.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            if (clip.name == animName)
                return clip.length;
        }
        return 0f;
    }
}
