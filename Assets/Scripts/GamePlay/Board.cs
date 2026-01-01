using System;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.Utilities;
using UnityEngine;

public class Board : Singleton<Board>
{
    public Action OnBoardButtonsEmpty;

    [SerializeField] private ButtonCircle _buttonPrefab;
    [SerializeField] private ButtonStack _stackPrefab;
    [SerializeField] private GridCell _cellPrefab;
    [SerializeField] private LockOverlay _lockOverlayPrefab;
    [SerializeField] private IceOverlay _iceOverlayPrefab;
    [SerializeField] private ParticleSystem _iceBreakVFXPrefab;
    [SerializeField] private ScrewOverlay _screwOverlayPrefab;
    [SerializeField] private ParticleSystem _screwBreakVFXPrefab;
    [SerializeField] private GateOverlay _gateOverlayPrefab;
    [SerializeField] private ParticleSystem _gateDepletedVFXPrefab;
    [SerializeField] private ParticleSystem _cellClearedVFXPrefab;
    [SerializeField] private List<CellClearedVfxByColor> _cellClearedVfxByColor;
    [SerializeField] private GameColorConfig _colorConfig;
    [SerializeField] private float _hexSize;
    [SerializeField] private float _paddingCamera;
    [SerializeField] private float _spacingButton;
    [HideInInspector] public GridCell[,] Cells;
    public float Spacing => _spacingButton;
    public GameColorConfig ColorConfig => _colorConfig;
    public ButtonCircle ButtonPrefab => _buttonPrefab;
    private LevelConfig _currentConfig;

    private readonly Dictionary<Vector2Int, LockCellRuntime> _lockCells = new();
    private readonly Dictionary<Vector2Int, LockOverlay> _lockOverlays = new();
    private readonly Dictionary<Vector2Int, int> _iceCells = new();
    private readonly Dictionary<Vector2Int, IceOverlay> _iceOverlays = new();
    private readonly Dictionary<Vector2Int, int> _screwCells = new();
    private readonly Dictionary<Vector2Int, ScrewOverlay> _screwOverlays = new();
    private readonly Dictionary<Vector2Int, HexEdge> _gateCells = new();
    private readonly Dictionary<Vector2Int, GateOverlay> _gateOverlays = new();
    private readonly Dictionary<Vector2Int, int> _gateNextWaveIndex = new();
    private readonly Dictionary<Vector2Int, int> _gateTargetReservations = new();
    private int _collectedTotal;
    private readonly Dictionary<ObjectColor, int> _collectedByColor = new();

    private bool _boardButtonsEmptyNotified;

    private const int SCREW_HP_MAX = 4;

