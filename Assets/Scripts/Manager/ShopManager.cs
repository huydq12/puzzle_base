using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[Serializable]
public class ShopCategoryUI
{
    public TextMeshProUGUI displayName;
    public Image icon;
    public List<ShopPackUI> shopPacks;
}

[Serializable]
public class ShopPackUI
{
    public TextMeshProUGUI displayName;
    public Image icon;
    public TextMeshProUGUI priceText; 
    public List<ShopItemData> shopItems;
    public TextMeshProUGUI discountText;
    public Button buyButton;
}

[Serializable]
public class ShopItemData
{
    public RewardType rewardType;
    public TextMeshProUGUI amount;
}

public class ShopManager : Singleton<ShopManager>
{
    private Dictionary<string, int> purchaseHistory = new Dictionary<string, int>();
    private Dictionary<string, DateTime> lastPurchaseTime = new Dictionary<string, DateTime>();

    [SerializeField] private ShopCatalog shopCatalog;

    [SerializeField] private List<ShopCategoryUI> shopCategories;

    private void Start()
    {
        InitializeShop();
    }

    private void InitializeShop()
    {
        if (shopCatalog == null || shopCatalog.categories == null)
        {
            Debug.LogError("ShopCatalog is not assigned or has no categories");
            return;
        }

        if (shopCategories == null || shopCategories.Count != shopCatalog.categories.Count)
        {
            Debug.LogError($"ShopCategoryUI count ({shopCategories?.Count ?? 0}) does not match ShopCatalog categories count ({shopCatalog.categories.Count})");
            return;
        }

        for (int i = 0; i < shopCatalog.categories.Count; i++)
        {
            var categoryData = shopCatalog.categories[i];
            var categoryUI = shopCategories[i];

            if (categoryUI.displayName != null)
                categoryUI.displayName.text = categoryData.categoryName;

            if (categoryUI.icon != null && categoryData.categoryBgIcon != null)
                categoryUI.icon.sprite = categoryData.categoryBgIcon;

            if (categoryUI.icon != null)
                categoryUI.icon.color = categoryData.categoryBgColor;

            MapShopPacks(categoryData.packs, categoryUI.shopPacks);
        }
    }

    private void MapShopPacks(List<ShopPackData> packDataList, List<ShopPackUI> packUIList)
    {
        if (packDataList == null || packUIList == null)
            return;

        if (packDataList.Count != packUIList.Count)
        {
            Debug.LogWarning($"Pack count mismatch: Data={packDataList.Count}, UI={packUIList.Count}");
            return;
        }

        var userData = GameManager.Instance.userData;
        
        for (int i = 0; i < packDataList.Count; i++)
        {
            var packData = packDataList[i];
            var packUI = packUIList[i];

            if (packData.limit == PurchaseLimit.OneTime && userData.purchasedPackIds.Contains(packData.id))
            {
                packUI.buyButton?.gameObject.SetActive(false);
                continue;
            }
            
            packUI.buyButton?.gameObject.SetActive(true);

            if (packUI.displayName != null)
                packUI.displayName.text = packData.displayName;

            if (packUI.icon != null && packData.icon != null)
                packUI.icon.sprite = packData.icon;

            if (packUI.priceText != null)
            {
                string priceDisplay = packData.currencyType switch
                {
                    ShopCurrency.Coin => packData.priceCoin.ToString(),
                    ShopCurrency.Diamond => packData.priceDiamond.ToString(),
                    ShopCurrency.IAP => packData.priceIAP,
                    _ => "N/A"
                };
                packUI.priceText.text = priceDisplay;
            }

            if (packUI.discountText != null)
            {
                if (packData.isSpecialOffer && packData.discountPercent > 0)
                {
                    packUI.discountText.text = $"-{packData.discountPercent}%";
                    packUI.discountText.gameObject.SetActive(true);
                }
                else
                {
                    packUI.discountText.gameObject.SetActive(false);
                }
            }

            MapRewardItems(packData.reward, packUI.shopItems);

            if (packUI.buyButton != null)
            {
                packUI.buyButton.onClick.RemoveAllListeners();
                packUI.buyButton.onClick.AddListener(() => OnPackPurchaseClicked(packData));
            }
        }
    }

    private void MapRewardItems(RewardPayload rewardPayload, List<ShopItemData> itemUIList)
    {
        if (rewardPayload == null || rewardPayload.items == null || itemUIList == null)
            return;

        if (rewardPayload.items.Count != itemUIList.Count)
        {
            Debug.LogWarning($"Reward item count mismatch: Name {rewardPayload.items[0].rewardType} Data={rewardPayload.items.Count}, UI={itemUIList.Count}");
            return;
        }

        for (int i = 0; i < rewardPayload.items.Count; i++)
        {
            var rewardEntry = rewardPayload.items[i];
            var itemUI = itemUIList[i];

            itemUI.rewardType = rewardEntry.rewardType;

            if (itemUI.amount != null)
            
               switch (rewardEntry.rewardType)
                {
                    case RewardType.Coin:
                        itemUI.amount.text = rewardEntry.amount.ToString();
                        break;

                    case RewardType.Health:
                        itemUI.amount.text = rewardEntry.amount.ToString()+"h";
                        break;

                    case RewardType.BoosterType1:
                        itemUI.amount.text = "x"+rewardEntry.amount.ToString();
                        break;

                    case RewardType.BoosterType2:
                        itemUI.amount.text = "x"+rewardEntry.amount.ToString();
                        break;

                    case RewardType.BoosterType3:
                        itemUI.amount.text = "x"+rewardEntry.amount.ToString();
                        break;

                    case RewardType.Avatar:
                        itemUI.amount.text = rewardEntry.amount.ToString();
                        break;

                    case RewardType.Frame:
                        itemUI.amount.text = rewardEntry.amount.ToString();
                        break;
                }
        }
    }

