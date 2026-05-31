using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UITopInGame : BaseScreen
{
    public override bool ManualHide => true;
    public override bool DestroyOnHide => false;
    public override bool UseBehindPanel => false;
    
    [SerializeField] private TextMeshProUGUI txt_level;
    [SerializeField] private TextMeshProUGUI txt_coin;
    [SerializeField] private ButtonBehavior buttonSetting;
    // [SerializeField] private ButtonBehavior buttonReplay;

    [SerializeField] private RectTransform reactLevelNormal;
    [SerializeField] private RectTransform reactLevelHard;
    [SerializeField] private Animator ani_hard_level;

    [SerializeField] private CanvasGroup trayNotificationGroup;
    [SerializeField] private TextMeshProUGUI textDeps;

    [SerializeField] private RectTransform infoSelectButon;
   
    private Coroutine hardLevelPopupCoroutine;
    private const float HardLevelPopupAutoHideDelay = 1.2f;
    private const string PopupOpenStateName = "Open";
    private const string PopupCloseStateName = "Close";

    [SerializeField] private List<Image> warningNotice;
    [SerializeField] private Color warningNoticeColor = Color.red;
    [SerializeField] private float warningNoticeBlinkDuration = 0.3f;

    private bool _isWarningNoticeBlinking;
    private readonly List<Tween> _warningNoticeTweens = new List<Tween>();
    private readonly List<Color> _warningNoticeDefaultColors = new List<Color>();
    private bool _isHardLevelPopupActive;
    private int _lastDisplayedCoin = int.MinValue;

    private static bool IsHardLevel(int level)
    {
        // Hard levels pattern: 13, 18, 23, 28, 33, 38, ...
        // i.e. start from level 13 and then every level ending with 3 or 8.
        if (level == 10) return true;
        if (level < 18) return false;
        int lastDigit = Mathf.Abs(level) % 10;
        return lastDigit == 3 || lastDigit == 8;
    }

    private void Start()
    {
        buttonSetting.OnClick.AddListener(() =>
        {
            UIManager.Instance.ShowUI<UISettingInGame>();
        });
        
        _warningNoticeDefaultColors.Clear();
        if (warningNotice == null) return;

        for (int i = 0; i < warningNotice.Count; i++)
        {
            var img = warningNotice[i];
            _warningNoticeDefaultColors.Add(img != null ? img.color : Color.white);
        }
    }

    private void OnDisable()
    {
        SetConveyorWarning(false);
        SetHardLevelWarningNotice(false);
        _lastDisplayedCoin = int.MinValue;

        if (hardLevelPopupCoroutine != null)
        {
            StopCoroutine(hardLevelPopupCoroutine);
            hardLevelPopupCoroutine = null;
        }

        if (ani_hard_level != null)
        {
            ani_hard_level.gameObject.SetActive(false);
        }
    }

    private void SetHardLevelWarningNotice(bool active)
    {
        _isHardLevelPopupActive = active;

        // Hard-level warning notice should follow the hard popup visibility, not conveyor warning state.
        // When active, show the notice (no blinking). When inactive, hide it.
        for (int i = 0; i < _warningNoticeTweens.Count; i++)
        {
            _warningNoticeTweens[i]?.Kill();
        }
        _warningNoticeTweens.Clear();
        _isWarningNoticeBlinking = false;

        if (warningNotice == null || warningNotice.Count == 0) return;

        if (!active)
        {
            SetWarningNoticeVisible(false);
            for (int i = 0; i < warningNotice.Count; i++)
            {
                var img = warningNotice[i];
                if (img == null) continue;
                var c = i < _warningNoticeDefaultColors.Count ? _warningNoticeDefaultColors[i] : img.color;
                img.color = c;
            }
            return;
        }

        SetWarningNoticeVisible(true);
        for (int i = 0; i < warningNotice.Count; i++)
        {
            var img = warningNotice[i];
            if (img == null) continue;
            var c = i < _warningNoticeDefaultColors.Count ? _warningNoticeDefaultColors[i] : img.color;
            img.color = c;
        }
    }

    private void SetWarningNoticeVisible(bool visible)
    {
        if (warningNotice == null) return;
        for (int i = 0; i < warningNotice.Count; i++)
        {
            var img = warningNotice[i];
            if (img == null) continue;
            img.gameObject.SetActive(visible);
        }
    }

    public void SetConveyorWarning(bool enabled)
    {
        if (_isHardLevelPopupActive) return;
        if (enabled && _isWarningNoticeBlinking) return;
        _isWarningNoticeBlinking = enabled;

        for (int i = 0; i < _warningNoticeTweens.Count; i++)
        {
            _warningNoticeTweens[i]?.Kill();
        }
        _warningNoticeTweens.Clear();

        if (warningNotice == null || warningNotice.Count == 0) return;

        if (!enabled)
        {
            SetWarningNoticeVisible(false);
            for (int i = 0; i < warningNotice.Count; i++)
            {
                var img = warningNotice[i];
                if (img == null) continue;
                var c = i < _warningNoticeDefaultColors.Count ? _warningNoticeDefaultColors[i] : img.color;
                img.color = c;
            }
            return;
        }

        SetWarningNoticeVisible(true);

        for (int i = 0; i < warningNotice.Count; i++)
        {
            var img = warningNotice[i];
            if (img == null) continue;

            var defaultColor = i < _warningNoticeDefaultColors.Count ? _warningNoticeDefaultColors[i] : img.color;
            img.color = defaultColor;

            var t = img
                .DOColor(warningNoticeColor, warningNoticeBlinkDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);

            _warningNoticeTweens.Add(t);
        }
    }

    public void ShowInfoButton(string message)
    {
        if (infoSelectButon == null) return;
        if (message == null || message.Length == 0){
            infoSelectButon.gameObject.SetActive(false);
            return;
        }
        infoSelectButon.gameObject.SetActive(true);
    }

    public override void Show()
    {
        base.Show();

        SetConveyorWarning(false);
        RefreshCoin();

        int level = GameManagerInGame.Instance != null ? GameManagerInGame.Instance.CurrentLevel : 1;
        txt_level.text = "Level " + level.ToString();

        bool isHard = IsHardLevel(level);
        if (reactLevelNormal != null) reactLevelNormal.gameObject.SetActive(!isHard);
        if (reactLevelHard != null) reactLevelHard.gameObject.SetActive(isHard);
        if (isHard) ShowHardLevelPopup();

        RectTransform target = isHard ? reactLevelHard : reactLevelNormal;
        if (target != null)
        {
            DOTween.Kill(target, false);
            target.localScale = Vector3.one * (isHard ? 0.75f : 0.85f);
            target.localRotation = Quaternion.identity;
            target.anchoredPosition3D = target.anchoredPosition3D;

            Sequence seq = DOTween.Sequence().SetTarget(target);
            seq.Append(target.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack));
            if (isHard)
            {
                seq.Join(target.DOPunchRotation(new Vector3(0f, 0f, 14f), 0.35f, 10, 0.8f));
                seq.Join(target.DOShakeAnchorPos(0.35f, new Vector2(10f, 0f), 12, 90f, false, true));
                seq.Append(target.DOScale(1.06f, 0.12f).SetEase(Ease.OutQuad));
                seq.Append(target.DOScale(1f, 0.12f).SetEase(Ease.InOutQuad));
            }
        }
    }

    private void Update()
    {
        RefreshCoin();
    }

    private void RefreshCoin()
    {
        if (txt_coin == null) return;

        int coin = 0;
        if (InventoryManager.Instance != null)
            coin = InventoryManager.Instance.GetCoin();
        else if (GameManagerInGame.Instance != null && GameManagerInGame.Instance.userData != null)
            coin = GameManagerInGame.Instance.userData.playerCash;

        if (_lastDisplayedCoin == coin) return;

        _lastDisplayedCoin = coin;
        txt_coin.text = coin.ToString();
    }

    private void ShowHardLevelPopup()
    {
        if (ani_hard_level == null) return;

        if (hardLevelPopupCoroutine != null)
        {
            StopCoroutine(hardLevelPopupCoroutine);
            hardLevelPopupCoroutine = null;
        }

        hardLevelPopupCoroutine = StartCoroutine(HardLevelPopupRoutine());
    }

    private IEnumerator HardLevelPopupRoutine()
    {
        var popupGo = ani_hard_level.gameObject;
        if (popupGo != null) popupGo.SetActive(true);
        SetHardLevelWarningNotice(true);

        ani_hard_level.Play(PopupOpenStateName, 0, 0f);

        yield return new WaitForSeconds(HardLevelPopupAutoHideDelay);

        ani_hard_level.Play(PopupCloseStateName, 0, 0f);
        yield return new WaitForSeconds(GetAnimationClipLengthSeconds(ani_hard_level, PopupCloseStateName, 0.3f));

        if (popupGo != null) popupGo.SetActive(false);
        SetHardLevelWarningNotice(false);
        hardLevelPopupCoroutine = null;
    }

    private static float GetAnimationClipLengthSeconds(Animator animator, string clipName, float fallbackSeconds)
    {
        if (animator == null) return fallbackSeconds;
        var controller = animator.runtimeAnimatorController;
        if (controller == null) return fallbackSeconds;

        var clips = controller.animationClips;
        if (clips == null) return fallbackSeconds;

        for (int i = 0; i < clips.Length; i++)
        {
            var clip = clips[i];
            if (clip != null && string.Equals(clip.name, clipName, StringComparison.Ordinal))
                return clip.length;
        }

        return fallbackSeconds;
    }

    public void ShowTrayNotificationLose(float autoHideDelay = 1.2f,string message = "")
    {
        if (trayNotificationGroup == null) return;
        
        if (textDeps != null) {
            textDeps.text = message;
        }

        var t = trayNotificationGroup.transform as RectTransform;

        trayNotificationGroup.gameObject.SetActive(true);
        trayNotificationGroup.alpha = 0f;
        trayNotificationGroup.interactable = false;
        trayNotificationGroup.blocksRaycasts = false;

        DOTween.Kill(trayNotificationGroup, false);
        if (t != null)
        {
            DOTween.Kill(t, false);
            t.localScale = Vector3.one * 0.9f;
            t.localRotation = Quaternion.identity;
        }

        Sequence seq = DOTween.Sequence().SetTarget(trayNotificationGroup);
        seq.Join(trayNotificationGroup.DOFade(1f, 0.18f).SetEase(Ease.OutQuad));
        if (t != null)
        {
            seq.Join(t.DOScale(1f, 0.18f).SetEase(Ease.OutBack).SetTarget(t));
            seq.Append(t.DORotate(new Vector3(0f, 0f, -8f), 0.08f).SetEase(Ease.OutQuad).SetTarget(t));
            seq.Append(t.DORotate(new Vector3(0f, 0f, 8f), 0.12f).SetEase(Ease.InOutQuad).SetTarget(t));
            seq.Append(t.DORotate(Vector3.zero, 0.1f).SetEase(Ease.OutQuad).SetTarget(t));
        }

        if (autoHideDelay > 0f)
        {
            seq.AppendInterval(autoHideDelay);
            seq.Append(trayNotificationGroup.DOFade(0f, 0.2f).SetEase(Ease.InQuad));
            seq.OnComplete(() =>
            {
                if (trayNotificationGroup != null)
                {
                    trayNotificationGroup.gameObject.SetActive(false);
                }
            });
        }
    }
}
