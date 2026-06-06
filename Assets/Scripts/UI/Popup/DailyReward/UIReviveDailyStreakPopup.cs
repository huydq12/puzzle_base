using TMPro;
using UnityEngine;

public class UIReviveDailyStreakPopup : BasePopup
{
    [SerializeField] private ButtonPlay m_reviveButton;
    [SerializeField] private ButtonPlay m_loseStreakButton;
    [SerializeField] private TextMeshProUGUI m_messageText;

    private void Start()
    {
        if (m_reviveButton != null) m_reviveButton.onClick.AddListener(OnReviveButtonClick);
        if (m_loseStreakButton != null) m_loseStreakButton.onClick.AddListener(OnLoseStreakButtonClick);
    }

    public override void BeforeShow()
    {
        base.BeforeShow();

        if (m_messageText != null)
        {
            m_messageText.text = "Revive your daily streak?";
        }
    }

    private void OnReviveButtonClick()
    {
        UserData userData = GameManagerInGame.Instance.userData;
        userData.dailyRewardHandler.ReviveStreak();
        userData.Save();
        HideUI();
    }

    private void OnLoseStreakButtonClick()
    {
        UserData userData = GameManagerInGame.Instance.userData;
        userData.dailyRewardHandler.GiveUpStreak();
        userData.Save();
        HideUI();
    }
}
