using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
using Sirenix.Utilities.Editor;
#endif

[CreateAssetMenu(fileName = "Level Config")]
public class LevelConfig : SerializedScriptableObject
{
    [FormerlySerializedAs("Level")]
    [SerializeField]
    private int _level;
#if UNITY_EDITOR
    [Range(1, 10)]
    [HorizontalGroup("Size", marginRight: 15)]
#endif
    [FormerlySerializedAs("Rows")]
    [SerializeField]
    private int _rows = 1;
#if UNITY_EDITOR
    [Range(1, 8)]
    [HorizontalGroup("Size")]
#endif
    [FormerlySerializedAs("Columns")]
    [SerializeField]
    private int _columns = 1;

#if UNITY_EDITOR
    [ListDrawerSettings(DefaultExpandedState = false)]
#endif
    public List<LevelGoalData> Goals = new();
#if UNITY_EDITOR
    [ListDrawerSettings(DefaultExpandedState = false, HideAddButton = true, OnTitleBarGUI = nameof(ToolbarSpawn)), CustomValueDrawer(nameof(DrawObjectColor))]
#endif
    [FormerlySerializedAs("Containers")]
    [SerializeField]
    private List<ObjectColor> _containers = new();

    public List<ObjectColor> GetContainers() => _containers;

    public void SetContainers(List<ObjectColor> containers)
    {
        _containers = containers != null ? containers : new List<ObjectColor>();
    }

    public int GetLevel() => _level;

    public void SetLevel(int level)
    {
        _level = level;
    }

    public int GetRows() => _rows;

    public void SetRows(int rows)
    {
        _rows = Mathf.Max(1, rows);
    }

    public int GetColumns() => _columns;

    public void SetColumns(int columns)
    {
        _columns = Mathf.Max(1, columns);
    }
    [Range(1, 40)]
    public int TotalSlot = 30;
#if UNITY_EDITOR
    [HideInInspector]
#endif
    public GridCellData[,] GridCellData;

#if UNITY_EDITOR
    private Dictionary<ObjectColor, int> _colorCellCounts = new();

    private int _totalColorCount;

    [OnInspectorGUI]
    private void DrawColorCountSummary()
    {
        RecountColorCellsAll();

        var style = new GUIStyle(EditorStyles.miniLabel);
        style.richText = true;

        SirenixEditorGUI.BeginBox();
        GUILayout.Label($"<b>Total Colors:</b> {_totalColorCount}", style);

        if (_colorCellCounts != null)
        {
            SirenixEditorGUI.BeginVerticalList();
            foreach (var kv in _colorCellCounts)
            {
                if (kv.Value <= 0) continue;
                GUILayout.Label($"{kv.Key}: {kv.Value}", style);
            }
            SirenixEditorGUI.EndVerticalList();
        }

        SirenixEditorGUI.EndBox();
    }

    private ObjectColor DrawObjectColor(ObjectColor value, GUIContent label)
    {
        return Common.DrawObjectColor(value, label);
    }

    private void ToolbarSpawn()
    {
        if (SirenixEditorGUI.ToolbarButton(new GUIContent(EditorIcons.Refresh.Raw, "Shuffle data")))
        {
            if (_containers.Count > 0)
            {
                _containers.Shuffle();
            }
        }
        Rect refreshRect = GUILayoutUtility.GetLastRect();
        EditorGUIUtility.AddCursorRect(refreshRect, MouseCursor.Link);
        if (SirenixEditorGUI.ToolbarButton(new GUIContent(EditorIcons.Plus.Raw, "Add data")))
        {
            if (_containers.Count == 0)
            {
                _containers.Add(ObjectColor.Green);
            }
            else
            {
                _containers.Add(_containers[^1]);
            }
        }
        Rect plusRect = GUILayoutUtility.GetLastRect();
        UnityEditor.EditorGUIUtility.AddCursorRect(plusRect, UnityEditor.MouseCursor.Link);
    }

    void OnValidate()
    {
        ResizeGridGridCellData();
        RecountColorCellsAll();
    }

