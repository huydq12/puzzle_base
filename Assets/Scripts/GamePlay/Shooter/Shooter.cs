using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Pool;
using System;

public enum ShooterState
{
    None,
    Hide,
    Show,
}

public class Shooter : MonoBehaviour
{
    [SerializeField, DisableInPlayMode, MinValue(1), DisableIn(PrefabKind.PrefabAsset)] private int _remaining;
    [ReadOnly] public Vector2Int GridPosition;
    [ReadOnly] public Holder Holder;
    [ReadOnly] public ObjectColor Color;
    [ReadOnly] public bool IsMoving;
    [ReadOnly] public ShooterState State;
    [SerializeField, DisableIn(PrefabKind.PrefabInstance)] private Renderer _renderer;
    [SerializeField, DisableIn(PrefabKind.PrefabInstance)] private Transform _pointShot;
    [SerializeField, DisableIn(PrefabKind.PrefabInstance)] private Bullet _bullet;
    [SerializeField, DisableIn(PrefabKind.PrefabInstance)] private TextMeshPro _text;
    [SerializeField, DisableIn(PrefabKind.PrefabInstance)] private Animation _animation;
    [SerializeField, DisableIn(PrefabKind.PrefabInstance)] private Outline _outline;
    [SerializeField, DisableIn(PrefabKind.PrefabInstance)] private Collider _collider;
    [SerializeField, DisableIn(PrefabKind.PrefabInstance)] private ParticleSystem _muzzleEffect;
    [SerializeField, DisableIn(PrefabKind.PrefabInstance)] private ParticleSystem _collectEffect;

    private Tween _recoilTween;
    private Sequence _resetLooksq;
    private Sequence _shootSeq;
    private ObjectPool<Bullet> _bulletPool;
    private Quaternion _lookRotation;
    private Transform _rendererTransform;
    private AnimationState _idleState;

    public int Remaining
    {
        get => _remaining;
        set
        {
            _remaining = Math.Max(0, value);
            UpdateRemainingText();
        }
    }
    private void UpdateRemainingText()
    {
        _text.text = _remaining.ToString();
#if UNITY_EDITOR
        _text.enabled = true;
#endif
    }
    void OnValidate()
    {
        UpdateRemainingText();
    }

    public bool IsShooting => _shootSeq != null && _shootSeq.IsActive();
    public bool ShowRemaining { get => _text.enabled; set => _text.enabled = value; }
    public bool CanTrigger { get => _collider.enabled; set => _collider.enabled = value; }
    public bool OnHolder => Holder != null;

    private void Awake()
    {
        _rendererTransform = _renderer.transform;
        _idleState = _animation["Idle_Deck"];

        _bulletPool = new ObjectPool<Bullet>(
            createFunc: CreateBullet,
            actionOnGet: OnGetBullet,
            actionOnRelease: OnReleaseBullet,
            actionOnDestroy: bullet => Destroy(bullet.gameObject),
            collectionCheck: false,
            defaultCapacity: 10,
            maxSize: 50
        );

        State = ShooterState.None;
        ShowRemaining = false;
        _text.text = _remaining.ToString();
        CanTrigger = true;
    }

    public void OnShooterOnHolder()
    {
        Idle();
        _outline.OutlineColor = Board.Instance.ColorConfig.GetShooterOutlineColor(Color);
    }

    public void Shoot(Base targetBase)
    {
        if (IsShooting || targetBase == null || targetBase.IsKill || targetBase.IsEmpty())
            return;

        var cubesToShoot = targetBase.GetAllCubes(true);
        if (cubesToShoot.Count == 0) return;
        AudioManager.Instance.PlaySFX(SFXType.Shoot);
        targetBase.IsKill = true;
        KillTweens();
        _animation.Stop("Idle_Deck");

        if (!_muzzleEffect.isPlaying)
            _muzzleEffect.Play();

        _shootSeq = DOTween.Sequence();

        foreach (var cube in cubesToShoot)
        {
            if (cube == null) continue;

            Vector3 targetPos = cube.transform.position;
            Transform cubeParent = cube.transform.parent;

            _shootSeq.AppendCallback(() => ShootBulletAtCube(cube, targetPos, cubeParent, targetBase));
            _shootSeq.AppendInterval(0.025f);
        }

        _shootSeq.OnComplete(OnShootComplete);
    }

