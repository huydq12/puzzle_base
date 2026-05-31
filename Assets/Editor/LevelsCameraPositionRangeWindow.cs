#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class LevelsCameraPositionRangeWindow : EditorWindow
{
    private const string DefaultSourceFolder = "Assets/SO";

    [SerializeField] private string sourceFolder = DefaultSourceFolder;
    [SerializeField] private int fromLevel = 1;
    [SerializeField] private int toLevel = 10;
    [SerializeField] private bool forceCameraEnabled = true;
    [SerializeField] private bool forceUsePosition = true;
    [SerializeField] private bool syncX;
    [SerializeField] private bool syncY;
    [SerializeField] private bool syncZ;

    private readonly List<Entry> entries = new();
    private Vector2 scroll;
    private string status = "";

    [MenuItem("Tools/Levels/Edit Camera Positions In Range (Assets/SO)")]
    private static void Open()
    {
        GetWindow<LevelsCameraPositionRangeWindow>("Level Camera Positions");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Level Camera Position Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        DrawSourceFolder();
        DrawRange();

        EditorGUILayout.BeginHorizontal();
        forceCameraEnabled = EditorGUILayout.ToggleLeft("Force Camera.Enabled", forceCameraEnabled, GUILayout.Width(170f));
        forceUsePosition = EditorGUILayout.ToggleLeft("Force UsePosition", forceUsePosition, GUILayout.Width(160f));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Sync Edited Axis", GUILayout.Width(110f));
        syncX = EditorGUILayout.ToggleLeft("X", syncX, GUILayout.Width(45f));
        syncY = EditorGUILayout.ToggleLeft("Y", syncY, GUILayout.Width(45f));
        syncZ = EditorGUILayout.ToggleLeft("Z", syncZ, GUILayout.Width(45f));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Load Levels", GUILayout.Height(28f)))
            LoadEntries();

        using (new EditorGUI.DisabledScope(entries.Count == 0))
        {
            if (GUILayout.Button("Apply To SO", GUILayout.Height(28f)))
                ApplyEntries();
        }

        if (GUILayout.Button("Export Secure Levels", GUILayout.Height(28f)))
            ExportSecureLevels();
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(status))
            EditorGUILayout.HelpBox(status, MessageType.Info);

        DrawEntries();
    }

    private void DrawSourceFolder()
    {
        EditorGUILayout.BeginHorizontal();
        sourceFolder = EditorGUILayout.TextField("SO Folder", sourceFolder);

        if (GUILayout.Button("Pick", GUILayout.Width(60f)))
        {
            string absolute = EditorUtility.OpenFolderPanel("Select LevelConfig folder", Application.dataPath, "");
            if (!string.IsNullOrEmpty(absolute))
            {
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string relative = absolute.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase)
                    ? absolute.Substring(projectRoot.Length + 1).Replace("\\", "/")
                    : absolute;

                sourceFolder = relative;
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawRange()
    {
        EditorGUILayout.BeginHorizontal();
        fromLevel = EditorGUILayout.IntField("From Level", Mathf.Max(0, fromLevel));
        toLevel = EditorGUILayout.IntField("To Level", Mathf.Max(fromLevel, toLevel));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawEntries()
    {
        if (entries.Count == 0) return;

        EditorGUILayout.Space(8f);
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Level", GUILayout.Width(48f));
        GUILayout.Label("Asset", GUILayout.Width(210f));
        GUILayout.Label("Enabled", GUILayout.Width(64f));
        GUILayout.Label("Use Pos", GUILayout.Width(64f));
        GUILayout.Label("Position");
        EditorGUILayout.EndHorizontal();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(entry.level.ToString(), GUILayout.Width(48f));

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(Path.GetFileName(entry.assetPath), GUILayout.Width(210f));
            }

            entry.enabled = EditorGUILayout.Toggle(entry.enabled, GUILayout.Width(64f));
            entry.usePosition = EditorGUILayout.Toggle(entry.usePosition, GUILayout.Width(64f));
            EditorGUI.BeginChangeCheck();
            Vector3 newPosition = EditorGUILayout.Vector3Field(GUIContent.none, entry.position);
            if (EditorGUI.EndChangeCheck())
                SetEntryPosition(i, newPosition);

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    private void SetEntryPosition(int changedIndex, Vector3 newPosition)
    {
        if (changedIndex < 0 || changedIndex >= entries.Count) return;

        Vector3 oldPosition = entries[changedIndex].position;
        entries[changedIndex].position = newPosition;

        if (!syncX && !syncY && !syncZ) return;

        for (int i = 0; i < entries.Count; i++)
        {
            if (i == changedIndex) continue;

            Vector3 position = entries[i].position;
            if (syncX && !Mathf.Approximately(oldPosition.x, newPosition.x)) position.x = newPosition.x;
            if (syncY && !Mathf.Approximately(oldPosition.y, newPosition.y)) position.y = newPosition.y;
            if (syncZ && !Mathf.Approximately(oldPosition.z, newPosition.z)) position.z = newPosition.z;
            entries[i].position = position;
        }
    }

    private void LoadEntries()
    {
        entries.Clear();

        if (!AssetDatabase.IsValidFolder(sourceFolder))
        {
            status = $"Invalid folder: {sourceFolder}";
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:LevelConfig", new[] { sourceFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            LevelConfig config = AssetDatabase.LoadAssetAtPath<LevelConfig>(path);
            if (config == null) continue;

            int level = ResolveLevel(config, path);
            if (level < fromLevel || level > toLevel) continue;

            entries.Add(new Entry
            {
                level = level,
                assetPath = path,
                config = config,
                enabled = config.Camera.Enabled,
                usePosition = config.Camera.UsePosition,
                position = config.Camera.Position
            });
        }

        entries.Sort((a, b) => a.level.CompareTo(b.level));
        status = entries.Count == 0
            ? $"No LevelConfig found from {fromLevel} to {toLevel} in {sourceFolder}."
            : $"Loaded {entries.Count} level(s) from {fromLevel} to {toLevel}.";
    }

    private void ApplyEntries()
    {
        int updated = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry.config == null) continue;

                Undo.RecordObject(entry.config, "Apply Level Camera Position");

                CameraSetupData camera = entry.config.Camera;
                camera.Enabled = forceCameraEnabled || entry.enabled;
                camera.UsePosition = forceUsePosition || entry.usePosition;
                camera.Position = entry.position;
                entry.config.Camera = camera;

                EditorUtility.SetDirty(entry.config);
                updated++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        status = $"Applied camera positions to {updated} LevelConfig asset(s).";
    }

    private void ExportSecureLevels()
    {
        const string menuPath = "Tools/Levels/Export Secure Levels (Encrypted StreamingAssets)";
        bool executed = EditorApplication.ExecuteMenuItem(menuPath);
        status = executed
            ? "Exported secure levels to StreamingAssets."
            : $"Failed to execute menu: {menuPath}";
    }

    private static int ResolveLevel(LevelConfig config, string assetPath)
    {
        if (config.Level > 0) return config.Level;

        string name = Path.GetFileNameWithoutExtension(assetPath);
        string digits = "";
        for (int i = 0; i < name.Length; i++)
        {
            if (char.IsDigit(name[i]))
                digits += name[i];
        }

        return int.TryParse(digits, out int level) ? level : 0;
    }

    private class Entry
    {
        public int level;
        public string assetPath;
        public LevelConfig config;
        public bool enabled;
        public bool usePosition;
        public Vector3 position;
    }
}
#endif
