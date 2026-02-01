using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class Base : MonoBehaviour
{
    [ReadOnly] public List<Cube> Cubes = new();

    [Title("Grid Settings")]
    [SerializeField] private int columns = 5;
    [SerializeField] private int rows = 3;
    [SerializeField, OnValueChanged(nameof(RefreshGrid))] private Vector2 cellSize = new Vector2(1f, 1f); // x = width, y = depth(Z)
    [SerializeField, OnValueChanged(nameof(RefreshGrid))] private Vector2 spacing = new Vector2(0.1f, 0.1f);

    [Button("Refresh Grid")]
    public void RefreshGrid()
    {
        int count = Mathf.Min(Cubes.Count, columns * rows);

        float totalWidth =
            columns * cellSize.x +
            (columns - 1) * spacing.x;

        float totalDepth =
            rows * cellSize.y +
            (rows - 1) * spacing.y;

        float startX = -totalWidth * 0.5f + cellSize.x * 0.5f;
        float startZ = totalDepth * 0.5f - cellSize.y * 0.5f;

        for (int i = 0; i < count; i++)
        {
            var cube = Cubes[i];
            if (cube == null) continue;

            cube.transform.SetParent(transform, false);

            int col = i % columns;
            int row = i / columns;

            float x = startX + col * (cellSize.x + spacing.x);
            float z = startZ - row * (cellSize.y + spacing.y);

            cube.transform.localPosition = new Vector3(
                x,
               0,
                z
            );
        }
    }

    [Button]
    public void AddCube(Cube cube)
    {
        if (Cubes.Contains(cube)) return;
        Cubes.Add(cube);
        RefreshGrid();
    }
    [Button]
    public void RemoveCube(Cube cube)
    {
        if (!Cubes.Remove(cube)) return;
        RefreshGrid();
    }
}