    public int GetTotalButtonsOnBoard()
    {
        if (Cells == null) return 0;

        int width = Cells.GetLength(0);
        int height = Cells.GetLength(1);
        int total = 0;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                var cell = Cells[x, z];
                if (cell == null || !cell.IsOccupied || cell.Stack == null || cell.Stack.Buttons == null) continue;
                total += cell.Stack.Buttons.Count;
            }
        }

        return total;
    }

    private void CheckNotifyBoardButtonsEmpty()
    {
        if (_boardButtonsEmptyNotified) return;
        if (GetTotalButtonsOnBoard() != 0) return;
        _boardButtonsEmptyNotified = true;
        // Debug.Log("[Board] Buttons empty (total Buttons.Count == 0)");
        Time.timeScale = 1.2f;
        OnBoardButtonsEmpty?.Invoke();
    }

    private int GetStackCount(GridCell cell)
    {
        if (cell == null || cell.Stack == null || cell.Stack.Buttons == null)
        {
            return 0;
        }
        return cell.Stack.Buttons.Count;
    }

    public int GateCount => _gateCells != null ? _gateCells.Count : 0;

    public bool IsGateCell(GridCell cell)
    {
        if (cell == null) return false;
        return _gateCells.ContainsKey(cell.Position);
    }

    public bool HasAnyActivatableGateMoveForLoseCheck()
    {
        if (_gateCells == null || _gateCells.Count == 0) return false;
        if (Cells == null) return false;

        foreach (var kv in _gateCells)
        {
            var gatePos = kv.Key;
            var dir = kv.Value;
            if (dir == HexEdge.None) continue;

            var gateCell = TryGetCell(gatePos);
            if (gateCell == null || !gateCell.IsOccupied || gateCell.Stack == null) continue;

            var stack = gateCell.Stack;
            if (!stack.CanInteraction || stack.IsMoving || stack.IsMerging) continue;

            Vector2Int nextPos = GetForwardPosition(gatePos, dir);
            if (nextPos == gatePos) continue;

            if (nextPos.x < 0 || nextPos.x >= Cells.GetLength(0) || nextPos.y < 0 || nextPos.y >= Cells.GetLength(1))
            {
                continue;
            }

            var target = Cells[nextPos.x, nextPos.y];
            if (!IsBlockedGateTarget(target))
            {
                return true;
            }
        }

        return false;
    }

    public bool HasAnyGateProgressPotentialForLoseCheck()
    {
        if (_gateCells == null || _gateCells.Count == 0) return false;
        if (Cells == null) return false;
        if (HasAnyActivatableGateMoveForLoseCheck()) return true;

        bool hasAnyFreeCell = false;
        for (int x = 0; x < Cells.GetLength(0) && !hasAnyFreeCell; x++)
        {
            for (int y = 0; y < Cells.GetLength(1); y++)
            {
                var c = Cells[x, y];
                if (c == null) continue;
                if (c.IsOccupied) continue;
                if (IsReservedGateTarget(c.Position)) continue;
                if (IsCellLocked(c)) continue;
                if (IsCellFrozen(c)) continue;
                if (IsCellScrewed(c)) continue;
                hasAnyFreeCell = true;
                break;
            }
        }

        if (!hasAnyFreeCell) return false;

        for (int x = 0; x < Cells.GetLength(0); x++)
        {
            for (int y = 0; y < Cells.GetLength(1); y++)
            {
                var cell = Cells[x, y];
                if (cell == null || !cell.IsOccupied || cell.Stack == null) continue;
                if (cell.Stack.TopButton == null) continue;
                if (!cell.Stack.CanInteraction || cell.Stack.IsMoving || cell.Stack.IsMerging) continue;
                if (IsGateCell(cell)) continue;
                if (IsCellLocked(cell) || IsCellFrozen(cell) || IsCellScrewed(cell)) continue;
                return true;
            }
        }

        foreach (var kv in _gateCells)
        {
            var gatePos = kv.Key;
            var dir = kv.Value;
            if (dir == HexEdge.None) continue;

            var gateCell = TryGetCell(gatePos);
            if (gateCell == null || !gateCell.IsOccupied || gateCell.Stack == null) continue;

            Vector2Int nextPos = GetForwardPosition(gatePos, dir);
            if (nextPos == gatePos) continue;
            if (nextPos.x < 0 || nextPos.x >= Cells.GetLength(0) || nextPos.y < 0 || nextPos.y >= Cells.GetLength(1)) continue;

            var target = Cells[nextPos.x, nextPos.y];
            if (target == null) continue;
            if (IsReservedGateTarget(target.Position)) continue;
            if (IsCellLocked(target)) continue;
            if (IsCellFrozen(target)) continue;
            if (IsCellScrewed(target)) continue;

            if (target.IsOccupied && target.Stack != null)
            {
                var s = target.Stack;
                if (s.CanInteraction && !s.IsMoving && !s.IsMerging)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void TryActivateIncomingGates(GridCell targetCell)
    {
        if (targetCell == null) return;
        if (targetCell.IsOccupied) return;
        if (_gateCells == null || _gateCells.Count == 0) return;
        if (targetCell.Neighbors == null || targetCell.Neighbors.Count == 0) return;

        for (int i = 0; i < targetCell.Neighbors.Count; i++)
        {
            var gateCell = targetCell.Neighbors[i];
            if (gateCell == null || !gateCell.IsOccupied || gateCell.Stack == null) continue;
            if (!_gateCells.TryGetValue(gateCell.Position, out var dir)) continue;
            if (dir == HexEdge.None) continue;

            var forward = GetForwardPosition(gateCell.Position, dir);
            if (forward != targetCell.Position) continue;

            TryActivateGate(gateCell);
        }
    }

    private void InitScrewFromConfig()
    {
        if (_currentConfig == null || _currentConfig.GridCellData == null || Cells == null)
        {
            return;
        }

        int width = Cells.GetLength(0);
        int height = Cells.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                GridCell cell = Cells[x, z];
                if (cell == null) continue;

                var data = _currentConfig.GridCellData[x, z];
                if (data == null) continue;

                if (data.ElementType == CellElementType.Screw)
                {
                    int hp = Mathf.Clamp(data.ScrewHitPoints, 0, SCREW_HP_MAX);
                    if (hp <= 0) continue;

                    _screwCells[cell.Position] = hp;
                }
            }
        }
    }

    public void UpdateGateOverlay(Vector2Int pos)
    {
        if (!_gateOverlays.TryGetValue(pos, out var overlay) || overlay == null)
        {
            return;
        }

        var cell = TryGetCell(pos);
        overlay.SetStackCount(GetStackCount(cell));
    }

    private void HandleGateDepleted(Vector2Int gatePos, GridCell gateCell)
    {
        if (_gateCells == null || !_gateCells.ContainsKey(gatePos))
        {
            return;
        }

        if (gateCell == null || gateCell.IsOccupied)
        {
            return;
        }

        _gateCells.Remove(gatePos);
        _gateNextWaveIndex.Remove(gatePos);

        if (_gateDepletedVFXPrefab != null)
        {
            ParticleSystem vfx = null;
            if (PoolManager.Instance != null)
            {
                vfx = PoolManager.Instance.Get(_gateDepletedVFXPrefab, gateCell.transform.position);
            }
            else
            {
                vfx = Instantiate(_gateDepletedVFXPrefab, gateCell.transform.position, Quaternion.identity);
            }

            if (vfx != null)
            {
                vfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                vfx.Play();
                DOVirtual.DelayedCall(2f, () =>
                {
                    if (vfx != null)
                    {
                        vfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        vfx.gameObject.SetActive(false);
                    }
                });
            }
        }

        var gameObject = gateCell.GetComponentInChildren<GateOverlay>()._gateOverlayPrefab;
       
        gameObject.transform.DOScale(Vector3.zero, 0.75f).SetEase(Ease.InBack).OnComplete(() =>
        {
            if (gateCell != null)
            {
                gateCell.SetActiveMeshRenderer(false);
            }
        });

    }

    private void TriggerGatesForOccupiedCells()
    {
        if (GameController.Instance == null) return;
        if (_gateCells == null || _gateCells.Count == 0) return;

        var gatePositions = new List<Vector2Int>(_gateCells.Keys);
        for (int i = 0; i < gatePositions.Count; i++)
        {
            var cell = TryGetCell(gatePositions[i]);
            if (cell == null || !cell.IsOccupied || cell.Stack == null) continue;
            GameController.Instance.StackPlaceCallBack(cell);
        }
    }

    private class LockCellRuntime
    {
        public CellElementType Type;
        public int Required;
        public ObjectColor TargetColor;
    }

    private static readonly HexEdge[] _edgeTypes = new HexEdge[]
    {
        HexEdge.TopRight,
        HexEdge.Top,
        HexEdge.TopLeft,
        HexEdge.BottomLeft,
        HexEdge.Bottom,
        HexEdge.BottomRight
    };
    private readonly Vector2Int[] _evenColOffsets = new Vector2Int[]
{
    new(+1,  0), // 0 TopRight
    new( 0, -1), // 1 Top
    new(-1,  0), // 2 TopLeft
    new(-1, +1), // 3 BottomLeft
    new( 0, +1), // 4 Bottom
    new(+1, +1), // 5 BottomRight
};

    private readonly Vector2Int[] _oddColOffsets = new Vector2Int[]
    {
    new(+1, -1), // 0 TopRight
    new( 0, -1), // 1 Top
    new(-1, -1), // 2 TopLeft
    new(-1,  0), // 3 BottomLeft
    new( 0, +1), // 4 Bottom
    new(+1,  0), // 5 BottomRight
    };

    void SetupCamera()
    {
        // int width = _currentConfig.Columns;
        // int height = _currentConfig.Rows;

        // float hexWidth = _hexSize * 2f;
        // float hexHeight = Mathf.Sqrt(3f) * _hexSize;
        // float horizSpacing = hexWidth * 0.75f;
        // float vertSpacing = hexHeight;

        // float boardWidth = (width - 1) * horizSpacing + hexWidth;
        // float extraHeight = width > 1 ? (vertSpacing * 0.5f) : 0f;
        // float boardHeight = (height - 1) * vertSpacing + hexHeight + extraHeight;

        // float sizeByHeight = (boardHeight * 0.5f) + _paddingCamera;
        // float sizeByWidth = ((boardWidth * 0.5f) + _paddingCamera) / Camera.main.aspect;
        // Camera.main.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);

        int width = _currentConfig.GetColumns();
        int height = _currentConfig.GetRows();

        if (height <= 5)
        {
            transform.localPosition = new Vector3(0, 0, 1.5f);
            Camera.main.orthographicSize = 13f;
        }else if (height < 8)
        {
            transform.localPosition = new Vector3(0, 0, 0f);
            Camera.main.orthographicSize = 13f;
        }else if (height < 12)
        {
            transform.localPosition = new Vector3(0, 0, -1f);
            Camera.main.orthographicSize = 14f;
        }else if (height < 16)
        {
            transform.localPosition = new Vector3(0, 0, -2f);
            Camera.main.orthographicSize = 15f;
        }
    }

    private void SetupGrid()
    {
        Cells = new GridCell[_currentConfig.GetColumns(), _currentConfig.GetRows()];
        int Width = _currentConfig.GetColumns();
        int Height = _currentConfig.GetRows();

        float hexWidth = _hexSize * 2f;
        float hexHeight = Mathf.Sqrt(3f) * _hexSize;
        float horizSpacing = hexWidth * 0.75f;
        float vertSpacing = hexHeight;

        float totalWidth = (Width - 1) * horizSpacing + hexWidth;
        float totalHeight = (Height - 1) * vertSpacing + hexHeight;

        Vector3 gridOffset = new Vector3(totalWidth / 2f - hexWidth / 2f, 0f, totalHeight / 2f - hexHeight / 2f);
        for (int x = 0; x < Width; x++)
        {
            for (int z = 0; z < Height; z++)
            {
                if (_currentConfig.GridCellData[x, z] != null && _currentConfig.GridCellData[x, z].IsEmpty) continue;


                float xPos = x * horizSpacing;
                float zPos = (Height - 1 - z) * vertSpacing;

                if (x % 2 == 1)
                {
                    zPos += vertSpacing / 2f;
                }

                Vector3 localPos = new Vector3(xPos, 0f, zPos) - gridOffset;
                GridCell cell = Instantiate(_cellPrefab, transform);
                cell.transform.localPosition = localPos;
                cell.Position = new Vector2Int(x, z);
                Cells[x, z] = cell;
                if (_currentConfig.GridCellData[x, z] != null && !_currentConfig.GridCellData[x, z].Colors.IsNullOrEmpty())
                {
                    ButtonStack btnStack = Instantiate(_stackPrefab);
                    btnStack.transform.position = cell.transform.position;

                    for (int i = 0; i < _currentConfig.GridCellData[x, z].Colors.Count; i++)
                    {
                        Vector3 hexagonLocalPos = Vector3.up * (i + 1) * _spacingButton;
                        Vector3 spawnPos = btnStack.transform.TransformPoint(hexagonLocalPos);
                        ButtonCircle hexagon = Instantiate(_buttonPrefab, spawnPos, Quaternion.identity, btnStack.transform);
                        hexagon.SetColor(_currentConfig.GridCellData[x, z].Colors[i].Color);
                        btnStack.AddButton(hexagon);
                    }
                    btnStack.Configure(cell);
                }

                if (_currentConfig.GridCellData[x, z] != null)
                {
                    InitGateWaveStateFromConfig(cell.Position);
                }
                cell.name = $"Cell_{x}_{z}";
            }
        }
        CenterPivotGrid();
        CacheAllNeighbors();
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
    private HexEdge[,] GetHexEdges()
    {
        int width = Cells.GetLength(0);
        int height = Cells.GetLength(1);
        HexEdge[,] result = new HexEdge[width, height];

        HexEdge[] edgeTypes = new HexEdge[]
        {
        HexEdge.TopRight,    // 0
        HexEdge.Top,         // 1
        HexEdge.TopLeft,     // 2
        HexEdge.BottomLeft,  // 3
        HexEdge.Bottom,      // 4
        HexEdge.BottomRight, // 5
        };

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                if (GetCellAt(new Vector2Int(x, z)) == null)
                    continue;

                HexEdge edge = HexEdge.None;

                var offsets = GetCorrectOffsets(x);

                for (int i = 0; i < offsets.Length; i++)
                {
                    int nx = x + offsets[i].x;
                    int nz = z + offsets[i].y;

                    if (nx < 0 || nx >= width || nz < 0 || nz >= height ||
                        GetCellAt(new Vector2Int(nx, nz)) == null)
                    {
                        edge |= edgeTypes[i];
                    }
                }

                result[x, z] = edge;
            }
        }

        return result;
    }

    private void CacheAllNeighbors()
    {
        int width = Cells.GetLength(0);
        int height = Cells.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                GridCell cell = GetCellAt(new Vector2Int(x, z));
                if (cell == null) continue;

                var offsets = GetCorrectOffsets(x);

                List<GridCell> neighbors = new List<GridCell>(6);
                foreach (var off in offsets)
                {
                    int nx = x + off.x;
                    int nz = z + off.y;

                    if (nx < 0 || nx >= width || nz < 0 || nz >= height) continue;

                    GridCell neighbor = GetCellAt(new Vector2Int(nx, nz));
                    if (neighbor != null && neighbor != cell)
                    {
                        neighbors.Add(neighbor);
                    }
                }

                cell.Neighbors = new List<GridCell>(neighbors);
            }
        }
    }

    private void UpdateScrewOverlay(Vector2Int pos, int hp)
    {
        if (_screwOverlays.TryGetValue(pos, out var overlay) && overlay != null)
        {
            if (hp <= 0)
            {
                overlay.SetHP(0);
                Destroy(overlay.gameObject, 0.35f);
                _screwOverlays.Remove(pos);
            }
            else
            {
                var cell = TryGetCell(pos);
                int stackCount = 0;
                if (cell != null && cell.Stack != null && cell.Stack.Buttons != null)
                {
                    stackCount = cell.Stack.Buttons.Count;
                }
                overlay.SetInfo(hp, stackCount, GetIceOverlayLocalY(cell));
            }
        }
        else
        {
            if (hp > 0 && _screwOverlayPrefab != null)
            {
                var cell = TryGetCell(pos);
                if (cell != null)
                {
                    var newOverlay = Instantiate(_screwOverlayPrefab, cell.transform);
                    float localY = GetIceOverlayLocalY(cell);
                    int stackCount = 0;
                    if (cell.Stack != null && cell.Stack.Buttons != null)
                    {
                        stackCount = cell.Stack.Buttons.Count;
                    }
                    newOverlay.SetInfo(hp, stackCount, localY);
                    _screwOverlays[pos] = newOverlay;
                }
            }
        }
    }

    public Vector2Int[] GetCorrectOffsets(int x)
    {
        bool isOddColumn = x % 2 == 1;
        return isOddColumn ? _oddColOffsets : _evenColOffsets;
    }
    private void SetUpHexFrame()
    {
        HexEdge[,] edgeInfo = GetHexEdges();

        for (int x = 0; x < edgeInfo.GetLength(0); x++)
        {
            for (int z = 0; z < edgeInfo.GetLength(1); z++)
            {
                GridCell cell = GetCellAt(new Vector2Int(x, z));
                if (cell == null)
                    continue;

                HexEdge edge = edgeInfo[x, z];
                cell.ShowHexEdges(edge);
            }
        }
    }
    private void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        foreach (var kv in _lockOverlays)
        {
            if (kv.Value != null)
            {
                Destroy(kv.Value.gameObject);
            }
        }
        _lockOverlays.Clear();

        foreach (var kv in _iceOverlays)
        {
            if (kv.Value != null)
            {
                Destroy(kv.Value.gameObject);
            }
        }
        _iceOverlays.Clear();

        foreach (var kv in _screwOverlays)
        {
            if (kv.Value != null)
            {
                Destroy(kv.Value.gameObject);
            }
        }
        _screwOverlays.Clear();

        foreach (var kv in _gateOverlays)
        {
            if (kv.Value != null)
            {
                Destroy(kv.Value.gameObject);
            }
        }
        _gateOverlays.Clear();
    }

    private void InitGatesFromConfig()
    {
        if (_currentConfig == null || _currentConfig.GridCellData == null || Cells == null)
        {
            return;
        }

        int width = Cells.GetLength(0);
        int height = Cells.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                GridCell cell = Cells[x, z];
                if (cell == null) continue;

                var data = _currentConfig.GridCellData[x, z];
                if (data == null) continue;

                if (data.ElementType == CellElementType.Gate)
                {
                    if (data.GateDirection != HexEdge.None)
                    {
                        _gateCells[cell.Position] = data.GateDirection;
                    }

                    if (!data.GateWaves.IsNullOrEmpty() && !_gateNextWaveIndex.ContainsKey(cell.Position))
                    {
                        _gateNextWaveIndex[cell.Position] = 0;
                    }
                }
            }
        }
    }

    private void RebuildGateOverlays()
    {
        foreach (var kv in _gateOverlays)
        {
            if (kv.Value != null)
            {
                Destroy(kv.Value.gameObject);
            }
        }
        _gateOverlays.Clear();

        if (_gateOverlayPrefab == null) return;

        foreach (var kv in _gateCells)
        {
            var cell = TryGetCell(kv.Key);
            if (cell == null) continue;

            var overlay = Instantiate(_gateOverlayPrefab, cell.transform);
            overlay.transform.localPosition = Vector3.up * 0.05f;
            overlay.SetDirection(kv.Value);
            overlay.SetStackCount(GetStackCount(cell));
            _gateOverlays[kv.Key] = overlay;
        }
    }

    private Vector2Int GetForwardPosition(Vector2Int pos, HexEdge dir)
    {
        var offsets = GetCorrectOffsets(pos.x);
        for (int i = 0; i < _edgeTypes.Length; i++)
        {
            if (_edgeTypes[i] == dir)
            {
                return pos + offsets[i];
            }
        }
        return pos;
    }

    private bool IsBlockedGateTarget(GridCell cell)
    {
        if (cell == null) return true;
        if (IsReservedGateTarget(cell.Position)) return true;
        if (cell.IsOccupied) return true;
        if (IsCellLocked(cell)) return true;
        if (IsCellFrozen(cell)) return true;
        if (IsCellScrewed(cell)) return true;
        return false;
    }

    public bool IsReservedGateTarget(Vector2Int pos)
    {
        return _gateTargetReservations.TryGetValue(pos, out int c) && c > 0;
    }

    private void ReserveGateTarget(Vector2Int pos)
    {
        if (_gateTargetReservations.TryGetValue(pos, out int c))
        {
            _gateTargetReservations[pos] = c + 1;
        }
        else
        {
            _gateTargetReservations[pos] = 1;
        }
    }

    private void ReleaseGateTarget(Vector2Int pos)
    {
        if (!_gateTargetReservations.TryGetValue(pos, out int c)) return;

        c -= 1;
        if (c <= 0)
        {
            _gateTargetReservations.Remove(pos);
        }
        else
        {
            _gateTargetReservations[pos] = c;
        }
    }

    public void TryActivateGate(GridCell gridCell)
    {
        if (gridCell == null || gridCell.Stack == null) return;
        if (!_gateCells.TryGetValue(gridCell.Position, out var dir)) return;
        if (dir == HexEdge.None) return;

        var gatePos = gridCell.Position;

        Vector2Int nextPos = GetForwardPosition(gridCell.Position, dir);
        if (nextPos == gridCell.Position) return;

        GridCell target = null;
        if (Cells != null && nextPos.x >= 0 && nextPos.x < Cells.GetLength(0) && nextPos.y >= 0 && nextPos.y < Cells.GetLength(1))
        {
            target = Cells[nextPos.x, nextPos.y];
        }

        if (IsBlockedGateTarget(target))
        {
            Vector3 dirWorld = (target != null ? (target.transform.position - gridCell.transform.position) : Vector3.right);
            dirWorld.y = 0;
            if (dirWorld.sqrMagnitude < 0.001f) dirWorld = Vector3.right;
            dirWorld.Normalize();
            gridCell.Stack.transform.DOPunchPosition(dirWorld * 0.25f, 0.18f, 8, 0.8f);
            return;
        }

        var stack = gridCell.Stack;
        stack.SkipGateOnce = true;
        stack.CanInteraction = false;
        stack.IsMoving = true;

        var targetPos = target.Position;
        ReserveGateTarget(targetPos);

        gridCell.AssignStack(null);
        UpdateGateOverlay(gridCell.Position);
        stack.Configure(null);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[Gate] ActivateGate move stack out. gatePos={gatePos} dir={dir} targetPos={targetPos}");
#endif

        stack.transform.DOMove(target.transform.position, 0.15f)
        .OnKill(() =>
        {
            ReleaseGateTarget(targetPos);
        })
        .OnComplete(() =>
        {
            ReleaseGateTarget(targetPos);
            stack.Configure(target);
            UpdateGateOverlay(target.Position);
            stack.IsMoving = false;
            stack.CanInteraction = true;

            bool spawned = TrySpawnNextGateWave(gatePos);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Gate] ActivateGate complete. gatePos={gatePos} targetPos={targetPos} spawnedNextWave={spawned}");
#endif
            if (GameController.Instance != null)
            {
                GameController.Instance.StackPlaceCallBack(target);
            }

            if (!spawned)
            {
                var emptiedGateCell = TryGetCell(gatePos);
                if (emptiedGateCell != null && !emptiedGateCell.IsOccupied)
                {
                    HandleGateDepleted(gatePos, emptiedGateCell);
                    TryActivateIncomingGates(emptiedGateCell);
                }
            }
        });
    }

    private void InitGateWaveStateFromConfig(Vector2Int pos)
    {
        if (_currentConfig == null || _currentConfig.GridCellData == null) return;

        int width = _currentConfig.GridCellData.GetLength(0);
        int height = _currentConfig.GridCellData.GetLength(1);
        if (pos.x < 0 || pos.x >= width || pos.y < 0 || pos.y >= height) return;

        var data = _currentConfig.GridCellData[pos.x, pos.y];
        if (data == null) return;
        if (data.ElementType != CellElementType.Gate) return;
        if (data.GateWaves.IsNullOrEmpty()) return;

        if (!_gateNextWaveIndex.ContainsKey(pos))
        {
            _gateNextWaveIndex[pos] = 0;
        }

        TrySpawnNextGateWave(pos);
    }

    private bool TrySpawnNextGateWave(Vector2Int gatePos)
    {
        if (_currentConfig == null || _currentConfig.GridCellData == null) return false;

        var gateCell = TryGetCell(gatePos);
        if (gateCell == null || gateCell.IsOccupied) return false;

        int width = _currentConfig.GridCellData.GetLength(0);
        int height = _currentConfig.GridCellData.GetLength(1);
        if (gatePos.x < 0 || gatePos.x >= width || gatePos.y < 0 || gatePos.y >= height) return false;

        var data = _currentConfig.GridCellData[gatePos.x, gatePos.y];
        if (data == null || data.ElementType != CellElementType.Gate) return false;
        if (data.GateWaves.IsNullOrEmpty()) return false;

        if (!_gateNextWaveIndex.TryGetValue(gatePos, out int nextIndex))
        {
            nextIndex = 0;
        }

        while (nextIndex < data.GateWaves.Count)
        {
            var wave = data.GateWaves[nextIndex];
            nextIndex += 1;

            if (wave == null || wave.Colors.IsNullOrEmpty())
            {
                continue;
            }

            SpawnStackFromColors(gateCell, wave.Colors);
            _gateNextWaveIndex[gatePos] = nextIndex;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Gate] SpawnNextWave. gatePos={gatePos} spawnedWaveIndex={nextIndex - 1} nextIndex={nextIndex}/{data.GateWaves.Count} colors={wave.Colors.Count}");
#endif

            if (GameController.Instance != null)
            {
                GameController.Instance.StackPlaceCallBack(gateCell);
            }
            return true;
        }

        _gateNextWaveIndex[gatePos] = nextIndex;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[Gate] OutOfWaves. gatePos={gatePos} nextIndex={nextIndex}/{data.GateWaves.Count}");
