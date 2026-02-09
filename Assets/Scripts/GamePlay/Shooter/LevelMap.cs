using System;
using System.Collections.Generic;
using System.Linq;
using Dreamteck.Splines;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
public class CellData
{
    public bool IsWall;
    public Shooter Shooter;
}
[Serializable]
public class QueueLane
{
    public SplineComputer SplineQueue;
    public List<ObjectColor> ColorsConveyorQueue = new List<ObjectColor>();
    [ReadOnly] public List<Base> BasesQueue = new List<Base>();
}
public class LevelMap : SerializedMonoBehaviour
{
    public List<ObjectColor> ColorsConveyor;
    public Holder[] Holders;
    [ReadOnly] public List<Base> Bases = new();
    [ReadOnly] public List<Base> BasesOnFireRange = new();
    public SplineComputer Spline;
    [TableList] public List<QueueLane> QueueLanes = new List<QueueLane>();
    [TableMatrix(DrawElementMethod = nameof(DrawCellDataWithPreview), SquareCells = true, RowHeight = 50)]
    public CellData[,] Grid;
    private Dictionary<Base, float> _queueBaseCurrentDistances = new();
    private readonly Dictionary<Base, Shooter> _activeShots = new();
    private readonly List<Base> _shotsToRemove = new();
    private List<SplinePositioner> _arrows = new();
    private bool[,] _bfsVisited;