    private void OnPackPurchaseClicked(ShopPackData packData)
    {
        PurchasePack(packData, 
            onSuccess: () => 
            {
                Debug.Log($"Successfully purchased: {packData.displayName}");
                
                if (packData.limit == PurchaseLimit.OneTime)
                {
                    var userData = GameManager.Instance.userData;
                    if (!userData.purchasedPackIds.Contains(packData.id))
                    {
                        userData.purchasedPackIds.Add(packData.id);
                        userData.Save();
                    }
                }
                
                InitializeShop();
            },
            onFailed: (error) => 
            {
                Debug.LogError($"Purchase failed: {error}");
            }
        );
    }

    public void PurchasePack(ShopPackData pack, Action onSuccess, Action<string> onFailed)
    {
        if (!CanPurchase(pack, out string errorMessage))
        {
            onFailed?.Invoke(errorMessage);
            return;
        }

        switch (pack.currencyType)
        {
            case ShopCurrency.Coin:
                PurchaseWithCoin(pack, onSuccess, onFailed);
                break;

            case ShopCurrency.Diamond:
                PurchaseWithDiamond(pack, onSuccess, onFailed);
                break;

            case ShopCurrency.IAP:
                PurchaseWithIAP(pack, onSuccess, onFailed);
                break;

            default:
                onFailed?.Invoke("Invalid currency type");
                break;
        }
    }

    private bool CanPurchase(ShopPackData pack, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (pack.limit != PurchaseLimit.None)
        {
            if (!CheckPurchaseLimit(pack))
            {
                errorMessage = $"Purchase limit reached for {pack.limit}";
                return false;
            }
        }

        return true;
    }

    private bool CheckPurchaseLimit(ShopPackData pack)
    {
        if (!lastPurchaseTime.ContainsKey(pack.id))
            return true;

        DateTime lastPurchase = lastPurchaseTime[pack.id];
        DateTime now = DateTime.Now;

        switch (pack.limit)
        {
            case PurchaseLimit.Daily:
                return (now - lastPurchase).TotalDays >= 1;

            case PurchaseLimit.Weekly:
                return (now - lastPurchase).TotalDays >= 7;

            case PurchaseLimit.OneTime:
                return !purchaseHistory.ContainsKey(pack.id);

            default:
                return true;
        }
    }

    private void PurchaseWithCoin(ShopPackData pack, Action onSuccess, Action<string> onFailed)
    {
        var userData = GameManager.Instance.userData;

        if (userData.playerCash < pack.priceCoin)
        {
            onFailed?.Invoke("Not enough coins");
            return;
        }

        userData.playerCash -= pack.priceCoin;
        GiveRewards(pack.reward);
        RecordPurchase(pack);
        GameManager.Instance.userData.Save();
        onSuccess?.Invoke();
    }

    private void PurchaseWithDiamond(ShopPackData pack, Action onSuccess, Action<string> onFailed)
    {
        var userData = GameManager.Instance.userData;

        if (userData.playerDiamond < pack.priceDiamond)
        {
            onFailed?.Invoke("Not enough diamonds");
            return;
        }

        userData.playerDiamond -= pack.priceDiamond;
        GiveRewards(pack.reward);
        RecordPurchase(pack);
        GameManager.Instance.userData.Save();
        onSuccess?.Invoke();
    }

    private void PurchaseWithIAP(ShopPackData pack, Action onSuccess, Action<string> onFailed)
    {
        Debug.Log($"Initiating IAP purchase for: {pack.priceIAPId}");
        GiveRewards(pack.reward);
        RecordPurchase(pack);
        GameManager.Instance.UpdateValueData();
        GameUI.Instance.Get<UIShop>().UpdateCash(GameManager.Instance.userData.playerCash);
        GameManager.Instance.userData.Save();
        onSuccess?.Invoke();
    }

    private void GiveRewards(RewardPayload reward)
    {
        if (reward == null || reward.items == null)
            return;

        var userData = GameManager.Instance.userData;

        foreach (var item in reward.items)
        {
            switch (item.rewardType)
            {
                case RewardType.Coin:
                    userData.playerCash += item.amount;
                    break;

                case RewardType.Health:
                    HeatManager.Instance.AddUnlimitedHeat(item.amount);
                    break;

                case RewardType.BoosterType1:
                    userData.boosterType1 += item.amount;
                    break;

                case RewardType.BoosterType2:
                    userData.boosterType2 += item.amount;
                    break;

                case RewardType.BoosterType3:
                    userData.boosterType3 += item.amount;
                    break;

                case RewardType.Avatar:
                    if (!userData.unlockedAvatars.Contains(item.amount))
                        userData.unlockedAvatars.Add(item.amount);
                    break;

                case RewardType.Frame:
                    if (!userData.unlockedFrames.Contains(item.amount))
                        userData.unlockedFrames.Add(item.amount);
                    break;
            }
        }
    }

    private void RecordPurchase(ShopPackData pack)
    {
        if (!purchaseHistory.ContainsKey(pack.id))
            purchaseHistory[pack.id] = 0;

        purchaseHistory[pack.id]++;
        lastPurchaseTime[pack.id] = DateTime.Now;
        
        if (pack.limit == PurchaseLimit.OneTime)
        {
            var userData = GameManager.Instance.userData;
            if (!userData.purchasedPackIds.Contains(pack.id))
            {
                userData.purchasedPackIds.Add(pack.id);
            }
        }
    }

    public int GetPurchaseCount(string packId)
    {
        return purchaseHistory.ContainsKey(packId) ? purchaseHistory[packId] : 0;
    }

    public bool HasPurchased(string packId)
    {
        return purchaseHistory.ContainsKey(packId);
    }
}