#endif
        return false;
    }

    private void SpawnStackFromColors(GridCell cell, List<ObjectColor> colors)
    {
        if (cell == null) return;
        if (cell.IsOccupied) return;
        if (colors.IsNullOrEmpty()) return;
        if (_stackPrefab == null || _buttonPrefab == null) return;

        ButtonStack btnStack = Instantiate(_stackPrefab);
        btnStack.transform.position = cell.transform.position;

        for (int i = 0; i < colors.Count; i++)
        {
            Vector3 localPos = Vector3.up * (i + 1) * _spacingButton;
            Vector3 spawnPos = btnStack.transform.TransformPoint(localPos);
            ButtonCircle hexagon = Instantiate(_buttonPrefab, spawnPos, Quaternion.identity, btnStack.transform);
            hexagon.SetColor(colors[i]);
            btnStack.AddButton(hexagon);
        }
        btnStack.Configure(cell);
    }

    private void InitIceFromConfig()
    {
        if (_currentConfig == null || _currentConfig.GridCellData == null || Cells == null)
        {
            return;
        }

        int width = Cells.GetLength(0);
        int height = Cells.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                GridCell cell = Cells[x, z];
                if (cell == null) continue;

                var data = _currentConfig.GridCellData[x, z];
                if (data == null) continue;

                if (data.ElementType == CellElementType.Ice)
                {
                    int hp = Mathf.Max(0, data.IceHitPoints);
                    if (hp <= 0) continue;

                    _iceCells[cell.Position] = hp;
                }
            }
        }
    }

    private void RebuildIceVisuals()
    {
        if (Cells == null) return;

        int width = Cells.GetLength(0);
        int height = Cells.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                var cell = Cells[x, z];
                if (cell == null) continue;
                cell.SetIce(0);
            }
        }

        foreach (var kv in _iceCells)
        {
            var cell = TryGetCell(kv.Key);
            if (cell != null)
            {
                cell.SetIce(kv.Value);
            }
        }
    }

    private void RebuildScrewVisuals()
    {
        if (Cells == null) return;

        int width = Cells.GetLength(0);
        int height = Cells.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                var cell = Cells[x, z];
                if (cell == null) continue;
                cell.SetScrew(0);
            }
        }

        foreach (var kv in _screwCells)
        {
            var cell = TryGetCell(kv.Key);
            if (cell != null)
            {
                cell.SetScrew(kv.Value);
            }
        }
    }

    private void RebuildIceOverlays()
    {
        foreach (var kv in _iceOverlays)
        {
            if (kv.Value != null)
            {
                Destroy(kv.Value.gameObject);
            }
        }
        _iceOverlays.Clear();

        if (_iceOverlayPrefab == null) return;

        foreach (var kv in _iceCells)
        {
            if (kv.Value <= 0) continue;
            var cell = TryGetCell(kv.Key);
            if (cell == null) continue;

            var overlay = Instantiate(_iceOverlayPrefab, cell.transform);
            float localY = GetIceOverlayLocalY(cell);
            int stackCount = 0;
            if (cell.Stack != null && cell.Stack.Buttons != null)
            {
                stackCount = cell.Stack.Buttons.Count;
            }
            overlay.SetInfo(kv.Value, stackCount, localY);
            _iceOverlays[kv.Key] = overlay;
        }
    }

    private void RebuildScrewOverlays()
    {
        foreach (var kv in _screwOverlays)
        {
            if (kv.Value != null)
            {
                Destroy(kv.Value.gameObject);
            }
        }
        _screwOverlays.Clear();

        if (_screwOverlayPrefab == null) return;

        foreach (var kv in _screwCells)
        {
            if (kv.Value <= 0) continue;
            var cell = TryGetCell(kv.Key);
            if (cell == null) continue;

            var overlay = Instantiate(_screwOverlayPrefab, cell.transform);
            float localY = GetIceOverlayLocalY(cell);
            int stackCount = 0;
            if (cell.Stack != null && cell.Stack.Buttons != null)
            {
                stackCount = cell.Stack.Buttons.Count;
            }
            overlay.SetInfo(kv.Value, stackCount, localY);
            _screwOverlays[kv.Key] = overlay;
        }
    }

    private float GetIceOverlayLocalY(GridCell cell)
    {
        if (cell == null) return 0.1f;

        int count = 0;
        if (cell.Stack != null && cell.Stack.Buttons != null)
        {
            count = cell.Stack.Buttons.Count;
        }

        return (count) * _spacingButton;
    }

    private void UpdateIceOverlay(Vector2Int pos, int hp)
    {
        if (_iceOverlays.TryGetValue(pos, out var overlay) && overlay != null)
        {
            if (hp <= 0)
            {
                Destroy(overlay.gameObject);
                _iceOverlays.Remove(pos);
            }
            else
            {
                var cell = TryGetCell(pos);
                int stackCount = 0;
                if (cell != null && cell.Stack != null && cell.Stack.Buttons != null)
                {
                    stackCount = cell.Stack.Buttons.Count;
                }
                overlay.SetInfo(hp, stackCount, GetIceOverlayLocalY(cell));
            }
        }
        else
        {
            if (hp > 0 && _iceOverlayPrefab != null)
            {
                var cell = TryGetCell(pos);
                if (cell != null)
                {
                    var newOverlay = Instantiate(_iceOverlayPrefab, cell.transform);
                    float localY = GetIceOverlayLocalY(cell);
                    int stackCount = 0;
                    if (cell.Stack != null && cell.Stack.Buttons != null)
                    {
                        stackCount = cell.Stack.Buttons.Count;
                    }
                    newOverlay.SetInfo(hp, stackCount, localY);
                    _iceOverlays[pos] = newOverlay;
                }
            }
        }
    }

    private void ResetLockRuntime()
    {
        _lockCells.Clear();
        _collectedTotal = 0;
        _collectedByColor.Clear();

        _boardButtonsEmptyNotified = false;

        _iceCells.Clear();
        _screwCells.Clear();

        _gateCells.Clear();
        _gateNextWaveIndex.Clear();
        _gateTargetReservations.Clear();

        foreach (var kv in _gateOverlays)
        {
            if (kv.Value != null)
            {
                Destroy(kv.Value.gameObject);
            }
        }
        _gateOverlays.Clear();

        foreach (var kv in _lockOverlays)
        {
            if (kv.Value != null)
            {
                Destroy(kv.Value.gameObject);
            }
        }
        _lockOverlays.Clear();

        foreach (var kv in _iceOverlays)
        {
            if (kv.Value != null)
            {
                Destroy(kv.Value.gameObject);
            }
        }
        _iceOverlays.Clear();

        foreach (var kv in _screwOverlays)
        {
            if (kv.Value != null)
            {
                Destroy(kv.Value.gameObject);
            }
        }
        _screwOverlays.Clear();
    }

    private int GetLockProgress(LockCellRuntime state)
    {
        if (state == null) return 0;
        if (state.Type == CellElementType.LockItem)
        {
            return _collectedTotal;
        }
        if (state.Type == CellElementType.LockItemColor)
        {
            _collectedByColor.TryGetValue(state.TargetColor, out int c);
            return c;
        }
        return 0;
    }

    private int GetLockRemaining(LockCellRuntime state)
    {
        if (state == null) return 0;
        return Mathf.Max(0, state.Required - GetLockProgress(state));
    }

    private GridCell TryGetCell(Vector2Int pos)
    {
        if (Cells == null) return null;
        if (pos.x < 0 || pos.x >= Cells.GetLength(0) || pos.y < 0 || pos.y >= Cells.GetLength(1)) return null;
        return Cells[pos.x, pos.y];
    }

    private void RebuildLockOverlays()
    {
        if (Cells != null)
        {
            int width = Cells.GetLength(0);
            int height = Cells.GetLength(1);
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    if (Cells[x, z] != null)
                    {
                        Cells[x, z].SetLocked(false);
                    }
                }
            }
        }

        foreach (var kv in _lockOverlays)
        {
            if (kv.Value != null)
            {
                Destroy(kv.Value.gameObject);
            }
        }
        _lockOverlays.Clear();

        if (_lockOverlayPrefab == null) return;

        foreach (var kv in _lockCells)
        {
            var pos = kv.Key;
            var state = kv.Value;
            GridCell cell = TryGetCell(pos);
            if (cell == null) continue;

            cell.SetLocked(true, state.Type, state.Type == CellElementType.LockItemColor ? state.TargetColor : (ObjectColor?)null);

            var overlay = Instantiate(_lockOverlayPrefab, cell.transform);
            overlay.transform.localPosition = Vector3.up * 0.1f;
            overlay.SetCount(GetLockRemaining(state));
            _lockOverlays[pos] = overlay;
        }
    }

    public bool IsCellFrozen(GridCell cell)
    {
        if (cell == null) return false;
        return _iceCells.TryGetValue(cell.Position, out int hp) && hp > 0;
    }

    public bool IsCellScrewed(GridCell cell)
    {
        if (cell == null) return false;
        return _screwCells.TryGetValue(cell.Position, out int hp) && hp > 0;
    }

    public void OnStackClearedFromCell(GridCell sourceCell)
    {
        if (sourceCell == null || sourceCell.Neighbors == null || sourceCell.Neighbors.Count == 0) return;

        for (int i = 0; i < sourceCell.Neighbors.Count; i++)
        {
            var n = sourceCell.Neighbors[i];
            if (n == null) continue;

            if (_iceCells.TryGetValue(n.Position, out int hp) && hp > 0)
            {
                int newHp = Mathf.Max(0, hp - 1);
                if (newHp <= 0)
                {
                    _iceCells.Remove(n.Position);
                    if (_iceBreakVFXPrefab != null)
                    {
                        ParticleSystem vfx = null;
                        if (PoolManager.Instance != null)
                        {
                            vfx = PoolManager.Instance.Get(_iceBreakVFXPrefab, n.transform.position);
                        }
                        else
                        {
                            vfx = Instantiate(_iceBreakVFXPrefab, n.transform.position, Quaternion.identity);
                        }

                        if (vfx != null)
                        {
                            vfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                            vfx.Play();
                            DOVirtual.DelayedCall(2f, () =>
                            {
                                if (vfx != null)
                                {
                                    vfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                                    vfx.gameObject.SetActive(false);
                                }
                            });
                        }
                        AudioManager.Instance.PlaySFX(SFXType.BreakIce);
                    }
                }
                else
                {
                    _iceCells[n.Position] = newHp;
                }
                n.SetIce(newHp);
                UpdateIceOverlay(n.Position, newHp);
            }

            if (_screwCells.TryGetValue(n.Position, out int sHp) && sHp > 0)
            {
                int newHp = Mathf.Clamp(sHp - 1, 0, SCREW_HP_MAX);
                if (newHp <= 0)
                {
                    _screwCells.Remove(n.Position);
                    if (_screwBreakVFXPrefab != null)
                    {
                        ParticleSystem vfx = null;
                        if (PoolManager.Instance != null)
                        {
                            vfx = PoolManager.Instance.Get(_screwBreakVFXPrefab, n.transform.position);
                        }
                        else
                        {
                            vfx = Instantiate(_screwBreakVFXPrefab, n.transform.position, Quaternion.identity);
                        }

                        if (vfx != null)
                        {
                            vfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                            vfx.Play();
                            DOVirtual.DelayedCall(2f, () =>
                            {
                                if (vfx != null)
                                {
                                    vfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                                    vfx.gameObject.SetActive(false);
                                }
                            });
                        }
                    }
                }
                else
                {
                    _screwCells[n.Position] = newHp;
                }
                n.SetScrew(newHp);
                UpdateScrewOverlay(n.Position, newHp);
            }
        }
    }

    private bool TryGetCellClearedVfxColor(ObjectColor color, out Color vfxColor)
    {
        vfxColor = default;
        if (_cellClearedVfxByColor != null)
        {
            for (int i = 0; i < _cellClearedVfxByColor.Count; i++)
            {
                var e = _cellClearedVfxByColor[i];
                if (e == null) continue;
                if (e.Color == color)
                {
                    vfxColor = e.ColorVfx;
                    return true;
                }
            }
        }
        return false;
    }

    public void OnCellCleared(GridCell cell, ObjectColor color)
    {
        if (cell == null) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[Cell] Cleared pos={cell.Position}");
#endif

        if (_cellClearedVFXPrefab != null)
        {
            var vfx = PoolManager.Instance.Get(_cellClearedVFXPrefab, cell.transform.position);
            if (vfx != null && TryGetCellClearedVfxColor(color, out var vfxColor))
            {
                var main = vfx.main;
                main.startColor = vfxColor;
            }
            vfx.Play();
            DOVirtual.DelayedCall(1f, () =>
            {
                if (vfx != null)
                {
                    vfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    vfx.gameObject.SetActive(false);
                }
            });
        }

        CheckNotifyBoardButtonsEmpty();
    }

    private void UpdateLockOverlays()
    {
        if (_lockOverlays.Count == 0) return;

        foreach (var kv in _lockCells)
        {
            if (_lockOverlays.TryGetValue(kv.Key, out var overlay) && overlay != null)
            {
                overlay.SetCount(GetLockRemaining(kv.Value));
            }
        }
    }

    private void InitLocksFromConfig()
    {
        if (_currentConfig == null || _currentConfig.GridCellData == null || Cells == null)
        {
            return;
        }

        int width = Cells.GetLength(0);
        int height = Cells.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                GridCell cell = Cells[x, z];
                if (cell == null) continue;

                var data = _currentConfig.GridCellData[x, z];
                if (data == null) continue;

                if (data.ElementType == CellElementType.LockItem || data.ElementType == CellElementType.LockItemColor)
                {
                    int required = Mathf.Max(0, data.LockItemCount);
                    if (required <= 0) continue;

                    _lockCells[cell.Position] = new LockCellRuntime
                    {
                        Type = data.ElementType,
                        Required = required,
                        TargetColor = data.LockItemColor
                    };
                }
            }
        }
    }

    public bool IsCellLocked(GridCell cell)
    {
        if (cell == null) return false;
        return _lockCells.ContainsKey(cell.Position);
    }

    public void OnCollected(ObjectColor color, int amount = 1)
    {
        if (amount <= 0) return;

        _collectedTotal += amount;
        if (_collectedByColor.TryGetValue(color, out int prev))
        {
            _collectedByColor[color] = prev + amount;
        }
        else
        {
            _collectedByColor[color] = amount;
        }

        if (_lockCells.Count == 0) return;

        List<Vector2Int> unlocked = null;
        foreach (var kv in _lockCells)
        {
            var state = kv.Value;
            bool canUnlock = GetLockRemaining(state) <= 0;

            if (canUnlock)
            {
                unlocked ??= new List<Vector2Int>();
                unlocked.Add(kv.Key);
            }
        }

        if (unlocked == null) return;

        for (int i = 0; i < unlocked.Count; i++)
        {
            var pos = unlocked[i];
            _lockCells.Remove(pos);
            if (_lockOverlays.TryGetValue(pos, out var overlay) && overlay != null)
            {
                Destroy(overlay.gameObject);
            }
            _lockOverlays.Remove(pos);

            GridCell cell = TryGetCell(pos);
            if (cell != null)
            {
                cell.SetLocked(false);
            }
        }

        UpdateLockOverlays();
    }

    private void ApplyLockSnapshot(BoardSnapshot snapshot)
    {
        ResetLockRuntime();
        if (snapshot == null) return;

        _collectedTotal = snapshot.CollectedTotal;
        if (snapshot.CollectedByColor != null)
        {
            for (int i = 0; i < snapshot.CollectedByColor.Count; i++)
            {
                var e = snapshot.CollectedByColor[i];
                _collectedByColor[e.Color] = e.Count;
            }
        }

        if (snapshot.LockCells != null)
        {
            for (int i = 0; i < snapshot.LockCells.Count; i++)
            {
                var l = snapshot.LockCells[i];
                _lockCells[l.Position] = new LockCellRuntime
                {
                    Type = l.Type,
                    Required = l.Required,
                    TargetColor = l.TargetColor
                };
            }
        }

        if (snapshot.IceCells != null)
        {
            for (int i = 0; i < snapshot.IceCells.Count; i++)
            {
                var e = snapshot.IceCells[i];
                if (e.HP > 0)
                {
                    _iceCells[e.Position] = e.HP;
                }
            }
        }

        if (snapshot.ScrewCells != null)
        {
            for (int i = 0; i < snapshot.ScrewCells.Count; i++)
            {
                var e = snapshot.ScrewCells[i];
                if (e.HP > 0)
                {
                    _screwCells[e.Position] = Mathf.Clamp(e.HP, 0, SCREW_HP_MAX);
                }
            }
        }
    }
    public void SetupLevel(LevelConfig config)
    {
        GameManagerInGame.Instance.SetState(GameStateInGame.Init);

        Clear();
        Tray.Instance.Clear();
        ContainerManager.Instance.Clear();
        GameController.Instance.Clear();

        ResetLockRuntime();

        _currentConfig = config;
        SetupGrid();
        SetUpHexFrame();
        InitLocksFromConfig();
        InitIceFromConfig();
        InitScrewFromConfig();
        InitGatesFromConfig();
        RebuildLockOverlays();
        RebuildIceVisuals();
        RebuildIceOverlays();
        RebuildScrewVisuals();
        RebuildScrewOverlays();
        RebuildGateOverlays();
        Tray.Instance.Setup(_currentConfig.TotalSlot, false);
        SetupCamera();
        ContainerManager.Instance.SetupContainers(config.GetContainers());


        TriggerGatesForOccupiedCells();
        TutorialManager.Instance.TryShowTutorial(config.GetLevel());

        GameManagerInGame.Instance.SetState(GameStateInGame.Playing);
    }

    public BoardSnapshot CreateSnapshot()
    {
        BoardSnapshot snapshot = new BoardSnapshot();

        snapshot.CollectedTotal = _collectedTotal;
        snapshot.CollectedByColor = new List<ColorCountSnapshot>(_collectedByColor.Count);
        foreach (var kv in _collectedByColor)
        {
            snapshot.CollectedByColor.Add(new ColorCountSnapshot { Color = kv.Key, Count = kv.Value });
        }

        snapshot.LockCells = new List<LockCellSnapshot>(_lockCells.Count);
        foreach (var kv in _lockCells)
        {
            var v = kv.Value;
            snapshot.LockCells.Add(new LockCellSnapshot
            {
                Position = kv.Key,
                Type = v.Type,
                Required = v.Required,
                TargetColor = v.TargetColor
            });
        }

        snapshot.IceCells = new List<IceCellSnapshot>(_iceCells.Count);
        foreach (var kv in _iceCells)
        {
            snapshot.IceCells.Add(new IceCellSnapshot { Position = kv.Key, HP = kv.Value });
        }

        snapshot.ScrewCells = new List<ScrewCellSnapshot>(_screwCells.Count);
        foreach (var kv in _screwCells)
        {
            snapshot.ScrewCells.Add(new ScrewCellSnapshot { Position = kv.Key, HP = Mathf.Clamp(kv.Value, 0, SCREW_HP_MAX) });
        }

        snapshot.GateWaveProgress = new List<GateWaveProgressSnapshot>(_gateNextWaveIndex.Count);
        foreach (var kv in _gateNextWaveIndex)
        {
            snapshot.GateWaveProgress.Add(new GateWaveProgressSnapshot { Position = kv.Key, NextWaveIndex = kv.Value });
        }

        if (Cells == null)
        {
            return snapshot;
        }

        int width = Cells.GetLength(0);
        int height = Cells.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                GridCell cell = Cells[x, z];
                if (cell == null || !cell.IsOccupied || cell.Stack == null || cell.Stack.Buttons == null || cell.Stack.Buttons.Count == 0)
                {
                    continue;
                }

                CellSnapshot cellSnapshot = new CellSnapshot
                {
                    Position = cell.Position,
                    Colors = new List<ObjectColor>(cell.Stack.Buttons.Count)
                };

                for (int i = 0; i < cell.Stack.Buttons.Count; i++)
                {
                    ButtonCircle button = cell.Stack.Buttons[i];
                    if (button != null)
                    {
                        cellSnapshot.Colors.Add(button.Color);
                    }
                }

                snapshot.Cells.Add(cellSnapshot);
            }
        }

        return snapshot;
    }

    public void ApplySnapshot(BoardSnapshot snapshot)
    {
        if (Cells == null || snapshot == null)
        {
            return;
        }

        ApplyLockSnapshot(snapshot);

        _gateNextWaveIndex.Clear();
        _gateTargetReservations.Clear();
        if (snapshot.GateWaveProgress != null)
        {
            for (int i = 0; i < snapshot.GateWaveProgress.Count; i++)
            {
                var e = snapshot.GateWaveProgress[i];
                _gateNextWaveIndex[e.Position] = Mathf.Max(0, e.NextWaveIndex);
            }
        }

        int width = Cells.GetLength(0);
        int height = Cells.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                GridCell cell = Cells[x, z];
                if (cell == null)
                {
                    continue;
                }

                if (cell.IsOccupied && cell.Stack != null)
                {
                    Destroy(cell.Stack.gameObject);
                    cell.AssignStack(null);
                }

                cell.Highlight = false;
                cell.Focus = false;
            }
        }

        foreach (CellSnapshot cellSnapshot in snapshot.Cells)
        {
            if (cellSnapshot.Colors == null || cellSnapshot.Colors.Count == 0)
            {
                continue;
            }

            if (cellSnapshot.Position.x < 0 || cellSnapshot.Position.x >= width ||
                cellSnapshot.Position.y < 0 || cellSnapshot.Position.y >= height)
            {
                continue;
            }

            GridCell cell = Cells[cellSnapshot.Position.x, cellSnapshot.Position.y];
            if (cell == null)
            {
                continue;
            }

            ButtonStack stack = Instantiate(_stackPrefab);
            stack.transform.position = cell.transform.position;

            for (int i = 0; i < cellSnapshot.Colors.Count; i++)
            {
                Vector3 localPos = Vector3.up * (i + 1) * _spacingButton;
                Vector3 spawnPos = stack.transform.TransformPoint(localPos);
                ButtonCircle button = Instantiate(_buttonPrefab, spawnPos, Quaternion.identity, stack.transform);
                button.SetColor(cellSnapshot.Colors[i]);
                stack.AddButton(button);
            }

            stack.Configure(cell);
        }

        InitGatesFromConfig();

        RebuildLockOverlays();
        RebuildIceVisuals();
        RebuildIceOverlays();
        RebuildGateOverlays();

        TriggerGatesForOccupiedCells();
    }
    public GridCell GetTopCell()
    {
        if (Cells == null) return null;

        GridCell topCell = null;
        float maxZ = float.NegativeInfinity;

        int width = Cells.GetLength(0);
        int height = Cells.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                GridCell cell = Cells[x, z];
                if (cell == null) continue;

                float posZ = cell.transform.position.z;
                if (posZ > maxZ)
                {
                    maxZ = posZ;
                    topCell = cell;
                }
            }
        }

        return topCell;
    }
    public GridCell CellStartInTutorialControl()
    {
        return TryGetCell(new Vector2Int(3, 3));
    }
    public GridCell CellsEndInTutorialControl()
    {
        return  TryGetCell(new Vector2Int(2, 1));
    }
}

