#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Sirenix.Utilities.Editor;
using System.Linq;
using System.Collections.Generic;
using System;

public enum SelectMode
{
    Color,
    Conveyor,
}
public enum ColorType
{
    Cell,
    Hover,
    Outline,
    Label,
    Empty
}
public enum gridViewState
{
    Image,
    Position
}
[CustomEditor(typeof(LevelConfig))]
public class LevelConfigEditor : OdinEditor
{
    private float _cellSize = 25f;
    private string _outlineHexColor;
    private string _defaultHexColor;
    private string _labelHexColor;
    private string _hoverHexColor;
    private string _emptyHexColor;

    private Color CellColor => GetColor(ColorType.Cell);
    private Color LabelColor => GetColor(ColorType.Label);
    private Color HoverColor => GetColor(ColorType.Hover);
    private Color OutlineColor => GetColor(ColorType.Outline);
    private Color EmptyColor => GetColor(ColorType.Empty);

    private SelectMode _currentSelectMode = SelectMode.Color;

    private LevelConfig _levelconfig;
    private ObjectColor _currentColorEdit;

    private gridViewState _gridViewState;
    private bool _isColorDragging;
    private ColorLine _currentDrawingLine;
    private ColorLine _selectedLine;

    private bool _showElementTypes = true;
    private bool _showElementTypeZero = false;
    private bool _showLineCounters = true;
    private bool _showLineOverlay = true;
    private bool _showElementTypeColors = true;
    private bool _showConveyorMeta = true;
    private int _activeConveyorLineIndex = 0;
    private int _elementTypeBrush = 0;
    private int _conveyorTypeBrush = 0;
    private int _conveyorCounterBrush = 0;
    private bool _conveyorIsHoleBrush = false;

