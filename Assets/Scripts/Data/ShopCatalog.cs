using System;
using System.Collections.Generic;
using UnityEngine;

public enum ShopCategory
{
    CoinPack,
    StartPack,
    SpecialPack,
    NoAdsPack,
    None
}

public enum ShopCurrency
{
    Coin,
    Diamond,
    IAP,
    None
}

public enum RewardType
{
    Coin,
    Health,
    BoosterType1,
    BoosterType2,
    BoosterType3,
    Avatar,
    Frame,
    NoAds,
    None
}

public enum PurchaseLimit
{
    None,
    Daily,
    Weekly,
    OneTime
}

[CreateAssetMenu(fileName = "ShopCatalog", menuName = "Game/Shop Catalog")]
[Serializable]
public class ShopCatalog : ScriptableObject
{
    public List<ShopCategoryData> categories;
}

[Serializable]
public class ShopCategoryData
{
    public ShopCategory category;
    public string categoryName;
    public Sprite categoryBgIcon;
    public Color categoryBgColor;
    public List<ShopPackData> packs;
}

[Serializable]
public class ShopPackData
{
    public string id;
    public string displayName;
    public Sprite icon;
    public ShopCurrency currencyType;
    public int priceCoin;
    public int priceDiamond;
    public string priceIAP;
    public string priceIAPId;
    public RewardPayload reward;
    public PurchaseLimit limit;
    public bool isSpecialOffer;
    public int discountPercent;
}

[Serializable]
public class RewardPayload
{
    public List<RewardEntry> items;
}

[Serializable]
public class RewardEntry
{
    public RewardType rewardType;
    public int amount;
}
