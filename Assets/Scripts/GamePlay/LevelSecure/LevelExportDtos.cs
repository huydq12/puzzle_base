using System;
using System.Collections.Generic;

[Serializable]
public class LevelsExportRootDto
{
    public List<LevelConfigDto> levels = new();
}

[Serializable]
public class LevelConfigDto
{
    public int level;
    public int rows;
    public int columns;
    public int totalSlot;
    public List<LevelGoalDataDto> goals = new();
    public List<ObjectColor> containers = new();
    public List<GridColumnDto> grid = new();
}

[Serializable]
public class LevelGoalDataDto
{
    public LevelGoalType type;
    public int targetCount;
    public ObjectColor targetColor;
}

[Serializable]
public class GridColumnDto
{
    public List<GridCellDataDto> cells = new();
}

[Serializable]
public class GridCellDataDto
{
    public bool isEmpty;
    public CellElementType elementType;
    public HexEdge gateDirection;
    public List<GateWaveDataDto> gateWaves = new();
    public int iceHitPoints;
    public int screwHitPoints;
    public int lockItemCount;
    public ObjectColor lockItemColor;
    public List<ObjectColor> colors = new();
}

[Serializable]
public class GateWaveDataDto
{
    public List<ObjectColor> colors = new();
}
