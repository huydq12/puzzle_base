using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
public class ToolInPlayLevel : MonoBehaviour
{
    [SerializeField] private bool showTool = true;
    [SerializeField] private int captureEndLevel;
    [SerializeField] private string captureFolderName = "LevelScreenshots";
    [SerializeField] private float normalLevelCaptureDelay = 0.1f;
    [SerializeField] private float hardLevelCaptureDelay = 2.2f;
    [SerializeField] private bool showGatePositions = true;
    [SerializeField] private bool editGatePositions = true;

    private Rect _toolRect = new Rect(8f, 8f, 380f, 420f);
    private string _levelInput = "1";
    private string _captureEndLevelInput = "";
    private string _status = "";
    private string _captureFolderPath = "";
    private Coroutine _captureAllRoutine;
    private bool _hideToolForCapture;
    private int _selectedGateIndex = -1;
    private bool _draggingGate;
    private float _draggingGateLocalY;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9))
            showTool = !showTool;
    }

    private void OnGUI()
    {
        if (!showTool || _hideToolForCapture) return;

        _toolRect = GUI.Window(GetInstanceID(), _toolRect, DrawWindow, "Level Tool");
        DrawGateMarkers();
    }

    private void DrawWindow(int windowId)
    {
        GameManagerInGame gm = GameManagerInGame.Instance;
        int currentLevel = gm != null ? gm.CurrentLevel : 1;
        int contentLevel = GetCurrentContentLevel();

        GUILayout.BeginVertical();
        GUILayout.Label($"Current: {currentLevel}  Content: {contentLevel}");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("< Back", GUILayout.Height(28f)))
            StartLevel(Mathf.Max(1, currentLevel - 1));
        if (GUILayout.Button("Next >", GUILayout.Height(28f)))
            StartLevel(Mathf.Max(1, currentLevel + 1));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Level", GUILayout.Width(42f));
        _levelInput = GUILayout.TextField(_levelInput, GUILayout.Width(70f));
        if (GUILayout.Button("Go", GUILayout.Height(24f)))
        {
            if (int.TryParse(_levelInput, out int level))
                StartLevel(Mathf.Max(1, level));
            else
                _levelInput = Mathf.Max(1, currentLevel).ToString();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4f);

        if (GUILayout.Button("Capture Current Level", GUILayout.Height(28f)))
            StartCoroutine(CaptureCurrentLevelRoutine());

        GUILayout.BeginHorizontal();
        GUILayout.Label("1 ->", GUILayout.Width(30f));
        if (captureEndLevel <= 0)
        {
            captureEndLevel = DiscoverLastLevel();
            _captureEndLevelInput = captureEndLevel.ToString();
        }
        _captureEndLevelInput = GUILayout.TextField(_captureEndLevelInput, GUILayout.Width(70f));
        if (int.TryParse(_captureEndLevelInput, out int endLevel))
            captureEndLevel = Mathf.Max(1, endLevel);
        GUILayout.EndHorizontal();

        bool wasEnabled = GUI.enabled;
        GUI.enabled = _captureAllRoutine == null;
        if (GUILayout.Button("Capture All Levels", GUILayout.Height(28f)))
            _captureAllRoutine = StartCoroutine(CaptureAllLevelsRoutine(1, Mathf.Max(1, captureEndLevel)));
        GUI.enabled = wasEnabled;

        if (_captureAllRoutine != null && GUILayout.Button("Stop Batch", GUILayout.Height(24f)))
        {
            StopCoroutine(_captureAllRoutine);
            _captureAllRoutine = null;
            _status = "Batch stopped.";
        }

        GUILayout.Space(4f);
        GUILayout.Label(GetCaptureFolder(), GUILayout.MaxWidth(360f));

        GUILayout.BeginHorizontal();
#if UNITY_EDITOR
        if (GUILayout.Button("Pick Folder", GUILayout.Height(24f)))
        {
            string selected = EditorUtility.OpenFolderPanel("Level screenshot output", GetCaptureFolder(), "");
            if (!string.IsNullOrEmpty(selected))
                _captureFolderPath = selected;
        }
#endif
        if (GUILayout.Button("Open", GUILayout.Height(24f)))
            OpenCaptureFolder();
        GUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(_status))
            GUILayout.Label(_status, GUILayout.MaxWidth(360f));

        GUILayout.Space(6f);
        showGatePositions = GUILayout.Toggle(showGatePositions, "Show Gate Positions");
        editGatePositions = GUILayout.Toggle(editGatePositions, "Drag Edit Gates");

        GUILayout.Label(GetSelectedGateText(), GUILayout.MaxWidth(360f));

