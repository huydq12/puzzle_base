using System;
using System.Collections.Generic;
using System.Linq;
using Dreamteck.Splines;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
public class CellData
{
    public bool IsWall;
    public Shooter Shooter;
}
public class LevelMap : SerializedMonoBehaviour
{
    public List<ObjectColor> ColorsConveyor;
    public List<ObjectColor> ColorsConveyorQueue;
    public List<Transform> QueuePoints;
    public List<Base> Bases;
    public List<Base> BasesQueue;
    public Holder[] Holders;
    public SplineComputer Spline;
    public SplineComputer SplineQueue;
    [TableMatrix(DrawElementMethod = nameof(DrawCellDataWithPreview), SquareCells = true, RowHeight = 50)]
    public CellData[,] Grid;
    [Button]
    public void addcolor(ObjectColor color, int total)
    {
        for(int i = 1; i <= total; i++)
        {
            ColorsConveyor.Add(color);
        }
    }
    void Start()
    {
        CheckAllShootersCanMove();
    }
    [Button]
    public void CheckAllShootersCanMove()
    {
        if (Grid == null)
        {
            Debug.LogWarning("Grid chưa được khởi tạo!");
            return;
        }

        int cols = Grid.GetLength(0);
        int rows = Grid.GetLength(1);

        List<Shooter> canMoveShooters = new List<Shooter>();
        List<Shooter> cannotMoveShooters = new List<Shooter>();

        // Duyệt qua toàn bộ Grid
        for (int c = 0; c < cols; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                CellData cell = Grid[c, r];

                // Chỉ kiểm tra cell có shooter
                if (cell.Shooter != null)
                {
                    Vector2Int pos = new Vector2Int(c, r);

                    if (CheckCanMove(pos))
                    {
                        canMoveShooters.Add(cell.Shooter);

                        // TODO: Play animation A cho shooter này
                        // cell.Shooter.PlayAnimationA();
                        cell.Shooter.Show();
                    }
                    else
                    {
                        cannotMoveShooters.Add(cell.Shooter);

                        // TODO: Play animation B cho shooter này
                        // cell.Shooter.PlayAnimationB();
                        cell.Shooter.Hide();
                    }
                }
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
    [Button]
    public bool CheckCanMove(Vector2Int shooterPos)
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
        var visited = new bool[cols, rows];
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(shooterPos);
        visited[x, y] = true;

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
                if (visited[nx, ny]) continue;

                CellData nextCell = Grid[nx, ny];

                // ❌ Không đi được nếu là WALL
                if (nextCell.IsWall) continue;

                // ❌ Không đi được nếu có SHOOTER khác (shooter = vật cản giống wall)
                if (nextCell.Shooter != null) continue;

                // ✅ Ô trống → có thể đi qua
                visited[nx, ny] = true;
                queue.Enqueue(new Vector2Int(nx, ny));
            }
        }

        return false; // Không tìm được đường ra - bị chặn bởi wall hoặc shooter khác
    }
    private void Update()
    {
        // Di chuyển Bases trên Spline chính
        if (Bases.Count > 0)
        {
            float basePercent = Time.time * Board.Instance.Speed % 1f;
            int count = Bases.Count;

            for (int i = 0; i < count; i++)
            {
                float offset = (float)i / count;
                var b = Bases[i];
                b.Positioner.SetPercent((basePercent + offset) % 1f);
            }
        }

    }

    public void RecreateAllHolderPrefabs(
    GameObject holderPrefab)
    {
        List<GameObject> holders = GetComponentsInChildren<Transform>(true)
     .Select(t => t.gameObject)
     .Where(go =>
         go != null &&
         go.name.Contains("Shooter", StringComparison.OrdinalIgnoreCase) &&
         go.TryGetComponent<BoxCollider>(out _))
     .ToList();

        foreach (var go in holders)
        {
            if (go == null)
                continue;

            // --- Lưu transform ---
            Transform parent = go.transform.parent;
            int siblingIndex = go.transform.GetSiblingIndex();
            Vector3 localPos = go.transform.localPosition;
            Quaternion localRot = go.transform.localRotation;
            Vector3 localScale = go.transform.localScale;
            string oldName = go.name;
            // --- Destroy old ---
            Undo.DestroyObjectImmediate(go);

            // --- Instantiate prefab ---
            GameObject newHolder =
                (GameObject)PrefabUtility.InstantiatePrefab(holderPrefab);

            Undo.RegisterCreatedObjectUndo(newHolder, "Recreate Holder");

            newHolder.transform.SetParent(parent, false);
            newHolder.transform.SetSiblingIndex(siblingIndex);
            newHolder.transform.localPosition = localPos;
            newHolder.transform.localRotation = localRot;
            newHolder.transform.localScale = localScale;

            newHolder.name = oldName;
        }

    }
    private Transform FindChildContainName(Transform parent, string keyword)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return child;

            Transform found = FindChildContainName(child, keyword);
            if (found != null)
                return found;
        }
        return null;
    }


