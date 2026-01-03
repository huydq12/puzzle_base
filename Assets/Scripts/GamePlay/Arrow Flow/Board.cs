using System;
using System.Collections.Generic;
using System.Linq;
using Dreamteck.Splines;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

public class Board : Singleton<Board>
{
    [SerializeField] private GridCell _cellPrefab;
    [SerializeField] private Line _linePrefab;
    [SerializeField] private CubeLine _cubePrefab;
    [SerializeField] private GameColorConfig _colorConfig;
    [SerializeField] private float _cellSize;
    [SerializeField] private float _paddingCamera;
    [SerializeField] private Vector2 _spacing;
    [SerializeField] private SplineComputer _conveyorBelt;
    [HideInInspector] public GridCell[,] Cells;
    public Vector2 Spacing => _spacing;
    public GameColorConfig ColorConfig => _colorConfig;
    private LevelConfig _currentConfig;

    void SetupCamera()
    {
        float limit = 12f;
        float minPosX = Mathf.Infinity;
        float maxPosX = Mathf.NegativeInfinity;

        foreach (Transform child in transform)
        {
            float childPosX = child.position.x;

            if (childPosX < minPosX) minPosX = childPosX;
            if (childPosX > maxPosX) maxPosX = childPosX;
        }
        float halfSizeBoard = (maxPosX - minPosX + _cellSize * 2f + _paddingCamera * 2f) / (2f * Camera.main.aspect);
        Camera.main.orthographicSize = Mathf.Max(halfSizeBoard, limit);
    }
    private SplinePoint CreatePoint(Vector3 pos)
    {
        SplinePoint p = new SplinePoint(pos);
        p.type = SplinePoint.Type.SmoothMirrored;
        p.size = 1f;
        return p;
    }


