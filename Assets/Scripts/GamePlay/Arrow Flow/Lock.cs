using UnityEngine;
using DG.Tweening;

public class Lock : MonoBehaviour
{
    [SerializeField] private Animator _animator;

    [SerializeField] private GameObject _keyLock;
    [Header("Unlock VFX")]
    [SerializeField] private ParticleSystem _unlockVfx;

    [SerializeField] private float _delayOff = 0.6f;
    

    public bool IsUnlocking { get; private set; }

    public Transform KeySocket => _keyLock != null ? _keyLock.transform : transform;

    public bool TryBeginUnlock(Key key, System.Action onComplete)
    {
        if (IsUnlocking) return false;
        IsUnlocking = true;

        if (key == null)
        {
            onComplete?.Invoke();
            return true;
        }

        Transform target = KeySocket;
        key.FlyTo(target, () =>
        {
            _keyLock.SetActive(true);
            _unlockVfx.Play();
            float delay = _delayOff;
            if (delay <= 0f)
                onComplete?.Invoke();
            else
                DOVirtual.DelayedCall(delay, () => onComplete?.Invoke());
        });

        return true;
    }

}
