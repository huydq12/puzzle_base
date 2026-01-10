using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public enum FollowMode
{
    Forward,
    Backward
}

public class Line : MonoBehaviour
{
    [ReadOnly] public ObjectColor Color;

    [Header("Cubes (0 = tail, last = head)")]
    [ReadOnly] public List<CubeLine> Cubes;

    [SerializeField] private float _moveSpeed;


    private float _cellDistance;
    private bool _isMoving;
    private bool _isReverting;
    private bool _loggedNoSpace;

    private Vector2Int _gridDir;
    private int _remainingSteps;
    private Stack<List<GridCell>> _history = new();
    private readonly List<CubeLine> _pendingDetach = new();
    private int _nextConveyorBaseIndex = -1;
    private int _reservedConveyorBaseIndex = -1;
    private bool _enqueueFailedThisStep;
    private bool _forceRevertAfterSegment;


    public void Initialize()
    {
        _cellDistance = Vector3.Distance(
            Cubes[0].transform.position,
            Cubes[1].transform.position
        );

        Vector2Int p0 = Cubes[^2].Cell.Position;
        Vector2Int p1 = Cubes[^1].Cell.Position;
        _gridDir = p1 - p0;
    }

    public void MoveLine()
    {
        if (_isMoving || _isReverting) return;

        _isMoving = true;
        _history.Clear();
        _loggedNoSpace = false;
        _nextConveyorBaseIndex = -1;
        _reservedConveyorBaseIndex = -1;
        _enqueueFailedThisStep = false;
        _forceRevertAfterSegment = false;

        if (!PrepareNextSegment())
            return;

        StepForward();
    }

    private bool PrepareNextSegment()
    {
        FlushPendingDetach();

        if (Cubes == null || Cubes.Count == 0)
        {
            _isMoving = false;
            _nextConveyorBaseIndex = -1;
            _reservedConveyorBaseIndex = -1;
            OnLineReverted();
            return false;
        }

        if (Cubes.Count >= 2)
        {
            Vector2Int p0 = Cubes[^2].Cell.Position;
            Vector2Int p1 = Cubes[^1].Cell.Position;
            _gridDir = p1 - p0;
        }

        if (Cubes.Count < 1)
        {
            _isMoving = false;
            OnLineReverted();
            return false;
        }

        if (Cubes.Count == 1)
        {
            GridCell only = Cubes[0].Cell;
            Vector2Int next = only.Position + _gridDir;
            GridCell nextCell = Board.Instance.GetCellAt(next);
            if (nextCell == null || (nextCell.IsOccupied && nextCell.CellType != GridCellType.Conveyor))
            {
                _isMoving = false;
                StartRevert();
                return false;
            }

            _remainingSteps = nextCell.CellType == GridCellType.Conveyor ? 1 : 0;
            if (_remainingSteps <= 0)
            {
                _isMoving = false;
                StartRevert();
                return false;
            }
            return true;
        }

        _forceRevertAfterSegment = false;

        Vector2Int prev = Cubes[^2].Cell.Position;
        Vector2Int curr = Cubes[^1].Cell.Position;

        GridCell occupiedCell = FindOccupiedCell(prev, curr);
        if (occupiedCell != null)
        {
            _remainingSteps = GetManhattanDistance(curr, occupiedCell.Position) - 1;
            _forceRevertAfterSegment = true;
        }
        else
        {
            GridCell conveyorCell = FindConveyorCell(prev, curr);
            if (conveyorCell == null)
            {
                _isMoving = false;
                StartRevert();
                return false;
            }
            else
            {
                for (int i = 0; i < Cubes.Count; i++)
                {
                    Cubes[i].SetTempType(CubeType.Normal);
                }
            }

            _remainingSteps = GetManhattanDistance(curr, conveyorCell.Position);
        }

        if (_remainingSteps <= 0)
        {
            _isMoving = false;
            StartRevert();
            return false;
        }
        for (int i = 0; i < Cubes.Count - 1; i++)
        {
            Cubes[i].SetTempType(CubeType.Normal);
        }

        return true;
    }

