#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class LevelIdAuditTool
{
    private const string SourceFolder = "Assets/SO";

    [MenuItem("Tools/Levels/Audit Level IDs (Assets/SO)")]
    private static void Audit()
    {
        string[] guids = AssetDatabase.FindAssets("t:LevelConfig", new[] { SourceFolder });
        if (guids == null || guids.Length == 0)
        {
            Debug.LogWarning($"[LevelIdAuditTool] No LevelConfig found in {SourceFolder}");
            return;
        }

        var entries = new List<Entry>(guids.Length);
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var config = AssetDatabase.LoadAssetAtPath<LevelConfig>(path);
            if (config == null) continue;
            entries.Add(new Entry(path, config.Level, TryParseLevelFromFileName(path, out int fromName) ? fromName : (int?)null));
        }

        var duplicates = entries
            .GroupBy(e => e.Level)
            .Where(g => g.Key > 0 && g.Count() > 1)
            .OrderBy(g => g.Key)
            .ToList();

        var mismatches = entries
            .Where(e => e.LevelFromFileName.HasValue && e.LevelFromFileName.Value != e.Level)
            .OrderBy(e => e.LevelFromFileName.Value)
            .ThenBy(e => e.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Debug.Log($"[LevelIdAuditTool] Scanned {entries.Count} LevelConfig under {SourceFolder}.");

        if (duplicates.Count == 0)
        {
            Debug.Log("[LevelIdAuditTool] No duplicate Level IDs found.");
        }
        else
        {
            Debug.LogError($"[LevelIdAuditTool] Found {duplicates.Count} duplicate Level IDs. This will cause levels.dat to overwrite entries (example: level 109 can show another level's data).");
            foreach (var g in duplicates)
            {
                string joined = string.Join(", ", g.Select(x => x.Path));
                Debug.LogError($"[LevelIdAuditTool] Duplicate Level={g.Key}: {joined}");
            }
        }

        if (mismatches.Count == 0)
        {
            Debug.Log("[LevelIdAuditTool] No filename/Level mismatches found.");
        }
        else
        {
            Debug.LogWarning($"[LevelIdAuditTool] Found {mismatches.Count} filename/Level mismatches (file name 'Level N.asset' but config.Level != N):");
            foreach (var e in mismatches)
            {
                Debug.LogWarning($"[LevelIdAuditTool] Mismatch: {e.Path} fileNameLevel={e.LevelFromFileName} config.Level={e.Level}");
            }
        }
    }

    private static bool TryParseLevelFromFileName(string assetPath, out int level)
    {
        level = 0;
        if (string.IsNullOrEmpty(assetPath)) return false;
        string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        if (string.IsNullOrEmpty(fileName)) return false;

        Match m = Regex.Match(fileName, @"^Level\s+(\d+)$", RegexOptions.IgnoreCase);
        if (!m.Success) return false;
        return int.TryParse(m.Groups[1].Value, out level);
    }

    private readonly struct Entry
    {
        public readonly string Path;
        public readonly int Level;
        public readonly int? LevelFromFileName;

        public Entry(string path, int level, int? levelFromFileName)
        {
            Path = path;
            Level = level;
            LevelFromFileName = levelFromFileName;
        }
    }
}
#endif

