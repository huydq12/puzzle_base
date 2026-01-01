#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using Sirenix.Utilities.Editor;

public enum SelectMode
{
    Edit,
    Empty,
}

public enum ColorType
{
    Cell,
    Outline,
    Label,
    Selected,
    Occupied,
    Empty,
}

[CustomEditor(typeof(LevelConfig))]
public class LevelConfigEditor : OdinEditor
{
    private float _hexSize = 30f;

    private string _outlineHexColor;
    private string _defaultHexColor;
    private string _labelHexColor;
    private string _selectedHexColor;
    private string _hasStackHexColor;
    private string _emptyHexColor;

    private Color DefaultColor => GetColor(ColorType.Cell);
    private Color LabelColor => GetColor(ColorType.Label);
    private Color OutlineColor => GetColor(ColorType.Outline);
    private Color SelectedColor => GetColor(ColorType.Selected);
    private Color OccupiedColor => GetColor(ColorType.Occupied);
    private Color EmptyColor => GetColor(ColorType.Empty);

    private Vector2Int? _selectedCell = null;
    private bool _showCellEditor = false;
    private bool _isColorsListExpanded = true;

    private ReorderableList _colorReorderableList;
    private SelectMode _currentSelectMode = SelectMode.Edit;
    private LevelConfig _levelconfig;
    private GridCellData _cellClipboard;
    private Vector2Int? _cellClipboardPos;

    [Serializable]
    private class LevelConfigJson
    {
        public int Level;
        public int Rows;
        public int Columns;
        public int TotalSlot;
        public List<LevelGoalData> Goals;
        public List<ObjectColor> Containers;
        public List<GridCellJson> Cells;
    }

    private GridCellData CloneCellData(GridCellData src)
    {
        if (src == null) return null;

        var dst = new GridCellData();
        dst.IsEmpty = src.IsEmpty;
        dst.ElementType = src.ElementType;
        dst.GateDirection = src.GateDirection;
        dst.IceHitPoints = src.IceHitPoints;
        dst.ScrewHitPoints = src.ScrewHitPoints;
        dst.LockItemCount = src.LockItemCount;
        dst.LockItemColor = src.LockItemColor;

        dst.Colors = src.Colors != null
            ? src.Colors.Select(c => new ColorData { Color = c != null ? c.Color : ObjectColor.Green }).ToList()
            : new List<ColorData>();

        if (src.GateWaves != null)
        {
            dst.GateWaves = src.GateWaves.Select(w => new GateWaveData
            {
                Colors = w != null && w.Colors != null ? new List<ObjectColor>(w.Colors) : new List<ObjectColor>()
            }).ToList();
        }
        else
        {
            dst.GateWaves = new List<GateWaveData>();
        }

        return dst;
    }

    [Serializable]
    private class GateWaveJson
    {
        public List<ObjectColor> Colors;
    }

    [Serializable]
    private class GridCellJson
    {
        public int X;
        public int Y;
        public bool IsEmpty;
        public CellElementType ElementType;
        public HexEdge GateDirection;
        public List<GateWaveJson> GateWaves;
        public int IceHitPoints;
        public int ScrewHitPoints;
        public int LockItemCount;
        public ObjectColor LockItemColor;
        public List<ObjectColor> Colors;
    }

    private new void OnEnable()
    {
        base.OnEnable();
        LoadColors();

        _levelconfig = (LevelConfig)target;
    }

    private string GetDefaultHex(ColorType type)
    {
        switch (type)
        {
            case ColorType.Outline: return "#000000ff";
            case ColorType.Cell: return "rgba(255, 255, 255, 1)";
            case ColorType.Label: return "#000000ff";
            case ColorType.Selected: return "#fffb06ff";
            case ColorType.Occupied: return "#2bff98ff";
            case ColorType.Empty: return "#ffffff00";
            default: return "#ffffffff";
        }
    }

    private Color GetColor(ColorType type)
    {
        string hex = type switch
        {
            ColorType.Outline => _outlineHexColor,
            ColorType.Cell => _defaultHexColor,
            ColorType.Label => _labelHexColor,
            ColorType.Selected => _selectedHexColor,
            ColorType.Occupied => _hasStackHexColor,
            ColorType.Empty => _emptyHexColor,
            _ => "#ffffffff"
        };
        return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.white;
    }

    private void SetColor(ColorType type, string hex)
    {
        switch (type)
        {
            case ColorType.Outline:
                _outlineHexColor = hex;
                break;
            case ColorType.Cell:
                _defaultHexColor = hex;
                break;
            case ColorType.Label:
                _labelHexColor = hex;
                break;
            case ColorType.Selected:
                _selectedHexColor = hex;
                break;
            case ColorType.Occupied:
                _hasStackHexColor = hex;
                break;
            case ColorType.Empty:
                _emptyHexColor = hex;
                break;
        }
        EditorPrefs.SetString(type.ToString(), hex);
    }

