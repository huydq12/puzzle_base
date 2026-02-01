using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Pool;

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
    [SerializeField] private float _bulletSpeed = 10f; // Tốc độ đạn

    [ReadOnly] public Holder Holder;
    [ReadOnly] public ObjectColor Color;
    [ReadOnly] public bool IsMoving;
    public Material mat => _renderer.sharedMaterials[0];
    // Object Pool cho bullets
    private ObjectPool<Bullet> _bulletPool;

    public bool CanTrigger
    {
        get => _collider.enabled;
        set => _collider.enabled = value;
    }
    public bool OnHolder => Holder != null;
    public GameColorConfig ColorConfig;

    private void Awake()
    {
        // Khởi tạo Object Pool
        _bulletPool = new ObjectPool<Bullet>(
            createFunc: () => CreateBullet(),
            actionOnGet: (bullet) => OnGetBullet(bullet),
            actionOnRelease: (bullet) => OnReleaseBullet(bullet),
            actionOnDestroy: (bullet) => Destroy(bullet.gameObject),
            collectionCheck: false,
            defaultCapacity: 10,
            maxSize: 50
        );
    }

    public void Setup(ObjectColor color, int total)
    {
        Color = color;
        _renderer.materials = new Material[]
        {
            ColorConfig.GetShooterColor(color),
            ColorConfig.GetShooterEye(color)
        };
        _text.text = total.ToString();
    }

    public void Shoot(Cube cube)
    {
        if (cube == null) return;

        Vector3 dir = cube.transform.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return;

        // Quay về phía cube
    Quaternion targetRot = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180f, 0);
        transform.rotation = targetRot;

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
                // Xử lý khi đạn chạm cube (có thể gọi hàm từ cube)
                OnBulletHitCube(bullet, cube);

                // Trả bullet về pool
                _bulletPool.Release(bullet);
            });
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
}