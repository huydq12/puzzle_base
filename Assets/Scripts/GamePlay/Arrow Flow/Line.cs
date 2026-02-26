using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;


public class Line : MonoBehaviour
{
    [ReadOnly] public ObjectColor Color;
    [ReadOnly] public int Counter;
    [ReadOnly] public int RemainingCounter;
    [ReadOnly] public bool IsIceLine;
    [ReadOnly] public List<int> ElementTypes;

    [Header("Cubes (0 = tail, last = head)")]
    [ReadOnly] public List<CubeLine> Cubes;

    [SerializeField] private float _moveSpeed;
    [SerializeField] private TextMeshPro _counterText;
    [SerializeField] private Vector3 _counterTextOffset = new Vector3(0f, 0.4f, 0f);

    [SerializeField] private Bomb _bomb;


    private float _cellDistance;
    private bool _isMoving;
    private bool _isReverting;

    private Vector2Int _gridDir;
    private int _totalSteps;
    private int _remainingSteps;
    private readonly Stack<GridCell[]> _history = new();
    private readonly List<CubeLine> _pendingDetach = new();
    private int _reservedConveyorBaseIndex = -1;
    private bool _willDefinitelyRevert;
    private GridCell _targetConveyorCell;

    private bool _reuseGridDirNextMove;

    private bool _waitingForConveyorEnter;
    private Coroutine _waitConveyorRoutine;

    private CubeLine _blockingCube;
    private Tween _blockingCubeTiltTween;

    private GridCell[] _targetsBuffer;
    private bool _hasNotifiedConveyorEnter;
    public void Clear()
    {
        _isMoving = false;
        _isReverting = false;
        _waitingForConveyorEnter = false;
        _reuseGridDirNextMove = false;
        _hasNotifiedConveyorEnter = false;

        CancelConveyorWait();

        if (_blockingCubeTiltTween != null && _blockingCubeTiltTween.IsActive())
        {
            _blockingCubeTiltTween.Kill();
            _blockingCubeTiltTween = null;
        }

        _totalSteps = 0;
        _remainingSteps = 0;
        _gridDir = Vector2Int.zero;
        _cellDistance = 0f;

        _history.Clear();
        _pendingDetach.Clear();
        _targetsBuffer = null;

        _reservedConveyorBaseIndex = -1;
        _willDefinitelyRevert = false;
        _targetConveyorCell = null;
        _blockingCube = null;

        Cubes.Clear();
        if (ElementTypes != null)
            ElementTypes.Clear();
    }
    private void Awake()
    {
        if (_counterText == null)
            _counterText = GetComponentInChildren<TextMeshPro>(true);
        UpdateCounterText();
    }

    public void InitializeCounter(int counter)
    {
        Counter = Mathf.Max(0, counter);
        RemainingCounter = Counter;
        UpdateCounterText();
    }

    public void SetIsIceLine(bool isIceLine)
    {
        IsIceLine = isIceLine;
    }

    public void SetElementTypes(List<int> elementTypes, int expectedCount)
    {
        if (ElementTypes == null)
            ElementTypes = new List<int>(expectedCount);

        ElementTypes.Clear();

        if (elementTypes != null)
        {
            int count = Mathf.Min(elementTypes.Count, expectedCount);
            for (int i = 0; i < count; i++)
                ElementTypes.Add(elementTypes[i]);
        }

        while (ElementTypes.Count < expectedCount)
            ElementTypes.Add(0);
    }

    public void SetRemainingCounter(int remaining)
    {
        RemainingCounter = Mathf.Max(0, remaining);
        UpdateCounterText();
    }

    public void ConsumeIceStep(int amount)
    {
        if (!IsIceLine) return;
        if (Counter <= 0) return;
        if (RemainingCounter <= 0) return;

        int clamped = Mathf.Max(0, amount);
        if (clamped <= 0) return;

        int before = RemainingCounter;
        RemainingCounter = Mathf.Max(0, RemainingCounter - clamped);
        UpdateCounterText();

        if (RemainingCounter < before)
        {
            // Play ice effect every time the ice counter decreases.
            if (Cubes != null)
            {
                for (int i = 0; i < Cubes.Count; i++)
                {
                    CubeLine cube = Cubes[i];
                    if (cube == null) continue;
                    if (cube.ElementType != 2) continue;
                    cube.PlayEffectMelt();
                }
            }
        }

        if (RemainingCounter <= 0)
        {
            MeltIce();
        }
    }

