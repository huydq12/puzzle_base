using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UIPopupRank : UIElement
{
    #region Constants
    private const int TOP_PLAYERS_COUNT = 3;
    private const int LEADERBOARD_DURATION_DAYS = 7;
    private const int RANK_CELL_HEIGHT = 238;
    private const float SCROLL_ANIMATION_DURATION = 0.5f;
    private const string SCORE_PREFIX = "SCORE: ";
    private const string END_TIME_PREFIX = "End time: ";
    private const string TIME_COLOR_HEX = "#FF0000";
    #endregion

    #region Item Type Names
    private static class ItemTypeNames
    {
        public const string COIN = "coin";
        public const string BOOSTER_TYPE_1 = "booster_type1";
        public const string BOOSTER_TYPE_2 = "booster_type2";
    }
    #endregion

    #region Serialized Fields
    [Header("Top Players Display")]
    [SerializeField] private List<UIRankDataPlayer> topPlayers;
    
    [Header("Booster Sprites")]
    [SerializeField] private Sprite boosterType1Sprite;
    [SerializeField] private Sprite boosterType2Sprite;

    [Header("Ranking List")]
    [SerializeField] private RankCell rankCellPrefab;
    [SerializeField] private RectTransform rankingListContainer;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Virtual Scrolling")]
    [SerializeField] private int poolSize = 12; // Chỉ tạo 12 items, reuse khi scroll

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI endTimeText;
    #endregion

    #region Properties
    public override bool ManualHide => true;
    public override bool DestroyOnHide => false;
    public override bool UseBehindPanel => false;
    #endregion

    #region Private Fields
    private List<RankingData> runtimeRankingData;
    private Dictionary<string, Action<int, UIRankDataPlayer>> rewardHandlers;
    private List<RankCell> pooledRankCells = new List<RankCell>();
    private bool isPoolInitialized = false;
    private float rankCellHeight = RANK_CELL_HEIGHT;
    private int lastFirstVisibleIndex = -1;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        InitializeRewardHandlers();
    }
    #endregion

    #region Public Methods
    public override void Show()
    {
        base.Show();
        
        InitializeRankingData();
        DisplayTopPlayers();
        UpdateEndTimeDisplay();
        SetupVirtualScrolling();
        ScrollToPlayerPosition();
    }

    public void ShowNoMove()
    {
        Show();
        DisableScroll();
    }

    public override void Hide()
    {
        if (scrollRect != null)
        {
            scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
        }
        base.Hide();
    }
    #endregion

    #region Initialization
    private void InitializeRewardHandlers()
    {
        rewardHandlers = new Dictionary<string, Action<int, UIRankDataPlayer>>
        {
            { ItemTypeNames.COIN, SetCoinReward },
            { ItemTypeNames.BOOSTER_TYPE_1, SetBoosterType1Reward },
            { ItemTypeNames.BOOSTER_TYPE_2, SetBoosterType2Reward }
        };
    }

    private void InitializeRankingData()
    {
        runtimeRankingData = new List<RankingData>();
        var rankConfigs = RankingManager.Instance.RankConfigs;
        
        if (!IsValidRankConfigs(rankConfigs)) return;

        PopulateRankingDataFromConfigs(rankConfigs);
        EnsureLeaderboardEndDateExists();
        UpdatePlayerRankByScore();
    }

    private bool IsValidRankConfigs(List<RankConfig> configs)
    {
        return configs != null && configs.Count > 0;
    }

    private void PopulateRankingDataFromConfigs(List<RankConfig> rankConfigs)
    {
        foreach (var config in rankConfigs)
        {
            string randomName = RankingManager.Instance.GetRandomName();
            runtimeRankingData.Add(new RankingData(
                config.rank,
                randomName,
                config.score,
                config.reward
            ));
        }
    }

    private void EnsureLeaderboardEndDateExists()
    {
        var userData = GameManager.Instance.userData;
        
        if (string.IsNullOrEmpty(userData.dateLeaderBoard))
        {
            userData.dateLeaderBoard = GetFutureDate(LEADERBOARD_DURATION_DAYS);
            userData.Save();
        }
    }

    private string GetFutureDate(int daysFromNow)
    {
        return DateTime.Now.AddDays(daysFromNow).ToString();
    }
    #endregion

    #region Player Rank Update
    private void UpdatePlayerRankByScore()
    {
        var userData = GameManager.Instance.userData;
        var rankConfig = RankingManager.Instance.GetRankByScore(userData.playerScore);
        
        if (rankConfig != null)
        {
            userData.playerRank = rankConfig.rank;
            userData.playerReward = rankConfig.reward;
        }
    }
    #endregion

    #region Top Players Display
    private void DisplayTopPlayers()
    {
        if (!CanDisplayTopPlayers()) return;

        for (int i = 0; i < TOP_PLAYERS_COUNT; i++)
        {
            UpdateTopPlayerUI(i);
        }
    }

    private bool CanDisplayTopPlayers()
    {
        return runtimeRankingData != null && 
               runtimeRankingData.Count >= TOP_PLAYERS_COUNT &&
               topPlayers != null && 
               topPlayers.Count >= TOP_PLAYERS_COUNT;
    }

    private void UpdateTopPlayerUI(int index)
    {
        var rankingData = runtimeRankingData[index];
        var playerUI = topPlayers[index];
        
        playerUI.UI_NamePlayer.text = rankingData.NamePlayer;
        playerUI.UI_Score.text = FormatScore(rankingData.Score);
        ParseAndApplyRewards(rankingData.Reward, playerUI);
    }

    private string FormatScore(int score)
    {
        return $"{SCORE_PREFIX}{score}";
    }
    #endregion

    #region Reward Parsing
    private void ParseAndApplyRewards(string rewardString, UIRankDataPlayer playerUI)
    {
        if (string.IsNullOrEmpty(rewardString)) return;

        var rewardItems = ParseRewardString(rewardString);
        ApplyRewardsToUI(rewardItems, playerUI);
    }

    private Dictionary<string, int> ParseRewardString(string rewardString)
    {
        var rewards = new Dictionary<string, int>();
        string[] items = rewardString.Split(';');

        foreach (string item in items)
        {
            if (TryParseRewardItem(item, out string itemName, out int quantity))
            {
                rewards[itemName] = quantity;
            }
        }

        return rewards;
    }

    private bool TryParseRewardItem(string item, out string itemName, out int quantity)
    {
        itemName = string.Empty;
        quantity = 0;

        string[] parts = item.Trim().Split(' ');
        
        if (parts.Length != 2) return false;
        if (!int.TryParse(parts[0], out quantity)) return false;
        
        itemName = parts[1].ToLower();
        return true;
    }

    private void ApplyRewardsToUI(Dictionary<string, int> rewards, UIRankDataPlayer playerUI)
    {
        foreach (var reward in rewards)
        {
            if (rewardHandlers.TryGetValue(reward.Key, out var handler))
            {
                handler?.Invoke(reward.Value, playerUI);
            }
        }
    }

    private void SetCoinReward(int amount, UIRankDataPlayer playerUI)
    {
        if (amount > 0)
        {
            playerUI.UI_Reward_Coin.text = amount.ToString();
        }
    }

    private void SetBoosterType1Reward(int amount, UIRankDataPlayer playerUI)
    {
        if (amount > 0)
        {
            playerUI.UI_Reward_Item_Coin.text = amount.ToString();
            playerUI.UI_Item.sprite = boosterType1Sprite;
        }
    }

    private void SetBoosterType2Reward(int amount, UIRankDataPlayer playerUI)
    {
        if (amount > 0)
        {
            playerUI.UI_Reward_Item_Coin.text = amount.ToString();
            playerUI.UI_Item.sprite = boosterType2Sprite;
        }
    }
    #endregion

    #region End Time Display
    private void UpdateEndTimeDisplay()
    {
        var leaderboardEndDate = GameManager.Instance.userData.dateLeaderBoard;
        
        if (!string.IsNullOrEmpty(leaderboardEndDate))
        {
            DisplayRemainingTime(leaderboardEndDate);
        }
    }

    private void DisplayRemainingTime(string endDateString)
    {
        if (!DateTime.TryParse(endDateString, out DateTime endDate))
        {
            Debug.LogError($"Invalid date format: {endDateString}");
            return;
        }

        TimeSpan remainingTime = endDate - DateTime.Now;
        string formattedTime = FormatRemainingTime(remainingTime);
        
        endTimeText.text = $"{END_TIME_PREFIX}{formattedTime}";
    }

    private string FormatRemainingTime(TimeSpan timeSpan)
    {
        int days = Mathf.Max(0, (int)timeSpan.TotalDays);
        int hours = Mathf.Max(0, timeSpan.Hours);
        int minutes = Mathf.Max(0, timeSpan.Minutes);

        return $"<color={TIME_COLOR_HEX}>{days:00}d{hours:00}h{minutes:00}m</color>";
    }
    #endregion

    #region Virtual Scrolling
    private void SetupVirtualScrolling()
    {
        if (!CanSetupVirtualScrolling()) return;

        DisableLayoutComponents();
        SetupContainerRect();
        CalculateCellHeight();
        SetContentHeight();
        EnsurePoolInitialized();
        
        // Setup scroll listener
        if (scrollRect != null)
        {
            scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
            scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        }

        // Initial refresh
        lastFirstVisibleIndex = -1;
        RefreshVisibleCells(force: true);
    }

    private bool CanSetupVirtualScrolling()
    {
        return runtimeRankingData != null && 
               rankCellPrefab != null && 
               rankingListContainer != null;
    }

    private void DisableLayoutComponents()
    {
        var vlg = rankingListContainer.GetComponent<VerticalLayoutGroup>();
        if (vlg != null) vlg.enabled = false;
        
        var fitter = rankingListContainer.GetComponent<ContentSizeFitter>();
        if (fitter != null) fitter.enabled = false;
        
        var hlg = rankingListContainer.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null) hlg.enabled = false;
    }

    private void SetupContainerRect()
    {
        rankingListContainer.anchorMin = new Vector2(0, 1);
        rankingListContainer.anchorMax = new Vector2(1, 1);
        rankingListContainer.pivot = new Vector2(0.5f, 1);
    }

    private void CalculateCellHeight()
    {
        var prefabRT = rankCellPrefab.GetComponent<RectTransform>();
        if (prefabRT != null && prefabRT.sizeDelta.y > 0)
        {
            rankCellHeight = prefabRT.sizeDelta.y;
        }
    }

    private void SetContentHeight()
    {
        int dataCount = Mathf.Max(0, runtimeRankingData.Count - TOP_PLAYERS_COUNT);
        float totalHeight = dataCount * rankCellHeight;
        rankingListContainer.sizeDelta = new Vector2(rankingListContainer.sizeDelta.x, totalHeight);
        rankingListContainer.anchoredPosition = Vector2.zero;
    }

    private void EnsurePoolInitialized()
    {
        if (isPoolInitialized) return;

        ClearPool();

        int dataCount = runtimeRankingData.Count - TOP_PLAYERS_COUNT;
        int createCount = Mathf.Min(poolSize, Mathf.Max(0, dataCount));
        
        // Tạo pool items (chỉ poolSize items thôi)
        for (int i = 0; i < createCount; i++)
        {
            RankCell rankCell = Instantiate(rankCellPrefab, rankingListContainer);
            var rt = rankCell.GetComponent<RectTransform>();
            
            // Setup anchoring
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            
            rankCell.gameObject.SetActive(false);
            pooledRankCells.Add(rankCell);
        }

        isPoolInitialized = true;
        Debug.Log($"Virtual Scrolling: Created pool of {createCount} items for {dataCount} total data items");
    }

    private void RefreshVisibleCells(bool force = false)
    {
        if (!isPoolInitialized || pooledRankCells.Count == 0) return;

        int dataCount = runtimeRankingData.Count - TOP_PLAYERS_COUNT;
        if (dataCount == 0)
        {
            HideAllPooledCells();
            return;
        }

        // Calculate first visible index based on scroll position
        float scrollY = rankingListContainer.anchoredPosition.y;
        int firstVisibleIndex = Mathf.Clamp(
            Mathf.FloorToInt(scrollY / Mathf.Max(1f, rankCellHeight)), 
            0, 
            Mathf.Max(0, dataCount - 1)
        );

        if (!force && firstVisibleIndex == lastFirstVisibleIndex) return;

        lastFirstVisibleIndex = firstVisibleIndex;

        var userData = GameManager.Instance.userData;
        int playerRankIndex = userData.playerRank - 1;

        // Reuse pooled cells
        for (int i = 0; i < pooledRankCells.Count; i++)
        {
            int dataIndex = firstVisibleIndex + i + TOP_PLAYERS_COUNT;
            var rankCell = pooledRankCells[i];
            var rt = rankCell.GetComponent<RectTransform>();

            if (dataIndex < runtimeRankingData.Count)
            {
                // Position cell theo data index thực
                float yPos = -(dataIndex - TOP_PLAYERS_COUNT) * rankCellHeight;
                rt.anchoredPosition = new Vector2(0, yPos);

                // Update data
                bool isPlayerRank = (dataIndex == playerRankIndex);
                if (isPlayerRank)
                {
                    SetupPlayerRankCell(rankCell, dataIndex);
                }
                else
                {
                    SetupOtherRankCell(rankCell, dataIndex);
                }

                if (!rankCell.gameObject.activeSelf)
                {
                    rankCell.gameObject.SetActive(true);
                }
            }
            else
            {
                if (rankCell.gameObject.activeSelf)
                {
                    rankCell.gameObject.SetActive(false);
                }
            }
        }
    }

    private void OnScrollValueChanged(Vector2 _)
    {
        RefreshVisibleCells();
    }

    private void HideAllPooledCells()
    {
        foreach (var cell in pooledRankCells)
        {
            if (cell.gameObject.activeSelf)
            {
                cell.gameObject.SetActive(false);
            }
        }
    }

    private void ClearPool()
    {
        foreach (var cell in pooledRankCells)
        {
            if (cell != null)
            {
                DestroyImmediate(cell.gameObject);
            }
        }
        pooledRankCells.Clear();
        isPoolInitialized = false;
    }

    private void SetupPlayerRankCell(RankCell rankCell, int index)
    {
        var userData = GameManager.Instance.userData;
        rankCell.SetupData(
            userData.playerRank,
            userData.playerName,
            userData.playerReward,
            userData.playerScore,
            true,
            index
        );
    }

    private void SetupOtherRankCell(RankCell rankCell, int index)
    {
        var data = runtimeRankingData[index];
        rankCell.SetupData(
            data.Rank,
            data.NamePlayer,
            data.Reward,
            data.Score,
            false,
            index
        );
    }
    #endregion

    #region Scroll Control
    private void ScrollToPlayerPosition()
    {
        if (rankingListContainer == null) return;

        int playerRank = GameManager.Instance.userData.playerRank;
        int dataIndex = playerRank - 1 - TOP_PLAYERS_COUNT; // Index trong data list (trừ top 3)
        
        if (dataIndex < 0) return; // Player trong top 3, không cần scroll

        // Calculate scroll position để player cell hiển thị ở giữa màn hình
        float targetScrollY = dataIndex * rankCellHeight;
        
        AnimateScrollToPosition(targetScrollY, onComplete: () => {
            RefreshVisibleCells(force: true);
        });
    }

    private void AnimateScrollToPosition(float positionY, System.Action onComplete = null)
    {
        rankingListContainer.DOAnchorPosY(positionY, SCROLL_ANIMATION_DURATION)
            .SetEase(Ease.OutCubic)
            .OnUpdate(() => RefreshVisibleCells())
            .OnComplete(() => onComplete?.Invoke());
    }

    private void DisableScroll()
    {
        if (scrollRect != null)
        {
            scrollRect.enabled = false;
        }
    }
    #endregion
}

#region Data Classes
[Serializable]
public class UIRankDataPlayer
{
    public TextMeshProUGUI UI_NamePlayer;
    public TextMeshProUGUI UI_Score;
    public TextMeshProUGUI UI_Reward_Coin;
    public TextMeshProUGUI UI_Reward_Item_Coin;
    public Image UI_Item;
}
#endregion


