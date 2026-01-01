using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UISpecialDeal : UIPopup
{
    [Header("Pack Display")]
    public List<ShopItemData> shopItems;
    
    [Header("Special Offer")]
    [SerializeField] private GameObject specialOfferBadge;
    [SerializeField] private TextMeshProUGUI txt_discount;
    [SerializeField] private TextMeshProUGUI txt_timeLeft;
    
    [Header("Buy Button")]
    [SerializeField] private Button btn_buy;
    [SerializeField] private TextMeshProUGUI txt_buyButton;
    
    [Header("Pack Configuration")]
    [SerializeField] private ShopCatalog shopCatalog;
    [SerializeField] private string packId = "special_deal_pack";
    [SerializeField] private bool useTimeLimitedOffer = true;
    [SerializeField] private float offerDurationHours = 24f;
    
    private ShopPackData currentPack;
    private DateTime offerEndTime;
    
    protected override void Start()
    {
        base.Start();
        
        if (btn_buy != null)
        {
            btn_buy.onClick.AddListener(OnBuyClicked);
        }
    }
    
    public override void Show()
    {
        base.Show();
        LoadPackData();
        
        if (useTimeLimitedOffer)
        {
            StartCoroutine(UpdateTimerCoroutine());
        }
    }
    
    private void LoadPackData()
    {
        // Find Special Deal pack from catalog
        currentPack = FindPackById(packId);
        
        if (currentPack != null)
        {
            UpdateUI();
        }
        else
        {
            Debug.LogWarning($"Special Deal pack with id '{packId}' not found in shop catalog");
        }
    }
    
    private ShopPackData FindPackById(string id)
    {
        if (shopCatalog == null || shopCatalog.categories == null)
        {
            Debug.LogError("ShopCatalog is not assigned or has no categories");
            return null;
        }
        
        foreach (var category in shopCatalog.categories)
        {
            if (category.packs == null) continue;
            
            foreach (var pack in category.packs)
            {
                // Match by pack id or IAP product id so we can configure this popup
                // using either the logical pack id or the store product id.
                if (pack.id == id || pack.priceIAPId == id)
                {
                    return pack;
                }
            }
        }
        
        Debug.LogWarning($"Pack with id '{id}' not found in ShopCatalog");
        return null;
    }
    
    private void UpdateUI()
    {
        if (currentPack == null) return;
        
        // Map rewards to shopItems
        MapRewardItems(currentPack.reward, shopItems);
        
        // Update special offer badge
        if (specialOfferBadge != null)
            specialOfferBadge.SetActive(currentPack.isSpecialOffer);
        
        if (txt_discount != null && currentPack.isSpecialOffer)
            txt_discount.text = $"-{currentPack.discountPercent}%";
        
        // Update buy button text
        if (txt_buyButton != null)
            txt_buyButton.text = "Buy Now";
        
        // Check if already purchased
        CheckPurchaseStatus();
        
        // Initialize offer timer
        if (useTimeLimitedOffer)
        {
            offerEndTime = DateTime.Now.AddHours(offerDurationHours);
        }
    }
    
    private void MapRewardItems(RewardPayload rewardPayload, List<ShopItemData> itemUIList)
    {
        if (rewardPayload == null || rewardPayload.items == null || itemUIList == null)
            return;

        if (rewardPayload.items.Count != itemUIList.Count)
        {
            Debug.LogWarning($"Reward item count mismatch: Data={rewardPayload.items.Count}, UI={itemUIList.Count}");
            return;
        }

        for (int i = 0; i < rewardPayload.items.Count; i++)
        {
            var rewardEntry = rewardPayload.items[i];
            var itemUI = itemUIList[i];

            itemUI.rewardType = rewardEntry.rewardType;

            if (itemUI.amount != null)
            {
                switch (rewardEntry.rewardType)
                {
                    case RewardType.Coin:
                        itemUI.amount.text = rewardEntry.amount.ToString();
                        break;

                    case RewardType.Health:
                        itemUI.amount.text = rewardEntry.amount.ToString() + "h";
                        break;

                    case RewardType.BoosterType1:
                    case RewardType.BoosterType2:
                    case RewardType.BoosterType3:
                        itemUI.amount.text = "x" + rewardEntry.amount.ToString();
                        break;

                    case RewardType.Avatar:
                    case RewardType.Frame:
                    case RewardType.NoAds:
                        itemUI.amount.text = rewardEntry.amount.ToString();
                        break;
                }
            }
        }
    }
    
    
    private IEnumerator UpdateTimerCoroutine()
    {
        while (gameObject.activeInHierarchy)
        {
            UpdateTimer();
            yield return new WaitForSeconds(1f);
        }
    }
    
    private void UpdateTimer()
    {
        if (txt_timeLeft == null || !useTimeLimitedOffer) return;
        
        TimeSpan timeRemaining = offerEndTime - DateTime.Now;
        
        if (timeRemaining.TotalSeconds <= 0)
        {
            txt_timeLeft.text = "Offer Expired";
            if (btn_buy != null)
                btn_buy.interactable = false;
        }
        else
        {
            txt_timeLeft.text = $"Time Left: {timeRemaining.Hours:D2}:{timeRemaining.Minutes:D2}:{timeRemaining.Seconds:D2}";
        }
    }
    
    private void CheckPurchaseStatus()
    {
        if (currentPack == null || btn_buy == null) return;
        
        var userData = GameManager.Instance.userData;
        if (currentPack.limit == PurchaseLimit.OneTime && userData.purchasedPackIds.Contains(currentPack.id))
        {
            btn_buy.interactable = false;
            if (txt_buyButton != null)
                txt_buyButton.text = "Purchased";
        }
        else
        {
            btn_buy.interactable = true;
        }
    }
    
    private void OnBuyClicked()
    {
        if (currentPack == null)
        {
            Debug.LogError("No pack data available for purchase");
            return;
        }
        
        ShopManager.Instance.PurchasePack(currentPack,
            onSuccess: () =>
            {
                Debug.Log("Special Deal pack purchased successfully!");
                UpdateUI();
                // Optionally hide popup after successful purchase
                // Hide();
            },
            onFailed: (error) =>
            {
                Debug.LogError($"Purchase failed: {error}");
                // Show error message to user
            }
        );
    }
    
    private void OnDestroy()
    {
        if (btn_buy != null)
        {
            btn_buy.onClick.RemoveListener(OnBuyClicked);
        }
    }
}
