using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using System.Linq;

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

    private Vector2Int _gridDir;
    private int _remainingSteps;
    private Stack<List<GridCell>> _history = new();
    private bool _stopOnConveyor;
    
    private Base _lastAssignedBase;

    private Base PrepareBaseForEnteringConveyor(CubeLine cube)
    {
        Base targetBase = null;

        if (_lastAssignedBase == null)
        {
            Base closest = cube.FindClosestBaseForConveyorInsertion();
            if (closest != null)
            {
                targetBase = ConveyorController.Instance.GetBaseAfter(closest, this);
            }
        }
        else
        {
            targetBase = FindNextBaseOnConveyor(_lastAssignedBase);
        }

        if (targetBase == null) return null;

        if (targetBase.IsOccupied)
        {
            ConveyorController.Instance.DisplaceBackwardUntilEmpty(this, targetBase);
            if (targetBase.IsOccupied)
            {
                Debug.LogWarning("Không thể chen vào vì base vẫn đang bị chiếm sau khi displace");
                return null;
            }
        }

        return targetBase;
    }

    public void Initialize()
    {
        _cellDistance = Vector3.Distance(
            Cubes[0].transform.position,
            Cubes[1].transform.position
        );

        Vector2Int p0 = Cubes[^2].Cell.Position;
        Vector2Int p1 = Cubes[^1].Cell.Position;
        _gridDir = p1 - p0;
        
        _lastAssignedBase = null;
    }

    public void MoveLine()
    {
        if (_isMoving || _isReverting) return;

        _isMoving = true;
        _history.Clear();
        _lastAssignedBase = null;

        GridCell blockCell = FindBlockCell(
            Cubes[^2].Cell.Position,
            Cubes[^1].Cell.Position
        );
        _stopOnConveyor = blockCell != null && blockCell.CellType == GridCellType.Conveyor;
        if (blockCell != null)
        {
            Vector2Int head = Cubes[^1].Cell.Position;
            Vector2Int block = blockCell.Position;
            Vector2Int d = block - head;

            _remainingSteps = Mathf.Abs(d.x) + Mathf.Abs(d.y) - (_stopOnConveyor ? 0 : 1);
            for (int i = 0; i < Cubes.Count - (_stopOnConveyor ? 0 : 1); i++)
            {
                Cubes[i].SetTempType(CubeType.Normal);
            }
            Debug.Log($"Line will move {_remainingSteps} steps");
        }

        StepForward();
    }

    private void StepForward()
    {
        if (_remainingSteps <= 0)
        {
            _isMoving = false;
            if (!_stopOnConveyor)
            {
                StartRevert();
                return;
            }

            if (Cubes.Count == 0) return;

            if (!TryContinueTowardsConveyor()) return;

            _isMoving = true;
            StepForward();
            return;
        }

        SaveSnapshot();

        DoStepForward(() =>
        {
            _remainingSteps--;
            StepForward();
        });
    }

    private void CheckBase(CubeLine cube)
    {
        Base targetBase = null;

        if (_lastAssignedBase == null)
        {
            Base closest = cube.FindClosestBaseForConveyorInsertion();
            if (closest != null)
            {
                targetBase = ConveyorController.Instance.GetBaseAfter(closest, this);
            }
        }
        else
        {
            targetBase = FindNextBaseOnConveyor(_lastAssignedBase);
        }

        if (targetBase != null)
        {
            // Nếu base đã bị chiếm, chen vào
            if (targetBase.IsOccupied)
            {
                // Remove 1 đoạn liên tiếp về phía trước cho tới khi gặp base trống,
                // sau đó mới cho cube của line chen vào.
                ConveyorController.Instance.DisplaceBackwardUntilEmpty(this, targetBase);

                // Nếu vì lý do nào đó base vẫn còn occupied, không assign để tránh ghi đè.
                if (targetBase.IsOccupied)
                {
                    Debug.LogWarning("Không thể chen vào vì base vẫn đang bị chiếm sau khi displace");
                    return;
                }
            }

            // Assign cube mới vào base
            targetBase.AssignCube(cube);
            _lastAssignedBase = targetBase;
        }
        else
        {
            Debug.LogWarning("Không tìm thấy base phù hợp trên conveyor");
        }
    }

    private Base FindNextBaseOnConveyor(Base currentBase)
    {
        var entries = ConveyorController.Instance.Entries;
        int currentIndex = entries.IndexOf(currentBase);

        if (currentIndex == -1)
        {
            Debug.LogError("Current base không có trong danh sách conveyor");
            return null;
        }

        // Tìm base TRƯỚC ĐÓ (có thể đã bị chiếm, nhưng không phải của line này)
        for (int offset = 1; offset < entries.Count; offset++)
        {
            int prevIndex = (currentIndex - offset + entries.Count) % entries.Count;
            Base prevBase = entries[prevIndex];
            
            // Bỏ qua base nếu đang chứa cube của chính line này
            if (prevBase.IsOccupied && prevBase.CubeOnBase != null && prevBase.CubeOnBase.Line == this)
            {
                continue;
            }
            
            // Trả về base đầu tiên tìm thấy (trống hoặc chứa cube của line khác)
            return prevBase;
        }

        return null;
    }

    private void DoStepForward(System.Action onComplete)
    {
        float duration = _cellDistance / _moveSpeed;
        int finished = 0;
        int cubesCount = Cubes.Count;
        int cubesEnteringConveyor = 0; // Đếm số cube sẽ lên conveyor

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

            bool enteringConveyor = (i == Cubes.Count - 1) && to != null && to.CellType == GridCellType.Conveyor;

            if (enteringConveyor)
            {
                cubesEnteringConveyor++;
                int currentEnteringCount = cubesEnteringConveyor;

                Base preparedBase = PrepareBaseForEnteringConveyor(cube);
                Vector3 enterTargetPos = preparedBase != null ? preparedBase.transform.position : to.transform.position;
                
                Tween tween = preparedBase != null
                    ? cube.transform.DOMove(preparedBase.transform.position, duration)
                    : cube.transform.DOMove(enterTargetPos, duration);

                tween
                    .SetEase(Ease.Linear)
                    .OnStart(() =>
                    {
                        if (from != null && from.CubeOnCell == cube)
                            from.CubeOnCell = null;

                        if (preparedBase != null)
                        {
                            cube.transform.SetParent(preparedBase.transform, true);
                        }
                    })
                    .OnComplete(() =>
                    {
                        cube.Cell = null;
                        Cubes.Remove(cube);

                        if (preparedBase != null)
                        {
                            preparedBase.CubeOnBase = cube;
                            cube.transform.localPosition = Vector3.zero;
                            _lastAssignedBase = preparedBase;
                        }
                        else
                        {
                            CheckBase(cube);
                        }

                        finished++;
                        
                        // Nếu đây là cube cuối cùng lên conveyor VÀ tất cả cubes đã finished
                        if (finished == cubesCount)
                        {
                            // Chỉ nối displaced cubes khi toàn bộ line đã lên hết conveyor
                            if (Cubes.Count == 0)
                            {
                                ConveyorController.Instance.ProcessDisplacedCubesForLine(this, _lastAssignedBase);
                            }
                            onComplete?.Invoke();
                        }
                    });
                continue;
            }

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
                    if (finished == cubesCount)
                    {
                        // Nếu không có cube nào lên conveyor, không process displaced ở đây
                        onComplete?.Invoke();
                    }
                });
        }
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

    private GridCell FindBlockCell(Vector2Int prev, Vector2Int curr)
    {
        Vector2Int dir = curr - prev;
        Vector2Int p = curr + dir;

        int w = Board.Instance.Cells.GetLength(0);
        int h = Board.Instance.Cells.GetLength(1);

        while (p.x >= 0 && p.x < w && p.y >= 0 && p.y < h)
        {
            GridCell c = Board.Instance.GetCellAt(p);
            if (c != null && (c.IsOccupied || c.CellType == GridCellType.Conveyor))
                return c;

            p += dir;
        }
        return null;
    }

    private GridCell FindBlockCellFromHead(Vector2Int head, Vector2Int dir)
    {
        Vector2Int p = head + dir;

        int w = Board.Instance.Cells.GetLength(0);
        int h = Board.Instance.Cells.GetLength(1);

        while (p.x >= 0 && p.x < w && p.y >= 0 && p.y < h)
        {
            GridCell c = Board.Instance.GetCellAt(p);
            if (c != null && (c.IsOccupied || c.CellType == GridCellType.Conveyor))
                return c;

            p += dir;
        }

        return null;
    }

    private bool TryContinueTowardsConveyor()
    {
        if (Cubes.Count == 0) return false;

        GridCell headCell = Cubes[^1].Cell;
        if (headCell == null) return false;

        GridCell blockCell = FindBlockCellFromHead(headCell.Position, _gridDir);
        if (blockCell == null)
        {
            _stopOnConveyor = false;
            return false;
        }

        if (blockCell.IsOccupied)
        {
            _stopOnConveyor = false;
            return false;
        }

        _stopOnConveyor = blockCell.CellType == GridCellType.Conveyor;
        Vector2Int d = blockCell.Position - headCell.Position;
        _remainingSteps = Mathf.Abs(d.x) + Mathf.Abs(d.y) - (_stopOnConveyor ? 0 : 1);
        return _remainingSteps > 0;
    }

    private void OnLineReverted()
    {
        for (int i = 0; i < Cubes.Count - 1; i++)
        {
            Cubes[i].RevertType();
        }
    }
}