    private bool IsRightAngle(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 d1 = (b - a).normalized;
        Vector3 d2 = (c - b).normalized;
        return Mathf.Abs(Vector3.Dot(d1, d2)) < 0.01f;
    }
    private void AddCornerPoints(
        List<SplinePoint> points,
        Vector3 prev,
        Vector3 curr,
        Vector3 next,
        float offset)
    {
        Vector3 dirIn = (curr - prev).normalized;
        Vector3 dirOut = (next - curr).normalized;

        points.Add(CreatePoint(curr - dirIn * offset));
        points.Add(CreatePoint(curr + dirOut * offset));
    }
    private void SetupShooter()
    {
        ShooterController.Instance.Setup(_currentConfig.Shooters);
    }
    private void SetupConveyor()
    {
        if (_currentConfig.ConveyorLine == null || _currentConfig.ConveyorLine.Cells.IsNullOrEmpty()) return;
        int rows = _currentConfig.Rows;
        int columns = _currentConfig.Columns;
        var orderedCells = BuildOrderedBoundaryCells(_currentConfig.ConveyorLine.Cells);

        List<Vector2> conveyorPolygon = new();
        foreach (var c in orderedCells)
        {
            GridCell cell = GetCellAt(c);
            if (cell == null) continue;

            Vector3 p = cell.transform.position;
            conveyorPolygon.Add(new Vector2(p.x, p.z));
        }
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                GridCell cell = Cells[col, row];
                Vector3 wp = cell.transform.position;

                bool inside = IsPointInsidePolygon(
                    new Vector2(wp.x, wp.z),
                    conveyorPolygon
                );

                cell.ShowRenderer(inside && _currentConfig.Cells[col, row].CellType != GridCellType.Conveyor);
            }
        }

        List<SplinePoint> points = new();
        float cornerOffset = _cellSize * 0.45f;

        for (int i = 0; i < orderedCells.Count; i++)
        {
            Vector3 prev = GetCellAt(
                orderedCells[(i - 1 + orderedCells.Count) % orderedCells.Count]
            ).transform.position;

            Vector3 curr = GetCellAt(orderedCells[i]).transform.position;

            Vector3 next = GetCellAt(
                orderedCells[(i + 1) % orderedCells.Count]
            ).transform.position;

            if (IsRightAngle(prev, curr, next))
            {
                AddCornerPoints(points, prev, curr, next, cornerOffset);
            }
            else
            {
                points.Add(CreatePoint(curr));
            }
        }

        _conveyorBelt.SetPoints(points.ToArray());
        _conveyorBelt.Close();
        _conveyorBelt.Rebuild();
    }


    static List<Vector2Int> BuildOrderedBoundaryCells(List<Vector2Int> cells)
    {
        HashSet<Vector2Int> set = new(cells);

        Vector2Int[] dirs =
        {
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.up
    };

        Vector2Int start = cells
            .OrderBy(c => c.y)
            .ThenBy(c => c.x)
            .First();

        List<Vector2Int> result = new();
        Vector2Int current = start;
        Vector2Int dir = Vector2Int.right;

        do
        {
            result.Add(current);

            bool moved = false;
            for (int i = 0; i < 4; i++)
            {
                Vector2Int nextDir = dirs[(Array.IndexOf(dirs, dir) + 3 + i) % 4];
                Vector2Int next = current + nextDir;

                if (set.Contains(next))
                {
                    dir = nextDir;
                    current = next;
                    moved = true;
                    break;
                }
            }

            if (!moved)
                break;

        } while (current != start);

        return result;
    }
    bool IsPointInsidePolygon(Vector2 p, List<Vector2> poly)
    {
        bool inside = false;

        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) /
                 (poly[j].y - poly[i].y) + poly[i].x))
            {
                inside = !inside;
            }
        }

        return inside;
    }
    bool IsCorner(Vector2Int prev, Vector2Int curr, Vector2Int next)
    {
        Vector2Int d1 = curr - prev;
        Vector2Int d2 = next - curr;
        return d1 != d2;
    }
    Direction DirFromDelta(Vector2Int delta)
    {
        if (delta == Vector2Int.up) return Direction.Forward;    // Y+ → Z+ ✓
        if (delta == Vector2Int.down) return Direction.Back;     // Y- → Z- ✓
        if (delta == Vector2Int.right) return Direction.Right;   // X+ ✓
        if (delta == Vector2Int.left) return Direction.Left;     // X- ✓

        return Direction.Forward;
    }
    float GetYawFromDirection(Direction dir)
    {
        switch (dir)
        {
            case Direction.Forward: return 0f;
            case Direction.Right: return 90f;
            case Direction.Back: return 180f;
            case Direction.Left: return 270f;
        }
        return 0f;
    }

    float GetCornerYaw(Direction inDir, Direction outDir)
    {
        int diff = ((int)outDir - (int)inDir + 4) % 4;

        if (diff == 1)
        {
            float yaw = ((int)inDir + 2) * 90f + 180f;
            return yaw % 360f;
        }
        else if (diff == 3)
        {
            float yaw = ((int)inDir + 1) * 90f;
            return yaw % 360f;
        }

        return 0f;
    }



    private void SetupLine()
    {
        foreach (var line in _currentConfig.ColorLines)
        {
            Line lineColor = Instantiate(
                _linePrefab,
                Vector3.zero,
                Quaternion.identity,
                transform
            );

            lineColor.Color = line.Color;
            lineColor.Cubes = new List<CubeLine>();

            var cells = line.Cells;
            int last = cells.Count - 1;

            for (int i = 0; i < cells.Count; i++)
            {
                Vector2Int curr = cells[i];
                Vector2Int? prev = i > 0 ? cells[i - 1] : (Vector2Int?)null;
                Vector2Int? next = i < last ? cells[i + 1] : (Vector2Int?)null;

                CubeLine cube = Instantiate(
                    _cubePrefab,
                    GetCellAt(curr).transform.position,
                    Quaternion.identity,
                    lineColor.transform
                );

                cube.SetColor(line.Color);

                if (i == last)
                {
                    cube.SetAsHead();

                    Direction dir = DirFromDelta(curr - prev.Value);
                    float yaw = GetYawFromDirection(dir);

                    cube.transform.localRotation = Quaternion.Euler(0f, yaw + 180, 0f);
                }

                else if (prev.HasValue && next.HasValue && IsCorner(prev.Value, curr, next.Value))
                {
                    cube.SetAsCorner();

                    Direction inDir = DirFromDelta(curr - prev.Value);
                    Direction outDir = DirFromDelta(next.Value - curr);

                    float yaw = GetCornerYaw(inDir, outDir);
                    cube.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                }
                else
                {
                    cube.SetAsNormal();
                }
                cube.Line = lineColor;
                lineColor.Cubes.Add(cube);
            }
        }
    }



    private void SetupGrid()
    {
        int rows = _currentConfig.Rows;
        int columns = _currentConfig.Columns;
        Vector2 spacing = _spacing;

        Cells = new GridCell[columns, rows];

        int expectedChildCount = rows * columns;
        for (int i = 0; i < expectedChildCount; i++)
            Instantiate(_cellPrefab, transform);

        Transform[] children = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
            children[i] = transform.GetChild(i);

        Vector2 offset = new Vector2(
            (columns - 1) * spacing.x / 2f,
            (rows - 1) * spacing.y / 2f
        );

        int index = 0;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Transform child = children[index++];

                Vector3 pos = new Vector3(
                    col * spacing.x - offset.x,
                    0f,
                    row * spacing.y - offset.y
                );
                child.localPosition = pos;

                GridCell cell = child.GetComponent<GridCell>();
                cell.Position = new Vector2Int(col, row);
                cell.name = $"Cell_{col}_{row}";
                Cells[col, row] = cell;
            }
        }
    }




    private void CenterPivotGrid()
    {
        if (transform.childCount == 0) return;

        Vector3 sum = Vector3.zero;
        int count = 0;

        foreach (Transform child in transform)
        {
            sum += child.localPosition;
            count++;
        }

        if (count == 0) return;

        Vector3 center = sum / count;

        foreach (Transform child in transform)
        {
            child.localPosition -= center;
        }
    }

    private GridCell GetCellAt(Vector2Int pos)
    {
        bool isValid = pos.x >= 0 && pos.x < Cells.GetLength(0) && pos.y >= 0 && pos.y < Cells.GetLength(1);
        if (!isValid)
        {
            Debug.LogError("Overflow");
            return null;
        }
        return Cells[pos.x, pos.y];
    }

    private void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }
    public void SetupLevel(LevelConfig config)
    {
        GameManagerInGame.Instance.SetState(GameStateInGame.Init);

        Clear();
        _currentConfig = config;
        SetupGrid();
        SetupLine();
        SetupConveyor();
        SetupShooter();
        // SetupCamera();
        GameManagerInGame.Instance.SetState(GameStateInGame.Playing);
    }
}