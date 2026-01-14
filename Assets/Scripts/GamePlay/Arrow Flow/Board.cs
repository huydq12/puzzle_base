using System;
using System.Collections.Generic;
using System.Linq;
using Dreamteck.Splines;
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
    public void RefreshAllHeadHighlights()
    {
        int w = Cells.GetLength(0);
        int h = Cells.GetLength(1);

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                GridCell cell = Cells[x, y];
                if (cell == null || !cell.IsOccupied) continue;

                CubeLine cube = cell.CubeOnCell;
                if (cube == null || cube.Type != CubeType.Head) continue;

                cube.HighlightHead = CanHeadReachConveyor(cube);
            }
        }
    }
    private bool CanHeadReachConveyor(CubeLine head)
    {
        if (head == null) return false;
        if (head.Type != CubeType.Head) return false;
        if (head.Line == null) return false;
        if (head.Cell == null) return false;

        List<CubeLine> cubes = head.Line.Cubes;
        if (cubes == null || cubes.Count < 2) return false;

        CubeLine tailMinus1 = cubes[^2];
        CubeLine tail = cubes[^1];
        if (tail == null || tailMinus1 == null) return false;
        if (tail.Cell == null || tailMinus1.Cell == null) return false;
        if (tail != head) return false;

        Vector2Int p0 = tailMinus1.Cell.Position;
        Vector2Int p1 = tail.Cell.Position;
        Vector2Int dir = NormalizeGridDir(p1 - p0);
        if (dir == Vector2Int.zero) return false;

        Vector2Int curr = p1;
        Vector2Int prev = curr - dir;

        GridCell conveyorCell = FindConveyorCell(prev, curr);
        if (conveyorCell == null) return false;

        GridCell occupiedCell = FindOccupiedCell(prev, curr);
        if (occupiedCell == null) return true;

        int distToConveyor = GetManhattanDistance(curr, conveyorCell.Position) - 1;
        int distToObstacle = GetManhattanDistance(curr, occupiedCell.Position) - 1;

        return distToObstacle >= distToConveyor;
    }
    public int GetManhattanDistance(Vector2Int a, Vector2Int b)
    {
        Vector2Int d = b - a;
        return Mathf.Abs(d.x) + Mathf.Abs(d.y);
    }
    public Vector2Int NormalizeGridDir(Vector2Int d)
    {
        if (d == Vector2Int.zero)
            return d;

        int ax = Mathf.Abs(d.x);
        int ay = Mathf.Abs(d.y);

        if (ax >= ay)
            return new Vector2Int(d.x > 0 ? 1 : -1, 0);
        return new Vector2Int(0, d.y > 0 ? 1 : -1);
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
        var conveyorCells = FilterInBoundsDistinct(_currentConfig.ConveyorLine.Cells, columns, rows);
        if (conveyorCells.Count < 3)
        {
            Debug.LogWarning("ConveyorLine has too few valid cells; skipping conveyor setup.");
            return;
        }

        var orderedCells = IsClosedNeighborLoop(conveyorCells) ? conveyorCells : BuildOrderedBoundaryCells(conveyorCells);
        if (orderedCells == null || orderedCells.Count < 3)
        {
            Debug.LogWarning("ConveyorLine cannot be ordered into a valid loop; skipping conveyor setup.");
            return;
        }

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
                cell.CellType = _currentConfig.Cells[col, row].CellType;
                cell.ShowRenderer(cell.IsOccupied);
            }
        }

        List<Vector3> allPositions = new();
        float cornerOffset = _cellSize * 0.45f;

        for (int i = 0; i < orderedCells.Count; i++)
        {
            GridCell prevCell = GetCellAt(orderedCells[(i - 1 + orderedCells.Count) % orderedCells.Count]);
            GridCell currCell = GetCellAt(orderedCells[i]);
            GridCell nextCell = GetCellAt(orderedCells[(i + 1) % orderedCells.Count]);

            if (prevCell == null || currCell == null || nextCell == null)
            {
                Debug.LogWarning("ConveyorLine contains invalid cell references; skipping conveyor setup.");
                return;
            }

            Vector3 prev = prevCell.transform.position;
            Vector3 curr = currCell.transform.position;
            Vector3 next = nextCell.transform.position;

            if (IsRightAngle(prev, curr, next))
            {
                Vector3 dirIn = (curr - prev).normalized;
                Vector3 dirOut = (next - curr).normalized;

                allPositions.Add(curr - dirIn * cornerOffset);
                allPositions.Add(curr + dirOut * cornerOffset);
            }
            else
            {
                allPositions.Add(curr);
            }
        }

        float totalDistance = 0f;
        for (int i = 0; i < allPositions.Count; i++)
        {
            int nextIdx = (i + 1) % allPositions.Count;
            totalDistance += Vector3.Distance(allPositions[i], allPositions[nextIdx]);
        }
        if (allPositions.Count < 2)
        {
            Debug.LogWarning("ConveyorLine produced too few spline points; skipping conveyor setup.");
            return;
        }

        float avgSegmentLength = totalDistance / allPositions.Count;

        List<SplinePoint> points = new();

        for (int i = 0; i < allPositions.Count; i++)
        {
            Vector3 curr = allPositions[i];
            Vector3 next = allPositions[(i + 1) % allPositions.Count];

            points.Add(CreatePoint(curr));

            float distance = Vector3.Distance(curr, next);
            int numMidPoints = Mathf.RoundToInt(distance / avgSegmentLength) - 1;

            for (int j = 1; j <= numMidPoints; j++)
            {
                float t = (float)j / (numMidPoints + 1);
                points.Add(CreatePoint(Vector3.Lerp(curr, next, t)));
            }
        }

        ConveyorController.Instance.SplineComputer.SetPoints(points.ToArray());
        ConveyorController.Instance.SplineComputer.Close();
        ConveyorController.Instance.SplineComputer.RebuildImmediate(true, true);
        ConveyorController.Instance.SetupFromSpline();
    }

    private static List<Vector2Int> FilterInBoundsDistinct(List<Vector2Int> cells, int columns, int rows)
    {
        List<Vector2Int> result = new();
        HashSet<Vector2Int> seen = new();

        if (cells == null) return result;

        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int cell = cells[i];
            if (cell.x < 0 || cell.x >= columns || cell.y < 0 || cell.y >= rows) continue;
            if (!seen.Add(cell)) continue;
            result.Add(cell);
        }

        return result;
    }

    private static bool IsClosedNeighborLoop(List<Vector2Int> cells)
    {
        if (cells == null || cells.Count < 3) return false;

        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int a = cells[i];
            Vector2Int b = cells[(i + 1) % cells.Count];
            Vector2Int d = b - a;
            int ax = Mathf.Abs(d.x);
            int ay = Mathf.Abs(d.y);
            if (Mathf.Max(ax, ay) != 1) return false;
        }

        return true;
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
                GridCell cell = GetCellAt(curr);
                CubeLine cube = Instantiate(
                    _cubePrefab,
                    cell.transform.position,
                    Quaternion.identity,
                    lineColor.transform
                );

                cube.SetColor(line.Color);
                cell.CubeOnCell = cube;
                cube.Cell = cell;
                if (i == last)
                {
                    cube.SetType(CubeType.Head);

                    Direction dir = DirFromDelta(curr - prev.Value);
                    float yaw = GetYawFromDirection(dir);

                    cube.transform.localRotation = Quaternion.Euler(0f, yaw + 180, 0f);
                }

                else if (prev.HasValue && next.HasValue && IsCorner(prev.Value, curr, next.Value))
                {
                    cube.SetType(CubeType.Corner);

                    Direction inDir = DirFromDelta(curr - prev.Value);
                    Direction outDir = DirFromDelta(next.Value - curr);

                    float yaw = GetCornerYaw(inDir, outDir);
                    cube.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                }
                else
                {
                    cube.SetType(CubeType.Normal);
                }
                cube.Line = lineColor;
                lineColor.Cubes.Add(cube);
            }
        }
    }

    public GridCell FindConveyorCell(Vector2Int prev, Vector2Int curr)
    {
        Vector2Int dir = curr - prev;
        Vector2Int p = curr + dir;

        int w = Cells.GetLength(0);
        int h = Cells.GetLength(1);

        while (p.x >= 0 && p.x < w && p.y >= 0 && p.y < h)
        {
            GridCell c = GetCellAt(p);
            if (c != null && c.CellType == GridCellType.Conveyor)
                return c;

            p += dir;
        }
        return null;
    }
    public GridCell FindOccupiedCell(Vector2Int prev, Vector2Int curr)
    {
        Vector2Int dir = curr - prev;
        Vector2Int p = curr + dir;

        int w = Cells.GetLength(0);
        int h = Cells.GetLength(1);

        while (p.x >= 0 && p.x < w && p.y >= 0 && p.y < h)
        {
            GridCell c = GetCellAt(p);
            if (c != null && c.IsOccupied)
                return c;

            p += dir;
        }
        return null;
    }

    private void SetupGrid()
    {
        int rows = _currentConfig.Rows;
        int columns = _currentConfig.Columns;
        Vector2 spacing = _spacing;

        Cells = new GridCell[columns, rows];

        int expectedChildCount = rows * columns;
        GridCell[] gridCells = new GridCell[expectedChildCount];
        for (int i = 0; i < expectedChildCount; i++)
        {
            GridCell cell = Instantiate(_cellPrefab, transform);
            gridCells[i] = cell;
        }

        Vector2 offset = new Vector2(
            (columns - 1) * spacing.x / 2f,
            (rows - 1) * spacing.y / 2f
        );

        int index = 0;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                GridCell cell = gridCells[index++];
                Transform child = cell.transform;

                Vector3 pos = new Vector3(
                    col * spacing.x - offset.x,
                    0f,
                    row * spacing.y - offset.y
                );
                child.localPosition = pos;

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

    public GridCell GetCellAt(Vector2Int pos)
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
        RefreshAllHeadHighlights();
        // SetupCamera();
        GameManagerInGame.Instance.SetState(GameStateInGame.Playing);
    }
}