    public void NotifyEnteredConveyor()
    {
        if (_hasNotifiedConveyorEnter) return;
        _hasNotifiedConveyorEnter = true;

        if (Board.Instance != null)
            Board.Instance.NotifyAnyLineEnteredConveyor();
    }

    public void MoveLine()
    {
        if (_isMoving || _isReverting) return;
        if (IsIceLine && RemainingCounter > 0) return;

        ResetMoveState();

        if (!TryGetMovePrerequisites(out Board board, out CubeLine head, out Vector2Int headPos))
            return;

        if (!TryComputeGridDirection(board))
            return;

        Vector2Int prev = headPos - _gridDir;

        GridCell conveyorCell = board.FindConveyorCell(prev, headPos);
        GridCell occupiedCell = board.FindOccupiedCell(prev, headPos);

        if (conveyorCell != null && board.IsConveyorCellBlockedByTunnel(conveyorCell.Position))
        {
            int distToTunnel = board.GetManhattanDistance(headPos, conveyorCell.Position);
            int distToOccupied = occupiedCell != null ? board.GetManhattanDistance(headPos, occupiedCell.Position) : int.MaxValue;
            if (distToTunnel <= distToOccupied)
                occupiedCell = conveyorCell;
            conveyorCell = null;
        }

        if (!TryBuildMovePlan(board, headPos, conveyorCell, occupiedCell))
            return;


        if (!_isMoving)
        {
            if (!_willDefinitelyRevert)
            {
                AudioManager.Instance.PlaySFX(SFXType.Select);
            }
        }
        _isMoving = true;

        StepForward();
    }

    public void RefreshCounterText()
    {
        UpdateCounterText();
        UpdateCounterTextPosition();
    }

    private void UpdateCounterText()
    {
        if (_counterText == null) return;
        bool hasElementType3 = false;
        if (Cubes != null)
        {
            for (int i = 0; i < Cubes.Count; i++)
            {
                CubeLine cube = Cubes[i];
                if (cube == null) continue;
                if (cube.ElementType == 3)
                {
                    hasElementType3 = true;
                    break;
                }
            }
        }
        // Hide counter when this line contains ElementType == 3; show for ElementType == 2 (and normal lines).
        bool show = Counter > 0 && !hasElementType3;
        if (show && IsIceLine && RemainingCounter <= 0)
            show = false;
        _counterText.gameObject.SetActive(show);
        if (show)
            _counterText.text = RemainingCounter.ToString();
    }

    private void MeltIce()
    {
        if (Cubes == null) return;
        for (int i = 0; i < Cubes.Count; i++)
        {
            CubeLine cube = Cubes[i];
            if (cube == null) continue;
            if (cube.ElementType != 2) continue;
            cube.SetElementType(0);
        }
        SyncElementTypesFromCubes();
        AudioManager.Instance.PlaySFX(SFXType.Ice);
    }

    private void SyncElementTypesFromCubes()
    {
        if (Cubes == null) return;
        if (ElementTypes == null)
            ElementTypes = new List<int>(Cubes.Count);

        if (ElementTypes.Count != Cubes.Count)
        {
            ElementTypes.Clear();
            for (int i = 0; i < Cubes.Count; i++)
            {
                CubeLine cube = Cubes[i];
                ElementTypes.Add(cube != null ? cube.ElementType : 0);
            }
            return;
        }

        for (int i = 0; i < Cubes.Count; i++)
        {
            CubeLine cube = Cubes[i];
            ElementTypes[i] = cube != null ? cube.ElementType : 0;
        }
    }

    private void UpdateCounterTextPosition()
    {
        if (_counterText == null) return;
        if (Cubes == null || Cubes.Count == 0) return;

        int midIndex = Cubes.Count / 2;
        CubeLine anchor = Cubes[midIndex];
        if (anchor == null) return;
        _counterText.transform.position = anchor.transform.position + _counterTextOffset;
    }

    private void ResetMoveState()
    {
        _history.Clear();
        _pendingDetach.Clear();
        _reservedConveyorBaseIndex = -1;
        _willDefinitelyRevert = false;
        _targetConveyorCell = null;
        _waitingForConveyorEnter = false;
        _blockingCube = null;
        CancelConveyorWait();
    }

