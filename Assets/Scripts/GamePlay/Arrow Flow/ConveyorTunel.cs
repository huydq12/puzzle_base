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
    [SerializeField] private MeshRenderer _renderer;
    [SerializeField] private SplineComputer _splineComputer;

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
        if (type != 0)
            gameObject.name = $"ConveyorTunel_T{type}_{counter}";

        if (_gate_start != null)
            _gate_start.transform.position = new Vector3(worldPositions[0].x, 0.5f, worldPositions[0].z);
        if (_gate_end != null)
            _gate_end.transform.position = new Vector3(worldPositions[^1].x, 0.5f, worldPositions[^1].z);

        if (_countTunel != null)
        {
            int midIdx = Mathf.Clamp(worldPositions.Count / 2, 0, worldPositions.Count - 1);
            _countTunel.transform.position = new Vector3(worldPositions[midIdx].x, 2f, worldPositions[midIdx].z);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DebugSetActive(_countTunel.gameObject, counter > 0, this, "ConveyorTunel.Setup() toggle _countTunel");
#else
            _countTunel.gameObject.SetActive(counter > 0);
#endif
            _countTunel.text = counter.ToString();
        }

        if (_splineComputer == null || worldPositions == null || worldPositions.Count < 2) return;

        SplineMesh splineMesh = GetComponent<SplineMesh>();
        if (splineMesh != null)
        {
            splineMesh.spline = _splineComputer;
            splineMesh.loopSamples = false;
            splineMesh.clipFrom = 0.0;
            splineMesh.clipTo = 1.0;
            splineMesh.autoUpdate = true;
        }

        if (_splineComputer.isClosed)
            _splineComputer.Break();

        _splineComputer.space = SplineComputer.Space.World;

        SplinePoint[] points = new SplinePoint[worldPositions.Count];
        for (int i = 0; i < worldPositions.Count; i++)
        {
            SplinePoint point = new SplinePoint(worldPositions[i])
            {
                type = SplinePoint.Type.SmoothMirrored,
                size = ConveyorController.Instance._cubeSize
            };
            points[i] = point;
        }

        _splineComputer.SetPoints(points, SplineComputer.Space.World);
        _splineComputer.RebuildImmediate(true, true);

        if (splineMesh != null)
            splineMesh.RebuildImmediate();
    }
}