    [Button]
    public void Addcolorqueue(ObjectColor color, int total, int index)
    {
        for (int i = 1; i <= total; i++)
            QueueLanes[index].ColorsConveyorQueue.Add(color);
    }
    [Button]
    public void Addcolor(ObjectColor color, int total)
    {
        for (int i = 1; i <= total; i++)
        {
            ColorsConveyor.Add(color);
        }

    }
    public void SpawnArrowAlongSpline(GameObject arrowPrefab)
    {
        if (Spline == null) return;

        foreach (var arrow in _arrows)
        {
            if (arrow != null) Destroy(arrow.gameObject);
        }
        _arrows.Clear();

        float splineLength = Spline.CalculateLength();
        int total = Mathf.Max(3, Mathf.RoundToInt(splineLength * 3f));

        for (int i = 0; i < total; i++)
        {
            double percent = i / (double)total;

            GameObject arrow = Instantiate(arrowPrefab);
            if (arrow == null) continue;
            arrow.transform.SetParent(Board.Instance.transform, false);

            var follower = arrow.GetComponent<SplinePositioner>();
            if (follower != null)
            {
                follower.spline = Spline;
                follower.SetPercent(percent);
                _arrows.Add(follower); 
            }
            else
            {
                SplineSample sample = Spline.Evaluate(percent);
                arrow.transform.SetPositionAndRotation(sample.position, Quaternion.LookRotation(sample.forward, sample.up));
            }
        }
    }
    private void UpdateArrowsMovement(float splineLength, float percentPerSecond)
    {
        if (_arrows == null || _arrows.Count == 0) return;

        float basePercent = Time.time * percentPerSecond % 1f;
        int count = _arrows.Count;

        for (int i = 0; i < count; i++)
        {
            var arrow = _arrows[i];
            if (arrow == null) continue;

            float offset = (float)i / count;
            arrow.SetPercent((basePercent + offset) % 1f);
        }
    }
    public void AddBaseSorted(Base newBase)
    {
        float x = newBase.transform.position.x;

        int index = BasesOnFireRange.BinarySearch(
            newBase,
            Comparer<Base>.Create((a, b) =>
                a.transform.position.x.CompareTo(b.transform.position.x))
        );

        if (index < 0)
            index = ~index;

        BasesOnFireRange.Insert(index, newBase);
    }
    void Start()
    {
        CheckAllShootersCanMove();
    }
    public void CheckAllShootersCanMove()
    {
        if (Grid == null)
        {
            Debug.LogWarning("Grid chưa được khởi tạo!");
            return;
        }

        int cols = Grid.GetLength(0);
        int rows = Grid.GetLength(1);

        for (int c = 0; c < cols; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                CellData cell = Grid[c, r];

                if (cell.Shooter != null)
                {
                    if (CanMove(new Vector2Int(c, r)))
                        cell.Shooter.Show();
                    else
                        cell.Shooter.Hide();
                }
            }
        }
    }

    public bool CanMove(Vector2Int shooterPos)
    {
        if (Grid == null) return false;

        int cols = Grid.GetLength(0);
        int rows = Grid.GetLength(1);
        int x = shooterPos.x;
        int y = shooterPos.y;

        // Kiểm tra vị trí hợp lệ
        if (x < 0 || x >= cols || y < 0 || y >= rows) return false;

        // Kiểm tra ô này có shooter hợp lệ không
        if (Grid[x, y].Shooter == null) return false;

        // BFS tìm đường ra ngoài grid (row >= rows) - tức là đi xuống ra khỏi board
        if (_bfsVisited == null || _bfsVisited.GetLength(0) != cols || _bfsVisited.GetLength(1) != rows)
            _bfsVisited = new bool[cols, rows];
        else
            System.Array.Clear(_bfsVisited, 0, _bfsVisited.Length);

        var queue = new Queue<Vector2Int>();
        queue.Enqueue(shooterPos);
        _bfsVisited[x, y] = true;

        // 4 hướng: lên, xuống, trái, phải
        int[] dx = { 0, 0, -1, 1 };
        int[] dy = { -1, 1, 0, 0 };

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            for (int i = 0; i < 4; i++)
            {
                int nx = current.x + dx[i];
                int ny = current.y + dy[i];

                // ✅ Nếu ra ngoài grid phía dưới (ny >= rows) → tìm được đường ra khỏi board
                if (ny >= rows)
                {
                    return true;
                }

                // Bỏ qua nếu ra ngoài biên khác hoặc đã visited
                if (nx < 0 || nx >= cols || ny < 0) continue;
                if (_bfsVisited[nx, ny]) continue;

                CellData nextCell = Grid[nx, ny];

                // ❌ Không đi được nếu là WALL
                if (nextCell.IsWall) continue;

                // ❌ Không đi được nếu có SHOOTER khác (shooter = vật cản giống wall)
                if (nextCell.Shooter != null) continue;

                // ✅ Ô trống → có thể đi qua
                _bfsVisited[nx, ny] = true;
                queue.Enqueue(new Vector2Int(nx, ny));
            }
        }

        return false; // Không tìm được đường ra - bị chặn bởi wall hoặc shooter khác
    }
    private void Update()
    {
        float splineLength = Spline != null ? Spline.CalculateLength() : 0f;
        float percentPerSecond = splineLength > 0.0001f ? (Board.Instance.Speed / splineLength) : 0f;

        UpdateArrowsMovement(splineLength, percentPerSecond);

        if (Bases != null && Bases.Count > 0)
        {
            float basePercent = Time.time * percentPerSecond % 1f;
            int count = Bases.Count;

            for (int i = 0; i < count; i++)
            {
                float offset = (float)i / count;
                var b = Bases[i];
                b.Positioner.SetPercent((basePercent + offset) % 1f);
            }
        }

        UpdateQueueBasesMovement();
        TryShootBasesInRange();
    }

    private const float QUEUE_SNAP_THRESHOLD = 0.01f;
    private bool _isQueueAnimating = false;
    private readonly Dictionary<Base, float> _queueBaseVelocities = new();
    private readonly Dictionary<SplineComputer, float> _queueSplineLengths = new();
    private void UpdateQueueBasesMovement()
    {
        if (QueueLanes == null || QueueLanes.Count == 0) return;
        if (!_isQueueAnimating) return;

        bool allAtTarget = true;
        foreach (var lane in QueueLanes)
        {
            if (lane == null || lane.SplineQueue == null || lane.BasesQueue == null || lane.BasesQueue.Count == 0)
                continue;

            if (!_queueSplineLengths.TryGetValue(lane.SplineQueue, out float splineLength))
            {
                splineLength = lane.SplineQueue.CalculateLength();
                _queueSplineLengths[lane.SplineQueue] = splineLength;
            }

            for (int i = 0; i < lane.BasesQueue.Count; i++)
            {
                Base baseObj = lane.BasesQueue[i];
                if (baseObj == null) continue;

                float targetDistanceFromStart = Mathf.Clamp(
                    splineLength - (i * Defines.QUEUE_BASE_OFFSET),
                    0f,
                    splineLength
                );

                if (!_queueBaseCurrentDistances.TryGetValue(baseObj, out float currentDist))
                {
                    currentDist = (float)(baseObj.Positioner.GetPercent() * splineLength);
                    _queueBaseCurrentDistances[baseObj] = currentDist;
                }

                float distanceFromStart;
                float gap = Mathf.Abs(currentDist - targetDistanceFromStart);

                if (gap < QUEUE_SNAP_THRESHOLD)
                {
                    distanceFromStart = targetDistanceFromStart;
                    _queueBaseVelocities.Remove(baseObj);
                }
                else
                {
                    allAtTarget = false;

                    if (!_queueBaseVelocities.TryGetValue(baseObj, out float velocity))
                        velocity = 0f;

                    float smoothTime = GetQueueSmoothTime(currentDist, targetDistanceFromStart);
                    distanceFromStart = Mathf.SmoothDamp(
                        currentDist,
                        targetDistanceFromStart,
                        ref velocity,
                        smoothTime,
                        Mathf.Infinity,
                        Time.deltaTime
                    );

                    _queueBaseVelocities[baseObj] = velocity;
                }

                _queueBaseCurrentDistances[baseObj] = distanceFromStart;

                double percent = lane.SplineQueue.Travel(0, distanceFromStart);
                baseObj.Positioner.SetPercent(percent);
            }
        }

        if (allAtTarget)
        {
            _isQueueAnimating = false;
            _queueBaseVelocities.Clear();
        }
    }
    private float GetQueueSmoothTime(float currentDist, float targetDist)
    {
        float speedDistPerSec = Mathf.Max(Board.Instance.Speed, 0.0001f);
        float distance = Mathf.Abs(targetDist - currentDist);
        float duration = distance / speedDistPerSec;
        return Mathf.Max(duration, 0.0001f);
    }
    public QueueLane GetQueueLane(SplineComputer splineQueue)
    {
        if (splineQueue == null || QueueLanes == null) return null;
        return QueueLanes.FirstOrDefault(lane => lane != null && lane.SplineQueue == splineQueue);
    }

    private void TryShootBasesInRange()
    {
        if (BasesOnFireRange == null || BasesOnFireRange.Count == 0) return;
        if (Holders == null || Holders.Length == 0) return;

        CleanupActiveShots();

        for (int i = BasesOnFireRange.Count - 1; i >= 0; i--)
        {
            Base baseObj = BasesOnFireRange[i];
            if (baseObj == null) continue;
            if (_activeShots.ContainsKey(baseObj)) continue;

            Cube firstCube = GetFirstCube(baseObj);
            if (firstCube == null) continue;

            Shooter shooter = FindAvailableShooter(firstCube.Color);
            if (shooter == null) continue;

            _activeShots[baseObj] = shooter;
            shooter.Shoot(baseObj);
        }
    }

    private void CleanupActiveShots()
    {
        if (_activeShots.Count == 0) return;

        _shotsToRemove.Clear();
        foreach (var kvp in _activeShots)
        {
            Base baseObj = kvp.Key;
            Shooter shooter = kvp.Value;

            if (baseObj == null || baseObj.IsEmpty() || shooter == null || !shooter.IsShooting)
            {
                _shotsToRemove.Add(baseObj);
            }
        }

        for (int i = 0; i < _shotsToRemove.Count; i++)
        {
            _activeShots.Remove(_shotsToRemove[i]);
        }
        _shotsToRemove.Clear();
    }

    private Shooter FindAvailableShooter(ObjectColor color)
    {
        for (int i = 0; i < Holders.Length; i++)
        {
            Holder holder = Holders[i];
            if (holder == null || !holder.IsOccupied) continue;

            Shooter shooter = holder.ShooterOnholder;
            if (shooter == null) continue;
            if (shooter.IsMoving) continue;
            if (shooter.Remaining <= 0) continue;
            if (shooter.Color != color) continue;
            if (shooter.IsShooting) continue;

            return shooter;
        }

        return null;
    }

    private static Cube GetFirstCube(Base baseObj)
    {
        if (baseObj == null || baseObj.Slots == null) return null;

        for (int i = 0; i < baseObj.Slots.Count; i++)
        {
            Slot slot = baseObj.Slots[i];
            if (slot != null && slot.IsOccupied)
            {
                return slot.CubeOnSlot;
            }
        }

        return null;
    }

    public void RemoveFirstBaseFromQueue(SplineComputer splineQueue)
    {
        QueueLane lane = GetQueueLane(splineQueue);
        if (lane == null || lane.BasesQueue == null || lane.BasesQueue.Count == 0)
        {
            Debug.LogWarning("BasesQueue trống hoặc chưa được khởi tạo!");
            return;
        }

        Base firstBase = lane.BasesQueue[0];

        _queueBaseCurrentDistances.Remove(firstBase);
        _queueBaseVelocities.Remove(firstBase);

        lane.BasesQueue.RemoveAt(0);

        Destroy(firstBase.gameObject);

        _isQueueAnimating = true;

    }

    public void GenerateBasesOnConveyorQueue()
    {
        if (QueueLanes == null || QueueLanes.Count == 0)
        {
            Debug.LogWarning("Chưa có QueueLanes nào để tạo BasesQueue!");
            return;
        }

        foreach (var lane in QueueLanes)
        {
            if (lane == null) continue;
            GenerateBasesOnConveyorQueue(lane);
        }
    }
    private void GenerateBasesOnConveyorQueue(QueueLane lane)
    {
        if (lane.SplineQueue == null)
        {
            Debug.LogWarning("SplineQueue chưa được tạo! Hãy chạy GenerateSplineFromQueuePoints trước.");
            return;
        }

        int colorCount = lane.ColorsConveyorQueue != null ? lane.ColorsConveyorQueue.Count : 0;
        if (colorCount == 0) return;

        lane.SplineQueue.Rebuild();

        int requiredBases = Mathf.CeilToInt((float)colorCount / 5);
        float splineLength = lane.SplineQueue.CalculateLength();
        _queueSplineLengths[lane.SplineQueue] = splineLength;

        _queueBaseCurrentDistances.Clear();

        List<Base> newBases = new List<Base>();

        for (int i = 0; i < requiredBases; i++)
        {
            var newBase = Instantiate(Board.Instance.BasePrefab, lane.SplineQueue.transform);
            newBase.name = $"BaseQueue_{i:D3}";
            newBases.Add(newBase);
        }
        lane.BasesQueue = new List<Base>(newBases);

        for (int i = 0; i < newBases.Count; i++)
        {
            float distanceFromEnd = i * Defines.QUEUE_BASE_OFFSET;
            float distanceFromStart = splineLength - distanceFromEnd;
            distanceFromStart = Mathf.Clamp(distanceFromStart, 0f, splineLength);

            double percent = lane.SplineQueue.Travel(0, distanceFromStart);

            var follower = newBases[i].Positioner;
            follower.spline = lane.SplineQueue;
            follower.SetPercent(percent);

            SplineSample sample = lane.SplineQueue.Evaluate(percent);
            newBases[i].transform.SetPositionAndRotation(sample.position, sample.rotation);

            _queueBaseCurrentDistances[newBases[i]] = distanceFromStart;

        }

        int colorIndex = 0;
        int cubePerBase = colorCount / newBases.Count;
        int remainder = colorCount % newBases.Count;

        for (int i = 0; i < newBases.Count; i++)
        {
            int cubesForThisBase = cubePerBase + (remainder > 0 ? 1 : 0);
            if (remainder > 0) remainder--;

            for (int j = 0; j < cubesForThisBase; j++)
            {
                if (colorIndex >= colorCount) break;

                var cube = Instantiate(Board.Instance.CubePrefab, transform);
                cube.SetUp(lane.ColorsConveyorQueue[colorIndex], newBases[i]);

                newBases[i].AddCube(cube, immediate: true);

                colorIndex++;
            }
        }
    }

    public void GenerateBasesOnConveyor()
    {
        int colorCount = ColorsConveyor.Count;
        if (colorCount == 0) return;

        int requiredBases = Mathf.CeilToInt((float)colorCount / 5);
        List<Base> newBases = new List<Base>();

        for (int i = 0; i < requiredBases; i++)
        {
            var newBase = Instantiate(Board.Instance.BasePrefab, Spline.transform);
            newBase.name = $"Base_{i:D3}";
            newBases.Add(newBase);
        }
        Bases = new List<Base>(newBases);

        for (int i = 0; i < newBases.Count; i++)
        {
            double percent = (double)i / requiredBases;
            var follower = newBases[i].Positioner;
            follower.spline = Spline;
            follower.SetPercent(percent);

            SplineSample sample = Spline.Evaluate(percent);
            newBases[i].transform.SetPositionAndRotation(sample.position, sample.rotation);
        }

        int colorIndex = 0;
        int cubePerBase = colorCount / newBases.Count;
        int remainder = colorCount % newBases.Count;

        for (int i = 0; i < newBases.Count; i++)
        {
            int cubesForThisBase = cubePerBase + (remainder > 0 ? 1 : 0);
            if (remainder > 0) remainder--;

            for (int j = 0; j < cubesForThisBase; j++)
            {
                if (colorIndex >= colorCount) break;

                var cube = Instantiate(Board.Instance.CubePrefab, transform);
                cube.SetUp(ColorsConveyor[colorIndex], newBases[i]);

                newBases[i].AddCube(cube, immediate: true);

                colorIndex++;
            }
        }
    }
