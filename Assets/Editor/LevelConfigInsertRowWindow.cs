#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class LevelConfigInsertRowWindow : EditorWindow
{
    private int _afterRowOneBased = 4;
    private int _afterColumnOneBased = 4;

    [MenuItem("Tools/Levels/Insert Empty Row/Column...")]
    private static void Open()
    {
        LevelConfigInsertRowWindow window = GetWindow<LevelConfigInsertRowWindow>(utility: true, title: "Insert Empty Row/Column");
        window.minSize = new Vector2(360f, 120f);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Insert an empty row/column into selected LevelConfig assets", EditorStyles.boldLabel);

        EditorGUILayout.Space(6f);
        _afterRowOneBased = EditorGUILayout.IntField("After Row (1-based)", _afterRowOneBased);
        EditorGUILayout.HelpBox("Example: enter 4 to insert between row 4 and row 5.\nAll points/cells with y >= 4 (0-based) will shift up by +1.", MessageType.Info);

        EditorGUILayout.Space(8f);

        using (new EditorGUI.DisabledScope(Selection.objects == null || Selection.objects.Length == 0))
        {
            if (GUILayout.Button("Insert Row Into Selected LevelConfig"))
            {
                ApplyRowToSelection();
            }
        }

        EditorGUILayout.Space(10f);
        _afterColumnOneBased = EditorGUILayout.IntField("After Column (1-based)", _afterColumnOneBased);
        EditorGUILayout.HelpBox("Example: enter 4 to insert between column 4 and column 5.\nAll points/cells with x >= 4 (0-based) will shift right by +1.", MessageType.Info);

        EditorGUILayout.Space(8f);

        using (new EditorGUI.DisabledScope(Selection.objects == null || Selection.objects.Length == 0))
        {
            if (GUILayout.Button("Insert Column Into Selected LevelConfig"))
            {
                ApplyColumnToSelection();
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

                bool changed = LevelConfigShiftRowTool.InsertEmptyRowAfterOneBased(config, _afterRowOneBased);
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

                bool changed = LevelConfigShiftRowTool.InsertEmptyColumnAfterOneBased(config, _afterColumnOneBased);
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
}
#endif
