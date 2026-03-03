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
    public List<GateDataDto> shooters = new();
    public List<GateDataDoubleDto> gatesDouble = new();

    // Stored as row-major list (index = x + y * columns).
    public List<int> cellTypes = new();

    public List<ColorLineDto> colorLines = new();
    public List<ElevatorDataDto> elevators = new();
    public List<LineDoorDataDto> lineDoors = new();
    public ConveyorLineDto conveyorLine;
    public CameraSetupDto camera;
}

[Serializable]
public class GateDataDto
{
    public Vec3Dto position;
    public int direction;
    public int counter;
    public int elementType;
    public List<ShooterDataDto> shooters = new();
}

[Serializable]
public class GateDataDoubleDto
{
    public Vec3Dto position;
    public int direction;
    public int counter;
    public int elementType;
    public List<ShooterDataDoubleDto> shootersDouble = new();
}

[Serializable]
public class ShooterDataDto
{
    public int color;
    public int counter;
    public int type;
    public int tieID;
}

[Serializable]
public class ShooterDataDoubleDto
{
    public List<ShooterDataDto> shootersLeft = new();
    public List<ShooterDataDto> shootersRight = new();
}

[Serializable]
public class ColorLineDto
{
    public int color;
    public List<Vec2IntDto> cells = new();
    public List<int> elementTypes = new();
    public int counter;
}

[Serializable]
public class ElevatorDataDto
{
    public Vec2IntDto position;
    public Vec2IntDto size;
    public List<ColorLineDto> lines = new();
}

[Serializable]
public class LineDoorDataDto
{
    public Vec2IntDto position;
    public Vec2IntDto size;
    public int direction;
    public int color;
    public int counter;
    public List<ColorLineDto> lines = new();
}

[Serializable]
public class ConveyorLineDto
{
    public List<Vec2IntDto> cells = new();
    public List<int> types = new();
    public List<int> counters = new();
    public List<bool> isHoles = new();
}

[Serializable]
public class CameraSetupDto
{
    public bool enabled;
    public float minOrthoSize;
    public float padding;
    public bool usePosition;
    public Vec3Dto position;
    public bool useOrthographicSize;
    public float orthographicSize;
}

[Serializable]
public struct Vec2IntDto
{
    public int x;
    public int y;
}

[Serializable]
public struct Vec3Dto
{
    public float x;
    public float y;
    public float z;
}
