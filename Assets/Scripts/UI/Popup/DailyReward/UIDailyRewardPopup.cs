using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils.Pattern;

public class UIDailyRewardPopup : BasePopup
{
    [Header("Items")]
    [SerializeField] private List<UIDailyRewardBundle> m_dailyRewardBundles;
    [SerializeField] private List<UIWeeklyRewardBundle> m_weeklyRewardBundles;

    [Header("Buttons")]
    [SerializeField] private Button m_tapBackButton;

    [Header("Sliders")]
    [SerializeField] private List<Image> m_sliderImages;
    [SerializeField] private List<TextMeshProUGUI> m_textDayToUnlockWeeklyList;
    [SerializeField] private TextMeshProUGUI m_streakText;
    [SerializeField] private List<GameObject> m_claimedWeeklyObjects;

    private DailyRewardConfigSO _dailyRewardConfigSO;

    private int _lastDailyCycle = -1;
    private int _lastWeeklyCycle = -1;

    private void Start()
    {
        Observer.Instance.AddObserver(ObserverTopic.UPDATE_DATA.ToString(), ListenUpdateData);
    }

    public override void Init()
    {
        if (_isInited) return;
        base.Init();

        _dailyRewardConfigSO = GameManagerInGame.Instance.dailyRewardConfigSO;

        m_tapBackButton.onClick.AddListener(OnBackButtonClick);

        UserData userData = GameManagerInGame.Instance.userData;
        DailyRewardHandler handler = userData.dailyRewardHandler;

        int globalState = handler.CheckRewardState();


        _lastDailyCycle = handler.totalClaims == 0 ? 0 : (handler.totalClaims - 1) / 7;
        _lastWeeklyCycle = handler.GetCurrentWeeklyCycle(_dailyRewardConfigSO.weeklyRewardDatas);

        SetupDailyUI(handler, globalState);
        SetUpWeeklyUI(handler);

        m_streakText.text = "STREAK " + handler.currentStreak;
    }

    public override void BeforeShow()
    {
        base.BeforeShow();
    }

    public void ListenUpdateData(object data)
    {
        if(data is UserData userData)
        {
            DailyRewardHandler handler = userData.dailyRewardHandler;

            _lastDailyCycle = handler.totalClaims == 0 ? 0 : (handler.totalClaims - 1) / 7;
            _lastWeeklyCycle = handler.GetCurrentWeeklyCycle(_dailyRewardConfigSO.weeklyRewardDatas);


            int globalState = handler.CheckRewardState();

            _lastDailyCycle = handler.totalClaims == 0 ? 0 : (handler.totalClaims - 1) / 7;
            _lastWeeklyCycle = handler.GetCurrentWeeklyCycle(_dailyRewardConfigSO.weeklyRewardDatas);

            SetupDailyUI(handler, globalState);
            SetUpWeeklyUI(handler);

            m_streakText.text = "STREAK " + handler.currentStreak;
        }
    }

    public void RefreshAllUI(UserData userData)
    {
        DailyRewardHandler handler = userData.dailyRewardHandler;
        int globalState = handler.CheckRewardState();


        int currentDailyCycle = handler.totalClaims == 0 ? 0 : (handler.totalClaims - 1) / 7;
        bool isDailyCycleChanged = (currentDailyCycle != _lastDailyCycle);

        if (isDailyCycleChanged)
        {
            _lastDailyCycle = currentDailyCycle;
            SetupDailyUI(handler, globalState);
        }
        else
        {
            for (int i = 0; i < m_dailyRewardBundles.Count; i++)
            {
                m_dailyRewardBundles[i].UpdateUI(globalState);
            }
        }

        int currentWeeklyCycle = handler.GetCurrentWeeklyCycle(_dailyRewardConfigSO.weeklyRewardDatas);
        bool isWeeklyCycleChanged = (currentWeeklyCycle != _lastWeeklyCycle);

        if (isWeeklyCycleChanged)
        {
            _lastWeeklyCycle = currentWeeklyCycle;
            SetUpWeeklyUI(handler);
        }
        else
        {
            for (int i = 0; i < m_weeklyRewardBundles.Count; i++)
            {
                m_weeklyRewardBundles[i].UpdateUI();
            }
        }
    }

