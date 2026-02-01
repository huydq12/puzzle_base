using System;
using System.Collections.Generic;
using System.Linq;
using Dreamteck.Splines;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEditor;
using UnityEngine;

public class LevelMap : SerializedMonoBehaviour
{
    public List<ObjectColor> ColorsConveyor;
    public List<ObjectColor> ColorsConveyorQueue;
    public List<Base> Bases;
    public Holder[] Holders;
    public SplineComputer Spline;

    private void Update()
    {
        if (Bases.Count == 0) return;
        float basePercent = Time.time * Board.Instance.Speed % 1f;
        int count = Bases.Count;

        for (int i = 0; i < count; i++)
        {
            float offset = (float)i / count;
            var b = Bases[i];
            b.Positioner.SetPercent((basePercent + offset) % 1f);
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


    [SerializeField] private GameColorConfig _config;
    [TableMatrix(DrawElementMethod = nameof(DrawShooterWithPreview), SquareCells = true, RowHeight = 50)]
    [OdinSerialize] public Shooter[,] GridShooter;
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
        List<Shooter> shooters = GetComponentsInChildren<Shooter>(true).ToList();

        // ---------- Helper: cluster 1D ----------
        List<List<Shooter>> Cluster(
            List<Shooter> list,
            Func<Shooter, float> selector)
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

            List<List<Shooter>> groups = new List<List<Shooter>>();

            foreach (var s in list)
            {
                float v = selector(s);
                bool added = false;

                foreach (var g in groups)
                {
                    if (Mathf.Abs(v - selector(g[0])) <= threshold)
                    {
                        g.Add(s);
                        added = true;
                        break;
                    }
                }

                if (!added)
                    groups.Add(new List<Shooter> { s });
            }

            return groups;
        }

        // ---------- 1. Cluster ROWS theo Z ----------
        var rows = Cluster(shooters, s => s.transform.position.z);

        // FIX ORDER: row 0 ở trên cùng (Z lớn → nhỏ)
        rows = rows
            .OrderByDescending(r => r.Average(s => s.transform.position.z))
            .ToList();

        // ---------- 2. Cluster COLUMNS theo X (GLOBAL) ----------
        var cols = Cluster(shooters, s => s.transform.position.x);

        // FIX ORDER: col 0 ở bên trái (X nhỏ → lớn)
        cols = cols
            .OrderBy(c => c.Average(s => s.transform.position.x))
            .ToList();

        int rowCount = rows.Count;
        int colCount = cols.Count;

        GridShooter = new Shooter[colCount, rowCount];

        // ---------- 3. Assign grid position ----------
        for (int r = 0; r < rowCount; r++)
        {
            foreach (var shooter in rows[r])
            {
                int c = cols.FindIndex(col => col.Contains(shooter));

                if (c < 0)
                {
                    Debug.LogWarning($"Cannot find column for {shooter.name}");
                    continue;
                }

                GridShooter[c, r] = shooter;
                shooter.GridPosition = new Vector2Int(c, r);
                shooter.gameObject.name = $"Shooter ({c},{r})";
            }
        }
    }
    [Button]
    public void FixColor()
    {
        var shooters = GetComponentsInChildren<Shooter>(true);
        foreach (var shoot in shooters)
        {
            var mat = shoot.mat;
            foreach (var c in _config.ColorList)
            {
                if (mat.name.IndexOf(c.Value.Shooter.name) != -1)
                {
                    shoot.Color = c.Key;
                    break;
                }
            }
        }
    }
    [Button]
    private void ValidShooter()
    {
        var children = GetComponentsInChildren<Shooter>(true);

        foreach (var child in children)
        {

            if (TryGetColorFromName(child.name, out ObjectColor color))
            {
                child.ColorConfig = _config;
                // child.OnValidate();
                child.Setup(color, 0);
            }
            else
            {
                Debug.LogWarning($"[SetShooterColor] Không tìm được color trong name: {child.name}", child);
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

    public void GenerateBasesOnConveyorQueue()
    {

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
