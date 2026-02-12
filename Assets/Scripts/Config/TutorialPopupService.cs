using UnityEngine;

public static class TutorialPopupService
{
    private const string DefaultResourcesPath = "Configs/TutorialPopupConfig";
    private static TutorialPopupConfig cachedConfig;

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
        if (cfg == null) return;

        currentLevel = Mathf.Max(1, currentLevel);
        var entry = cfg.GetEntry(currentLevel);
        if (entry == null) return;

        string key = $"tutorial_popup_level_{currentLevel}";
        if (PlayerPrefs.GetInt(key, 0) == 1) return;

        var tutorialManager = Object.FindFirstObjectByType<TutorialManager>();
        if (tutorialManager == null) return;
        tutorialManager.ShowTutorialPopup(entry.icon, entry.title, entry.description);
        PlayerPrefs.SetInt(key, 1);
    }
}
