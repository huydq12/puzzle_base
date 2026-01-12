using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public enum ShooterRole
{
    Current,
    Next,
    Queue
}

public class Shooter : MonoBehaviour
{
    [SerializeField] private Renderer[] _renderer;
    [SerializeField] private TextMeshPro _total;
    [SerializeField] private Vector3 _offsetRay;
    [SerializeField] private float _rayDistance;
    [SerializeField] private LayerMask _cubeLayer;
    [SerializeField] private Transform _bulletPrefab;
    [SerializeField] private float _bulletSpeed;
    [SerializeField] private bool _drawGizmos;
    [SerializeField] private int _bulletPoolSize;
    [ReadOnly] public ObjectColor Color;
    [ReadOnly] public bool CanShoot;
    [ReadOnly] public Gate Gate;
    private RaycastHit _hit;
    private Vector3 _originalScale;
    private CubeLine _lastHit;

    private bool _collectRequested;

    private Queue<Transform> _bulletPool;

    public int Total
    {
        get => int.Parse(_total.text);
        set => _total.text = value.ToString();
    }

    public bool ShowTotal
    {
        get => _total.enabled;
        set => _total.enabled = value;
    }

    private void Awake()
    {
        InitializeBulletPool();
    }

    private void InitializeBulletPool()
    {
        _bulletPool = new Queue<Transform>();

        for (int i = 0; i < _bulletPoolSize; i++)
        {
            Transform bullet = Instantiate(_bulletPrefab, transform);
            bullet.gameObject.SetActive(false);
            _bulletPool.Enqueue(bullet);
        }
    }

    private Transform GetBullet()
    {
        if (_bulletPool.Count > 0)
        {
            Transform bullet = _bulletPool.Dequeue();
            bullet.gameObject.SetActive(true);
            return bullet;
        }

        Transform newBullet = Instantiate(_bulletPrefab, transform);
        return newBullet;
    }

    private void ReturnBullet(Transform bullet)
    {
        bullet.DOKill();
        bullet.gameObject.SetActive(false);
        bullet.SetParent(transform);
        _bulletPool.Enqueue(bullet);
    }

    public void SetRole(ShooterRole role)
    {
        switch (role)
        {
            case ShooterRole.Current:
                CanShoot = true;
                ShowTotal = true;
                _collectRequested = false;
                SetSize(0.75f);
                break;

            case ShooterRole.Next:
            case ShooterRole.Queue:
                CanShoot = false;
                ShowTotal = false;
                SetSize(0.65f);
                break;
        }
    }

    public void SetSize(float size)
    {
        transform.localScale = size * Vector3.one;
        _originalScale = transform.localScale;
    }

    private void Update()
    {
        if (!CanShoot) return;
        AimCube();
    }

    private void AimCube()
    {
        if (Physics.Raycast(transform.position + _offsetRay, -transform.right, out _hit, _rayDistance, _cubeLayer))
        {
            if (_hit.transform.TryGetComponent(out CubeLine cube) && cube != _lastHit && cube.Color == Color)
            {
                _lastHit = cube;
                Shoot(cube);
            }
        }
        else
        {
            _lastHit = null;
        }
    }

    private void Shoot(CubeLine cube)
    {
        transform.DOKill();
        transform.localScale = _originalScale;

        transform.DOPunchScale(_originalScale * 0.2f, 0.15f, vibrato: 1, elasticity: 0f)
        .OnComplete(() =>
        {
            transform.localScale = _originalScale;
        });

        cube.OnHit();

        FireBullet();
    }

    private void FireBullet()
    {
        Vector3 origin = transform.position;
        Vector3 direction = -transform.right;

        Transform bullet = GetBullet();
        bullet.position = origin;
        bullet.rotation = Quaternion.LookRotation(direction);

        float distance = Vector3.Distance(origin, _hit.point);
        float duration = distance / _bulletSpeed;

        bullet.DOMove(origin + direction * distance, duration).SetEase(Ease.Linear)
        .OnComplete(() =>
        {
            ReturnBullet(bullet);
            Total = Mathf.Max(0, Total - 1);
            if (Total <= 0 && !_collectRequested)
            {
                _collectRequested = true;
                Gate.CollectCurrentShooter();
            }
        });
    }

    public void SetColor(ObjectColor color)
    {
        Color = color;
        foreach (var renderer in _renderer)
        {
            renderer.sharedMaterial = Board.Instance.ColorConfig.GetShooterByColor(color);
        }
    }

    private void OnDrawGizmos()
    {
        if (!_drawGizmos) return;

        Vector3 origin = transform.position + _offsetRay;
        Vector3 direction = -transform.right;

        Gizmos.color = UnityEngine.Color.green;
        Gizmos.DrawLine(origin, origin + direction * _rayDistance);

        if (Application.isPlaying && _hit.collider != null)
        {
            Gizmos.color = UnityEngine.Color.red;
            Gizmos.DrawSphere(_hit.point, 0.08f);
        }
    }
}