    // Cached GUIStyles
    private static GUIStyle _cellLabelStyle;
    private static GUIStyle CellLabelStyle
    {
        get
        {
            if (_cellLabelStyle == null)
            {
                _cellLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 7,
                    fontStyle = FontStyle.Bold
                };
            }
            return _cellLabelStyle;
        }
    }

    private static GUIStyle _elementTypeLabelStyle;
    private static GUIStyle ElementTypeLabelStyle
    {
        get
        {
            if (_elementTypeLabelStyle == null)
            {
                _elementTypeLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.UpperLeft,
                    fontSize = 8,
                    fontStyle = FontStyle.Bold
                };
            }
            return _elementTypeLabelStyle;
        }
    }

    // Cache for occupied cells to avoid repeated LINQ queries
    private HashSet<Vector2Int> _occupiedCellsCache = new HashSet<Vector2Int>();
    private readonly Dictionary<Vector2Int, int> _elementTypeByCellCache = new Dictionary<Vector2Int, int>();
    private readonly Dictionary<Vector2Int, int> _lineCounterByHeadCellCache = new Dictionary<Vector2Int, int>();
    private int _lastRowsCount;
    private int _lastColumnsCount;

    private void RebuildOccupiedCellsCache()
    {
        _occupiedCellsCache.Clear();
        _elementTypeByCellCache.Clear();
        _lineCounterByHeadCellCache.Clear();

        if (_levelconfig == null) return;

        void AddLineToCaches(ColorLine line)
        {
            if (line == null) return;
            bool changed = EnsureLineElementTypes(line);
            if (changed) EditorUtility.SetDirty(_levelconfig);

            foreach (var cell in line.Cells)
            {
                _occupiedCellsCache.Add(cell);
            }

            int count = line.Cells.Count;
            for (int i = 0; i < count; i++)
            {
                Vector2Int cell = line.Cells[i];
                int elementType = (line.ElementTypes != null && i < line.ElementTypes.Count) ? line.ElementTypes[i] : 0;
                _elementTypeByCellCache[cell] = elementType;
            }

            if (count > 0)
            {
                Vector2Int headCell = line.Cells[count - 1];
                _lineCounterByHeadCellCache[headCell] = line.Counter;
            }
        }

        if (_levelconfig.ColorLines != null)
        {
            foreach (var line in _levelconfig.ColorLines)
                AddLineToCaches(line);
        }

        if (_levelconfig.Elevators != null)
        {
            for (int e = 0; e < _levelconfig.Elevators.Count; e++)
            {
                ElevatorData elevator = _levelconfig.Elevators[e];
                if (elevator == null || elevator.Lines == null) continue;
                for (int i = 0; i < elevator.Lines.Count; i++)
                    AddLineToCaches(elevator.Lines[i]);
            }
        }
    }

    private static bool EnsureLineElementTypes(ColorLine line)
    {
        if (line == null) return false;

        bool changed = false;
        if (line.Cells == null)
        {
            line.Cells = new List<Vector2Int>();
            changed = true;
        }
        if (line.ElementTypes == null)
        {
            line.ElementTypes = new List<int>();
            changed = true;
        }

        int targetCount = line.Cells.Count;
        while (line.ElementTypes.Count < targetCount)
        {
            line.ElementTypes.Add(0);
            changed = true;
        }
        if (line.ElementTypes.Count > targetCount)
        {
            line.ElementTypes.RemoveRange(targetCount, line.ElementTypes.Count - targetCount);
            changed = true;
        }

        return changed;
    }

    private bool IsCellOccupied(Vector2Int cellPos)
    {
        return _occupiedCellsCache.Contains(cellPos);
    }

    private bool TryGetElementType(Vector2Int cellPos, out int elementType)
    {
        return _elementTypeByCellCache.TryGetValue(cellPos, out elementType);
    }

    private bool TryGetLineCounterAtHead(Vector2Int cellPos, out int counter)
    {
        return _lineCounterByHeadCellCache.TryGetValue(cellPos, out counter);
    }

    private Vector2 GetCellCenter(Rect gridRect, int x, int y, int rows)
    {
        return new Vector2(
            gridRect.x + x * _cellSize + _cellSize / 2,
            gridRect.y + (rows - 1 - y) * _cellSize + _cellSize / 2
        );
    }

    private Rect GetCellRect(Vector2 cellCenter)
    {
        return new Rect(
            cellCenter.x - _cellSize / 2,
            cellCenter.y - _cellSize / 2,
            _cellSize,
            _cellSize
        );
    }

    private void DrawCellOutline(Rect cellRect, Color color)
    {
        Handles.color = color;
        Vector3[] corners = new Vector3[5]
        {
            new Vector3(cellRect.xMin, cellRect.yMin),
            new Vector3(cellRect.xMax, cellRect.yMin),
            new Vector3(cellRect.xMax, cellRect.yMax),
            new Vector3(cellRect.xMin, cellRect.yMax),
            new Vector3(cellRect.xMin, cellRect.yMin)
        };
        Handles.DrawAAPolyLine(corners);
        Handles.color = Color.white;
    }

    private new void OnEnable()
    {
        base.OnEnable();
        LoadColors();
        _levelconfig = (LevelConfig)target;
    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        // Rebuild cache mỗi đầu frame
        RebuildOccupiedCellsCache();

        // Chỉ cleanup khi grid size thay đổi
        if (_levelconfig != null && (_lastRowsCount != _levelconfig.Rows || _lastColumnsCount != _levelconfig.Columns))
        {
            CleanupLinesOutOfBounds();
            _lastRowsCount = _levelconfig.Rows;
            _lastColumnsCount = _levelconfig.Columns;
        }

        GUILayout.Space(10);

        DrawSelectModeButtons();

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        _showElementTypes = EditorGUILayout.ToggleLeft("Show Element Types", _showElementTypes, GUILayout.Width(160));
        _showElementTypeZero = EditorGUILayout.ToggleLeft("Show Zero", _showElementTypeZero, GUILayout.Width(100));
        _showElementTypeColors = EditorGUILayout.ToggleLeft("Colorize", _showElementTypeColors, GUILayout.Width(90));
        _showConveyorMeta = EditorGUILayout.ToggleLeft("Conveyor Meta", _showConveyorMeta, GUILayout.Width(120));
        _showLineCounters = EditorGUILayout.ToggleLeft("Show Counters", _showLineCounters, GUILayout.Width(120));
        _showLineOverlay = EditorGUILayout.ToggleLeft("Show Lines", _showLineOverlay, GUILayout.Width(100));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        if (_currentSelectMode == SelectMode.Color)
        {
            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            _currentColorEdit = Common.DrawObjectColor(_currentColorEdit, new GUIContent(), GUILayout.Width(120));

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (_selectedLine != null)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUI.BeginChangeCheck();
                int newCounter = EditorGUILayout.IntField("Counter", _selectedLine.Counter, GUILayout.Width(240));
                if (EditorGUI.EndChangeCheck())
                {
                    _selectedLine.Counter = Mathf.Max(0, newCounter);
                    EditorUtility.SetDirty(_levelconfig);
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                _elementTypeBrush = EditorGUILayout.IntField("Element Type", _elementTypeBrush, GUILayout.Width(240));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Apply Element Type To Line", GUILayout.Width(240)))
                {
                    EnsureLineElementTypes(_selectedLine);
                    for (int i = 0; i < _selectedLine.ElementTypes.Count; i++)
                        _selectedLine.ElementTypes[i] = _elementTypeBrush;
                    EditorUtility.SetDirty(_levelconfig);
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }
        else if (_currentSelectMode == SelectMode.Conveyor)
        {
            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            int maxConveyorIndex = Mathf.Max(0, GetEditableConveyorLines().Count);
            _activeConveyorLineIndex = EditorGUILayout.IntSlider("Active Conveyor", Mathf.Clamp(_activeConveyorLineIndex, 0, maxConveyorIndex), 0, maxConveyorIndex, GUILayout.Width(320));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            _conveyorTypeBrush = EditorGUILayout.IntField("Conveyor Type", _conveyorTypeBrush, GUILayout.Width(240));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            _conveyorCounterBrush = EditorGUILayout.IntField("Conveyor Counter", _conveyorCounterBrush, GUILayout.Width(240));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            _conveyorIsHoleBrush = EditorGUILayout.ToggleLeft("Conveyor Is Hole", _conveyorIsHoleBrush, GUILayout.Width(240));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.Space(10);

        DrawGrid();
    }

    private static Color GetElementTypeColor(int elementType)
    {
        return elementType switch
        {
            0 => new Color(0f, 0f, 0f, 0f),
            1 => new Color(1f, 0.92f, 0.02f, 0.35f),
            2 => new Color(0.3f, 0.9f, 1f, 0.35f),
            3 => new Color(1f, 0.2f, 0.2f, 0.35f),
            _ => new Color(0.85f, 0.2f, 1f, 0.25f)
        };
    }

    private static Rect InsetRect(Rect rect, float inset)
    {
        return new Rect(rect.x + inset, rect.y + inset, rect.width - inset * 2f, rect.height - inset * 2f);
    }

    private void DrawConveyorMetaLabel(Rect cellRect, int type, int counter, bool isHole)
    {
        string text = isHole ? "Hole" : $"T{type}";
        if (!isHole && counter != 0)
            text += $":{counter}";

        GUIContent content = new GUIContent(text);
        Vector2 size = ElementTypeLabelStyle.CalcSize(content);
        Rect rect = new Rect(cellRect.xMin + 2f, cellRect.yMax - size.y - 1f, size.x + 4f, size.y);
        EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.35f));
        GUI.Label(new Rect(rect.x + 2f, rect.y, rect.width, rect.height), content, ElementTypeLabelStyle);
    }

    private void LoadColors()
    {
        _outlineHexColor = EditorPrefs.GetString(ColorType.Outline.ToString(), GetDefaultHex(ColorType.Outline));
        _defaultHexColor = EditorPrefs.GetString(ColorType.Cell.ToString(), GetDefaultHex(ColorType.Cell));
        _labelHexColor = EditorPrefs.GetString(ColorType.Label.ToString(), GetDefaultHex(ColorType.Label));
        _hoverHexColor = EditorPrefs.GetString(ColorType.Hover.ToString(), GetDefaultHex(ColorType.Hover));
        _emptyHexColor = EditorPrefs.GetString(ColorType.Empty.ToString(), GetDefaultHex(ColorType.Empty));
    }

    private Color GetColor(ColorType type)
    {
        string hex = type switch
        {
            ColorType.Outline => _outlineHexColor,
            ColorType.Cell => _defaultHexColor,
            ColorType.Label => _labelHexColor,
            ColorType.Hover => _hoverHexColor,
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
            case ColorType.Hover:
                _hoverHexColor = hex;
                break;
            case ColorType.Empty:
                _emptyHexColor = hex;
                break;
        }
        EditorPrefs.SetString(type.ToString(), hex);
    }

    private string GetDefaultHex(ColorType type)
    {
        switch (type)
        {
            case ColorType.Outline: return "#000000ff";
            case ColorType.Cell: return "#ffffff00";
            case ColorType.Label: return "#ffffffff";
            case ColorType.Hover: return "#00ff2aff";
            case ColorType.Empty: return "#808080ff";
            default: return "#ffffffff";
        }
    }


    private void DrawToggleViewButton(Rect rect)
    {
        if (_gridViewState == gridViewState.Position)
        {
            if (SirenixEditorGUI.IconButton(rect, EditorIcons.Image, "View line"))
            {
                _gridViewState = gridViewState.Image;
            }
        }
        else
        {
            if (SirenixEditorGUI.IconButton(rect, EditorIcons.Marker, "View position"))
            {
                _gridViewState = gridViewState.Position;
            }
        }
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
    }
    private void DrawResetButton(Rect rect)
    {
        if (SirenixEditorGUI.IconButton(rect, EditorIcons.Refresh, "Reset board"))
        {
            if (EditorUtility.DisplayDialog("Confirm Reset", "Are you sure you want to reset board?", "Yes", "No"))
            {
                _levelconfig.ColorLines.Clear();
                _levelconfig.SetConveyorLines(new List<ConveyorLine>());
                _levelconfig.Cells = new GridCellData[_levelconfig.Columns, _levelconfig.Rows];
                for (int x = 0; x < _levelconfig.Columns; x++)
                {
                    for (int y = 0; y < _levelconfig.Rows; y++)
                    {
                        _levelconfig.Cells[x, y] = new GridCellData { CellType = GridCellType.Normal };
                    }
                }
                EditorUtility.SetDirty(_levelconfig);
            }
        }

        EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
    }
    private void DrawRandomFillButton(Rect rect)
    {
        if (SirenixEditorGUI.IconButton(rect, EditorIcons.Link, "Fill random lines"))
        {
            PopupWindow.Show(rect, new RandomFillPopup(this));
        }

        EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
    }
    private void DrawSettingButton(Rect rect)
    {
        if (SirenixEditorGUI.IconButton(rect, EditorIcons.SettingsCog, "Open settings"))
        {
            PopupWindow.Show(rect, new ColorSettingPopup(this));
        }

        EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
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

        // Tạo GUIContent cho từng mode với tooltip
        GUIContent[] toolbarOptions = new GUIContent[]
        {
        new GUIContent("Colors", "Draw line color"),
        new GUIContent("Conveyor", "Set cell to conveyor"),
        };

        int previousMode = (int)_currentSelectMode;
        int newMode = GUILayout.Toolbar(previousMode, toolbarOptions, GUILayout.Width(320), GUILayout.Height(30));
        Rect toolbarRect = GUILayoutUtility.GetLastRect();
        EditorGUIUtility.AddCursorRect(toolbarRect, MouseCursor.Link);
        if (newMode != previousMode)
        {
            _currentSelectMode = (SelectMode)newMode;
            GUI.changed = true;
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }
    // ...existing code...
    public void FillLines(ObjectColor[] colors, int minLength, int maxLength)
    {
        int columns = _levelconfig.Columns;
        int rows = _levelconfig.Rows;

        System.Random rnd = new System.Random();
        var allColors = colors != null && colors.Length > 0 ? colors.ToList() : Enum.GetValues(typeof(ObjectColor)).Cast<ObjectColor>().ToList();

        bool[,] filled = new bool[columns, rows];
        // Tạo danh sách các ô chưa thuộc line nào (và CellType == Normal)
        List<Vector2Int> unfilledCells = new List<Vector2Int>();
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                var cell = _levelconfig.Cells[x, y];
                var cellPos = new Vector2Int(x, y);
                if (cell != null && cell.CellType == GridCellType.Normal && !IsCellOccupied(cellPos))
                {
                    unfilledCells.Add(cellPos);
                }
                else
                {
                    filled[x, y] = true;
                }
            }
        }

        // Hướng đi 4 phía
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),   // phải
            new Vector2Int(-1, 0),  // trái
            new Vector2Int(0, 1),   // lên
            new Vector2Int(0, -1),  // xuống
        };

        Dictionary<Vector2Int, int> retryCount = new Dictionary<Vector2Int, int>();

        while (unfilledCells.Count > 0)
        {
            int startIdx = rnd.Next(unfilledCells.Count);
            Vector2Int start = unfilledCells[startIdx];

            if (!retryCount.ContainsKey(start))
                retryCount[start] = 0;

            ObjectColor color = allColors[rnd.Next(allColors.Count)];

            int maxLen = Math.Min(maxLength, unfilledCells.Count);
            int minLen = Math.Min(minLength, maxLen);
            int len = rnd.Next(minLen, maxLen + 1);

            List<Vector2Int> lineCells = new List<Vector2Int>();
            Vector2Int current = start;
            lineCells.Add(current);
            filled[current.x, current.y] = true;
            unfilledCells.Remove(current);

            for (int i = 1; i < len; i++)
            {
                List<Vector2Int> validDirs = new List<Vector2Int>();
                foreach (var dir in directions)
                {
                    Vector2Int next = current + dir;
                    if (next.x >= 0 && next.x < columns && next.y >= 0 && next.y < rows && !filled[next.x, next.y] && !IsCellOccupied(next))
                    {
                        // Kiểm tra nếu thêm cell này, mũi tên có thể thoát ra không
                        if (CanArrowEscape(lineCells, next, filled, columns, rows))
                        {
                            validDirs.Add(dir);
                        }
                    }
                }
                if (validDirs.Count == 0) break;

                Vector2Int chosenDir = validDirs[rnd.Next(validDirs.Count)];
                Vector2Int nextCell = current + chosenDir;

                lineCells.Add(nextCell);
                filled[nextCell.x, nextCell.y] = true;
                unfilledCells.Remove(nextCell);
                current = nextCell;
            }

            if (lineCells.Count >= minLength)
            {
                ColorLine line = new ColorLine { Color = color };
                line.Cells.AddRange(lineCells);
                for (int i = 0; i < lineCells.Count; i++)
                {
                    line.ElementTypes.Add(0);
                }
                line.Counter = 0;
                _levelconfig.ColorLines.Add(line);
            }
            else
            {
                retryCount[start]++;
                if (retryCount[start] > 5)
                {
                    // Đánh dấu các cell trong lineCells là filled nhưng không tạo line
                }
                else
                {
                    foreach (var cell in lineCells)
                    {
                        filled[cell.x, cell.y] = false;
                        if (!unfilledCells.Contains(cell))
                            unfilledCells.Add(cell);
                    }
                }
            }
        }
        EditorUtility.SetDirty(_levelconfig);
    }

    /// <summary>
    /// Kiểm tra xem nếu thêm newCell vào line, mũi tên có thể thoát ra khỏi board không.
    /// Mũi tên sẽ đi theo hướng từ cell trước đó đến newCell, và tiếp tục thẳng cho đến khi ra khỏi board.
    /// Nếu trên đường đi có bất kỳ cell nào của chính line đó, thì không thể thoát.
    /// </summary>
    private bool CanArrowEscape(List<Vector2Int> currentLine, Vector2Int newCell, bool[,] filled, int columns, int rows)
    {
        if (currentLine.Count == 0) return true;

        // Tính hướng mũi tên: từ cell cuối hiện tại đến newCell
        Vector2Int lastCell = currentLine[currentLine.Count - 1];
        Vector2Int arrowDir = newCell - lastCell;

        // Tạo HashSet chứa tất cả cells của line (bao gồm cả newCell)
        HashSet<Vector2Int> lineCellsSet = new HashSet<Vector2Int>(currentLine);
        lineCellsSet.Add(newCell);

        // Bắt đầu từ newCell, đi theo hướng mũi tên cho đến khi ra khỏi board
        Vector2Int checkPos = newCell + arrowDir;
        while (checkPos.x >= 0 && checkPos.x < columns && checkPos.y >= 0 && checkPos.y < rows)
        {
            // Nếu gặp cell của chính line này → không thể thoát
            if (lineCellsSet.Contains(checkPos))
            {
                return false;
            }
            checkPos += arrowDir;
        }

        // Đã ra khỏi board mà không gặp cell nào của line → có thể thoát
        return true;
    }
    // ...existing code...
    private void DrawGrid()
    {
        if (_levelconfig == null || _levelconfig.Cells == null) return;

        int columns = _levelconfig.Columns;
        int rows = _levelconfig.Rows;

        float totalWidth = columns * _cellSize;
        float totalHeight = rows * _cellSize;

        float btnSize = 20f;
        float btnMargin = 5f;
        // Đảm bảo luôn có không gian tối thiểu cho buttons
        float minWidth = Mathf.Max(totalWidth, btnSize) + btnMargin + btnSize;
        float minHeight = Mathf.Max(totalHeight, btnSize * 3 + btnMargin * 2);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        Rect fullRect = GUILayoutUtility.GetRect(minWidth, minHeight);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // gridRect chỉ chứa phần grid cells
        Rect gridRect = new Rect(fullRect.x, fullRect.y, totalWidth, totalHeight);

        Event e = Event.current;
        Vector2 mousePos = e.mousePosition;

        float btnX = fullRect.x + totalWidth + btnMargin;
        float btnY = fullRect.y;

        Rect toggleButtonRect = new Rect(btnX, btnY, btnSize, btnSize);
        DrawToggleViewButton(toggleButtonRect);

        float SettingBtnY = btnY + btnSize + 5;
        Rect settingButtonRect = new Rect(btnX, SettingBtnY, btnSize, btnSize);
        DrawSettingButton(settingButtonRect);

        float randomBtnY = SettingBtnY + btnSize + 5;
        Rect randomButtonRect = new Rect(btnX, randomBtnY, btnSize, btnSize);
        DrawRandomFillButton(randomButtonRect);

        float resetBtnY = randomBtnY + btnSize + 5;
        Rect resetButtonRect = new Rect(btnX, resetBtnY, btnSize, btnSize);
        DrawResetButton(resetButtonRect);

        Vector2Int? hoveredCell = null;
        for (int x = 0; x < columns; x++)
        {
            for (int y = rows - 1; y >= 0; y--)
            {
                Vector2 cellCenter = GetCellCenter(gridRect, x, y, rows);
                Rect cellRect = GetCellRect(cellCenter);

                var cell = _levelconfig.Cells[x, y];

                Color cellDrawColor = cell.CellType == GridCellType.Conveyor ? EmptyColor : CellColor;

                EditorGUI.DrawRect(cellRect, cellDrawColor);

                // Vẽ outline cho toàn bộ 4 cạnh
                DrawCellOutline(cellRect, OutlineColor);

                // Hiển thị label
                if (_gridViewState == gridViewState.Position)
                {
                    DrawCellLabel(cellCenter, $"{x},{y}");
                }

                if (_showElementTypes && TryGetElementType(new Vector2Int(x, y), out int elementType))
                {
                    if (_showElementTypeZero || elementType != 0)
                    {
                        if (_showElementTypeColors)
                        {
                            Color tint = GetElementTypeColor(elementType);
                            if (tint.a > 0.0001f)
                                EditorGUI.DrawRect(InsetRect(cellRect, 2f), tint);
                        }
                        DrawElementTypeLabel(cellRect, elementType);
                    }
                }

                if (_showLineCounters && TryGetLineCounterAtHead(new Vector2Int(x, y), out int counter) && counter > 0)
                {
                    DrawCounterLabel(cellRect, counter);
                }

                if (cellRect.Contains(mousePos))
                {
                    hoveredCell = new Vector2Int(x, y);
                }

                if (_currentSelectMode == SelectMode.Color)
                {
                    if (e.type == EventType.MouseDown && cellRect.Contains(mousePos))
                    {
                        _isColorDragging = true;
                        if (e.button == 0)
                        {
                            HandleColorLineInput(x, y, _currentColorEdit, true);
                        }
                        else if (e.button == 1)
                        {
                            RemoveCellAndTailFromLine(new Vector2Int(x, y));
                        }
                        e.Use();
                    }
                    else if (e.type == EventType.MouseDrag && _isColorDragging && cellRect.Contains(mousePos))
                    {
                        if (e.button == 0)
                        {
                            HandleColorLineInput(x, y, _currentColorEdit, false);
                        }
                        else if (e.button == 1)
                        {
                            RemoveCellAndTailFromLine(new Vector2Int(x, y));
                        }
                    }
                    else if (e.type == EventType.MouseUp)
                    {
                        _isColorDragging = false;
                        if (_currentDrawingLine != null && _currentDrawingLine.Cells.Count <= 1)
                        {
                            _levelconfig.ColorLines.Remove(_currentDrawingLine);
                            EditorUtility.SetDirty(_levelconfig);
                        }
                        _currentDrawingLine = null; // Kết thúc nhóm
                    }
                }
                else if (_currentSelectMode == SelectMode.Conveyor)
                {
                    if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && cellRect.Contains(mousePos))
                    {
                        if (e.button == 0)
                        {
                            Vector2Int targetPos = new Vector2Int(x, y);
                            cell.CellType = GridCellType.Conveyor;

                            RemoveConveyorCellFromAllLines(targetPos);
                            ConveyorLine activeConveyor = GetOrCreateActiveConveyorLine();
                            if (!activeConveyor.Cells.Contains(targetPos))
                            {
                                activeConveyor.Cells.Add(targetPos);
                                activeConveyor.Types.Add(_conveyorTypeBrush);
                                activeConveyor.Counters.Add(_conveyorCounterBrush);
                                activeConveyor.IsHoles.Add(_conveyorIsHoleBrush);
                            }
                            RemoveCellAndTailFromLine(targetPos);
                        }
                        else if (e.button == 1)
                        {
                            Vector2Int targetPos = new Vector2Int(x, y);
                            RemoveConveyorCellFromAllLines(targetPos);
                            cell.CellType = GridCellType.Normal;
                        }
                        EditorUtility.SetDirty(_levelconfig);
                        e.Use();
                    }
                }
            }
        }

        if (_showConveyorMeta)
        {
            List<ConveyorLine> conveyorLines = GetEditableConveyorLines();
            for (int conveyorIndex = 0; conveyorIndex < conveyorLines.Count; conveyorIndex++)
            {
                ConveyorLine conveyor = conveyorLines[conveyorIndex];
                if (conveyor == null || conveyor.Cells == null) continue;

                for (int i = 0; i < conveyor.Cells.Count; i++)
                {
                    Vector2Int pos = conveyor.Cells[i];
                    if (pos.x < 0 || pos.x >= columns || pos.y < 0 || pos.y >= rows) continue;
                    Rect metaRect = GetCellRect(GetCellCenter(gridRect, pos.x, pos.y, rows));

                    int type = (conveyor.Types != null && i < conveyor.Types.Count) ? conveyor.Types[i] : 0;
                    int counter = (conveyor.Counters != null && i < conveyor.Counters.Count) ? conveyor.Counters[i] : 0;
                    bool isHole = (conveyor.IsHoles != null && i < conveyor.IsHoles.Count) && conveyor.IsHoles[i];

                    if (isHole || type != 0 || counter != 0)
                        DrawConveyorMetaLabel(metaRect, type, counter, isHole);
                }
            }
        }


        if (_showLineOverlay && _levelconfig.Elevators != null && _gridViewState == gridViewState.Image)
        {
            for (int eIdx = 0; eIdx < _levelconfig.Elevators.Count; eIdx++)
            {
                ElevatorData elevator = _levelconfig.Elevators[eIdx];
                if (elevator == null) continue;

                Rect elevatorRect = new Rect(
                    gridRect.x + elevator.Position.x * _cellSize,
                    gridRect.y + (rows - elevator.Position.y - elevator.Size.y) * _cellSize,
                    elevator.Size.x * _cellSize,
                    elevator.Size.y * _cellSize
                );

                EditorGUI.DrawRect(InsetRect(elevatorRect, 1f), new Color(1f, 1f, 1f, 0.06f));
                DrawCellOutline(elevatorRect, new Color(1f, 1f, 1f, 0.35f));

                if (elevator.Lines == null) continue;
                for (int l = 0; l < elevator.Lines.Count; l++)
                {
                    var line = elevator.Lines[l];
                    if (line == null || line.Cells == null || line.Cells.Count < 2) continue;
                    Handles.color = Common.GetColorForEnumEditor(line.Color);
                    Vector3[] points = new Vector3[line.Cells.Count];
                    for (int i = 0; i < line.Cells.Count; i++)
                    {
                        var cell = line.Cells[i];
                        points[i] = new Vector3(
                            gridRect.x + cell.x * _cellSize + _cellSize / 2f,
                            gridRect.y + (rows - 1 - cell.y) * _cellSize + _cellSize / 2f,
                            0
                        );
                    }
                    Handles.DrawAAPolyLine(4f, points);
                    Handles.color = Color.white;
                }
            }
        }

        if (_showLineOverlay && _levelconfig.ColorLines != null && _gridViewState == gridViewState.Image)
        {
            foreach (var line in _levelconfig.ColorLines)
            {
                if (line.Cells.Count < 2) continue;
                Handles.color = Common.GetColorForEnumEditor(line.Color);
                Vector3[] points = new Vector3[line.Cells.Count];
                for (int i = 0; i < line.Cells.Count; i++)
                {
                    var cell = line.Cells[i];
                    points[i] = new Vector3(
                        gridRect.x + cell.x * _cellSize + _cellSize / 2f,
                        gridRect.y + (rows - 1 - cell.y) * _cellSize + _cellSize / 2f,
                        0
                    );
                }
                Handles.DrawAAPolyLine(8f, points);

                // Vẽ mũi tên tam giác ở cuối line
                Vector3 tail = points[points.Length - 2];
                Vector3 head = points[points.Length - 1];
                Vector3 dir = (head - tail).normalized;
                float arrowLength = _cellSize * 0.5f;
                float arrowWidth = _cellSize * 0.32f;

                // Tính các điểm tam giác
                Vector3 tip = head + dir * (arrowLength * 0.5f);
                Vector3 baseCenter = head - dir * (arrowLength * 0.3f);
                Vector3 perp = new Vector3(-dir.y, dir.x, 0) * (arrowWidth * 0.5f);

                Vector3 p1 = tip;
                Vector3 p2 = baseCenter + perp;
                Vector3 p3 = baseCenter - perp;

                Handles.DrawAAConvexPolygon(p1, p2, p3);
                Handles.color = Color.white;
            }
        }

        // Vẽ outline xanh cho ô đang hover (nếu có)
        if (hoveredCell.HasValue)
        {
            Vector2 hoverCenter = GetCellCenter(gridRect, hoveredCell.Value.x, hoveredCell.Value.y, rows);
            Rect hoverRect = GetCellRect(hoverCenter);
            DrawCellOutline(hoverRect, HoverColor);
        }

        // Đảm bảo repaint khi di chuột hoặc click
        if (e.type == EventType.MouseMove || e.type == EventType.MouseDown || e.type == EventType.MouseDrag || e.type == EventType.MouseUp)
        {
            Repaint();
        }
    }
    private void CleanupLinesOutOfBounds()
    {
        int columns = _levelconfig.Columns;
        int rows = _levelconfig.Rows;
        var toRemove = new List<(ColorLine, Vector2Int)>();

        foreach (var line in _levelconfig.ColorLines.ToList())
        {
            for (int i = 0; i < line.Cells.Count; i++)
            {
                var cell = line.Cells[i];
                if (cell.x < 0 || cell.x >= columns || cell.y < 0 || cell.y >= rows)
                {
                    toRemove.Add((line, cell));
                }
            }
        }
        foreach (var (line, cell) in toRemove)
        {
            RemoveCellAndTailFromLine(cell);
        }

        List<ConveyorLine> conveyorLines = GetEditableConveyorLines();
        for (int conveyorIndex = 0; conveyorIndex < conveyorLines.Count; conveyorIndex++)
        {
            ConveyorLine conveyor = conveyorLines[conveyorIndex];
            if (conveyor == null || conveyor.Cells == null) continue;

            for (int i = conveyor.Cells.Count - 1; i >= 0; i--)
            {
                var cell = conveyor.Cells[i];
                if (cell.x < 0 || cell.x >= columns || cell.y < 0 || cell.y >= rows)
                {
                    conveyor.Cells.RemoveAt(i);
                    if (conveyor.Types != null && i < conveyor.Types.Count) conveyor.Types.RemoveAt(i);
                    if (conveyor.Counters != null && i < conveyor.Counters.Count) conveyor.Counters.RemoveAt(i);
                    if (conveyor.IsHoles != null && i < conveyor.IsHoles.Count) conveyor.IsHoles.RemoveAt(i);

                    if (cell.x >= 0 && cell.x < _levelconfig.Cells.GetLength(0) &&
                        cell.y >= 0 && cell.y < _levelconfig.Cells.GetLength(1))
                    {
                        _levelconfig.Cells[cell.x, cell.y].CellType = GridCellType.Normal;
                    }
                }
            }
        }

        EditorUtility.SetDirty(_levelconfig);
    }
    private List<Vector2Int> BuildOrderedBoundaryCells(List<Vector2Int> cells)
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

    private List<ConveyorLine> GetEditableConveyorLines()
    {
        if (_levelconfig.ConveyorLines == null)
            _levelconfig.ConveyorLines = new List<ConveyorLine>();

        if (_levelconfig.ConveyorLines.Count == 0 && _levelconfig.ConveyorLine != null)
            _levelconfig.ConveyorLines.Add(_levelconfig.ConveyorLine);

        _levelconfig.SetConveyorLines(_levelconfig.ConveyorLines);
        return _levelconfig.ConveyorLines;
    }

    private ConveyorLine GetOrCreateActiveConveyorLine()
    {
        List<ConveyorLine> conveyors = GetEditableConveyorLines();
        _activeConveyorLineIndex = Mathf.Max(0, _activeConveyorLineIndex);

        while (conveyors.Count <= _activeConveyorLineIndex)
        {
            conveyors.Add(new ConveyorLine
            {
                Cells = new List<Vector2Int>(),
                Types = new List<int>(),
                Counters = new List<int>(),
                IsHoles = new List<bool>()
            });
        }

        ConveyorLine conveyor = conveyors[_activeConveyorLineIndex];
        if (conveyor.Cells == null) conveyor.Cells = new List<Vector2Int>();
        if (conveyor.Types == null) conveyor.Types = new List<int>();
        if (conveyor.Counters == null) conveyor.Counters = new List<int>();
        if (conveyor.IsHoles == null) conveyor.IsHoles = new List<bool>();
        _levelconfig.SetConveyorLines(conveyors);
        return conveyor;
    }

    private void RemoveConveyorCellFromAllLines(Vector2Int targetPos)
    {
        List<ConveyorLine> conveyors = GetEditableConveyorLines();
        for (int conveyorIndex = 0; conveyorIndex < conveyors.Count; conveyorIndex++)
        {
            ConveyorLine conveyor = conveyors[conveyorIndex];
            if (conveyor == null || conveyor.Cells == null) continue;

            int idx = conveyor.Cells.IndexOf(targetPos);
            if (idx < 0) continue;

            conveyor.Cells.RemoveAt(idx);
            if (conveyor.Types != null && idx < conveyor.Types.Count) conveyor.Types.RemoveAt(idx);
            if (conveyor.Counters != null && idx < conveyor.Counters.Count) conveyor.Counters.RemoveAt(idx);
            if (conveyor.IsHoles != null && idx < conveyor.IsHoles.Count) conveyor.IsHoles.RemoveAt(idx);
        }

        _levelconfig.SetConveyorLines(conveyors);
    }
    private void RemoveCellAndTailFromLine(Vector2Int cellPos)
    {
        // Tìm line chứa cell này
        var line = _levelconfig.ColorLines.FirstOrDefault(l => l.Cells.Contains(cellPos));
        if (line == null) return;

        int idx = line.Cells.IndexOf(cellPos);
        if (idx < 0) return;

        // Xóa từ cell này tới cuối line bằng RemoveRange
        line.Cells.RemoveRange(idx, line.Cells.Count - idx);
        EnsureLineElementTypes(line);
        if (line.ElementTypes != null && idx < line.ElementTypes.Count)
        {
            line.ElementTypes.RemoveRange(idx, line.ElementTypes.Count - idx);
        }

        if (line.Cells.Count <= 1)
        {
            _levelconfig.ColorLines.Remove(line);
        }
        EditorUtility.SetDirty(_levelconfig);
    }


    private void DrawCellLabel(Vector2 center, string position)
    {
        CellLabelStyle.normal.textColor = LabelColor;
        Vector2 textSize = CellLabelStyle.CalcSize(new GUIContent(position));
        Rect labelRect = new Rect(
            center.x - textSize.x * 0.5f,
            center.y - textSize.y * 0.5f,
            textSize.x,
            textSize.y
        );
        GUI.Label(labelRect, position, CellLabelStyle);
    }

    private void DrawElementTypeLabel(Rect cellRect, int elementType)
    {
        string text = $"T{elementType}";
        GUIContent content = new GUIContent(text);

        ElementTypeLabelStyle.normal.textColor = LabelColor;
        Vector2 size = ElementTypeLabelStyle.CalcSize(content);
        Rect rect = new Rect(cellRect.xMin + 2f, cellRect.yMin + 1f, size.x + 4f, size.y);
        EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.4f));
        GUI.Label(new Rect(rect.x + 2f, rect.y, rect.width, rect.height), content, ElementTypeLabelStyle);
    }

    private void DrawCounterLabel(Rect cellRect, int counter)
    {
        string text = $"C{counter}";
        GUIContent content = new GUIContent(text);

        ElementTypeLabelStyle.normal.textColor = LabelColor;
        Vector2 size = ElementTypeLabelStyle.CalcSize(content);
        Rect rect = new Rect(cellRect.xMax - (size.x + 8f), cellRect.yMin + 1f, size.x + 6f, size.y);
        EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.4f));
        GUI.Label(new Rect(rect.x + 2f, rect.y, rect.width, rect.height), content, ElementTypeLabelStyle);
    }


    private void HandleColorLineInput(int x, int y, ObjectColor color, bool isStartClick)
    {
        var cellPos = new Vector2Int(x, y);
        var cell = _levelconfig.Cells[x, y];
        if (cell == null || cell.CellType == GridCellType.Conveyor) return;

        if (isStartClick)
        {
            if (!IsCellOccupied(cellPos))
            {
                var line = new ColorLine { Color = color };
                line.Cells.Add(cellPos);
                line.ElementTypes.Add(_elementTypeBrush);
                _levelconfig.ColorLines.Add(line);
                _occupiedCellsCache.Add(cellPos);
                _currentDrawingLine = line;
                _selectedLine = line;
            }
            else
            {
                _currentDrawingLine = _levelconfig.ColorLines.FirstOrDefault(l => l.Cells.Contains(cellPos));
                if (_currentDrawingLine != null)
                {
                    _currentColorEdit = _currentDrawingLine.Color;
                    _selectedLine = _currentDrawingLine;
                }
            }
            EditorUtility.SetDirty(_levelconfig);
            return;
        }

        // Drag - thêm cells vào line đang vẽ
        if (_currentDrawingLine == null || IsCellOccupied(cellPos)) return;

        var lastCell = _currentDrawingLine.Cells.Last();
        var finalPath = FindPathBetweenCells(lastCell, cellPos);

        if (finalPath != null)
        {
            EnsureLineElementTypes(_currentDrawingLine);
            int elementType = _elementTypeBrush;

            foreach (var pos in finalPath)
            {
                _currentDrawingLine.Cells.Add(pos);
                _currentDrawingLine.ElementTypes.Add(elementType);
                _occupiedCellsCache.Add(pos);
            }
            EditorUtility.SetDirty(_levelconfig);
        }
    }

    private List<Vector2Int> FindPathBetweenCells(Vector2Int from, Vector2Int to)
    {
        var path1 = TryPath(from, to, true);
        if (path1 != null) return path1;

        return TryPath(from, to, false);
    }

    private List<Vector2Int> TryPath(Vector2Int from, Vector2Int to, bool horizontalFirst)
    {
        var path = new List<Vector2Int>();

        int x0 = from.x, x1 = to.x;
        int y0 = from.y, y1 = to.y;

        if (horizontalFirst)
        {
            if (!AddHorizontalSegment(path, x0, x1, y0)) return null;
            if (!AddVerticalSegment(path, y0, y1, x1)) return null;
        }
        else
        {
            if (!AddVerticalSegment(path, y0, y1, x0)) return null;
            if (!AddHorizontalSegment(path, x0, x1, y1)) return null;
        }

        return path.Count > 0 ? path : null;
    }

    private bool AddHorizontalSegment(List<Vector2Int> path, int x0, int x1, int y)
    {
        if (x0 == x1) return true;
        int step = x1 > x0 ? 1 : -1;
        for (int x = x0 + step; x != x1 + step; x += step)
        {
            var pos = new Vector2Int(x, y);
            var c = _levelconfig.Cells[pos.x, pos.y];
            if (IsCellOccupied(pos) || c == null || c.CellType == GridCellType.Conveyor) return false;
            path.Add(pos);
        }
        return true;
    }

    private bool AddVerticalSegment(List<Vector2Int> path, int y0, int y1, int x)
    {
        if (y0 == y1) return true;
        int step = y1 > y0 ? 1 : -1;
        for (int y = y0 + step; y != y1 + step; y += step)
        {
            var pos = new Vector2Int(x, y);
            var c = _levelconfig.Cells[pos.x, pos.y];
            if (IsCellOccupied(pos) || c == null || c.CellType == GridCellType.Conveyor) return false;
            path.Add(pos);
        }
        return true;
    }
    #region Random Fill Popup
    private class RandomFillPopup : PopupWindowContent
    {
        private bool[] _selected;
        public int minValue = 2;
        public int maxValue = 20;
        private List<ObjectColor> GetSelectedColors()
        {
            List<ObjectColor> selectedColors = new List<ObjectColor>();
            var allColors = (ObjectColor[])Enum.GetValues(typeof(ObjectColor));
            for (int i = 0; i < allColors.Length; i++)
            {
                if (_selected != null && _selected.Length > i && _selected[i])
                {
                    selectedColors.Add(allColors[i]);
                }
            }
            return selectedColors;
        }
        public override Vector2 GetWindowSize() => new Vector2(225, 200);
        private readonly LevelConfigEditor _editor;

        public RandomFillPopup(LevelConfigEditor editor)
        {
            _editor = editor;
        }

        private static Color GetContrastColor(Color color)
        {
            float luminance = 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
            return luminance > 0.5f ? Color.black : Color.white;
        }
        public override void OnGUI(Rect rect)
        {
            GUILayout.Space(5);
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.Label("Min Value", GUILayout.Width(80));
            minValue = EditorGUILayout.IntField(minValue, GUILayout.Width(60));
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.Label("Max Value", GUILayout.Width(80));
            maxValue = EditorGUILayout.IntField(maxValue, GUILayout.Width(60));
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);

            if (minValue < 2) minValue = 2;
            if (maxValue < 2) maxValue = 2;
            if (minValue > maxValue) minValue = maxValue;

            if (_selected == null) _selected = new bool[Enum.GetValues(typeof(ObjectColor)).Length];

            var _colors = (ObjectColor[])Enum.GetValues(typeof(ObjectColor));

            // Grid layout cho color buttons
            int columns = 5;
            float btnSize = 32f;
            float spacing = 4f;
            float totalWidth = columns * btnSize + (columns - 1) * spacing;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Rect gridRect = GUILayoutUtility.GetRect(totalWidth, Mathf.Ceil(_colors.Length / (float)columns) * (btnSize + spacing));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < _colors.Length; i++)
            {
                int row = i / columns;
                int col = i % columns;

                Rect btnRect = new Rect(
                    gridRect.x + col * (btnSize + spacing),
                    gridRect.y + row * (btnSize + spacing),
                    btnSize,
                    btnSize
                );

                Color color = Common.GetColorForEnumEditor(_colors[i]);

                // Vẽ nền màu
                EditorGUI.DrawRect(btnRect, color);

                // Vẽ viền (đậm hơn nếu được chọn)
                if (_selected[i])
                {
                    // Viền trắng dày khi selected
                    Handles.color = Color.white;
                    Vector3[] corners = new Vector3[] {
                        new Vector3(btnRect.xMin, btnRect.yMin),
                        new Vector3(btnRect.xMax, btnRect.yMin),
                        new Vector3(btnRect.xMax, btnRect.yMax),
                        new Vector3(btnRect.xMin, btnRect.yMax),
                        new Vector3(btnRect.xMin, btnRect.yMin)
                    };
                    Handles.DrawAAPolyLine(3f, corners);

                    // Vẽ checkmark
                    Handles.color = GetContrastColor(color);
                    Vector3 center = btnRect.center;
                    float checkSize = btnSize * 0.25f;
                    Handles.DrawAAPolyLine(2f,
                        new Vector3(center.x - checkSize, center.y),
                        new Vector3(center.x - checkSize * 0.3f, center.y + checkSize * 0.7f),
                        new Vector3(center.x + checkSize, center.y - checkSize * 0.5f)
                    );
                }
                else
                {
                    // Viền mờ khi chưa chọn
                    Handles.color = new Color(0, 0, 0, 0.3f);
                    Vector3[] corners = new Vector3[] {
                        new Vector3(btnRect.xMin, btnRect.yMin),
                        new Vector3(btnRect.xMax, btnRect.yMin),
                        new Vector3(btnRect.xMax, btnRect.yMax),
                        new Vector3(btnRect.xMin, btnRect.yMax),
                        new Vector3(btnRect.xMin, btnRect.yMin)
                    };
                    Handles.DrawAAPolyLine(1f, corners);
                }

                // Xử lý click
                EditorGUIUtility.AddCursorRect(btnRect, MouseCursor.Link);
                if (Event.current.type == EventType.MouseDown && btnRect.Contains(Event.current.mousePosition))
                {
                    _selected[i] = !_selected[i];
                    Event.current.Use();
                }
            }
            Handles.color = Color.white;


            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            Rect btnRandomRect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.button, GUILayout.Width(100));
            EditorGUIUtility.AddCursorRect(btnRandomRect, MouseCursor.Link);

            if (GUI.Button(btnRandomRect, "Random lines"))
            {
                _editor.FillLines(GetSelectedColors().ToArray(), minValue, maxValue);
            }

            Rect btnClearRect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.button, GUILayout.Width(100));
            EditorGUIUtility.AddCursorRect(btnClearRect, MouseCursor.Link);
            if (GUI.Button(btnClearRect, "Clear"))
            {
                _editor._levelconfig.ColorLines.Clear();
                EditorUtility.SetDirty(_editor._levelconfig);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            Rect btnCloseRect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.button, GUILayout.Width(205));
            EditorGUIUtility.AddCursorRect(btnCloseRect, MouseCursor.Link);
            if (GUI.Button(btnCloseRect, "Close"))
            {
                editorWindow?.Close();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
    }
    #endregion
    #region Grid Settings Popup
    private class ColorSettingPopup : PopupWindowContent
    {
        private readonly LevelConfigEditor _editor;

        public ColorSettingPopup(LevelConfigEditor editor)
        {
            _editor = editor;
        }

        public override Vector2 GetWindowSize() => new Vector2(250, 160);

        public override void OnGUI(Rect rect)
        {
            GUILayout.Space(5);
            EditorGUILayout.BeginVertical();
            _editor.DrawColorSampleField(ColorType.Outline.ToString(), ref _editor._outlineHexColor, ColorType.Outline);
            _editor.DrawColorSampleField(ColorType.Cell.ToString(), ref _editor._defaultHexColor, ColorType.Cell);
            _editor.DrawColorSampleField(ColorType.Label.ToString(), ref _editor._labelHexColor, ColorType.Label);
            _editor.DrawColorSampleField(ColorType.Hover.ToString(), ref _editor._hoverHexColor, ColorType.Hover);
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
                _editor.SetColor(ColorType.Hover, _editor.GetDefaultHex(ColorType.Hover));
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