#if UNITY_EDITOR
        if (GUILayout.Button("Export Gate Positions To SO", GUILayout.Height(28f)))
            ExportGatePositionsToSo();

        if (GUILayout.Button("Export Secure Levels (levels.dat)", GUILayout.Height(28f)))
            ExportSecureLevels();
#endif

        GUILayout.EndVertical();
        GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
    }

    private void StartLevel(int level)
    {
        _levelInput = Mathf.Max(1, level).ToString();
        _selectedGateIndex = -1;
        _draggingGate = false;

        GameManagerInGame gm = GameManagerInGame.Instance;
        if (gm != null)
            gm.StartGameWithoutSavingProgress(level);
    }

    private void DrawGateMarkers()
    {
        if (!showGatePositions) return;

        GameManagerInGame gm = GameManagerInGame.Instance;
        if (gm == null || gm.CurrentGameStateInGame != GameStateInGame.Playing) return;
        if (ShooterController.Instance == null || ShooterController.Instance.Gates == null) return;
        if (Board.Instance == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Event e = Event.current;
        List<IGate> gates = ShooterController.Instance.Gates;
        int singleGateCount = GetSingleGateCount();

        for (int i = 0; i < gates.Count; i++)
        {
            Transform root = gates[i] != null ? gates[i].RootTransform : null;
            if (root == null) continue;

            Vector3 screen = cam.WorldToScreenPoint(root.position);
            if (screen.z <= 0f) continue;

            Vector2 guiPos = new Vector2(screen.x, Screen.height - screen.y);
            Rect rect = new Rect(guiPos.x - 18f, guiPos.y - 18f, 36f, 36f);
            string label = GetGateLabel(i, singleGateCount);

            if (editGatePositions)
                HandleGateDrag(e, i, root, rect);

            Color oldColor = GUI.color;
            GUI.color = i == _selectedGateIndex ? Color.yellow : new Color(0.3f, 1f, 0.45f, 0.9f);
            GUI.Box(rect, label);
            GUI.color = oldColor;

            Vector3 local = root.localPosition;
            Rect textRect = new Rect(rect.xMax + 2f, rect.y - 10f, 150f, 42f);
            GUI.Label(textRect, $"{local.x:0.##}, {local.y:0.##}, {local.z:0.##}");
        }
    }

    private void HandleGateDrag(Event e, int gateIndex, Transform root, Rect rect)
    {
        if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
        {
            _selectedGateIndex = gateIndex;
            _draggingGate = true;
            _draggingGateLocalY = root.localPosition.y;
            e.Use();
        }

        if (_draggingGate && _selectedGateIndex == gateIndex && e.type == EventType.MouseDrag)
        {
            if (TryGetGateLocalPositionFromMouse(e.mousePosition, _draggingGateLocalY, out Vector3 localPos))
                ApplyGatePosition(gateIndex, localPos);
            e.Use();
        }

        if (_draggingGate && _selectedGateIndex == gateIndex && e.type == EventType.MouseUp)
        {
            _draggingGate = false;
            e.Use();
        }
    }

    private string GetGateLabel(int gateIndex, int singleGateCount)
    {
        if (gateIndex < singleGateCount)
            return $"G{gateIndex + 1}";

        return $"D{gateIndex - singleGateCount + 1}";
    }

    private string GetSelectedGateText()
    {
        if (_selectedGateIndex < 0) return "Selected Gate: none";
        if (ShooterController.Instance == null || ShooterController.Instance.Gates == null) return "Selected Gate: none";
        if (_selectedGateIndex >= ShooterController.Instance.Gates.Count) return "Selected Gate: none";

        Transform root = ShooterController.Instance.Gates[_selectedGateIndex]?.RootTransform;
        if (root == null) return "Selected Gate: none";

        int singleGateCount = GetSingleGateCount();

        Vector3 p = root.localPosition;
        return $"{GetGateLabel(_selectedGateIndex, singleGateCount)} Pos: {p.x:0.###}, {p.y:0.###}, {p.z:0.###}";
    }

    private bool TryGetGateLocalPositionFromMouse(Vector2 guiMousePosition, float localY, out Vector3 localPosition)
    {
        localPosition = Vector3.zero;
        if (Board.Instance == null) return false;

        Camera cam = Camera.main;
        if (cam == null) return false;

        Transform boardTransform = Board.Instance.transform;
        Vector3 screenMouse = new Vector3(guiMousePosition.x, Screen.height - guiMousePosition.y, 0f);
        Ray ray = cam.ScreenPointToRay(screenMouse);
        Plane plane = new Plane(boardTransform.up, boardTransform.TransformPoint(new Vector3(0f, localY, 0f)));

        if (!plane.Raycast(ray, out float distance)) return false;

        Vector3 world = ray.GetPoint(distance);
        localPosition = boardTransform.InverseTransformPoint(world);
        localPosition.y = localY;
        return true;
    }

    private void ApplyGatePosition(int gateIndex, Vector3 localPosition)
    {
        if (ShooterController.Instance == null || ShooterController.Instance.Gates == null) return;
        if (gateIndex < 0 || gateIndex >= ShooterController.Instance.Gates.Count) return;

        Transform root = ShooterController.Instance.Gates[gateIndex]?.RootTransform;
        if (root == null) return;

        root.localPosition = localPosition;
    }

    private void ApplyGatePositionToConfig(LevelConfig config, int gateIndex, Vector3 localPosition)
    {
        if (config == null) return;

        int singleGateCount = config.Gates != null ? config.Gates.Count : 0;
        if (gateIndex < singleGateCount)
        {
            if (config.Gates[gateIndex] != null)
                config.Gates[gateIndex].Position = localPosition;
            return;
        }

        int doubleIndex = gateIndex - singleGateCount;
        if (config.GatesDouble != null && doubleIndex >= 0 && doubleIndex < config.GatesDouble.Count && config.GatesDouble[doubleIndex] != null)
            config.GatesDouble[doubleIndex].Position = localPosition;
    }

#if UNITY_EDITOR
    private void ExportGatePositionsToSo()
    {
        if (Board.Instance == null)
        {
            _status = "No active board.";
            return;
        }

        int contentLevel = GetCurrentContentLevel();
        LevelConfig target = LoadLevelAsset(contentLevel);
        if (target == null)
        {
            _status = $"SO not found for level {contentLevel}.";
            return;
        }

        if (ShooterController.Instance == null || ShooterController.Instance.Gates == null)
        {
            _status = "No runtime gates.";
            return;
        }

        Undo.RecordObject(target, "Export Gate Positions To SO");

        List<IGate> gates = ShooterController.Instance.Gates;
        for (int i = 0; i < gates.Count; i++)
        {
            Transform root = gates[i] != null ? gates[i].RootTransform : null;
            if (root == null) continue;
            ApplyGatePositionToConfig(target, i, root.localPosition);
        }

        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        _status = $"Exported gates to {AssetDatabase.GetAssetPath(target)}";
    }

    private LevelConfig LoadLevelAsset(int contentLevel)
    {
        LevelConfig config = AssetDatabase.LoadAssetAtPath<LevelConfig>($"Assets/SO/Level {contentLevel}.asset");
        if (config != null) return config;
        return AssetDatabase.LoadAssetAtPath<LevelConfig>($"Assets/Levels/SO/Level {contentLevel}.asset");
    }

    private void ExportSecureLevels()
    {
        const string menuPath = "Tools/Levels/Export Secure Levels (Encrypted StreamingAssets)";
        bool executed = EditorApplication.ExecuteMenuItem(menuPath);
        _status = executed
            ? "Exported secure levels to StreamingAssets."
            : $"Failed to execute menu: {menuPath}";
    }
#endif

    private IEnumerator CaptureCurrentLevelRoutine()
    {
        yield return WaitUntilLevelReady();
        if (!IsLevelReady())
        {
            _status = "Level is not ready.";
            yield break;
        }

        int currentLevel = GameManagerInGame.Instance != null ? GameManagerInGame.Instance.CurrentLevel : 1;
        yield return WaitBeforeCapture(currentLevel);

        string path = Path.Combine(GetCaptureFolder(), $"Level_{Mathf.Max(1, currentLevel):000}.png");
        yield return CaptureScreenshotToFile(path);
        _status = $"Saved: {Path.GetFileName(path)}";
    }

    private IEnumerator CaptureAllLevelsRoutine(int startLevel, int endLevel)
    {
        startLevel = Mathf.Max(1, startLevel);
        endLevel = Mathf.Max(startLevel, endLevel);

        string folder = GetCaptureFolder();
        Directory.CreateDirectory(folder);

        int originalLevel = GameManagerInGame.Instance != null ? Mathf.Max(1, GameManagerInGame.Instance.CurrentLevel) : 1;

        for (int level = startLevel; level <= endLevel; level++)
        {
            if (level > startLevel)
                yield return new WaitForSecondsRealtime(0.3f);

            _status = $"Capturing {level}/{endLevel}...";
            StartLevel(level);

            yield return WaitUntilLevelReady();
            if (!IsLevelReady())
            {
                _status = $"Skipped {level}: level not ready.";
                continue;
            }

            yield return WaitBeforeCapture(level);

            string path = Path.Combine(folder, $"Level_{level:000}.png");
            yield return CaptureScreenshotToFile(path);
        }

        _captureAllRoutine = null;
        _status = $"Done. Saved {startLevel}-{endLevel}.";
        StartLevel(originalLevel);
        OpenCaptureFolder();
    }

    private IEnumerator WaitUntilLevelReady()
    {
        float deadline = Time.realtimeSinceStartup + 10f;

        while (!IsLevelReady() && Time.realtimeSinceStartup < deadline)
            yield return null;

        if (IsLevelReady() && UILoadingInGame.Instance != null)
            UILoadingInGame.Instance.Hide();

        yield return new WaitForEndOfFrame();
    }

    private bool IsLevelReady()
    {
        GameManagerInGame gm = GameManagerInGame.Instance;
        return gm != null && gm.CurrentGameStateInGame == GameStateInGame.Playing;
    }

    private IEnumerator WaitBeforeCapture(int level)
    {
        float delay = IsHardLevel(level) ? hardLevelCaptureDelay : normalLevelCaptureDelay;
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        yield return new WaitForEndOfFrame();
    }

    private IEnumerator CaptureScreenshotToFile(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        _hideToolForCapture = true;
        yield return new WaitForEndOfFrame();

        Texture2D texture = null;
        try
        {
            int width = Mathf.Max(1, Screen.width);
            int height = Mathf.Max(1, Screen.height);
            texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply(false);

            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally
        {
            if (texture != null)
                Destroy(texture);

            _hideToolForCapture = false;
        }

        yield return null;
    }

    private string GetCaptureFolder()
    {
        if (!string.IsNullOrEmpty(_captureFolderPath))
            return _captureFolderPath;

#if UNITY_EDITOR
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        return Path.Combine(projectRoot, captureFolderName);
#else
        return Path.Combine(Application.persistentDataPath, captureFolderName);
#endif
    }

    private void OpenCaptureFolder()
    {
        string folder = GetCaptureFolder();
        Directory.CreateDirectory(folder);
#if UNITY_EDITOR
        EditorUtility.RevealInFinder(folder);
#else
        Application.OpenURL(folder);
#endif
    }

    private int DiscoverLastLevel()
    {
#if UNITY_EDITOR
        int maxLevel = 0;
        List<string> searchFolders = new List<string>();
        if (AssetDatabase.IsValidFolder("Assets/SO")) searchFolders.Add("Assets/SO");
        if (AssetDatabase.IsValidFolder("Assets/Levels/SO")) searchFolders.Add("Assets/Levels/SO");

        string[] guids = searchFolders.Count > 0
            ? AssetDatabase.FindAssets("t:LevelConfig", searchFolders.ToArray())
            : Array.Empty<string>();
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            LevelConfig config = AssetDatabase.LoadAssetAtPath<LevelConfig>(path);
            if (config != null)
                maxLevel = Mathf.Max(maxLevel, config.Level);
        }

        if (maxLevel > 0) return maxLevel;
#endif
        return 292;
    }

    private int GetCurrentContentLevel()
    {
        GameManagerInGame gm = GameManagerInGame.Instance;
        return gm != null ? NormalizeContentLevel(gm.CurrentLevel) : 1;
    }

    private int GetSingleGateCount()
    {
#if UNITY_EDITOR
        LevelConfig config = LoadLevelAsset(GetCurrentContentLevel());
        return config != null && config.Gates != null ? config.Gates.Count : 0;
#else
        return 0;
#endif
    }

    private static int NormalizeContentLevel(int level)
    {
        const int loopStartLevel = 30;
        const int loopEndLevel = 292;

        level = Mathf.Max(1, level);
        if (level <= loopEndLevel) return level;
        if (level < loopStartLevel) return level;

        int loopLen = loopEndLevel - loopStartLevel + 1;
        int offset = (level - loopStartLevel) % loopLen;
        return loopStartLevel + offset;
    }

    private static bool IsHardLevel(int level)
    {
        if (level == 10) return true;
        if (level < 18) return false;
        int lastDigit = Mathf.Abs(level) % 10;
        return lastDigit == 3 || lastDigit == 8;
    }
}
#endif