[System.Serializable]
public class CellClearedVfxByColor
{
    public ObjectColor Color;
    public Color ColorVfx;
}

[System.Serializable]
public class CellSnapshot
{
    public Vector2Int Position;
    public List<ObjectColor> Colors;
}

[System.Serializable]
public class BoardSnapshot
{
    public List<CellSnapshot> Cells = new List<CellSnapshot>();
    public int CollectedTotal;
    public List<ColorCountSnapshot> CollectedByColor = new List<ColorCountSnapshot>();
    public List<LockCellSnapshot> LockCells = new List<LockCellSnapshot>();
    public List<IceCellSnapshot> IceCells = new List<IceCellSnapshot>();
    public List<ScrewCellSnapshot> ScrewCells = new List<ScrewCellSnapshot>();
    public List<GateWaveProgressSnapshot> GateWaveProgress = new List<GateWaveProgressSnapshot>();
}

[System.Serializable]
public class GateWaveProgressSnapshot
{
    public Vector2Int Position;
    public int NextWaveIndex;
}

[System.Serializable]
public class IceCellSnapshot
{
    public Vector2Int Position;
    public int HP;
}

[System.Serializable]
public class ScrewCellSnapshot
{
    public Vector2Int Position;
    public int HP;
}

[System.Serializable]
public class ColorCountSnapshot
{
    public ObjectColor Color;
    public int Count;
}

[System.Serializable]
public class LockCellSnapshot
{
    public Vector2Int Position;
    public CellElementType Type;
    public int Required;
    public ObjectColor TargetColor;
}