    private void StepForward()
    {
        if (_remainingSteps <= 0)
        {
            if (_forceRevertAfterSegment)
            {
                _isMoving = false;
                _forceRevertAfterSegment = false;
                StartRevert();
                return;
            }

            if (PrepareNextSegment())
            {
                StepForward();
                return;
            }
            return;
        }

        if (!TryReserveConveyorBaseIfNeeded())
        {
            _isMoving = false;
            StartRevert();
            return;
        }

        SaveSnapshot();

        DoStepForward(() =>
        {
            _remainingSteps--;
            StepForward();
        });
    }

    private void DoStepForward(System.Action onComplete)
    {
        float duration = _cellDistance / _moveSpeed;
        int finished = 0;
        int expected = Cubes.Count;

        // target cell cho từng cube
        GridCell[] targets = new GridCell[Cubes.Count];

        for (int i = 0; i < Cubes.Count; i++)
        {
            if (i == Cubes.Count - 1)
            {
                // head → cell tiếp theo
                Vector2Int next = Cubes[i].Cell.Position + _gridDir;
                targets[i] = Board.Instance.GetCellAt(next);
            }
            else
            {
                // cube → cell của cube trước
                targets[i] = Cubes[i + 1].Cell;
            }
        }

        for (int i = 0; i < Cubes.Count; i++)
        {
            CubeLine cube = Cubes[i];
            GridCell from = cube.Cell;
            GridCell to = targets[i];

            Vector3 targetPos = to.transform.position;
            if (to.CellType == GridCellType.Conveyor && ConveyorController.Instance != null)
            {
                Vector3 moveDir = Vector3.zero;
                if (from != null)
                {
                    moveDir = targetPos - from.transform.position;
                    moveDir.y = 0f;
                }
                if (moveDir.sqrMagnitude < 0.0001f)
                {
                    moveDir = new Vector3(_gridDir.x, 0f, _gridDir.y);
                }
                if (moveDir.sqrMagnitude < 0.0001f)
                    moveDir = Vector3.forward;
                moveDir.Normalize();

                // Offset theo chính hướng di chuyển của line
                targetPos += moveDir * ConveyorController.Instance.BaseOffsetAmount;
            }

            cube.transform.DOMove(targetPos, duration)
                .SetEase(Ease.Linear)
                .OnStart(() =>
                {
                    if (from != null && from.CubeOnCell == cube)
                        from.CubeOnCell = null;
                })
                .OnComplete(() =>
                {
                    cube.Cell = to;
                    to.CubeOnCell = cube;

                    if (to.CellType == GridCellType.Conveyor)
                    {
                        TryAssignToConveyorBase(cube);
                    }

                    finished++;
                    if (finished == expected)
                    {
                        if (_enqueueFailedThisStep)
                        {
                            _isMoving = false;
                            StartRevert();
                            return;
                        }

                        FlushPendingDetach();
                        onComplete?.Invoke();
                    }
                });
        }
    }

    private void FlushPendingDetach()
    {
        if (_pendingDetach.Count == 0) return;
        for (int i = 0; i < _pendingDetach.Count; i++)
        {
            Cubes.Remove(_pendingDetach[i]);
        }
        _pendingDetach.Clear();
    }

    private bool TryReserveConveyorBaseIfNeeded()
    {
        if (_reservedConveyorBaseIndex >= 0) return true;
        if (Cubes == null || Cubes.Count == 0) return true;
        if (ConveyorController.Instance == null || ConveyorController.Instance.Bases == null) return true;

        CubeLine head = Cubes[^1];
        if (head == null || head.Cell == null) return true;

        Vector2Int nextPos = head.Cell.Position + _gridDir;
        GridCell nextCell = Board.Instance.GetCellAt(nextPos);
        if (nextCell == null || nextCell.CellType != GridCellType.Conveyor) return true;

        int startIndex = ConveyorController.Instance.GetInsertIndexForWorldPosition(nextCell.transform.position);
        if (startIndex < 0) return true;

        _reservedConveyorBaseIndex = startIndex;
        return true;
    }

