using System;
using System.Collections.Generic;
using UnityEngine;
using Utils.Pattern;

[Serializable]
public class UserData : SavePlayerPrefs
{
    public bool isDefaultData = false; 

    public string lastTimePlayGame = string.Empty;
    public string lastDailyResetTime = string.Empty;

    public int playerCash;

    public int playerHeat;
    public int playerDiamond;
    
    public bool hasUnlimitedHeat = false;
    public string unlimitedHeatExpireTime = string.Empty;
    
    public int playerHealth = 0;
    public bool hasClaimedFreeLoseRefill = false;
    public int boosterType1 = 0;
    public int boosterType2 = 0;
    public int boosterType3 = 0;
    public int boosterType4 = 0;

    public List<string> boosterUnlockGiftClaimedKeys = new();
    public List<string> boosterUnlockTutorialShownKeys = new();
    public List<string> boosterUnlockFreeUsedKeys = new();
    public List<int> claimedWinRewardLevels = new();

    public string playerName;

    public int maxLevel = 1;
    public int currentLevel = 1;

    public int currentMap;
    public bool hasUnlockMap;
    public bool soundOn;
    public bool musicOn;
    public bool vibrateOn;
    public bool removeAds;
    public bool greatdeal;
    
    public bool isResetQuest;
    public TutorialsData tutorials;

    public List<MapData> listMap;

    public bool isFirstClaimDailyReward;
    public bool isShowDailyReward;
    public string lastDailyRewardAutoShowDate = string.Empty;
    public DailyBonus dailyBonus = new();
    public DailyRewardHandler dailyRewardHandler = new();

    // Profile Data
    public int currentAvatarIndex = 0;
    public int currentFrameIndex = 0;
    public List<int> unlockedAvatars = new();
    public List<int> unlockedFrames = new();
    
    public List<string> purchasedPackIds = new();

    public int playerRank = 0;
    public int playerScore = 0;
    public string playerReward = string.Empty;
    public string dateLeaderBoard = string.Empty;

    public UserData()
    {
        soundOn = musicOn = vibrateOn = true;
        listMap = new List<MapData>();
    }
    public MapData mapData
    {
        get => listMap[currentMap - 1];
    }

    public void EnsureDailyRewardHandler()
    {
        if (dailyRewardHandler == null)
        {
            dailyRewardHandler = new DailyRewardHandler();
        }

        dailyRewardHandler.UpdateCurrentDay();
    }

    public void SetDefaultData()
    {
        isDefaultData = true;
        lastTimePlayGame = string.Empty;
        lastDailyResetTime = DateTime.Now.ToString();
        playerCash = 0;
        playerHeat = 5;
        playerDiamond = 0;
        hasUnlimitedHeat = false;
        unlimitedHeatExpireTime = string.Empty;
        playerHealth = 0;
        hasClaimedFreeLoseRefill = false;
        boosterType1 = 0;
        boosterType2 = 0;
        boosterType3 = 0;
        boosterType4 = 0;

        boosterUnlockGiftClaimedKeys = new List<string>();
        boosterUnlockTutorialShownKeys = new List<string>();
        boosterUnlockFreeUsedKeys = new List<string>();
        claimedWinRewardLevels = new List<int>();
        playerName = "Player"+UnityEngine.Random.Range(0,1000);
        maxLevel = 1;
        currentLevel = 1;
        currentMap = 0;
        hasUnlockMap = false;
        soundOn = true;
        musicOn = true;
        vibrateOn = true;
        removeAds = false;
        greatdeal = false;
        isResetQuest = false;
        tutorials = new TutorialsData();
        listMap = new List<MapData>();
        isFirstClaimDailyReward = false;
        isShowDailyReward = false;
        lastDailyRewardAutoShowDate = string.Empty;
        dailyBonus = new DailyBonus();
        dailyRewardHandler = new DailyRewardHandler();
        dailyRewardHandler.UpdateCurrentDay();
        currentAvatarIndex = 0;
        currentFrameIndex = 0;
        unlockedAvatars = new List<int> {0,1};
        unlockedFrames = new List<int> {0};
        purchasedPackIds = new List<string>();
        playerRank = 0;
        playerScore = 0;
        playerReward = string.Empty;
        dateLeaderBoard = string.Empty;
    }