#if UNITY_EDITOR
    private Shooter DrawShooterWithPreview(Rect rect, Shooter value)
    {
        if (value != null)
        {
            var preview = UnityEditor.AssetPreview.GetAssetPreview(value.gameObject);
            if (preview != null)
            {
                // Preview chiếm 80% chiều cao
                Rect previewRect = new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height * 0.8f);
                GUI.DrawTexture(previewRect, preview, ScaleMode.ScaleToFit);

                // Tên ngắn phía dưới (20% chiều cao còn lại)
                Rect labelRect = new Rect(rect.x, rect.y + rect.height * 0.8f, rect.width, rect.height * 0.2f);
                GUI.Label(labelRect, value.name, new GUIStyle()
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 9
                });
            }
        }

        // Drag & drop
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
                            value = obj.GetComponent<Shooter>();
                        }
                    }
                    evt.Use();
                }
            }
        }

        return value;
    }
#endif
    [Button]
    public void ValidGrid()
    {
        Transform root = transform.GetComponentsInChildren<Transform>().Where(t => t.gameObject.name == "GridController").First();
        // Lấy tất cả các child transform (không quan tâm có Shooter hay không)
        List<Transform> allChildren = root.GetComponentsInChildren<Transform>(true)
            .Where(t => t != root && (t.TryGetComponent(out Shooter _) || t.gameObject.name.Contains("Wall")))
            .ToList();
        if (allChildren.Count == 0)
        {
            Debug.LogWarning("Không tìm thấy child nào!");
            return;
        }

        // ---------- Helper: cluster 1D ----------
        List<List<Transform>> Cluster(
            List<Transform> list,
            Func<Transform, float> selector)
        {
            list.Sort((a, b) => selector(a).CompareTo(selector(b)));

            List<float> values = list.Select(selector).ToList();
            values.Sort();

            List<float> diffs = new List<float>();
            for (int i = 1; i < values.Count; i++)
            {
                float d = Mathf.Abs(values[i] - values[i - 1]);
                if (d > 0.001f)
                    diffs.Add(d);
            }

            float spacing = diffs.Count > 0
                ? diffs[diffs.Count / 2]   // median
                : 1f;

            float threshold = spacing * 0.5f;

            List<List<Transform>> groups = new List<List<Transform>>();

            foreach (var t in list)
            {
                float v = selector(t);
                bool added = false;

                foreach (var g in groups)
                {
                    if (Mathf.Abs(v - selector(g[0])) <= threshold)
                    {
                        g.Add(t);
                        added = true;
                        break;
                    }
                }

                if (!added)
                    groups.Add(new List<Transform> { t });
            }

            return groups;
        }

        // ---------- 1. Cluster ROWS theo Z ----------
        var rows = Cluster(allChildren, t => t.position.z);

        // FIX ORDER: row 0 ở trên cùng (Z lớn → nhỏ)
        rows = rows
            .OrderByDescending(r => r.Average(t => t.position.z))
            .ToList();

        // ---------- 2. Cluster COLUMNS theo X (GLOBAL) ----------
        var cols = Cluster(allChildren, t => t.position.x);

        // FIX ORDER: col 0 ở bên trái (X nhỏ → lớn)
        cols = cols
            .OrderBy(c => c.Average(t => t.position.x))
            .ToList();

        int rowCount = rows.Count;
        int colCount = cols.Count;

        // Khởi tạo Grid mới
        Grid = new CellData[colCount, rowCount];

        // Khởi tạo tất cả CellData
        for (int c = 0; c < colCount; c++)
        {
            for (int r = 0; r < rowCount; r++)
            {
                Grid[c, r] = new CellData();
            }
        }

        // ---------- 3. Assign grid position ----------
        for (int r = 0; r < rowCount; r++)
        {
            foreach (var child in rows[r])
            {
                int c = cols.FindIndex(col => col.Contains(child));

                if (c < 0)
                {
                    Debug.LogWarning($"Cannot find column for {child.name}");
                    continue;
                }

                // Kiểm tra xem child có Shooter component không
                Shooter shooter = child.GetComponent<Shooter>();

                if (shooter != null)
                {
                    // Có Shooter → IsWall = false
                    Grid[c, r].Shooter = shooter;
                    Grid[c, r].IsWall = false;

                    // Cập nhật thông tin Shooter
                    shooter.GridPosition = new Vector2Int(c, r);
                    shooter.gameObject.name = $"Shooter ({c},{r})";
                }
                else
                {
                    // Không có Shooter → IsWall = true
                    Grid[c, r].Shooter = null;
                    Grid[c, r].IsWall = true;

                    child.gameObject.name = $"Wall ({c},{r})";
                }
            }
        }
    }


    private bool TryGetColorFromName(string name, out ObjectColor color)
    {
        foreach (ObjectColor c in Enum.GetValues(typeof(ObjectColor)))
        {
            if (name.Contains(c.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                color = c;
                return true;
            }
        }

        color = default;
        return false;
    }

    [Button]
    public void GenerateSplineFromQueuePoints()
    {
        if (QueuePoints == null || QueuePoints.Count < 2)
        {
            Debug.LogWarning("Cần ít nhất 2 QueuePoints để tạo Spline!");
            return;
        }

        // Tạo hoặc lấy SplineQueue
        if (SplineQueue == null)
        {
            var splineGO = new GameObject("SplineQueue");
            splineGO.transform.SetParent(transform);
            SplineQueue = splineGO.AddComponent<SplineComputer>();
        }

        // Tạo các SplinePoint từ QueuePoints
        var points = new SplinePoint[QueuePoints.Count];
        for (int i = 0; i < QueuePoints.Count; i++)
        {
            points[i] = new SplinePoint
            {
                position = QueuePoints[i].position,
                normal = Vector3.up,
                size = 1f,
                color = Color.white
            };
        }

        SplineQueue.SetPoints(points);
        SplineQueue.type = Spline.type;

        Debug.Log($"Đã tạo SplineQueue với {points.Length} điểm");
    }

    [SerializeField] private float _queueBaseOffset = 2f; // Khoảng cách giữa các Base trong queue

    [Button]
    public void GenerateBasesOnConveyorQueue()
    {
        if (SplineQueue == null)
        {
            Debug.LogWarning("SplineQueue chưa được tạo! Hãy chạy GenerateSplineFromQueuePoints trước.");
            return;
        }

        int colorCount = ColorsConveyorQueue.Count;
        if (colorCount == 0) return;

        // Rebuild spline để đảm bảo tính toán chính xác
        SplineQueue.Rebuild();

        int requiredBases = Mathf.CeilToInt((float)colorCount / 5);
        float splineLength = SplineQueue.CalculateLength();

        Debug.Log($"SplineQueue length: {splineLength}, requiredBases: {requiredBases}, offset: {_queueBaseOffset}");

        List<Base> newBases = new List<Base>();

        for (int i = 0; i < requiredBases; i++)
        {
            // Parent là transform của LevelMap, không phải SplineQueue
            var newBase = Instantiate(Board.Instance.BasePrefab, transform);
            newBase.name = $"BaseQueue_{i:D3}";
            newBases.Add(newBase);
        }
        BasesQueue = new List<Base>(newBases);

        // Base đầu tiên ở cuối spline (percent = 1), các base tiếp theo lùi về phía đầu
        for (int i = 0; i < newBases.Count; i++)
        {
            // Tính khoảng cách từ cuối spline
            float distanceFromEnd = i * _queueBaseOffset;
            // Chuyển sang percent (1 = cuối, 0 = đầu)
            double percent = 1.0 - (distanceFromEnd / splineLength);
            percent = Math.Max(0, percent); // Đảm bảo không âm

            SplineSample sample = SplineQueue.Evaluate(percent);
            newBases[i].transform.position = sample.position;
            newBases[i].transform.rotation = sample.rotation;

            Debug.Log($"Base {i}: percent={percent:F3}, pos={sample.position}");
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
                cube.SetUp(ColorsConveyorQueue[colorIndex], newBases[i]);
                newBases[i].AddCube(cube);

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
            newBases[i].transform.position = sample.position;
            newBases[i].transform.rotation = sample.rotation;
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
                newBases[i].AddCube(cube);

                colorIndex++;
            }
        }
    }
}
