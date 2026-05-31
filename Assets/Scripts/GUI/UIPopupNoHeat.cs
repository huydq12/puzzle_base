using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UIPopupNoHeat : UIPopup
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI txt_title;
    [SerializeField] private TextMeshProUGUI txt_message;
    [SerializeField] private TextMeshProUGUI txt_countdown;
    [SerializeField] private Button btn_close;
    [SerializeField] private Button btn_watchAd;
    [SerializeField] private Button btn_buyUnlimited;
    
    protected override void Start()
    {
        base.Start();
        if (btn_close != null)
            btn_close.onClick.AddListener(OnClickClose);
            
        if (btn_watchAd != null)
            btn_watchAd.onClick.AddListener(OnClickWatchAd);
            
        if (btn_buyUnlimited != null)
            btn_buyUnlimited.onClick.AddListener(OnClickBuyUnlimited);
    }
    
    public override void BeforeShow()
    {
        base.BeforeShow();
        UpdateDisplay();
        Game.Update.AddTask(UpdateCountdown);
    }
    
    public override void BeforeHide()
    {
        base.BeforeHide();
        Game.Update.RemoveTask(UpdateCountdown);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Game.Update.RemoveTask(UpdateCountdown);
    }
    
    private void UpdateDisplay()
    {
        if (txt_title != null)
            txt_title.text = "Out of Heat!";
            
        if (txt_message != null)
            txt_message.text = "You need Heat to play. Wait for it to regenerate or get unlimited Heat!";
    }
    
    private void UpdateCountdown()
    {
        if (txt_countdown == null) return;

        var heatManager = HeatManager.TryGetInstance();
        if (heatManager == null) return;

        TimeSpan timeUntilNext = heatManager.GetTimeUntilNextHeat();
        
        if (timeUntilNext.TotalSeconds > 0)
        {
            txt_countdown.text = $"Next Heat in: {timeUntilNext.Hours:D2}:{timeUntilNext.Minutes:D2}:{timeUntilNext.Seconds:D2}";
        }
        else
        {
            txt_countdown.text = "Heat available now!";
        }
    }
    
    private void OnClickClose()
    {
        UIManager.Instance.HideUI<UIPopupNoHeat>();
    }
    
    private void OnClickWatchAd()
    {
        UIManager.Instance.HideUI<UIPopupNoHeat>();
    }
    
    private void OnClickBuyUnlimited()
    {
        UIManager.Instance.HideUI<UIPopupNoHeat>();
    }
}
