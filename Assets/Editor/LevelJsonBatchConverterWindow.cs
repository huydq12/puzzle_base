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

    [SerializeField] private float shooterLocalXOffset = 0f;

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
            jsonFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/Levels");
        }

        if (soFolder == null)
        {
            soFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/SO");
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

        shooterLocalXOffset = EditorGUILayout.FloatField("Shooter Local X Offset", shooterLocalXOffset);

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
        // Optional camera fields (when present in JSON): cameraPosition + cameraOrthographicSize
        if (root != null)
        {
            if (root.cameraPosition != null)
            {
                config.Camera.Enabled = true;
                config.Camera.UsePosition = true;
                config.Camera.Position = new Vector3(root.cameraPosition.x, root.cameraPosition.y, root.cameraPosition.z);
            }

            if (root.cameraOrthographicSize > 0f)
            {
                config.Camera.Enabled = true;
                config.Camera.UseOrthographicSize = true;
                config.Camera.OrthographicSize = root.cameraOrthographicSize;
            }
        }

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
        if (config.Elevators == null) config.Elevators = new List<ElevatorData>();
        config.Elevators.Clear();
        if (config.LineDoors == null) config.LineDoors = new List<LineDoorData>();
        config.LineDoors.Clear();

        HashSet<string> seenLineKeys = new HashSet<string>();

        void AddArrowAsLine(LevelJsonArrow arrow)
        {
            if (arrow == null || arrow.unitPositions == null || arrow.unitPositions.Count == 0) return;

            // Deduplicate by (color + path).
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(arrow.color).Append('|');
            for (int k = 0; k < arrow.unitPositions.Count; k++)
            {
                LevelJsonUnitPos up = arrow.unitPositions[k];
                if (up == null) continue;
                sb.Append(up.x).Append(',').Append(up.y).Append(';');
            }

            string key = sb.ToString();
            if (!seenLineKeys.Add(key)) return;

            ColorLine line = new ColorLine();
            line.Color = ResolveJsonColor(arrow.color);
            line.Cells = new List<Vector2Int>();
            line.ElementTypes = new List<int>();
            line.Counter = arrow.counter;

            for (int p = arrow.unitPositions.Count - 1; p >= 0; p--)
            {
                LevelJsonUnitPos pos = arrow.unitPositions[p];
                if (pos == null) continue;
                line.Cells.Add(new Vector2Int(pos.x - originOffset.x, pos.y - originOffset.y));
                int elementType = pos.elementType != 0 ? pos.elementType : arrow.elementType;
                line.ElementTypes.Add(elementType);
            }

            if (line.Cells.Count > 0)
                config.ColorLines.Add(line);
        }

        // Old schema: root.arrows
        if (root.arrows != null)
        {
            for (int i = 0; i < root.arrows.Count; i++)
                AddArrowAsLine(root.arrows[i]);
        }

        // New schema: nested shooters.arrowData
        if (root.shooters != null)
        {
            for (int i = 0; i < root.shooters.Count; i++)
            {
                LevelJsonShooter shooter = root.shooters[i];
                if (shooter == null || shooter.arrowData == null) continue;
                for (int a = 0; a < shooter.arrowData.Count; a++)
                    AddArrowAsLine(shooter.arrowData[a]);
            }
        }
        
        // Elevator schema: root.elementData (type=4)
        if (root.elementData != null)
        {
            for (int e = 0; e < root.elementData.Count; e++)
            {
                LevelJsonElementData ed = root.elementData[e];
                if (ed == null) continue;
                if (ed.position == null || ed.size == null) continue;

                if (ed.type == 4)
                {
                    ElevatorData elevator = new ElevatorData();
                    elevator.Position = new Vector2Int(ed.position.x - originOffset.x, ed.position.y - originOffset.y);
                    elevator.Size = new Vector2Int(ed.size.x, ed.size.y);
                    elevator.Lines = new List<ColorLine>();

                    if (ed.arrowData != null)
                    {
                        for (int a = 0; a < ed.arrowData.Count; a++)
                        {
                            LevelJsonArrow arrow = ed.arrowData[a];
                            if (arrow == null || arrow.unitPositions == null || arrow.unitPositions.Count == 0) continue;

                            ColorLine line = new ColorLine();
                            line.Color = ResolveJsonColor(arrow.color);
                            line.Cells = new List<Vector2Int>();
                            line.ElementTypes = new List<int>();
                            line.Counter = arrow.counter;

                            for (int p = arrow.unitPositions.Count - 1; p >= 0; p--)
                            {
                                LevelJsonUnitPos pos = arrow.unitPositions[p];
                                if (pos == null) continue;
                                line.Cells.Add(new Vector2Int(pos.x - originOffset.x, pos.y - originOffset.y));
                                int elementType = pos.elementType != 0 ? pos.elementType : arrow.elementType;
                                line.ElementTypes.Add(elementType);
                            }

                            if (line.Cells.Count > 0)
                                elevator.Lines.Add(line);
                        }
                    }

                    config.Elevators.Add(elevator);
                    continue;
                }

                if (ed.type == 12)
                {
                    LineDoorData door = new LineDoorData();
                    door.Position = new Vector2Int(ed.position.x - originOffset.x, ed.position.y - originOffset.y);
                    door.Size = new Vector2Int(ed.size.x, ed.size.y);
                    door.Direction = ed.direction;
                    door.Color = ResolveJsonColor(ed.color);
                    door.Counter = ed.counter;
                    door.Lines = new List<ColorLine>();

                    if (ed.arrowData != null)
                    {
                        for (int a = 0; a < ed.arrowData.Count; a++)
                        {
                            LevelJsonArrow arrow = ed.arrowData[a];
                            if (arrow == null || arrow.unitPositions == null || arrow.unitPositions.Count == 0) continue;

                            ColorLine line = new ColorLine();
                            line.Color = ResolveJsonColor(arrow.color);
                            line.Cells = new List<Vector2Int>();
                            line.ElementTypes = new List<int>();
                            line.Counter = arrow.counter;

                            for (int p = arrow.unitPositions.Count - 1; p >= 0; p--)
                            {
                                LevelJsonUnitPos pos = arrow.unitPositions[p];
                                if (pos == null) continue;
                                line.Cells.Add(new Vector2Int(pos.x - originOffset.x, pos.y - originOffset.y));
                                int elementType = pos.elementType != 0 ? pos.elementType : arrow.elementType;
                                line.ElementTypes.Add(elementType);
                            }

                            if (line.Cells.Count > 0)
                                door.Lines.Add(line);
                        }
                    }

                    config.LineDoors.Add(door);
                }
            }
        }

        if (root.conveyors != null && root.conveyors.Count > 0 && root.conveyors[0] != null)
        {
            if (config.ConveyorLine == null) config.ConveyorLine = new ConveyorLine();
            if (config.ConveyorLine.Cells == null) config.ConveyorLine.Cells = new List<Vector2Int>();
            config.ConveyorLine.Cells.Clear();
            if (config.ConveyorLine.Types == null) config.ConveyorLine.Types = new List<int>();
            if (config.ConveyorLine.Counters == null) config.ConveyorLine.Counters = new List<int>();
            if (config.ConveyorLine.IsHoles == null) config.ConveyorLine.IsHoles = new List<bool>();
            config.ConveyorLine.Types.Clear();
            config.ConveyorLine.Counters.Clear();
            config.ConveyorLine.IsHoles.Clear();

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
                    config.ConveyorLine.IsHoles.Add(node.isHole);
                    config.ConveyorLine.Counters.Add(node.counter);
                    config.ConveyorLine.Types.Add(node.type);

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
            else
            {
                // Keep metadata lists aligned with Cells.
                int count = config.ConveyorLine.Cells.Count;
                while (config.ConveyorLine.IsHoles.Count < count) config.ConveyorLine.IsHoles.Add(false);
                while (config.ConveyorLine.Counters.Count < count) config.ConveyorLine.Counters.Add(0);
                while (config.ConveyorLine.Types.Count < count) config.ConveyorLine.Types.Add(0);
                if (config.ConveyorLine.IsHoles.Count > count) config.ConveyorLine.IsHoles.RemoveRange(count, config.ConveyorLine.IsHoles.Count - count);
                if (config.ConveyorLine.Counters.Count > count) config.ConveyorLine.Counters.RemoveRange(count, config.ConveyorLine.Counters.Count - count);
                if (config.ConveyorLine.Types.Count > count) config.ConveyorLine.Types.RemoveRange(count, config.ConveyorLine.Types.Count - count);
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
                gate.Counter = shooter.counter;
                gate.ElementType = shooter.elementType;
                float px = shooter.position != null ? shooter.position.x : 0f;
                float py = shooter.position != null ? shooter.position.y : 0f;

                float gx = px - shooterOffset.x;
                float gy = py - shooterOffset.y;
                if (shooterPositionMode == ShooterPositionMode.BoardLocalCentered)
                {
                    Vector3 basePos = GridToLocalPosition(gx, gy, columns, rows);
                    Vector2Int dir = DirectionToGridVector(shooter.direction);
                    Vector3 edgeOffset = new Vector3(dir.x * spacing.x * 0.5f, 0f, dir.y * spacing.y * 0.5f);
                    gate.Position = basePos + edgeOffset + new Vector3(shooterLocalXOffset, 0f, 0f);
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
                        data.Color = ResolveJsonColor(unit.color);
                        data.Counter = unit.counter;
                        data.Type = ResolveShooterElementType(unit);
                        data.TieID = unit.tieID;
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

        void ConsiderArrow(LevelJsonArrow arrow)
        {
            if (arrow == null || arrow.unitPositions == null) return;
            for (int p = 0; p < arrow.unitPositions.Count; p++)
            {
                LevelJsonUnitPos pos = arrow.unitPositions[p];
                if (pos == null) continue;
                Consider(pos.x, pos.y);
            }
        }

        if (root.arrows != null)
        {
            for (int i = 0; i < root.arrows.Count; i++)
            {
                ConsiderArrow(root.arrows[i]);
            }
        }

        if (root.shooters != null)
        {
            for (int i = 0; i < root.shooters.Count; i++)
            {
                LevelJsonShooter s = root.shooters[i];
                if (s == null || s.position == null) continue;
                Consider(Mathf.RoundToInt(s.position.x), Mathf.RoundToInt(s.position.y));

                if (s.arrowData != null)
                {
                    for (int a = 0; a < s.arrowData.Count; a++)
                        ConsiderArrow(s.arrowData[a]);
                }
            }
        }

        if (root.elementData != null)
        {
            for (int e = 0; e < root.elementData.Count; e++)
            {
                LevelJsonElementData ed = root.elementData[e];
                if (ed == null || ed.arrowData == null) continue;
                for (int a = 0; a < ed.arrowData.Count; a++)
                    ConsiderArrow(ed.arrowData[a]);
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
        public List<LevelJsonElementData> elementData;
        public LevelJsonVector3 cameraPosition;
        public float cameraOrthographicSize;
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
        public int elementType;
    }

    [Serializable]
    public class LevelJsonShooter
    {
        public LevelJsonFloat2 position;
        public int direction;
        public int counter;
        public int elementType;
        public List<LevelJsonShooterUnit> shooterUnits;
        public List<LevelJsonArrow> arrowData;
    }

    [Serializable]
    public class LevelJsonElementData
    {
        public LevelJsonInt2 position;
        public int direction;
        public int type;
        public LevelJsonInt2 size;
        public List<LevelJsonArrow> arrowData;
        public int counter;
        public int color;
    }

    [Serializable]
    public class LevelJsonInt2
    {
        public int x;
        public int y;
    }

    [Serializable]
    public class LevelJsonShooterUnit
    {
        public int color;
        public int counter;
        public int type;
        public int elementType;
        public int tieID;
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
        public int counter;
        public int type;
    }

    [Serializable]
    public class LevelJsonVector3
    {
        public float x;
        public float y;
        public float z;
    }

    private static ObjectColor ResolveJsonColor(int color)
    {
        if (color <= 0) return ObjectColor.None;
        int mapped = color - 1;
        if (mapped > (int)ObjectColor.White) return ObjectColor.None;
        return (ObjectColor)mapped;
    }

    private static int ResolveShooterElementType(LevelJsonShooterUnit unit)
    {
        if (unit == null) return 0;
        if (unit.elementType != 0 && unit.type != 0 && unit.elementType != unit.type)
        {
            Debug.LogWarning($"ShooterUnit has both type={unit.type} and elementType={unit.elementType}. Using elementType.");
        }
        return unit.elementType != 0 ? unit.elementType : unit.type;
    }

}
