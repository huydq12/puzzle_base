#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class LevelsExportTool
{
    [Serializable]
    private class ExportRoot
    {
        public string sourceFolder;
        public string exportedAtUtc;
        public List<LevelConfigExport> levels;
    }

    [Serializable]
    private class LevelConfigExport
    {
        public string assetPath;
        public int level;
        public int rows;
        public int columns;
        public int totalSlot;
        public List<LevelGoalDataExport> goals;
        public List<ObjectColor> containers;
        public List<GridColumnExport> grid;
    }

    [Serializable]
    private class LevelGoalDataExport
    {
        public LevelGoalType type;
        public int targetCount;
        public ObjectColor targetColor;
    }

    [Serializable]
    private class GridColumnExport
    {
        public List<GridCellDataExport> cells;
    }

    [Serializable]
    private class GridCellDataExport
    {
        public bool isEmpty;
        public CellElementType elementType;
        public HexEdge gateDirection;
        public List<GateWaveDataExport> gateWaves;
        public int iceHitPoints;
        public int screwHitPoints;
        public int lockItemCount;
        public ObjectColor lockItemColor;
        public List<ColorDataExport> colors;
    }

    [Serializable]
    private class GateWaveDataExport
    {
        public List<ObjectColor> colors;
    }

    [Serializable]
    private class ColorDataExport
    {
        public ObjectColor color;
    }

    [MenuItem("Tools/Levels/Export All Levels (JSON)...")]
    private static void ExportAllLevelsJson()
    {
        const string sourceFolder = "Assets/Levels";

        string outputPath = EditorUtility.SaveFilePanel(
            "Export Levels to JSON",
            Application.dataPath,
            "levels_export.json",
            "json"
        );

        if (string.IsNullOrEmpty(outputPath)) return;

        string[] guids = AssetDatabase.FindAssets("t:LevelConfig", new[] { sourceFolder });
        var assets = new List<(string assetPath, LevelConfig config)>();

        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            var config = AssetDatabase.LoadAssetAtPath<LevelConfig>(assetPath);
            if (config == null) continue;
            assets.Add((assetPath, config));
        }

        assets = assets
            .OrderBy(a => a.config.GetLevel())
            .ThenBy(a => a.assetPath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var root = new ExportRoot
        {
            sourceFolder = sourceFolder,
            exportedAtUtc = DateTime.UtcNow.ToString("O"),
            levels = new List<LevelConfigExport>(assets.Count)
        };

        for (int i = 0; i < assets.Count; i++)
        {
            var item = assets[i];
            root.levels.Add(ToExport(item.assetPath, item.config));
        }

        string json = JsonUtility.ToJson(root, true);
        File.WriteAllText(outputPath, json);

        string projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string fullOutput = Path.GetFullPath(outputPath);
        if (fullOutput.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
        {
            AssetDatabase.Refresh();
        }

        EditorUtility.RevealInFinder(outputPath);
    }

    [MenuItem("Tools/Levels/Export All Levels (Encrypted StreamingAssets)")]
    private static void ExportAllLevelsEncryptedStreamingAssets()
    {
        const string sourceFolder = "Assets/Levels";
        const string streamingAssetsFolder = "Assets/StreamingAssets";
        const string fileName = "levels.dat";

        string[] guids = AssetDatabase.FindAssets("t:LevelConfig", new[] { sourceFolder });
        var assets = new List<(string assetPath, LevelConfig config)>();

        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            var config = AssetDatabase.LoadAssetAtPath<LevelConfig>(assetPath);
            if (config == null) continue;
            assets.Add((assetPath, config));
        }

        assets = assets
            .OrderBy(a => a.config.GetLevel())
            .ThenBy(a => a.assetPath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var root = new LevelsExportRootDto { levels = new List<LevelConfigDto>(assets.Count) };
        for (int i = 0; i < assets.Count; i++)
        {
            root.levels.Add(ToDto(assets[i].config));
        }

        string json = JsonUtility.ToJson(root, false);
        byte[] plaintext = System.Text.Encoding.UTF8.GetBytes(json);
        byte[] encrypted = LevelCrypto.EncryptAndSign(plaintext);

        if (!AssetDatabase.IsValidFolder(streamingAssetsFolder))
        {
            AssetDatabase.CreateFolder("Assets", "StreamingAssets");
        }

        string outputPath = Path.Combine(streamingAssetsFolder, fileName);
        File.WriteAllBytes(outputPath, encrypted);
        AssetDatabase.Refresh();
        EditorUtility.RevealInFinder(outputPath);
    }

    [MenuItem("Tools/Levels/Export Each Level (JSON)...")]
    private static void ExportEachLevelJson()
    {
        const string sourceFolder = "Assets/Levels";

        string outputFolder = EditorUtility.OpenFolderPanel(
            "Export Levels to Folder",
            Application.dataPath,
            ""
        );

        if (string.IsNullOrEmpty(outputFolder)) return;

        string[] guids = AssetDatabase.FindAssets("t:LevelConfig", new[] { sourceFolder });
        var assets = new List<(string assetPath, LevelConfig config)>();

        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            var config = AssetDatabase.LoadAssetAtPath<LevelConfig>(assetPath);
            if (config == null) continue;
            assets.Add((assetPath, config));
        }

        assets = assets
            .OrderBy(a => a.config.GetLevel())
            .ThenBy(a => a.assetPath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var usedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < assets.Count; i++)
        {
            var item = assets[i];
            var export = ToExport(item.assetPath, item.config);
            string json = JsonUtility.ToJson(export, true);

            string baseName = $"Level_{export.level}";
            string filePath = Path.Combine(outputFolder, baseName + ".json");
            int suffix = 2;
            while (usedPaths.Contains(filePath) || File.Exists(filePath))
            {
                filePath = Path.Combine(outputFolder, baseName + "_" + suffix + ".json");
                suffix++;
            }

            usedPaths.Add(filePath);
            File.WriteAllText(filePath, json);
        }

        string projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string fullOutputFolder = Path.GetFullPath(outputFolder);
        if (fullOutputFolder.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
        {
            AssetDatabase.Refresh();
        }

        EditorUtility.RevealInFinder(outputFolder);
    }

    [MenuItem("Tools/Levels/Move Levels From Resources To Assets/Levels")]
    private static void MoveLevelsFromResourcesToAssetsLevels()
    {
        const string sourceFolder = "Assets/Resources/Levels";
        const string destFolder = "Assets/Levels";

        if (!AssetDatabase.IsValidFolder(destFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Levels");
        }

        string[] guids = AssetDatabase.FindAssets("t:LevelConfig", new[] { sourceFolder });
        if (guids == null || guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Move Levels", "No LevelConfig found in Assets/Resources/Levels", "OK");
            return;
        }

        int moved = 0;
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (string.IsNullOrEmpty(assetPath)) continue;

            string fileName = Path.GetFileName(assetPath);
            if (string.IsNullOrEmpty(fileName)) continue;

            string destPath = Path.Combine(destFolder, fileName).Replace("\\\\", "/");
            string err = AssetDatabase.MoveAsset(assetPath, destPath);
            if (string.IsNullOrEmpty(err))
            {
                moved += 1;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Move Levels", $"Moved: {moved}/{guids.Length}\nDestination: {destFolder}", "OK");
    }

    private static LevelConfigExport ToExport(string assetPath, LevelConfig cfg)
    {
        var containers = cfg.GetContainers();
        var e = new LevelConfigExport
        {
            assetPath = assetPath,
            level = cfg.GetLevel(),
            rows = cfg.GetRows(),
            columns = cfg.GetColumns(),
            totalSlot = cfg.TotalSlot,
            goals = cfg.Goals == null ? new List<LevelGoalDataExport>() : cfg.Goals.Select(g => new LevelGoalDataExport
            {
                type = g.Type,
                targetCount = g.TargetCount,
                targetColor = g.TargetColor
            }).ToList(),
            containers = containers == null ? new List<ObjectColor>() : new List<ObjectColor>(containers),
            grid = ExportGrid(cfg)
        };

        return e;
    }

    private static LevelConfigDto ToDto(LevelConfig cfg)
    {
        var containers = cfg.GetContainers();
        var dto = new LevelConfigDto
        {
            level = cfg.GetLevel(),
            rows = cfg.GetRows(),
            columns = cfg.GetColumns(),
            totalSlot = cfg.TotalSlot,
            goals = cfg.Goals == null ? new List<LevelGoalDataDto>() : cfg.Goals.Select(g => new LevelGoalDataDto
            {
                type = g.Type,
                targetCount = g.TargetCount,
                targetColor = g.TargetColor
            }).ToList(),
            containers = containers == null ? new List<ObjectColor>() : new List<ObjectColor>(containers),
            grid = ExportGridDto(cfg)
        };

        return dto;
    }

    private static List<GridColumnDto> ExportGridDto(LevelConfig cfg)
    {
        if (cfg.GridCellData == null) return new List<GridColumnDto>();

        int cols = cfg.GridCellData.GetLength(0);
        int rows = cfg.GridCellData.GetLength(1);

        var result = new List<GridColumnDto>(cols);
        for (int col = 0; col < cols; col++)
        {
            var colExport = new GridColumnDto { cells = new List<GridCellDataDto>(rows) };
            for (int row = 0; row < rows; row++)
            {
                var cell = cfg.GridCellData[col, row];
                colExport.cells.Add(ToCellDto(cell));
            }
            result.Add(colExport);
        }

        return result;
    }

    private static GridCellDataDto ToCellDto(GridCellData cell)
    {
        if (cell == null)
        {
            return new GridCellDataDto
            {
                isEmpty = true,
                colors = new List<ObjectColor>(),
                gateWaves = new List<GateWaveDataDto>()
            };
        }

        return new GridCellDataDto
        {
            isEmpty = cell.IsEmpty,
            elementType = cell.ElementType,
            gateDirection = cell.GateDirection,
            gateWaves = cell.GateWaves == null ? new List<GateWaveDataDto>() : cell.GateWaves.Select(w => new GateWaveDataDto
            {
                colors = w == null || w.Colors == null ? new List<ObjectColor>() : new List<ObjectColor>(w.Colors)
            }).ToList(),
            iceHitPoints = cell.IceHitPoints,
            screwHitPoints = cell.ScrewHitPoints,
            lockItemCount = cell.LockItemCount,
            lockItemColor = cell.LockItemColor,
            colors = cell.Colors == null ? new List<ObjectColor>() : cell.Colors.Select(c => c == null ? default : c.Color).ToList()
        };
    }

    private static List<GridColumnExport> ExportGrid(LevelConfig cfg)
    {
        if (cfg.GridCellData == null) return new List<GridColumnExport>();

        int cols = cfg.GridCellData.GetLength(0);
        int rows = cfg.GridCellData.GetLength(1);

        var result = new List<GridColumnExport>(cols);
        for (int col = 0; col < cols; col++)
        {
            var colExport = new GridColumnExport { cells = new List<GridCellDataExport>(rows) };
            for (int row = 0; row < rows; row++)
            {
                var cell = cfg.GridCellData[col, row];
                colExport.cells.Add(ToCellExport(cell));
            }
            result.Add(colExport);
        }

        return result;
    }

    private static GridCellDataExport ToCellExport(GridCellData cell)
    {
        if (cell == null)
        {
            return new GridCellDataExport
            {
                isEmpty = true,
                colors = new List<ColorDataExport>(),
                gateWaves = new List<GateWaveDataExport>()
            };
        }

        return new GridCellDataExport
        {
            isEmpty = cell.IsEmpty,
            elementType = cell.ElementType,
            gateDirection = cell.GateDirection,
            gateWaves = cell.GateWaves == null ? new List<GateWaveDataExport>() : cell.GateWaves.Select(w => new GateWaveDataExport
            {
                colors = w == null || w.Colors == null ? new List<ObjectColor>() : new List<ObjectColor>(w.Colors)
            }).ToList(),
            iceHitPoints = cell.IceHitPoints,
            screwHitPoints = cell.ScrewHitPoints,
            lockItemCount = cell.LockItemCount,
            lockItemColor = cell.LockItemColor,
            colors = cell.Colors == null ? new List<ColorDataExport>() : cell.Colors.Select(c => new ColorDataExport
            {
                color = c == null ? default : c.Color
            }).ToList()
        };
    }
}

#endif
