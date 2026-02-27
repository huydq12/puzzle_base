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
    [SerializeField] private Renderer _renderer;
    [SerializeField] private TextMeshPro _total;
    [SerializeField] private Material _materialType1;
    [SerializeField] private Material _materialType6;
    [SerializeField] private Vector3 _offsetRay;
    [SerializeField] private float _rayDistance;
    [SerializeField] private LayerMask _cubeLayer;
    [SerializeField] private ParticleSystem _hiddenEffect;
    [SerializeField] private Bullet _bulletPrefab;
    [SerializeField] private Transform _bulletSpawnPoint;
    [SerializeField] private Animation _animation;
    [SerializeField] private Outline _outline;
    private float _bulletSpeed = 50f; // set a sensible default (tune in Inspector)
    [SerializeField] private bool _drawGizmos;
    [SerializeField] private int _bulletPoolSize;
    [ReadOnly] public ObjectColor Color;
    [ReadOnly] public int Type;
    [ReadOnly] public bool CanShoot;
    [ReadOnly] public IGate Gate;
    private RaycastHit _hit;
    private Vector3 _originalScale;
    private CubeLine _lastHit;

    private bool _collectRequested;

    //Fire cooldown (seconds) � controls max fire rate while preserving existing logic
    private float _fireCooldown = 0.025f; // was 0.15f
    private float _nextFireTime = 0f;

    private int _totalValue;

    private const float IdleFallbackDelaySeconds = 3f;
    private float _lastActivityTime;

    private ShooterRole _role;

    private Tween _idleTween;
    private Vector3 _idleBaseEuler;
    private Vector3 _idleBaseScale;
    private bool _hasIdleBase;

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
    }

    private void OnDisable()
    {
        ResetForReuse();
    }

    public void ResetForReuse()
    {
        StopIdleTween();
        transform.DOKill();
        _hit = default;
        _lastHit = null;
        _collectRequested = false;
        _nextFireTime = 0f;
        _lastActivityTime = Time.time;
        CanShoot = false;
        Gate = null;

        Total = 0;

        if (_hiddenEffect != null)
            _hiddenEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void PrewarmBulletPool()
    {
        // Pooling disabled: keep method for backward compatibility.
    }

    private Bullet GetBullet()
    {
        if (_bulletPrefab == null) return null;
        Bullet bullet = Instantiate(_bulletPrefab);
        if (bullet != null)
        {
            Transform parent = _bulletSpawnPoint != null ? _bulletSpawnPoint : transform;
            bullet.transform.SetParent(parent, false);
        }
        return bullet;
    }

    private void ReturnBullet(Bullet bullet)
    {
        if (bullet == null) return;
        bullet.DOKill();
        Destroy(bullet.gameObject);
    }

    public void SetRole(ShooterRole role)
    {
        _role = role;
        StopIdleTween();
        _animation.Play(role == ShooterRole.Current ? "Show" : "Hide", PlayMode.StopAll);
        _lastActivityTime = Time.time;
        _outline.enabled = role == ShooterRole.Current;
        if (role == ShooterRole.Current)
        {
            StartCoroutine(Common.DelayActionToNextFrame(() =>
            {
                _outline.OutlineColor = Board.Instance.ColorConfig.GetOutlineShooter(Color);
                _outline.RenderOutline();
            }
            ));
        }
        switch (role)
        {
            case ShooterRole.Current:
                StartCoroutine(Common.DelayAction(0.2f, () =>
                {
                    CanShoot = true;
                }));
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
        if (_role == ShooterRole.Current && gameObject.activeInHierarchy && Time.time - _lastActivityTime >= IdleFallbackDelaySeconds)
        {
            StartIdleTweenIfNeeded();
        }

        // Prevent firing while cooldown active or other conditions block shooting
        if (!CanShoot || _collectRequested || Total <= 0 || (Gate != null && Gate.IsClosed) || (Gate != null && Gate.IsShooterFrozen) || Time.time < _nextFireTime) return;
        AimCube();
    }

    private void StopIdleTween()
    {
        if (_idleTween != null && _idleTween.IsActive())
        {
            _idleTween.Kill();
        }
        _idleTween = null;

        if (_hasIdleBase)
        {
            transform.localEulerAngles = _idleBaseEuler;
            transform.localScale = _idleBaseScale;
            _hasIdleBase = false;
        }
    }

    private void StartIdleTweenIfNeeded()
    {
        if (_idleTween != null && _idleTween.IsActive() && _idleTween.IsPlaying()) return;

        StopIdleTween();

        _idleBaseEuler = transform.localEulerAngles;
        _idleBaseScale = transform.localScale;
        _hasIdleBase = true;
        float baseX = _idleBaseEuler.x;
        float baseY = _idleBaseEuler.y;
        float baseZ = _idleBaseEuler.z;

        Sequence seq = DOTween.Sequence();
        seq.SetUpdate(false);

        seq.Append(DOTween.To(() => 0f, z => SetIdleLocalEuler(baseX, baseY, baseZ, z), -1.933f, 0.2666669f).SetEase(Ease.Linear));
        seq.Append(DOTween.To(() => -1.933f, z => SetIdleLocalEuler(baseX, baseY, baseZ, z), 3.642f, 0.1666665f).SetEase(Ease.Linear));
        seq.Append(DOTween.To(() => 3.642f, z => SetIdleLocalEuler(baseX, baseY, baseZ, z), -3.997f, 0.23333335f).SetEase(Ease.Linear));
        seq.Append(DOTween.To(() => -3.997f, z => SetIdleLocalEuler(baseX, baseY, baseZ, z), 3.987f, 0.26666665f).SetEase(Ease.Linear));
        seq.Append(DOTween.To(() => 3.987f, z => SetIdleLocalEuler(baseX, baseY, baseZ, z), 0f, 0.4000001f).SetEase(Ease.Linear));

        float baseSX = _idleBaseScale.x;
        float baseSY = _idleBaseScale.y;
        float baseSZ = _idleBaseScale.z;

        seq.Insert(0f, DOTween.To(() => 0f, t => SetIdleLocalScale(baseSX, baseSY, baseSZ, t), 1f, 0.7333336f).SetEase(Ease.Linear));
        seq.Insert(0.7333336f, DOTween.To(() => 1f, t => SetIdleLocalScale(baseSX, baseSY, baseSZ, t), 2f, 0.3999998f).SetEase(Ease.Linear));
        seq.Insert(1.1333334f, DOTween.To(() => 2f, t => SetIdleLocalScale(baseSX, baseSY, baseSZ, t), 3f, 0.2000001f).SetEase(Ease.Linear));

        seq.SetLoops(-1, LoopType.Restart);

        _idleTween = seq;
    }

    private void SetIdleLocalEuler(float x, float y, float baseZ, float zOffset)
    {
        transform.localEulerAngles = new Vector3(x, y, baseZ + zOffset);
    }

    private void SetIdleLocalScale(float baseX, float baseY, float baseZ, float phase)
    {
        Vector3 factor;
        if (phase < 1.5f)
            factor = new Vector3(1.0289985f, 0.95f, 1.0289985f);
        else if (phase < 2.5f)
            factor = new Vector3(1.0077523f, 0.9749892f, 1.0077523f);
        else
            factor = Vector3.one;

        transform.localScale = new Vector3(baseX * factor.x, baseY * factor.y, baseZ * factor.z);
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
        _lastActivityTime = Time.time;
        StopIdleTween();

        transform.DOKill();
        transform.localScale = _originalScale;

        transform.DOPunchScale(_originalScale * 0.2f, 0.15f, vibrato: 1, elasticity: 0f)
        .OnComplete(() =>
        {
            transform.localScale = _originalScale;
        });

        // game logic removal happens immediately; bullet is visual
        ObjectColor hitColor = cube.Color;
        bool destroyed = cube.OnHit(Type == 6);
        if (destroyed)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Shooter] LineDoor hit color={hitColor} shooter={name}", this);
#endif
            Board.Instance?.NotifyLineDoorHit(hitColor, this);
        }

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
        Vector3 origin = _bulletSpawnPoint != null ? _bulletSpawnPoint.position : transform.position;
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
        if (_renderer == null) return;

        Material material = null;
        Material eyeMaterial = null;
        if (Type == 6)
        {
            material = _materialType6;
            eyeMaterial = _materialType6;
        }
        else if (!forceColorMaterial && Type == 1)
        {
            material = _materialType1;
            eyeMaterial = _materialType1;
        }

        if (material == null && eyeMaterial == null)
        {
            material = Board.Instance.ColorConfig.GetShooterColor(Color);
            eyeMaterial = Board.Instance.ColorConfig.GetShooterEyeColor(Color);

            if (Type == 1)
            {
                Type = 0;
                _hiddenEffect.Play();
            }
        }

        if (material == null || eyeMaterial == null) return;


        _renderer.sharedMaterials = new Material[] { material, eyeMaterial };
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
