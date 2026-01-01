using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using System;

public class UIHome : UIElement
{
    public override bool ManualHide => true;
    public override bool DestroyOnHide => false;
    public override bool UseBehindPanel => false;


    [Header("Top Bar Buttons")]
    [SerializeField] private Button btn_profile;
    [SerializeField] private Button btn_setting;
    [SerializeField] private Button btn_noAds;
    [SerializeField] private Button btn_specialDeal;
    [SerializeField] private Button btn_dailyReward;

    [Header("Top Bar")]
    [SerializeField] private TextMeshProUGUI txt_playerCash;
    [SerializeField] private TextMeshProUGUI txt_playerHeat;
    [SerializeField] private TextMeshProUGUI txt_heatCountdown;
    [SerializeField] private GameObject obj_unlimitedHeatIcon;

    [Header("Home Page")]

    [SerializeField] private ItemLevelHome itemLevelHome;

    [SerializeField] private Transform parentLevelHome;

    [SerializeField] private int MaxLevel = 10;

    private void Start()
    {
        btn_profile.onClick.AddListener(OnClickProfile);
        btn_setting.onClick.AddListener(OnClickSetting);
        btn_noAds.onClick.AddListener(OnClickNoAds);
        btn_specialDeal.onClick.AddListener(OnClickSpecialDeal);
        btn_dailyReward.onClick.AddListener(OnClickDailyReward);

        var heatManager = HeatManager.TryGetInstance();
        if (heatManager != null)
        {
            heatManager.OnHeatChanged += UpdateHeatDisplay;
            heatManager.OnUnlimitedHeatChanged += UpdateHeatDisplay;
        }
    }

    private void OnClickNoAds()
    {
        GameUI.Instance.Get<UINoAds>().Show();
    }

    private void OnClickSpecialDeal()
    {
        GameUI.Instance.Get<UISpecialDeal>().Show();
    }

    private void OnClickDailyReward()
    {
        GameUI.Instance.Get<UIDailyReward>().Show();
    }
    
    private void OnDestroy()
    {
        Game.Update.RemoveTask(UpdateHeatCountdown);

        var heatManager = HeatManager.TryGetInstance();
        if (heatManager != null)
        {
            heatManager.OnHeatChanged -= UpdateHeatDisplay;
            heatManager.OnUnlimitedHeatChanged -= UpdateHeatDisplay;
        }
    }

    private void OnClickSetting()
    {
        GameUI.Instance.Get<UISettingHome>().Show();
    }

    private void OnClickProfile()
    {
        GameUI.Instance.Get<UIProfile>().Show();
    }

    public override void Show()
    {
        base.Show();
        UpdateCash(GameManager.Instance.userData.playerCash);
        UpdateHeatDisplay();
        UpdateLevelHome();
        Game.Update.AddTask(UpdateHeatCountdown);
    }
    
    public override void Hide()
    {
        base.Hide();
        Game.Update.RemoveTask(UpdateHeatCountdown);
    }

    public void UpdateLevelHome()
    {
        for (int i = 1; i <= MaxLevel; i++)
        {
            ItemLevelHome item = Instantiate(itemLevelHome, parentLevelHome);
        }
    }



    public void UpdateCash(int cash)
    {
        txt_playerCash.text = cash.ToString();
    }

    public void UpdateHeatDisplay()
    {
        var heatManager = HeatManager.TryGetInstance();
        if (heatManager == null) return;

        bool hasUnlimited = heatManager.HasUnlimitedHeat();

        if (hasUnlimited)
        {
            txt_playerHeat.text = "∞";
            if (obj_unlimitedHeatIcon != null)
                obj_unlimitedHeatIcon.SetActive(true);
        }
        else
        {
            int currentHeat = heatManager.GetCurrentHeat();
            txt_playerHeat.text = $"{currentHeat}";
            if (obj_unlimitedHeatIcon != null)
                obj_unlimitedHeatIcon.SetActive(false);
        }
        
        UpdateHeatCountdown();
    }
    
    private void UpdateHeatCountdown()
    {
        if (txt_heatCountdown == null) return;

        var heatManager = HeatManager.TryGetInstance();
        if (heatManager == null) return;

        if (heatManager.HasUnlimitedHeat())
        {
            TimeSpan remaining = heatManager.GetUnlimitedHeatTimeRemaining();
            if (remaining.TotalSeconds > 0)
            {
                if (remaining.TotalHours >= 24)
                {
                    int days = (int)remaining.TotalDays;
                    txt_heatCountdown.text = $"{days}d {remaining.Hours}h";
                }
                else
                {
                    txt_heatCountdown.text = $"{remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
                }
            }
            else
            {
                txt_heatCountdown.text = "Unlimited Heat";
            }
        }
        else
        {
            int currentHeat = heatManager.GetCurrentHeat();
            if (currentHeat >= HeatManager.MAX_HEAT_DAY)
            {
                txt_heatCountdown.text = "Full!";
            }
            else
            {
                TimeSpan timeUntilNext = heatManager.GetTimeUntilNextHeat();
                if (timeUntilNext.TotalSeconds > 0)
                {
                    txt_heatCountdown.text = $"Next: {timeUntilNext.Hours:D2}:{timeUntilNext.Minutes:D2}:{timeUntilNext.Seconds:D2}";
                }
                else
                {
                    txt_heatCountdown.text = "Ready!";
                }
            }
        }
    }
    
    public void AddUnlimitedHeat(float hours)
    {
        var heatManager = HeatManager.TryGetInstance();
        if (heatManager == null) return;

        heatManager.AddUnlimitedHeat(hours);
        UpdateHeatDisplay();
    }
}
