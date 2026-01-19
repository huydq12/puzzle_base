using System;
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
    [SerializeField] private Material _materialType1;
    [SerializeField] private Material _materialType6;
    [SerializeField] private Vector3 _offsetRay;
    [SerializeField] private float _rayDistance;
    [SerializeField] private LayerMask _cubeLayer;
    [SerializeField] private ParticleSystem _hiddenEffect;
    [SerializeField] private Bullet _bulletPrefab;
    private float _bulletSpeed = 50f; // set a sensible default (tune in Inspector)
    [SerializeField] private bool _drawGizmos;
    [SerializeField] private int _bulletPoolSize;
    [ReadOnly] public ObjectColor Color;
    [ReadOnly] public int Type;
    [ReadOnly] public bool CanShoot;
    [ReadOnly] public Gate Gate;
    private RaycastHit _hit;
    private Vector3 _originalScale;
    private CubeLine _lastHit;

    private bool _collectRequested;

    //Fire cooldown (seconds) � controls max fire rate while preserving existing logic
    private float _fireCooldown = 0.025f; // was 0.15f
    private float _nextFireTime = 0f;

    private int _totalValue;

    public int Total
    {
        get => _totalValue;
        set
        {
            _totalValue = value;
            if (_total != null)
                _total.text = _totalValue.ToString();
            if (_totalValue <= 0)
            {
                CanShoot = false;
            }
        }
    }

    public bool ShowTotal
    {
        get => _total.enabled;
        set => _total.enabled = value;
    }

    private void Awake()
    {
        PrewarmBulletPool();
    }

    private void PrewarmBulletPool()
    {
        if (PoolManager.Instance == null) return;
        if (_bulletPrefab == null) return;

        for (int i = 0; i < _bulletPoolSize; i++)
        {
            Bullet bullet = PoolManager.Instance.Get(_bulletPrefab);
            if (bullet == null) continue;
            bullet.transform.SetParent(transform, false);
            bullet.gameObject.SetActive(false);
        }
    }

    private Bullet GetBullet()
    {
        if (_bulletPrefab == null) return null;
        if (PoolManager.Instance == null) return null;
        Bullet bullet = PoolManager.Instance.Get(_bulletPrefab);
        if (bullet != null)
        {
            bullet.transform.SetParent(transform, false);
        }
        return bullet;
    }

    private void ReturnBullet(Bullet bullet)
    {
        if (bullet == null) return;
        bullet.DOKill();
        bullet.gameObject.SetActive(false);
        bullet.transform.SetParent(transform, false);
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

        if (Type == 1 && role == ShooterRole.Current)
            ApplyMaterial(forceColorMaterial: true);
        else
            ApplyMaterial();
    }

    public void SetSize(float size)
    {
        transform.localScale = size * Vector3.one;
        _originalScale = transform.localScale;
    }

    private void Update()
    {
        // Prevent firing while cooldown active or other conditions block shooting
        if (!CanShoot || _collectRequested || Total <= 0 || (Gate != null && Gate.IsClosed) || Time.time < _nextFireTime) return;
        AimCube();
    }

    private void AimCube()
    {
        Vector3 origin = transform.position + _offsetRay;
        Vector3 dir = -transform.right;

        int mask = _cubeLayer != 0 ? _cubeLayer : Physics.DefaultRaycastLayers;

        RaycastHit[] hits = Physics.RaycastAll(origin, dir, _rayDistance, mask);
        if (hits == null || hits.Length == 0)
        {
            _lastHit = null;
            return;
        }

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.transform == null) continue;

            if (hit.transform.TryGetComponent(out CubeLine cube))
            {
                // choose first cube that matches color (or any color if rainbow Type 6) and is not placed on a cell
                bool colorMatches = Type == 6 || cube.Color == Color;
                if (cube != _lastHit && colorMatches && cube.Cell == null)
                {
                    // record the exact hit so the visual bullet travels to the raycast hit point
                    _hit = hit;
                    _lastHit = cube;
                    Shoot(cube);
                    return;
                }

                // if this collider is a CubeLine but doesn't match, stop at first CubeLine to avoid hitting behind it
                break;
            }
        }

        _lastHit = null;
    }

    private void Shoot(CubeLine cube)
    {
        AudioManager.Instance.PlaySFX(SFXType.Shoot);
        // Rate control: set next allowed fire time immediately to enforce cooldown
        _nextFireTime = Time.time + _fireCooldown;

        transform.DOKill();
        transform.localScale = _originalScale;

        transform.DOPunchScale(_originalScale * 0.2f, 0.15f, vibrato: 1, elasticity: 0f)
        .OnComplete(() =>
        {
            transform.localScale = _originalScale;
        });

        // game logic removal happens immediately; bullet is visual
        cube.OnHit(Type == 6);

        // Immediately decrement shooter ammo and handle collect � keeps logic atomic with hit
        Total = Mathf.Max(0, Total - 1);

        if (Total <= 0)
        {
            CanShoot = false;
        }

        if (Total <= 0 && !_collectRequested)
        {
            _collectRequested = true;
            Gate?.CollectCurrentShooter();
        }

        // Visual bullet
        FireBullet();
    }

    private void FireBullet()
    {
        Vector3 origin = transform.position;
        Vector3 direction = -transform.right;

        Bullet bullet = GetBullet();
        if (bullet == null) return;
        bullet.ShowTrail = false;
        bullet.transform.position = origin;
        bullet.ShowTrail = true;
        bullet.transform.rotation = Quaternion.LookRotation(direction);

        float distance = Vector3.Distance(origin, _hit.point);
        float duration = distance / _bulletSpeed;

        bullet.transform.DOMove(origin + direction * distance, duration).SetEase(Ease.Linear)
        .OnComplete(() =>
        {
            ReturnBullet(bullet);
        });
    }

    public void SetColor(ObjectColor color)
    {
        Color = color;
        ApplyMaterial();
    }

    public void SetType(int type)
    {
        Type = type;
        ApplyMaterial();
    }

    public void SetRainbow()
    {
        Type = 6;
        ApplyMaterial();
    }

    private void ApplyMaterial(bool forceColorMaterial = false)
    {
        if (_renderer == null || _renderer.Length == 0) return;

        Material material = null;

        if (Type == 6)
        {
            material = _materialType6;
        }
        else if (!forceColorMaterial && Type == 1)
        {
            material = _materialType1;
        }

        if (material == null)
        {
            material = Board.Instance.ColorConfig.GetShooterColor(Color);
            if (Type == 1)
            {
                Type = 0;
                _hiddenEffect.Play();
            }
        }

        if (material == null) return;

        for (int i = 0; i < _renderer.Length; i++)
        {
            if (_renderer[i] == null) continue;
            _renderer[i].sharedMaterial = material;
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
