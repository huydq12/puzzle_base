using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using Sirenix.OdinInspector;


public class Line : MonoBehaviour
{
    [ReadOnly] public ObjectColor Color;

    [Header("Cubes (0 = tail, last = head)")]
    [ReadOnly] public List<CubeLine> Cubes;

    [SerializeField] private float _moveSpeed;


    private float _cellDistance;
    private bool _isMoving;
    private bool _isReverting;

    private Vector2Int _gridDir;
    private int _totalSteps;
    private int _remainingSteps;
    private Stack<List<GridCell>> _history = new();
    private List<CubeLine> _pendingDetach = new();
    private int _reservedConveyorBaseIndex = -1;
    private bool _enqueueFailedThisStep;
    private bool _willDefinitelyRevert;
    private GridCell _targetConveyorCell;

    private bool _reuseGridDirNextMove;

    private bool _waitingForConveyorEnter;
    private Coroutine _waitConveyorRoutine;

    public void MoveLine()
    {
        if (_isMoving || _isReverting) return;

        _history.Clear();
        _reservedConveyorBaseIndex = -1;
        _enqueueFailedThisStep = false;
        _willDefinitelyRevert = false;
        _targetConveyorCell = null;
        _waitingForConveyorEnter = false;

        if (_waitConveyorRoutine != null)
        {
            StopCoroutine(_waitConveyorRoutine);
            _waitConveyorRoutine = null;
        }

        if (Cubes == null || Cubes.Count == 0)
            return;

        // Xác định hướng
        if (!_reuseGridDirNextMove || _gridDir == Vector2Int.zero)
        {
            if (Cubes.Count < 2)
                return;

            _cellDistance = Vector3.Distance(Cubes[0].transform.position, Cubes[1].transform.position);
            Vector2Int p0 = Cubes[^2].Cell.Position;
            Vector2Int p1 = Cubes[^1].Cell.Position;
            _gridDir = Board.Instance.NormalizeGridDir(p1 - p0);
        }
        _reuseGridDirNextMove = false;

        Vector2Int curr = Cubes[^1].Cell.Position;
        Vector2Int prev = curr - _gridDir;

        GridCell conveyorCell = Board.Instance.FindConveyorCell(prev, curr);
        GridCell occupiedCell = Board.Instance.FindOccupiedCell(prev, curr);

        if (conveyorCell == null)
        {
            // Không có conveyor -> chắc chắn revert
            _willDefinitelyRevert = true;
            _targetConveyorCell = null;

            if (occupiedCell != null)
            {
                int distToObstacle = Board.Instance.GetManhattanDistance(curr, occupiedCell.Position) - 1;

                if (distToObstacle <= 0)
                {
                    // Chạm vật cản ngay lập tức
                    foreach (var cube in Cubes)
                    {
                        cube.ShowWarning();
                    }
                    for (int i = 0; i < Cubes.Count - 1; i++)
                    {
                        Cubes[i].SetTempType(CubeType.Normal);
                    }
                    StartRevert();
                    return;
                }

                // Di chuyển đến trước vật cản rồi revert
                for (int i = 0; i < Cubes.Count - 1; i++)
                {
                    Cubes[i].SetTempType(CubeType.Normal);
                }
                _totalSteps = distToObstacle;
                _remainingSteps = _totalSteps;
            }
            else
            {
                // Không có conveyor và không có vật cản (đi ra ngoài biên)
                StartRevert();
                return;
            }
        }
        else
        {
            // Có conveyor
            int distToConveyor = Board.Instance.GetManhattanDistance(curr, conveyorCell.Position) - 1;
            _targetConveyorCell = conveyorCell;

            if (occupiedCell != null)
            {
                int distToObstacle = Board.Instance.GetManhattanDistance(curr, occupiedCell.Position) - 1;

                // Vật cản nằm TRƯỚC conveyor -> chắc chắn bị chặn
                if (distToObstacle < distToConveyor)
                {
                    _willDefinitelyRevert = true;
                    _targetConveyorCell = null;

                    if (distToObstacle <= 0)
                    {
                        // Chạm vật cản ngay lập tức
                        foreach (var cube in Cubes)
                        {
                            cube.ShowWarning();
                        }
                        for (int i = 0; i < Cubes.Count - 1; i++)
                        {
                            Cubes[i].SetTempType(CubeType.Normal);
                        }
                        StartRevert();
                        return;
                    }

                    // Di chuyển đến trước vật cản rồi revert
                    for (int i = 0; i < Cubes.Count - 1; i++)
                    {
                        Cubes[i].SetTempType(CubeType.Normal);
                    }
                    _totalSteps = distToObstacle;
                    _remainingSteps = _totalSteps;
                }
                else
                {
                    // Conveyor nằm trước vật cản -> sẽ lên conveyor
                    _willDefinitelyRevert = false;
                    for (int i = 0; i < Cubes.Count; i++)
                    {
                        Cubes[i].SetTempType(CubeType.Normal);
                    }
                    _totalSteps = distToConveyor;
                    _remainingSteps = _totalSteps;
                }
            }
            else
            {
                // Không có vật cản, đường thông đến conveyor
                _willDefinitelyRevert = false;
                for (int i = 0; i < Cubes.Count; i++)
                {
                    Cubes[i].SetTempType(CubeType.Normal);
                }
                _totalSteps = distToConveyor;
                _remainingSteps = _totalSteps;
            }
        }

        // Bắt đầu di chuyển theo kế hoạch
        _isMoving = true;
        StepForward();
    }

