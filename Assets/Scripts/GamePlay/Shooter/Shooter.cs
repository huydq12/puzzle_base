using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Pool;
using System;
using System.Linq;

public enum ShooterState
{
    None,
    Hide,
    Show,
}

public class Shooter : MonoBehaviour
{
    private Tween _recoilTween;
    [ReadOnly] public Vector2Int GridPosition;
    [SerializeField] private Renderer _renderer;
    [SerializeField] private Transform _pointShot;
    [SerializeField] private Bullet _bullet;
    [SerializeField] private TextMeshPro _text;
    [SerializeField] private Animation _animation;
    [SerializeField] private Outline _outline;
    [SerializeField] private Collider _collider;
    [SerializeField] private int _remaining;
    [SerializeField] private float _bulletSpeed;
    public ShooterState State;
    private Quaternion _lookRotation;

    [ReadOnly] public Holder Holder;
    [ReadOnly] public ObjectColor Color;
    [ReadOnly] public bool IsMoving;

    public int Remaining
    {
        get => _remaining;
        set
        {
            _remaining = Math.Max(0, value);
            UpdateRemaining();
        }
    }
    private void PlayShootRecoil()
    {
        _recoilTween?.Kill();

        Quaternion startRot = _lookRotation;

        Vector3 recoilAxis = _lookRotation * Vector3.right;

        Quaternion kickRot =
            Quaternion.AngleAxis(10, recoilAxis) * startRot;

        Sequence sq = DOTween.Sequence();

        sq.Append(
            _renderer.transform.DORotateQuaternion(kickRot, 0.1f)
        ).SetEase(Ease.OutQuad);

        sq.Append(
             _renderer.transform.DORotateQuaternion(startRot, 0.15f)
        ).SetEase(Ease.OutBack);

        _recoilTween = sq;
    }
    private Sequence _resetLooksq;
    private Sequence _shootSeq;
    private ObjectPool<Bullet> _bulletPool;
    public bool IsShooting => _shootSeq != null && _shootSeq.IsActive() && _shootSeq.IsPlaying();

    public bool ShowRemaining
    {
        get => _text.enabled;
        set => _text.enabled = value;
    }

    public bool CanTrigger
    {
        get => _collider.enabled;
        set => _collider.enabled = value;
    }

    public bool OnHolder => Holder != null;

    private void UpdateRemaining()
    {
        _text.text = _remaining.ToString();
    }

    private void Awake()
    {
        _bulletPool = new ObjectPool<Bullet>(
            createFunc: () => CreateBullet(),
            actionOnGet: (bullet) => OnGetBullet(bullet),
            actionOnRelease: (bullet) => OnReleaseBullet(bullet),
            actionOnDestroy: (bullet) => Destroy(bullet.gameObject),
            collectionCheck: false,
            defaultCapacity: 10,
            maxSize: 50
        );
        State = ShooterState.None;
        ShowRemaining = false;
        UpdateRemaining();
        CanTrigger = true;
    }

    private void Destroy()
    {
        ShowRemaining = false;
        if (Holder != null) Holder.AssignShooter(null);
        _resetLooksq?.Kill();
        _shootSeq?.Kill();
        CanTrigger = false;
        float jumpHeight = 0.25f;
        float jumpUpDuration = 0.2f;
        float scaleDuration = 0.2f;

        Vector3 startPos = transform.position;
        Vector3 topPos = startPos + Vector3.up * jumpHeight;

        Sequence seq = DOTween.Sequence();

        seq.Append(
            transform.DOMove(topPos, jumpUpDuration)
                .SetEase(Ease.OutQuad)
        );

        seq.Append(
            transform.DOScale(Vector3.zero, scaleDuration)
                .SetEase(Ease.InBack)
        );

        seq.Join(
          transform.DORotate(
              new Vector3(0, 360, 0),
              scaleDuration,
              RotateMode.LocalAxisAdd
          ).SetEase(Ease.Linear)
      );

        seq.OnComplete(() =>
        {
            Destroy(gameObject);
        });
    }

    public void Shoot(Base targetBase)
    {
        if (IsShooting || !targetBase.CanTrigger) return;
        if (targetBase == null || targetBase.IsEmpty()) return;

        _resetLooksq?.Kill();
        _animation.Stop("Idle_Deck");


        var cubesToShoot = targetBase.GetAllCubes();
        if (cubesToShoot.Count == 0) return;
        targetBase.CanTrigger = false;
        _shootSeq = DOTween.Sequence();
        cubesToShoot.Reverse();
        foreach (var cube in cubesToShoot)
        {
            if (cube == null) continue;

            Vector3 targetPos = cube.transform.position;
            float waitTime = Vector3.Distance(_pointShot.position, targetPos) / _bulletSpeed;

            _shootSeq.AppendCallback(() =>
            {
                if (this == null || _pointShot == null) return;

                Vector3 dir = targetPos - transform.position;
                dir.y = 0f;

                if (dir.sqrMagnitude > 0.0001f)
                {
                    _lookRotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180f, 0);
                    transform.rotation = _lookRotation;
                }

                PlayShootRecoil();

                Bullet bullet = _bulletPool.Get();

                float distance = Vector3.Distance(_pointShot.position, targetPos);
                float duration = distance / _bulletSpeed;
                Remaining--;
                bullet.transform.SetParent(cube.transform.parent);
                bullet.transform.DOLocalMove(Vector3.zero + Vector3.up * 0.03f, duration).SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        OnBulletHitCube(cube, targetBase);
                        _bulletPool.Release(bullet);
                    });
            });

            _shootSeq.AppendInterval(0.025f);
        }

        _shootSeq.OnComplete(() =>
        {
            if (Remaining > 0)
            {
                _resetLooksq = DOTween.Sequence();
                _resetLooksq.AppendInterval(1.5f);
                _resetLooksq.Append(transform.DORotateQuaternion(Quaternion.Euler(0, 180, 0), 0.25f));
                _resetLooksq.OnComplete(() =>
                {
                    Idle();
                    _resetLooksq = null;
                });
            }
            else
            {
                Destroy();
            }
        });
    }

    private Bullet CreateBullet()
    {
        Bullet newBullet = Instantiate(_bullet);
        return newBullet;
    }

    private void OnGetBullet(Bullet bullet)
    {
        bullet.transform.position = _pointShot.transform.position;
        bullet.gameObject.SetActive(true);
    }

    private void OnReleaseBullet(Bullet bullet)
    {
        bullet.transform.DOKill();
        bullet.gameObject.SetActive(false);
    }

    private void OnBulletHitCube(Cube cube, Base targetBase)
    {
        targetBase.RemoveCube(cube);
        cube.Destroy();
    }

    private void OnDestroy()
    {
        _shootSeq?.Kill();
        _bulletPool?.Clear();
    }
    public void Idle()
    {
        _animation["Idle_Deck"].speed = 0.35f;
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
        _animation.Play("TouchLock", PlayMode.StopAll);
    }

    public void Hide()
    {
        if (State == ShooterState.Hide) return;
        State = ShooterState.Hide;
        _animation.Play("Hide", PlayMode.StopSameLayer);
    }
}
