using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using TMPro;
using Dreamteck.Splines;

public class ConveyorTunel : MonoBehaviour
{
    [SerializeField] private TextMeshPro _countTunel;
    [SerializeField] private GameObject _gate_start;
    [SerializeField] private GameObject _gate_end;
    [SerializeField] private GameObject _bg_text;
    [SerializeField] private MeshRenderer _renderer;
    [SerializeField] private SplineComputer _splineComputer;

    [SerializeField] private float _splineHeight;
    [SerializeField] private float _gateHeight = 0.5f;
    [SerializeField] private float _textHeight = 2f;
    [SerializeField] private float _closeDuration = 0.25f;
    [SerializeField] private float _shooterVfxTravelDuration = 0.25f;
    [SerializeField] private float _shooterVfxArcHeight = 1.5f;

    [SerializeField] private ParticleSystem _particleSystemHole1;
    [SerializeField] private ParticleSystem _particleSystemHole2;

    [SerializeField] private ParticleSystem _particleSystemShooter;

    public int Type { get; private set; }
    public int Counter { get; private set; }

    private SplineMesh _splineMesh;
    private Coroutine _closeRoutine;
    private Coroutine _shooterVfxRoutine;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private static void DebugSetActive(GameObject obj, bool active, UnityEngine.Object context, string reason)
    {
        if (obj == null) return;
        if (obj.activeSelf == active) return;
        UnityEngine.Debug.LogWarning($"[ConveyorTunel.DebugSetActive] {obj.name} -> {active} reason={reason}\n{new StackTrace(true)}", context);
        obj.SetActive(active);
    }

    private void OnEnable()
    {
        UnityEngine.Debug.Log($"[ConveyorTunel] Enabled: {name} (activeSelf={gameObject.activeSelf}, activeInHierarchy={gameObject.activeInHierarchy})", this);
    }

    private void OnDisable()
    {
        UnityEngine.Debug.LogWarning(
            $"[ConveyorTunel] Disabled: {name} (activeSelf={gameObject.activeSelf}, activeInHierarchy={gameObject.activeInHierarchy})\n{new StackTrace(true)}",
            this
        );
    }
#endif

    public void Setup(int type, int counter, IReadOnlyList<Vector3> worldPositions)
    {
        Type = type;
        SetCounter(counter);

        if (Type != 0)
            gameObject.name = $"ConveyorTunel_T{Type}_{Counter}";

        if (_gate_start != null)
            _gate_start.transform.position = new Vector3(worldPositions[0].x, _gateHeight, worldPositions[0].z);
        if (_gate_end != null)
            _gate_end.transform.position = new Vector3(worldPositions[^1].x, _gateHeight, worldPositions[^1].z);
        if (worldPositions != null && worldPositions.Count >= 2)
        {
            Vector3 startDir = worldPositions[1] - worldPositions[0];
            startDir.y = 0f;
            if (_gate_start != null && startDir.sqrMagnitude > 0.0001f)
                _gate_start.transform.rotation = Quaternion.LookRotation(startDir.normalized, Vector3.up);

            Vector3 endDir = worldPositions[^1] - worldPositions[^2];
            endDir.y = 0f;
            if (_gate_end != null && endDir.sqrMagnitude > 0.0001f)
                _gate_end.transform.rotation = Quaternion.LookRotation(endDir.normalized, Vector3.up);
        }

        // If counter is 0, tunnel should be hidden and not block.
        if (Counter <= 0)
            return;

        if (_countTunel != null)
        {
            int midIdx = Mathf.Clamp(worldPositions.Count / 2, 0, worldPositions.Count - 1);

            _bg_text.transform.position = new Vector3(worldPositions[midIdx].x, 2f, worldPositions[midIdx].z);
            _countTunel.transform.position = new Vector3(worldPositions[midIdx].x, 2f, worldPositions[midIdx].z);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DebugSetActive(_countTunel.gameObject, Counter > 0, this, "ConveyorTunel.Setup() toggle _countTunel");
#else
            _countTunel.gameObject.SetActive(Counter > 0);
#endif
            _countTunel.text = Counter.ToString();

            _countTunel.transform.position = worldPositions[midIdx] + Vector3.up * _splineHeight + Vector3.up * _textHeight;
        }

        if (_splineComputer == null || worldPositions == null || worldPositions.Count < 2) return;

        _splineMesh = GetComponent<SplineMesh>();
        if (_splineMesh != null)
        {
            _splineMesh.spline = _splineComputer;
            _splineMesh.loopSamples = false;
            _splineMesh.clipFrom = 0.0;
            _splineMesh.clipTo = 1.0;
            _splineMesh.autoUpdate = true;
        }

        if (_splineComputer.isClosed)
            _splineComputer.Break();

        _splineComputer.space = SplineComputer.Space.World;

        SplinePoint[] points = new SplinePoint[worldPositions.Count];
        for (int i = 0; i < worldPositions.Count; i++)
        {
            SplinePoint point = new SplinePoint(worldPositions[i] + Vector3.up * _splineHeight)
            {
                type = SplinePoint.Type.SmoothMirrored,
                size = _splineHeight
            };
            points[i] = point;
        }

        _splineComputer.SetPoints(points, SplineComputer.Space.World);
        _splineComputer.RebuildImmediate(true, true);

        if (_splineMesh != null)
            _splineMesh.RebuildImmediate();
    }