    private void ShootBulletAtCube(Cube cube, Vector3 targetPos, Transform cubeParent, Base targetBase)
    {
        if (this == null || _pointShot == null) return;

        RotateTowardsTarget(targetPos);
        PlayShootRecoil();

        Bullet bullet = _bulletPool.Get();
        Remaining--;

        float distance = Vector3.Distance(_pointShot.position, targetPos);
        float duration = distance / Defines.BULLET_SPEED;

        bullet.transform.SetParent(cubeParent);
        bullet.transform.DOLocalMove(Vector3.up * 0.1f, duration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                targetBase.RemoveCube(cube);
                cube.Destroy();
                _bulletPool.Release(bullet);
            });
    }

    private void RotateTowardsTarget(Vector3 targetPos)
    {
        Vector3 dir = targetPos - _rendererTransform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.0001f)
        {
            _lookRotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180f, 0);
            _rendererTransform.rotation = _lookRotation;
        }
    }

    private void PlayShootRecoil()
    {
        _recoilTween?.Kill();

        Quaternion kickRot = Quaternion.AngleAxis(10, _lookRotation * Vector3.right) * _lookRotation;

        _recoilTween = DOTween.Sequence()
            .Append(_rendererTransform.DORotateQuaternion(kickRot, 0.1f).SetEase(Ease.OutQuad))
            .Append(_rendererTransform.DORotateQuaternion(_lookRotation, 0.15f).SetEase(Ease.OutBack));
    }

    private void OnShootComplete()
    {
        _muzzleEffect.Stop();

        if (Remaining > 0)
        {
            _resetLooksq = DOTween.Sequence()
                .AppendInterval(1.5f)
                .Append(_rendererTransform.DORotateQuaternion(Quaternion.Euler(0, 180, 0), 0.25f))
                .OnComplete(() =>
                {
                    Idle();
                    _resetLooksq = null;
                });
        }
        else
        {
            DestroyShooter();
        }
    }

    private void DestroyShooter()
    {
        ShowRemaining = false;
        KillTweens();
        CanTrigger = false;

        Vector3 topPos = _rendererTransform.position + Vector3.up * 0.25f;

        DOTween.Sequence()
            .Append(_rendererTransform.DOMove(topPos, 0.2f).SetEase(Ease.OutQuad))
            .Append(_rendererTransform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack))
            .Join(_rendererTransform.DORotate(new Vector3(0, 360, 0), 0.2f, RotateMode.LocalAxisAdd).SetEase(Ease.Linear))
            .OnComplete(() =>
            {
                Destroy(gameObject);
                Holder?.Clear();
            });

        _collectEffect.Play();
    }

    private void KillTweens()
    {
        _resetLooksq?.Kill();
        _shootSeq?.Kill();
    }

    private Bullet CreateBullet() => Instantiate(_bullet);

    private void OnGetBullet(Bullet bullet)
    {
        bullet.transform.position = _pointShot.position;
        bullet.gameObject.SetActive(true);
    }

    private void OnReleaseBullet(Bullet bullet)
    {
        bullet.transform.DOKill();
        bullet.gameObject.SetActive(false);
    }

    public void Idle()
    {
        _idleState.speed = 0.25f;
        _animation.Play("Idle_Deck", PlayMode.StopAll);
    }

    public void Show()
    {
        if (State == ShooterState.Show) return;
        State = ShooterState.Show;
        _animation.Play("ShooterAppear", PlayMode.StopAll);
    }

    public void Shake()
    {
        AudioManager.Instance.PlaySFX(SFXType.SelectWrong);
        _animation.Play("TouchLock", PlayMode.StopAll);
    }

    public void Hide()
    {
        if (State == ShooterState.Hide) return;
        State = ShooterState.Hide;
        _animation.Play("Hide", PlayMode.StopSameLayer);
    }

    private void OnDestroy()
    {
        _recoilTween?.Kill();
        _resetLooksq?.Kill();
        _shootSeq?.Kill();
        _bulletPool?.Clear();
    }
    [Button, DisableInPlayMode, DisableIn(PrefabKind.PrefabAsset)]
    public void SetColor(ObjectColor color)
    {
        Color = color;
        var config = Resources.Load<GameColorConfig>("Color game Config");
        _renderer.sharedMaterials = new Material[] { config.GetShooterColor(color), config.GetShooterEye(color) };
    }
}