    private void LoadColors()
    {
        _outlineHexColor = EditorPrefs.GetString(ColorType.Outline.ToString(), GetDefaultHex(ColorType.Outline));
        _defaultHexColor = EditorPrefs.GetString(ColorType.Cell.ToString(), GetDefaultHex(ColorType.Cell));
        _labelHexColor = EditorPrefs.GetString(ColorType.Label.ToString(), GetDefaultHex(ColorType.Label));
        _selectedHexColor = EditorPrefs.GetString(ColorType.Selected.ToString(), GetDefaultHex(ColorType.Selected));
        _hasStackHexColor = EditorPrefs.GetString(ColorType.Occupied.ToString(), GetDefaultHex(ColorType.Occupied));
        _emptyHexColor = EditorPrefs.GetString(ColorType.Empty.ToString(), GetDefaultHex(ColorType.Empty));
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        DrawJsonButtons();

        DrawSelectModeButtons();
        DrawHexagonGrid();

        if (_selectedCell.HasValue && _showCellEditor)
        {
            if (_currentSelectMode == SelectMode.Edit)
            {
                DrawCellEditor();
            }
            else
            {
                _selectedCell = null;
            }
        }
    }

    private void DrawJsonButtons()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Import JSON", GUILayout.Width(150), GUILayout.Height(24)))
        {
            ImportJson();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Export JSON", GUILayout.Width(150), GUILayout.Height(24)))
        {
            ExportJson();
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(6);
    }

    private void ImportJson()
    {
        if (_levelconfig == null)
        {
            _levelconfig = (LevelConfig)target;
        }

        string path = EditorUtility.OpenFilePanel("Import LevelConfig JSON", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            var importData = JsonUtility.FromJson<LevelConfigJson>(json);
            if (importData == null)
            {
                EditorUtility.DisplayDialog("Import JSON", "Invalid json", "OK");
                return;
            }

            Undo.RecordObject(_levelconfig, "Import LevelConfig JSON");

            ApplyImportData(importData);

            _showCellEditor = false;
            _selectedCell = null;

            EditorUtility.SetDirty(_levelconfig);
            AssetDatabase.SaveAssets();

            if (path.StartsWith(Application.dataPath, StringComparison.OrdinalIgnoreCase))
            {
                AssetDatabase.Refresh();
            }

            Repaint();
            GUI.changed = true;
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Import JSON", e.Message, "OK");
        }
    }

    private void ApplyImportData(LevelConfigJson importData)
    {
        _levelconfig.SetLevel(importData.Level);
        _levelconfig.SetRows(importData.Rows);
        _levelconfig.SetColumns(importData.Columns);
        _levelconfig.TotalSlot = importData.TotalSlot;

        _levelconfig.Goals = importData.Goals != null ? new List<LevelGoalData>(importData.Goals) : new List<LevelGoalData>();
        _levelconfig.SetContainers(importData.Containers != null ? new List<ObjectColor>(importData.Containers) : new List<ObjectColor>());

        int cols = _levelconfig.GetColumns();
        int rows = _levelconfig.GetRows();
        _levelconfig.GridCellData = new GridCellData[cols, rows];
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                _levelconfig.GridCellData[x, y] = new GridCellData { IsEmpty = false, Colors = new List<ColorData>() };
            }
        }

        if (importData.Cells == null) return;

