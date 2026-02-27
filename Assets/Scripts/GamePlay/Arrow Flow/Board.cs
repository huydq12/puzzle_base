using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Dreamteck.Splines;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using Debug = UnityEngine.Debug;
using StackTrace = System.Diagnostics.StackTrace;
public enum BoosterType
{
    None,
    Hammer,
    Conveyor,
    Rainbow,
    Shuffle
}
public class Board : Singleton<Board>
{
    [SerializeField] private GridCell _cellPrefab;
    [SerializeField] private Line _linePrefab;
    [SerializeField] private CubeLine _cubePrefab;
    [SerializeField] private Elevator _elevatorPrefab;
    [SerializeField] private LineDoor _lineDoorPrefab;
    [SerializeField] private ConveyorTunel _conveyorTunelPrefab;
    [SerializeField] private GameColorConfig _colorConfig;
    [SerializeField] private float _cellSize;
    [SerializeField] private float _paddingCamera;
    [SerializeField] private Vector2 _spacing;
    [Header("Elevator Spawn Timing")]
    [SerializeField] private float _elevatorLineStagger = 0.05f;
    [SerializeField] private float _elevatorCubeScaleDuration = 0.12f;
    [SerializeField] private float _elevatorCubeScaleStagger = 0.02f;

    [SerializeField] private Camera mainCam;
    [SerializeField] private Camera effectCam;

    [HideInInspector] public GridCell[,] Cells;
    [SerializeField] private SpriteRenderer _overlay;
    [ReadOnly] public BoosterType CurrentBooster;
    [ReadOnly] public bool IsUsingBooster;
    public Vector2 Spacing => _spacing;
    public int InitLine => _currentConfig.ColorLines.Count;
    public GameColorConfig ColorConfig => _colorConfig;
    private LevelConfig _currentConfig;

    private readonly List<Line> _iceLines = new();
    private readonly List<(Elevator elevator, ElevatorData data, bool activated)> _elevators = new();
    private readonly List<LineDoorEntry> _lineDoors = new();
    private readonly Dictionary<Vector2Int, int> _conveyorTunnelBlockCounts = new();

    private readonly List<(ConveyorTunel tunnel, List<Vector2Int> cells)> _activeTunnels = new();

    private struct ConveyorMeta
    {
        public int Type;
        public int Counter;
        public bool IsHole;

        public bool HasAny => IsHole || Type != 0 || Counter > 0;

        public bool SameAs(in ConveyorMeta other)
        {
            return Type == other.Type && Counter == other.Counter && IsHole == other.IsHole;
        }
    }

    private float _nextElevatorCheckTime;
    private float _nextLineDoorCheckTime;

    private class LineDoorEntry
    {
        public LineDoor door;
        public LineDoorData data;
        public int remaining;
        public bool opened;
        public bool spawned;
    }

    protected override void Awake()
    {
        base.Awake();
        EnsureCameras();
    }

