using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class LevelDatabase
{
    private const string FileName = "levels.dat";

    private static bool _loadAttempted;
    private static readonly Dictionary<int, LevelConfig> _cache = new();

    public static IEnumerator LoadLevelAsync(int level, Action<LevelConfig> onLoaded)
    {
        if (onLoaded == null) yield break;

        if (_cache.TryGetValue(level, out var cached) && cached != null)
        {
            onLoaded(cached);
            yield break;
        }

        if (!_loadAttempted)
        {
            yield return LoadAllAsync();
        }

        if (_cache.TryGetValue(level, out var cfg) && cfg != null)
        {
            onLoaded(cfg);
        }
        else
        {
            onLoaded(null);
        }
    }

    private static IEnumerator LoadAllAsync()
    {
        _loadAttempted = true;

        string path = Path.Combine(Application.streamingAssetsPath, FileName);
        byte[] bytes = null;

        if (path.Contains("://") || path.Contains(":\\\\"))
        {
            using var req = UnityWebRequest.Get(path);
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                yield break;
            }

            bytes = req.downloadHandler.data;
        }
        else
        {
            if (!File.Exists(path)) yield break;
            bytes = File.ReadAllBytes(path);
        }

        if (bytes == null || bytes.Length == 0) yield break;

        if (!LevelCrypto.TryVerifyAndDecrypt(bytes, out var plaintext) || plaintext == null || plaintext.Length == 0)
        {
            yield break;
        }

        string json = Encoding.UTF8.GetString(plaintext);
        var root = JsonUtility.FromJson<LevelsExportRootDto>(json);
        if (root == null || root.levels == null) yield break;

        for (int i = 0; i < root.levels.Count; i++)
        {
            var dto = root.levels[i];
            if (dto == null) continue;
            if (dto.level <= 0) continue;
            if (dto.rows <= 0 || dto.columns <= 0) continue;
            int expectedCells = dto.rows * dto.columns;
            if (dto.cellTypes == null || dto.cellTypes.Count != expectedCells) continue;
            _cache[dto.level] = BuildLevelConfig(dto);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[LevelDatabase] Secure levels loaded. jsonLevels={root.levels.Count} cached={_cache.Count} file={path}");
#endif
    }

    private static LevelConfig BuildLevelConfig(LevelConfigDto dto)
    {
        var cfg = ScriptableObject.CreateInstance<LevelConfig>();

        cfg.Level = dto.level;
        cfg.Rows = Mathf.Max(1, dto.rows);
        cfg.Columns = Mathf.Max(1, dto.columns);

        cfg.Gates = new List<GateData>();
        cfg.GatesDouble = null;
        if (dto.shooters != null)
        {
            for (int i = 0; i < dto.shooters.Count; i++)
            {
                var g = dto.shooters[i];
                if (g == null) continue;
                var gate = new GateData
                {
                    Position = new Vector3(g.position.x, g.position.y, g.position.z),
                    Direction = g.direction,
                    Counter = g.counter,
                    ElementType = g.elementType,
                    Shooters = new List<ShooterData>()
                };

                if (g.shooters != null)
                {
                    for (int s = 0; s < g.shooters.Count; s++)
                    {
                        var shooter = g.shooters[s];
                        if (shooter == null) continue;
                        gate.Shooters.Add(new ShooterData
                        {
                            Color = (ObjectColor)shooter.color,
                            Counter = shooter.counter,
                            Type = shooter.type,
                            TieID = shooter.tieID
                        });
                    }
                }

                cfg.Gates.Add(gate);
            }
        }

        if (dto.gatesDouble != null && dto.gatesDouble.Count > 0)
        {
            cfg.GatesDouble = new List<GateDataDouble>(dto.gatesDouble.Count);
            for (int i = 0; i < dto.gatesDouble.Count; i++)
            {
                GateDataDoubleDto g = dto.gatesDouble[i];
                if (g == null) continue;

                var gate = new GateDataDouble
                {
                    Position = new Vector3(g.position.x, g.position.y, g.position.z),
                    Direction = g.direction,
                    Counter = g.counter,
                    ElementType = g.elementType,
                    ShootersDouble = new List<ShooterDataDouble>()
                };

                if (g.shootersDouble != null)
                {
                    for (int e = 0; e < g.shootersDouble.Count; e++)
                    {
                        ShooterDataDoubleDto entry = g.shootersDouble[e];
                        if (entry == null) continue;

                        var runtimeEntry = new ShooterDataDouble
                        {
                            ShootersLeft = new List<ShooterData>(),
                            ShootersRight = new List<ShooterData>()
                        };

                        if (entry.shootersLeft != null)
                        {
                            for (int s = 0; s < entry.shootersLeft.Count; s++)
                            {
                                ShooterDataDto shooter = entry.shootersLeft[s];
                                if (shooter == null) continue;
                                runtimeEntry.ShootersLeft.Add(new ShooterData
                                {
                                    Color = (ObjectColor)shooter.color,
                                    Counter = shooter.counter,
                                    Type = shooter.type,
                                    TieID = shooter.tieID
                                });
                            }
                        }

                        if (entry.shootersRight != null)
                        {
                            for (int s = 0; s < entry.shootersRight.Count; s++)
                            {
                                ShooterDataDto shooter = entry.shootersRight[s];
                                if (shooter == null) continue;
                                runtimeEntry.ShootersRight.Add(new ShooterData
                                {
                                    Color = (ObjectColor)shooter.color,
                                    Counter = shooter.counter,
                                    Type = shooter.type,
                                    TieID = shooter.tieID
                                });
                            }
                        }

                        gate.ShootersDouble.Add(runtimeEntry);
                    }
                }

                cfg.GatesDouble.Add(gate);
            }
        }

        int cols = cfg.Columns;
        int rows = cfg.Rows;

        cfg.Cells = new GridCellData[cols, rows];
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                int index = x + y * cols;
                int cellTypeInt = (dto.cellTypes != null && index < dto.cellTypes.Count) ? dto.cellTypes[index] : 0;
                cfg.Cells[x, y] = new GridCellData { CellType = (GridCellType)cellTypeInt };
            }
        }

        cfg.ColorLines = new List<ColorLine>();
        if (dto.colorLines != null)
        {
            for (int i = 0; i < dto.colorLines.Count; i++)
            {
                var lineDto = dto.colorLines[i];
                if (lineDto == null) continue;

                var line = new ColorLine
                {
                    Color = (ObjectColor)lineDto.color,
                    Cells = new List<Vector2Int>(),
                    ElementTypes = lineDto.elementTypes != null ? new List<int>(lineDto.elementTypes) : new List<int>(),
                    Counter = lineDto.counter
                };

                if (lineDto.cells != null)
                {
                    for (int c = 0; c < lineDto.cells.Count; c++)
                    {
                        var cell = lineDto.cells[c];
                        line.Cells.Add(new Vector2Int(cell.x, cell.y));
                    }
                }

                cfg.ColorLines.Add(line);
            }
        }

        cfg.Elevators = new List<ElevatorData>();
        if (dto.elevators != null)
        {
            for (int i = 0; i < dto.elevators.Count; i++)
            {
                var e = dto.elevators[i];
                if (e == null) continue;

                var elevator = new ElevatorData
                {
                    Position = new Vector2Int(e.position.x, e.position.y),
                    Size = new Vector2Int(e.size.x, e.size.y),
                    Lines = new List<ColorLine>()
                };

                if (e.lines != null)
                {
                    for (int l = 0; l < e.lines.Count; l++)
                    {
                        var lineDto = e.lines[l];
                        if (lineDto == null) continue;

                        var line = new ColorLine
                        {
                            Color = (ObjectColor)lineDto.color,
                            Cells = new List<Vector2Int>(),
                            ElementTypes = lineDto.elementTypes != null ? new List<int>(lineDto.elementTypes) : new List<int>(),
                            Counter = lineDto.counter
                        };

                        if (lineDto.cells != null)
                        {
                            for (int c = 0; c < lineDto.cells.Count; c++)
                            {
                                var cell = lineDto.cells[c];
                                line.Cells.Add(new Vector2Int(cell.x, cell.y));
                            }
                        }

                        elevator.Lines.Add(line);
                    }
                }

                cfg.Elevators.Add(elevator);
            }
        }

        cfg.LineDoors = new List<LineDoorData>();
        if (dto.lineDoors != null)
        {
            for (int i = 0; i < dto.lineDoors.Count; i++)
            {
                var d = dto.lineDoors[i];
                if (d == null) continue;

                var door = new LineDoorData
                {
                    Position = new Vector2Int(d.position.x, d.position.y),
                    Size = new Vector2Int(d.size.x, d.size.y),
                    Direction = d.direction,
                    Color = (ObjectColor)d.color,
                    Counter = d.counter,
                    Lines = new List<ColorLine>()
                };

                if (d.lines != null)
                {
                    for (int l = 0; l < d.lines.Count; l++)
                    {
                        var lineDto = d.lines[l];
                        if (lineDto == null) continue;

                        var line = new ColorLine
                        {
                            Color = (ObjectColor)lineDto.color,
                            Cells = new List<Vector2Int>(),
                            ElementTypes = lineDto.elementTypes != null ? new List<int>(lineDto.elementTypes) : new List<int>(),
                            Counter = lineDto.counter
                        };

                        if (lineDto.cells != null)
                        {
                            for (int c = 0; c < lineDto.cells.Count; c++)
                            {
                                var cell = lineDto.cells[c];
                                line.Cells.Add(new Vector2Int(cell.x, cell.y));
                            }
                        }

                        door.Lines.Add(line);
                    }
                }

                cfg.LineDoors.Add(door);
            }
        }

        if (dto.conveyorLine != null)
        {
            var cl = new ConveyorLine
            {
                Cells = new List<Vector2Int>(),
                Types = dto.conveyorLine.types != null ? new List<int>(dto.conveyorLine.types) : new List<int>(),
                Counters = dto.conveyorLine.counters != null ? new List<int>(dto.conveyorLine.counters) : new List<int>(),
                IsHoles = dto.conveyorLine.isHoles != null ? new List<bool>(dto.conveyorLine.isHoles) : new List<bool>()
            };

            if (dto.conveyorLine.cells != null)
            {
                for (int i = 0; i < dto.conveyorLine.cells.Count; i++)
                {
                    var cell = dto.conveyorLine.cells[i];
                    cl.Cells.Add(new Vector2Int(cell.x, cell.y));
                }
            }

            cfg.ConveyorLine = cl;
        }

        if (dto.camera != null)
        {
            cfg.Camera = new CameraSetupData
            {
                Enabled = dto.camera.enabled,
                MinOrthoSize = dto.camera.minOrthoSize,
                Padding = dto.camera.padding,
                UsePosition = dto.camera.usePosition,
                Position = new Vector3(dto.camera.position.x, dto.camera.position.y, dto.camera.position.z),
                UseOrthographicSize = dto.camera.useOrthographicSize,
                OrthographicSize = dto.camera.orthographicSize
            };
        }

        return cfg;
    }
}