        for (int i = 0; i < importData.Cells.Count; i++)
        {
            var c = importData.Cells[i];
            if (c == null) continue;
            if (c.X < 0 || c.X >= cols) continue;
            if (c.Y < 0 || c.Y >= rows) continue;

            var cell = _levelconfig.GridCellData[c.X, c.Y];
            if (cell == null)
            {
                cell = new GridCellData { Colors = new List<ColorData>() };
                _levelconfig.GridCellData[c.X, c.Y] = cell;
            }

            cell.IsEmpty = c.IsEmpty;
            cell.ElementType = c.ElementType;
            cell.GateDirection = c.GateDirection;

            if (cell.GateWaves == null)
            {
                cell.GateWaves = new List<GateWaveData>();
            }
            cell.GateWaves.Clear();
            if (c.GateWaves != null)
            {
                for (int k = 0; k < c.GateWaves.Count; k++)
                {
                    var w = c.GateWaves[k];
                    if (w == null) continue;

                    var wave = new GateWaveData();
                    wave.Colors = w.Colors != null ? new List<ObjectColor>(w.Colors) : new List<ObjectColor>();
                    cell.GateWaves.Add(wave);
                }
            }
            cell.IceHitPoints = Mathf.Max(0, c.IceHitPoints);
            cell.ScrewHitPoints = Mathf.Max(0, c.ScrewHitPoints);
            cell.LockItemCount = Mathf.Max(0, c.LockItemCount);
            cell.LockItemColor = c.LockItemColor;

            if (cell.Colors == null)
            {
                cell.Colors = new List<ColorData>();
            }
            cell.Colors.Clear();
            if (c.Colors != null)
            {
                for (int k = 0; k < c.Colors.Count; k++)
                {
                    cell.Colors.Add(new ColorData { Color = c.Colors[k] });
                }
            }
        }
    }

    private void ExportJson()
    {
        if (_levelconfig == null)
        {
            _levelconfig = (LevelConfig)target;
        }

        string defaultName = _levelconfig != null ? $"Level_{_levelconfig.GetLevel()}" : "LevelConfig";
        string path = EditorUtility.SaveFilePanel("Export LevelConfig JSON", Application.dataPath, defaultName, "json");
        if (string.IsNullOrEmpty(path)) return;

        var exportData = BuildExportData();
        string json = JsonUtility.ToJson(exportData, true);
        File.WriteAllText(path, json, Encoding.UTF8);

        if (path.StartsWith(Application.dataPath, StringComparison.OrdinalIgnoreCase))
        {
            AssetDatabase.Refresh();
        }

        EditorUtility.DisplayDialog("Export JSON", "Export success", "OK");
    }

    private LevelConfigJson BuildExportData()
    {
        var exportData = new LevelConfigJson
        {
            Level = _levelconfig.GetLevel(),
            Rows = _levelconfig.GetRows(),
            Columns = _levelconfig.GetColumns(),
            TotalSlot = _levelconfig.TotalSlot,
            Goals = _levelconfig.Goals != null ? new List<LevelGoalData>(_levelconfig.Goals) : new List<LevelGoalData>(),
            Containers = _levelconfig.GetContainers() != null ? new List<ObjectColor>(_levelconfig.GetContainers()) : new List<ObjectColor>(),
            Cells = new List<GridCellJson>()
        };

        if (_levelconfig.GridCellData == null)
        {
            return exportData;
        }

        int width = _levelconfig.GridCellData.GetLength(0);
        int height = _levelconfig.GridCellData.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var cell = _levelconfig.GridCellData[x, y];

                var cellJson = new GridCellJson
                {
                    X = x,
                    Y = y,
                    IsEmpty = cell == null || cell.IsEmpty,
                    ElementType = cell != null ? cell.ElementType : CellElementType.None,
                    GateDirection = cell != null ? cell.GateDirection : HexEdge.None,
                    GateWaves = cell != null && cell.GateWaves != null
                        ? cell.GateWaves.Select(w => new GateWaveJson
                        {
                            Colors = w != null && w.Colors != null ? new List<ObjectColor>(w.Colors) : new List<ObjectColor>()
                        }).ToList()
                        : new List<GateWaveJson>(),
                    IceHitPoints = cell != null ? cell.IceHitPoints : 0,
                    ScrewHitPoints = cell != null ? cell.ScrewHitPoints : 0,
                    LockItemCount = cell != null ? cell.LockItemCount : 0,
                    LockItemColor = cell != null ? cell.LockItemColor : default(ObjectColor),
                    Colors = cell != null && cell.Colors != null
                        ? cell.Colors.Select(c => c != null ? c.Color : default(ObjectColor)).ToList()
                        : new List<ObjectColor>()
                };

                exportData.Cells.Add(cellJson);
            }
        }

        return exportData;
    }

    private void DrawClearButton(Rect rect)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.padding = new RectOffset(8, 8, 4, 4);
        if (SirenixEditorGUI.IconButton(rect, EditorIcons.Refresh))
        {
            if (EditorUtility.DisplayDialog("Xác nhận xoá", "Bạn có chắc chắn muốn xoá toàn bộ dữ liệu lưới không?", "Xoá", "Huỷ"))
            {
                int cols = _levelconfig.GetColumns();
                int rows = _levelconfig.GetRows();
                _levelconfig.GridCellData = new GridCellData[cols, rows];
                for (int i = 0; i < cols; i++)
                {
                    for (int j = 0; j < rows; j++)
                    {
                        _levelconfig.GridCellData[i, j] = new GridCellData(){ Colors = new List<ColorData>(), IsEmpty = false };
                    }
                }
                EditorUtility.SetDirty(target);
            }
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSettingsBoardButton(Rect rect)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.padding = new RectOffset(8, 8, 4, 4);
        if (SirenixEditorGUI.IconButton(rect, EditorIcons.SettingsCog))
        {
            PopupWindow.Show(rect, new ColorSettingPopup(this));
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawColorSampleField(string label, ref string hexColorField, ColorType type)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(120));
        Color currentColor = ColorUtility.TryParseHtmlString(hexColorField, out var c) ? c : Color.white;
        EditorGUI.BeginChangeCheck();
        Color newColor = EditorGUILayout.ColorField(currentColor);
        if (EditorGUI.EndChangeCheck())
        {
            hexColorField = "#" + ColorUtility.ToHtmlStringRGBA(newColor);
            SetColor(type, hexColorField);
            EditorUtility.SetDirty(target);
            Repaint();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(2);
    }


    private void DrawSelectModeButtons()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        // Tạo mảng tên cho các mode
        string[] toolbarOptions = System.Enum.GetNames(typeof(SelectMode));

        int previousMode = (int)_currentSelectMode;
        int newMode = GUILayout.Toolbar(previousMode, toolbarOptions, GUILayout.Width(320), GUILayout.Height(30));

        if (newMode != previousMode)
        {
            _currentSelectMode = (SelectMode)newMode;
            GUI.changed = true;
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

    }

    private void DrawHexagonGrid()
    {
        int Width = _levelconfig.GetColumns();
        int Height = _levelconfig.GetRows();

        // Calculate hexagon dimensions
        float hexWidth = _hexSize * 2f;
        float hexHeight = Mathf.Sqrt(3f) * _hexSize;
        float horizSpacing = hexWidth * 0.75f;
        float vertSpacing = hexHeight;

        float paddingY = hexHeight * 0.1f;

        // Tìm Y min và max thực tế để tính đúng bounds
        float minZ = 0;
        float maxZ = (Height - 1) * vertSpacing;

        // Kiểm tra có cột lẻ không để điều chỉnh bounds
        bool hasOddColumn = false;
        for (int x = 0; x < Width; x++)
        {
            if (x % 2 == 1)
            {
                hasOddColumn = true;
                break;
            }
        }

        // Nếu có cột lẻ, chúng sẽ offset lên trên vertSpacing/2
        if (hasOddColumn)
        {
            minZ -= vertSpacing / 2f; // Cột lẻ sẽ cao hơn
        }

        // Tính tổng chiều cao dựa trên bounds thực tế
        float gridHeight = maxZ - minZ;
        float totalWidth = (Width - 1) * horizSpacing + hexWidth;
        float totalHeight = gridHeight + hexHeight + paddingY * 2;

        // Get rect for the grid in the inspector
        Rect gridRect = GUILayoutUtility.GetRect(totalWidth, totalHeight);
        float centerOffsetX = (gridRect.width - totalWidth) / 2;

        Rect settingButtonRect = new Rect(
            gridRect.x + centerOffsetX + totalWidth,
            gridRect.y + paddingY,
            20, 20
        );

        DrawSettingsBoardButton(settingButtonRect);

        Rect clearButtonRect = new Rect(
       gridRect.x + centerOffsetX + totalWidth,  // Cùng vị trí x
       gridRect.y + paddingY + 20 + 5,           // y = y của nút trên + chiều cao nút (20) + khoảng cách (5)
       20, 20
   );

        DrawClearButton(clearButtonRect);


        Vector2 offset = new Vector2(
            gridRect.x + centerOffsetX + hexWidth / 2,
            gridRect.y + paddingY + hexHeight / 2 - minZ
        );

        Event e = Event.current;

        for (int x = 0; x < Width; x++)
        {
            for (int z = 0; z < Height; z++)
            {
                // Check if cell is empty
                bool isEmpty = _levelconfig.GridCellData[x, z] != null && _levelconfig.GridCellData[x, z].IsEmpty;


                // Calculate position
                float xPos = x * horizSpacing;
                float zPos = z * vertSpacing;
                if (x % 2 == 1)
                {
                    zPos -= vertSpacing / 2f; // Giữ nguyên như cũ
                }

                Vector2 hexCenter = new Vector2(xPos, zPos) + offset;

                // Calculate vertices for hexagon
                Vector3[] vertices = GetHexVertices(hexCenter);

                // Check for mouse hover and handle click
                if (IsPointInPolygon(e.mousePosition, vertices))
                {
                    if (e.type == EventType.MouseDown && e.button == 0)
                    {
                        Vector2Int cellpos = new Vector2Int(x, z);
                        HandleHexagonClick(cellpos);
                        e.Use();
                    }
                }

                Color cellColor;
                Vector2Int cellPos = new Vector2Int(x, z);
                var dataView = _levelconfig.GridCellData[cellPos.x, cellPos.y];

                if (isEmpty)
                {
                    cellColor = EmptyColor;
                }
                else
                {
                    if (dataView != null)
                    {

                        if (!dataView.Colors.IsNullOrEmpty())
                        {
                            cellColor = OccupiedColor;
                        }
                        else
                        {
                            cellColor = DefaultColor;
                        }
                    }
                    else
                    {
                        cellColor = DefaultColor;
                    }
                }

                if (!isEmpty && dataView != null && dataView.ElementType != CellElementType.None)
                {
                    cellColor = Color.red;
                }
                if (_selectedCell == cellPos)
                {
                    cellColor = SelectedColor;
                }
                DrawHexagon(hexCenter, cellColor);
                DrawHexagonLabel(hexCenter, $"({x},{z})");
            }
        }

        // Handle repaint
        if (e.type == EventType.MouseMove || e.type == EventType.MouseDown)
        {
            Repaint();
        }
    }

    private void HandleHexagonClick(Vector2Int cellPos)
    {
        switch (_currentSelectMode)
        {
            case SelectMode.Edit:
                if (_levelconfig.GridCellData[cellPos.x, cellPos.y].IsEmpty)
                {
                    EditorUtility.DisplayDialog("Warning", "Cannot edit empty cell", "OK");
                    return;
                }
                _selectedCell = cellPos;
                _showCellEditor = true;
                break;

            case SelectMode.Empty:
                _levelconfig.GridCellData[cellPos.x, cellPos.y].IsEmpty = !_levelconfig.GridCellData[cellPos.x, cellPos.y].IsEmpty;
                EditorUtility.SetDirty(_levelconfig);
                break;
        }

        GUI.changed = true;
    }

    private Vector3[] GetHexVertices(Vector2 center)
    {
        Vector3[] vertices = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f * Mathf.Deg2Rad;
            vertices[i] = new Vector3(
                center.x + _hexSize * Mathf.Cos(angle),
                center.y + _hexSize * Mathf.Sin(angle),
                0f
            );
        }
        return vertices;
    }

    private bool IsPointInPolygon(Vector2 point, Vector3[] polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            if (((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
                (point.x < (polygon[j].x - polygon[i].x) *
                (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x))
            {
                inside = !inside;
            }
        }
        return inside;
    }

    private void DrawHexagon(Vector2 center, Color fillColor)
    {
        // Tính 6 đỉnh của hexagon
        Vector3[] vertices = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f * Mathf.Deg2Rad;
            vertices[i] = new Vector3(
                center.x + _hexSize * Mathf.Cos(angle),
                center.y + _hexSize * Mathf.Sin(angle),
                0f
            );
        }

        // Vẽ fill hexagon với màu có alpha
        Color oldColor = Handles.color;
        Handles.color = fillColor;
        Handles.DrawAAConvexPolygon(vertices);

        // Vẽ outline
        Handles.color = OutlineColor;
        for (int i = 0; i < 6; i++)
        {
            int next = (i + 1) % 6;
            Handles.DrawLine(vertices[i], vertices[next]);
        }

        Handles.color = oldColor;
    }

    private void DrawHexagonLabel(Vector2 center, string text)
    {
        var labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.normal.textColor = LabelColor;
        labelStyle.fontSize = 12;

        labelStyle.hover.textColor = Color.black;

        Vector2 textSize = labelStyle.CalcSize(new GUIContent(text));
        Rect labelRect = new Rect(
            center.x - textSize.x * 0.5f,
            center.y - textSize.y * 0.5f,
            textSize.x,
            textSize.y
        );

        GUI.Label(labelRect, text, labelStyle);
    }

    private void DrawCellEditor()
    {
        if (!_selectedCell.HasValue) { return; }
        GUILayout.Space(20);
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Editing Cell ({_selectedCell.Value.x}, {_selectedCell.Value.y})",
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleCenter });

            if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(20)))
            {
                _showCellEditor = false;
                _selectedCell = null;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider, GUILayout.Width(350));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        var dataView = _levelconfig.GridCellData[_selectedCell.Value.x, _selectedCell.Value.y];

        if (dataView == null)
        {
            dataView = new GridCellData();
            dataView.IsEmpty = false;
            dataView.Colors = new List<ColorData>();
            _levelconfig.GridCellData[_selectedCell.Value.x, _selectedCell.Value.y] = dataView;
            EditorUtility.SetDirty(_levelconfig);
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginVertical(GUILayout.Width(350));
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Copy", GUILayout.Width(70), GUILayout.Height(20)))
        {
            _cellClipboard = CloneCellData(dataView);
            _cellClipboardPos = _selectedCell;
        }

        GUI.enabled = _cellClipboard != null;
        if (GUILayout.Button("Paste", GUILayout.Width(70), GUILayout.Height(20)))
        {
            var cloned = CloneCellData(_cellClipboard);
            _levelconfig.GridCellData[_selectedCell.Value.x, _selectedCell.Value.y] = cloned;
            dataView = cloned;
            EditorUtility.SetDirty(_levelconfig);
        }
        GUI.enabled = true;

        if (_cellClipboardPos.HasValue)
        {
            GUILayout.Space(8);
            GUILayout.Label($"From ({_cellClipboardPos.Value.x}, {_cellClipboardPos.Value.y})", EditorStyles.miniLabel);
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        DrawRegularCellEditor(dataView, _levelconfig);

        EditorGUILayout.EndVertical();
        GUILayout.Space(20);
    }

    private void DrawRegularCellEditor(GridCellData data, LevelConfig levelConfig)
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical(GUILayout.Width(350));
        EditorGUILayout.Space(6);

        EditorGUI.BeginChangeCheck();
        data.ElementType = (CellElementType)EditorGUILayout.EnumPopup("Element", data.ElementType);
        if (data.ElementType == CellElementType.Ice)
        {
            data.IceHitPoints = EditorGUILayout.IntField("Ice HP", Mathf.Max(0, data.IceHitPoints));
        }
        else if (data.ElementType == CellElementType.Screw)
        {
            data.ScrewHitPoints = EditorGUILayout.IntField("Screw HP", Mathf.Max(0, data.ScrewHitPoints));
        }
        else if (data.ElementType == CellElementType.Gate)
        {
            data.GateDirection = (HexEdge)EditorGUILayout.EnumPopup("Gate Dir", data.GateDirection);
            DrawGateWavesEditor(data, levelConfig);
        }
        else if (data.ElementType == CellElementType.LockItem)
        {
            data.LockItemCount = EditorGUILayout.IntField("Lock Count", Mathf.Max(0, data.LockItemCount));
        }
        else if (data.ElementType == CellElementType.LockItemColor)
        {
            data.LockItemCount = EditorGUILayout.IntField("Lock Count", Mathf.Max(0, data.LockItemCount));
            data.LockItemColor = (ObjectColor)EditorGUILayout.EnumPopup("Lock Color", data.LockItemColor);
        }
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(levelConfig);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);

        // Phần hiển thị colors
        if (data.Colors == null)
        {
            data.Colors = new List<ColorData>();
        }

        // Khởi tạo ReorderableList nếu cần
        if (_colorReorderableList == null || _colorReorderableList.list != data.Colors)
        {
            _colorReorderableList = new ReorderableList(data.Colors, typeof(ColorData), true, true, false, false);

            // Tiêu đề của danh sách với nút Add/Remove/Clear và Foldout
            _colorReorderableList.drawHeaderCallback = (Rect rect) =>
     {
         float buttonWidth = 30f;
         float clearButtonWidth = 60f;
         float buttonHeight = EditorGUIUtility.singleLineHeight;

         // Bắt đầu tính từ cạnh phải của list
         float right = rect.x + rect.width + 5;

         // --- Random (ngoài cùng bên phải) ---
         Rect randomRect = new Rect(
             right - buttonWidth,
             rect.y,
             buttonWidth,
             buttonHeight
         );
         right -= buttonWidth;
         if (GUI.Button(randomRect, EditorGUIUtility.IconContent("Refresh"), EditorStyles.miniButtonRight))
         {
             var containers = _levelconfig.GetContainers();
             foreach (var c in data.Colors)
                 c.Color = containers[UnityEngine.Random.Range(0, containers.Count)];

            data.Colors = data.Colors.GroupBy(c => c.Color).SelectMany(g => g).ToList();
             EditorUtility.SetDirty(levelConfig);
         }

         // --- Clear ---
         Rect clearRect = new Rect(
             right - clearButtonWidth,
             rect.y,
             clearButtonWidth,
             buttonHeight
         );
         right -= clearButtonWidth;
         if (GUI.Button(clearRect, "Clear", EditorStyles.miniButtonMid))
         {
             data.Colors.Clear();
             EditorUtility.SetDirty(levelConfig);
         }

         // --- Remove ---
         Rect removeRect = new Rect(
             right - buttonWidth,
             rect.y,
             buttonWidth,
             buttonHeight
         );
         right -= buttonWidth;

         GUI.enabled = data.Colors.Count > 0;
         if (GUI.Button(removeRect, "-", EditorStyles.miniButtonMid))
         {
             int target = (_colorReorderableList.index >= 0 && _colorReorderableList.index < data.Colors.Count)
                         ? _colorReorderableList.index
                         : data.Colors.Count - 1;

             if (target >= 0)
             {
                 data.Colors.RemoveAt(target);
                 EditorUtility.SetDirty(levelConfig);
             }
         }
         GUI.enabled = true;

         // --- Add ---
         Rect addRect = new Rect(
             right - buttonWidth,
             rect.y,
             buttonWidth,
             buttonHeight
         );
         right -= buttonWidth;

         if (GUI.Button(addRect, "+", EditorStyles.miniButtonLeft))
         {
             ObjectColor newColor = data.Colors.Count > 0 ? data.Colors[^1].Color : ObjectColor.Green;
             data.Colors.Add(new ColorData { Color = newColor });
             EditorUtility.SetDirty(levelConfig);
         }

         // --- Foldout (chiếm phần còn lại bên trái) ---
         Rect foldoutRect = new Rect(
             rect.x + 10,
             rect.y,
             right - rect.x - 10,
             rect.height
         );

         GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
         _isColorsListExpanded = EditorGUI.Foldout(
             foldoutRect,
             _isColorsListExpanded,
             $"Cell Colors ({data.Colors.Count})",
             true,
             foldoutStyle
         );
     };


            // Vẽ từng phần tử
            _colorReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
   {
       var colorData = data.Colors[index];

       float spacing = 5f;
       float removeWidth = 25f;

       // Toàn bộ width còn lại dành cho color field
       float colorWidth = rect.width - removeWidth - spacing * 2;

       float startX = rect.x;
       float y = rect.y + 2;

       // --- FIELD COLOR ---
       Rect colorRect = new Rect(startX, y, colorWidth, EditorGUIUtility.singleLineHeight);
       EditorGUI.BeginChangeCheck();
       colorData.Color = Common.DrawObjectColor(colorRect, colorData.Color);
       if (EditorGUI.EndChangeCheck())
       {
           EditorUtility.SetDirty(levelConfig);
       }

       // --- REMOVE BUTTON ---
       Rect removeRect = new Rect(
           startX + colorWidth + spacing,
           y,
           removeWidth,
           EditorGUIUtility.singleLineHeight
       );

       if (GUI.Button(removeRect, "X"))
       {
           EditorApplication.delayCall += () =>
           {
               data.Colors.RemoveAt(index);
               EditorUtility.SetDirty(levelConfig);
           };
       }
   };


        }

        // Chỉ vẽ danh sách nếu đang mở rộng
        if (_isColorsListExpanded)
        {
            // Vẽ danh sách có thể kéo thả (centered)
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical(GUILayout.Width(350));
            _colorReorderableList.DoLayoutList();
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            // Chỉ vẽ header khi danh sách đóng
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical(GUILayout.Width(350));

            Rect headerRect = GUILayoutUtility.GetRect(10, 20, GUILayout.ExpandWidth(true));
            _colorReorderableList.headerHeight = headerRect.height;
            _colorReorderableList.drawHeaderCallback(headerRect);

            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawGateWavesEditor(GridCellData data, LevelConfig levelConfig)
    {
        if (data == null || levelConfig == null) return;

        if (data.GateWaves == null)
        {
            data.GateWaves = new List<GateWaveData>();
            EditorUtility.SetDirty(levelConfig);
        }

        EditorGUILayout.Space(6);

        int totalGateColors = 0;
        var uniqueGateColors = new HashSet<ObjectColor>();
        var gateColorCounts = new Dictionary<ObjectColor, int>();
        var allEnumColors = (ObjectColor[])Enum.GetValues(typeof(ObjectColor));
        for (int i = 0; i < allEnumColors.Length; i++)
        {
            gateColorCounts[allEnumColors[i]] = 0;
        }
        for (int w = 0; w < data.GateWaves.Count; w++)
        {
            var wave = data.GateWaves[w];
            if (wave == null || wave.Colors == null) continue;
            totalGateColors += wave.Colors.Count;
            for (int i = 0; i < wave.Colors.Count; i++)
            {
                var c = wave.Colors[i];
                uniqueGateColors.Add(c);
                if (gateColorCounts.ContainsKey(c)) gateColorCounts[c] += 1;
                else gateColorCounts[c] = 1;
            }
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Gate Colors: {totalGateColors} (Unique: {uniqueGateColors.Count})", EditorStyles.miniBoldLabel);
        EditorGUILayout.EndHorizontal();

        if (totalGateColors > 0)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            for (int i = 0; i < allEnumColors.Length; i++)
            {
                var c = allEnumColors[i];
                if (!gateColorCounts.TryGetValue(c, out int count) || count <= 0) continue;
                var style = new GUIStyle(EditorStyles.miniLabel);
                style.normal.textColor = Common.GetColorForEnumEditor(c);
                EditorGUILayout.LabelField($"{c}: {count}", style);
            }
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Gate Waves ({data.GateWaves.Count})", EditorStyles.boldLabel);
        if (GUILayout.Button("+", GUILayout.Width(28)))
        {
            data.GateWaves.Add(new GateWaveData());
            EditorUtility.SetDirty(levelConfig);
        }
        EditorGUILayout.EndHorizontal();

        for (int w = 0; w < data.GateWaves.Count; w++)
        {
            var wave = data.GateWaves[w];
            if (wave == null)
            {
                wave = new GateWaveData();
                data.GateWaves[w] = wave;
                EditorUtility.SetDirty(levelConfig);
            }
            if (wave.Colors == null)
            {
                wave.Colors = new List<ObjectColor>();
                EditorUtility.SetDirty(levelConfig);
            }

            var allColors = (ObjectColor[])Enum.GetValues(typeof(ObjectColor));

            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Wave {w + 1} ({wave.Colors.Count})", GUILayout.Width(95));

            GUI.enabled = w > 0;
            if (GUILayout.Button("Up", GUILayout.Width(34)))
            {
                (data.GateWaves[w - 1], data.GateWaves[w]) = (data.GateWaves[w], data.GateWaves[w - 1]);
                EditorUtility.SetDirty(levelConfig);
            }

            GUI.enabled = w < data.GateWaves.Count - 1;
            if (GUILayout.Button("Dn", GUILayout.Width(34)))
            {
                (data.GateWaves[w + 1], data.GateWaves[w]) = (data.GateWaves[w], data.GateWaves[w + 1]);
                EditorUtility.SetDirty(levelConfig);
            }

            GUI.enabled = true;
            if (GUILayout.Button("-", GUILayout.Width(28)))
            {
                data.GateWaves.RemoveAt(w);
                EditorUtility.SetDirty(levelConfig);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(EditorGUIUtility.IconContent("Refresh"), GUILayout.Width(30)))
            {
                if (wave.Colors.Count > 0)
                {
                    for (int i = 0; i < wave.Colors.Count; i++)
                    {
                        var containers = levelConfig.GetContainers();
                        if (containers != null && containers.Count > 0)
                        {
                            wave.Colors[i] = containers[UnityEngine.Random.Range(0, containers.Count)];
                        }
                        else
                        {
                            wave.Colors[i] = allColors[UnityEngine.Random.Range(0, allColors.Length)];
                        }
                    }
                    EditorUtility.SetDirty(levelConfig);
                }
            }

            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                wave.Colors.Clear();
                EditorUtility.SetDirty(levelConfig);
            }

            GUI.enabled = wave.Colors.Count > 0;
            if (GUILayout.Button("-", GUILayout.Width(28)))
            {
                wave.Colors.RemoveAt(wave.Colors.Count - 1);
                EditorUtility.SetDirty(levelConfig);
            }
            GUI.enabled = true;

            if (GUILayout.Button("+", GUILayout.Width(28)))
            {
                var fallback = ObjectColor.Green;
                var containers = levelConfig.GetContainers();
                if (containers != null && containers.Count > 0)
                {
                    fallback = containers[0];
                }
                var newColor = wave.Colors.Count > 0 ? wave.Colors[^1] : fallback;
                wave.Colors.Add(newColor);
                EditorUtility.SetDirty(levelConfig);
            }
            EditorGUILayout.EndHorizontal();

            if (allColors.Length > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Palette", GUILayout.Width(70));
                for (int p = 0; p < allColors.Length; p++)
                {
                    var c = allColors[p];

                    Rect r = GUILayoutUtility.GetRect(18, 18, GUILayout.Width(18), GUILayout.Height(18));
                    EditorGUI.DrawRect(r, Common.GetColorForEnumEditor(c));
                    EditorGUIUtility.AddCursorRect(r, MouseCursor.Link);
                    if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                    {
                        wave.Colors.Add(c);
                        EditorUtility.SetDirty(levelConfig);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            for (int i = 0; i < wave.Colors.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUI.BeginChangeCheck();
                wave.Colors[i] = Common.DrawObjectColor(wave.Colors[i], GUIContent.none, GUILayout.Width(200));
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(levelConfig);
                }

                if (GUILayout.Button("X", GUILayout.Width(28)))
                {
                    wave.Colors.RemoveAt(i);
                    EditorUtility.SetDirty(levelConfig);
                    EditorGUILayout.EndHorizontal();
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }
    }
    #region Grid Color Popup
    private class ColorSettingPopup : PopupWindowContent
    {
        private readonly LevelConfigEditor _editor;

        public ColorSettingPopup(LevelConfigEditor editor)
        {
            _editor = editor;
        }

        public override Vector2 GetWindowSize() => new Vector2(300, 185);

        public override void OnGUI(Rect rect)
        {
            GUILayout.Space(5);
            EditorGUILayout.BeginVertical();
            _editor.DrawColorSampleField(ColorType.Outline.ToString(), ref _editor._outlineHexColor, ColorType.Outline);
            _editor.DrawColorSampleField(ColorType.Cell.ToString(), ref _editor._defaultHexColor, ColorType.Cell);
            _editor.DrawColorSampleField(ColorType.Label.ToString(), ref _editor._labelHexColor, ColorType.Label);
            _editor.DrawColorSampleField(ColorType.Selected.ToString(), ref _editor._selectedHexColor, ColorType.Selected);
            _editor.DrawColorSampleField(ColorType.Occupied.ToString(), ref _editor._hasStackHexColor, ColorType.Occupied);
            _editor.DrawColorSampleField(ColorType.Empty.ToString(), ref _editor._emptyHexColor, ColorType.Empty);


            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            Rect btnRect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.button, GUILayout.Width(100));
            EditorGUIUtility.AddCursorRect(btnRect, MouseCursor.Link);

            if (GUI.Button(btnRect, "Reset Colors"))
            {
                _editor.SetColor(ColorType.Cell, _editor.GetDefaultHex(ColorType.Cell));
                _editor.SetColor(ColorType.Outline, _editor.GetDefaultHex(ColorType.Outline));
                _editor.SetColor(ColorType.Label, _editor.GetDefaultHex(ColorType.Label));
                _editor.SetColor(ColorType.Selected, _editor.GetDefaultHex(ColorType.Selected));
                _editor.SetColor(ColorType.Occupied, _editor.GetDefaultHex(ColorType.Occupied));
                _editor.SetColor(ColorType.Empty, _editor.GetDefaultHex(ColorType.Empty));
                _editor.Repaint();
            }

            Rect closeBtnRect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.button, GUILayout.Width(100));
            EditorGUIUtility.AddCursorRect(closeBtnRect, MouseCursor.Link);

            if (GUI.Button(closeBtnRect, "Close"))
            {
                editorWindow?.Close();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
    }
    #endregion

}
#endif