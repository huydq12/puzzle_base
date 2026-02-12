using UnityEngine;

public static class TutorialPopupService
{
    private const string DefaultResourcesPath = "Configs/TutorialPopupConfig";
    private static TutorialPopupConfig cachedConfig;

    private static bool DebugLogs => false;

    private static void Log(string message)
    {
        if (!DebugLogs) return;
        Debug.Log($"[TutorialPopupService] {message}");
    }

    public static TutorialPopupConfig Config
    {
        get
        {
            if (cachedConfig != null) return cachedConfig;
            cachedConfig = Resources.Load<TutorialPopupConfig>(DefaultResourcesPath);
            return cachedConfig;
        }
    }

    public static void TryShowAtLevelStart(int currentLevel)
    {
        var cfg = Config;
        if (cfg == null)
        {
            Log("TryShowAtLevelStart: Config null");
            return;
        }

        currentLevel = Mathf.Max(1, currentLevel);
        var entry = cfg.GetEntry(currentLevel);
        if (entry == null)
        {
            Log($"TryShowAtLevelStart: no entry for level={currentLevel}");
            return;
        }

        string key = $"tutorial_popup_level_{currentLevel}";
        int pref = PlayerPrefs.GetInt(key, 0);
        if (pref == 1)
        {
            Log($"TryShowAtLevelStart: already shown key={key}");
            return;
        }

        var tutorialManager = Object.FindFirstObjectByType<TutorialManager>();
        if (tutorialManager == null)
        {
            Log("TryShowAtLevelStart: TutorialManager not found");
            return;
        }
        Log($"TryShowAtLevelStart: showing popup level={currentLevel} key={key}");
        tutorialManager.ShowTutorialPopup(entry.icon, entry.title, entry.description);
        PlayerPrefs.SetInt(key, 1);
    }
}
