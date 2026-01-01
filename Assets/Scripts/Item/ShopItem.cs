using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ShopItem : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private Button buyButton;
    [SerializeField] private GameObject coinIcon;
    [SerializeField] private GameObject diamondIcon;
    [SerializeField] private GameObject iapIcon;
    [SerializeField] private GameObject specialOfferBadge;
    [SerializeField] private TextMeshProUGUI discountText;
    [SerializeField] private GameObject limitBadge;
    [SerializeField] private TextMeshProUGUI limitText;

    private ShopPackData packData;
    private Action<ShopPackData> onPurchaseCallback;

    public void Setup(ShopPackData data, Action<ShopPackData> onPurchase)
    {
        packData = data;
        onPurchaseCallback = onPurchase;

        if (iconImage != null && data.icon != null)
            iconImage.sprite = data.icon;

        if (nameText != null)
            nameText.text = data.displayName;

        SetupPrice();
        SetupReward();
        SetupSpecialOffer();
        SetupLimit();

        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyClicked);
    }

    private void SetupPrice()
    {
        if (coinIcon != null) coinIcon.SetActive(false);
        if (diamondIcon != null) diamondIcon.SetActive(false);
        if (iapIcon != null) iapIcon.SetActive(false);

        switch (packData.currencyType)
        {
            case ShopCurrency.Coin:
                if (coinIcon != null) coinIcon.SetActive(true);
                if (priceText != null) priceText.text = packData.priceCoin.ToString();
                break;

            case ShopCurrency.Diamond:
                if (diamondIcon != null) diamondIcon.SetActive(true);
                if (priceText != null) priceText.text = packData.priceDiamond.ToString();
                break;

            case ShopCurrency.IAP:
                if (iapIcon != null) iapIcon.SetActive(true);
                if (priceText != null) priceText.text = packData.priceIAP;
                break;
        }
    }

    private void SetupReward()
    {
        if (rewardText == null || packData.reward == null || packData.reward.items == null)
            return;

        string rewardDescription = "";
        foreach (var item in packData.reward.items)
        {
            if (!string.IsNullOrEmpty(rewardDescription))
                rewardDescription += "\n";

            rewardDescription += GetRewardDescription(item);
        }

        rewardText.text = rewardDescription;
    }

    private string GetRewardDescription(RewardEntry entry)
    {
        string itemName = entry.rewardType switch
        {
            RewardType.Coin => "Coin",
            RewardType.Health => "Health",
            RewardType.BoosterType1 => "Booster Type 1",
            RewardType.BoosterType2 => "Booster Type 2",
            RewardType.BoosterType3 => "Booster Type 3",
            _ => ""
        };

        return $"+{entry.amount} {itemName}";
    }

    private void SetupSpecialOffer()
    {
        if (specialOfferBadge != null)
            specialOfferBadge.SetActive(packData.isSpecialOffer);

        if (discountText != null && packData.isSpecialOffer)
            discountText.text = $"-{packData.discountPercent}%";
    }

    private void SetupLimit()
    {
        if (limitBadge != null)
            limitBadge.SetActive(packData.limit != PurchaseLimit.None);

        if (limitText != null && packData.limit != PurchaseLimit.None)
        {
            limitText.text = packData.limit switch
            {
                PurchaseLimit.Daily => "Daily",
                PurchaseLimit.Weekly => "Weekly",
                PurchaseLimit.OneTime => "One Time",
                _ => ""
            };
        }
    }

    private void OnBuyClicked()
    {
        onPurchaseCallback?.Invoke(packData);
    }

    private void OnDestroy()
    {
        if (buyButton != null)
            buyButton.onClick.RemoveListener(OnBuyClicked);
    }
}
