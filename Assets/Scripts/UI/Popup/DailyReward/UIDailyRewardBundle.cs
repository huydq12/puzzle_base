using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIDailyRewardBundle : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_dayText;
    [SerializeField] private Transform m_rewardHolder;
    [SerializeField] private UIItem m_itemPrefabs;
    [SerializeField] private ButtonPlay m_button;
    [SerializeField] private GameObject m_claimedOverlay;
    [SerializeField] private ButtonPlay m_failedButton;

    private List<UIItem> _uiItems = new();
    private DailyRewardData _dailyRewardData;
    private int _slotIndex;
    private UIDailyRewardPopup _uIDailyRewardPopup;
    private DailyRewardHandler _dailyRewardHandler;

    private void Start()
    {
        m_button.onClick.AddListener(OnButtonClick);
        m_failedButton.onClick.AddListener(OnButtonFailClick);
    }

    public void Init(DailyRewardData dailyRewardData, int displayDay, int slotIndex, DailyRewardHandler handler, int globalState, UIDailyRewardPopup uIDailyRewardPopup)
    {
        ResetUI();

        _dailyRewardData = dailyRewardData;
        _slotIndex = slotIndex;
        _uIDailyRewardPopup = uIDailyRewardPopup;
        _dailyRewardHandler = handler;

        foreach (Item item in dailyRewardData.items)
        {
            UIItem uiItem = Instantiate(m_itemPrefabs, m_rewardHolder);
            uiItem.Init(item);
            _uiItems.Add(uiItem);
        }

        m_dayText.text = "Day " + displayDay;

        UpdateUI(globalState);
    }

    public void UpdateUI(int globalState)
    {
        m_button.gameObject.SetActive(false);
        if (m_claimedOverlay != null) m_claimedOverlay.SetActive(false);
        if (m_failedButton != null) m_failedButton.gameObject.SetActive(false);

        int currentSlotWaiting = _dailyRewardHandler.totalClaims % 7;

        if (_slotIndex < currentSlotWaiting ||
            (currentSlotWaiting == 0 && _dailyRewardHandler.currentDay == _dailyRewardHandler.currentStreak && _dailyRewardHandler.currentDay != 0))
        {
            if (m_claimedOverlay != null) m_claimedOverlay.SetActive(true);
            return;
        }

        if (_slotIndex == currentSlotWaiting)
        {
            if (globalState == 2)
            {
                m_failedButton.transform.DOKill();
                m_failedButton.transform.localScale = Vector3.one;
                m_failedButton.gameObject.SetActive(true);
                m_failedButton.transform.ScaleLoop();
                return;
            }

            if (globalState == 0)
            {
                m_button.gameObject.SetActive(true);
                return;
            }

            if (globalState == -1)
            {
                m_failedButton.transform.DOKill();
                m_failedButton.transform.localScale = Vector3.one;
                m_failedButton.gameObject.SetActive(true);
                m_failedButton.transform.ScaleLoop();
                return;
            }
        }
    }
    public void ResetUI()
    {
        foreach (UIItem uIItem in _uiItems)
        {
            if (uIItem != null) Destroy(uIItem.gameObject);
        }
        _uiItems.Clear();

        m_button.gameObject.SetActive(false);
        m_failedButton.gameObject.SetActive(false);
        if (m_claimedOverlay != null) m_claimedOverlay.SetActive(false);
        if (m_failedButton != null) m_failedButton.gameObject.SetActive(false);
        m_failedButton.transform.DOKill();
        m_failedButton.transform.localScale = Vector3.one;
    }

    public void OnButtonClick()
    {
        UserData userData = GameManagerInGame.Instance.userData;

        _dailyRewardHandler.ClaimReward();

        foreach (UIItem uIItem in _uiItems) uIItem.Claim();

        userData.Save();

        m_button.transform.localPosition = Vector3.zero;

        m_claimedOverlay.SetActive(true);
        m_button.gameObject.SetActive(false);

        _uIDailyRewardPopup.OnDailyClaim(userData);
    }

    public void OnButtonFailClick()
    {
        UIManager.Instance.ShowUI<UIReviveDailyStreakPopup>(false);
        m_failedButton.transform.DOKill();
        m_failedButton.transform.localScale = Vector3.one;
    }
}
