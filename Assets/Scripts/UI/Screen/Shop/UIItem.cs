using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIItem : MonoBehaviour
{
    [SerializeField] private Image m_iconImage;
    [SerializeField] private TextMeshProUGUI m_amountText;
    [SerializeField] private Sprite m_coinSprite;
    [SerializeField] private Sprite m_boosterType1Sprite;
    [SerializeField] private Sprite m_boosterType2Sprite;
    [SerializeField] private Sprite m_boosterType3Sprite;
    [SerializeField] private Sprite m_infiniteHealthSprite;
    [SerializeField] private Sprite m_noAdsSprite;
    [SerializeField] private float m_iconShakeAngle = 4f;
    [SerializeField] private float m_iconShakeDuration = 1.2f;
    [SerializeField] private float m_iconShakeDelay = 0.35f;

    private Item _item;
    private Tween _iconShakeTween;

    public void Init(Item item)
    {
        _item = item;

        ItemType type = item.ItemType;
        int amount = item.Quantity;

        string amountText = amount.ToString();
        Sprite iconSprite = null;


        switch (type)
        {
            case ItemType.Gold:
                iconSprite = m_coinSprite;
                break;
            case ItemType.Booster_Type1:
                iconSprite = m_boosterType1Sprite;
                amountText = "x" + amountText;
                break;
            case ItemType.Booster_Type2:
                iconSprite = m_boosterType2Sprite;
                amountText = "x" + amountText;
                break;
            case ItemType.Booster_Type3:
                iconSprite = m_boosterType3Sprite;
                amountText = "x" + amountText;
                break;
            case ItemType.InfiniteHealth:
                iconSprite = m_infiniteHealthSprite;
                amountText = amountText + "h";
                break;
            case ItemType.NoAds:
                iconSprite = m_noAdsSprite;
                amountText = "Skip Ads";
                break;
            
        }


        m_amountText.text = amountText;

        if (m_iconImage == null) return;
        m_iconImage.sprite = iconSprite;
        m_iconImage.preserveAspect = true;
        m_iconImage.enabled = iconSprite != null;
        PlayIconShake();
    }

    private void PlayIconShake()
    {
        if (m_iconImage == null) return;

        Transform iconTransform = m_iconImage.transform;
        _iconShakeTween?.Kill();
        iconTransform.localRotation = Quaternion.identity;

        if (!m_iconImage.enabled) return;

        _iconShakeTween = iconTransform
            .DOPunchRotation(new Vector3(0f, 0f, m_iconShakeAngle), m_iconShakeDuration, 6, 0.45f)
            .SetDelay(m_iconShakeDelay)
            .SetLoops(-1, LoopType.Restart)
            .SetTarget(iconTransform);
    }

    private void OnDisable()
    {
        _iconShakeTween?.Kill();
        _iconShakeTween = null;

        if (m_iconImage != null)
            m_iconImage.transform.localRotation = Quaternion.identity;
    }

    public void Claim()
    {
        UserData userData = GameManagerInGame.Instance.userData;

        userData.ApplyShopItem(_item);
    }
}
