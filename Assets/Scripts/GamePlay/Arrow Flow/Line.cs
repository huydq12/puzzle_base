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
    private int _reservedConveyorBaseIndex = -1;
    private bool _enqueueFailedThisStep;
    private bool _forceRevertAfterSegment;

    private bool _waitingForConveyorEnter;
    private Coroutine _waitConveyorRoutine;


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
        _reservedConveyorBaseIndex = -1;
        _enqueueFailedThisStep = false;
        _forceRevertAfterSegment = false;
        _waitingForConveyorEnter = false;

        if (_waitConveyorRoutine != null)
        {
            StopCoroutine(_waitConveyorRoutine);
            _waitConveyorRoutine = null;
        }

        if (!PrepareNextSegment())
            return;

        StepForward();
    }

   private bool PrepareNextSegment()
{
    FlushPendingDetach();
    _forceRevertAfterSegment = false;

    if (Cubes == null || Cubes.Count == 0)
    {
        _isMoving = false;
        OnLineReverted();
        return false;
    }

    // Cập nhật hướng dựa trên 2 cube cuối (nếu có)
    if (Cubes.Count >= 2)
    {
        Vector2Int p0 = Cubes[^2].Cell.Position;
        Vector2Int p1 = Cubes[^1].Cell.Position;
        _gridDir = p1 - p0;
    }
    // Nếu chỉ có 1 cube, _gridDir sẽ giữ nguyên giá trị từ lần Initialize hoặc lần di chuyển trước

    Vector2Int curr = Cubes[^1].Cell.Position;
    Vector2Int prev = curr - _gridDir; // Giả lập ô phía sau để hàm Find hoạt động đồng nhất

    // 1. Kiểm tra xem có đâm vào vật cản nào không
    GridCell occupiedCell = FindOccupiedCell(prev, curr);
    
    // 2. Kiểm tra xem có đường dẫn tới băng chuyền không
    GridCell conveyorCell = FindConveyorCell(prev, curr);

    if (occupiedCell != null)
    {
        int distToObstacle = GetManhattanDistance(curr, occupiedCell.Position) - 1;
        
        // Nếu có băng chuyền TRƯỚC vật cản, ưu tiên đi đến băng chuyền
        if (conveyorCell != null)
        {
            int distToConveyor = GetManhattanDistance(curr, conveyorCell.Position) - 1;
            if (distToConveyor <= distToObstacle)
            {
                _remainingSteps = distToConveyor;
                return true; 
            }
        }

        // Nếu vật cản ở ngay trước mặt (dist = 0) hoặc không có đường đi hợp lệ
        if (distToObstacle <= 0)
        {
            _isMoving = false;
            StartRevert();
            return false;
        }

        _remainingSteps = distToObstacle;
        _forceRevertAfterSegment = true;
        return true;
    }
    else if (conveyorCell != null)
    {
        // Không có vật cản, nhưng có băng chuyền
        _remainingSteps = GetManhattanDistance(curr, conveyorCell.Position) - 1;
        return true;
    }
    else
    {
        // Không vật cản, không băng chuyền -> Revert luôn
        _isMoving = false;
        StartRevert();
        return false;
    }
}

    private void StepForward()
    {
        if (_waitingForConveyorEnter)
            return;

        if (_remainingSteps <= 0)
        {
            if (_forceRevertAfterSegment)
            {
                _isMoving = false;
                _forceRevertAfterSegment = false;
                StartRevert();
                return;
            }

            if (TryStartWaitingForConveyorEnter())
                return;

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
        if (ConveyorController.Instance == null) return true;

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
        if (ConveyorController.Instance == null) return;

        int targetIndex = _reservedConveyorBaseIndex;
        if (targetIndex < 0) return;

        if (!ConveyorController.Instance.TryEnqueueInsertAtIndex(targetIndex, cube))
        {
            _enqueueFailedThisStep = true;
            return;
        }

        _reservedConveyorBaseIndex = -1;

        if (cube.Cell != null && cube.Cell.CubeOnCell == cube)
            cube.Cell.CubeOnCell = null;

        cube.Cell = null;

        if (!_pendingDetach.Contains(cube))
            _pendingDetach.Add(cube);
    }

    private bool TryStartWaitingForConveyorEnter()
    {
        if (_waitingForConveyorEnter) return true;
        if (Cubes == null || Cubes.Count == 0) return false;
        if (ConveyorController.Instance == null) return false;

        CubeLine head = Cubes[^1];
        if (head == null || head.Cell == null) return false;

        Vector2Int nextPos = head.Cell.Position + _gridDir;
        GridCell nextCell = Board.Instance.GetCellAt(nextPos);
        if (nextCell == null || nextCell.CellType != GridCellType.Conveyor) return false;

        int insertIndex = ConveyorController.Instance.GetInsertIndexForWorldPosition(nextCell.transform.position);
        if (insertIndex < 0) return false;

        _waitingForConveyorEnter = true;
        _isMoving = true;
        _reservedConveyorBaseIndex = insertIndex;

        if (_waitConveyorRoutine != null)
            StopCoroutine(_waitConveyorRoutine);
        _waitConveyorRoutine = StartCoroutine(WaitAndRequestConveyorEnter(head, insertIndex));
        return true;
    }

    private System.Collections.IEnumerator WaitAndRequestConveyorEnter(CubeLine head, int insertIndex)
    {
        while (true)
        {
            if (!_waitingForConveyorEnter)
                yield break;
            if (head == null)
                yield break;
            if (ConveyorController.Instance == null)
            {
                yield return null;
                continue;
            }

            bool queued = ConveyorController.Instance.TryRequestEnter(head, insertIndex, () => OnHeadInsertedToConveyor(head));
            if (queued)
                yield break;

            yield return null;
        }
    }

    private void OnHeadInsertedToConveyor(CubeLine head)
    {
        if (head != null && head.Cell != null && head.Cell.CubeOnCell == head)
            head.Cell.CubeOnCell = null;

        if (head != null)
            head.Cell = null;

        if (head != null && !_pendingDetach.Contains(head))
            _pendingDetach.Add(head);

        _reservedConveyorBaseIndex = -1;
        _waitingForConveyorEnter = false;

        FlushPendingDetach();

        if (_isReverting) return;
        if (!_isMoving) _isMoving = true;

        if (PrepareNextSegment())
            StepForward();
        else
            _isMoving = false;
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