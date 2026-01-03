using UnityEngine;
using System;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Newtonsoft.Json;


#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
using UnityEditor;
using Sirenix.OdinInspector.Editor;
#endif

[CreateAssetMenu(fileName = "New Level config")]
public class LevelConfig : SerializedScriptableObject
{
    public int Level;
#if UNITY_EDITOR
    [Range(0, 100), HorizontalGroup("Size", MarginRight = 15)]
#endif
    public int Rows;
#if UNITY_EDITOR
    [Range(0, 100), HorizontalGroup("Size")]
#endif
    public int Columns;
    public List<GateData> Shooters;
#if UNITY_EDITOR
    [HideInInspector]
#endif
    public GridCellData[,] Cells;
#if UNITY_EDITOR
    [HideInInspector]
#endif
    public List<ColorLine> ColorLines = new List<ColorLine>();
#if UNITY_EDITOR
    [HideInInspector]
#endif
    public ConveyorLine ConveyorLine;

#if UNITY_EDITOR
    private void ResizeGridCells()
    {
        GridCellData[,] newCells = new GridCellData[Columns, Rows];

        if (Cells != null)
        {
            int currentColumns = Cells.GetLength(0);
            int currentRows = Cells.GetLength(1);

            for (int col = 0; col < Mathf.Min(currentColumns, Columns); col++)
            {
                for (int row = 0; row < Mathf.Min(currentRows, Rows); row++)
                {
                    newCells[col, row] = Cells[col, row];
                }
            }
        }

        for (int col = 0; col < Columns; col++)
        {
            for (int row = 0; row < Rows; row++)
            {
                if (newCells[col, row] == null)
                {
                    newCells[col, row] = new GridCellData();
                }
            }
        }
        Cells = newCells;
    }
    void OnEnable()
    {
        ResizeGridCells();
    }
    void OnValidate()
    {
        ResizeGridCells();
    }
    [Button]
    public void ComnvertJsonToLevel(TextAsset text)
    {
        if (text == null)
        {
            Debug.LogError("JSON TextAsset is null");
            return;
        }

        // Clear data cũ
        ColorLines.Clear();

        // Deserialize phần cần thiết
        JsonRoot root = JsonConvert.DeserializeObject<JsonRoot>(text.text);

        if (root == null || root.arrows == null)
        {
            Debug.LogError("Invalid JSON or no arrows found");
            return;
        }

        foreach (var arrow in root.arrows)
        {
            if (arrow.unitPositions == null || arrow.unitPositions.Count == 0)
                continue;

            ColorLine line = new ColorLine();
            line.Color = (ObjectColor)arrow.color;

            foreach (var pos in arrow.unitPositions)
            {
                line.Cells.Add(new Vector2Int(pos.x, pos.y));
            }

            ColorLines.Add(line);
        }

        EditorUtility.SetDirty(this);
        Debug.Log($"Convert JSON → LevelConfig done. ColorLines: {ColorLines.Count}");
    }
#endif
}
[HideReferenceObjectPicker]
public class GateData
{
    public Vector3 Position;
    [Range(0, 4)] public int Direction;
    public List<ShooterData> Shooters;
}
[HideReferenceObjectPicker]
public class ShooterData
{
    public ObjectColor Color;
    public int Counter;
}
public class GridCellData
{
    public GridCellType CellType = GridCellType.Normal;
}
[Serializable]
public class ColorLine
{
    public ObjectColor Color;
    public List<Vector2Int> Cells = new List<Vector2Int>();
}

[Serializable]
public class ConveyorLine
{
    [ListDrawerSettings(DefaultExpandedState = false)]
    public List<Vector2Int> Cells = new List<Vector2Int>();
}

[Serializable]
public class JsonRoot
{
    public List<JsonArrow> arrows;
}

[Serializable]
public class JsonArrow
{
    public List<JsonUnitPos> unitPositions;
    public int color;
}

[Serializable]
public class JsonUnitPos
{
    public int x;
    public int y;
}
