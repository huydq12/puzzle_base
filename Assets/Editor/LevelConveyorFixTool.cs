#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class LevelConveyorFixTool
{
    [MenuItem("Tools/Levels/Fix ConveyorLine From JSON (Selected SO)")]
    private static void FixSelected()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[LevelConveyorFixTool] Stop Play Mode before fixing LevelConfig assets.");
            return;
        }

        LevelConfig[] selected = Selection.GetFiltered<LevelConfig>(SelectionMode.Assets);
        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("[LevelConveyorFixTool] Select one or more LevelConfig assets first (e.g. Assets/SO/Level 139.asset).");
            return;
        }

        int fixedCount = 0;
        int failedCount = 0;

        for (int i = 0; i < selected.Length; i++)
        {
            LevelConfig cfg = selected[i];
            if (cfg == null)
            {
                failedCount++;
                continue;
            }

            if (!TryLoadLevelJson(cfg.Level, out TextAsset jsonAsset, out string jsonPath))
            {
                Debug.LogError($"[LevelConveyorFixTool] Missing JSON for level={cfg.Level}. Expected at `Assets/Levels/Level_{cfg.Level}.json` (or `Assets/Resources/Levels/Level_{cfg.Level}.json`).", cfg);
                failedCount++;
                continue;
            }

            try
            {
                LevelJsonRoot root = JsonUtility.FromJson<LevelJsonRoot>(jsonAsset.text);
                if (root == null)
                {
                    Debug.LogError($"[LevelConveyorFixTool] JsonUtility returned null: {jsonPath}", cfg);
                    failedCount++;
                    continue;
                }

                Bounds2Int bounds = ComputeBounds(root);
                Vector2Int originOffset = bounds.Min;

                ConveyorLine rebuilt = BuildConveyorLine(root, originOffset);
                if (rebuilt == null || rebuilt.Cells == null || rebuilt.Cells.Count == 0)
                {
                    Undo.RecordObject(cfg, "Fix ConveyorLine From JSON");
                    cfg.ConveyorLine = null;
                    EditorUtility.SetDirty(cfg);
                    fixedCount++;
                    Debug.Log($"[LevelConveyorFixTool] level={cfg.Level} cleared ConveyorLine (no nodes).", cfg);
                    continue;
                }

                int columns = Mathf.Max(1, cfg.Columns);
                int rows = Mathf.Max(1, cfg.Rows);
                bool didAutoClose = TryAutoCloseConveyorLoop(rebuilt, columns, rows);
                EnsureMetaAligned(rebuilt);

                Undo.RecordObject(cfg, "Fix ConveyorLine From JSON");
                cfg.ConveyorLine = rebuilt;

                EnsureGridCells(cfg, columns, rows);
                ClearConveyorFlags(cfg, columns, rows);
                MarkConveyorFlags(cfg, columns, rows, rebuilt.Cells);

                EditorUtility.SetDirty(cfg);
                fixedCount++;

                Vector2Int first = rebuilt.Cells[0];
                Vector2Int last = rebuilt.Cells[rebuilt.Cells.Count - 1];
                Debug.Log($"[LevelConveyorFixTool] level={cfg.Level} conveyorNodes={rebuilt.Cells.Count} first=({first.x},{first.y}) last=({last.x},{last.y}) autoClose={didAutoClose}", cfg);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LevelConveyorFixTool] Fix failed for level={cfg.Level}: {e.Message}\n{e.StackTrace}", cfg);
                failedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[LevelConveyorFixTool] Done. Fixed={fixedCount} Failed={failedCount}");
    }

    [MenuItem("Tools/Levels/Nudge ConveyorLine Right (+1 Grid X) (Selected SO)")]
    private static void NudgeSelectedRightOne()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[LevelConveyorFixTool] Stop Play Mode before fixing LevelConfig assets.");
            return;
        }

        LevelConfig[] selected = Selection.GetFiltered<LevelConfig>(SelectionMode.Assets);
        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("[LevelConveyorFixTool] Select one or more LevelConfig assets first (e.g. Assets/SO/Level 139.asset).");
            return;
        }

        int nudged = 0;
        int skipped = 0;

        for (int i = 0; i < selected.Length; i++)
        {
            LevelConfig cfg = selected[i];
            if (cfg == null || cfg.ConveyorLine == null || cfg.ConveyorLine.Cells == null || cfg.ConveyorLine.Cells.Count == 0)
            {
                skipped++;
                continue;
            }

            Undo.RecordObject(cfg, "Nudge ConveyorLine Right (+1 Grid X)");

            int columns = Mathf.Max(1, cfg.Columns);
            int rows = Mathf.Max(1, cfg.Rows);

            List<Vector2Int> cells = cfg.ConveyorLine.Cells;
            int minXAfter = int.MaxValue;
            int maxXAfter = int.MinValue;
            for (int c = 0; c < cells.Count; c++)
            {
                Vector2Int p = cells[c];
                int nx = p.x + 1;
                minXAfter = Mathf.Min(minXAfter, nx);
                maxXAfter = Mathf.Max(maxXAfter, nx);
            }

            // Keep base grid size unchanged: if it would go out of bounds, skip.
            if (minXAfter < 0 || maxXAfter >= columns)
            {
                Debug.LogWarning($"[LevelConveyorFixTool] level={cfg.Level} nudgeRight skipped (would go out of bounds). minXAfter={minXAfter} maxXAfter={maxXAfter} columns={columns}", cfg);
                skipped++;
                continue;
            }

            Undo.RecordObject(cfg, "Nudge ConveyorLine Right (+1 Grid X)");

            ClearConveyorFlags(cfg, columns, rows);

            for (int c = 0; c < cells.Count; c++)
            {
                Vector2Int p = cells[c];
                p.x += 1;
                cells[c] = p;
            }

            EnsureGridCells(cfg, columns, rows);
            MarkConveyorFlags(cfg, columns, rows, cells);
            EditorUtility.SetDirty(cfg);
            nudged++;

            Vector2Int first = cells[0];
            Vector2Int last = cells[cells.Count - 1];
            Debug.Log($"[LevelConveyorFixTool] level={cfg.Level} nudgedRight. nodes={cells.Count} first=({first.x},{first.y}) last=({last.x},{last.y}) rows={rows} cols={columns}", cfg);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[LevelConveyorFixTool] Nudge done. Nudged={nudged} Skipped={skipped}");
    }

    private static bool TryLoadLevelJson(int level, out TextAsset asset, out string path)
    {
        path = $"Assets/Levels/Level_{level}.json";
        asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        if (asset != null) return true;

        path = $"Assets/Resources/Levels/Level_{level}.json";
        asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        return asset != null;
    }

    private static ConveyorLine BuildConveyorLine(LevelJsonRoot root, Vector2Int originOffset)
    {
        if (root == null || root.conveyors == null || root.conveyors.Count == 0) return null;

        LevelJsonConveyor primary = SelectPrimaryConveyor(root.conveyors);
        if (primary == null || primary.conveyorNodes == null || primary.conveyorNodes.Count == 0) return null;

        ConveyorLine line = new ConveyorLine
        {
            Cells = new List<Vector2Int>(),
            Types = new List<int>(),
            Counters = new List<int>(),
            IsHoles = new List<bool>()
        };

        if (root.conveyors.Count > 1)
        {
            Debug.LogWarning(
                $"[LevelConveyorFixTool] Multiple conveyors detected (count={root.conveyors.Count}). LevelConfig supports a single ConveyorLine; fixing only the primary one."
            );
        }

        for (int n = 0; n < primary.conveyorNodes.Count; n++)
        {
            LevelJsonConveyorNode node = primary.conveyorNodes[n];
            if (node == null || node.position == null) continue;
            int x = Mathf.RoundToInt(node.position.x) - originOffset.x;
            int y = Mathf.RoundToInt(node.position.z) - originOffset.y;
            line.Cells.Add(new Vector2Int(x, y));
            line.Types.Add(node.type);
            line.Counters.Add(node.counter);
            line.IsHoles.Add(node.isHole);
        }

        return line;
    }

    private static LevelJsonConveyor SelectPrimaryConveyor(List<LevelJsonConveyor> conveyors)
    {
        if (conveyors == null || conveyors.Count == 0) return null;

        int bestIndex = -1;
        int bestNodeCount = -1;
        int bestMetaCount = -1;

        for (int i = 0; i < conveyors.Count; i++)
        {
            LevelJsonConveyor c = conveyors[i];
            if (c == null || c.conveyorNodes == null || c.conveyorNodes.Count == 0) continue;

            int nodeCount = c.conveyorNodes.Count;
            int metaCount = 0;
            for (int n = 0; n < c.conveyorNodes.Count; n++)
            {
                LevelJsonConveyorNode node = c.conveyorNodes[n];
                if (node == null) continue;
                if (node.isHole || node.type != 0 || node.counter != 0) metaCount++;
            }

            if (metaCount > bestMetaCount || (metaCount == bestMetaCount && nodeCount > bestNodeCount))
            {
                bestMetaCount = metaCount;
                bestNodeCount = nodeCount;
                bestIndex = i;
            }
        }

        if (bestIndex < 0) return conveyors[0];
        return conveyors[bestIndex];
    }

    private static void EnsureMetaAligned(ConveyorLine line)
    {
        if (line == null || line.Cells == null) return;
        int count = line.Cells.Count;
        if (line.Types == null) line.Types = new List<int>();
        if (line.Counters == null) line.Counters = new List<int>();
        if (line.IsHoles == null) line.IsHoles = new List<bool>();

        while (line.Types.Count < count) line.Types.Add(0);
        while (line.Counters.Count < count) line.Counters.Add(0);
        while (line.IsHoles.Count < count) line.IsHoles.Add(false);

        if (line.Types.Count > count) line.Types.RemoveRange(count, line.Types.Count - count);
        if (line.Counters.Count > count) line.Counters.RemoveRange(count, line.Counters.Count - count);
        if (line.IsHoles.Count > count) line.IsHoles.RemoveRange(count, line.IsHoles.Count - count);
    }

    private static void EnsureGridCells(LevelConfig cfg, int columns, int rows)
    {
        if (cfg.Cells != null && cfg.Cells.GetLength(0) == columns && cfg.Cells.GetLength(1) == rows) return;

        GridCellData[,] newCells = new GridCellData[columns, rows];
        if (cfg.Cells != null)
        {
            int curColumns = cfg.Cells.GetLength(0);
            int curRows = cfg.Cells.GetLength(1);
            for (int x = 0; x < Mathf.Min(curColumns, columns); x++)
            {
                for (int y = 0; y < Mathf.Min(curRows, rows); y++)
                {
                    newCells[x, y] = cfg.Cells[x, y];
                }
            }
        }

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (newCells[x, y] == null) newCells[x, y] = new GridCellData();
            }
        }

        cfg.Cells = newCells;
    }

    private static void ClearConveyorFlags(LevelConfig cfg, int columns, int rows)
    {
        if (cfg == null || cfg.Cells == null) return;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                GridCellData cell = cfg.Cells[x, y];
                if (cell != null && cell.CellType == GridCellType.Conveyor)
                    cell.CellType = GridCellType.Normal;
            }
        }
    }

    private static void MarkConveyorFlags(LevelConfig cfg, int columns, int rows, List<Vector2Int> cells)
    {
        if (cfg == null || cfg.Cells == null || cells == null) return;
        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int c = cells[i];
            if (c.x < 0 || c.x >= columns || c.y < 0 || c.y >= rows) continue;
            cfg.Cells[c.x, c.y].CellType = GridCellType.Conveyor;
        }
    }

    private static bool TryAutoCloseConveyorLoop(ConveyorLine line, int columns, int rows)
    {
        if (line == null || line.Cells == null) return false;
        if (line.Cells.Count < 2) return false;

        for (int i = 0; i < line.Cells.Count - 1; i++)
        {
            if (!Are8Neighbors(line.Cells[i], line.Cells[i + 1]))
                return false;
        }

        Vector2Int first = line.Cells[0];
        Vector2Int last = line.Cells[line.Cells.Count - 1];
        if (Are8Neighbors(last, first)) return false;

        HashSet<Vector2Int> existing = new HashSet<Vector2Int>(line.Cells);
        List<Vector2Int> toAppend = BuildChebyshevPath(last, first);
        if (toAppend.Count == 0) return false;

        for (int i = 0; i < toAppend.Count; i++)
        {
            Vector2Int p = toAppend[i];
            if (p.x < 0 || p.x >= columns || p.y < 0 || p.y >= rows) return false;
            if (existing.Contains(p)) return false;
        }

        for (int i = 0; i < toAppend.Count; i++)
        {
            Vector2Int p = toAppend[i];
            line.Cells.Add(p);
            line.Types.Add(0);
            line.Counters.Add(0);
            line.IsHoles.Add(false);
        }

        return true;
    }

    private static bool Are8Neighbors(Vector2Int a, Vector2Int b)
    {
        Vector2Int d = b - a;
        int ax = Mathf.Abs(d.x);
        int ay = Mathf.Abs(d.y);
        return Mathf.Max(ax, ay) == 1;
    }

    private static List<Vector2Int> BuildChebyshevPath(Vector2Int from, Vector2Int to)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int current = from;

        while (current != to)
        {
            int sx = Math.Sign(to.x - current.x);
            int sy = Math.Sign(to.y - current.y);
            current = new Vector2Int(current.x + sx, current.y + sy);
            if (current == to) break;
            path.Add(current);
        }

        return path;
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
                ConsiderArrow(root.arrows[i]);
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
    private class LevelJsonRoot
    {
        public List<LevelJsonArrow> arrows;
        public List<LevelJsonShooter> shooters;
        public List<LevelJsonConveyor> conveyors;
        public List<LevelJsonElementData> elementData;
    }

    [Serializable]
    private class LevelJsonArrow
    {
        public List<LevelJsonUnitPos> unitPositions;
    }

    [Serializable]
    private class LevelJsonUnitPos
    {
        public int x;
        public int y;
        public int elementType;
    }

    [Serializable]
    private class LevelJsonShooter
    {
        public LevelJsonFloat2 position;
        public List<LevelJsonArrow> arrowData;
    }

    [Serializable]
    private class LevelJsonElementData
    {
        public List<LevelJsonArrow> arrowData;
    }

    [Serializable]
    private class LevelJsonFloat2
    {
        public float x;
        public float y;
    }

    [Serializable]
    private class LevelJsonConveyor
    {
        public List<LevelJsonConveyorNode> conveyorNodes;
    }

    [Serializable]
    private class LevelJsonConveyorNode
    {
        public LevelJsonVector3 position;
        public bool isHole;
        public int counter;
        public int type;
    }

    [Serializable]
    private class LevelJsonVector3
    {
        public float x;
        public float y;
        public float z;
    }
}
#endif
