using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DailyRewardConfigSO", menuName = "ScriptableObjects/DailyRewardConfigSO", order = 10)]
public class DailyRewardConfigSO : ScriptableObject
{
    public List<DailyRewardData> dailyRewardDatas = new();
    public List<WeeklyRewardData> weeklyRewardDatas = new();
}

[Serializable]
public class DailyRewardData
{
    public List<Item> items = new();
}

[Serializable]
public class WeeklyRewardData
{
    public int unlockDay;
    public List<Item> items = new();
}

public enum ItemType
{
    Gold,
    Booster_Type1,
    Booster_Type2,
    Booster_Type3,
    InfiniteHealth,
    NoAds,
}

[Serializable]
public class Item
{
    public ItemType ItemType;
    public int Quantity;
}

public enum ObserverTopic
{
    UPDATE_DATA
}