    public void SetCounter(int counter)
    {
        Counter = Mathf.Max(0, counter);

        if (Counter <= 0)
        {
            SetGatesActive(false);
            if (_countTunel != null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                DebugSetActive(_countTunel.gameObject, false, this, "ConveyorTunel.SetCounter() toggle _countTunel");
#else
                _countTunel.gameObject.SetActive(false);
#endif
            }

            StartCloseAnimation();
            return;
        }

        StopCloseAnimation();
        ResetSplineClip();
        SetGatesActive(true);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        DebugSetActive(gameObject, true, this, "ConveyorTunel.SetCounter() toggle tunnel");
#else
        gameObject.SetActive(true);
#endif

        if (_countTunel == null) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        DebugSetActive(_countTunel.gameObject, true, this, "ConveyorTunel.SetCounter() toggle _countTunel");
#else
        _countTunel.gameObject.SetActive(true);
#endif

        _countTunel.text = Counter.ToString();
    }

    private void SetGatesActive(bool active)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        DebugSetActive(_gate_start, active, this, "ConveyorTunel.SetGatesActive() gate_start");
        DebugSetActive(_gate_end, active, this, "ConveyorTunel.SetGatesActive() gate_end");
#else
        if (_gate_start != null) _gate_start.SetActive(active);
        if (_gate_end != null) _gate_end.SetActive(active);
#endif
    }

    private void StartCloseAnimation()
    {
        EnsureSplineMesh();

        if (_closeRoutine != null)
            StopCoroutine(_closeRoutine);

        if (_closeDuration <= 0f || _splineMesh == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DebugSetActive(gameObject, false, this, "ConveyorTunel.StartCloseAnimation() no spline");
#else
            gameObject.SetActive(false);
#endif
            return;
        }

        _closeRoutine = StartCoroutine(CloseRoutine());
    }

    private void StopCloseAnimation()
    {
        if (_closeRoutine == null) return;
        StopCoroutine(_closeRoutine);
        _closeRoutine = null;
    }

    private void ResetSplineClip()
    {
        EnsureSplineMesh();
        if (_splineMesh == null) return;
        _splineMesh.clipFrom = 0.0f;
        _splineMesh.clipTo = 1.0f;
        _splineMesh.RebuildImmediate();
    }

    private void EnsureSplineMesh()
    {
        if (_splineMesh == null)
            _splineMesh = GetComponent<SplineMesh>();
    }

    private IEnumerator CloseRoutine()
    {
        float startFrom = (float)_splineMesh.clipFrom;
        float startTo = (float)_splineMesh.clipTo;
        float target = 0.5f;
        float time = 0f;

        while (time < _closeDuration)
        {
            float t = time / _closeDuration;
            _splineMesh.clipFrom = Mathf.Lerp(startFrom, target, t);
            _splineMesh.clipTo = Mathf.Lerp(startTo, target, t);
            _splineMesh.RebuildImmediate();
            time += Time.deltaTime;
            yield return null;
        }

        _splineMesh.clipFrom = target;
        _splineMesh.clipTo = target;
        _splineMesh.RebuildImmediate();

        while (_shooterVfxRoutine != null)
            yield return null;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        DebugSetActive(gameObject, false, this, "ConveyorTunel.CloseRoutine() done");
#else
        gameObject.SetActive(false);
#endif

        _closeRoutine = null;
    }

    public void PlayShooterVfx(Transform shooterTransform)
    {
        if (_particleSystemShooter == null) return;

        Vector3 startPos = shooterTransform != null ? shooterTransform.position : _particleSystemShooter.transform.position;
        Vector3 targetPos = GetHoleTargetPosition(startPos);

        if (_shooterVfxRoutine != null)
            StopCoroutine(_shooterVfxRoutine);

        _shooterVfxRoutine = StartCoroutine(ShooterVfxRoutine(startPos, targetPos));
    }

    private Vector3 GetHoleTargetPosition(Vector3 from)
    {
        bool hasHole1 = _particleSystemHole1 != null;
        bool hasHole2 = _particleSystemHole2 != null;

        if (hasHole1 && hasHole2)
        {
            Vector3 p1 = _particleSystemHole1.transform.position;
            Vector3 p2 = _particleSystemHole2.transform.position;
            return Vector3.Distance(from, p1) <= Vector3.Distance(from, p2) ? p1 : p2;
        }

        if (hasHole1) return _particleSystemHole1.transform.position;
        if (hasHole2) return _particleSystemHole2.transform.position;
        return transform.position;
    }

    private IEnumerator ShooterVfxRoutine(Vector3 startPos, Vector3 targetPos)
    {
        ParticleSystem shooterVfx = _particleSystemShooter;
        if (shooterVfx == null)
        {
            _shooterVfxRoutine = null;
            yield break;
        }

        shooterVfx.transform.position = startPos;
        if (!shooterVfx.gameObject.activeSelf)
            shooterVfx.gameObject.SetActive(true);

        shooterVfx.Play(true);

        float duration = Mathf.Max(0.01f, _shooterVfxTravelDuration);
        Vector3 mid = (startPos + targetPos) * 0.5f + Vector3.up * _shooterVfxArcHeight;
        float time = 0f;
        while (time < duration)
        {
            float t = time / duration;
            shooterVfx.transform.position = QuadraticBezier(startPos, mid, targetPos, t);
            time += Time.deltaTime;
            yield return null;
        }

        shooterVfx.transform.position = targetPos;
        shooterVfx.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        PlayHoleVfx();

        _shooterVfxRoutine = null;
    }

    private static Vector3 QuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        float u = 1f - t;
        return u * u * a + 2f * u * t * b + t * t * c;
    }

    private void PlayHoleVfx()
    {
        if (_particleSystemHole1 != null)
        {
            if (!_particleSystemHole1.gameObject.activeSelf)
                _particleSystemHole1.gameObject.SetActive(true);
            _particleSystemHole1.Play(true);
        }

        if (_particleSystemHole2 != null)
        {
            if (!_particleSystemHole2.gameObject.activeSelf)
                _particleSystemHole2.gameObject.SetActive(true);
            _particleSystemHole2.Play(true);
        }
    }
}
