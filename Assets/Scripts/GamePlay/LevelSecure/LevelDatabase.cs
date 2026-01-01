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

    private static bool _loaded;
    private static readonly Dictionary<int, LevelConfig> _cache = new();

    public static IEnumerator LoadLevelAsync(int level, Action<LevelConfig> onLoaded)
    {
        if (onLoaded == null) yield break;

        if (_cache.TryGetValue(level, out var cached) && cached != null)
        {
            onLoaded(cached);
            yield break;
        }

        if (!_loaded)
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
        _loaded = true;

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
            _cache[dto.level] = BuildLevelConfig(dto);
        }
    }

    private static LevelConfig BuildLevelConfig(LevelConfigDto dto)
    {
        var cfg = ScriptableObject.CreateInstance<LevelConfig>();
        cfg.SetLevel(dto.level);
        cfg.SetRows(dto.rows);
        cfg.SetColumns(dto.columns);
        cfg.TotalSlot = dto.totalSlot;

        cfg.Goals = new List<LevelGoalData>();
        if (dto.goals != null)
        {
            for (int i = 0; i < dto.goals.Count; i++)
            {
                var g = dto.goals[i];
                if (g == null) continue;
                cfg.Goals.Add(new LevelGoalData
                {
                    Type = g.type,
                    TargetCount = g.targetCount,
                    TargetColor = g.targetColor
                });
            }
        }

        cfg.SetContainers(dto.containers != null ? new List<ObjectColor>(dto.containers) : new List<ObjectColor>());

        int colsCfg = cfg.GetColumns();
        int rowsCfg = cfg.GetRows();
        cfg.GridCellData = new GridCellData[colsCfg, rowsCfg];
        for (int x = 0; x < colsCfg; x++)
        {
            for (int y = 0; y < rowsCfg; y++)
            {
                cfg.GridCellData[x, y] = new GridCellData { IsEmpty = false, Colors = new List<ColorData>(), GateWaves = new List<GateWaveData>() };
            }
        }

        if (dto.grid != null)
        {
            int cols = Mathf.Min(colsCfg, dto.grid.Count);
            for (int x = 0; x < cols; x++)
            {
                var col = dto.grid[x];
                if (col == null || col.cells == null) continue;
                int rows = Mathf.Min(rowsCfg, col.cells.Count);
                for (int y = 0; y < rows; y++)
                {
                    var cellDto = col.cells[y];
                    if (cellDto == null) continue;

                    var cell = cfg.GridCellData[x, y];
                    if (cell == null)
                    {
                        cell = new GridCellData { Colors = new List<ColorData>(), GateWaves = new List<GateWaveData>() };
                        cfg.GridCellData[x, y] = cell;
                    }

                    cell.IsEmpty = cellDto.isEmpty;
                    cell.ElementType = cellDto.elementType;
                    cell.GateDirection = cellDto.gateDirection;
                    cell.IceHitPoints = cellDto.iceHitPoints;
                    cell.ScrewHitPoints = cellDto.screwHitPoints;
                    cell.LockItemCount = cellDto.lockItemCount;
                    cell.LockItemColor = cellDto.lockItemColor;

                    if (cell.Colors == null) cell.Colors = new List<ColorData>();
                    cell.Colors.Clear();
                    if (cellDto.colors != null)
                    {
                        for (int k = 0; k < cellDto.colors.Count; k++)
                        {
                            cell.Colors.Add(new ColorData { Color = cellDto.colors[k] });
                        }
                    }

                    if (cell.GateWaves == null) cell.GateWaves = new List<GateWaveData>();
                    cell.GateWaves.Clear();
                    if (cellDto.gateWaves != null)
                    {
                        for (int w = 0; w < cellDto.gateWaves.Count; w++)
                        {
                            var waveDto = cellDto.gateWaves[w];
                            var wave = new GateWaveData();
                            wave.Colors = waveDto != null && waveDto.colors != null ? new List<ObjectColor>(waveDto.colors) : new List<ObjectColor>();
                            cell.GateWaves.Add(wave);
                        }
                    }
                }
            }
        }

        return cfg;
    }
}
