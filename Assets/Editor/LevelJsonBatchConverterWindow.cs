using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class LevelJsonBatchConverterWindow : EditorWindow
{
    [SerializeField] private DefaultAsset jsonFolder;
    [SerializeField] private DefaultAsset soFolder;

    [SerializeField] private bool inferGridSizeFromData = true;
    [SerializeField] private bool normalizeCoordinatesToZero = true;
    [SerializeField] private bool normalizeShootersWithGrid = true;
    [SerializeField] private int defaultRows = 10;
    [SerializeField] private int defaultColumns = 10;

    [SerializeField] private Vector2 spacing = Vector2.one;

    private enum ShooterPositionMode
    {
        RawXZ,
        BoardLocalCentered
    }

    [SerializeField] private ShooterPositionMode shooterPositionMode = ShooterPositionMode.BoardLocalCentered;

    [MenuItem("Tools/Levels/Batch Convert JSON To SO")]
    private static void Open()
    {
        GetWindow<LevelJsonBatchConverterWindow>("Level JSON -> SO");
    }

    private void OnEnable()
    {
        if (jsonFolder == null)
        {
            jsonFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/Resources/Levels");
        }

        if (soFolder == null)
        {
            soFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/Resources/Levels/SO");
        }
    }

    private void OnGUI()
    {
        jsonFolder = (DefaultAsset)EditorGUILayout.ObjectField("JSON Folder", jsonFolder, typeof(DefaultAsset), false);
        soFolder = (DefaultAsset)EditorGUILayout.ObjectField("SO Folder", soFolder, typeof(DefaultAsset), false);

        inferGridSizeFromData = EditorGUILayout.Toggle("Infer Rows/Columns", inferGridSizeFromData);

        using (new EditorGUI.DisabledScope(!inferGridSizeFromData))
        {
            normalizeCoordinatesToZero = EditorGUILayout.Toggle("Normalize Coords To (0,0)", normalizeCoordinatesToZero);
            normalizeShootersWithGrid = EditorGUILayout.Toggle("Normalize Shooters With Grid", normalizeShootersWithGrid);
        }

        using (new EditorGUI.DisabledScope(inferGridSizeFromData))
        {
            defaultRows = EditorGUILayout.IntField("Default Rows", defaultRows);
            defaultColumns = EditorGUILayout.IntField("Default Columns", defaultColumns);
        }

        spacing = EditorGUILayout.Vector2Field("Spacing", spacing);

        shooterPositionMode = (ShooterPositionMode)EditorGUILayout.EnumPopup("Shooter Position Mode", shooterPositionMode);

        EditorGUILayout.Space(8);

        using (new EditorGUI.DisabledScope(jsonFolder == null || soFolder == null))
        {
            if (GUILayout.Button("Convert All"))
            {
                ConvertAll();
            }
        }
    }

    private void ConvertAll()
    {
        string jsonFolderPath = AssetDatabase.GetAssetPath(jsonFolder);
        string soFolderPath = AssetDatabase.GetAssetPath(soFolder);

        if (string.IsNullOrEmpty(jsonFolderPath) || string.IsNullOrEmpty(soFolderPath))
        {
            Debug.LogError("Invalid folder selection");
            return;
        }

        string[] allTextGuids = AssetDatabase.FindAssets("t:TextAsset", new[] { jsonFolderPath });
        if (allTextGuids == null || allTextGuids.Length == 0)
        {
            Debug.LogWarning($"No TextAsset found in folder: {jsonFolderPath}");
            return;
        }

        List<string> levelJsonGuids = new List<string>();
        for (int i = 0; i < allTextGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(allTextGuids[i]);
            if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;

            int level;
            if (!TryParseLevelIndexFromJsonPath(path, out level)) continue;

            levelJsonGuids.Add(allTextGuids[i]);
        }

        if (levelJsonGuids.Count == 0)
        {
            Debug.LogWarning($"No Level_*.json found in folder: {jsonFolderPath}");
            return;
        }

        Debug.Log($"Found {levelJsonGuids.Count} level json(s) in: {jsonFolderPath}");

        int converted = 0;
        int failed = 0;

        for (int i = 0; i < levelJsonGuids.Count; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(levelJsonGuids[i]);
            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (json == null)
            {
                Debug.LogError($"Failed to load TextAsset at path: {path}");
                failed++;
                continue;
            }

            int level;
            if (!TryParseLevelIndexFromJsonPath(path, out level))
            {
                Debug.LogError($"Failed to parse level index from: {path}");
                failed++;
                continue;
            }

            try
            {
                LevelJsonRoot root = JsonUtility.FromJson<LevelJsonRoot>(json.text);
                if (root == null)
                {
                    Debug.LogError($"JsonUtility returned null for: {path}");
                    failed++;
                    continue;
                }

                int arrowsCount = root.arrows != null ? root.arrows.Count : 0;
                int shootersCount = root.shooters != null ? root.shooters.Count : 0;
                int conveyorsCount = root.conveyors != null ? root.conveyors.Count : 0;
                Debug.Log($"Parsed: {path} | arrows={arrowsCount}, shooters={shootersCount}, conveyors={conveyorsCount}");

                Bounds2Int bounds = inferGridSizeFromData ? ComputeBounds(root) : new Bounds2Int(Vector2Int.zero, new Vector2Int(defaultColumns, defaultRows));
                Vector2Int originOffset = (inferGridSizeFromData && normalizeCoordinatesToZero) ? bounds.Min : Vector2Int.zero;
                Vector2Int shooterOffset = normalizeShootersWithGrid ? originOffset : Vector2Int.zero;

                Vector2Int size = inferGridSizeFromData
                    ? new Vector2Int(bounds.Size.x, bounds.Size.y)
                    : new Vector2Int(defaultColumns, defaultRows);

                Debug.Log($"Grid: {path} | size={size.x}x{size.y}, originOffset=({originOffset.x},{originOffset.y}), shooterOffset=({shooterOffset.x},{shooterOffset.y}), shooterMode={shooterPositionMode}");

                size.x = Mathf.Max(1, size.x);
                size.y = Mathf.Max(1, size.y);

                LevelConfig config = LoadOrCreateLevelConfig(soFolderPath, level);
                if (config == null)
                {
                    Debug.LogError($"Failed to load/create LevelConfig for level: {level}");
                    failed++;
                    continue;
                }

                ApplyRootToConfig(config, level, size.x, size.y, root, originOffset, shooterOffset);
                EditorUtility.SetDirty(config);
                int outShooterCount = config.Shooters != null ? config.Shooters.Count : 0;
                int outConveyorCount = (config.ConveyorLine != null && config.ConveyorLine.Cells != null) ? config.ConveyorLine.Cells.Count : 0;
                Debug.Log($"Converted: {path} -> {AssetDatabase.GetAssetPath(config)} | outShooters={outShooterCount}, outConveyorCells={outConveyorCount}");
                converted++;
            }
            catch (Exception e)
            {
                Debug.LogError($"Convert failed: {path} - {e.Message}");
                failed++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Convert done. Converted: {converted}, Failed: {failed}");
    }

    private void ApplyRootToConfig(LevelConfig config, int level, int columns, int rows, LevelJsonRoot root, Vector2Int originOffset, Vector2Int shooterOffset)
    {
        config.Level = level;
        config.Rows = rows;
        config.Columns = columns;

        GridCellData[,] newCells = new GridCellData[columns, rows];
        if (config.Cells != null)
        {
            int curColumns = config.Cells.GetLength(0);
            int curRows = config.Cells.GetLength(1);

            for (int col = 0; col < Mathf.Min(curColumns, columns); col++)
            {
                for (int row = 0; row < Mathf.Min(curRows, rows); row++)
                {
                    newCells[col, row] = config.Cells[col, row];
                }
            }
        }

        for (int col = 0; col < columns; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                if (newCells[col, row] == null)
                {
                    newCells[col, row] = new GridCellData();
                }
            }
        }

        for (int col = 0; col < columns; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                newCells[col, row].CellType = GridCellType.Normal;
            }
        }

        config.Cells = newCells;

        if (config.ColorLines == null) config.ColorLines = new List<ColorLine>();
        config.ColorLines.Clear();

        if (root.arrows != null)
        {
            for (int i = 0; i < root.arrows.Count; i++)
            {
                LevelJsonArrow arrow = root.arrows[i];
                if (arrow == null || arrow.unitPositions == null || arrow.unitPositions.Count == 0) continue;

                ColorLine line = new ColorLine();
                line.Color = (ObjectColor)arrow.color;
                line.Cells = new List<Vector2Int>();

                for (int p = 0; p < arrow.unitPositions.Count; p++)
                {
                    LevelJsonUnitPos pos = arrow.unitPositions[p];
                    line.Cells.Add(new Vector2Int(pos.x - originOffset.x, pos.y - originOffset.y));
                }

                config.ColorLines.Add(line);
            }
        }

        if (root.conveyors != null && root.conveyors.Count > 0 && root.conveyors[0] != null)
        {
            if (config.ConveyorLine == null) config.ConveyorLine = new ConveyorLine();
            if (config.ConveyorLine.Cells == null) config.ConveyorLine.Cells = new List<Vector2Int>();
            config.ConveyorLine.Cells.Clear();

            for (int c = 0; c < root.conveyors.Count; c++)
            {
                LevelJsonConveyor conveyor = root.conveyors[c];
                if (conveyor == null || conveyor.conveyorNodes == null) continue;

                for (int n = 0; n < conveyor.conveyorNodes.Count; n++)
                {
                    LevelJsonConveyorNode node = conveyor.conveyorNodes[n];
                    if (node == null || node.position == null) continue;
                    int x = Mathf.RoundToInt(node.position.x) - originOffset.x;
                    int y = Mathf.RoundToInt(node.position.z) - originOffset.y;
                    Vector2Int cell = new Vector2Int(x, y);
                    config.ConveyorLine.Cells.Add(cell);

                    if (x >= 0 && x < columns && y >= 0 && y < rows)
                    {
                        config.Cells[x, y].CellType = GridCellType.Conveyor;
                    }
                }
            }

            if (config.ConveyorLine.Cells.Count == 0)
            {
                config.ConveyorLine = null;
            }
        }
        else
        {
            config.ConveyorLine = null;
        }

        if (root.shooters != null)
        {
            if (config.Shooters == null) config.Shooters = new List<GateData>();
            config.Shooters.Clear();

            for (int i = 0; i < root.shooters.Count; i++)
            {
                LevelJsonShooter shooter = root.shooters[i];
                if (shooter == null) continue;

                GateData gate = new GateData();
                gate.Direction = shooter.direction;
                float px = shooter.position != null ? shooter.position.x : 0f;
                float py = shooter.position != null ? shooter.position.y : 0f;

                float gx = px - shooterOffset.x;
                float gy = py - shooterOffset.y;
                if (shooterPositionMode == ShooterPositionMode.BoardLocalCentered)
                {
                    Vector3 basePos = GridToLocalPosition(gx, gy, columns, rows);
                    Vector2Int dir = DirectionToGridVector(shooter.direction);
                    Vector3 edgeOffset = new Vector3(dir.x * spacing.x * 0.5f, 0f, dir.y * spacing.y * 0.5f);
                    gate.Position = basePos + edgeOffset;
                }
                else
                {
                    gate.Position = new Vector3(gx, 0f, gy);
                }

                Debug.Log($"Shooter[{i}] json=({px},{py}) normalized=({gx},{gy}) -> gate.Position=({gate.Position.x},{gate.Position.y},{gate.Position.z})");

                gate.Shooters = new List<ShooterData>();
                if (shooter.shooterUnits != null)
                {
                    for (int u = 0; u < shooter.shooterUnits.Count; u++)
                    {
                        LevelJsonShooterUnit unit = shooter.shooterUnits[u];
                        if (unit == null) continue;

                        ShooterData data = new ShooterData();
                        data.Color = (ObjectColor)unit.color;
                        data.Counter = unit.counter;
                        gate.Shooters.Add(data);
                    }
                }

                config.Shooters.Add(gate);
            }
        }
    }

    private Vector3 GridToLocalPosition(float gridX, float gridY, int columns, int rows)
    {
        float offsetX = (columns - 1) * spacing.x / 2f;
        float offsetY = (rows - 1) * spacing.y / 2f;

        float x = gridX * spacing.x - offsetX;
        float z = gridY * spacing.y - offsetY;

        return new Vector3(x, 0f, z);
    }

    private static Vector2Int DirectionToGridVector(int direction)
    {
        if (direction == 1) return Vector2Int.right;
        if (direction == 2) return Vector2Int.down;
        if (direction == 3) return Vector2Int.left;
        if (direction == 4) return Vector2Int.up;
        return Vector2Int.zero;
    }

    private static LevelConfig LoadOrCreateLevelConfig(string soFolderPath, int level)
    {
        string assetPath = soFolderPath.TrimEnd('/') + $"/Level {level}.asset";

        LevelConfig existing = AssetDatabase.LoadAssetAtPath<LevelConfig>(assetPath);
        if (existing != null) return existing;

        LevelConfig created = CreateInstance<LevelConfig>();
        AssetDatabase.CreateAsset(created, assetPath);
        Debug.Log($"Created LevelConfig: {assetPath}");
        return created;
    }

    private static bool TryParseLevelIndexFromJsonPath(string assetPath, out int level)
    {
        level = 0;
        string fileName = Path.GetFileNameWithoutExtension(assetPath);
        if (string.IsNullOrEmpty(fileName)) return false;

        Match m = Regex.Match(fileName, @"Level_(\d+)", RegexOptions.IgnoreCase);
        if (!m.Success) return false;

        return int.TryParse(m.Groups[1].Value, out level);
    }

    private readonly struct Bounds2Int
    {
        public readonly Vector2Int Min;
        public readonly Vector2Int Size;

        public Bounds2Int(Vector2Int min, Vector2Int size)
        {
            Min = min;
            Size = size;
        }
    }

    private static Bounds2Int ComputeBounds(LevelJsonRoot root)
    {
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;

        void Consider(int x, int y)
        {
            minX = Mathf.Min(minX, x);
            minY = Mathf.Min(minY, y);
            maxX = Mathf.Max(maxX, x);
            maxY = Mathf.Max(maxY, y);
        }

        if (root.arrows != null)
        {
            for (int i = 0; i < root.arrows.Count; i++)
            {
                LevelJsonArrow a = root.arrows[i];
                if (a == null || a.unitPositions == null) continue;
                for (int p = 0; p < a.unitPositions.Count; p++)
                {
                    LevelJsonUnitPos pos = a.unitPositions[p];
                    if (pos == null) continue;
                    Consider(pos.x, pos.y);
                }
            }
        }

        if (root.shooters != null)
        {
            for (int i = 0; i < root.shooters.Count; i++)
            {
                LevelJsonShooter s = root.shooters[i];
                if (s == null || s.position == null) continue;
                Consider(Mathf.RoundToInt(s.position.x), Mathf.RoundToInt(s.position.y));
            }
        }

        if (root.conveyors != null)
        {
            for (int c = 0; c < root.conveyors.Count; c++)
            {
                LevelJsonConveyor conveyor = root.conveyors[c];
                if (conveyor == null || conveyor.conveyorNodes == null) continue;
                for (int n = 0; n < conveyor.conveyorNodes.Count; n++)
                {
                    LevelJsonConveyorNode node = conveyor.conveyorNodes[n];
                    if (node == null || node.position == null) continue;
                    Consider(Mathf.RoundToInt(node.position.x), Mathf.RoundToInt(node.position.z));
                }
            }
        }

        if (minX == int.MaxValue)
        {
            return new Bounds2Int(Vector2Int.zero, Vector2Int.one);
        }

        Vector2Int min = new Vector2Int(minX, minY);
        Vector2Int size = new Vector2Int((maxX - minX) + 1, (maxY - minY) + 1);
        return new Bounds2Int(min, size);
    }

    [Serializable]
    public class LevelJsonRoot
    {
        public List<LevelJsonArrow> arrows;
        public List<LevelJsonShooter> shooters;
        public List<LevelJsonConveyor> conveyors;
    }

    [Serializable]
    public class LevelJsonArrow
    {
        public List<LevelJsonUnitPos> unitPositions;
        public int color;
        public int elementType;
        public int counter;
        public int arrowID;
    }

    [Serializable]
    public class LevelJsonUnitPos
    {
        public int x;
        public int y;
    }

    [Serializable]
    public class LevelJsonShooter
    {
        public LevelJsonFloat2 position;
        public int direction;
        public List<LevelJsonShooterUnit> shooterUnits;
    }

    [Serializable]
    public class LevelJsonShooterUnit
    {
        public int color;
        public int counter;
        public int type;
    }

    [Serializable]
    public class LevelJsonFloat2
    {
        public float x;
        public float y;
    }

    [Serializable]
    public class LevelJsonConveyor
    {
        public List<LevelJsonConveyorNode> conveyorNodes;
    }

    [Serializable]
    public class LevelJsonConveyorNode
    {
        public LevelJsonVector3 position;
        public bool isHole;
    }

    [Serializable]
    public class LevelJsonVector3
    {
        public float x;
        public float y;
        public float z;
    }
}
