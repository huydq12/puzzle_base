using System.Collections.Generic;
using DG.Tweening;
using Dreamteck.Splines;
using Sirenix.OdinInspector;
using UnityEngine;

public class Base : MonoBehaviour
{
    [ReadOnly] public List<Cube> Cubes = new();
    [SerializeField, OnValueChanged(nameof(RefreshGrid))] private Vector2 _cellSize = new Vector2(1f, 1f);

    [SerializeField, OnValueChanged(nameof(RefreshGrid))] private float _spacingX = 0.1f;
    [SerializeField] private SplinePositioner _positioner;
    public SplinePositioner Positioner => _positioner;
    private const int COLUMNS = 5;
    public void DestroyLine()
    {
        if (Cubes.Count == 0) return;

        Sequence sq = DOTween.Sequence();
        float delay = 0f;

        for (int i = Cubes.Count - 1; i >= 0; i--)
        {
            Cube cube = Cubes[i];
            if (cube == null) continue;

            sq.Insert(delay += 0.1f, cube.Destroy());
        }

        sq.OnComplete(() =>
        {
            Cubes.Clear();
        });
    }
    public void RefreshGrid()
    {
        int count = Mathf.Min(Cubes.Count, COLUMNS);

        float totalWidth = COLUMNS * _cellSize.x + (COLUMNS - 1) * _spacingX;

        float startX = -totalWidth * 0.5f + _cellSize.x * 0.5f;

        for (int i = 0; i < count; i++)
        {
            var cube = Cubes[i];
            if (cube == null) continue;

            cube.transform.SetParent(transform, false);

            float x = startX + i * (_cellSize.x + _spacingX);

            cube.transform.localPosition = new Vector3(x, 0f, 0f);
        }
    }

    public void AddCube(Cube cube)
    {
        if (cube == null || Cubes.Contains(cube)) return;

        Cubes.Add(cube);
        RefreshGrid();
    }

    public void RemoveCube(Cube cube)
    {
        if (cube == null) return;

        if (Cubes.Remove(cube))
            RefreshGrid();
    }
}
