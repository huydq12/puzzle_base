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
            if (value == 0)
            {
                ShowRemaining = false;
                Destroy();
            }
        }
    }

    private Sequence _resetLooksq;
    private ObjectPool<Bullet> _bulletPool;

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
        if (Holder != null) Holder.AssignShooter(null);
        _resetLooksq?.Kill();
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
        if (targetBase == null || targetBase.Cubes.Count == 0) return;

        _resetLooksq?.Kill();

        // Lấy 2 cube random từ base (hoặc ít hơn nếu base không đủ cube)
        int shotCount = Mathf.Min(2, targetBase.Cubes.Count);
        var cubesToShoot = targetBase.Cubes.OrderBy(x => UnityEngine.Random.value).Take(shotCount).ToList();

        // Quay về phía base (lấy vị trí trung tâm của base hoặc cube đầu tiên)
        Vector3 baseCenter = targetBase.transform.position;
        Vector3 dir = baseCenter - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180f, 0);
            transform.rotation = targetRot;
        }

        // Đếm số viên đạn đã hoàn thành
        int completedBullets = 0;

        // Bắn đồng thời vào tất cả các cube
        foreach (var cube in cubesToShoot)
        {
            if (cube == null) continue;

            // Lấy bullet từ pool
            Bullet bullet = _bulletPool.Get();

            // Tính khoảng cách và thời gian di chuyển
            float distance = Vector3.Distance(_pointShot.position, cube.transform.position);
            float duration = distance / _bulletSpeed;

            // Di chuyển bullet đến cube bằng DOTween
            bullet.transform.DOMove(cube.transform.position, duration)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    // Xử lý khi đạn chạm cube
                    OnBulletHitCube(bullet, cube);
                    _bulletPool.Release(bullet);

                    completedBullets++;

                    // Chỉ giảm remaining và reset rotation khi TẤT CẢ viên đạn đã bay xong
                    if (completedBullets >= shotCount)
                    {
                        Remaining--;

                        if (Remaining > 0)
                        {
                            _resetLooksq = DOTween.Sequence();
                            _resetLooksq.AppendInterval(1.5f);
                            _resetLooksq.Append(transform.DORotate(new Vector3(0, 180, 0), 0.25f));
                            _resetLooksq.OnComplete(() => _resetLooksq = null);
                        }
                    }
                });
        }
    }

    // Tạo bullet mới
    private Bullet CreateBullet()
    {
        Bullet newBullet = Instantiate(_bullet);
        return newBullet;
    }

    // Khi lấy bullet từ pool
    private void OnGetBullet(Bullet bullet)
    {
        bullet.transform.SetParent(_pointShot);
        bullet.transform.localPosition = Vector3.zero;
        bullet.transform.localRotation = Quaternion.identity;
        bullet.gameObject.SetActive(true);
    }

    // Khi trả bullet về pool
    private void OnReleaseBullet(Bullet bullet)
    {
        bullet.transform.DOKill(); // Dừng mọi tween đang chạy
        bullet.gameObject.SetActive(false);
        bullet.transform.SetParent(transform); // Hoặc có thể để null
    }

    // Xử lý khi đạn chạm cube
    private void OnBulletHitCube(Bullet bullet, Cube cube)
    {
        // Thêm logic xử lý khi đạn chạm cube
        // Ví dụ: cube.TakeDamage(), PlayHitEffect(), etc.
    }

    private void OnDestroy()
    {
        // Clear pool khi destroy object
        _bulletPool?.Clear();
    }

    public void Show()
    {
        if (State == ShooterState.Show) return;
        State = ShooterState.Show;
        _animation.Play("Show", PlayMode.StopAll);

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