using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIItem : MonoBehaviour
{
    [SerializeField] private SkeletonGraphic m_iconSpine;
    [SerializeField] private TextMeshProUGUI m_amountText;

    private Item _item;

    public void Init(Item item)
    {
        _item = item;

        ItemType type = item.ItemType;
        int amount = item.Quantity;

        string amountText = amount.ToString();
        string animName = "";


        switch (type)
        {
            case ItemType.Gold:
                if (amount <= 100)
                    animName = "coin/coin1";
                else if (amount <= 200 && amount > 100)
                    animName = "coin/coin2";
                else if (amount <= 500 && amount > 200)
                    animName = "coin/coin3";
                else if (amount <= 1000 && amount > 500)
                    animName = "coin/coin4";
                else if (amount <= 2000 && amount > 1000)
                    animName = "coin/coin5";
                else if (amount <= 5000 & amount > 2000)
                    animName = "coin/coin6";
                break;
            case ItemType.Booster_Type1:
                animName = "magnet";
                amountText = "x" + amountText;
                break;
            case ItemType.Booster_Type2:
                animName = "shuffle";
                amountText = "x" + amountText;
                break;
            case ItemType.Booster_Type3:
                animName = "clear";
                amountText = "x" + amountText;
                break;
            case ItemType.InfiniteHealth:
                animName = "heart";
                amountText = "<sprite=0>" + amountText + "h";
                break;
            case ItemType.NoAds:
                animName = "ads";
                amountText = "Skip Ads";
                break;
            
        }


        m_amountText.text = amountText;

        if (string.IsNullOrEmpty(animName)) return;

        m_iconSpine.Initialize(true);
        m_iconSpine.AnimationState.SetAnimation(0, animName, true);
    }

    public void Claim()
    {
        UserData userData = GameManagerInGame.Instance.userData;

        userData.ApplyShopItem(_item);
    }
}
