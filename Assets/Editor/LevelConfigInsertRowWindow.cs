#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class LevelConfigInsertRowWindow : EditorWindow
{
    private int _beforeRowOneBased = 5;
    private int _removeRowOneBased = 5;
    private int _beforeColumnOneBased = 5;
    private int _removeColumnOneBased = 5;

    [MenuItem("Tools/Levels/Insert Empty Row/Column...")]
    private static void Open()
    {
        LevelConfigInsertRowWindow window = GetWindow<LevelConfigInsertRowWindow>(utility: true, title: "Insert Empty Row/Column");
        window.minSize = new Vector2(360f, 120f);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Insert/remove an empty row/column in selected LevelConfig assets", EditorStyles.boldLabel);

        EditorGUILayout.Space(6f);
        _beforeRowOneBased = EditorGUILayout.IntField("Insert Before Row (1-based)", _beforeRowOneBased);
        EditorGUILayout.HelpBox("Example: enter 5 to insert between row 4 and row 5.\nAll points/cells with y >= 4 (0-based) will shift up by +1.", MessageType.Info);

        EditorGUILayout.Space(8f);

        using (new EditorGUI.DisabledScope(Selection.objects == null || Selection.objects.Length == 0))
        {
            if (GUILayout.Button("Insert Row Into Selected LevelConfig"))
            {
                ApplyRowToSelection();
            }
        }

        EditorGUILayout.Space(6f);
        _removeRowOneBased = EditorGUILayout.IntField("Remove Row (1-based)", _removeRowOneBased);
        EditorGUILayout.HelpBox("Removes the given row and shifts everything above it down by -1.", MessageType.Warning);

        EditorGUILayout.Space(8f);
        using (new EditorGUI.DisabledScope(Selection.objects == null || Selection.objects.Length == 0))
        {
            if (GUILayout.Button("Remove Row From Selected LevelConfig"))
            {
                RemoveRowFromSelection();
            }
        }

        EditorGUILayout.Space(10f);
        _beforeColumnOneBased = EditorGUILayout.IntField("Insert Before Column (1-based)", _beforeColumnOneBased);
        EditorGUILayout.HelpBox("Example: enter 5 to insert between column 4 and column 5.\nAll points/cells with x >= 4 (0-based) will shift right by +1.", MessageType.Info);

        EditorGUILayout.Space(8f);

        using (new EditorGUI.DisabledScope(Selection.objects == null || Selection.objects.Length == 0))
        {
            if (GUILayout.Button("Insert Column Into Selected LevelConfig"))
            {
                ApplyColumnToSelection();
            }
        }

        EditorGUILayout.Space(6f);
        _removeColumnOneBased = EditorGUILayout.IntField("Remove Column (1-based)", _removeColumnOneBased);
        EditorGUILayout.HelpBox("Removes the given column and shifts everything right of it left by -1.", MessageType.Warning);

        EditorGUILayout.Space(8f);
        using (new EditorGUI.DisabledScope(Selection.objects == null || Selection.objects.Length == 0))
        {
            if (GUILayout.Button("Remove Column From Selected LevelConfig"))
            {
                RemoveColumnFromSelection();
            }
        }
    }

    private void ApplyRowToSelection()
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

                bool changed = LevelConfigShiftRowTool.InsertEmptyRowBeforeOneBased(config, _beforeRowOneBased);
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

    private void RemoveRowFromSelection()
    {
        Object[] selected = Selection.objects;
        if (selected == null || selected.Length == 0)
        {
            EditorUtility.DisplayDialog("Remove Row", "Select one or more `LevelConfig` assets in Project window.", "OK");
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

                bool changed = LevelConfigShiftRowTool.RemoveRowOneBased(config, _removeRowOneBased);
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

        EditorUtility.DisplayDialog("Remove Row", $"Done.\nUpdated: {updated}\nSkipped: {skipped}", "OK");
    }

    private void ApplyColumnToSelection()
    {
        Object[] selected = Selection.objects;
        if (selected == null || selected.Length == 0)
        {
            EditorUtility.DisplayDialog("Insert Column", "Select one or more `LevelConfig` assets in Project window.", "OK");
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

                bool changed = LevelConfigShiftRowTool.InsertEmptyColumnBeforeOneBased(config, _beforeColumnOneBased);
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

        EditorUtility.DisplayDialog("Insert Column", $"Done.\nUpdated: {updated}\nSkipped: {skipped}", "OK");
    }

    private void RemoveColumnFromSelection()
    {
        Object[] selected = Selection.objects;
        if (selected == null || selected.Length == 0)
        {
            EditorUtility.DisplayDialog("Remove Column", "Select one or more `LevelConfig` assets in Project window.", "OK");
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

                bool changed = LevelConfigShiftRowTool.RemoveColumnOneBased(config, _removeColumnOneBased);
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

        EditorUtility.DisplayDialog("Remove Column", $"Done.\nUpdated: {updated}\nSkipped: {skipped}", "OK");
    }
}
#endif
