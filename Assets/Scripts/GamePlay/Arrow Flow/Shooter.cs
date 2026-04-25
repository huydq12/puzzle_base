using System;
using System.Collections;
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
    // Must match level config ShooterData.Type for rainbow shooters.
    public const int RainbowType = 9;
    // Marks the injected fallback rainbow shooter so it can start disabled until manually enabled (debug).
    public const int FallbackRainbowShooterTieId = -999;
    [SerializeField] private Renderer _renderer;
    [SerializeField] private Renderer _rendererDouble;
    [Header("Rainbow Visual (optional)")]
    [SerializeField] private GameObject _rainbow;
    [SerializeField] private GameObject _rainbowLock;
    [SerializeField] private TextMeshPro _total;
    [SerializeField] private Material _materialType1;
    [SerializeField] private Material _materialType6;
    [SerializeField] private Material _materialHoleDefault;
    [SerializeField] private Vector3 _offsetRay;
    [SerializeField] private float _rayDistance;
    [SerializeField] private float _rainbowRayDistance = 20f;
    [SerializeField] private LayerMask _cubeLayer;
    [SerializeField] private ParticleSystem _hiddenEffect;
    [SerializeField] private Bullet _bulletPrefab;
    [SerializeField] private Transform _bulletSpawnPoint;
    [SerializeField] private Animation _animation;
    [SerializeField] private Outline _outline;
    [SerializeField] private Renderer _holeRenderer;

    [SerializeField] private Renderer _hidenRenderer;

    [SerializeField] private Transform _holeBottom;
    private float _bulletSpeed = 50f; // set a sensible default (tune in Inspector)
    [SerializeField] private bool _drawGizmos;
    [SerializeField] private int _bulletPoolSize;
    [ReadOnly] public ObjectColor Color;
    [ReadOnly] public int Type;
    [ReadOnly] public int TieID = -1;
    [ReadOnly] public bool CanShoot;
    [ReadOnly] public IGate Gate;
    private RaycastHit _hit;
    private Vector3 _originalScale;
    private CubeLine _lastHit;

    private bool _collectRequested;
    private float _baseRayDistance;
    private bool _baseRayDistanceCached;

    //Fire cooldown (seconds) � controls max fire rate while preserving existing logic
    private float _fireCooldown = 0.025f; // was 0.15f
    private float _nextFireTime = 0f;

    private int _totalValue;
    private int _inFlightCount;

    private const float IdleFallbackDelaySeconds = 3f;
    private float _lastActivityTime;

    private ShooterRole _role;

    private Tween _idleTween;
    private Vector3 _idleBaseEuler;
    private Vector3 _idleBaseScale;
    private bool _hasIdleBase;
    private Coroutine _enableShootRoutine;

    public bool IsRainbow => Type == RainbowType;
    private float _nextDebugRaycastTime;
    private bool _rainbowVisible;
    private bool _suppressRainbowLockIcon;

    [SerializeField] private ParticleSystem _hitEffect;
    [SerializeField] private ParticleSystem _activeEffect;
    private readonly Dictionary<ParticleSystem, ParticleSystem.MinMaxGradient> _effectBaseStartColors = new();

    private void UpdateDoubleRendererState()
    {
        if (_rendererDouble == null) return;
        bool shouldEnable = Type == 6 && _role == ShooterRole.Current && gameObject.activeInHierarchy;
        if (_rendererDouble.enabled != shouldEnable)
            _rendererDouble.enabled = shouldEnable;
    }

    public void SetCubeLayerMask(LayerMask layerMask)
    {
        _cubeLayer = layerMask;
    }

    private void CacheBaseRayDistance()
    {
        if (_baseRayDistanceCached) return;
        _baseRayDistance = _rayDistance > 0f ? _rayDistance : 4f;
        _baseRayDistanceCached = true;
    }

    private void UpdateRayDistance()
    {
        CacheBaseRayDistance();
        _rayDistance = IsRainbow ? Mathf.Max(0.1f, _rainbowRayDistance) : _baseRayDistance;
    }

    private void UpdateRainbowState(bool force = false)
    {
        // When in rainbow mode we hide the base render object (a separate rainbow visual can be used).
        if (_renderer != null)
        {
            // Avoid disabling the whole Shooter object if the renderer is on the same GameObject.
            if (_renderer.gameObject == gameObject)
                _renderer.enabled = !IsRainbow;
            else
                _renderer.gameObject.SetActive(!IsRainbow);
        }

        // If no custom rainbow model is wired, keep old behavior (material swap).
        if (_rainbow == null)
        {
            _rainbowVisible = false;
            UpdateRainbowLockState(force: true);
            return;
        }

        bool shouldShowRainbow = IsRainbow && gameObject.activeInHierarchy;
        if (!force && _rainbowVisible == shouldShowRainbow) return;
        _rainbowVisible = shouldShowRainbow;

        _rainbow.SetActive(shouldShowRainbow);
        if (_rendererDouble != null) _rendererDouble.enabled = !shouldShowRainbow && (Type == 6 && _role == ShooterRole.Current);
        UpdateRainbowLockState(force: true);
    }

    private void UpdateRainbowLockState(bool force)
    {
        if (_rainbowLock == null) return;

        if (_suppressRainbowLockIcon)
        {
            if (_rainbowLock.activeSelf)
                _rainbowLock.SetActive(false);
            return;
        }

        // Only applies to the rainbow visual.
        if (!IsRainbow || _rainbow == null || !_rainbow.activeInHierarchy)
        {
            if (_rainbowLock.activeSelf)
            {
                _rainbowLock.SetActive(false);
            }
            return;
        }

        // Show lock only while waiting-to-open with ammo available.
        // Do not show it during in-flight final shots (e.g. Total == 1 about to finish).
        bool desired = !CanShoot && Total > 0 && _inFlightCount <= 0 && !_collectRequested;
        bool changed = _rainbowLock.activeSelf != desired;
        if (!force && !changed) return;

        _rainbowLock.SetActive(desired);
    }

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
        CacheBaseRayDistance();
        UpdateRayDistance();
    }

    private void OnEnable()
    {
        ApplyShooterEffectColor();
        TriggerActiveEffect();
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
        _inFlightCount = 0;
        _nextFireTime = 0f;
        _lastActivityTime = Time.time;
        CanShoot = false;
        Gate = null;
        TieID = -1;

        Total = 0;

        if (_hiddenEffect != null)
            _hiddenEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (_activeEffect != null)
            _activeEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (_rendererDouble != null)
            _rendererDouble.enabled = false;

        _rainbowVisible = false;
        _suppressRainbowLockIcon = false;
        if (_rainbow != null) _rainbow.SetActive(false);
        if (_rainbowLock != null) _rainbowLock.SetActive(false);
        if (_renderer != null) _renderer.enabled = true;

        if (_enableShootRoutine != null)
        {
            StopCoroutine(_enableShootRoutine);
            _enableShootRoutine = null;
        }

        _effectBaseStartColors.Clear();
    }

    public void SetSuppressRainbowLockIcon(bool suppress)
    {
        _suppressRainbowLockIcon = suppress;
        UpdateRainbowLockState(force: true);
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
        // Hole mode: no Show/Hide anim, no outline.
        _lastActivityTime = Time.time;
        if (_outline != null) _outline.enabled = false;
        switch (role)
        {
            case ShooterRole.Current:
                if (_enableShootRoutine != null) StopCoroutine(_enableShootRoutine);
                if (TieID == FallbackRainbowShooterTieId)
                {
                    // Fallback shooter starts disabled until explicitly enabled.
                    CanShoot = false;
                    _enableShootRoutine = null;
                }
                else
                {
                    _enableShootRoutine = StartCoroutine(Common.DelayAction(0.2f, () =>
                    {
                        CanShoot = true;
                        _enableShootRoutine = null;
                    }));
                }
                ShowTotal = true;
                _collectRequested = false;
                SetSize(0.75f);
                TriggerActiveEffect();
                break;

            case ShooterRole.Next:
            case ShooterRole.Queue:
                if (_enableShootRoutine != null)
                {
                    StopCoroutine(_enableShootRoutine);
                    _enableShootRoutine = null;
                }
                CanShoot = false;
                ShowTotal = false;
                SetSize(0.65f);
                break;
        }

        if (Type == 1 && role == ShooterRole.Current)
            ApplyMaterial(forceColorMaterial: true);
        else
            ApplyMaterial();

        UpdateDoubleRendererState();
        UpdateRainbowState(force: true);
    }

    public void SetSize(float size)
    {
        transform.localScale = size * Vector3.one;
        _originalScale = transform.localScale;
    }

    private void Update()
    {
        UpdateRainbowLockState(force: false);

        // Hole mode: no idle animation

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
                // Ignore cubes already placed on grid cells (these are not shoot targets and should not block).
                if (cube.Cell != null)
                    continue;

                // Choose first cube that matches color (or any color if rainbow) and is not placed on a cell.
                bool colorMatches = IsRainbow || cube.Color == Color;
                if (cube != _lastHit && colorMatches)
                {
                    _hit = hit;
                    _lastHit = cube;
                    AbsorbCube(cube);
                    return;
                }

                // First non-cell cube blocks further hits.
                break;
            }
        }

        _lastHit = null;

    }

    private void AbsorbCube(CubeLine cube)
    {
        _nextFireTime = Time.time + _fireCooldown;
        _lastActivityTime = Time.time;

        // Cube jumps up above the hole entrance, then falls down into _holeBottom.
        Vector3 peakPosition = transform.position + Vector3.up * 2.5f;
        ObjectColor hitColor = cube.Color;
        bool destroyed = cube.OnHitByHole(IsRainbow, peakPosition, _holeBottom, () =>
        {
            // Called when the cube reaches the bottom of the hole.
            AudioManager.Instance.PlaySFX(SFXType.Shoot);
            SpawnHoleHitEffect();
            _inFlightCount = Mathf.Max(0, _inFlightCount - 1);
            Board.Instance?.NotifyLineDoorHit(hitColor, this);

            Total = Mathf.Max(0, Total - 1);
            if (Total <= 0)
            {
                CanShoot = false;
            }
            TryRequestCollect();
        });

        if (destroyed)
        {
            _inFlightCount++;
            // Stop firing once all remaining ammo is in flight.
            if (_inFlightCount >= Total)
            {
                CanShoot = false;
            }
        }
        else
        {
            // Cube did not jump (ice locked / layer revealed). Consume ammo immediately.
            Total = Mathf.Max(0, Total - 1);
            if (Total <= 0)
            {
                CanShoot = false;
            }
            TryRequestCollect();
        }
    }

    private void SpawnHoleHitEffect()
    {
        if (_hitEffect == null) return;

        // if (_holeBottom != null)
        //     _hitEffect.transform.position = _holeBottom.position;
        // else
        //     _hitEffect.transform.position = transform.position;

        // _hitEffect.transform.rotation = Quaternion.identity;
        _hitEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _hitEffect.Play(true);
    }

    private void TriggerActiveEffect()
    {
        if (_activeEffect == null) return;
        _activeEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _activeEffect.Play(true);
    }

    private void ApplyShooterEffectColor()
    {
        UnityEngine.Color shooterFxColor = ResolveShooterFxColor();
        ApplyColorToEffect(_hitEffect, shooterFxColor);
        ApplyColorToEffect(_activeEffect, shooterFxColor);
    }

    private UnityEngine.Color ResolveShooterFxColor()
    {
        if (Board.Instance == null || Board.Instance.ColorConfig == null)
            return UnityEngine.Color.white;

        UnityEngine.Color c = Board.Instance.ColorConfig.GetLineDoorColor(Color);
        if (c.a <= 0f)
            c = Board.Instance.ColorConfig.GetOutlineShooter(Color);
        if (c.a <= 0f)
            c = UnityEngine.Color.white;
        return c;
    }

    private void ApplyColorToEffect(ParticleSystem rootEffect, UnityEngine.Color tintColor)
    {
        if (rootEffect == null) return;

        ParticleSystem[] systems = rootEffect.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem ps = systems[i];
            if (ps == null) continue;

            if (!_effectBaseStartColors.TryGetValue(ps, out var baseStartColor))
            {
                baseStartColor = ps.main.startColor;
                _effectBaseStartColors[ps] = baseStartColor;
            }

            var main = ps.main;
            main.startColor = TintStartGradient(baseStartColor, tintColor);
        }
    }

    private static ParticleSystem.MinMaxGradient TintStartGradient(ParticleSystem.MinMaxGradient baseGradient, UnityEngine.Color tintColor)
    {
        switch (baseGradient.mode)
        {
            case ParticleSystemGradientMode.Color:
                return new ParticleSystem.MinMaxGradient(TintColor(baseGradient.color, tintColor));

            case ParticleSystemGradientMode.TwoColors:
                return new ParticleSystem.MinMaxGradient(
                    TintColor(baseGradient.colorMin, tintColor),
                    TintColor(baseGradient.colorMax, tintColor)
                );

            case ParticleSystemGradientMode.Gradient:
                return new ParticleSystem.MinMaxGradient(TintGradient(baseGradient.gradient, tintColor));

            case ParticleSystemGradientMode.TwoGradients:
                return new ParticleSystem.MinMaxGradient(
                    TintGradient(baseGradient.gradientMin, tintColor),
                    TintGradient(baseGradient.gradientMax, tintColor)
                );

            case ParticleSystemGradientMode.RandomColor:
                return new ParticleSystem.MinMaxGradient(TintGradient(baseGradient.gradient, tintColor));

            default:
                return new ParticleSystem.MinMaxGradient(TintColor(baseGradient.color, tintColor));
        }
    }

    private static Gradient TintGradient(Gradient source, UnityEngine.Color tintColor)
    {
        if (source == null) return null;

        GradientColorKey[] colorKeys = source.colorKeys;
        GradientAlphaKey[] alphaKeys = source.alphaKeys;

        for (int i = 0; i < colorKeys.Length; i++)
        {
            UnityEngine.Color c = colorKeys[i].color;
            c.r *= tintColor.r;
            c.g *= tintColor.g;
            c.b *= tintColor.b;
            colorKeys[i].color = c;
        }

        Gradient g = new Gradient();
        g.SetKeys(colorKeys, alphaKeys);
        return g;
    }

    private static UnityEngine.Color TintColor(UnityEngine.Color source, UnityEngine.Color tint)
    {
        source.r = tint.r;
        source.g = tint.g;
        source.b = tint.b;
        return source;
    }

    private void TryRequestCollect()
    {
        if (_collectRequested) return;
        if (Total > 0) return;
        if (_inFlightCount > 0) return;
        _collectRequested = true;
        Gate?.CollectCurrentShooter();
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
        ApplyShooterEffectColor();
        UpdateDoubleRendererState();
        UpdateRainbowState();
    }

    public void SetType(int type)
    {
        Type = type;
        UpdateRayDistance();
        ApplyMaterial();
        ApplyShooterEffectColor();
        UpdateDoubleRendererState();
        UpdateRainbowState(force: true);
    }

    public void SetTieId(int tieId)
    {
        TieID = tieId;
    }

    public void SetRainbow()
    {
        Type = RainbowType;
        UpdateRayDistance();
        ApplyMaterial();
        ApplyShooterEffectColor();
        UpdateDoubleRendererState();
        UpdateRainbowState(force: true);
    }

    private void ApplyMaterial(bool forceColorMaterial = false)
    {
        // Hole mode: hide normal/double renderers and render via _holeRenderer only.
        if (_renderer != null) _renderer.enabled = false;
        if (_rendererDouble != null) _rendererDouble.enabled = false;

        if (_holeRenderer == null) return;

        bool wasType1 = Type == 1;
        Material material = null;

        // Keep legacy type materials for hole-render mode as well.
        if (IsRainbow && _materialType6 != null)
        {
            material = _materialType6;
        }
        else if (!forceColorMaterial && Type == 1 && _materialType1 != null)
        {
            material = _materialType1;
            _hidenRenderer.sharedMaterial = _materialType1;
        }

        if (material == null)
        {
            if (Board.Instance == null || Board.Instance.ColorConfig == null) return;
            material = Board.Instance.ColorConfig.GetShooterColor(Color);
            
            // Legacy behavior: when a type-1 shooter becomes current (forced color material),
            // convert it to normal and play reveal VFX once.
            if (wasType1)
            {
                Type = 0;
                TriggerHiddenEffect();
            }
        }

        if (material != null)
            _holeRenderer.sharedMaterial = material;
    }

    private void TriggerHiddenEffect()
    {
        if (_hiddenEffect == null) return;
        _hidenRenderer.sharedMaterial = _materialHoleDefault;
        _hiddenEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _hiddenEffect.Play(true);
        AudioManager.Instance.PlaySFX(SFXType.ShowHidden);
        VibrateManager.Instance.MediumVibrate();
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
