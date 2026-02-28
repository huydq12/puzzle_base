#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class LevelsCameraAutoConfigTool
{
    [MenuItem("Tools/Levels/Fix Camera Ortho From Board Size (Assets/SO)")]
    private static void FixCameraOrthoForAllLevelsInAssetsSO()
    {
        const string sourceFolder = "Assets/SO";
        string[] guids = AssetDatabase.FindAssets("t:LevelConfig", new[] { sourceFolder });
        if (guids == null || guids.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Fix Camera Ortho",
                $"No LevelConfig assets found in `{sourceFolder}`.",
                "OK"
            );
            return;
        }

        // Simple size-based camera rules (grid-size driven):
        // - Map bigger => Position.y bigger
        // - Map bigger => OrthographicSize bigger
        // Example:
        //   5x5 => y=13, ortho=8
        //   6x6 => y=13, ortho=8.5
        const int baseDim = 5;
        const float baseY = 13f;
        const float baseOrtho = 8f;
        const float orthoStepPerCell = 0.5f;
        const float yIncreasePerCell = 0.2f;

        int updated = 0;
        int skipped = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                LevelConfig config = AssetDatabase.LoadAssetAtPath<LevelConfig>(assetPath);
                if (config == null)
                {
                    skipped++;
                    continue;
                }

                int columns = Mathf.Max(1, config.Columns);
                int rows = Mathf.Max(1, config.Rows);

                float minSize = config.Camera.MinOrthoSize > 0f ? config.Camera.MinOrthoSize : 0f;
                int maxDim = Mathf.Max(columns, rows);

                float orthoSize = baseOrtho + Mathf.Max(0, maxDim - baseDim) * orthoStepPerCell;
                if (orthoSize < minSize) orthoSize = minSize;

                config.Camera.Enabled = true;
                config.Camera.UseOrthographicSize = true;
                config.Camera.OrthographicSize = orthoSize;

                Vector3 pos = config.Camera.Position;
                pos.x = 0f;
                pos.z = 0f;
                float camY = baseY - Mathf.Max(0, maxDim - baseDim) * yIncreasePerCell;
                pos.y = camY;
                config.Camera.UsePosition = true;
                config.Camera.Position = pos;

                EditorUtility.SetDirty(config);
                updated++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Fix Camera Ortho",
            $"Done.\nUpdated: {updated}\nSkipped: {skipped}",
            "OK"
        );
    }
}
#endif