    public void OnDailyClaim(UserData userData)
    {
        DailyRewardHandler handler = userData.dailyRewardHandler;
        DailyRewardConfigSO configSO = GameManagerInGame.Instance.dailyRewardConfigSO;

        List<WeeklyRewardData> autoClaimedWeeks = handler.CheckAndAutoClaimWeeklyRewards(configSO);

        for (int i = 0; i < autoClaimedWeeks.Count; i++)
        {
            var weekData = autoClaimedWeeks[i];
            for (int j = 0; j < weekData.items.Count; j++)
            {
                userData.ApplyShopItem(weekData.items[j]);
            }
        }

        userData.Save();
        int currentWeeklyCycle = handler.GetCurrentWeeklyCycle(_dailyRewardConfigSO.weeklyRewardDatas); 
        bool isWeeklyCycleChanged = (currentWeeklyCycle != _lastWeeklyCycle); 
        
        if (isWeeklyCycleChanged) 
        {
            _lastWeeklyCycle = currentWeeklyCycle; SetUpWeeklyUI(handler); 
        }
        else 
        {
            for (int i = 0; i < m_weeklyRewardBundles.Count; i++) 
            {
                m_weeklyRewardBundles[i].UpdateUI(); 
            } 
        }

        m_streakText.text = "STREAK " + handler.currentStreak;

        AnimateWeeklyProgressUI(handler);
    }

    public void SetUpWeeklyUI(DailyRewardHandler handler)
    {
        var weeklyConfigs = _dailyRewardConfigSO.weeklyRewardDatas;
        int maxConfigCount = weeklyConfigs.Count;
        if (maxConfigCount == 0) return;

        int totalCycleDays = handler.GetTotalWeeklyCycleDays(weeklyConfigs);
        int currentCycle = handler.GetCurrentWeeklyCycle(weeklyConfigs);

        for (int i = 0; i < 4; i++)
        {
            UIWeeklyRewardBundle bundle = m_weeklyRewardBundles[i];

            int configIndex = handler.GetWeeklyConfigIndex(i, maxConfigCount);
            WeeklyRewardData originalData = weeklyConfigs[configIndex];

            WeeklyRewardData virtualData = new WeeklyRewardData
            {
                items = originalData.items,
                unlockDay = originalData.unlockDay + (currentCycle * totalCycleDays)
            };

            bundle.Init(virtualData, handler, this);
        }

        UpdateWeeklyProgressUI(handler);
    }

    public void SetupDailyUI(DailyRewardHandler handler, int globalState)
    {
        int totalClaims = handler.totalClaims;
        int k = 1;
        if (handler.currentDay > handler.currentStreak) k = 0;

        int currentCycle = totalClaims == 0 ? 0 : (totalClaims - k) / 7;
        int currentCycleStartDay = currentCycle * 7;

        for (int i = 0; i < 7; i++)
        {
            UIDailyRewardBundle bundle = m_dailyRewardBundles[i];

            int displayDayNum = currentCycleStartDay + i + 1;

            int configIndex = handler.GetRollingConfigIndex(i, _dailyRewardConfigSO.dailyRewardDatas.Count);
            DailyRewardData data = _dailyRewardConfigSO.dailyRewardDatas[configIndex];

            bundle.Init(data, displayDayNum, i, handler, globalState, this);
        }
    }

    private void OnReviveClicked()
    {
        UserData userData = GameManagerInGame.Instance.userData;
        userData.dailyRewardHandler.ReviveStreak();
        userData.Save();

        RefreshAllUI(userData);
    }

    private void OnGiveUpClicked()
    {
        GameManagerInGame.Instance.userData.dailyRewardHandler.GiveUpStreak();
        GameManagerInGame.Instance.userData.Save();

        UserData userData = GameManagerInGame.Instance.userData;
        _lastDailyCycle = userData.dailyRewardHandler.totalClaims == 0 ? 0 : (userData.dailyRewardHandler.totalClaims - 1) / 7;
        _lastWeeklyCycle = userData.dailyRewardHandler.GetCurrentWeeklyCycle(_dailyRewardConfigSO.weeklyRewardDatas);

        SetupDailyUI(userData.dailyRewardHandler, userData.dailyRewardHandler.CheckRewardState());
        RefreshAllUI(userData);
    }