    private void EnsureCameras()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) mainCam = FindMainCameraFallback();
        if (effectCam == null) effectCam = FindEffectCameraFallback(mainCam);
        SyncEffectCameraFromMainInternal();
    }

    private void SyncEffectCameraFromMainInternal()
    {
        if (mainCam == null || effectCam == null) return;

        effectCam.transform.SetPositionAndRotation(mainCam.transform.position, mainCam.transform.rotation);
        effectCam.orthographic = mainCam.orthographic;
        effectCam.orthographicSize = mainCam.orthographicSize;
        effectCam.fieldOfView = mainCam.fieldOfView;
        effectCam.nearClipPlane = mainCam.nearClipPlane;
        effectCam.farClipPlane = mainCam.farClipPlane;
        effectCam.aspect = mainCam.aspect;
    }

    private static Camera FindMainCameraFallback()
    {
        Camera[] cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        for (int i = 0; i < cams.Length; i++)
        {
            Camera cam = cams[i];
            if (cam != null && cam.CompareTag("MainCamera"))
            {
                return cam;
            }
        }

        return cams.Length > 0 ? cams[0] : null;
    }

    private static Camera FindEffectCameraFallback(Camera main)
    {
        Camera[] cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        for (int i = 0; i < cams.Length; i++)
        {
            Camera cam = cams[i];
            if (cam == null || cam == main) continue;
            if (!cam.CompareTag("MainCamera")) return cam;
        }

        return null;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private static void DebugSetActive(GameObject obj, bool active, UnityEngine.Object context, string reason)
    {
        if (obj == null) return;
        if (obj.activeSelf == active) return;
        UnityEngine.Debug.LogWarning($"[Board.DebugSetActive] {obj.name} -> {active} reason={reason}\n{new StackTrace(true)}", context);
        obj.SetActive(active);
    }
#endif
    public void NotifyAnyLineEnteredConveyor()
    {
        if (_iceLines.Count > 0)
        {
            for (int i = 0; i < _iceLines.Count; i++)
            {
                Line line = _iceLines[i];
                if (line == null) continue;
                line.ConsumeIceStep(1);
            }
        }

        ShooterController.Instance?.ConsumeShooterIceStep(1);
    }

    void SetupCamera(float paddingCamera, float minOrthoSize)
    {
        EnsureCameras();
        if (mainCam == null) return;

        float limit = minOrthoSize > 0f ? minOrthoSize : 12f;
        float minPosX = Mathf.Infinity;
        float maxPosX = Mathf.NegativeInfinity;

        foreach (Transform child in transform)
        {
            float childPosX = child.position.x;

            if (childPosX < minPosX) minPosX = childPosX;
            if (childPosX > maxPosX) maxPosX = childPosX;
        }
        float padding = paddingCamera > 0f ? paddingCamera : _paddingCamera;
        float halfSizeBoard = (maxPosX - minPosX + _cellSize * 2f + padding * 2f) / (2f * mainCam.aspect);
        mainCam.orthographicSize = Mathf.Max(halfSizeBoard, limit);
        SyncEffectCameraFromMainInternal();
    }
    public void UseHammer()
    {
        TryUseBooster(BoosterType.Hammer);
    }
    public void UseConveyor()
    {
        TryUseBooster(BoosterType.Conveyor);
    }
    public void UseRainbow()
    {
        TryUseBooster(BoosterType.Rainbow);
    }

    public void UseShuffle()
    {
        TryUseBooster(BoosterType.Shuffle);
    }

    public bool CanActivateBooster(BoosterType type)
    {
        switch (type)
        {
            case BoosterType.Hammer:
                if (Cells == null) return false;
                int w = Cells.GetLength(0);
                int h = Cells.GetLength(1);
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        if (Cells[x, y].IsOccupied) return true;
                    }
                }
                return false;

            case BoosterType.Conveyor:
                return ConveyorController.Instance != null && ConveyorController.Instance.HasAnyCubeOnConveyor();

            case BoosterType.Rainbow:
                return ShooterController.Instance != null && ShooterController.Instance.CanConvertRandomShooterToRainbow();

            case BoosterType.Shuffle:
                return ShooterController.Instance != null && ShooterController.Instance.CanShuffleShootersOnGates();

            default:
                return true;
        }
    }

    private void TryUseBooster(BoosterType type)
    {
        if (CurrentBooster != BoosterType.None)
        {
            return;
        }

        UseBooster(type);
    }
    public void ResetBooster()
    {
        _overlay.DOFade(0f, 0.25f).OnComplete(() =>
        {
            _overlay.enabled = false;
            SetShuffleGateLayerActive(false);
            foreach (var cell in Cells)
            {
                if (!cell.IsOccupied) continue;
                cell.CubeOnCell.BringToTop = false;
            }
            IsUsingBooster = false;
        });
    }
    public void UseBooster(BoosterType type)
    {
        CurrentBooster = type;
        switch (type)
        {
            case BoosterType.Hammer:
                {
                    foreach (var cell in Cells)
                    {
                        if (!cell.IsOccupied) continue;
                        cell.CubeOnCell.BringToTop = true;
                    }
                    _overlay.color = _overlay.color.With(a: 0f);
                    _overlay.enabled = true;
                    _overlay.DOFade(0.85f, 0.25f);
                    break;
                }
            case BoosterType.Conveyor:
                {
                    if (!ConveyorController.Instance.HasAnyCubeOnConveyor())
                    {
                        CurrentBooster = BoosterType.None;
                        return;
                    }
                    else
                    {
                        ConveyorController.Instance.StopConveyor();
                        ConveyorController.Instance.BringToTop = true;
                        ConveyorController.Instance.SetAllCubesBringToTop(true);
                        _overlay.color = _overlay.color.With(a: 0f);
                        _overlay.enabled = true;
                        _overlay.DOFade(0.85f, 0.25f);
                    }
                    break;
                }
            case BoosterType.Rainbow:
                {
                    bool success = ShooterController.Instance.ConvertRandomShooterToRainbow();
                    CurrentBooster = BoosterType.None;
                    if (!success)
                    {
                        return;
                    }
                    break;
                }

            case BoosterType.Shuffle:
                {
                    if (ShooterController.Instance == null || !ShooterController.Instance.CanShuffleShootersOnGates())
                    {
                        CurrentBooster = BoosterType.None;
                        return;
                    }

                    SetShuffleGateLayerActive(true);

                    _overlay.color = _overlay.color.With(a: 0f);
                    _overlay.enabled = true;
                    _overlay.DOFade(0.85f, 0.25f);
                    break;
                }
        }
    }

    private void SetShuffleGateLayerActive(bool active)
    {
        int layer = LayerMask.NameToLayer(active ? "Top" : "Gate");
        if (layer < 0) return;
        if (ShooterController.Instance == null || ShooterController.Instance.Gates == null) return;

        for (int i = 0; i < ShooterController.Instance.Gates.Count; i++)
        {
            IGate gate = ShooterController.Instance.Gates[i];
            if (gate == null || gate.RootTransform == null) continue;
            SetLayerRecursively(gate.RootTransform, layer);
        }
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        if (root == null) return;
        root.gameObject.layer = layer;
        int childCount = root.childCount;
        for (int i = 0; i < childCount; i++)
        {
            SetLayerRecursively(root.GetChild(i), layer);
        }
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
    public bool IsConveyorCellBlockedByTunnel(Vector2Int cellPos)
    {
        return _conveyorTunnelBlockCounts.TryGetValue(cellPos, out int count) && count > 0;
    }
    public void NotifyShooterDisappeared(Shooter shooter = null, string reason = null)
    {
        if (_activeTunnels == null || _activeTunnels.Count == 0) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (shooter != null)
            UnityEngine.Debug.Log(
                $"[Board] Shooter disappeared: {DescribeShooter(shooter)} reason={reason}",
                shooter
            );
#endif

        for (int i = _activeTunnels.Count - 1; i >= 0; i--)
        {
            var entry = _activeTunnels[i];
            ConveyorTunel tunnel = entry.tunnel;
            if (tunnel == null)
            {
                _activeTunnels.RemoveAt(i);
                continue;
            }

            if (tunnel.Counter <= 0)
                continue;

            int nextCounter = tunnel.Counter - 1;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (shooter != null)
                UnityEngine.Debug.Log(
                    $"[Board] ConveyorTunel '{tunnel.name}' counter {tunnel.Counter} -> {nextCounter} by {DescribeShooter(shooter)}",
                    tunnel
                );
#endif
            if (shooter != null)
                tunnel.PlayShooterVfx(shooter.transform);
            tunnel.SetCounter(nextCounter);

            if (nextCounter <= 0)
            {
                UnregisterTunnelCells(entry.cells);
                RefreshAllHeadHighlights();
                _activeTunnels.RemoveAt(i);
            }
        }
    }

    private static string DescribeShooter(Shooter shooter)
    {
        if (shooter == null) return "null";
        return $"{shooter.name} (Type={shooter.Type}, Color={shooter.Color}, Total={shooter.Total})";
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

        // A conveyor cell covered by a tunnel is not a valid entry point.
        if (IsConveyorCellBlockedByTunnel(conveyorCell.Position)) return false;

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

	    private void SetupElevators()
	    {
        _elevators.Clear();
        _nextElevatorCheckTime = 0f;

	        if (_currentConfig == null || _currentConfig.Elevators == null || _currentConfig.Elevators.Count == 0)
	            return;
	        if (_elevatorPrefab == null) return;

	        for (int i = 0; i < _currentConfig.Elevators.Count; i++)
	        {
            ElevatorData data = _currentConfig.Elevators[i];
            if (data == null) continue;
            if (data.Size.x <= 0 || data.Size.y <= 0) continue;

	            Elevator elevator = Instantiate(_elevatorPrefab);
	            if (elevator == null) continue;
	            elevator.transform.SetParent(transform, false);

            Vector3 center = GetRectCenterWorld(data.Position, data.Size);
            elevator.transform.position = center;

            float scaleX = data.Size.x / 4f;
            float scaleZ = data.Size.y / 4f;
            elevator.transform.localScale = new Vector3(scaleX, 1f, scaleZ);

            _elevators.Add((elevator, data, false));
        }
    }

    private void SetupLineDoors()
    {
        _lineDoors.Clear();
        _nextLineDoorCheckTime = 0f;
        if (_currentConfig == null || _currentConfig.LineDoors == null || _currentConfig.LineDoors.Count == 0)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[Board.LineDoor] No LineDoors in config.", this);
#endif
            return;
        }
        if (_lineDoorPrefab == null) return;

        for (int i = 0; i < _currentConfig.LineDoors.Count; i++)
        {
            LineDoorData data = _currentConfig.LineDoors[i];
            if (data == null) continue;
            if (data.Size.x <= 0 || data.Size.y <= 0) continue;

            LineDoor door = Instantiate(_lineDoorPrefab);
            if (door == null) continue;
            door.transform.SetParent(transform, false);

            Vector3 center = GetRectCenterWorld(data.Position, data.Size);
            door.transform.position = center;
            door.transform.rotation = DirectionToRotation(data.Direction);

            door.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

            int remaining = data.Counter;
            door.Setup(data.Color, remaining);
            var entry = new LineDoorEntry
            {
                door = door,
                data = data,
                remaining = remaining,
                opened = false,
                spawned = false
            };
            _lineDoors.Add(entry);

            if (remaining <= 0)
                OpenLineDoorAtIndex(_lineDoors.Count - 1);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Board.LineDoor] Spawned door idx={_lineDoors.Count - 1} color={data.Color} counter={remaining} pos={data.Position} size={data.Size}", door);
#endif
        }
    }

    private Vector3 GetRectCenterWorld(Vector2Int pos, Vector2Int size)
    {
        Vector2Int min = pos;
        Vector2Int max = new Vector2Int(pos.x + size.x - 1, pos.y + size.y - 1);

        GridCell a = GetCellAt(min);
        GridCell b = GetCellAt(max);
        if (a == null || b == null) return Vector3.zero;

        Vector3 p0 = a.transform.position;
        Vector3 p1 = b.transform.position;
        return (p0 + p1) * 0.5f;
    }

    private static Quaternion DirectionToRotation(int direction)
    {
        float y = 0f;
        if (direction == 1) y = 45f;
        else if (direction == 2) y = 90f;
        else if (direction == 3) y = 135f;
        else if (direction == 4) y = 180f;
        else if (direction == 5) y = 225f;
        else if (direction == 6) y = 270f;
        else if (direction == 7) y = 315f;
        else if (direction == 8) y = 360f;
        return Quaternion.Euler(0f, y, 0f);
    }

    private void Update()
    {
        if (_currentConfig == null) return;
        UpdateElevators();
        UpdateLineDoors();
    }

    private void UpdateElevators()
    {
        if (_elevators.Count == 0) return;
        if (Time.time < _nextElevatorCheckTime) return;
        _nextElevatorCheckTime = Time.time + 0.1f;

        for (int i = 0; i < _elevators.Count; i++)
        {
            var entry = _elevators[i];
            if (entry.activated) continue;
            if (!IsRectEmpty(entry.data.Position, entry.data.Size)) continue;

            ActivateElevator(i);
        }
    }

    private void UpdateLineDoors()
    {
        if (_lineDoors.Count == 0) return;
        if (Time.time < _nextLineDoorCheckTime) return;
        _nextLineDoorCheckTime = Time.time + 0.1f;

        for (int i = 0; i < _lineDoors.Count; i++)
        {
            var entry = _lineDoors[i];
            if (!entry.opened || entry.spawned) continue;
            TrySpawnLineDoorLines(i);
        }
    }

    private bool IsRectEmpty(Vector2Int pos, Vector2Int size)
    {
        int minX = pos.x;
        int minY = pos.y;
        int maxX = pos.x + size.x - 1;
        int maxY = pos.y + size.y - 1;

        if (Cells == null) return false;
        int w = Cells.GetLength(0);
        int h = Cells.GetLength(1);
        if (minX < 0 || minY < 0 || maxX >= w || maxY >= h) return false;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                GridCell cell = Cells[x, y];
                if (cell == null) continue;
                if (cell.CubeOnCell != null) return false;
            }
        }

        return true;
    }
    public GridCell CellTaptInTutorialControl()
    {
        return GetCellAt(new Vector2Int(2, 1));
    }
    private void ActivateElevator(int index)
    {
        var entry = _elevators[index];
        if (entry.activated) return;

        // Mark first to prevent re-entry.
        _elevators[index] = (entry.elevator, entry.data, true);

        void SpawnAllElevatorLines()
        {
            if (entry.data != null && entry.data.Lines != null)
            {
                for (int i = 0; i < entry.data.Lines.Count; i++)
                {
                    float delay = Mathf.Max(0f, _elevatorLineStagger) * i;
                    SpawnLine(entry.data.Lines[i], animateSpawn: true, spawnDelay: delay);
                }
            }
            RefreshAllHeadHighlights();
        }

        if (entry.elevator != null)
            entry.elevator.ActivateAndDisappear(SpawnAllElevatorLines);
        else
            SpawnAllElevatorLines();
    }

    public void NotifyLineDoorHit(ObjectColor color, Shooter shooter = null)
    {
        if (_lineDoors == null || _lineDoors.Count == 0) return;
        if (color == ObjectColor.None) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[Board.LineDoor] Hit color={color} shooter={DescribeShooter(shooter)}", shooter);
#endif

        for (int i = 0; i < _lineDoors.Count; i++)
        {
            var entry = _lineDoors[i];
            if (entry.opened) continue;
            if (entry.data == null) continue;
            if (entry.data.Color != color) continue;

            LineDoor door = entry.door;

            if (door != null)
            {
                bool opened = door.Consume(1, shooter != null ? shooter.transform : null, () => TrySpawnLineDoorLines(i));
                entry.remaining = door.Remaining;
                if (opened)
                {
                    entry.opened = true;
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[Board.LineDoor] Door idx={i} color={entry.data.Color} remaining={entry.remaining} opened={entry.opened}", door);
#endif
            }
            else
            {
                entry.remaining = Mathf.Max(0, entry.remaining - 1);
                if (entry.remaining <= 0)
                {
                    entry.opened = true;
                    TrySpawnLineDoorLines(i);
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[Board.LineDoor] Door idx={i} (no instance) color={entry.data.Color} remaining={entry.remaining} opened={entry.opened}", this);
#endif
            }
        }
    }

    private void OpenLineDoorAtIndex(int index)
    {
        if (index < 0 || index >= _lineDoors.Count) return;
        var entry = _lineDoors[index];
        if (entry.opened) return;
        entry.opened = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[Board.LineDoor] Open door idx={index} color={entry.data?.Color} counter={entry.remaining}", entry.door);
#endif

        if (entry.door != null)
            entry.door.Open(() => TrySpawnLineDoorLines(index));
        else
            TrySpawnLineDoorLines(index);
    }

    private void TrySpawnLineDoorLines(int index)
    {
        if (index < 0 || index >= _lineDoors.Count) return;
        var entry = _lineDoors[index];
        if (entry.spawned) return;
        if (entry.data == null) return;

        if (!IsRectEmpty(entry.data.Position, entry.data.Size))
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (TryGetFirstOccupiedCell(entry.data.Position, entry.data.Size, out GridCell blocked))
            {
                Vector2Int bp = blocked != null ? blocked.Position : new Vector2Int(-1, -1);
                Debug.Log($"[Board.LineDoor] Spawn blocked idx={index} color={entry.data.Color} at cell={bp}", blocked);
            }
            else
            {
                Debug.Log($"[Board.LineDoor] Spawn blocked idx={index} color={entry.data.Color} (unknown blocker)", this);
            }
#endif
            return;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[Board.LineDoor] Spawn lines idx={index} color={entry.data.Color}", this);
#endif
        SpawnLineDoorLines(entry.data);
        entry.spawned = true;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private bool TryGetFirstOccupiedCell(Vector2Int pos, Vector2Int size, out GridCell occupied)
    {
        occupied = null;
        if (Cells == null) return false;

        int minX = pos.x;
        int minY = pos.y;
        int maxX = pos.x + size.x - 1;
        int maxY = pos.y + size.y - 1;

        int w = Cells.GetLength(0);
        int h = Cells.GetLength(1);
        if (minX < 0 || minY < 0 || maxX >= w || maxY >= h) return false;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                GridCell cell = Cells[x, y];
                if (cell == null) continue;
                if (cell.CubeOnCell != null)
                {
                    occupied = cell;
                    return true;
                }
            }
        }

        return false;
    }
#endif

    private void SpawnLineDoorLines(LineDoorData data)
    {
        if (data == null || data.Lines == null) return;
        for (int i = 0; i < data.Lines.Count; i++)
        {
            float delay = Mathf.Max(0f, _elevatorLineStagger) * i;
            SpawnLine(data.Lines[i], animateSpawn: true, spawnDelay: delay);
        }
        RefreshAllHeadHighlights();
    }

	    private void SpawnLine(ColorLine line, bool animateSpawn = false, float spawnDelay = 0f)
	    {
	        if (line == null || line.Cells == null || line.Cells.Count == 0) return;

	        bool lineHasIce = line.ElementTypes != null && line.ElementTypes.Contains(2);

	        Line lineGo = Instantiate(_linePrefab);
	        lineGo.transform.SetParent(transform, false);
	        lineGo.transform.localPosition = Vector3.zero;
	        lineGo.transform.localRotation = Quaternion.identity;

        lineGo.Color = line.Color;
        lineGo.InitializeCounter(line.Counter);
        lineGo.SetIsIceLine(lineHasIce);
        lineGo.SetElementTypes(line.ElementTypes, line.Cells.Count);
        lineGo.Cubes = new List<CubeLine>();

        int last = line.Cells.Count - 1;
        for (int i = 0; i < line.Cells.Count; i++)
        {
            Vector2Int curr = line.Cells[i];
            Vector2Int? prev = i > 0 ? line.Cells[i - 1] : (Vector2Int?)null;
            Vector2Int? next = i < last ? line.Cells[i + 1] : (Vector2Int?)null;

	            GridCell cell = GetCellAt(curr);
	            if (cell == null) continue;
	            if (cell.CubeOnCell != null) continue;

	            CubeLine cube = Instantiate(_cubePrefab);
	            cube.transform.SetParent(lineGo.transform, false);
	            cube.transform.SetPositionAndRotation(cell.transform.position, Quaternion.identity);

            cube.SetColor(line.Color);
            int elementType = (line.ElementTypes != null && i < line.ElementTypes.Count) ? line.ElementTypes[i] : 0;
            cube.SetElementType(elementType);
            if (lineGo.ElementTypes != null && i < lineGo.ElementTypes.Count)
                lineGo.ElementTypes[i] = elementType;
            cell.CubeOnCell = cube;
            cell.ShowRenderer(true);
            cube.Cell = cell;

            if (animateSpawn && _elevatorCubeScaleDuration > 0f)
            {
                Vector3 targetScale = cube.transform.localScale;
                cube.transform.localScale = Vector3.zero;
                float delay = Mathf.Max(0f, spawnDelay) + Mathf.Max(0f, _elevatorCubeScaleStagger) * i;
                cube.transform.DOScale(targetScale, _elevatorCubeScaleDuration).SetDelay(delay).SetEase(Ease.OutQuad);
            }

            if (i == last)
            {
                cube.SetType(CubeType.Head);
                if (prev.HasValue)
                {
                    Direction dir = DirFromDelta(curr - prev.Value);
                    float yaw = GetYawFromDirection(dir);
                    cube.transform.localRotation = Quaternion.Euler(0f, yaw + 180, 0f);
                }
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

            cube.Line = lineGo;
            lineGo.Cubes.Add(cube);
        }

        if (lineHasIce)
            _iceLines.Add(lineGo);

        lineGo.RefreshCounterText();
        lineGo.InitializeBombIfNeeded();
    }
    private void SetupConveyor()
    {
        _conveyorTunnelBlockCounts.Clear();
        _activeTunnels.Clear();
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

        Dictionary<Vector2Int, ConveyorMeta> metaByCell = BuildConveyorMetaByCell(_currentConfig.ConveyorLine);

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

        SpawnConveyorTunels(orderedCells, metaByCell, cornerOffset);
    }

    private static Dictionary<Vector2Int, ConveyorMeta> BuildConveyorMetaByCell(ConveyorLine line)
    {
        Dictionary<Vector2Int, ConveyorMeta> result = new();
        if (line == null || line.Cells == null) return result;

        for (int i = 0; i < line.Cells.Count; i++)
        {
            int type = (line.Types != null && i < line.Types.Count) ? line.Types[i] : 0;
            int counter = (line.Counters != null && i < line.Counters.Count) ? line.Counters[i] : 0;
            bool isHole = (line.IsHoles != null && i < line.IsHoles.Count) && line.IsHoles[i];

            // Some legacy/serialized data uses -1 to mean "unset".
            if (type < 0) type = 0;
            if (counter < 0) counter = 0;

            ConveyorMeta meta = new ConveyorMeta
            {
                Type = type,
                Counter = counter,
                IsHole = isHole
            };

            if (!meta.HasAny) continue;
            Vector2Int cell = line.Cells[i];
            if (!result.ContainsKey(cell))
                result.Add(cell, meta);
        }

        return result;
    }

    private void SpawnConveyorTunels(List<Vector2Int> orderedCells, Dictionary<Vector2Int, ConveyorMeta> metaByCell, float cornerOffset)
    {
        if (_conveyorTunelPrefab == null) return;
        if (orderedCells == null || orderedCells.Count < 2) return;
        if (metaByCell == null || metaByCell.Count == 0) return;

        _conveyorTunnelBlockCounts.Clear();
        _activeTunnels.Clear();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_currentConfig != null && _currentConfig.Level == 39)
        {
            Debug.Log($"[Board] Level 39: Conveyor meta entries={metaByCell.Count}, conveyor orderedCells={orderedCells.Count}", this);
            foreach (var kvp in metaByCell)
                Debug.Log($"[Board] Level 39 meta cell={kvp.Key} type={kvp.Value.Type} counter={kvp.Value.Counter} isHole={kvp.Value.IsHole}", this);
        }
#endif

        List<(ConveyorMeta meta, List<Vector2Int> cells)> groups = new();
        ConveyorMeta currentMeta = default;
        List<Vector2Int> currentCells = null;

        for (int i = 0; i < orderedCells.Count; i++)
        {
            Vector2Int cell = orderedCells[i];
            bool hasMeta = metaByCell.TryGetValue(cell, out ConveyorMeta meta) && meta.HasAny && !meta.IsHole && meta.Type != 0;

            if (!hasMeta)
            {
                if (currentCells != null)
                {
                    groups.Add((currentMeta, currentCells));
                    currentCells = null;
                }
                continue;
            }

            if (currentCells != null && meta.SameAs(currentMeta))
            {
                currentCells.Add(cell);
                continue;
            }

            if (currentCells != null)
                groups.Add((currentMeta, currentCells));

            currentMeta = meta;
            currentCells = new List<Vector2Int> { cell };
        }

        if (currentCells != null)
            groups.Add((currentMeta, currentCells));

        if (groups.Count > 1 && groups[0].meta.SameAs(groups[^1].meta))
        {
            var merged = (meta: groups[^1].meta, cells: groups[^1].cells);
            merged.cells.AddRange(groups[0].cells);
            groups.RemoveAt(0);
            groups[^1] = merged;
        }

        for (int i = 0; i < groups.Count; i++)
        {
            var group = groups[i];
            if (group.cells.Count < 2) continue;

            List<Vector3> worldPositions = BuildOpenConveyorSplinePositions(group.cells, cornerOffset);
            if (worldPositions.Count < 2) continue;

	            ConveyorTunel tunel = Instantiate(_conveyorTunelPrefab);

            tunel.transform.SetParent(transform, false);
            tunel.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            tunel.transform.localScale = Vector3.one;
            tunel.gameObject.SetActive(true);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_currentConfig != null && _currentConfig.Level == 39)
            {
                Debug.Log($"[Board] Level 39 spawn tunnel type={group.meta.Type} counter={group.meta.Counter} cells={group.cells.Count} splinePoints={worldPositions.Count} activeInHierarchy={tunel.gameObject.activeInHierarchy}", tunel);
            }
#endif
            tunel.Setup(group.meta.Type, group.meta.Counter, worldPositions);

            if (group.meta.Counter > 0)
            {
                RegisterTunnelCells(group.cells);
                _activeTunnels.Add((tunel, group.cells));
            }
        }
    }

    private void RegisterTunnelCells(List<Vector2Int> cells)
    {
        if (cells == null) return;
        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int cell = cells[i];
            if (_conveyorTunnelBlockCounts.TryGetValue(cell, out int count))
                _conveyorTunnelBlockCounts[cell] = count + 1;
            else
                _conveyorTunnelBlockCounts[cell] = 1;
        }
    }

    private void UnregisterTunnelCells(List<Vector2Int> cells)
    {
        if (cells == null) return;
        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int cell = cells[i];
            if (!_conveyorTunnelBlockCounts.TryGetValue(cell, out int count)) continue;
            count--;
            if (count <= 0)
                _conveyorTunnelBlockCounts.Remove(cell);
            else
                _conveyorTunnelBlockCounts[cell] = count;
        }
    }

    private List<Vector3> BuildOpenConveyorSplinePositions(List<Vector2Int> cells, float cornerOffset)
    {
        List<Vector3> allPositions = new();
        if (cells == null || cells.Count < 2) return allPositions;

        for (int i = 0; i < cells.Count; i++)
        {
            GridCell currCell = GetCellAt(cells[i]);
            if (currCell == null) continue;

            Vector3 curr = currCell.transform.position;

            if (i == 0 || i == cells.Count - 1)
            {
                allPositions.Add(curr);
                continue;
            }

            GridCell prevCell = GetCellAt(cells[i - 1]);
            GridCell nextCell = GetCellAt(cells[i + 1]);
            if (prevCell == null || nextCell == null)
            {
                allPositions.Add(curr);
                continue;
            }

            Vector3 prev = prevCell.transform.position;
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

        return allPositions;
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
        _iceLines.Clear();

	        foreach (var line in _currentConfig.ColorLines)
	        {
	            bool lineHasIce = line != null && line.ElementTypes != null && line.ElementTypes.Contains(2);

	            Line lineColor = Instantiate(_linePrefab);
	            lineColor.transform.SetParent(transform, false);
	            lineColor.transform.localPosition = Vector3.zero;
	            lineColor.transform.localRotation = Quaternion.identity;

            lineColor.Color = line.Color;
            lineColor.InitializeCounter(line.Counter);
            lineColor.SetIsIceLine(lineHasIce);
            lineColor.SetElementTypes(line.ElementTypes, line.Cells.Count);
            lineColor.Cubes = new List<CubeLine>();

            var cells = line.Cells;
            int last = cells.Count - 1;

	            for (int i = 0; i < cells.Count; i++)
	            {
	                Vector2Int curr = cells[i];
                Vector2Int? prev = i > 0 ? cells[i - 1] : (Vector2Int?)null;
	                Vector2Int? next = i < last ? cells[i + 1] : (Vector2Int?)null;
	                GridCell cell = GetCellAt(curr);
	                CubeLine cube = Instantiate(_cubePrefab);
	                cube.transform.SetParent(lineColor.transform, false);
	                cube.transform.SetPositionAndRotation(cell.transform.position, Quaternion.identity);

                cube.SetColor(line.Color);
                int elementType = (line.ElementTypes != null && i < line.ElementTypes.Count) ? line.ElementTypes[i] : 0;
                cube.SetElementType(elementType);
                if (lineColor.ElementTypes != null && i < lineColor.ElementTypes.Count)
                    lineColor.ElementTypes[i] = elementType;
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

            if (lineHasIce)
            {
                _iceLines.Add(lineColor);
            }

            lineColor.RefreshCounterText();
            lineColor.InitializeBombIfNeeded();
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
	            GridCell cell = Instantiate(_cellPrefab);
	            cell.transform.SetParent(transform, false);
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

        Transform conveyorTemplate = _conveyorTunelPrefab != null ? _conveyorTunelPrefab.transform : null;
        Vector3 sum = Vector3.zero;
        int count = 0;

        foreach (Transform child in transform)
        {
            if (conveyorTemplate != null && child == conveyorTemplate) continue;
            sum += child.localPosition;
            count++;
        }

        if (count == 0) return;

        Vector3 center = sum / count;

        foreach (Transform child in transform)
        {
            if (conveyorTemplate != null && child == conveyorTemplate) continue;
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
	        Transform conveyorTemplate = _conveyorTunelPrefab != null ? _conveyorTunelPrefab.transform : null;

	        for (int i = transform.childCount - 1; i >= 0; i--)
	        {
	            Transform child = transform.GetChild(i);
	            if (conveyorTemplate != null && child == conveyorTemplate) continue;
            // Stop any tweens on pooled objects so replay spam can't leave them in a mid-animation state.
            DOTween.Kill(child, false);
            var childTransforms = child.GetComponentsInChildren<Transform>(true);
	            for (int t = 0; t < childTransforms.Length; t++)
	            {
	                DOTween.Kill(childTransforms[t], false);
	            }
	            if (child.TryGetComponent(out Line line)) line.Clear();
	            else if (child.TryGetComponent(out CubeLine cube)) cube.Clear();
	            Destroy(child.gameObject);
	        }
	    }
    public void SetupLevel(LevelConfig config)
    {
        GameManagerInGame.Instance.SetState(GameStateInGame.Init);

        Clear();
        _currentConfig = config;
        SetupGrid();
        SetupLine();
        SetupElevators();
        SetupLineDoors();
        SetupConveyor();
        SetupShooter();
        RefreshAllHeadHighlights();
        EnsureCameras();
        if (config != null && config.Camera.Enabled && mainCam != null)
        {
            if (config.Camera.UsePosition)
            {
                mainCam.transform.position = config.Camera.Position;
                SyncEffectCameraFromMainInternal();
            }

            if (config.Camera.UseOrthographicSize && config.Camera.OrthographicSize > 0f)
            {
                float refAspect = 9f / 16f;
                float aspect = mainCam.aspect;
                if (aspect <= 0.0001f) aspect = refAspect;

                float scaledSize = config.Camera.OrthographicSize * (refAspect / aspect);
                float minSize = config.Camera.MinOrthoSize > 0f ? config.Camera.MinOrthoSize : 0f;
                mainCam.orthographicSize = Mathf.Max(scaledSize, minSize);
                SyncEffectCameraFromMainInternal();
            }
            else
                SetupCamera(config.Camera.Padding, config.Camera.MinOrthoSize);
        }
        GameManagerInGame.Instance.SetState(GameStateInGame.Playing);
    }
}
