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
    private LevelUiConfig _activeLevelUiConfig;
    private bool _settingButtonRegistered;

    private static LevelUiType GetLevelUiType(int level)
    {
        if (level > 0 && level % 10 == 0) return LevelUiType.SuperHard;
        if (level == 10) return LevelUiType.SuperHard;
        if (level < 18) return LevelUiType.Normal;
        int lastDigit = Mathf.Abs(level) % 10;
        return (lastDigit == 3 || lastDigit == 8) ? LevelUiType.Hard : LevelUiType.Normal;
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
        _lastDisplayedCoin = int.MinValue;
        _lastDisplayedLevel = int.MinValue;
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
            string levelTypeText = "Level";
            Color levelColor = txt_level_type != null ? txt_level_type.color : txt_level.color;

            if (_activeLevelUiConfig != null)
            {
                if (!string.IsNullOrEmpty(_activeLevelUiConfig.textLevelType))
                    levelTypeText = _activeLevelUiConfig.textLevelType;

                levelColor = _activeLevelUiConfig.colorLevel;
            }

            txt_level.text = level.ToString();
            if (txt_level_type != null)
            {
                txt_level_type.text = levelTypeText;
                txt_level_type.color = levelColor;
            }
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