    private void UpdateWeeklyProgressUI(DailyRewardHandler handler)
    {
        var weeklyConfigs = _dailyRewardConfigSO.weeklyRewardDatas;
        if (weeklyConfigs == null || weeklyConfigs.Count == 0) return;

        int currentCycle = handler.GetCurrentWeeklyCycle(weeklyConfigs);
        int totalCycleDays = handler.GetTotalWeeklyCycleDays(weeklyConfigs);

        int baseDay = currentCycle * totalCycleDays;
        int currentDay = handler.totalClaims; // hoặc handler.currentDay tùy logic của bạn

        // ====== TEXT ======
        if (m_textDayToUnlockWeeklyList != null && m_textDayToUnlockWeeklyList.Count > 0)
        {
            // Text[0]
            m_textDayToUnlockWeeklyList[0].text = (baseDay + 1).ToString();

            int cumulative = 0;

            for (int i = 0; i < weeklyConfigs.Count; i++)
            {
                cumulative = weeklyConfigs[i].unlockDay;

                int index = i + 1;
                if (index < m_textDayToUnlockWeeklyList.Count)
                {
                    m_textDayToUnlockWeeklyList[index].text = (baseDay + cumulative).ToString();
                }
            }
        }

        // ====== SLIDER ======
        int prevUnlock = 0;

        for (int i = 0; i < m_sliderImages.Count; i++)
        {
            if (i >= weeklyConfigs.Count) break;

            int start = baseDay + prevUnlock + 1;
            int end = baseDay + weeklyConfigs[i].unlockDay;

            float fill = 0f;

            if (currentDay >= end)
            {
                fill = 1f;
            }
            else if (currentDay < start)
            {
                fill = 0f;
            }
            else
            {
                fill = (float)(currentDay - start + 1) / (end - start + 1);
            }

            m_sliderImages[i].fillAmount = fill;

            prevUnlock = weeklyConfigs[i].unlockDay;
        }

        UpdateClaimedWeeklyObjects(handler);
    }

    private List<Tween> _sliderTweens = new List<Tween>();

    private void AnimateWeeklyProgressUI(DailyRewardHandler handler)
    {
        var weeklyConfigs = _dailyRewardConfigSO.weeklyRewardDatas;
        if (weeklyConfigs == null || weeklyConfigs.Count == 0) return;

        int currentCycle = handler.GetCurrentWeeklyCycle(weeklyConfigs);
        int totalCycleDays = handler.GetTotalWeeklyCycleDays(weeklyConfigs);

        int baseDay = currentCycle * totalCycleDays;
        int currentDay = handler.totalClaims;

        // ===== Kill tween cũ =====
        for (int i = 0; i < _sliderTweens.Count; i++)
        {
            if (_sliderTweens[i] != null && _sliderTweens[i].IsActive())
                _sliderTweens[i].Kill();
        }
        _sliderTweens.Clear();

        // ===== Claimed index 0 =====
        if (m_claimedWeeklyObjects != null && m_claimedWeeklyObjects.Count > 0)
        {
            m_claimedWeeklyObjects[0].SetActive(handler.totalClaims > 0);
        }

        int prevUnlock = 0;

        for (int i = 0; i < m_sliderImages.Count; i++)
        {
            if (i >= weeklyConfigs.Count) break;

            int start = baseDay + prevUnlock + 1;
            int end = baseDay + weeklyConfigs[i].unlockDay;

            float target;

            if (currentDay >= end)
                target = 1f;
            else if (currentDay < start)
                target = 0f;
            else
                target = (float)(currentDay - start + 1) / (end - start + 1);

            Image img = m_sliderImages[i];

            int index = i; // capture để tránh bug closure

            Tween t = img.DOFillAmount(target, 0.5f)
                         .SetEase(Ease.OutCubic)

                         // 🔥 update realtime (mượt)
                         .OnUpdate(() =>
                         {
                             if (m_claimedWeeklyObjects == null) return;

                             int claimedIndex = index + 1;

                             if (claimedIndex < m_claimedWeeklyObjects.Count)
                             {
                                 bool isFull = img.fillAmount >= 0.999f;
                                 m_claimedWeeklyObjects[claimedIndex].SetActive(isFull);
                             }
                         })

                         // 🔥 đảm bảo đúng sau khi kết thúc
                         .OnComplete(() =>
                         {
                             if (m_claimedWeeklyObjects == null) return;

                             int claimedIndex = index + 1;

                             if (claimedIndex < m_claimedWeeklyObjects.Count)
                             {
                                 bool isFull = target >= 0.999f;
                                 m_claimedWeeklyObjects[claimedIndex].SetActive(isFull);
                             }
                         });

            _sliderTweens.Add(t);

            prevUnlock = weeklyConfigs[i].unlockDay;
        }
    }

    private void UpdateClaimedWeeklyObjects(DailyRewardHandler handler)
    {
        if (m_claimedWeeklyObjects == null || m_claimedWeeklyObjects.Count == 0) return;

        // ===== index 0 =====
        bool hasAnyClaim = handler.totalClaims > 0;
        m_claimedWeeklyObjects[0].SetActive(hasAnyClaim);

        // ===== index >= 1 =====
        for (int i = 1; i < m_claimedWeeklyObjects.Count; i++)
        {
            if (i - 1 >= m_sliderImages.Count) break;

            bool isFull = m_sliderImages[i - 1].fillAmount >= 0.999f;
            m_claimedWeeklyObjects[i].SetActive(isFull);
        }
    }

    private void OnBackButtonClick()
    {
        HideUI();
    }
}
