using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RandomNameData
{
    public List<string> names;
}

[Serializable]
public class RankConfig
{
    public int rank;
    public int score;
    public string reward;
}

[Serializable]
public class RankConfigData
{
    public List<RankConfig> rankConfigs;
}

public class RankingManager : Singleton<RankingManager>
{
    private RandomNameData randomNameData;
    private RankConfigData rankConfigData;

    public List<string> RandomNames => randomNameData?.names;
    public List<RankConfig> RankConfigs => rankConfigData?.rankConfigs;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    private void LoadData()
    {
        TextAsset randomNameJson = Resources.Load<TextAsset>("Data/RandomName");
        if (randomNameJson != null)
        {
            randomNameData = JsonUtility.FromJson<RandomNameData>(randomNameJson.text);
        }

        TextAsset rankConfigJson = Resources.Load<TextAsset>("Data/RankConfigData");
        if (rankConfigJson != null)
        {
            rankConfigData = JsonUtility.FromJson<RankConfigData>(rankConfigJson.text);
        }
    }

    public string GetRandomName()
    {
        if (randomNameData?.names != null && randomNameData.names.Count > 0)
        {
            return randomNameData.names[UnityEngine.Random.Range(0, randomNameData.names.Count)];
        }
        return "Player";
    }

    public RankConfig GetRankConfig(int rank)
    {
        if (rankConfigData?.rankConfigs != null)
        {
            return rankConfigData.rankConfigs.Find(r => r.rank == rank);
        }
        return null;
    }

    public RankConfig GetRankByScore(int score)
    {
        if (rankConfigData?.rankConfigs != null)
        {
            for (int i = 0; i < rankConfigData.rankConfigs.Count; i++)
            {
                if (score >= rankConfigData.rankConfigs[i].score)
                {
                    return rankConfigData.rankConfigs[i];
                }
            }
        }
        return null;
    }

    public void UpdatePlayerScore(int newScore)
    {
        var userData = GameManager.Instance.userData;
        userData.playerScore = newScore;
        
        var rankConfig = GetRankByScore(newScore);
        if (rankConfig != null)
        {
            userData.playerRank = rankConfig.rank;
            userData.playerReward = rankConfig.reward;
        }
        
        userData.Save();
    }
}
