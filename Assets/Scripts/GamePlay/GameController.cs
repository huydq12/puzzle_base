using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class GameController : Singleton<GameController>
{
    [ReadOnly] public bool CanTouch;
    [SerializeField] private LayerMask _stackLayer;
    [SerializeField] private LayerMask _blockLayer;
    [SerializeField] private float _dragSpeed;
    [SerializeField] private float _collisionSkin;
    [SerializeField] private float _collisionRadius;
    [SerializeField] private int _maxSlideIterations;
    private Vector3 _desiredMove;
    private Camera _camera;
    private ButtonStack _currentStack;
    private bool _isDragging;
    private Vector3 _dragOffset;
    private GridCell _dragSourceCell;
    private GridCell _lastUserPlacedCell;
    private Tween _placeTween;
    private Stack<GridCell> _pendingMerges = new();
    private Stack<GameSnapshot> _snapshots = new();
    private readonly Dictionary<GridCell, int> _tmpConnectableCellIndex = new();
    private readonly Dictionary<GridCell, int> _tmpConnectableVisited = new();
    private readonly Queue<GridCell> _tmpConnectableQueue = new();
    private readonly Dictionary<ObjectColor, List<GridCell>> _tmpColorCellMap = new();
    private readonly List<List<GridCell>> _tmpColorCellListPool = new();

    [ReadOnly] public int ActiveMergeCount;
    [ReadOnly] public bool IsCollecting;
    [ReadOnly, ShowInInspector] private HashSet<GridCell> _cellsInMerging = new();
    public bool IsAnimating => ActiveMergeCount > 0 || _placeTween != null || IsCollecting || _isDragging || _currentStack != null || !ContainerManager.Instance.IsContainerReady;

    [Button("Check Lose")]
    public bool IsLoseByNoValidMove()
    {
        var containerManager = ContainerManager.Instance;
        var currentContainer = containerManager != null ? containerManager.CurrentContainer : null;
        var containerColor = currentContainer != null ? currentContainer.Color : default;
        bool canCollectToContainer = containerManager != null && containerManager.IsContainerReady && currentContainer != null && currentContainer.EmptySlotCount >= Defines.COLLECT_COUNT;

        if (Board.Instance != null && Board.Instance.HasAnyGateProgressPotentialForLoseCheck())
        {
            return false;
        }
        int trayMatchCount = 0;
        if (Tray.Instance != null && Tray.Instance.Slots != null)
        {
            int totalSlot = Tray.Instance.TotalSlot;
            var slots = Tray.Instance.Slots;
            for (int i = 0; i < totalSlot && i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot != null && slot.IsOccupied && slot.ButtonInSlot != null && slot.ButtonInSlot.Color == containerColor)
                {
                    trayMatchCount++;
                }
            }
        }
        if (trayMatchCount >= Defines.COLLECT_COUNT && canCollectToContainer)
        {
            return false;
        }

        //Check nếu trên board 
        foreach (var cell in Board.Instance.Cells)
        {
            if (cell == null || !cell.IsOccupied || cell.Stack == null || cell.Stack.TopButton == null) continue;
            if (Board.Instance != null && (Board.Instance.IsCellLocked(cell) || Board.Instance.IsCellFrozen(cell) || Board.Instance.IsCellScrewed(cell))) continue;
            if (!canCollectToContainer) continue;
            if (cell.Stack.TopButton.Color != containerColor) continue;
            if (cell.Stack.TopColors().Count >= Defines.COLLECT_COUNT)
            {
                return false;
            }
        }

        foreach (var kv in _tmpColorCellMap)
        {
            kv.Value.Clear();
            _tmpColorCellListPool.Add(kv.Value);
        }
        _tmpColorCellMap.Clear();

        var colorCellMap = _tmpColorCellMap;

        foreach (var cell in Board.Instance.Cells)
        {
            if (cell == null || !cell.IsOccupied || cell.Stack == null || cell.Stack.TopButton == null)
                continue;
            if (Board.Instance != null && (Board.Instance.IsCellLocked(cell) || Board.Instance.IsCellFrozen(cell) || Board.Instance.IsCellScrewed(cell))) continue;
            if (cell.Stack.IsMoving || cell.Stack.IsMerging) continue;

            var color = cell.Stack.TopButton.Color;

            if (!colorCellMap.TryGetValue(color, out var list))
            {
                list = GetPooledCellList();
                colorCellMap[color] = list;
            }

            list.Add(cell);
        }
        foreach (var kv in colorCellMap)
        {
            var cellList = kv.Value;
            if (cellList.Count <= 1)
                continue;

            if (HasConnectablePair(cellList))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsPickableForLoseCheck(GridCell cell)
    {
        if (cell == null || !cell.IsOccupied || cell.Stack == null) return false;
        if (cell.Stack.TopButton == null) return false;
        if (!cell.Stack.CanInteraction || cell.Stack.IsMoving || cell.Stack.IsMerging) return false;
        if (_cellsInMerging.Contains(cell)) return false;
        if (Board.Instance != null)
        {
            if (Board.Instance.IsCellLocked(cell) || Board.Instance.IsCellFrozen(cell) || Board.Instance.IsCellScrewed(cell)) return false;
            if (Board.Instance.IsGateCell(cell)) return false;
        }
        return true;
    }

    private bool IsTraversableEmptyForLoseCheck(GridCell cell)
    {
        if (cell == null) return false;
        if (cell.IsOccupied) return false;
        if (Board.Instance != null)
        {
            if (Board.Instance.IsCellLocked(cell) || Board.Instance.IsCellFrozen(cell) || Board.Instance.IsCellScrewed(cell)) return false;
        }
        return true;
    }

    private List<GridCell> GetPooledCellList()
    {
        int last = _tmpColorCellListPool.Count - 1;
        if (last >= 0)
        {
            var list = _tmpColorCellListPool[last];
            _tmpColorCellListPool.RemoveAt(last);
            return list;
        }

        return new List<GridCell>();
    }

    private bool HasConnectablePair(List<GridCell> cells)
    {
        if (cells == null || cells.Count <= 1)
        {
            return false;
        }

        _tmpConnectableCellIndex.Clear();
        for (int i = 0; i < cells.Count; i++)
        {
            GridCell c = cells[i];
            if (c != null)
            {
                _tmpConnectableCellIndex[c] = i;
            }
        }

        _tmpConnectableVisited.Clear();
        _tmpConnectableQueue.Clear();

        for (int i = 0; i < cells.Count; i++)
        {
            GridCell src = cells[i];
            if (src == null || src.Neighbors == null) continue;
            if (!IsPickableForLoseCheck(src)) continue;

            for (int n = 0; n < src.Neighbors.Count; n++)
            {
                GridCell nb = src.Neighbors[n];
                if (nb == null) continue;

                if (_tmpConnectableCellIndex.TryGetValue(nb, out int ignoreIndex))
                {
                    return true;
                }

                if (IsTraversableEmptyForLoseCheck(nb))
                {
                    if (_tmpConnectableVisited.TryGetValue(nb, out int from))
                    {
                        if (from != i) return true;
                    }
                    else
                    {
                        _tmpConnectableVisited[nb] = i;
                        _tmpConnectableQueue.Enqueue(nb);
                    }
                }
            }
        }

        while (_tmpConnectableQueue.Count > 0)
        {
            GridCell cur = _tmpConnectableQueue.Dequeue();
            int from = _tmpConnectableVisited[cur];

            if (cur.Neighbors == null) continue;

            for (int i = 0; i < cur.Neighbors.Count; i++)
            {
                GridCell nb = cur.Neighbors[i];
                if (nb == null) continue;

                if (IsTraversableEmptyForLoseCheck(nb))
                {
                    if (_tmpConnectableVisited.TryGetValue(nb, out int prev))
                    {
                        if (prev != from) return true;
                    }
                    else
                    {
                        _tmpConnectableVisited[nb] = from;
                        _tmpConnectableQueue.Enqueue(nb);
                    }
                }
                else
                {
                    if (_tmpConnectableCellIndex.TryGetValue(nb, out int other) && other != from)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    void Start()
    {
        _camera = Camera.main;
        CanTouch = true;
        GameManagerInGame.Instance.OnEndLevel += () =>
        {
            if (_isDragging)
            {
                OnTouchEnded();
            }
        };
        GameManagerInGame.Instance.OnStartLevel += () =>
         {
             StopAllCoroutines();
             _isDragging = false;
             _currentStack = null;
         };
    }

    public void Clear()
    {
        StopAllCoroutines();
        _isDragging = false;
        _placeTween?.Kill();
        _pendingMerges.Clear();
        _cellsInMerging.Clear();
        ActiveMergeCount = 0;
        _desiredMove = Vector3.zero;
        CanTouch = true;
        _placeTween = null;
        IsCollecting = false;
        _snapshots.Clear();
        _lastUserPlacedCell = null;
    }
    void Update()
    {
        if (GameManagerInGame.Instance.CurrentGameStateInGame != GameStateInGame.Playing || _placeTween != null || !CanTouch) return;
        HandleDefaultTouch();
        ProcessPendingMerges();
    }
    private void ProcessPendingMerges()
    {
        while (_pendingMerges.Count > 0)
        {
            GridCell cell = _pendingMerges.Pop();

            if (cell != null && !_cellsInMerging.Contains(cell))
            {
                StartCoroutine(CheckForMergeAsync(cell));
            }
        }
    }
    private List<GridCell> GetSimilarNeighBorGridCells(ObjectColor? gridCellTopButtonColor, GridCell gridCell)
    {
        List<GridCell> similars = new List<GridCell>();
        foreach (var cell in gridCell.Neighbors)
        {
            if (Board.Instance != null && Board.Instance.IsGateCell(cell))
            {
                continue;
            }
            if (_cellsInMerging.Contains(cell))
            {
                continue;
            }
            if (Board.Instance != null && Board.Instance.IsCellLocked(cell))
            {
                continue;
            }
            if (Board.Instance != null && Board.Instance.IsCellFrozen(cell))
            {
                continue;
            }
            if (Board.Instance != null && Board.Instance.IsCellScrewed(cell))
            {
                continue;
            }
            if (cell.IsOccupied && cell.Stack.IsMoving) continue;
            if (cell.IsOccupied && cell.Stack.TopButton.Color == gridCellTopButtonColor)
            {
                similars.Add(cell);
            }
        }
        return similars;
    }

    private static int GetStackButtonCount(GridCell cell)
    {
        if (cell == null || cell.Stack == null || cell.Stack.Buttons == null)
        {
            return 0;
        }
        return cell.Stack.Buttons.Count;
    }

    private static int GetSameColorNeighborCount(GridCell cell, HashSet<GridCell> group, ObjectColor color)
    {
        if (cell == null || cell.Neighbors == null || group == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < cell.Neighbors.Count; i++)
        {
            var nb = cell.Neighbors[i];
            if (nb == null) continue;
            if (!group.Contains(nb)) continue;
            if (!nb.IsOccupied || nb.Stack == null || nb.Stack.TopButton == null) continue;
            if (nb.Stack.TopButton.Color != color) continue;
            count++;
        }
        return count;
    }

    private static GridCell PickMergeRoot(List<GridCell> cells, GridCell preferred, ObjectColor color)
    {
        if (cells != null)
        {
            var group = new HashSet<GridCell>(cells);

            GridCell root = null;
            int bestStackCount = -1;
            int bestNeighborCount = -1;

            for (int i = 0; i < cells.Count; i++)
            {
                var c = cells[i];
                if (c == null) continue;

                int stackCount = GetStackButtonCount(c);
                int neighborCount = GetSameColorNeighborCount(c, group, color);

                if (stackCount > bestStackCount)
                {
                    root = c;
                    bestStackCount = stackCount;
                    bestNeighborCount = neighborCount;
                    continue;
                }

                if (stackCount == bestStackCount)
                {
                    if (neighborCount > bestNeighborCount)
                    {
                        root = c;
                        bestNeighborCount = neighborCount;
                        continue;
                    }

                    if (neighborCount == bestNeighborCount && preferred != null && c == preferred)
                    {
                        root = c;
                    }
                }
            }

            if (root != null)
            {
                return root;
            }
        }

        if (cells != null)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                var c = cells[i];
                if (c != null && c.Stack != null)
                {
                    return c;
                }
            }
            for (int i = 0; i < cells.Count; i++)
            {
                var c = cells[i];
                if (c != null)
                {
                    return c;
                }
            }
        }

        return null;
    }

    private IEnumerator CheckForMergeAsync(GridCell startCell)
    {
        if (Board.Instance != null && Board.Instance.IsCellLocked(startCell))
            yield break;
        if (Board.Instance != null && Board.Instance.IsCellFrozen(startCell))
            yield break;
        if (Board.Instance != null && Board.Instance.IsCellScrewed(startCell))
            yield break;
        if (Board.Instance != null && Board.Instance.IsGateCell(startCell))
            yield break;
        if (!startCell.IsOccupied || startCell.Stack.TopButton == null || startCell.Stack.IsMoving)
            yield break;

        var color = startCell.Stack.TopButton.Color;
        List<GridCell> firstNeighbors = GetSimilarNeighBorGridCells(color, startCell);

        if (firstNeighbors.Count == 0)
        {
            if (ActiveMergeCount == 0 && !IsCollecting)
            {
                IsCollecting = true;

                yield return ContainerManager.Instance.TryCollectButtons();
                yield return Tray.Instance.TryCollectButtons();

                IsCollecting = false;

                yield return null;
                if (!IsAnimating)
                {
                    CheckLoseByNoValidMove();
                }

            }
            yield break;
        }

        HashSet<GridCell> mergeGroup = new HashSet<GridCell> { startCell };
        Queue<GridCell> queue = new Queue<GridCell>(firstNeighbors);

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            if (_cellsInMerging.Contains(cur)) continue;

            if (!mergeGroup.Add(cur)) continue;

            var similars = GetSimilarNeighBorGridCells(color, cur);
            foreach (var n in similars)
            {
                if (!_cellsInMerging.Contains(n) && !mergeGroup.Contains(n))
                    queue.Enqueue(n);
            }
        }

        if (mergeGroup.Count <= 1)
            yield break;
    
        List<GridCell> cells = mergeGroup.ToList();
        GridCell root;
        if (cells.Count >= 3 && _lastUserPlacedCell != null && cells.Contains(_lastUserPlacedCell))
        {
            root = _lastUserPlacedCell;
            _lastUserPlacedCell = null;
        }
        else
        {
            root = PickMergeRoot(cells, startCell, color);
        }
        if (root == null)
            yield break;

        foreach (var cell in cells)
        {
            _cellsInMerging.Add(cell);
            if (cell != null && cell.Stack != null)
            {
                cell.Stack.IsMerging = true;
            }
            cell.Focus = true;
        }

        ActiveMergeCount++;
        yield return MergeGroupIntoSingleStack(cells, root);
        ActiveMergeCount--;

        if (ActiveMergeCount == 0 && !IsCollecting)
        {
            IsCollecting = true;
            yield return ContainerManager.Instance.TryCollectButtons();
            yield return Tray.Instance.TryCollectButtons();
            IsCollecting = false;
            yield return null;
            if (IsAnimating) yield break;
            CheckLoseByNoValidMove();
        }
    }

    private IEnumerator MergeGroupIntoSingleStack(
    List<GridCell> cells,
    GridCell root)
    {
        if (root == null)
        {
            Debug.LogError("Root is null!");
            yield break;
        }

        var emptiedCells = new List<GridCell>();

        // BFS tree từ root
        Dictionary<GridCell, GridCell> parent = new Dictionary<GridCell, GridCell>();
        Dictionary<GridCell, int> depth = new Dictionary<GridCell, int>();
        Queue<GridCell> q = new Queue<GridCell>();

        parent[root] = null;
        depth[root] = 0;
        q.Enqueue(root);

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            if (cur == null || cur.Neighbors == null) continue;
            foreach (var nb in cur.Neighbors)
            {
                if (!cells.Contains(nb)) continue;
                if (parent.ContainsKey(nb)) continue;

                parent[nb] = cur;
                depth[nb] = depth[cur] + 1;
                q.Enqueue(nb);
            }
        }

        // Merge từ lá → root
        List<GridCell> ordered = new List<GridCell>();
        foreach (var kv in parent)
        {
            if (kv.Key != root)
                ordered.Add(kv.Key);
        }

        ordered.Sort((a, b) => depth[b].CompareTo(depth[a]));

        foreach (var from in ordered)
        {
            var to = parent[from];
            if (to == null) continue;

            if (from == null)
            {
                continue;
            }

            var fromStack = from.Stack;
            if (fromStack == null)
            {
                from.Focus = false;
                _cellsInMerging.Remove(from);
                continue;
            }

            if (to.Stack == null)
            {
                fromStack.IsMerging = false;
                from.Focus = false;
                _cellsInMerging.Remove(from);
                continue;
            }

            from.Focus = false;

            yield return fromStack
                .MoveToCell(to)
                .WaitForCompletion();

            if (fromStack != null)
            {
                fromStack.IsMerging = false;
            }
            _cellsInMerging.Remove(from);

            yield return null;

            if (from.IsOccupied)
                StackPlaceCallBack(from);
            else
                emptiedCells.Add(from);
        }

        root.Focus = false;
        if (root != null && root.Stack != null)
        {
            root.Stack.IsMerging = false;
        }
        _cellsInMerging.Remove(root);

        yield return null;

        if (root.IsOccupied)
            StackPlaceCallBack(root);

        if (Board.Instance != null && emptiedCells.Count > 0)
        {
            for (int i = 0; i < emptiedCells.Count; i++)
            {
                var c = emptiedCells[i];
                if (c == null) continue;
                if (c.IsOccupied) continue;
                Board.Instance.TryActivateIncomingGates(c);
            }
        }
    }

    private void HandleDefaultTouch()
    {
        if (Input.GetMouseButtonDown(0))
        {
            OnTouchBegan();

        }
        else if (Input.GetMouseButton(0))
        {
            OnTouchMoved();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            OnTouchEnded();
        }
    }


    private void OnTouchEnded()
    {
        GameUI.Instance.Get<UITopInGame>().ShowInfoButton("");
        if (_currentStack == null) return;
        if (TutorialManager.Instance.IsInTutorial)
        {
            switch (TutorialManager.Instance.CurrentTutorialType)
            {
                case TutorialType.Control:
                    {
                        if (TutorialManager.Instance.TutorialControlWaitMoveButton)
                        {
                            TutorialManager.Instance.HandleNextStep();
                        }
                        break;
                    }
                case TutorialType.Ice:
                    {
                        break;
                    }
            }
        }
        _currentStack.Highlight = false;

        GridCell targetCell = _currentStack.CurrentHoverCell;
        _currentStack.ClearCurrentCell();
        AudioManager.Instance.PlaySFX(SFXType.EndDrag);
        PlaceStackOnCell(targetCell);
    }
    private Vector3 GetMouseWorldPosition(Ray ray)
    {
        float targetY = _currentStack != null ? _currentStack.transform.position.y : 0f;
        float distance = (targetY - ray.origin.y) / ray.direction.y;

        Vector3 worldPos = ray.GetPoint(distance);
        worldPos.y = targetY;

        return worldPos;
    }
    private void OnTouchBegan()
    {
        if (TutorialManager.Instance.IsInTutorial)
        {
            switch (TutorialManager.Instance.CurrentTutorialType)
            {
                case TutorialType.Control:
                    {
                        if (!TutorialManager.Instance.TutorialControlWaitMoveButton)
                        {
                            return;
                        }
                        break;
                    }
                case TutorialType.Ice:
                    {
                        return;
                    }
            }
        }
        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100, _stackLayer))
        {
            if (!hit.transform.TryGetComponent(out _currentStack) || !_currentStack.CanInteraction || _currentStack.IsMerging || _cellsInMerging.Contains(_currentStack.Cell) || _currentStack.IsMoving)
            {
                _currentStack = null;
                return;
            }

            if (_currentStack != null && _currentStack.Cell != null && Board.Instance != null && Board.Instance.IsGateCell(_currentStack.Cell))
            {
                _currentStack = null;
                return;
            }

            if (_currentStack != null && _currentStack.Cell != null && Board.Instance != null && Board.Instance.IsCellLocked(_currentStack.Cell))
            {
                _currentStack = null;
                return;
            }

            if (_currentStack != null && _currentStack.Cell != null && Board.Instance != null && Board.Instance.IsCellFrozen(_currentStack.Cell))
            {
                _currentStack = null;
                return;
            }

            if (_currentStack != null && _currentStack.Cell != null && Board.Instance != null && Board.Instance.IsCellScrewed(_currentStack.Cell))
            {
                _currentStack = null;
                return;
            }
            AudioManager.Instance.PlaySFX(SFXType.BeginDrag);
            SaveSnapshot();

            var colorCounts = string.Empty;
            if (_currentStack.Buttons != null)
            {
                var segments = new List<string>();
                ObjectColor? lastColor = null;
                var runCount = 0;

                foreach (var button in _currentStack.Buttons)
                {
                    if (button == null) continue;

                    var color = button.Color;
                    if (lastColor == null)
                    {
                        lastColor = color;
                        runCount = 1;
                        continue;
                    }

                    if (EqualityComparer<ObjectColor>.Default.Equals(lastColor.Value, color))
                    {
                        runCount++;
                    }
                    else
                    {
                        segments.Add(lastColor.Value + ":" + runCount);
                        lastColor = color;
                        runCount = 1;
                    }
                }

                if (lastColor != null && runCount > 0)
                {
                    segments.Add(lastColor.Value + ":" + runCount);
                }

                colorCounts = string.Join(", ", segments);
            }

            Debug.Log("màu theo đoạn: " + (string.IsNullOrEmpty(colorCounts) ? "0" : colorCounts));

            if (colorCounts != null && colorCounts.Length > 0)
            {
                GameUI.Instance.Get<UITopInGame>().ShowInfoButton(colorCounts);
            }

            var sourceCell = _currentStack.Cell;
            _dragSourceCell = sourceCell;

            _currentStack.CanInteraction = false;
            _currentStack.Cell.AssignStack(null);
            _currentStack.Configure(null);
            _currentStack.UpdateCurrentCell();
            _currentStack.Highlight = true;

            Vector3 mouseWorld = GetMouseWorldPosition(ray);
            _dragOffset = _currentStack.transform.position - mouseWorld;

            _isDragging = true;
            HandleStackDrag();
        }
    }


    private void OnTouchMoved()
    {
        if (_currentStack == null || !_isDragging) return;

        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        _currentStack.UpdateCurrentCell();

        Vector3 mouseWorld = GetMouseWorldPosition(ray);
        Vector3 hitPoint = mouseWorld + _dragOffset;
        Vector3 currentPos = _currentStack.transform.position;

        Vector3 desiredMove = hitPoint - currentPos;
        desiredMove.y = 0;

        _desiredMove = Vector3.ClampMagnitude(desiredMove / Time.deltaTime, _dragSpeed);
        HandleStackDrag();
    }

    private void PlaceStackOnCell(GridCell targetCell)
    {
        float timeMove = 0.1f;
        if (_currentStack != null)
        {
            _lastUserPlacedCell = null;
            if (targetCell != null && Board.Instance != null && Board.Instance.IsCellLocked(targetCell))
            {
                targetCell = null;
            }
            if (targetCell != null && Board.Instance != null && Board.Instance.IsCellFrozen(targetCell))
            {
                targetCell = null;
            }
            if (targetCell != null && Board.Instance != null && Board.Instance.IsCellScrewed(targetCell))
            {
                targetCell = null;
            }
            if (targetCell != null && Board.Instance != null && Board.Instance.IsGateCell(targetCell))
            {
                targetCell = null;
            }
            if (targetCell != null && Board.Instance != null && Board.Instance.IsReservedGateTarget(targetCell.Position))
            {
                targetCell = null;
            }
            if (targetCell == null)
            {
                var fallbackCell = _dragSourceCell;
                _dragSourceCell = null;

                if (fallbackCell == null)
                {
                    _currentStack.CanInteraction = true;
                    _currentStack = null;
                    _isDragging = false;
                    _placeTween = null;
                    _desiredMove = Vector3.zero;
                    return;
                }

                _placeTween = _currentStack.transform.DOMove(fallbackCell.transform.position, timeMove)
                .OnComplete(() =>
                {
                    _currentStack.Configure(fallbackCell);
                    StackPlaceCallBack(fallbackCell);
                    _currentStack.CanInteraction = true;
                    _currentStack = null;
                    _isDragging = false;
                    _placeTween = null;
                    _desiredMove = Vector3.zero;
                });
                return;
            }

            _placeTween = _currentStack.transform.DOMove(targetCell.transform.position, timeMove)
            .OnComplete(() =>
            {
                var sourceCell = _dragSourceCell;
                _currentStack.Configure(targetCell);
                _lastUserPlacedCell = targetCell;
                StackPlaceCallBack(targetCell);
                if (sourceCell != null && sourceCell != targetCell && Board.Instance != null && !sourceCell.IsOccupied)
                {
                    Board.Instance.TryActivateIncomingGates(sourceCell);
                }
                _currentStack.CanInteraction = true;
                _currentStack = null;
                _isDragging = false;
                _placeTween = null;
                _desiredMove = Vector3.zero;
                _dragSourceCell = null;
            });
        }
    }

    private void HandleStackDrag()
    {
        Vector3 position = _currentStack.transform.position;
        Vector3 velocity = _desiredMove;
        float remainingTime = Time.deltaTime;

        Vector3 primaryNormal = Vector3.zero;
        Vector3 secondaryNormal;

        for (int i = 0; i < _maxSlideIterations; i++)
        {
            if (velocity.sqrMagnitude < 0.0001f)
                break;

            Vector3 displacement = velocity * remainingTime;
            float distance = displacement.magnitude;

            Vector3 bottom = position;
            Vector3 top = position + Vector3.up;

            if (!Physics.CapsuleCast(bottom, top, _collisionRadius,
                displacement.normalized, out RaycastHit hit,
                distance + _collisionSkin, _blockLayer))
            {
                position += displacement;
                break;
            }

            // Di chuyển đến điểm va chạm
            float travel = Mathf.Max(0, hit.distance - _collisionSkin);
            position += displacement.normalized * travel;

            // Xử lý va chạm với 2 tường (góc)
            if (primaryNormal == Vector3.zero)
            {
                // Tường đầu tiên
                primaryNormal = hit.normal;
                velocity = Vector3.ProjectOnPlane(velocity, primaryNormal);
            }
            else
            {
                float dot = Vector3.Dot(primaryNormal, hit.normal);

                // Nếu đây là tường khác (không song song)
                if (Mathf.Abs(dot - 1f) > 0.01f)
                {
                    secondaryNormal = hit.normal;

                    // Tính hướng trượt dọc theo cạnh góc (edge)
                    Vector3 slideDir = Vector3.Cross(primaryNormal, secondaryNormal);
                    slideDir.y = 0; // Giữ trên mặt phẳng ngang

                    if (slideDir.sqrMagnitude > 0.001f)
                    {
                        slideDir.Normalize();

                        // Chiếu velocity lên hướng edge
                        float projectedSpeed = Vector3.Dot(velocity, slideDir);

                        // Nếu đang đi về phía góc (projectedSpeed âm) → dừng
                        if (projectedSpeed < 0.1f)
                        {
                            velocity = Vector3.zero;
                            break;
                        }

                        // Nếu không → trượt dọc theo edge
                        velocity = slideDir * projectedSpeed;
                    }
                    else
                    {
                        // 2 tường song song hoặc đối nhau → dừng
                        velocity = Vector3.zero;
                        break;
                    }
                }
                else
                {
                    // Vẫn là tường cũ → project bình thường
                    velocity = Vector3.ProjectOnPlane(velocity, hit.normal);
                }
            }

            // Tính thời gian còn lại
            float speed = velocity.magnitude;
            if (speed < 0.0001f)
                break;

            remainingTime -= travel / speed;

            if (remainingTime <= 0f)
                break;
        }

        _currentStack.transform.position = position;
    }

    public void StackPlaceCallBack(GridCell gridCell)
    {
        if (gridCell == null) return;

        if (gridCell != null && gridCell.Stack != null && gridCell.Stack.SkipGateOnce)
        {
            gridCell.Stack.SkipGateOnce = false;
        }
        else
        {
            if (Board.Instance != null)
            {
                Board.Instance.TryActivateGate(gridCell);
            }
        }
        _pendingMerges.Push(gridCell);
    }

    private void SaveSnapshot()
    {
        if (Board.Instance == null || Tray.Instance == null || ContainerManager.Instance == null)
        {
            return;
        }

        GameSnapshot snapshot = new GameSnapshot
        {
            Board = Board.Instance.CreateSnapshot(),
            Tray = Tray.Instance.CreateSnapshot(),
            ContainerManager = ContainerManager.Instance.CreateSnapshot(),
        };

        _snapshots.Push(snapshot);
    }

    public void ClearSnapshots()
    {
        _snapshots.Clear();
    }

    public void SwapCurrentAndNextContainers()
    {
        if (GameManagerInGame.Instance.CurrentGameStateInGame != GameStateInGame.Playing)
        {
            return;
        }

        if (IsAnimating || !CanTouch)
        {
            return;
        }

        if (ContainerManager.Instance == null || !ContainerManager.Instance.CanSwapCurrentAndNextContainers())
        {
            return;
        }

        SaveSnapshot();
        ContainerManager.Instance.SwapCurrentAndNextContainers();
    }

    [Button("Shuffle Equal Stacks")]
    public void ShuffleEqualStacks()
    {
        if (GameManagerInGame.Instance.CurrentGameStateInGame != GameStateInGame.Playing)
        {
            return;
        }

        if (IsAnimating || !CanTouch)
        {
            return;
        }

        if (Board.Instance == null || Board.Instance.Cells == null)
        {
            return;
        }

        var cells = Board.Instance.Cells;
        int width = cells.GetLength(0);
        int height = cells.GetLength(1);

        HashSet<ButtonStack> candidateStacks = new HashSet<ButtonStack>();
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                GridCell cell = cells[x, z];
                if (cell == null || !cell.IsOccupied || cell.Stack == null || cell.Stack.Buttons == null)
                {
                    continue;
                }

                var stack = cell.Stack;
                if (stack.IsMoving || stack.IsMerging)
                {
                    continue;
                }

                if (stack.Buttons.Count < 2)
                {
                    continue;
                }

                bool hasSecondColor = false;
                ObjectColor firstColor = default;
                bool hasFirstColor = false;
                for (int i = 0; i < stack.Buttons.Count; i++)
                {
                    var btn = stack.Buttons[i];
                    if (btn == null) continue;

                    if (!hasFirstColor)
                    {
                        firstColor = btn.Color;
                        hasFirstColor = true;
                        continue;
                    }

                    if (btn.Color != firstColor)
                    {
                        hasSecondColor = true;
                        break;
                    }
                }

                if (hasSecondColor)
                {
                    candidateStacks.Add(stack);
                }
            }
        }

        if (candidateStacks.Count == 0)
        {
            return;
        }

        SaveSnapshot();

        HashSet<GridCell> affectedCells = new HashSet<GridCell>();

        foreach (var targetStack in candidateStacks)
        {
            if (targetStack == null || targetStack.Buttons == null || targetStack.Buttons.Count < 2)
            {
                continue;
            }

            List<ObjectColor> colors = new List<ObjectColor>();
            for (int i = 0; i < targetStack.Buttons.Count; i++)
            {
                var btn = targetStack.Buttons[i];
                if (btn == null) continue;

                var c = btn.Color;
                bool exists = false;
                for (int k = 0; k < colors.Count; k++)
                {
                    if (colors[k] == c)
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    colors.Add(c);
                }
            }

            if (colors.Count < 2)
            {
                continue;
            }

            List<ObjectColor> shuffled = new List<ObjectColor>(colors);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            bool isIdentity = true;
            for (int i = 0; i < colors.Count; i++)
            {
                if (shuffled[i] != colors[i])
                {
                    isIdentity = false;
                    break;
                }
            }

            if (isIdentity)
            {
                (shuffled[0], shuffled[1]) = (shuffled[1], shuffled[0]);
            }

            Dictionary<ObjectColor, List<ButtonCircle>> colorButtons = new Dictionary<ObjectColor, List<ButtonCircle>>();
            for (int i = 0; i < targetStack.Buttons.Count; i++)
            {
                var btn = targetStack.Buttons[i];
                if (btn == null) continue;

                if (!colorButtons.TryGetValue(btn.Color, out var list))
                {
                    list = new List<ButtonCircle>();
                    colorButtons[btn.Color] = list;
                }

                list.Add(btn);
            }

            List<ButtonCircle> newButtons = new List<ButtonCircle>(targetStack.Buttons.Count);
            for (int i = 0; i < shuffled.Count; i++)
            {
                if (colorButtons.TryGetValue(shuffled[i], out var list))
                {
                    newButtons.AddRange(list);
                }
            }

            targetStack.Buttons.Clear();
            for (int i = 0; i < newButtons.Count; i++)
            {
                if (newButtons[i] != null)
                {
                    targetStack.Buttons.Add(newButtons[i]);
                }
            }

            for (int i = 0; i < targetStack.Buttons.Count; i++)
            {
                var btn = targetStack.Buttons[i];
                if (btn == null) continue;

                btn.transform.SetParent(targetStack.transform);
                btn.transform.localPosition = Vector3.up * (i + 1) * Board.Instance.Spacing;
            }

            if (targetStack.Cell != null)
            {
                affectedCells.Add(targetStack.Cell);
            }
        }

        foreach (var cell in affectedCells)
        {
            StackPlaceCallBack(cell);
        }
        ProcessPendingMerges();
    }

    public void UndoLastMove()
    {
        if (_snapshots.Count == 0)
        {
            return;
        }

        if (IsAnimating)
        {
            return;
        }

        StopAllCoroutines();
        _placeTween?.Kill();
        _placeTween = null;

        _isDragging = false;
        _desiredMove = Vector3.zero;

        if (_currentStack != null)
        {
            _currentStack.Highlight = false;
            _currentStack = null;
        }

        _pendingMerges.Clear();
        _cellsInMerging.Clear();
        ActiveMergeCount = 0;
        IsCollecting = false;

        GameSnapshot snapshot = _snapshots.Pop();

        Board.Instance.ApplySnapshot(snapshot.Board);
        Tray.Instance.ApplySnapshot(snapshot.Tray);
        ContainerManager.Instance.ApplySnapshot(snapshot.ContainerManager);

        GameManagerInGame.Instance.CurrentGameStateInGame = GameStateInGame.Playing;
        CanTouch = true;
    }

    public void CheckLoseByNoValidMove()
    {
        if (GameManagerInGame.Instance.CurrentGameStateInGame == GameStateInGame.Result) return;
        if (IsLoseByNoValidMove())
        {
            int boardOccupied = 0;
            int boardButtons = 0;
            if (Board.Instance != null && Board.Instance.Cells != null)
            {
                foreach (var c in Board.Instance.Cells)
                {
                    if (c == null || !c.IsOccupied || c.Stack == null || c.Stack.Buttons == null) continue;
                    boardOccupied++;
                    boardButtons += c.Stack.Buttons.Count;
                }
            }

            int trayOccupied = 0;
            int trayTotal = 0;
            int trayMatchCount = 0;
            var container = ContainerManager.Instance != null ? ContainerManager.Instance.CurrentContainer : null;
            var containerColor = container != null ? container.Color : default;
            if (Tray.Instance != null)
            {
                trayTotal = Tray.Instance.TotalSlot;
                if (Tray.Instance.Slots != null)
                {
                    for (int i = 0; i < trayTotal && i < Tray.Instance.Slots.Length; i++)
                    {
                        var slot = Tray.Instance.Slots[i];
                        if (slot != null && slot.IsOccupied && slot.ButtonInSlot != null)
                        {
                            trayOccupied++;
                            if (slot.ButtonInSlot.Color == containerColor) trayMatchCount++;
                        }
                    }
                }
            }

            // int containerTotal = ContainerManager.Instance != null && ContainerManager.Instance.Containers != null ? ContainerManager.Instance.Containers.Count : 0;
            // int gateCount = Board.Instance != null ? Board.Instance.GateCount : 0;
            // bool hasActivatableGateMove = Board.Instance != null && Board.Instance.HasAnyActivatableGateMoveForLoseCheck();
            // bool hasGateProgressPotential = Board.Instance != null && Board.Instance.HasAnyGateProgressPotentialForLoseCheck();

            // var containerManager = ContainerManager.Instance;
            // var currentContainer = containerManager != null ? containerManager.CurrentContainer : null;
            // bool isContainerReady = containerManager != null && containerManager.IsContainerReady;
            // int containerEmptySlots = currentContainer != null ? currentContainer.EmptySlotCount : -1;
            // bool canCollectToContainer = containerManager != null && isContainerReady && currentContainer != null && containerEmptySlots >= Defines.COLLECT_COUNT;

            // StringBuilder boardStacks = new StringBuilder();
            // if (Board.Instance != null && Board.Instance.Cells != null)
            // {
            //     foreach (var c in Board.Instance.Cells)
            //     {
            //         if (c == null || !c.IsOccupied || c.Stack == null || c.Stack.TopButton == null) continue;
            //         var s = c.Stack;
            //         int topCount = 0;
            //         var tops = s.TopColors();
            //         if (tops != null) topCount = tops.Count;
            //         bool locked = Board.Instance.IsCellLocked(c);
            //         bool frozen = Board.Instance.IsCellFrozen(c);
            //         bool screwed = Board.Instance.IsCellScrewed(c);
            //         bool gate = Board.Instance.IsGateCell(c);
            //         boardStacks.Append($"[pos={c.Position} top={s.TopButton.Color} topCount={topCount} can={s.CanInteraction} moving={s.IsMoving} merging={s.IsMerging} locked={locked} frozen={frozen} screwed={screwed} gate={gate}] ");
            //     }
            // }

            // StringBuilder selectedStack = new StringBuilder();
            // if (_currentStack != null)
            // {
            //     int selectedTopCount = 0;
            //     var selectedTops = _currentStack.TopColors();
            //     if (selectedTops != null) selectedTopCount = selectedTops.Count;
            //     selectedStack.Append($"[dragging={_isDragging} sourcePos={(_dragSourceCell != null ? _dragSourceCell.Position.ToString() : "null")} top={(_currentStack.TopButton != null ? _currentStack.TopButton.Color.ToString() : "null")} topCount={selectedTopCount} can={_currentStack.CanInteraction} moving={_currentStack.IsMoving} merging={_currentStack.IsMerging}]");
            // }

            // Debug.Log($"[LoseCheck] reason=NoValidMove boardOccupied={boardOccupied} boardButtons={boardButtons} trayOccupied={trayOccupied}/{trayTotal} trayMatchCount={trayMatchCount} containerColor={containerColor} currentIndex={(containerManager != null ? containerManager._currentIndex : -1)} containerTotal={containerTotal} isContainerReady={isContainerReady} containerEmptySlots={containerEmptySlots} canCollectToContainer={canCollectToContainer} gateCount={gateCount} hasActivatableGateMove={hasActivatableGateMove} hasGateProgressPotential={hasGateProgressPotential} isDragging={_isDragging} hasSelectedStack={_currentStack != null} selectedStack={selectedStack} boardStacks={boardStacks}");
            GameUI.Instance.Get<UITopInGame>().ShowTrayNotificationLose(1.2f, "No valid moves left!");
            GameManagerInGame.Instance.SetLose();
            DOVirtual.DelayedCall(2f, () =>
            {
                GameUI.Instance.Get<UILose>().Show();
            });
        }
    }

    void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            if (_isDragging)
            {
                OnTouchEnded();
            }
        }
    }

    public void AddSlotButton()
    {
        Tray.Instance.AddSlots(10, true);
    }


}

[System.Serializable]
public class GameSnapshot
{
    public BoardSnapshot Board;
    public TraySnapshot Tray;
    public ContainerManagerSnapshot ContainerManager;
}