    private void RecountColorCellsAll()
    {
        if (_colorCellCounts == null) _colorCellCounts = new();
        _colorCellCounts.Clear();

        _totalColorCount = 0;

        foreach (ObjectColor c in Enum.GetValues(typeof(ObjectColor)))
            _colorCellCounts[c] = 0;

        if (GridCellData == null) return;

        int cols = GridCellData.GetLength(0);
        int rows = GridCellData.GetLength(1);

        for (int col = 0; col < cols; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                var cell = GridCellData[col, row];
                if (cell == null) continue;
                if (cell.IsEmpty) continue;

                if (cell.Colors != null && cell.Colors.Count > 0)
                {
                    for (int i = 0; i < cell.Colors.Count; i++)
                    {
                        var cd = cell.Colors[i];
                        if (cd == null) continue;

                        _totalColorCount += 1;

                        var c = cd.Color;
                        if (_colorCellCounts.ContainsKey(c)) _colorCellCounts[c] += 1;
                        else _colorCellCounts[c] = 1;
                    }
                }

                if (cell.ElementType == CellElementType.Gate && cell.GateWaves != null && cell.GateWaves.Count > 0)
                {
                    for (int w = 0; w < cell.GateWaves.Count; w++)
                    {
                        var wave = cell.GateWaves[w];
                        if (wave == null || wave.Colors == null || wave.Colors.Count == 0) continue;

                        for (int i = 0; i < wave.Colors.Count; i++)
                        {
                            var c = wave.Colors[i];
                            _totalColorCount += 1;
                            if (_colorCellCounts.ContainsKey(c)) _colorCellCounts[c] += 1;
                            else _colorCellCounts[c] = 1;
                        }
                    }
                }
            }
        }
    }

    private void ResizeGridGridCellData()
    {
        if (GridCellData == null)
        {
            GridCellData = new GridCellData[_columns, _rows];
            for (int col = 0; col < _columns; col++)
            {
                for (int row = 0; row < _rows; row++)
                {
                    GridCellData[col, row] = new GridCellData() { Colors = new(), IsEmpty = false };
                }
            }
        }
        else
        {
            int currentColumns = GridCellData.GetLength(0);
            int currentRows = GridCellData.GetLength(1);

            GridCellData[,] newGridCellData = new GridCellData[_columns, _rows];

            for (int col = 0; col < Mathf.Min(currentColumns, _columns); col++)
            {
                for (int row = 0; row < Mathf.Min(currentRows, _rows); row++)
                {
                    if (GridCellData[col, row] == null) GridCellData[col, row] = new GridCellData() { Colors = new(), IsEmpty = false };
                    newGridCellData[col, row] = GridCellData[col, row];
                }
            }

            for (int col = 0; col < _columns; col++)
            {
                for (int row = 0; row < _rows; row++)
                {
                    if (col >= currentColumns || row >= currentRows)
                    {
                        newGridCellData[col, row] = new GridCellData()
                        {
                            Colors = new(),
                            IsEmpty = false
                        };
                    }
                }
            }
            GridCellData = newGridCellData;
        }
    }

#endif
}

[Serializable]
public class LevelGoalData
{
    public LevelGoalType Type;
    [Min(1)]
    public int TargetCount = 1;
#if UNITY_EDITOR
    [ShowIf(nameof(ShowTargetColor))]
#endif
    public ObjectColor TargetColor;

#if UNITY_EDITOR
    private bool ShowTargetColor()
    {
        return Type == LevelGoalType.CollectSpecificColor;
    }
#endif
}

public enum LevelGoalType
{
    CollectTotal,
    CollectSpecificColor
}

public enum CellElementType
{
    None,
    Ice,
    Gate,
    LockItem,
    LockItemColor,
    Screw
}

public class ColorData
{
    public ObjectColor Color;
}

[Serializable]
public class GateWaveData
{
    public List<ObjectColor> Colors = new();
}

public class GridCellData
{
    public bool IsEmpty;
#if UNITY_EDITOR
    [ShowIf(nameof(ShowElements))]
#endif
    public CellElementType ElementType;

#if UNITY_EDITOR
    [ShowIf(nameof(ShowGateFields))]
#endif
    public HexEdge GateDirection;

#if UNITY_EDITOR
    [ShowIf(nameof(ShowGateFields))]
#endif
    public List<GateWaveData> GateWaves = new();

#if UNITY_EDITOR
    [ShowIf(nameof(ShowIceFields))]
#endif
    [Min(0)]
    public int IceHitPoints;

#if UNITY_EDITOR
    [ShowIf(nameof(ShowScrewFields))]
#endif
    [Min(0)]
    public int ScrewHitPoints;

#if UNITY_EDITOR
    [ShowIf(nameof(ShowLockItemFields))]
#endif
    [Min(0)]
    public int LockItemCount;

#if UNITY_EDITOR
    [ShowIf(nameof(ShowLockItemColorFields))]
#endif
    public ObjectColor LockItemColor;

#if UNITY_EDITOR
    [ListDrawerSettings(DefaultExpandedState = false, HideAddButton = true, OnTitleBarGUI = nameof(DrawColorsToolbar))]
#endif
    public List<ColorData> Colors = new();

#if UNITY_EDITOR

    private bool ShowElements()
    {
        return !IsEmpty;
    }

    private bool ShowIceFields()
    {
        return !IsEmpty && ElementType == CellElementType.Ice;
    }

    private bool ShowScrewFields()
    {
        return !IsEmpty && ElementType == CellElementType.Screw;
    }

    private bool ShowGateFields()
    {
        return !IsEmpty && ElementType == CellElementType.Gate;
    }

    private bool ShowLockItemFields()
    {
        return !IsEmpty && (ElementType == CellElementType.LockItem || ElementType == CellElementType.LockItemColor);
    }

    private bool ShowLockItemColorFields()
    {
        return !IsEmpty && ElementType == CellElementType.LockItemColor;
    }

    private void DrawColorsToolbar()
    {
        if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
        {
            if (Colors.Count > 0)
            {
                foreach (var e in Colors)
                {
                    e.Color = Common.GetRandomEnumValue<ObjectColor>();
                }
            }
        }

        if (SirenixEditorGUI.ToolbarButton(EditorIcons.Plus))
        {
            Colors.Add(new ColorData());
        }
    }

    private ObjectColor DrawObjectColor(ObjectColor value, GUIContent label)
    {
        return Common.DrawObjectColor(value, label);
    }
#endif
}