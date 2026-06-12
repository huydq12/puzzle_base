using UnityEngine;
using TMPro;
using UnityEngine.UI;
using AZUR;

public class UIBuyBooster : UIPopup
{
    public int RequestedBoosterType { get; private set; }

    [SerializeField] private TextMeshProUGUI txt_coin;
    [SerializeField] private ButtonBehavior btn_close;

    [SerializeField] private ButtonBehavior btn_buy;
    [SerializeField] private ButtonBehavior btn_watchAdsBuy;
    [SerializeField] private TextMeshProUGUI txt_title;
    [SerializeField] private TextMeshProUGUI txt_price;
    [SerializeField] private Image img_booster;

    [SerializeField] private string titleBooster1 = "Booster 1";
    [SerializeField] private string titleBooster2 = "Booster 2";
    [SerializeField] private string titleBooster3 = "Booster 3";
    [SerializeField] private string titleBooster4 = "Booster 4";

    [SerializeField] private Sprite iconBooster1;
    [SerializeField] private Sprite iconBooster2;
    [SerializeField] private Sprite iconBooster3;
    [SerializeField] private Sprite iconBooster4;

    [SerializeField] private int priceBooster1 = 100;
    [SerializeField] private int priceBooster2 = 150;
    [SerializeField] private int priceBooster3 = 200;
    [SerializeField] private int priceBooster4 = 250;


    [SerializeField] private TextMeshProUGUI txt_amountPerBuy;
    [SerializeField] private int amountPerBuy = 1;

    protected override void Start()
    {
        base.Start();
        if (btn_close != null)
        {
            btn_close.OnClick.AddListener(OnCloseClicked);
        }

        if (btn_buy != null)
        {
            btn_buy.OnClick.AddListener(OnBuyClicked);
        }

        if (btn_watchAdsBuy != null)
        {
            btn_watchAdsBuy.OnClick.AddListener(OnWatchAdsBuyClicked);
        }
    }

    public void ShowForBooster(int boosterType)
    {
        RequestedBoosterType = Mathf.Clamp(boosterType, 1, 4);
        UIManager.Instance.ShowUI<UIBuyBooster>();
    }

    public override void BeforeShow()
    {
        base.BeforeShow();
        RefreshView();
    }

    private void RefreshView()
    {
        int price = GetTotalPrice();

        if (txt_amountPerBuy != null)
            txt_amountPerBuy.text = "x" + GetAmountPerBuy();

        if (txt_coin != null)
        {
            txt_coin.text = InventoryManager.Instance != null ? InventoryManager.Instance.GetCoin().ToString() : "0";
        }

        if (txt_title != null)
        {
            txt_title.text = GetTitle();
        }

        if (txt_price != null)
        {
            txt_price.text = price.ToString();
        }

        if (btn_buy != null)
        {
            bool canBuy = InventoryManager.Instance != null && InventoryManager.Instance.HasEnoughCoin(price);
            // btn_buy.SetInteractable(canBuy);
        }

        if (img_booster != null)
        {
            var sp = GetIcon();
            img_booster.sprite = sp;
            img_booster.enabled = sp != null;
        }
    }

    private string GetTitle()
    {
        return RequestedBoosterType switch
        {
            1 => titleBooster1,
            2 => titleBooster2,
            3 => titleBooster3,
            4 => titleBooster4,
            _ => titleBooster1
        };
    }

    private Sprite GetIcon()
    {
        return RequestedBoosterType switch
        {
            1 => iconBooster1,
            2 => iconBooster2,
            3 => iconBooster3,
            4 => iconBooster4,
            _ => iconBooster1
        };
    }

    private int GetPrice()
    {
        return RequestedBoosterType switch
        {
            1 => priceBooster1,
            2 => priceBooster2,
            3 => priceBooster3,
            4 => priceBooster4,
            _ => priceBooster1
        };
    }

    private int GetAmountPerBuy()
    {
        return Mathf.Max(1, amountPerBuy);
    }

    private int GetTotalPrice()
    {
        return GetPrice() * GetAmountPerBuy();
    }

    private void OnBuyClicked()
    {
        if (InventoryManager.Instance == null)
        {
            return;
        }

        int price = GetTotalPrice();
        if (!InventoryManager.Instance.SpendCoin(price))
        {
            UIManager.Instance.Get<UINotification>().ShowToast("Not enough gold");
            RefreshView();
            return;
        }

        AddRequestedBooster();

        UIManager.Instance.Get<UINotification>().ShowToast("Booster bought");

        RefreshBottomBoosterUI();

        RefreshView();

        UIManager.Instance.HideUI<UIBuyBooster>();
    }

    private void OnWatchAdsBuyClicked()
    {
        if (InventoryManager.Instance == null)
            return;

        const string placement = "ui_buy_booster_reward";
        var toast = UIManager.Instance.Get<UINotification>();
        toast?.HideNow();
        AnalyticsBridge.OnRewardedAdRequested(placement);
        bool shown = AzurAds.ShowRewarded(
            onRewardGranted: () =>
            {
                AnalyticsBridge.OnRewardedAdRewardGranted(placement);
                AddRequestedBooster();

                if (toast != null)
                    toast.ShowToast("Booster received");

                RefreshBottomBoosterUI();
                RefreshView();
                UIManager.Instance.HideUI<UIBuyBooster>();
            },
            placement: placement,
            onClosedWithoutGrant: () =>
            {
                AnalyticsBridge.OnRewardedAdClosedWithoutGrant(placement);
                toast?.ShowRewardNotGrantedToast();
            });

        if (!shown)
        {
            AnalyticsBridge.OnRewardedAdUnavailable(placement);
            toast?.ShowRewardedUnavailableToast();
        }
    }

    private void AddRequestedBooster()
    {
        int amount = GetAmountPerBuy();
        switch (RequestedBoosterType)
        {
            case 1:
                InventoryManager.Instance.AddBoosterType1(amount);
                break;
            case 2:
                InventoryManager.Instance.AddBoosterType2(amount);
                break;
            case 3:
                InventoryManager.Instance.AddBoosterType3(amount);
                break;
            case 4:
                InventoryManager.Instance.AddBoosterType4(amount);
                break;
        }
    }

    private void RefreshBottomBoosterUI()
    {
        var bottomInGame = UIManager.Instance.Get<UIBottomInGame>();
        if (bottomInGame != null)
            bottomInGame.RefreshBoosterQuantity();
    }

    private void OnCloseClicked()
    {
        UIManager.Instance.HideUI<UIBuyBooster>();
    }
}