    private void TryAssignToConveyorBase(CubeLine cube)
    {
        if (cube == null) return;

        if (ConveyorController.Instance == null || ConveyorController.Instance.Bases == null)
            return;

        List<Base> bases = ConveyorController.Instance.Bases;
        if (bases.Count == 0) return;

        int targetIndex = -1;
        if (_reservedConveyorBaseIndex >= 0)
        {
            targetIndex = _reservedConveyorBaseIndex;
            _nextConveyorBaseIndex = (_reservedConveyorBaseIndex - 1 + bases.Count) % bases.Count;
            _reservedConveyorBaseIndex = -1;
        }
        else if (_nextConveyorBaseIndex >= 0)
        {
            targetIndex = _nextConveyorBaseIndex;
            _nextConveyorBaseIndex = (_nextConveyorBaseIndex - 1 + bases.Count) % bases.Count;
        }
        else
        {
            return;
        }

        if (targetIndex < 0)
            return;

        if (!ConveyorController.Instance.TryEnqueueInsertAtIndex(targetIndex, cube))
        {
            _enqueueFailedThisStep = true;
            return;
        }

        Vector3 dir = new Vector3(_gridDir.x, 0f, _gridDir.y);
        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector3.forward;
        dir.Normalize();

        if (cube.Cell != null && cube.Cell.CubeOnCell == cube)
            cube.Cell.CubeOnCell = null;

        cube.Cell = null;

        if (!_pendingDetach.Contains(cube))
            _pendingDetach.Add(cube);
    }

    private void StartRevert()
    {
        if (_history.Count == 0)
        {
            OnLineReverted();
            return;
        }

        _isReverting = true;
        StepBackward();
    }

    private void StepBackward()
    {
        if (_history.Count == 0)
        {
            _isReverting = false;
            OnLineReverted();
            return;
        }

        List<GridCell> prev = _history.Pop();
        float duration = _cellDistance / _moveSpeed;
        int finished = 0;

        for (int i = 0; i < Cubes.Count; i++)
        {
            CubeLine cube = Cubes[i];
            GridCell from = cube.Cell;
            GridCell to = prev[i];

            cube.transform.DOMove(to.transform.position, duration)
                .SetEase(Ease.Linear)
                .OnStart(() =>
                {
                    if (from != null && from.CubeOnCell == cube)
                        from.CubeOnCell = null;
                })
                .OnComplete(() =>
                {
                    cube.Cell = to;
                    to.CubeOnCell = cube;

                    finished++;
                    if (finished == Cubes.Count)
                        StepBackward();
                });
        }
    }

    private void SaveSnapshot()
    {
        List<GridCell> snap = new();
        foreach (var c in Cubes)
            snap.Add(c.Cell);

        _history.Push(snap);
    }

    private GridCell FindConveyorCell(Vector2Int prev, Vector2Int curr)
    {
        Vector2Int dir = curr - prev;
        Vector2Int p = curr + dir;

        int w = Board.Instance.Cells.GetLength(0);
        int h = Board.Instance.Cells.GetLength(1);

        while (p.x >= 0 && p.x < w && p.y >= 0 && p.y < h)
        {
            GridCell c = Board.Instance.GetCellAt(p);
            if (c != null && c.CellType == GridCellType.Conveyor)
                return c;

            p += dir;
        }
        return null;
    }

    private static int GetManhattanDistance(Vector2Int a, Vector2Int b)
    {
        Vector2Int d = b - a;
        return Mathf.Abs(d.x) + Mathf.Abs(d.y);
    }
    private GridCell FindOccupiedCell(Vector2Int prev, Vector2Int curr)
    {
        Vector2Int dir = curr - prev;
        Vector2Int p = curr + dir;

        int w = Board.Instance.Cells.GetLength(0);
        int h = Board.Instance.Cells.GetLength(1);

        while (p.x >= 0 && p.x < w && p.y >= 0 && p.y < h)
        {
            GridCell c = Board.Instance.GetCellAt(p);
            if (c != null && c.IsOccupied)
                return c;

            p += dir;
        }
        return null;
    }

    private void OnLineReverted()
    {
        for (int i = 0; i < Cubes.Count - 1; i++)
        {
            Cubes[i].RevertType();
        }
    }
}
