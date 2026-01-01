using System;
using System.Collections.Generic;
using UnityEngine;

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
    public int boosterType1 = 0;
    public int boosterType2 = 0;
    public int boosterType3 = 0;
    public int boosterType4 = 0;

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
    public DailyBonus dailyBonus = new();

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
        boosterType1 = 0;
        boosterType2 = 0;
        boosterType3 = 0;
        boosterType4 = 0;
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
        dailyBonus = new DailyBonus();
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

    
}

[Serializable]

public class DailyBonus
{
    public string dateTracking = DateTime.Now.ToString();
    public int currentIndex = -1;
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