    private void CancelConveyorWait()
    {
        if (_waitConveyorRoutine == null)
            return;

        StopCoroutine(_waitConveyorRoutine);
        _waitConveyorRoutine = null;
    }

    private bool TryGetMovePrerequisites(out Board board, out CubeLine head, out Vector2Int headPos)
    {
        board = Board.Instance;
        head = null;
        headPos = default;

        if (board == null) return false;
        if (Cubes == null || Cubes.Count == 0) return false;

        head = Cubes[^1];
        if (head == null || head.Cell == null) return false;
        headPos = head.Cell.Position;
        return true;
    }

    private bool TryComputeGridDirection(Board board)
    {
        if (_reuseGridDirNextMove && _gridDir != Vector2Int.zero)
        {
            _reuseGridDirNextMove = false;
            return true;
        }

        _reuseGridDirNextMove = false;

        if (Cubes.Count < 2)
            return false;

        CubeLine tail = Cubes[0];
        CubeLine tailNext = Cubes[1];
        if (tail == null || tailNext == null) return false;

        _cellDistance = Vector3.Distance(tail.transform.position, tailNext.transform.position);
        if (_cellDistance <= 0.0001f) return false;

        CubeLine headPrev = Cubes[^2];
        CubeLine head = Cubes[^1];
        if (headPrev == null || headPrev.Cell == null || head == null || head.Cell == null) return false;

        Vector2Int p0 = headPrev.Cell.Position;
        Vector2Int p1 = head.Cell.Position;
        _gridDir = board.NormalizeGridDir(p1 - p0);
        return _gridDir != Vector2Int.zero;
    }

    private bool TryBuildMovePlan(Board board, Vector2Int headPos, GridCell conveyorCell, GridCell occupiedCell)
    {
        _blockingCube = occupiedCell != null ? occupiedCell.CubeOnCell : null;

        if (conveyorCell == null)
        {
            _willDefinitelyRevert = true;
            _targetConveyorCell = null;

            if (occupiedCell == null)
            {
                StartRevert();
                return false;
            }

            int distToObstacle = board.GetManhattanDistance(headPos, occupiedCell.Position) - 1;
            return ApplyRevertPlan(distToObstacle);
        }

        _targetConveyorCell = conveyorCell;
        int distToConveyor = board.GetManhattanDistance(headPos, conveyorCell.Position) - 1;

        if (occupiedCell == null)
            return ApplyConveyorPlan(distToConveyor);

        int distToObstacle2 = board.GetManhattanDistance(headPos, occupiedCell.Position) - 1;
        if (distToObstacle2 < distToConveyor)
        {
            _willDefinitelyRevert = true;
            _targetConveyorCell = null;
            return ApplyRevertPlan(distToObstacle2);
        }

        return ApplyConveyorPlan(distToConveyor);
    }

    private bool ApplyRevertPlan(int stepsBeforeStop)
    {
        SetTempTypeOnBodyOnly(CubeType.Normal);

        if (stepsBeforeStop <= 0)
        {
            TriggerBlockingCubeTilt();
            ShowWarningAll();
            StartRevert();
            return false;
        }

        _totalSteps = stepsBeforeStop;
        _remainingSteps = _totalSteps;
        return true;
    }

    private bool ApplyConveyorPlan(int stepsToConveyor)
    {
        _willDefinitelyRevert = false;
        SetTempTypeOnAll(CubeType.Normal);
        _totalSteps = Mathf.Max(0, stepsToConveyor);
        _remainingSteps = _totalSteps;
        return true;
    }

    private void SetTempTypeOnAll(CubeType type)
    {
        for (int i = 0; i < Cubes.Count; i++)
            Cubes[i].SetTempType(type);
    }

    private void SetTempTypeOnBodyOnly(CubeType type)
    {
        for (int i = 0; i < Cubes.Count - 1; i++)
            Cubes[i].SetTempType(type);
    }

