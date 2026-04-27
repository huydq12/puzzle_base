using UnityEngine;
using System;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.Serialization;


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
    [FormerlySerializedAs("Shooters")]
    public List<GateData> Gates;
    [Sirenix.OdinInspector.LabelText("Gate Double")]
    public List<GateDataDouble> GatesDouble;
#if UNITY_EDITOR
    [HideInInspector]
#endif
    public GridCellData[,] Cells;
    [HideInInspector]
    public List<ColorLine> ColorLines = new List<ColorLine>();
    public List<ElevatorData> Elevators = new List<ElevatorData>();
    public List<LineDoorData> LineDoors = new List<LineDoorData>();
    [InlineProperty]
    public ConveyorLine ConveyorLine;
    public CameraSetupData Camera;

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
#endif
    [Button]
    public void SwapColor(ObjectColor from, ObjectColor to)
    {
        // Shooters in Gates
        if (Gates != null)
        {
            foreach (var gate in Gates)
            {
                if (gate.Shooters == null) continue;
                foreach (var shooter in gate.Shooters)
                    if (shooter.Color == from) shooter.Color = to;
            }
        }

        // Shooters in GatesDouble
        if (GatesDouble != null)
        {
            foreach (var gateDouble in GatesDouble)
            {
                if (gateDouble.ShootersDouble == null) continue;
                foreach (var sd in gateDouble.ShootersDouble)
                {
                    if (sd.ShootersLeft != null)
                        foreach (var s in sd.ShootersLeft)
                            if (s.Color == from) s.Color = to;
                    if (sd.ShootersRight != null)
                        foreach (var s in sd.ShootersRight)
                            if (s.Color == from) s.Color = to;
                }
            }
        }

        // ColorLines
        if (ColorLines != null)
            foreach (var line in ColorLines)
                if (line.Color == from) line.Color = to;

        // Elevators
        if (Elevators != null)
            foreach (var elevator in Elevators)
                if (elevator.Lines != null)
                    foreach (var line in elevator.Lines)
                        if (line.Color == from) line.Color = to;

        // LineDoors
        if (LineDoors != null)
            foreach (var door in LineDoors)
            {
                if (door.Color == from) door.Color = to;
                if (door.Lines != null)
                    foreach (var line in door.Lines)
                        if (line.Color == from) line.Color = to;
            }
    }
}

[Serializable]
public struct CameraSetupData
{
    public bool Enabled;
    public float MinOrthoSize;
    public float Padding;
    public bool UsePosition;
    public Vector3 Position;
    public bool UseOrthographicSize;
    public float OrthographicSize;
}
[HideReferenceObjectPicker]
public class GateData
{
    public Vector3 Position;
    [Range(0, 7)] public int Direction;
    [Sirenix.OdinInspector.LabelText("Shooter Counter")] public int Counter;
    [Sirenix.OdinInspector.LabelText("Gate Element Type")] public int ElementType;
    public List<ShooterData> Shooters;
}

[HideReferenceObjectPicker]
public class GateDataDouble
{
    public Vector3 Position;
    [Range(0, 7)] public int Direction;
    [Sirenix.OdinInspector.LabelText("Shooter Counter")] public int Counter;
    [Sirenix.OdinInspector.LabelText("Gate Element Type")] public int ElementType;
    public List<ShooterDataDouble> ShootersDouble;
}




[HideReferenceObjectPicker]
public class ShooterData
{
    public ObjectColor Color;
    public int Counter;
    [Sirenix.OdinInspector.LabelText("Element Type")] public int Type;
    [Sirenix.OdinInspector.LabelText("Tie ID")] public int TieID;
}

[HideReferenceObjectPicker]
public class ShooterDataDouble
{
    public List<ShooterData> ShootersLeft;
    public List<ShooterData> ShootersRight;
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
    public List<int> ElementTypes = new List<int>();
    public int Counter;
}

[Serializable]
public class ElevatorData
{
    public Vector2Int Position;
    public Vector2Int Size;
    public List<ColorLine> Lines = new List<ColorLine>();
}

[Serializable]
public class LineDoorData
{
    public Vector2Int Position;
    public Vector2Int Size;
    public int Direction;
    public ObjectColor Color;
    public int Counter;
    public List<ColorLine> Lines = new List<ColorLine>();
}

[Serializable]
public class ConveyorLine
{
    [ListDrawerSettings(DefaultExpandedState = false)]
    public List<Vector2Int> Cells = new List<Vector2Int>();

    // Optional per-node metadata (aligned by index with Cells).
    [ListDrawerSettings(DefaultExpandedState = false)]
    public List<int> Types = new List<int>();
    [ListDrawerSettings(DefaultExpandedState = false)]
    public List<int> Counters = new List<int>();
    [ListDrawerSettings(DefaultExpandedState = false)]
    public List<bool> IsHoles = new List<bool>();
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
