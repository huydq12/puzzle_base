using UnityEngine;

public static class BoosterUnlockService
{
    private const string DefaultResourcesPath = "Configs/BoosterUnlockConfig";

    private static BoosterUnlockConfig cachedConfig;

    public static BoosterUnlockConfig Config
    {
        get
        {
            if (cachedConfig != null) return cachedConfig;
            cachedConfig = Resources.Load<BoosterUnlockConfig>(DefaultResourcesPath);
            return cachedConfig;
        }
    }

    public static bool IsUnlocked(int boosterType, int currentLevel)
    {
        var cfg = Config;
        if (cfg == null) return true;
        var entry = cfg.GetEntry(boosterType);
        if (entry == null) return true;
        return currentLevel >= Mathf.Max(1, entry.unlockLevel);
    }

    public static int GetUnlockLevel(int boosterType)
    {
        var cfg = Config;
        if (cfg == null) return 1;
        var entry = cfg.GetEntry(boosterType);
        if (entry == null) return 1;
        return Mathf.Max(1, entry.unlockLevel);
    }

    public static string GetLockedToast(int boosterType)
    {
        var cfg = Config;
        int unlockLevel = GetUnlockLevel(boosterType);
        if (cfg == null || string.IsNullOrEmpty(cfg.lockedToastFormat))
        {
            return $"Mở khóa ở level {unlockLevel}";
        }
        return string.Format(cfg.lockedToastFormat, unlockLevel);
    }

    public static void TryGrantUnlockGift(int completedLevel)
    {
        var cfg = Config;
        if (cfg == null || cfg.boosters == null) return;

        var ud = GetUserData();
        if (ud == null) return;

        for (int i = 0; i < cfg.boosters.Count; i++)
        {
            var entry = cfg.boosters[i];
            if (entry == null) continue;
            if (entry.unlockLevel != completedLevel) continue;

            string key = GetGiftClaimKey(entry.boosterType, entry.unlockLevel);
            if (ud.boosterUnlockGiftClaimedKeys != null && ud.boosterUnlockGiftClaimedKeys.Contains(key)) continue;

            GrantBooster(entry.boosterType, Mathf.Max(1, entry.giftAmount));

            ud.boosterUnlockGiftClaimedKeys ??= new System.Collections.Generic.List<string>();
            ud.boosterUnlockGiftClaimedKeys.Add(key);
            ud.Save();
        }
    }

    public static void TryShowUnlockTutorialAtLevelStart(int currentLevel)
    {
        var cfg = Config;
        if (cfg == null || cfg.boosters == null) return;

        var ud = GetUserData();
        if (ud == null) return;

        for (int i = 0; i < cfg.boosters.Count; i++)
        {
            var entry = cfg.boosters[i];
            if (entry == null) continue;
            if (entry.unlockLevel != currentLevel) continue;

            string key = GetTutorialShownKey(entry.boosterType, entry.unlockLevel);
            if (ud.boosterUnlockTutorialShownKeys != null && ud.boosterUnlockTutorialShownKeys.Contains(key)) continue;

            ShowTutorial(entry);

            ud.boosterUnlockTutorialShownKeys ??= new System.Collections.Generic.List<string>();
            ud.boosterUnlockTutorialShownKeys.Add(key);
            ud.Save();
        }
    }

    private static void GrantBooster(int boosterType, int amount)
    {
        if (InventoryManager.Instance != null)
        {
            switch (boosterType)
            {
                case 1:
                    InventoryManager.Instance.AddBoosterType1(amount);
                    return;
                case 2:
                    InventoryManager.Instance.AddBoosterType2(amount);
                    return;
                case 3:
                    InventoryManager.Instance.AddBoosterType3(amount);
                    return;
                case 4:
                    InventoryManager.Instance.AddBoosterType4(amount);
                    return;
            }
        }

        var ud = GetUserData();
        if (ud == null) return;

        switch (boosterType)
        {
            case 1:
                ud.boosterType1 += amount;
                break;
            case 2:
                ud.boosterType2 += amount;
                break;
            case 3:
                ud.boosterType3 += amount;
                break;
            case 4:
                ud.boosterType4 += amount;
                break;
        }
        ud.Save();
    }

    private static UserData GetUserData()
    {
        var gm = GameManagerInGame.Instance;
        if (gm != null && gm.userData != null)
        {
            return gm.userData;
        }
        return Game.Data.Load<UserData>();
    }

    private static void ShowTutorial(BoosterUnlockConfig.BoosterEntry entry)
    {
        if (TutorialManager.Instance == null) return;
        TutorialManager.Instance.ShowBoosterUnlockTutorial(entry.boosterType);

        if (GameUI.Instance == null) return;
        var bottom = GameUI.Instance.Get<UIBottomInGame>();
        if (bottom != null)
        {
            bottom.RefreshBoosterQuantity();
        }
    }

    private static string GetGiftClaimKey(int boosterType, int unlockLevel)
    {
        return $"booster_unlock_gift_{boosterType}_{unlockLevel}";
    }

    private static string GetTutorialShownKey(int boosterType, int unlockLevel)
    {
        return $"booster_unlock_tutorial_{boosterType}_{unlockLevel}";
    }
}
