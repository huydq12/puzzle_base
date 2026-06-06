using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;

public class UIWeeklyRewardBundle : MonoBehaviour
{
    [SerializeField] private Transform m_rewardHolder;
    [SerializeField] private UIItem m_itemPrefabs;
    [SerializeField] private ButtonPlay m_button;
    [SerializeField] private GameObject m_claimedOverlay;
    [SerializeField] private GameObject m_rewardHolderObject;

    private List<UIItem> _uiItems = new();
    private WeeklyRewardData _weekRewardData;
    private UIDailyRewardPopup _uIDailyRewardPopup;
    private DailyRewardHandler _dailyRewardHandler;

    private void Start()
    {
        if (m_button != null)
        {
            m_button.onClick.AddListener(OnBundlePreviewClick);
        }
    }

    public void Init(WeeklyRewardData weekRewardData, DailyRewardHandler handler, UIDailyRewardPopup uIDailyRewardPopup)
    {
        ResetUI();
        _weekRewardData = weekRewardData;
        _uIDailyRewardPopup = uIDailyRewardPopup;
        _dailyRewardHandler = handler;

        foreach (Item item in weekRewardData.items)
        {
            UIItem uiItem = Instantiate(m_itemPrefabs, m_rewardHolder);
            uiItem.Init(item);
            _uiItems.Add(uiItem);
        }

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (_weekRewardData == null || _dailyRewardHandler == null) return;

        bool isClaimed = _dailyRewardHandler.IsWeeklyRewardClaimed(_weekRewardData.unlockDay);

        if (isClaimed)
        {
            if (m_button != null) m_button.gameObject.SetActive(false);
            if (m_claimedOverlay != null) m_claimedOverlay.SetActive(true);
        }
        else
        {
            if (m_claimedOverlay != null) m_claimedOverlay.SetActive(false);
            if (m_button != null)
            {
                m_button.gameObject.SetActive(true);
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

        if (m_claimedOverlay != null) m_claimedOverlay.SetActive(false);
        if (m_button != null) m_button.gameObject.SetActive(false);
    }


    private CancellationTokenSource _previewCancelTokenSource;

    private async void OnBundlePreviewClick()
    {
        if (_weekRewardData == null) return;
        Debug.Log($"Mốc tuần này cần {_weekRewardData.unlockDay} ngày để tự động mở khóa.");

        if (_previewCancelTokenSource != null)
        {
            _previewCancelTokenSource.Cancel();
            _previewCancelTokenSource.Dispose();
            _previewCancelTokenSource = null;
        }

        if (!m_rewardHolderObject.activeSelf)
        {
            m_rewardHolderObject.SetActive(true);

            m_rewardHolderObject.transform.DOKill();
            m_rewardHolderObject.transform.ScaleFrom(0, 1, 0.1f, Ease.OutBack);
        }
        else
        {
            m_rewardHolderObject.transform.localScale = Vector3.one;
        }

        _previewCancelTokenSource = new CancellationTokenSource();

        try
        {
            await UniTask.Delay(2000, cancellationToken: _previewCancelTokenSource.Token);

            m_rewardHolderObject.SetActive(false);
        }
        catch (System.OperationCanceledException)
        {
            // Hàm bị hủy bởi click mới, không làm gì cả (Object vẫn mở để đợi chu kỳ 2s tiếp theo)
        }
    }


    private void OnDestroy()
    {
        if (_previewCancelTokenSource != null)
        {
            _previewCancelTokenSource.Cancel();
            _previewCancelTokenSource.Dispose();
        }
    }
}
