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
        Board board = UnityEngine.Object.FindFirstObjectByType<Board>();
        if (board == null)
        {
            EditorUtility.DisplayDialog(
                "Fix Camera Ortho",
                "No `Board` found in the open scene.\n\nOpen the gameplay scene that contains the Board (e.g. `Assets/Scenes/_Game/Scene/Game.unity`) and run this again.",
                "OK"
            );
            return;
        }

        Vector2 spacing = board.Spacing;

        SerializedObject boardSo = new SerializedObject(board);
        float cellSize = boardSo.FindProperty("_cellSize")?.floatValue ?? 0f;
        float defaultPadding = boardSo.FindProperty("_paddingCamera")?.floatValue ?? 0f;

        if (cellSize <= 0f)
        {
            EditorUtility.DisplayDialog(
                "Fix Camera Ortho",
                "Board `_cellSize` is not set (<= 0). Aborting to avoid writing wrong camera sizes.",
                "OK"
            );
            return;
        }

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
                float padding = config.Camera.Padding > 0f ? config.Camera.Padding : defaultPadding;

                // Matches Board.SetupCamera() logic, but stored in "reference aspect" 9:16 so it can be scaled at runtime.
                float refAspect = 9f / 16f;
                float width = (columns - 1) * spacing.x;
                float height = (rows - 1) * spacing.y;
                float widthPlus = width + cellSize * 2f + padding * 2f;
                float heightPlus = height + cellSize * 2f + padding * 2f;
                float orthoRef = Mathf.Max(heightPlus / 2f, widthPlus / (2f * refAspect));

                float minSize = config.Camera.MinOrthoSize > 0f ? config.Camera.MinOrthoSize : 0f;
                orthoRef = Mathf.Max(orthoRef, minSize);

                config.Camera.Enabled = true;
                config.Camera.UseOrthographicSize = true;
                config.Camera.OrthographicSize = orthoRef;

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
