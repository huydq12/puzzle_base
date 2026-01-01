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

    [SerializeField] private int rewardCoin;
    [SerializeField] private TextMeshProUGUI txt_coin_reward;

    [SerializeField] private GameObject currencyPrefab;
    [SerializeField] private RectTransform spawnPos;
    [SerializeField] private Transform target;
    [SerializeField] private int fxCount = 10;

    private readonly List<GameObject> activeFx = new List<GameObject>();

    [SerializeField] private Image img_slide_next_element;
    private Tween coinCountTween;
    private Tween slideFillTween;

    private int pendingCoinTarget;
    private bool pendingCoinApplied;

    public override void Show()
    {
        base.Show();
        VibrateManager.Instance.MediumVibrate();
        AudioManager.Instance.PlaySFX(SFXType.Win);

        CleanupAnimations();

        UpdateNextTutorialFill();

        int fromAmount = GameManagerInGame.Instance.userData.playerCash;
        int toAmount = fromAmount + rewardCoin;

        pendingCoinTarget = toAmount;
        pendingCoinApplied = false;

        if (txt_coin != null)
        {
            txt_coin.text = fromAmount.ToString();
        }

        if (txt_coin_reward != null)
        {
            txt_coin_reward.text = "+" + rewardCoin.ToString();
        }

        StopAllCoroutines();
        StartCoroutine(ShowCoinFxMoveToTarget(fromAmount, toAmount));
    }

    public override void Hide()
    {
        CleanupAnimations();
        base.Hide();
    }

    private void CleanupAnimations()
    {
        ApplyPendingReward();
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
            go.SetActive(false);
        }
        activeFx.Clear();
    }

    private void ApplyPendingReward()
    {
        if (pendingCoinApplied) return;
        if (GameManagerInGame.Instance == null || GameManagerInGame.Instance.userData == null) return;

        GameManagerInGame.Instance.userData.playerCash = pendingCoinTarget;
        GameManagerInGame.Instance.userData.Save();
        pendingCoinApplied = true;

        if (txt_coin != null)
        {
            txt_coin.text = pendingCoinTarget.ToString();
        }
    }

    private void UpdateNextTutorialFill()
    {
        if (img_slide_next_element == null) return;
        if (GameManagerInGame.Instance == null) return;

        int currentLevelAfterWin = Mathf.Max(1, GameManagerInGame.Instance.CurrentLevel);
        int completedLevel = Mathf.Max(1, currentLevelAfterWin - 1);

        int prevMilestone;
        int nextMilestone;
        GetTutorialMilestones(completedLevel, out prevMilestone, out nextMilestone);

        float toFill = CalcSegmentFill(completedLevel, prevMilestone, nextMilestone);
        float fromFill = CalcSegmentFill(Mathf.Max(1, completedLevel - 1), prevMilestone, nextMilestone);

        img_slide_next_element.fillAmount = fromFill;
        slideFillTween = DOTween.To(() => img_slide_next_element.fillAmount, v => img_slide_next_element.fillAmount = v, toFill, 0.6f)
            .SetEase(Ease.OutQuad);
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

    protected override void Start()
    {
        base.Start();
        btn_next.onClick.AddListener(NextGame);
        btn_close_hide.onClick.AddListener(NextGame);
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
        GameObject go = PoolManager.Instance != null ? PoolManager.Instance.Get(currencyPrefab) : Instantiate(currencyPrefab);
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
                go.SetActive(false);
            });
        });
    }

    private void NextGame()
    {
        CleanupAnimations();
        GameManagerInGame.Instance.StartGame();
        Hide();
    }
}