    public void ApplyShopItem(Item item)
    {
        if (item == null) return;

        switch (item.ItemType)
        {
            case ItemType.Gold:
                playerCash += item.Quantity;
                break;
            case ItemType.Booster_Type1:
                boosterType1 += item.Quantity;
                break;
            case ItemType.Booster_Type2:
                boosterType2 += item.Quantity;
                break;
            case ItemType.Booster_Type3:
                boosterType3 += item.Quantity;
                break;
            case ItemType.InfiniteHealth:
                ApplyUnlimitedHeatHours(item.Quantity);
                break;
            case ItemType.NoAds:
                removeAds = true;
                break;
        }

        if (GameManagerInGame.Instance != null)
        {
            GameManagerInGame.Instance.UpdateValueData();
        }
    }

    private void ApplyUnlimitedHeatHours(int hours)
    {
        if (hours <= 0) return;

        DateTime now = DateTime.Now;
        DateTime currentExpire;
        DateTime baseTime = now;

        if (hasUnlimitedHeat &&
            DateTime.TryParse(unlimitedHeatExpireTime, out currentExpire) &&
            currentExpire > now)
        {
            baseTime = currentExpire;
        }

        hasUnlimitedHeat = true;
        unlimitedHeatExpireTime = baseTime.AddHours(hours).ToString();
    }

    
}

[Serializable]

public class DailyBonus
{
    public string dateTracking = DateTime.Now.ToString();
    public int currentIndex = -1;
}

[Serializable]
public class DailyRewardHandler
{
    public int currentDay;
    public int currentStreak;
    public int highestStreak;
    public int lastClaimedDay;
    public int totalClaims;

    public bool isMissedStreak;
    public int totalLostStreak;
    public int lastLoginDay;

    public List<int> claimedWeeklyMilestones = new();

    public void UpdateCurrentDay()
    {
        int today = GetTodayEpochDay();

        if (lastLoginDay <= 0)
        {
            lastLoginDay = today;
            currentDay = 1;
            return;
        }

        int delta = today - lastLoginDay;
        if (delta <= 0) return;

        currentDay = delta == 1 ? currentDay + 1 : 1;
        lastLoginDay = today;
    }

    public int CheckRewardState()
    {
        if (isMissedStreak) return 2;

        int todayEpoch = GetTodayEpochDay();
        if (lastClaimedDay == todayEpoch) return 1;
        if (lastClaimedDay == todayEpoch - 1 || lastClaimedDay <= 0) return 0;

        isMissedStreak = true;
        return 2;
    }

    public int ClaimReward()
    {
        if (CheckRewardState() != 0) return currentStreak;

        int claimedDayIndex = currentStreak;

        lastClaimedDay = GetTodayEpochDay();
        totalClaims++;
        currentStreak++;

        if (currentDay <= 0) currentDay = 1;
        if (currentStreak > highestStreak) highestStreak = currentStreak;

        return claimedDayIndex;
    }

    public void ReviveStreak()
    {
        isMissedStreak = false;
        lastClaimedDay = GetTodayEpochDay() - 1;
        totalLostStreak++;
    }

    public void GiveUpStreak()
    {
        isMissedStreak = false;
        currentStreak = 0;
        currentDay = 1;
        lastClaimedDay = GetTodayEpochDay() - 1;
        ResetWeeklyClaimedMilestones();

        if (Observer.Instance != null && GameManagerInGame.Instance != null)
        {
            Observer.Instance.Notify(ObserverTopic.UPDATE_DATA.ToString(), GameManagerInGame.Instance.userData);
        }
    }

    public int GetRollingConfigIndex(int slotIndex, int maxConfigCount)
    {
        if (maxConfigCount <= 0) return 0;
        int currentCycleStart = (totalClaims == 0 ? 0 : (totalClaims - 1) / 7) * 7;
        return (currentCycleStart + slotIndex) % maxConfigCount;
    }

    public bool IsWeeklyRewardClaimed(int unlockDay)
    {
        return claimedWeeklyMilestones.Contains(unlockDay);
    }

    public void ClaimWeeklyReward(int unlockDay)
    {
        if (!claimedWeeklyMilestones.Contains(unlockDay))
        {
            claimedWeeklyMilestones.Add(unlockDay);
        }
    }

