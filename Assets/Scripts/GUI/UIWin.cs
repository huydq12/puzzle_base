using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System.Collections.Generic;
public class UIWin : UIPopup
{
    [SerializeField] private Button btn_next;

    [SerializeField] private Button btn_close_hide;

    [SerializeField] private TextMeshProUGUI txt_coin;
    [SerializeField] private TextMeshProUGUI txt_level;


    [SerializeField] private int rewardCoin;
    [SerializeField] private TextMeshProUGUI txt_coin_reward;

    [SerializeField] private Image iconElementNext;
    [SerializeField] private Image iconElementNextFill;
    [SerializeField] private Image img_slide_next_element;
    [SerializeField] private TextMeshProUGUI txt_fill_element_next;

    [SerializeField] private GameObject currencyPrefab;
    [SerializeField] private RectTransform spawnPos;
    [SerializeField] private Transform target;
    [SerializeField] private int fxCount = 10;

    [SerializeField] private ButtonBehavior claimReward;
    [SerializeField] private ButtonBehavior claimRewardAds;


    private readonly List<GameObject> activeFx = new List<GameObject>();

    [SerializeField] private GameObject groupReward;

    private Tween coinCountTween;
    private Tween slideFillTween;

    private int pendingCoinTarget;
    private int pendingCoinFromAmount;
    private int pendingRewardAmount;
    private bool pendingCoinApplied = true;
    private int pendingRewardLevel;
    private bool shouldGrantReward;
    private bool rewardClaimStarted;
    private bool closeAfterClaimAnimation;

    public override void BeforeShow()
    {
        base.BeforeShow();
        VibrateManager.Instance.MediumVibrate();
        AudioManager.Instance.PlaySFX(SFXType.Win);

        ApplyPendingReward();
        CleanupAnimations();

        UpdateNextTutorialFill();

        var gameManager = GameManagerInGame.Instance;
        var userData = gameManager != null ? gameManager.userData : null;
        int completedLevel = GetCompletedLevel();
        bool hasClaimedReward = HasClaimedReward(userData, completedLevel);

        if (txt_level != null)
        {
            txt_level.text = "Level "+completedLevel.ToString()+" complete";
        }

        shouldGrantReward = !hasClaimedReward;
        pendingRewardLevel = shouldGrantReward ? completedLevel : -1;

        int fromAmount = userData != null ? userData.playerCash : 0;
        int rewardAmount = shouldGrantReward ? rewardCoin : 0;
        int toAmount = fromAmount + rewardAmount;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[UIWin] Show completedLevel={completedLevel} currentLevel={(gameManager != null ? gameManager.CurrentLevel : -1)} rewardCoin={rewardCoin} hasClaimedReward={hasClaimedReward} from={fromAmount} to={toAmount}");
#endif

        pendingCoinFromAmount = fromAmount;
        pendingRewardAmount = rewardAmount;
        pendingCoinTarget = toAmount;
        pendingCoinApplied = false;
        rewardClaimStarted = false;
        closeAfterClaimAnimation = false;

        if (groupReward != null)
        {
            groupReward.SetActive(shouldGrantReward);
        }

        if (txt_coin != null)
        {
            txt_coin.text = fromAmount.ToString();
        }

        if (txt_coin_reward != null)
        {
            txt_coin_reward.text = "+" + rewardAmount.ToString();
        }

        StopAllCoroutines();
        RefreshClaimButtons();
    }

    public override void BeforeHide()
    {
        base.BeforeHide();
        ApplyPendingReward();
        CleanupAnimations();
    }

    private void CleanupAnimations()
    {
        StopAllCoroutines();

        if (coinCountTween != null && coinCountTween.IsActive())
        {
            coinCountTween.Kill(false);
        }
        coinCountTween = null;

        if (slideFillTween != null && slideFillTween.IsActive())
        {
            slideFillTween.Kill(false);
        }
        slideFillTween = null;

	        for (int i = activeFx.Count - 1; i >= 0; i--)
	        {
	            var go = activeFx[i];
	            if (go == null) continue;
	            DOTween.Kill(go.transform, false);
	            Destroy(go);
	        }
	        activeFx.Clear();
	    }

    private void ApplyPendingReward()
    {
        if (pendingCoinApplied) return;
        if (GameManagerInGame.Instance == null || GameManagerInGame.Instance.userData == null) return;

        var userData = GameManagerInGame.Instance.userData;
        userData.playerCash = pendingCoinTarget;

        if (shouldGrantReward && pendingRewardLevel > 0 && !HasClaimedReward(userData, pendingRewardLevel))
        {
            userData.claimedWinRewardLevels.Add(pendingRewardLevel);
        }

        userData.Save();
        pendingCoinApplied = true;

        if (txt_coin != null)
        {
            txt_coin.text = pendingCoinTarget.ToString();
        }

        RefreshClaimButtons();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[UIWin] ApplyPendingReward level={pendingRewardLevel} granted={shouldGrantReward} playerCash={userData.playerCash}");
#endif
    }

