using System.Collections.Generic;
using Dreamteck.Splines;
using Sirenix.OdinInspector;
using UnityEngine;

public class ConveyorController : MonoBehaviour
{
    [SerializeField] private SplinePositioner _cubePrefab;
    [SerializeField] private SplineComputer _conveyor;
    [SerializeField] private float _speed = 0.15f; 
    [SerializeField] private float _cubeSize = 1f;
    [SerializeField] private float _spacing = 0.1f;     public CubeLine CubeOnBase;
    public bool IsOccupied => CubeOnBase != null;
    private readonly List<SplinePositioner> _cubes = new();

    [Button]
    public void Spawn(int total)
    {
        Clear();
        SpawnCubes(total);
    }

    [Button]
    public void AutoSpawn()
    {
        Clear();

        float splineLength = _conveyor.CalculateLength();
        float distancePerCube = _cubeSize + _spacing;
        int total = Mathf.CeilToInt(splineLength / distancePerCube);
        SpawnCubes(total);
    }

    private void SpawnCubes(int total)
    {
        for (int i = 0; i < total; i++)
        {
            var cube = Instantiate(_cubePrefab, transform);
            cube.spline = _conveyor;
            cube.motion.applyRotation = false;

            _cubes.Add(cube);
        }
    }

    void Update()
    {
        if (_cubes.Count == 0) return;

        float basePercent = (Time.time * _speed) % 1f;
        int count = _cubes.Count;

        for (int i = 0; i < count; i++)
        {
            float offset = (float)i / count;
            _cubes[i].SetPercent((basePercent + offset) % 1f);
        }
    }

    private void Clear()
    {
        foreach (var c in _cubes)
        {
            if (c != null) DestroyImmediate(c.gameObject);
        }
        _cubes.Clear();
    }
}