    public void ResetWeeklyClaimedMilestones()
    {
        claimedWeeklyMilestones.Clear();
    }

    public int GetTotalWeeklyCycleDays(List<WeeklyRewardData> weeklyConfigs)
    {
        if (weeklyConfigs == null || weeklyConfigs.Count == 0) return 28;
        return weeklyConfigs[weeklyConfigs.Count - 1].unlockDay;
    }

    public int GetCurrentWeeklyCycle(List<WeeklyRewardData> weeklyConfigs)
    {
        int totalCycleDays = GetTotalWeeklyCycleDays(weeklyConfigs);
        if (totalCycleDays <= 0 || totalClaims == 0) return 0;

        int offset = currentDay > currentStreak ? 0 : 1;
        return (totalClaims - offset) / totalCycleDays;
    }

    public int GetWeeklyConfigIndex(int slotIndex, int maxConfigCount)
    {
        if (maxConfigCount <= 0) return 0;

        int currentCycleStart = (totalClaims == 0 ? 0 : (totalClaims - 1) / 28) * 4;
        return (currentCycleStart + slotIndex) % maxConfigCount;
    }

    public List<WeeklyRewardData> CheckAndAutoClaimWeeklyRewards(DailyRewardConfigSO configSO)
    {
        List<WeeklyRewardData> newlyClaimedBundles = new();
        if (configSO == null || configSO.weeklyRewardDatas == null || configSO.weeklyRewardDatas.Count == 0)
            return newlyClaimedBundles;

        List<WeeklyRewardData> weeklyConfigs = configSO.weeklyRewardDatas;
        int totalCycleDays = GetTotalWeeklyCycleDays(weeklyConfigs);
        int currentCycle = GetCurrentWeeklyCycle(weeklyConfigs);

        for (int i = 0; i < 4; i++)
        {
            int configIndex = GetWeeklyConfigIndex(i, weeklyConfigs.Count);
            WeeklyRewardData originalData = weeklyConfigs[configIndex];
            int virtualUnlockDay = originalData.unlockDay + currentCycle * totalCycleDays;

            if (totalClaims >= virtualUnlockDay && !IsWeeklyRewardClaimed(virtualUnlockDay))
            {
                ClaimWeeklyReward(virtualUnlockDay);
                newlyClaimedBundles.Add(originalData);
            }
        }

        return newlyClaimedBundles;
    }

    private int GetTodayEpochDay()
    {
        TimeSpan timeSinceEpoch = DateTime.Today - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return (int)timeSinceEpoch.TotalDays;
    }
}



[Serializable]
public class MapData
{
    public int id;
    public int idMap;
    public int mapExp;
    public int mapLevel;
    

    public MapData(int id)
    {
        idMap = id;
        mapLevel = 1;
    }
}


[Serializable]
public class TutorialsData
{
    public bool script2;
    public bool script3;
    public bool script4;
    public bool script5;
    public bool script6;
    public bool script8WakeUpDoctor;
    public bool script9;//het thuoc
    public bool script10;//het thuoc2
    public bool script11;//lay giay vs
    public bool script12;
    //public bool script12b;
    public bool script13;
    public bool script14;//anh da den be hom
    public bool script15;//atm
    public List<int> CompletedTutoS = new();
    public bool tutorialDropMoney;
    public bool hasCreateMoneyItemAtPlayerPos = false;
    public bool hasCreateSpeedItemAtPlayerPos = false;
    public bool hasFreeSpeedItem = false;
    //WareHouse show item
    public bool hasShowItemMedince;
    public bool hasShowItemToiletPaper;
    public bool hasShowItemThermometer;

    public bool hasActiceArrow;
}



[Serializable]
public class DataSession
{
    public float minutes;
    public bool completed;
    public bool status;

    public DataSession(float minutes=0, bool completed=false, bool status=false)
    {
        this.minutes = minutes;
        this.completed = completed;
        this.status = status;
    }
}

[Serializable]
public class RankingData
{
    public int Rank;
    public string NamePlayer;
    public int Score;
    public string Reward;

    public RankingData(int rank = 0, string namePlayer = "", int score = 0, string reward = "")
    {
        Rank = rank;
        NamePlayer = namePlayer;
        Score = score;
        Reward = reward;
    }
}