    private void RefreshClaimButtons()
    {
        bool canClaim = shouldGrantReward && !pendingCoinApplied && !rewardClaimStarted;

        if (claimReward != null)
        {
            claimReward.SetInteractable(canClaim);
        }

        if (claimRewardAds != null)
        {
            claimRewardAds.SetInteractable(canClaim);
        }
    }

    private void UpdateNextTutorialFill()
    {
        if (GameManagerInGame.Instance == null) return;

        int completedLevel = GetCompletedLevel();

        int prevMilestone;
        int nextMilestone;
        if (!GetTutorialMilestonesFromConfig(completedLevel, out prevMilestone, out nextMilestone))
        {
            GetTutorialMilestones(completedLevel, out prevMilestone, out nextMilestone);
        }

        float toFill = CalcSegmentFill(completedLevel, prevMilestone, nextMilestone);
        float fromFill = CalcSegmentFill(Mathf.Max(1, completedLevel - 1), prevMilestone, nextMilestone);

        ApplyNextElementVisual(nextMilestone);

        if (iconElementNextFill != null) iconElementNextFill.fillAmount = fromFill;
        if (img_slide_next_element != null) img_slide_next_element.fillAmount = fromFill;

        if (iconElementNextFill != null || img_slide_next_element != null)
        {
            float tweenValue = fromFill;
            slideFillTween = DOTween.To(() => tweenValue, v =>
            {
                tweenValue = v;
                if (iconElementNextFill != null) iconElementNextFill.fillAmount = 1-v;
                if (img_slide_next_element != null) img_slide_next_element.fillAmount = v;
            }, toFill, 0.6f).SetEase(Ease.OutQuad);
        }

        if (txt_fill_element_next != null)
        {
            int percent = Mathf.RoundToInt(toFill * 100f);
            txt_fill_element_next.text = $"{percent}%";
        }
    }

    private void ApplyNextElementVisual(int nextMilestone)
    {
        Sprite icon = null;

        var cfg = TutorialPopupService.Config;
        if (cfg != null && nextMilestone > 0)
        {
            var entry = cfg.GetEntry(nextMilestone);
            if (entry != null)
            {
                icon = entry.icon;
            }
        }

        if (iconElementNext != null)
        {
            iconElementNext.sprite = icon;
            iconElementNext.enabled = icon != null;
        }

        if (iconElementNextFill != null)
        {
            iconElementNextFill.sprite = icon;
            iconElementNextFill.enabled = icon != null;
        }

    }

    private static float CalcSegmentFill(int level, int prevMilestone, int nextMilestone)
    {
        if (nextMilestone <= 0) return 1f;
        int denom = nextMilestone - prevMilestone;
        if (denom <= 0) return 1f;

        int progressed = level - prevMilestone + 1;
        progressed = Mathf.Clamp(progressed, 0, denom);
        return Mathf.Clamp01(progressed / (float)denom);
    }

    private static void GetTutorialMilestones(int level, out int prevMilestone, out int nextMilestone)
    {
        prevMilestone = 1;
        nextMilestone = -1;

        Array values = Enum.GetValues(typeof(TutorialType));
        List<int> milestones = new List<int>(values.Length);
        for (int i = 0; i < values.Length; i++)
        {
            milestones.Add((int)values.GetValue(i));
        }
        milestones.Sort();

        for (int i = 0; i < milestones.Count; i++)
        {
            int m = milestones[i];
            if (m <= level)
            {
                prevMilestone = m;
                continue;
            }
            nextMilestone = m;
            break;
        }
    }

    private static bool GetTutorialMilestonesFromConfig(int level, out int prevMilestone, out int nextMilestone)
    {
        prevMilestone = 1;
        nextMilestone = -1;

        var cfg = TutorialPopupService.Config;
        if (cfg == null || cfg.entries == null || cfg.entries.Count == 0) return false;

        List<int> milestones = new List<int>(cfg.entries.Count);
        for (int i = 0; i < cfg.entries.Count; i++)
        {
            var e = cfg.entries[i];
            if (e == null) continue;
            milestones.Add(Mathf.Max(1, e.level));
        }

        if (milestones.Count == 0) return false;

        milestones.Sort();

        // de-dup
        for (int i = milestones.Count - 1; i > 0; i--)
        {
            if (milestones[i] == milestones[i - 1]) milestones.RemoveAt(i);
        }

        level = Mathf.Max(1, level);

        for (int i = 0; i < milestones.Count; i++)
        {
            int m = milestones[i];
            if (m <= level)
            {
                prevMilestone = m;
                continue;
            }
            nextMilestone = m;
            break;
        }

        return true;
    }

