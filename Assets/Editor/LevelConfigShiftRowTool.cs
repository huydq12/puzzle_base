#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class LevelConfigShiftRowTool
{
    [MenuItem("Tools/Levels/Shift Selected LevelConfig +1 Row (Up)")]
    private static void ShiftSelectedLevelConfigsUpOneRow()
    {
        Object[] selected = Selection.objects;
        if (selected == null || selected.Length == 0)
        {
            EditorUtility.DisplayDialog("Shift LevelConfig", "Select one or more `LevelConfig` assets in Project window.", "OK");
            return;
        }

        int updated = 0;
        int skipped = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            for (int i = 0; i < selected.Length; i++)
            {
                if (selected[i] is not LevelConfig config)
                {
                    skipped++;
                    continue;
                }

                bool changed = ShiftConfigUpOneRow(config);
                if (changed)
                {
                    EditorUtility.SetDirty(config);
                    updated++;
                }
                else
                {
                    skipped++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Shift LevelConfig", $"Done.\nUpdated: {updated}\nSkipped: {skipped}", "OK");
    }

    [MenuItem("Tools/Levels/Fix Selected LevelConfig Connectivity (Arrows/Conveyor)")]
    private static void FixSelectedLevelConfigConnectivity()
    {
        Object[] selected = Selection.objects;
        if (selected == null || selected.Length == 0)
        {
            EditorUtility.DisplayDialog("Fix Connectivity", "Select one or more `LevelConfig` assets in Project window.", "OK");
            return;
        }

        int updated = 0;
        int skipped = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            for (int i = 0; i < selected.Length; i++)
            {
                if (selected[i] is not LevelConfig config)
                {
                    skipped++;
                    continue;
                }

                bool changed = FixConnectivity(config);
                if (changed)
                {
                    EditorUtility.SetDirty(config);
                    updated++;
                }
                else
                {
                    skipped++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Fix Connectivity", $"Done.\nUpdated: {updated}\nSkipped: {skipped}", "OK");
    }

    [MenuItem("Tools/Levels/Sync Selected LevelConfig Cells From ConveyorLine")]
    private static void SyncSelectedLevelConfigCellsFromConveyorLine()
    {
        Object[] selected = Selection.objects;
        if (selected == null || selected.Length == 0)
        {
            EditorUtility.DisplayDialog("Sync Cells", "Select one or more `LevelConfig` assets in Project window.", "OK");
            return;
        }

        int updated = 0;
        int skipped = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            for (int i = 0; i < selected.Length; i++)
            {
                if (selected[i] is not LevelConfig config)
                {
                    skipped++;
                    continue;
                }

                bool changed = SyncCellsFromConveyorLine(config);
                if (changed)
                {
                    EditorUtility.SetDirty(config);
                    updated++;
                }
                else
                {
                    skipped++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Sync Cells", $"Done.\nUpdated: {updated}\nSkipped: {skipped}", "OK");
    }

    [MenuItem("Tools/Levels/Insert Empty Row Between Row 4 and 5")]
    private static void InsertEmptyRowBetweenRow4And5()
    {
        Object[] selected = Selection.objects;
        if (selected == null || selected.Length == 0)
        {
            EditorUtility.DisplayDialog("Insert Row", "Select one or more `LevelConfig` assets in Project window.", "OK");
            return;
        }

        int updated = 0;
        int skipped = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            for (int i = 0; i < selected.Length; i++)
            {
                if (selected[i] is not LevelConfig config)
                {
                    skipped++;
                    continue;
                }

                bool changed = InsertEmptyRowAfterOneBased(config, afterRowOneBased: 4);
                if (changed)
                {
                    EditorUtility.SetDirty(config);
                    updated++;
                }
                else
                {
                    skipped++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Insert Row", $"Done.\nUpdated: {updated}\nSkipped: {skipped}", "OK");
    }

    [MenuItem("Tools/Levels/Shift Selected LevelConfig +1 Column (Right)")]
    private static void ShiftSelectedLevelConfigsRightOneColumn()
    {
        Object[] selected = Selection.objects;
        if (selected == null || selected.Length == 0)
        {
            EditorUtility.DisplayDialog("Shift LevelConfig", "Select one or more `LevelConfig` assets in Project window.", "OK");
            return;
        }

        int updated = 0;
        int skipped = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            for (int i = 0; i < selected.Length; i++)
            {
                if (selected[i] is not LevelConfig config)
                {
                    skipped++;
                    continue;
                }

                bool changed = ShiftConfigRightOneColumn(config);
                if (changed)
                {
                    EditorUtility.SetDirty(config);
                    updated++;
                }
                else
                {
                    skipped++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Shift LevelConfig", $"Done.\nUpdated: {updated}\nSkipped: {skipped}", "OK");
    }

    private static bool ShiftConfigUpOneRow(LevelConfig config)
    {
        if (config == null) return false;
        if (config.Columns <= 0 || config.Rows <= 0) return false;

        int oldCols = config.Columns;
        int oldRows = config.Rows;
        int newRows = oldRows + 1;

        config.Rows = newRows;

        ShiftCellsArray(config, oldCols, oldRows, newRows);
        ShiftColorLines(config.ColorLines);
        ShiftElevators(config.Elevators);
        ShiftLineDoors(config.LineDoors);
        ShiftConveyor(config.ConveyorLine);

        return true;
    }

    private static bool ShiftConfigRightOneColumn(LevelConfig config)
    {
        if (config == null) return false;
        if (config.Columns <= 0 || config.Rows <= 0) return false;

        int oldCols = config.Columns;
        int oldRows = config.Rows;
        int newCols = oldCols + 1;

        config.Columns = newCols;

        ShiftCellsArrayRight(config, oldCols, newCols, oldRows);
        ShiftColorLinesRight(config.ColorLines);
        ShiftElevatorsRight(config.Elevators);
        ShiftLineDoorsRight(config.LineDoors);
        ShiftConveyorRight(config.ConveyorLine);

        return true;
    }

    internal static bool InsertEmptyRowAfterOneBased(LevelConfig config, int afterRowOneBased)
    {
        if (config == null) return false;
        if (config.Columns <= 0 || config.Rows <= 0) return false;
        if (afterRowOneBased < 0) return false;

        int insertAtY = afterRowOneBased; // 1-based row N ends at y=N-1, so insert row at y=N (0-based).
        int oldRows = config.Rows;
        if (insertAtY < 0) insertAtY = 0;
        if (insertAtY > oldRows) insertAtY = oldRows;

        int cols = config.Columns;
        int newRows = oldRows + 1;
        config.Rows = newRows;

        GridCellData[,] oldCells = config.Cells;
        GridCellData[,] newCells = new GridCellData[cols, newRows];

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < newRows; y++)
            {
                newCells[x, y] = new GridCellData { CellType = GridCellType.Normal };
            }
        }

        if (oldCells != null && oldCells.GetLength(0) == cols && oldCells.GetLength(1) == oldRows)
        {
            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < oldRows; y++)
                {
                    GridCellData c = oldCells[x, y];
                    if (c == null) continue;
                    int ny = y >= insertAtY ? y + 1 : y;
                    newCells[x, ny].CellType = c.CellType;
                }
            }
        }

        config.Cells = newCells;

        InsertEmptyRowIntoColorLines(config.ColorLines, insertAtY);
        InsertEmptyRowIntoElevators(config.Elevators, insertAtY);
        InsertEmptyRowIntoLineDoors(config.LineDoors, insertAtY);
        InsertEmptyRowIntoConveyor(config.ConveyorLine, insertAtY);

        return true;
    }

    internal static bool InsertEmptyColumnAfterOneBased(LevelConfig config, int afterColumnOneBased)
    {
        if (config == null) return false;
        if (config.Columns <= 0 || config.Rows <= 0) return false;
        if (afterColumnOneBased < 0) return false;

        int insertAtX = afterColumnOneBased; // 1-based column N ends at x=N-1, so insert column at x=N (0-based).
        int oldCols = config.Columns;
        int rows = config.Rows;
        if (insertAtX < 0) insertAtX = 0;
        if (insertAtX > oldCols) insertAtX = oldCols;

        int newCols = oldCols + 1;
        config.Columns = newCols;

        GridCellData[,] oldCells = config.Cells;
        GridCellData[,] newCells = new GridCellData[newCols, rows];

        for (int x = 0; x < newCols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                newCells[x, y] = new GridCellData { CellType = GridCellType.Normal };
            }
        }

        if (oldCells != null && oldCells.GetLength(0) == oldCols && oldCells.GetLength(1) == rows)
        {
            for (int x = 0; x < oldCols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    GridCellData c = oldCells[x, y];
                    if (c == null) continue;
                    int nx = x >= insertAtX ? x + 1 : x;
                    newCells[nx, y].CellType = c.CellType;
                }
            }
        }

        config.Cells = newCells;

        InsertEmptyColumnIntoColorLines(config.ColorLines, insertAtX);
        InsertEmptyColumnIntoElevators(config.Elevators, insertAtX);
        InsertEmptyColumnIntoLineDoors(config.LineDoors, insertAtX);
        InsertEmptyColumnIntoConveyor(config.ConveyorLine, insertAtX);

        return true;
    }

    internal static bool InsertEmptyRowBeforeOneBased(LevelConfig config, int beforeRowOneBased)
    {
        // Insert "before row N" == insert at y = N-1 (0-based) == after row N-1.
        if (beforeRowOneBased <= 1) return InsertEmptyRowAfterOneBased(config, 0);
        return InsertEmptyRowAfterOneBased(config, beforeRowOneBased - 1);
    }

    internal static bool InsertEmptyColumnBeforeOneBased(LevelConfig config, int beforeColumnOneBased)
    {
        // Insert "before column N" == insert at x = N-1 (0-based) == after column N-1.
        if (beforeColumnOneBased <= 1) return InsertEmptyColumnAfterOneBased(config, 0);
        return InsertEmptyColumnAfterOneBased(config, beforeColumnOneBased - 1);
    }

    internal static bool RemoveRowOneBased(LevelConfig config, int rowOneBased)
    {
        if (config == null) return false;
        if (config.Columns <= 0 || config.Rows <= 0) return false;
        if (rowOneBased <= 0) return false;

        int removeAtY = rowOneBased - 1;
        int cols = config.Columns;
        int oldRows = config.Rows;
        if (removeAtY < 0 || removeAtY >= oldRows) return false;
        if (oldRows <= 1) return false;

        int newRows = oldRows - 1;
        config.Rows = newRows;

        GridCellData[,] oldCells = config.Cells;
        GridCellData[,] newCells = new GridCellData[cols, newRows];
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < newRows; y++)
                newCells[x, y] = new GridCellData { CellType = GridCellType.Normal };
        }

        if (oldCells != null && oldCells.GetLength(0) == cols && oldCells.GetLength(1) == oldRows)
        {
            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < oldRows; y++)
                {
                    if (y == removeAtY) continue;
                    GridCellData c = oldCells[x, y];
                    if (c == null) continue;
                    int ny = y > removeAtY ? y - 1 : y;
                    if (ny >= 0 && ny < newRows)
                        newCells[x, ny].CellType = c.CellType;
                }
            }
        }

        config.Cells = newCells;

        RemoveRowFromColorLines(config.ColorLines, removeAtY);
        RemoveRowFromElevators(config.Elevators, removeAtY);
        RemoveRowFromLineDoors(config.LineDoors, removeAtY);
        RemoveRowFromConveyor(config.ConveyorLine, removeAtY);

        return true;
    }

    internal static bool RemoveColumnOneBased(LevelConfig config, int columnOneBased)
    {
        if (config == null) return false;
        if (config.Columns <= 0 || config.Rows <= 0) return false;
        if (columnOneBased <= 0) return false;

        int removeAtX = columnOneBased - 1;
        int oldCols = config.Columns;
        int rows = config.Rows;
        if (removeAtX < 0 || removeAtX >= oldCols) return false;
        if (oldCols <= 1) return false;

        int newCols = oldCols - 1;
        config.Columns = newCols;

        GridCellData[,] oldCells = config.Cells;
        GridCellData[,] newCells = new GridCellData[newCols, rows];
        for (int x = 0; x < newCols; x++)
        {
            for (int y = 0; y < rows; y++)
                newCells[x, y] = new GridCellData { CellType = GridCellType.Normal };
        }

        if (oldCells != null && oldCells.GetLength(0) == oldCols && oldCells.GetLength(1) == rows)
        {
            for (int x = 0; x < oldCols; x++)
            {
                if (x == removeAtX) continue;
                for (int y = 0; y < rows; y++)
                {
                    GridCellData c = oldCells[x, y];
                    if (c == null) continue;
                    int nx = x > removeAtX ? x - 1 : x;
                    if (nx >= 0 && nx < newCols)
                        newCells[nx, y].CellType = c.CellType;
                }
            }
        }

        config.Cells = newCells;

        RemoveColumnFromColorLines(config.ColorLines, removeAtX);
        RemoveColumnFromElevators(config.Elevators, removeAtX);
        RemoveColumnFromLineDoors(config.LineDoors, removeAtX);
        RemoveColumnFromConveyor(config.ConveyorLine, removeAtX);

        return true;
    }

    private static bool SyncCellsFromConveyorLine(LevelConfig config)
    {
        if (config == null) return false;
        if (config.Columns <= 0 || config.Rows <= 0) return false;
        if (config.Cells == null || config.Cells.GetLength(0) != config.Columns || config.Cells.GetLength(1) != config.Rows) return false;

        bool changed = false;

        for (int x = 0; x < config.Columns; x++)
        {
            for (int y = 0; y < config.Rows; y++)
            {
                GridCellData cell = config.Cells[x, y];
                if (cell == null)
                {
                    config.Cells[x, y] = new GridCellData { CellType = GridCellType.Normal };
                    changed = true;
                    continue;
                }

                if (cell.CellType != GridCellType.Normal)
                {
                    cell.CellType = GridCellType.Normal;
                    changed = true;
                }
            }
        }

        if (config.ConveyorLine == null || config.ConveyorLine.Cells == null) return changed;

        for (int i = 0; i < config.ConveyorLine.Cells.Count; i++)
        {
            Vector2Int p = config.ConveyorLine.Cells[i];
            if (p.x < 0 || p.x >= config.Columns || p.y < 0 || p.y >= config.Rows) continue;
            GridCellData cell = config.Cells[p.x, p.y];
            if (cell == null)
            {
                config.Cells[p.x, p.y] = new GridCellData { CellType = GridCellType.Conveyor };
                changed = true;
                continue;
            }

            if (cell.CellType != GridCellType.Conveyor)
            {
                cell.CellType = GridCellType.Conveyor;
                changed = true;
            }
        }

        return changed;
    }

    private static bool FixConnectivity(LevelConfig config)
    {
        if (config == null) return false;

        bool anyChanged = false;

        if (config.ColorLines != null)
        {
            for (int i = 0; i < config.ColorLines.Count; i++)
            {
                ColorLine line = config.ColorLines[i];
                if (line == null || line.Cells == null || line.Cells.Count < 2) continue;

                bool syncElementTypes = line.ElementTypes != null && line.ElementTypes.Count == line.Cells.Count;
                anyChanged |= BridgeAnyGaps(line.Cells, isLoop: false, insertIndex =>
                {
                    if (syncElementTypes) line.ElementTypes.Insert(insertIndex, 0);
                });
            }
        }

        if (config.Elevators != null)
        {
            for (int i = 0; i < config.Elevators.Count; i++)
            {
                ElevatorData e = config.Elevators[i];
                if (e == null || e.Lines == null) continue;
                anyChanged |= FixColorLinesConnectivity(e.Lines);
            }
        }

        if (config.LineDoors != null)
        {
            for (int i = 0; i < config.LineDoors.Count; i++)
            {
                LineDoorData d = config.LineDoors[i];
                if (d == null || d.Lines == null) continue;
                anyChanged |= FixColorLinesConnectivity(d.Lines);
            }
        }

        if (config.ConveyorLine != null && config.ConveyorLine.Cells != null && config.ConveyorLine.Cells.Count >= 2)
        {
            ConveyorLine conveyor = config.ConveyorLine;

            bool syncTypes = conveyor.Types != null && conveyor.Types.Count == conveyor.Cells.Count;
            bool syncCounters = conveyor.Counters != null && conveyor.Counters.Count == conveyor.Cells.Count;
            bool syncHoles = conveyor.IsHoles != null && conveyor.IsHoles.Count == conveyor.Cells.Count;

            anyChanged |= BridgeAnyGaps(conveyor.Cells, isLoop: true, insertIndex =>
            {
                if (syncTypes) conveyor.Types.Insert(insertIndex, 0);
                if (syncCounters) conveyor.Counters.Insert(insertIndex, 0);
                if (syncHoles) conveyor.IsHoles.Insert(insertIndex, false);
            });
        }

        return anyChanged;
    }

    private static bool FixColorLinesConnectivity(List<ColorLine> lines)
    {
        if (lines == null) return false;

        bool anyChanged = false;
        for (int i = 0; i < lines.Count; i++)
        {
            ColorLine line = lines[i];
            if (line == null || line.Cells == null || line.Cells.Count < 2) continue;

            bool syncElementTypes = line.ElementTypes != null && line.ElementTypes.Count == line.Cells.Count;
            anyChanged |= BridgeAnyGaps(line.Cells, isLoop: false, insertIndex =>
            {
                if (syncElementTypes) line.ElementTypes.Insert(insertIndex, 0);
            });
        }
        return anyChanged;
    }

    private static void ShiftCellsArray(LevelConfig config, int cols, int oldRows, int newRows)
    {
        GridCellData[,] oldCells = config.Cells;
        GridCellData[,] newCells = new GridCellData[cols, newRows];

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < newRows; y++)
            {
                newCells[x, y] = new GridCellData { CellType = GridCellType.Normal };
            }
        }

        if (oldCells != null && oldCells.GetLength(0) == cols && oldCells.GetLength(1) == oldRows)
        {
            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < oldRows; y++)
                {
                    GridCellData c = oldCells[x, y];
                    if (c == null) continue;
                    newCells[x, y + 1].CellType = c.CellType;
                }
            }
        }

        config.Cells = newCells;
    }

    private static void ShiftCellsArrayRight(LevelConfig config, int oldCols, int newCols, int rows)
    {
        GridCellData[,] oldCells = config.Cells;
        GridCellData[,] newCells = new GridCellData[newCols, rows];

        for (int x = 0; x < newCols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                newCells[x, y] = new GridCellData { CellType = GridCellType.Normal };
            }
        }

        if (oldCells != null && oldCells.GetLength(0) == oldCols && oldCells.GetLength(1) == rows)
        {
            for (int x = 0; x < oldCols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    GridCellData c = oldCells[x, y];
                    if (c == null) continue;
                    newCells[x + 1, y].CellType = c.CellType;
                }
            }
        }

        config.Cells = newCells;
    }

    private static void ShiftColorLines(List<ColorLine> lines)
    {
        if (lines == null) return;

        for (int i = 0; i < lines.Count; i++)
        {
            ColorLine line = lines[i];
            if (line == null || line.Cells == null) continue;

            for (int j = 0; j < line.Cells.Count; j++)
            {
                Vector2Int p = line.Cells[j];
                p.y += 1;
                line.Cells[j] = p;
            }
        }
    }

    private static void ShiftColorLinesY(List<ColorLine> lines, int thresholdYInclusive, int deltaY)
    {
        if (lines == null) return;

        for (int i = 0; i < lines.Count; i++)
        {
            ColorLine line = lines[i];
            if (line == null || line.Cells == null) continue;

            for (int j = 0; j < line.Cells.Count; j++)
            {
                Vector2Int p = line.Cells[j];
                if (p.y >= thresholdYInclusive) p.y += deltaY;
                line.Cells[j] = p;
            }
        }
    }

    private static void InsertEmptyRowIntoColorLines(List<ColorLine> lines, int insertAtY)
    {
        if (lines == null) return;

        for (int i = 0; i < lines.Count; i++)
        {
            ColorLine line = lines[i];
            if (line == null || line.Cells == null || line.Cells.Count == 0) continue;

            bool syncElementTypes = line.ElementTypes != null && line.ElementTypes.Count == line.Cells.Count;
            ShiftAndBridgeY(line.Cells, insertAtY, isLoop: false, insertIndex =>
            {
                if (syncElementTypes) line.ElementTypes.Insert(insertIndex, 0);
            });
        }
    }

    private static void InsertEmptyColumnIntoColorLines(List<ColorLine> lines, int insertAtX)
    {
        if (lines == null) return;

        for (int i = 0; i < lines.Count; i++)
        {
            ColorLine line = lines[i];
            if (line == null || line.Cells == null || line.Cells.Count == 0) continue;

            bool syncElementTypes = line.ElementTypes != null && line.ElementTypes.Count == line.Cells.Count;
            ShiftAndBridgeX(line.Cells, insertAtX, isLoop: false, insertIndex =>
            {
                if (syncElementTypes) line.ElementTypes.Insert(insertIndex, 0);
            });
        }
    }

    private static void ShiftColorLinesX(List<ColorLine> lines, int thresholdXInclusive, int deltaX)
    {
        if (lines == null) return;

        for (int i = 0; i < lines.Count; i++)
        {
            ColorLine line = lines[i];
            if (line == null || line.Cells == null) continue;

            for (int j = 0; j < line.Cells.Count; j++)
            {
                Vector2Int p = line.Cells[j];
                if (p.x >= thresholdXInclusive) p.x += deltaX;
                line.Cells[j] = p;
            }
        }
    }

    private static void ShiftColorLinesRight(List<ColorLine> lines)
    {
        if (lines == null) return;

        for (int i = 0; i < lines.Count; i++)
        {
            ColorLine line = lines[i];
            if (line == null || line.Cells == null) continue;

            for (int j = 0; j < line.Cells.Count; j++)
            {
                Vector2Int p = line.Cells[j];
                p.x += 1;
                line.Cells[j] = p;
            }
        }
    }

    private static void ShiftElevators(List<ElevatorData> elevators)
    {
        if (elevators == null) return;

        for (int i = 0; i < elevators.Count; i++)
        {
            ElevatorData e = elevators[i];
            if (e == null) continue;
            e.Position = new Vector2Int(e.Position.x, e.Position.y + 1);
            ShiftColorLines(e.Lines);
        }
    }

    private static void ShiftElevatorsY(List<ElevatorData> elevators, int thresholdYInclusive, int deltaY)
    {
        if (elevators == null) return;

        for (int i = 0; i < elevators.Count; i++)
        {
            ElevatorData e = elevators[i];
            if (e == null) continue;
            if (e.Position.y >= thresholdYInclusive)
                e.Position = new Vector2Int(e.Position.x, e.Position.y + deltaY);
            ShiftColorLinesY(e.Lines, thresholdYInclusive, deltaY);
        }
    }

    private static void InsertEmptyRowIntoElevators(List<ElevatorData> elevators, int insertAtY)
    {
        if (elevators == null) return;

        for (int i = 0; i < elevators.Count; i++)
        {
            ElevatorData e = elevators[i];
            if (e == null) continue;
            if (e.Position.y >= insertAtY)
                e.Position = new Vector2Int(e.Position.x, e.Position.y + 1);
            InsertEmptyRowIntoColorLines(e.Lines, insertAtY);
        }
    }

    private static void InsertEmptyColumnIntoElevators(List<ElevatorData> elevators, int insertAtX)
    {
        if (elevators == null) return;

        for (int i = 0; i < elevators.Count; i++)
        {
            ElevatorData e = elevators[i];
            if (e == null) continue;
            if (e.Position.x >= insertAtX)
                e.Position = new Vector2Int(e.Position.x + 1, e.Position.y);
            InsertEmptyColumnIntoColorLines(e.Lines, insertAtX);
        }
    }

    private static void ShiftElevatorsX(List<ElevatorData> elevators, int thresholdXInclusive, int deltaX)
    {
        if (elevators == null) return;

        for (int i = 0; i < elevators.Count; i++)
        {
            ElevatorData e = elevators[i];
            if (e == null) continue;
            if (e.Position.x >= thresholdXInclusive)
                e.Position = new Vector2Int(e.Position.x + deltaX, e.Position.y);
            ShiftColorLinesX(e.Lines, thresholdXInclusive, deltaX);
        }
    }

    private static void ShiftElevatorsRight(List<ElevatorData> elevators)
    {
        if (elevators == null) return;

        for (int i = 0; i < elevators.Count; i++)
        {
            ElevatorData e = elevators[i];
            if (e == null) continue;
            e.Position = new Vector2Int(e.Position.x + 1, e.Position.y);
            ShiftColorLinesRight(e.Lines);
        }
    }

    private static void ShiftLineDoors(List<LineDoorData> doors)
    {
        if (doors == null) return;

        for (int i = 0; i < doors.Count; i++)
        {
            LineDoorData d = doors[i];
            if (d == null) continue;
            d.Position = new Vector2Int(d.Position.x, d.Position.y + 1);
            ShiftColorLines(d.Lines);
        }
    }

    private static void ShiftLineDoorsY(List<LineDoorData> doors, int thresholdYInclusive, int deltaY)
    {
        if (doors == null) return;

        for (int i = 0; i < doors.Count; i++)
        {
            LineDoorData d = doors[i];
            if (d == null) continue;
            if (d.Position.y >= thresholdYInclusive)
                d.Position = new Vector2Int(d.Position.x, d.Position.y + deltaY);
            ShiftColorLinesY(d.Lines, thresholdYInclusive, deltaY);
        }
    }

    private static void InsertEmptyRowIntoLineDoors(List<LineDoorData> doors, int insertAtY)
    {
        if (doors == null) return;

        for (int i = 0; i < doors.Count; i++)
        {
            LineDoorData d = doors[i];
            if (d == null) continue;
            if (d.Position.y >= insertAtY)
                d.Position = new Vector2Int(d.Position.x, d.Position.y + 1);
            InsertEmptyRowIntoColorLines(d.Lines, insertAtY);
        }
    }

    private static void InsertEmptyColumnIntoLineDoors(List<LineDoorData> doors, int insertAtX)
    {
        if (doors == null) return;

        for (int i = 0; i < doors.Count; i++)
        {
            LineDoorData d = doors[i];
            if (d == null) continue;
            if (d.Position.x >= insertAtX)
                d.Position = new Vector2Int(d.Position.x + 1, d.Position.y);
            InsertEmptyColumnIntoColorLines(d.Lines, insertAtX);
        }
    }

    private static void ShiftLineDoorsX(List<LineDoorData> doors, int thresholdXInclusive, int deltaX)
    {
        if (doors == null) return;

        for (int i = 0; i < doors.Count; i++)
        {
            LineDoorData d = doors[i];
            if (d == null) continue;
            if (d.Position.x >= thresholdXInclusive)
                d.Position = new Vector2Int(d.Position.x + deltaX, d.Position.y);
            ShiftColorLinesX(d.Lines, thresholdXInclusive, deltaX);
        }
    }

    private static void ShiftLineDoorsRight(List<LineDoorData> doors)
    {
        if (doors == null) return;

        for (int i = 0; i < doors.Count; i++)
        {
            LineDoorData d = doors[i];
            if (d == null) continue;
            d.Position = new Vector2Int(d.Position.x + 1, d.Position.y);
            ShiftColorLinesRight(d.Lines);
        }
    }

    private static void ShiftConveyor(ConveyorLine conveyor)
    {
        if (conveyor == null || conveyor.Cells == null) return;

        for (int i = 0; i < conveyor.Cells.Count; i++)
        {
            Vector2Int p = conveyor.Cells[i];
            p.y += 1;
            conveyor.Cells[i] = p;
        }
    }

    private static void ShiftConveyorY(ConveyorLine conveyor, int thresholdYInclusive, int deltaY)
    {
        if (conveyor == null || conveyor.Cells == null) return;

        for (int i = 0; i < conveyor.Cells.Count; i++)
        {
            Vector2Int p = conveyor.Cells[i];
            if (p.y >= thresholdYInclusive) p.y += deltaY;
            conveyor.Cells[i] = p;
        }
    }

    private static void InsertEmptyRowIntoConveyor(ConveyorLine conveyor, int insertAtY)
    {
        if (conveyor == null || conveyor.Cells == null || conveyor.Cells.Count == 0) return;

        bool syncTypes = conveyor.Types != null && conveyor.Types.Count == conveyor.Cells.Count;
        bool syncCounters = conveyor.Counters != null && conveyor.Counters.Count == conveyor.Cells.Count;
        bool syncHoles = conveyor.IsHoles != null && conveyor.IsHoles.Count == conveyor.Cells.Count;

        ShiftAndBridgeY(conveyor.Cells, insertAtY, isLoop: true, insertIndex =>
        {
            if (syncTypes) conveyor.Types.Insert(insertIndex, 0);
            if (syncCounters) conveyor.Counters.Insert(insertIndex, 0);
            if (syncHoles) conveyor.IsHoles.Insert(insertIndex, false);
        });
    }

    private static void InsertEmptyColumnIntoConveyor(ConveyorLine conveyor, int insertAtX)
    {
        if (conveyor == null || conveyor.Cells == null || conveyor.Cells.Count == 0) return;

        bool syncTypes = conveyor.Types != null && conveyor.Types.Count == conveyor.Cells.Count;
        bool syncCounters = conveyor.Counters != null && conveyor.Counters.Count == conveyor.Cells.Count;
        bool syncHoles = conveyor.IsHoles != null && conveyor.IsHoles.Count == conveyor.Cells.Count;

        ShiftAndBridgeX(conveyor.Cells, insertAtX, isLoop: true, insertIndex =>
        {
            if (syncTypes) conveyor.Types.Insert(insertIndex, 0);
            if (syncCounters) conveyor.Counters.Insert(insertIndex, 0);
            if (syncHoles) conveyor.IsHoles.Insert(insertIndex, false);
        });
    }

    private static void ShiftConveyorX(ConveyorLine conveyor, int thresholdXInclusive, int deltaX)
    {
        if (conveyor == null || conveyor.Cells == null) return;

        for (int i = 0; i < conveyor.Cells.Count; i++)
        {
            Vector2Int p = conveyor.Cells[i];
            if (p.x >= thresholdXInclusive) p.x += deltaX;
            conveyor.Cells[i] = p;
        }
    }

    private static void ShiftAndBridgeY(List<Vector2Int> cells, int insertAtY, bool isLoop, System.Action<int> onInsertAtIndex)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int p = cells[i];
            if (p.y >= insertAtY) p.y += 1;
            cells[i] = p;
        }

        for (int i = 0; i < cells.Count - 1; i++)
        {
            Vector2Int a = cells[i];
            Vector2Int b = cells[i + 1];
            int dx = b.x - a.x;
            int dy = b.y - a.y;
            if (dx == 0 && Mathf.Abs(dy) == 2)
            {
                Vector2Int mid = new Vector2Int(a.x, a.y + (dy > 0 ? 1 : -1));
                cells.Insert(i + 1, mid);
                onInsertAtIndex?.Invoke(i + 1);
                i++;
            }
        }

        if (isLoop && cells.Count >= 2)
        {
            Vector2Int a = cells[^1];
            Vector2Int b = cells[0];
            int dx = b.x - a.x;
            int dy = b.y - a.y;
            if (dx == 0 && Mathf.Abs(dy) == 2)
            {
                Vector2Int mid = new Vector2Int(a.x, a.y + (dy > 0 ? 1 : -1));
                cells.Add(mid);
                onInsertAtIndex?.Invoke(cells.Count - 1);
            }
        }
    }

    private static void ShiftAndBridgeX(List<Vector2Int> cells, int insertAtX, bool isLoop, System.Action<int> onInsertAtIndex)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int p = cells[i];
            if (p.x >= insertAtX) p.x += 1;
            cells[i] = p;
        }

        for (int i = 0; i < cells.Count - 1; i++)
        {
            Vector2Int a = cells[i];
            Vector2Int b = cells[i + 1];
            int dx = b.x - a.x;
            int dy = b.y - a.y;
            if (dy == 0 && Mathf.Abs(dx) == 2)
            {
                Vector2Int mid = new Vector2Int(a.x + (dx > 0 ? 1 : -1), a.y);
                cells.Insert(i + 1, mid);
                onInsertAtIndex?.Invoke(i + 1);
                i++;
            }
        }

        if (isLoop && cells.Count >= 2)
        {
            Vector2Int a = cells[^1];
            Vector2Int b = cells[0];
            int dx = b.x - a.x;
            int dy = b.y - a.y;
            if (dy == 0 && Mathf.Abs(dx) == 2)
            {
                Vector2Int mid = new Vector2Int(a.x + (dx > 0 ? 1 : -1), a.y);
                cells.Add(mid);
                onInsertAtIndex?.Invoke(cells.Count - 1);
            }
        }
    }

    private static bool BridgeAnyGaps(List<Vector2Int> cells, bool isLoop, System.Action<int> onInsertAtIndex)
    {
        if (cells == null || cells.Count < 2) return false;

        bool changed = false;

        for (int i = 0; i < cells.Count - 1; i++)
        {
            Vector2Int a = cells[i];
            Vector2Int b = cells[i + 1];
            int dx = b.x - a.x;
            int dy = b.y - a.y;

            if (dx == 0 && Mathf.Abs(dy) == 2)
            {
                Vector2Int mid = new Vector2Int(a.x, a.y + (dy > 0 ? 1 : -1));
                cells.Insert(i + 1, mid);
                onInsertAtIndex?.Invoke(i + 1);
                changed = true;
                i++;
                continue;
            }

            if (dy == 0 && Mathf.Abs(dx) == 2)
            {
                Vector2Int mid = new Vector2Int(a.x + (dx > 0 ? 1 : -1), a.y);
                cells.Insert(i + 1, mid);
                onInsertAtIndex?.Invoke(i + 1);
                changed = true;
                i++;
            }
        }

        if (isLoop && cells.Count >= 2)
        {
            Vector2Int a = cells[^1];
            Vector2Int b = cells[0];
            int dx = b.x - a.x;
            int dy = b.y - a.y;

            if (dx == 0 && Mathf.Abs(dy) == 2)
            {
                Vector2Int mid = new Vector2Int(a.x, a.y + (dy > 0 ? 1 : -1));
                cells.Add(mid);
                onInsertAtIndex?.Invoke(cells.Count - 1);
                changed = true;
            }
            else if (dy == 0 && Mathf.Abs(dx) == 2)
            {
                Vector2Int mid = new Vector2Int(a.x + (dx > 0 ? 1 : -1), a.y);
                cells.Add(mid);
                onInsertAtIndex?.Invoke(cells.Count - 1);
                changed = true;
            }
        }

        return changed;
    }

    private static void RemoveRowFromColorLines(List<ColorLine> lines, int removeAtY)
    {
        if (lines == null) return;

        for (int i = 0; i < lines.Count; i++)
        {
            ColorLine line = lines[i];
            if (line == null || line.Cells == null || line.Cells.Count == 0) continue;

            bool syncElementTypes = line.ElementTypes != null && line.ElementTypes.Count == line.Cells.Count;
            for (int j = line.Cells.Count - 1; j >= 0; j--)
            {
                Vector2Int p = line.Cells[j];
                if (p.y == removeAtY)
                {
                    line.Cells.RemoveAt(j);
                    if (syncElementTypes) line.ElementTypes.RemoveAt(j);
                    continue;
                }

                if (p.y > removeAtY)
                {
                    p.y -= 1;
                    line.Cells[j] = p;
                }
            }
        }
    }

    private static void RemoveColumnFromColorLines(List<ColorLine> lines, int removeAtX)
    {
        if (lines == null) return;

        for (int i = 0; i < lines.Count; i++)
        {
            ColorLine line = lines[i];
            if (line == null || line.Cells == null || line.Cells.Count == 0) continue;

            bool syncElementTypes = line.ElementTypes != null && line.ElementTypes.Count == line.Cells.Count;
            for (int j = line.Cells.Count - 1; j >= 0; j--)
            {
                Vector2Int p = line.Cells[j];
                if (p.x == removeAtX)
                {
                    line.Cells.RemoveAt(j);
                    if (syncElementTypes) line.ElementTypes.RemoveAt(j);
                    continue;
                }

                if (p.x > removeAtX)
                {
                    p.x -= 1;
                    line.Cells[j] = p;
                }
            }
        }
    }

    private static void RemoveRowFromElevators(List<ElevatorData> elevators, int removeAtY)
    {
        if (elevators == null) return;
        for (int i = 0; i < elevators.Count; i++)
        {
            ElevatorData e = elevators[i];
            if (e == null) continue;
            if (e.Position.y > removeAtY)
                e.Position = new Vector2Int(e.Position.x, e.Position.y - 1);
            else if (e.Position.y == removeAtY)
                e.Position = new Vector2Int(e.Position.x, Mathf.Max(0, e.Position.y - 1));
            RemoveRowFromColorLines(e.Lines, removeAtY);
        }
    }

    private static void RemoveColumnFromElevators(List<ElevatorData> elevators, int removeAtX)
    {
        if (elevators == null) return;
        for (int i = 0; i < elevators.Count; i++)
        {
            ElevatorData e = elevators[i];
            if (e == null) continue;
            if (e.Position.x > removeAtX)
                e.Position = new Vector2Int(e.Position.x - 1, e.Position.y);
            else if (e.Position.x == removeAtX)
                e.Position = new Vector2Int(Mathf.Max(0, e.Position.x - 1), e.Position.y);
            RemoveColumnFromColorLines(e.Lines, removeAtX);
        }
    }

    private static void RemoveRowFromLineDoors(List<LineDoorData> doors, int removeAtY)
    {
        if (doors == null) return;
        for (int i = 0; i < doors.Count; i++)
        {
            LineDoorData d = doors[i];
            if (d == null) continue;
            if (d.Position.y > removeAtY)
                d.Position = new Vector2Int(d.Position.x, d.Position.y - 1);
            else if (d.Position.y == removeAtY)
                d.Position = new Vector2Int(d.Position.x, Mathf.Max(0, d.Position.y - 1));
            RemoveRowFromColorLines(d.Lines, removeAtY);
        }
    }

    private static void RemoveColumnFromLineDoors(List<LineDoorData> doors, int removeAtX)
    {
        if (doors == null) return;
        for (int i = 0; i < doors.Count; i++)
        {
            LineDoorData d = doors[i];
            if (d == null) continue;
            if (d.Position.x > removeAtX)
                d.Position = new Vector2Int(d.Position.x - 1, d.Position.y);
            else if (d.Position.x == removeAtX)
                d.Position = new Vector2Int(Mathf.Max(0, d.Position.x - 1), d.Position.y);
            RemoveColumnFromColorLines(d.Lines, removeAtX);
        }
    }

    private static void RemoveRowFromConveyor(ConveyorLine conveyor, int removeAtY)
    {
        if (conveyor == null || conveyor.Cells == null || conveyor.Cells.Count == 0) return;

        bool syncTypes = conveyor.Types != null && conveyor.Types.Count == conveyor.Cells.Count;
        bool syncCounters = conveyor.Counters != null && conveyor.Counters.Count == conveyor.Cells.Count;
        bool syncHoles = conveyor.IsHoles != null && conveyor.IsHoles.Count == conveyor.Cells.Count;

        for (int i = conveyor.Cells.Count - 1; i >= 0; i--)
        {
            Vector2Int p = conveyor.Cells[i];
            if (p.y == removeAtY)
            {
                conveyor.Cells.RemoveAt(i);
                if (syncTypes) conveyor.Types.RemoveAt(i);
                if (syncCounters) conveyor.Counters.RemoveAt(i);
                if (syncHoles) conveyor.IsHoles.RemoveAt(i);
                continue;
            }

            if (p.y > removeAtY)
            {
                p.y -= 1;
                conveyor.Cells[i] = p;
            }
        }
    }

    private static void RemoveColumnFromConveyor(ConveyorLine conveyor, int removeAtX)
    {
        if (conveyor == null || conveyor.Cells == null || conveyor.Cells.Count == 0) return;

        bool syncTypes = conveyor.Types != null && conveyor.Types.Count == conveyor.Cells.Count;
        bool syncCounters = conveyor.Counters != null && conveyor.Counters.Count == conveyor.Cells.Count;
        bool syncHoles = conveyor.IsHoles != null && conveyor.IsHoles.Count == conveyor.Cells.Count;

        for (int i = conveyor.Cells.Count - 1; i >= 0; i--)
        {
            Vector2Int p = conveyor.Cells[i];
            if (p.x == removeAtX)
            {
                conveyor.Cells.RemoveAt(i);
                if (syncTypes) conveyor.Types.RemoveAt(i);
                if (syncCounters) conveyor.Counters.RemoveAt(i);
                if (syncHoles) conveyor.IsHoles.RemoveAt(i);
                continue;
            }

            if (p.x > removeAtX)
            {
                p.x -= 1;
                conveyor.Cells[i] = p;
            }
        }
    }

    private static void ShiftConveyorRight(ConveyorLine conveyor)
    {
        if (conveyor == null || conveyor.Cells == null) return;

        for (int i = 0; i < conveyor.Cells.Count; i++)
        {
            Vector2Int p = conveyor.Cells[i];
            p.x += 1;
            conveyor.Cells[i] = p;
        }
    }
}
#endif
