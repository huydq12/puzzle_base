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

    private Vector2Int _gridDir;
    private int _remainingSteps;
    private Stack<List<GridCell>> _history = new();
    private bool _stopOnConveyor;

    private void SetupConveyorChain()
    {
        if (Cubes == null || Cubes.Count == 0) return;

        // Cubes: 0 = tail, last = head
        for (int i = 0; i < Cubes.Count; i++)
        {
            if (Cubes[i] != null) Cubes[i].Line = this;
        }

        for (int i = Cubes.Count - 1; i >= 0; i--)
        {
            CubeLine cube = Cubes[i];
            if (cube == null) continue;

            bool isHead = i == Cubes.Count - 1;
            cube.isEngine = isHead;
            cube.offset = isHead ? 0f : 1f;

            cube.front = isHead ? null : Cubes[i + 1];
            cube.Back = i > 0 ? Cubes[i - 1] : null;
        }
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
    }

    public void MoveLine()
    {
        if (_isMoving || _isReverting) return;

        _isMoving = true;
        _history.Clear();

        GridCell blockCell = FindBlockCell(
            Cubes[^2].Cell.Position,
            Cubes[^1].Cell.Position
        );
        _stopOnConveyor = blockCell != null && blockCell.CellType == GridCellType.Conveyor;
        if (_stopOnConveyor) SetupConveyorChain();
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
        else
        {
            //??
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


    private void DoStepForward(System.Action onComplete)
    {
        float duration = _cellDistance / _moveSpeed;
        int finished = 0;
        int cubesCount = Cubes.Count;

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

            bool enteringConveyor = (i == Cubes.Count - 1) && to != null && to.CellType == GridCellType.Conveyor;

            if (enteringConveyor)
            {
                cube.transform.DOMove(to.transform.position, duration)
                    .SetEase(Ease.Linear)
                    .OnStart(() =>
                    {
                        if (from != null && from.CubeOnCell == cube)
                            from.CubeOnCell = null;
                    })
                    .OnComplete(() =>
                    {
                        cube.Cell = null;

                        Cubes.Remove(cube);

                        if (cube.front != null)
                        {
                            cube.front.Back = null;
                            cube.front = null;
                        }
                        if (cube.Back != null)
                        {
                            cube.Back.front = null;
                            cube.Back = null;
                        }

                        ConveyorController.Instance.AddCube(cube, to.transform.position, duration);

                        finished++;
                        if (finished == cubesCount)
                            onComplete?.Invoke();
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
                        onComplete?.Invoke();
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
