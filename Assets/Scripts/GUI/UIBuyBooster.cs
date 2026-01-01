using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIBuyBooster : UIPopup
{
    public int RequestedBoosterType { get; private set; }

    [SerializeField] private TextMeshProUGUI txt_coin;

    [SerializeField] private Button btn_buy;
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

    [SerializeField] private int amountPerBuy = 1;

    protected override void Start()
    {
        base.Start();
        if (btn_buy != null)
        {
            btn_buy.onClick.AddListener(OnBuyClicked);
        }
    }

    public void ShowForBooster(int boosterType)
    {
        RequestedBoosterType = Mathf.Clamp(boosterType, 1, 4);
        Show();
    }

    public override void Show()
    {
        base.Show();
        RefreshView();
    }

    private void RefreshView()
    {
        int price = GetPrice();

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
            btn_buy.interactable = InventoryManager.Instance != null && InventoryManager.Instance.HasEnoughCoin(price);
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

    private void OnBuyClicked()
    {
        if (InventoryManager.Instance == null)
        {
            return;
        }

        int price = GetPrice();
        if (!InventoryManager.Instance.SpendCoin(price))
        {
            GameUI.Instance.Get<UINotification>().ShowToast("Not enough gold");
            RefreshView();
            return;
        }

        switch (RequestedBoosterType)
        {
            case 1:
                InventoryManager.Instance.AddBoosterType1(amountPerBuy);
                break;
            case 2:
                InventoryManager.Instance.AddBoosterType2(amountPerBuy);
                break;
            case 3:
                InventoryManager.Instance.AddBoosterType3(amountPerBuy);
                break;
            case 4:
                InventoryManager.Instance.AddBoosterType4(amountPerBuy);
                break;
        }

        GameUI.Instance.Get<UINotification>().ShowToast("Booster bought");

        var bottomInGame = GameUI.Instance.Get<UIBottomInGame>();
        if (bottomInGame != null)
        {
            bottomInGame.RefreshBoosterQuantity();
        }

        RefreshView();

        Hide();
    }
}