    private void ShowWarningAll()
    {
        for (int i = 0; i < Cubes.Count; i++)
            Cubes[i].ShowWarning();
    }
    public void DestroyLine()
    {
        ResetMoveState();

        Board.Instance.NotifyAnyLineEnteredConveyor();

        // Track destroyed cubes by color
        Dictionary<ObjectColor, int> destroyedByColor = new Dictionary<ObjectColor, int>();

        for (int i = Cubes.Count - 1; i >= 0; i--)
        {
            CubeLine cube = Cubes[i];
            if (cube == null) continue;

            cube.transform.DOKill();

            // Check if cube will only reveal (ElementType 3 not yet revealed)
            bool willOnlyReveal = cube.ElementType == 3 && !cube.IsElementType3Revealed;

            // Get the color to reduce from shooter (use OriginalColor for outer layer)
            ObjectColor colorToReduce = cube.OriginalColor;

            if (willOnlyReveal)
            {
                // Only reveal the inner layer, keep the cube in the line
                // Reduce shooter for the outer color being removed
                if (!destroyedByColor.ContainsKey(colorToReduce))
                    destroyedByColor[colorToReduce] = 0;
                destroyedByColor[colorToReduce]++;

                cube.OnHit();
            }
            else
            {
                // Actually destroy the cube
                // Use current Color for cubes that are fully destroyed
                ObjectColor cubeColor = cube.Color;
                if (!destroyedByColor.ContainsKey(cubeColor))
                    destroyedByColor[cubeColor] = 0;
                destroyedByColor[cubeColor]++;

                if (cube.Cell != null && cube.Cell.CubeOnCell == cube)
                {
                    cube.Cell.CubeOnCell = null;
                }

                cube.Line = null;
                cube.transform.SetParent(Board.Instance.transform, true);
                cube.OnHit();
                Cubes.RemoveAt(i);
            }
        }

        // Reduce shooters for each color
        foreach (var kvp in destroyedByColor)
        {
            ShooterController.Instance.ReduceShooterTotalByColor(kvp.Key, kvp.Value);
        }

        if (Cubes.Count == 0)
        {
            Destroy(gameObject);
        }
        else
        {
            // Refresh counter text if some cubes remain
            RefreshCounterText();
        }
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
                TriggerBlockingCubeTilt();
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

        if (!TryValidateAndPrepareTargets())
        {
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

    private bool TryValidateAndPrepareTargets()
    {
        if (Cubes == null || Cubes.Count == 0) return false;
        if (_gridDir == Vector2Int.zero) return false;

        EnsureTargetsBuffer();

        for (int i = 0; i < Cubes.Count; i++)
        {
            if (i == Cubes.Count - 1)
            {
                Vector2Int next = Cubes[i].Cell.Position + _gridDir;
                _targetsBuffer[i] = Board.Instance.GetCellAt(next);
            }
            else
            {
                _targetsBuffer[i] = Cubes[i + 1].Cell;
            }

            if (_targetsBuffer[i] == null)
                return false;
        }

        return true;
    }

    private void EnsureTargetsBuffer()
    {
        if (_targetsBuffer != null && _targetsBuffer.Length == Cubes.Count)
            return;
        _targetsBuffer = new GridCell[Cubes.Count];
    }

    private void TriggerBlockingCubeTilt()
    {
        if (_blockingCube == null)
            return;

        if (_blockingCubeTiltTween != null && _blockingCubeTiltTween.IsActive())
            _blockingCubeTiltTween.Kill();

        Vector3 pushDirWorld = new Vector3(_gridDir.x, 0f, _gridDir.y);
        if (pushDirWorld == Vector3.zero)
            return;

        Vector3 axis = Vector3.Cross(Vector3.up, pushDirWorld);
        if (axis == Vector3.zero)
            return;

        axis.Normalize();

        Quaternion startRot = _blockingCube.transform.rotation;
        Quaternion targetRot = Quaternion.AngleAxis(10f, axis) * startRot;

        _blockingCubeTiltTween = _blockingCube.transform.DORotateQuaternion(targetRot, 0.08f).SetEase(Ease.OutQuad).SetLoops(2, LoopType.Yoyo).OnKill(() =>
        {
            _blockingCube.transform.rotation = startRot;
        });
    }

    private void DoStepForward(System.Action onComplete)
    {
        float duration = GetStepDuration();
        int finished = 0;
        int expected = Cubes.Count;
        CubeLine anchor = (Cubes != null && Cubes.Count > 0) ? Cubes[Cubes.Count / 2] : null;

        for (int i = 0; i < Cubes.Count; i++)
        {
            CubeLine cube = Cubes[i];
            GridCell from = cube.Cell;
            GridCell to = _targetsBuffer[i];

            Vector3 targetPos = to.transform.position;

            Tween tween = cube.transform.DOMove(targetPos, duration)
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

            if (cube == anchor && _counterText != null)
            {
                tween.OnUpdate(UpdateCounterTextPosition);
            }
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
        RefreshCounterText();
        if (Cubes.Count == 0)
        {
            ConveyorController.Instance.OnLineMoved();
            Destroy(gameObject);
        }
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

        if (Board.Instance != null && Board.Instance.IsConveyorCellBlockedByTunnel(_targetConveyorCell.Position))
        {
            _isMoving = false;
            _waitingForConveyorEnter = false;
            _reservedConveyorBaseIndex = -1;
            _targetConveyorCell = null;
            _willDefinitelyRevert = true;
            StartRevert();
            return true;
        }

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

        CancelConveyorWait();
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

        _isMoving = true;
        _reuseGridDirNextMove = true;

        _waitingForConveyorEnter = false;
        _reservedConveyorBaseIndex = -1;
        _targetConveyorCell = null;

        if (Cubes.Count > 0)
        {
            CubeLine newHead = Cubes[^1];
            if (newHead != null && newHead.Cell != null)
            {
                Vector2Int newHeadPos = newHead.Cell.Position;
                Vector2Int prev = newHeadPos - _gridDir;

                GridCell conveyorCell = Board.Instance.FindConveyorCell(prev, newHeadPos);
                GridCell occupiedCell = Board.Instance.FindOccupiedCell(prev, newHeadPos);

                if (conveyorCell != null && Board.Instance.IsConveyorCellBlockedByTunnel(conveyorCell.Position))
                {
                    int distToTunnel = Board.Instance.GetManhattanDistance(newHeadPos, conveyorCell.Position);
                    int distToOccupied = occupiedCell != null ? Board.Instance.GetManhattanDistance(newHeadPos, occupiedCell.Position) : int.MaxValue;
                    if (distToTunnel <= distToOccupied)
                        occupiedCell = conveyorCell;
                    conveyorCell = null;
                }

                if (conveyorCell != null)
                {
                    _targetConveyorCell = conveyorCell;
                    int distToConveyor = Board.Instance.GetManhattanDistance(newHeadPos, conveyorCell.Position) - 1;

                    if (occupiedCell != null)
                    {
                        int distToObstacle = Board.Instance.GetManhattanDistance(newHeadPos, occupiedCell.Position) - 1;
                        if (distToObstacle < distToConveyor)
                        {
                            _willDefinitelyRevert = true;
                            _targetConveyorCell = null;
                            _totalSteps = distToObstacle;
                            _remainingSteps = _totalSteps;
                            SetTempTypeOnBodyOnly(CubeType.Normal);
                        }
                        else
                        {
                            _totalSteps = distToConveyor;
                            _remainingSteps = _totalSteps;
                        }
                    }
                    else
                    {
                        _totalSteps = distToConveyor;
                        _remainingSteps = _totalSteps;
                    }
                }
            }
        }

        StepForward();
    }

    private void StartRevert()
    {
        AudioManager.Instance.PlaySFX(SFXType.SelectWrong);
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

        GridCell[] prev = _history.Pop();
        float duration = GetStepDuration();
        int finished = 0;
        CubeLine anchor = (Cubes != null && Cubes.Count > 0) ? Cubes[Cubes.Count / 2] : null;

        for (int i = 0; i < Cubes.Count; i++)
        {
            CubeLine cube = Cubes[i];
            GridCell from = cube.Cell;
            GridCell to = i < prev.Length ? prev[i] : null;
            if (to == null)
            {
                _history.Clear();
                _isReverting = false;
                _willDefinitelyRevert = false;
                OnLineReverted();
                return;
            }

            Tween tween = cube.transform.DOMove(to.transform.position, duration)
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

            if (cube == anchor && _counterText != null)
            {
                tween.OnUpdate(UpdateCounterTextPosition);
            }
        }
    }

    private void SaveSnapshot()
    {
        GridCell[] snapshot = new GridCell[Cubes.Count];
        for (int i = 0; i < Cubes.Count; i++)
            snapshot[i] = Cubes[i].Cell;

        _history.Push(snapshot);
    }

    private float GetStepDuration()
    {
        float speed = Mathf.Max(0.01f, _moveSpeed);
        return _cellDistance / speed;
    }

    private void OnLineReverted()
    {
        for (int i = 0; i < Cubes.Count - 1; i++)
        {
            Cubes[i].RevertType();
        }
    }
}
