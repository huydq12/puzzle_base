#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class LevelsSecureExportTool
{
    [MenuItem("Tools/Levels/Export Secure Levels (Encrypted StreamingAssets)")]
    private static void ExportAllLevelsEncryptedStreamingAssets()
    {
        const string sourceFolder = "Assets/SO";
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
            .OrderBy(a => a.config != null ? a.config.Level : int.MaxValue)
            .ThenBy(a => a.assetPath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Guard against duplicate Level IDs (will overwrite in LevelDatabase cache at runtime).
        var duplicates = assets
            .Where(a => a.config != null && a.config.Level > 0)
            .GroupBy(a => a.config.Level)
            .Where(g => g.Count() > 1)
            .OrderBy(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            foreach (var g in duplicates)
            {
                string joined = string.Join(", ", g.Select(x => x.assetPath));
                Debug.LogError($"[LevelsSecureExportTool] Duplicate Level={g.Key}: {joined}");
            }

            int choice = EditorUtility.DisplayDialogComplex(
                "Duplicate Level IDs Found",
                "There are duplicate `LevelConfig.Level` values under Assets/SO. Exporting will cause levels.dat to contain duplicates, and runtime will load the last one (wrong level data).\n\nFix duplicates first (Tools/Levels/Audit Level IDs), or export anyway?",
                "Export Anyway",
                "Cancel",
                "Audit Tool"
            );

            if (choice == 2)
            {
                EditorApplication.ExecuteMenuItem("Tools/Levels/Audit Level IDs (Assets/SO)");
                return;
            }

            if (choice != 0)
                return;
        }

        var root = new LevelsExportRootDto { levels = new List<LevelConfigDto>(assets.Count) };
        for (int i = 0; i < assets.Count; i++)
        {
            var config = assets[i].config;
            if (config == null) continue;
            root.levels.Add(ToDto(config));
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

    [MenuItem("Tools/Levels/Export Secure Levels (JSON)...")]
    private static void ExportAllLevelsJson()
    {
        const string sourceFolder = "Assets/SO";

        string outputPath = EditorUtility.SaveFilePanel(
            "Export Secure Levels to JSON",
            Application.dataPath,
            "levels_export_secure.json",
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
            .OrderBy(a => a.config != null ? a.config.Level : int.MaxValue)
            .ThenBy(a => a.assetPath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var root = new LevelsExportRootDto { levels = new List<LevelConfigDto>(assets.Count) };
        for (int i = 0; i < assets.Count; i++)
        {
            var config = assets[i].config;
            if (config == null) continue;
            root.levels.Add(ToDto(config));
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

    private static LevelConfigDto ToDto(LevelConfig config)
    {
        int rows = Mathf.Max(1, config.Rows);
        int columns = Mathf.Max(1, config.Columns);

        var dto = new LevelConfigDto
        {
            level = config.Level,
            rows = rows,
            columns = columns,
            shooters = new List<GateDataDto>(),
            cellTypes = new List<int>(rows * columns),
            colorLines = new List<ColorLineDto>(),
            elevators = new List<ElevatorDataDto>(),
            lineDoors = new List<LineDoorDataDto>(),
            conveyorLine = null,
            conveyorLines = new List<ConveyorLineDto>(),
            camera = new CameraSetupDto
            {
                enabled = config.Camera.Enabled,
                minOrthoSize = config.Camera.MinOrthoSize,
                padding = config.Camera.Padding,
                usePosition = config.Camera.UsePosition,
                position = new Vec3Dto { x = config.Camera.Position.x, y = config.Camera.Position.y, z = config.Camera.Position.z },
                useOrthographicSize = config.Camera.UseOrthographicSize,
                orthographicSize = config.Camera.OrthographicSize
            }
        };

        static ShooterDataDto ToShooterDto(ShooterData shooter)
        {
            return new ShooterDataDto
            {
                color = (int)shooter.Color,
                counter = shooter.Counter,
                type = shooter.Type,
                tieID = shooter.TieID
            };
        }

        if (config.Gates != null)
        {
            for (int i = 0; i < config.Gates.Count; i++)
            {
                var g = config.Gates[i];
                if (g == null) continue;

                var gDto = new GateDataDto
                {
                    position = new Vec3Dto { x = g.Position.x, y = g.Position.y, z = g.Position.z },
                    direction = g.Direction,
                    counter = g.Counter,
                    elementType = g.ElementType,
                    shooters = new List<ShooterDataDto>()
                };

                if (g.Shooters != null)
                {
                    for (int s = 0; s < g.Shooters.Count; s++)
                    {
                        var shooter = g.Shooters[s];
                        if (shooter == null) continue;
                        gDto.shooters.Add(ToShooterDto(shooter));
                    }
                }

                dto.shooters.Add(gDto);
            }
        }

        if (config.GatesDouble != null)
        {
            for (int i = 0; i < config.GatesDouble.Count; i++)
            {
                var g = config.GatesDouble[i];
                if (g == null) continue;

                var gDto = new GateDataDoubleDto
                {
                    position = new Vec3Dto { x = g.Position.x, y = g.Position.y, z = g.Position.z },
                    direction = g.Direction,
                    counter = g.Counter,
                    elementType = g.ElementType,
                    shootersDouble = new List<ShooterDataDoubleDto>()
                };

                if (g.ShootersDouble != null)
                {
                    for (int e = 0; e < g.ShootersDouble.Count; e++)
                    {
                        var entry = g.ShootersDouble[e];
                        if (entry == null) continue;

                        var entryDto = new ShooterDataDoubleDto();
                        if (entry.ShootersLeft != null)
                            for (int s = 0; s < entry.ShootersLeft.Count; s++)
                                if (entry.ShootersLeft[s] != null) entryDto.shootersLeft.Add(ToShooterDto(entry.ShootersLeft[s]));
                        if (entry.ShootersRight != null)
                            for (int s = 0; s < entry.ShootersRight.Count; s++)
                                if (entry.ShootersRight[s] != null) entryDto.shootersRight.Add(ToShooterDto(entry.ShootersRight[s]));

                        gDto.shootersDouble.Add(entryDto);
                    }
                }

                dto.gatesDouble.Add(gDto);
            }
        }

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                int cellType = 0;
                if (config.Cells != null &&
                    x >= 0 && x < config.Cells.GetLength(0) &&
                    y >= 0 && y < config.Cells.GetLength(1) &&
                    config.Cells[x, y] != null)
                {
                    cellType = (int)config.Cells[x, y].CellType;
                }
                dto.cellTypes.Add(cellType);
            }
        }

        if (config.ColorLines != null)
        {
            for (int i = 0; i < config.ColorLines.Count; i++)
            {
                var line = config.ColorLines[i];
                if (line == null) continue;

                var lineDto = new ColorLineDto
                {
                    color = (int)line.Color,
                    counter = line.Counter,
                    cells = new List<Vec2IntDto>(),
                    elementTypes = line.ElementTypes != null ? new List<int>(line.ElementTypes) : new List<int>()
                };

                if (line.Cells != null)
                {
                    for (int c = 0; c < line.Cells.Count; c++)
                    {
                        var cell = line.Cells[c];
                        lineDto.cells.Add(new Vec2IntDto { x = cell.x, y = cell.y });
                    }
                }

                dto.colorLines.Add(lineDto);
            }
        }

        if (config.Elevators != null)
        {
            for (int i = 0; i < config.Elevators.Count; i++)
            {
                var e = config.Elevators[i];
                if (e == null) continue;

                var eDto = new ElevatorDataDto
                {
                    position = new Vec2IntDto { x = e.Position.x, y = e.Position.y },
                    size = new Vec2IntDto { x = e.Size.x, y = e.Size.y },
                    lines = new List<ColorLineDto>()
                };

                if (e.Lines != null)
                {
                    for (int l = 0; l < e.Lines.Count; l++)
                    {
                        var line = e.Lines[l];
                        if (line == null) continue;

                        var lineDto = new ColorLineDto
                        {
                            color = (int)line.Color,
                            counter = line.Counter,
                            cells = new List<Vec2IntDto>(),
                            elementTypes = line.ElementTypes != null ? new List<int>(line.ElementTypes) : new List<int>()
                        };

                        if (line.Cells != null)
                        {
                            for (int c = 0; c < line.Cells.Count; c++)
                            {
                                var cell = line.Cells[c];
                                lineDto.cells.Add(new Vec2IntDto { x = cell.x, y = cell.y });
                            }
                        }

                        eDto.lines.Add(lineDto);
                    }
                }

                dto.elevators.Add(eDto);
            }
        }

        if (config.LineDoors != null)
        {
            for (int i = 0; i < config.LineDoors.Count; i++)
            {
                var d = config.LineDoors[i];
                if (d == null) continue;

                var dDto = new LineDoorDataDto
                {
                    position = new Vec2IntDto { x = d.Position.x, y = d.Position.y },
                    size = new Vec2IntDto { x = d.Size.x, y = d.Size.y },
                    direction = d.Direction,
                    color = (int)d.Color,
                    counter = d.Counter,
                    lines = new List<ColorLineDto>()
                };

                if (d.Lines != null)
                {
                    for (int l = 0; l < d.Lines.Count; l++)
                    {
                        var line = d.Lines[l];
                        if (line == null) continue;

                        var lineDto = new ColorLineDto
                        {
                            color = (int)line.Color,
                            counter = line.Counter,
                            cells = new List<Vec2IntDto>(),
                            elementTypes = line.ElementTypes != null ? new List<int>(line.ElementTypes) : new List<int>()
                        };

                        if (line.Cells != null)
                        {
                            for (int c = 0; c < line.Cells.Count; c++)
                            {
                                var cell = line.Cells[c];
                                lineDto.cells.Add(new Vec2IntDto { x = cell.x, y = cell.y });
                            }
                        }

                        dDto.lines.Add(lineDto);
                    }
                }

                dto.lineDoors.Add(dDto);
            }
        }

        List<ConveyorLine> conveyorLines = config.GetConveyorLines();
        for (int conveyorIndex = 0; conveyorIndex < conveyorLines.Count; conveyorIndex++)
        {
            var cl = conveyorLines[conveyorIndex];
            if (cl == null || cl.Cells == null || cl.Cells.Count == 0) continue;

            var clDto = new ConveyorLineDto
            {
                cells = new List<Vec2IntDto>(),
                types = cl.Types != null ? new List<int>(cl.Types) : new List<int>(),
                counters = cl.Counters != null ? new List<int>(cl.Counters) : new List<int>(),
                isHoles = cl.IsHoles != null ? new List<bool>(cl.IsHoles) : new List<bool>()
            };

            for (int i = 0; i < cl.Cells.Count; i++)
            {
                var cell = cl.Cells[i];
                clDto.cells.Add(new Vec2IntDto { x = cell.x, y = cell.y });
            }

            dto.conveyorLines.Add(clDto);
        }

        if (dto.conveyorLines.Count > 0)
            dto.conveyorLine = dto.conveyorLines[0];

        return dto;
    }
}
#endif