    private static bool HasClaimedReward(UserData userData, int level)
    {
        return userData != null
            && userData.claimedWinRewardLevels != null
            && level > 0
            && userData.claimedWinRewardLevels.Contains(level);
    }

    private static int GetCompletedLevel()
    {
        if (GameManagerInGame.Instance == null) return 1;
        if (GameManagerInGame.Instance.LastCompletedLevel > 0)
            return GameManagerInGame.Instance.LastCompletedLevel;

        int currentLevelAfterWin = Mathf.Max(1, GameManagerInGame.Instance.CurrentLevel);
        return Mathf.Max(1, currentLevelAfterWin - 1);
    }

    protected override void Start()
    {
        base.Start();
        btn_next.onClick.AddListener(NextGame);
        btn_close_hide.onClick.AddListener(NextGame);
        if (claimReward != null)
        {
            claimReward.OnClick.AddListener(ClaimReward);
        }
        if (claimRewardAds != null)
        {
            claimRewardAds.OnClick.AddListener(ClaimRewardAds);
        }
    }

    private void ClaimReward()
    {
        StartClaimRewardFlow(false, false);
    }

    private void ClaimRewardAds()
    {
        StartClaimRewardFlow(false, true);
    }

    private void StartClaimRewardFlow(bool closeAfterAnimation, bool useAdsMultiplier)
    {
        if (!shouldGrantReward || pendingCoinApplied || rewardClaimStarted) return;

        rewardClaimStarted = true;
        closeAfterClaimAnimation = closeAfterAnimation;
        pendingCoinTarget = pendingCoinFromAmount + (useAdsMultiplier ? pendingRewardAmount * 2 : pendingRewardAmount);
        RefreshClaimButtons();
        StopAllCoroutines();
        StartCoroutine(ShowCoinFxMoveToTarget(pendingCoinFromAmount, pendingCoinTarget));
    }

    private IEnumerator ShowCoinFxMoveToTarget(int fromAmount, int toAmount)
    {
        yield return new WaitForSeconds(0.25f);
        PlayCoinFx();

        yield return new WaitForSeconds(1.25f);
        float currentValue = fromAmount;
        float targetValue = toAmount;
        coinCountTween = DOTween.To(() => currentValue, x => currentValue = (int)x, targetValue, 1f)
            .OnUpdate(() =>
            {
                if (txt_coin != null)
                {
                    txt_coin.text = currentValue.ToString();
                }
            });

        yield return new WaitForSeconds(1.5f);

        ApplyPendingReward();

        if (closeAfterClaimAnimation)
        {
            CloseAfterClaimAnimation();
        }
    }

    private void PlayCoinFx()
    {
        if (currencyPrefab == null || spawnPos == null || target == null) return;
        for (int i = 0; i < fxCount; i++)
        {
            Flying();
        }
    }

	private void Flying()
	{
	    GameObject go = Instantiate(currencyPrefab);
	    if (go == null) return;

        activeFx.Add(go);

        go.transform.SetParent(spawnPos.transform, false);
        go.transform.localScale = Vector3.one;
        go.transform.localRotation = Quaternion.identity;
        go.transform.position = spawnPos.transform.position;

        DOTween.Kill(go.transform, false);
        go.transform.DORotate(new Vector3(0, 0, UnityEngine.Random.Range(0, 180)), 0.5f).SetEase(Ease.Linear);
        go.transform.DOLocalMove(new Vector3(UnityEngine.Random.Range(-300, 300), UnityEngine.Random.Range(-300, 300), 0), 0.5f).OnComplete(() =>
        {
            if (go == null || !go.activeInHierarchy) return;
            go.transform.DORotate(new Vector3(0, 0, 70), 0.3f).SetEase(Ease.Linear);
            go.transform.DOScale(Vector3.zero, 1.7f);
            go.transform.DOMove(target.position, 0.5f).OnComplete(() =>
	            {
	                if (go == null) return;
	                activeFx.Remove(go);
	                Destroy(go);
	            });
	        });
	    }

    private void NextGame()
    {
        if (shouldGrantReward && !pendingCoinApplied)
        {
            if (!rewardClaimStarted)
            {
                StartClaimRewardFlow(true, false);
            }
            return;
        }

        CloseAfterClaimAnimation();
    }

    private void CloseAfterClaimAnimation()
    {
        closeAfterClaimAnimation = false;
        CleanupAnimations();
        GameManagerInGame.Instance.StartNextLevel();
        UIManager.Instance.HideUI<UIWin>();
    }
}