    private void StepForward()
    {
        if (_waitingForConveyorEnter)
            return;

        if (_remainingSteps <= 0)
        {
            // Đã đến đích
            if (_willDefinitelyRevert)
            {
                // Kế hoạch là revert
                _isMoving = false;
                foreach (var cube in Cubes)
                {
                    cube.ShowWarning();
                }
                StartRevert();
                return;
            }
            else
            {
                // Kế hoạch là lên conveyor
                if (TryStartWaitingForConveyorEnter())
                    return;

                _isMoving = false;
                return;
            }
        }

        if (!_willDefinitelyRevert && !TryReserveConveyorBaseIfNeeded())
        {
            // Không reserve được conveyor -> chuyển sang revert
            _isMoving = false;
            _willDefinitelyRevert = true;
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

        GridCell[] targets = new GridCell[Cubes.Count];

        for (int i = 0; i < Cubes.Count; i++)
        {
            if (i == Cubes.Count - 1)
            {
                Vector2Int next = Cubes[i].Cell.Position + _gridDir;
                targets[i] = Board.Instance.GetCellAt(next);
            }
            else
            {
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
                    // Giữ trạng thái board ổn định khi tween: forward chỉ clear cell của tail.
                    if (!_willDefinitelyRevert && cube == Cubes[0])
                    {
                        if (from != null && from.CubeOnCell == cube)
                            from.CubeOnCell = null;
                    }
                })
                .OnComplete(() =>
                {
                    cube.Cell = to;

                    finished++;
                    if (finished == expected)
                    {
                        if (_enqueueFailedThisStep)
                        {
                            _isMoving = false;
                            _willDefinitelyRevert = true;
                            StartRevert();
                            return;
                        }

                        // Chỉ commit occupancy nếu không chắc chắn sẽ revert
                        if (!_willDefinitelyRevert)
                        {
                            for (int j = 0; j < Cubes.Count; j++)
                            {
                                CubeLine c = Cubes[j];
                                if (c != null && c.Cell != null)
                                    c.Cell.CubeOnCell = c;
                            }
                            Board.Instance.RefreshAllHeadHighlights();
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
            _pendingDetach[i].transform.SetParent(Board.Instance.transform, true);
        }
        _pendingDetach.Clear();
    }

    private bool TryReserveConveyorBaseIfNeeded()
    {
        if (_reservedConveyorBaseIndex >= 0) return true;
        if (_targetConveyorCell == null) return true;
        if (ConveyorController.Instance == null) return false;

        int startIndex = ConveyorController.Instance.GetInsertIndexForWorldPosition(_targetConveyorCell.transform.position);
        if (startIndex < 0) return false;

        _reservedConveyorBaseIndex = startIndex;
        return true;
    }

    private bool TryStartWaitingForConveyorEnter()
    {
        if (_waitingForConveyorEnter) return true;
        if (_targetConveyorCell == null) return false;
        if (Cubes == null || Cubes.Count == 0) return false;
        if (ConveyorController.Instance == null) return false;

        CubeLine head = Cubes[^1];
        if (head == null || head.Cell == null) return false;

        int insertIndex = _reservedConveyorBaseIndex;
        if (insertIndex < 0)
        {
            insertIndex = ConveyorController.Instance.GetInsertIndexForWorldPosition(_targetConveyorCell.transform.position);
            if (insertIndex < 0) return false;
        }

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
        head.transform.DOKill();

        if (head != null && head.Cell != null && head.Cell.CubeOnCell == head)
        {
            head.Cell.CubeOnCell = null;
        }

        if (head != null)
            head.Cell = null;

        if (head != null && !_pendingDetach.Contains(head))
            _pendingDetach.Add(head);
        _waitingForConveyorEnter = false;

        FlushPendingDetach();

        // Sau khi head lên conveyor, kiểm tra còn cubes không
        if (Cubes == null || Cubes.Count == 0)
        {
            _isMoving = false;
            Board.Instance.RefreshAllHeadHighlights();
            return;
        }

        // Còn cubes thì tiếp tục MoveLine cho phần còn lại
        _isMoving = false;
        _reuseGridDirNextMove = true;
        MoveLine();
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
            _willDefinitelyRevert = false;
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
                    // Giữ trạng thái board ổn định khi tween: revert chỉ clear cell của head.
                    if (cube == Cubes[^1])
                    {
                        if (from != null && from.CubeOnCell == cube)
                            from.CubeOnCell = null;
                    }
                })
                .OnComplete(() =>
                {
                    cube.Cell = to;

                    finished++;
                    if (finished == Cubes.Count)
                    {
                        for (int j = 0; j < Cubes.Count; j++)
                        {
                            CubeLine c = Cubes[j];
                            if (c != null && c.Cell != null)
                                c.Cell.CubeOnCell = c;
                        }
                        Board.Instance.RefreshAllHeadHighlights();
                        StepBackward();
                    }
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

    private void OnLineReverted()
    {
        for (int i = 0; i < Cubes.Count - 1; i++)
        {
            Cubes[i].RevertType();
        }
    }
}