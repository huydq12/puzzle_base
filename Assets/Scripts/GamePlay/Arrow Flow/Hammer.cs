using System;
using System.Collections;
using UnityEngine;

public class Hammer : MonoBehaviour
{
    [SerializeField] private Vector3 offset;
    [SerializeField] private Animator _animator;

    [SerializeField] private ParticleSystem _effect;

    private Action _onHit;

    public IEnumerator Hit(CubeLine cube, Action onHit = null)
    {
        _onHit = onHit;
        transform.position = cube.transform.position + offset;
        _animator.Play("Hammer2_");
        yield return new WaitForSeconds(GetAnimationLength("Hammer2_"));
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
