using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UITopInGame : BaseScreen
{
    private enum LevelUiType
    {
        Normal,
        Hard,
        SuperHard
    }

    [Serializable]
    private class LevelUiConfig
    {
        public LevelUiType levelType;
        public string textLevelType;
        public Color colorLevel = Color.white;
        public Sprite settingButton;
        public Sprite levelImage1;
        public Sprite levelImage2;
    }

    public override bool ManualHide => true;
    public override bool DestroyOnHide => false;
    public override bool UseBehindPanel => false;
    
    [SerializeField] private TextMeshProUGUI txt_level;
    [SerializeField] private TextMeshProUGUI txt_level_type;
    [SerializeField] private TextMeshProUGUI txt_coin;
    [SerializeField] private ButtonBehavior buttonSetting;
    [SerializeField] private Image imgSettingButton;
    [SerializeField] private Image imgLevel1;
    [SerializeField] private Image imgLevel2;
    [SerializeField] private List<LevelUiConfig> levelUiConfigs = new List<LevelUiConfig>();

    [SerializeField] private CanvasGroup trayNotificationGroup;
    [SerializeField] private TextMeshProUGUI textDeps;

    [SerializeField] private RectTransform infoSelectButon;
   
    [SerializeField] private List<Image> warningNotice;
    [SerializeField] private Color warningNoticeColor = Color.red;
    [SerializeField] private float warningNoticeBlinkDuration = 0.3f;

    private bool _isWarningNoticeBlinking;
    private readonly List<Tween> _warningNoticeTweens = new List<Tween>();
    private readonly List<Color> _warningNoticeDefaultColors = new List<Color>();
    private int _lastDisplayedCoin = int.MinValue;
    private int _lastDisplayedLevel = int.MinValue;
    private int _lastHardAnimationLevel = int.MinValue;
    private int _lastSuperHardAnimationLevel = int.MinValue;
    private LevelUiConfig _activeLevelUiConfig;
    private bool _settingButtonRegistered;
    private Tween _hardObjectTween;
    private Tween _superHardObjectTween;

    [SerializeField] private GameObject HardObject;
    [SerializeField] private Animator ani_hard;
    [SerializeField] private GameObject SuperHardObject;
    [SerializeField] private Animator ani_super_hard;
    [SerializeField] private float difficultyPopupHoldDuration = 0.2f;

    private const string PopupOpenStateName = "Open";
    private const string PopupCloseStateName = "Close";

    private static LevelUiType GetLevelUiType(int level)
    {
        int lastDigit = Mathf.Abs(level) % 10;
        if (level >= 18 && lastDigit == 8) return LevelUiType.SuperHard;
        if (level > 0 && level % 10 == 0) return LevelUiType.Hard;
        if (level >= 18 && lastDigit == 3) return LevelUiType.Hard;
        return LevelUiType.Normal;
    }

    private void Start()
    {
        RegisterSettingButtonListener();
        
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
        StopHardObjectAutoHide();
        StopSuperHardObjectAutoHide();
        if (HardObject != null) HardObject.SetActive(false);
        if (SuperHardObject != null) SuperHardObject.SetActive(false);
        _lastDisplayedCoin = int.MinValue;
        _lastDisplayedLevel = int.MinValue;
        _lastHardAnimationLevel = int.MinValue;
        _lastSuperHardAnimationLevel = int.MinValue;
        _activeLevelUiConfig = null;
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

    public override void BeforeShow()
    {
        base.BeforeShow();

        SetConveyorWarning(false);
        RefreshCoin();
        RefreshLevel();
    }

    private void Update()
    {
        RefreshCoin();
        RefreshLevel();
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

    private void RefreshLevel()
    {
        int level = GameManagerInGame.Instance != null ? GameManagerInGame.Instance.CurrentLevel : 1;
        level = Mathf.Max(1, level);

        ApplyLevelUi(level);

        if (_lastDisplayedLevel == level) return;

        _lastDisplayedLevel = level;

        if (txt_level != null)
        {
            txt_level.text = level.ToString();
        }
    }

    private void RegisterSettingButtonListener()
    {
        if (_settingButtonRegistered) return;
        if (buttonSetting == null) return;

        buttonSetting.OnClick.AddListener(OnClickSetting);
        _settingButtonRegistered = true;
    }

    private void OnClickSetting()
    {
        UIManager.Instance.ShowUI<UISettingInGame>();
    }

    private void ApplyLevelUi(int level)
    {
        LevelUiType levelType = GetLevelUiType(level);
        _activeLevelUiConfig = GetLevelUiConfig(levelType);

        ApplyConfigVisibility(_activeLevelUiConfig);
        ApplyDifficultyObjects(level, levelType);

        if (_activeLevelUiConfig == null) return;

        bool isSpecialLevel = levelType != LevelUiType.Normal;
        ApplyConfigSprites(_activeLevelUiConfig);
        // AnimateLevelTarget(imgLevel1 != null ? imgLevel1.rectTransform : null, isSpecialLevel);
        // AnimateLevelTarget(imgLevel2 != null ? imgLevel2.rectTransform : null, isSpecialLevel);
    }

    private LevelUiConfig GetLevelUiConfig(LevelUiType levelType)
    {
        if (levelUiConfigs == null) return null;

        for (int i = 0; i < levelUiConfigs.Count; i++)
        {
            var config = levelUiConfigs[i];
            if (config != null && config.levelType == levelType)
                return config;
        }

        return null;
    }

    private void ApplyConfigVisibility(LevelUiConfig activeConfig)
    {
        bool hasActiveConfig = activeConfig != null;
        if (buttonSetting != null) buttonSetting.gameObject.SetActive(hasActiveConfig);
        if (imgLevel1 != null) imgLevel1.gameObject.SetActive(hasActiveConfig);
        if (imgLevel2 != null) imgLevel2.gameObject.SetActive(hasActiveConfig);
    }

    private void ApplyDifficultyObjects(int level, LevelUiType levelType)
    {
        if (levelType == LevelUiType.Hard)
        {
            if (_lastHardAnimationLevel != level)
            {
                _lastHardAnimationLevel = level;
                ShowHardObjectOnce();
            }
        }
        else
        {
            StopHardObjectAutoHide();
            if (HardObject != null)
                HardObject.SetActive(false);
        }

        if (levelType == LevelUiType.SuperHard)
        {
            if (_lastSuperHardAnimationLevel != level)
            {
                _lastSuperHardAnimationLevel = level;
                ShowSuperHardObjectOnce();
            }
        }
        else
        {
            StopSuperHardObjectAutoHide();
            if (SuperHardObject != null)
                SuperHardObject.SetActive(false);
        }
    }

    private void ShowHardObjectOnce()
    {
        StopHardObjectAutoHide();
        _hardObjectTween = PlayDifficultyObjectAnimation(HardObject, ani_hard);
    }

    private void StopHardObjectAutoHide()
    {
        _hardObjectTween?.Kill();
        _hardObjectTween = null;
    }

    private void ShowSuperHardObjectOnce()
    {
        StopSuperHardObjectAutoHide();
        _superHardObjectTween = PlayDifficultyObjectAnimation(SuperHardObject, ani_super_hard);
    }

    private void StopSuperHardObjectAutoHide()
    {
        _superHardObjectTween?.Kill();
        _superHardObjectTween = null;
    }

    private Tween PlayDifficultyObjectAnimation(GameObject targetObject, Animator animator)
    {
        if (targetObject == null) return null;
        targetObject.SetActive(true);

        if (animator == null)
            return null;

        float openDuration = GetAnimatorClipLength(animator, PopupOpenStateName);
        float closeDuration = GetAnimatorClipLength(animator, PopupCloseStateName);

        animator.Rebind();
        animator.Update(0f);
        animator.Play(PopupOpenStateName, 0, 0f);

        Sequence seq = DOTween.Sequence().SetTarget(targetObject);
        seq.AppendInterval(Mathf.Max(0f, openDuration) + difficultyPopupHoldDuration);
        seq.AppendCallback(() =>
        {
            if (animator != null && targetObject.activeInHierarchy)
                animator.Play(PopupCloseStateName, 0, 0f);
        });
        seq.AppendInterval(Mathf.Max(0f, closeDuration));
        seq.OnComplete(() =>
        {
            if (targetObject != null)
                targetObject.SetActive(false);
        });

        return seq;
    }

    private static float GetAnimatorClipLength(Animator animator, string clipName)
    {
        if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrEmpty(clipName))
            return 0f;

        var clips = animator.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip != null && clip.name == clipName)
                return clip.length;
        }

        return 0f;
    }

    private void ApplyConfigSprites(LevelUiConfig activeConfig)
    {
        if (activeConfig == null) return;

        if (imgSettingButton != null)
        {
            imgSettingButton.sprite = activeConfig.settingButton;
            imgSettingButton.enabled = activeConfig.settingButton != null;
        }

        if (imgLevel1 != null)
        {
            imgLevel1.sprite = activeConfig.levelImage1;
            imgLevel1.enabled = activeConfig.levelImage1 != null;
        }

        if (imgLevel2 != null)
        {
            imgLevel2.sprite = activeConfig.levelImage2;
            imgLevel2.enabled = activeConfig.levelImage2 != null;
        }
    }

    private void AnimateLevelTarget(RectTransform target, bool isSpecialLevel)
    {
        if (target == null) return;

        DOTween.Kill(target, false);
        target.localScale = Vector3.one * (isSpecialLevel ? 0.75f : 0.85f);
        target.localRotation = Quaternion.identity;
        target.anchoredPosition3D = target.anchoredPosition3D;

        Sequence seq = DOTween.Sequence().SetTarget(target);
        seq.Append(target.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack));
        if (!isSpecialLevel) return;

        seq.Join(target.DOPunchRotation(new Vector3(0f, 0f, 14f), 0.35f, 10, 0.8f));
        seq.Join(target.DOShakeAnchorPos(0.35f, new Vector2(10f, 0f), 12, 90f, false, true));
        seq.Append(target.DOScale(1.06f, 0.12f).SetEase(Ease.OutQuad));
        seq.Append(target.DOScale(1f, 0.12f).SetEase(Ease.InOutQuad));
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