#if UNITY_EDITOR
    private CellData DrawCellDataWithPreview(Rect rect, CellData value)
    {
        if (value == null)
        {
            value = new CellData();
        }

        // Màu nền dựa trên IsWall
        Color bgColor = value.IsWall ? new Color(0.3f, 0.3f, 0.3f, 0.5f) : new Color(0.2f, 0.5f, 0.2f, 0.3f);
        EditorGUI.DrawRect(rect, bgColor);

        if (value.Shooter != null)
        {
            var preview = AssetPreview.GetAssetPreview(value.Shooter.gameObject);
            if (preview != null)
            {
                // Preview chiếm 70% chiều cao
                Rect previewRect = new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height * 0.7f);
                GUI.DrawTexture(previewRect, preview, ScaleMode.ScaleToFit);

                // Tên ngắn phía dưới
                Rect labelRect = new Rect(rect.x, rect.y + rect.height * 0.7f, rect.width, rect.height * 0.3f);
                GUI.Label(labelRect, value.Shooter.name, new GUIStyle()
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 9,
                    normal = new GUIStyleState() { textColor = Color.white }
                });
            }
        }
        else if (value.IsWall)
        {
            // Hiển thị "WALL" cho ô tường
            Rect labelRect = new Rect(rect.x, rect.y, rect.width, rect.height);
            GUI.Label(labelRect, "WALL", new GUIStyle()
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = new GUIStyleState() { textColor = Color.red }
            });
        }
        else
        {
            // Ô trống
            Rect labelRect = new Rect(rect.x, rect.y, rect.width, rect.height);
            GUI.Label(labelRect, "Empty", new GUIStyle()
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 8,
                normal = new GUIStyleState() { textColor = Color.gray }
            });
        }

        // Drag & drop cho Shooter
        Event evt = Event.current;
        if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
        {
            if (rect.Contains(evt.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    if (DragAndDrop.objectReferences.Length > 0)
                    {
                        var obj = DragAndDrop.objectReferences[0] as GameObject;
                        if (obj != null)
                        {
                            var shooter = obj.GetComponent<Shooter>();
                            if (shooter != null)
                            {
                                value.Shooter = shooter;
                                value.IsWall = false;
                            }
                        }
                    }
                    evt.Use();
                }
            }
        }

        // Click chuột phải để toggle Wall
        if (evt.type == EventType.MouseDown && evt.button == 1 && rect.Contains(evt.mousePosition))
        {
            value.IsWall = !value.IsWall;
            if (value.IsWall)
            {
                value.Shooter = null;
            }
            evt.Use();
        }

        return value;
    }
#endif
}
