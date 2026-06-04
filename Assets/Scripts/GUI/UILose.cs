using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UILose : UIPopup
{
    private const string LifeRecoverClip = "ANI_life_recover";

    [Header("Legacy Optional Refs")]
    [SerializeField] private ButtonBehavior btn_next;
    [SerializeField] private ButtonBehavior btn_close_hide;
    [SerializeField] private TextMeshProUGUI txt_coin;
    [SerializeField] private ButtonBehavior btn_refill;
    [SerializeField] private ButtonBehavior btn_unlimited;
    [SerializeField] private TextMeshProUGUI txt_unlimitedTimer;
    [SerializeField] private GameObject obj_unlimitedLives;
    [SerializeField] private RectTransform panel_description;

    [Header("Lives Config")]
    [SerializeField] private int refillCoinPrice = 300;
    [SerializeField] private float unlimitedLivesMinutes = 15f;
    [SerializeField] private string notEnoughCoinToast = "Not enough coin";
    [SerializeField] private string fullLivesToast = "Lives are already full";
    [SerializeField] private string nextLifeToastFormat = "Next life in {0}";
    [SerializeField] private string lockedLifeToast = "Extra life slots are locked";
    [SerializeField] private string refillPromptToastFormat = "Refill for {0}";
    [SerializeField] private float loseHeartFadeDelay = 0.2f;
    [SerializeField] private float loseHeartFadeDuration = 0.35f;

    private ButtonBehavior _closeButton;
    private ButtonBehavior _refillButton;

    [SerializeField] private GameObject _obj_first_refill_free;
    [SerializeField] private GameObject _obj_first_refill_coin;

    private ButtonBehavior _unlimitedButton;
    private TextMeshProUGUI _coinValueText;
    private TextMeshProUGUI _timerText;
    [SerializeField] private TextMeshProUGUI txt_refillPrice;
    private GameObject _unlimitedLivesBanner;
    private RectTransform _descriptionPanel;
    private readonly List<LiveContainerView> _liveContainers = new();
    private readonly List<LiveContainerView> _specialContainers = new();
    private bool _bound;
    private bool _subscribed;
    private bool _isPlayingLoseHeartAnimation;
    private Tween _loseHeartTween;

    public override void BeforeShow()
    {
        base.BeforeShow();
        GameManagerInGame.Instance?.CommitPendingLose();
        BindIfNeeded();
        SubscribeEvents();
        PlayLoseHeartFadeIfNeeded();
        Game.Update.AddTask(UpdateCountdown);
    }

    public override void BeforeHide()
    {
        base.BeforeHide();
        _loseHeartTween?.Kill(false);
        _loseHeartTween = null;
        _isPlayingLoseHeartAnimation = false;
        Game.Update.RemoveTask(UpdateCountdown);
        UnsubscribeEvents();

        GameManagerInGame.Instance?.ReplayLevel();
    }

    protected override void Start()
    {
        base.Start();
        BindIfNeeded();
    }

    protected override void OnDestroy()
    {
        _loseHeartTween?.Kill(false);
        _loseHeartTween = null;
        Game.Update.RemoveTask(UpdateCountdown);
        UnsubscribeEvents();
        base.OnDestroy();
    }

    private void BindIfNeeded()
    {
        if (_bound) return;
        _bound = true;

        _closeButton = btn_close_hide != null ? btn_close_hide : FindButtonByName("ButtonClose");
        if (_closeButton == null) _closeButton = FindButtonByName("ButtonCloseEnabled");
        _refillButton = btn_refill != null ? btn_refill : FindButtonByName("LivesRefillButton");
        _unlimitedButton = btn_unlimited != null ? btn_unlimited : FindButtonByName("ButtonPopupUnlimitedAD");

        _coinValueText = txt_coin != null ? txt_coin : FindTextByName("CoinValue");
        _timerText = txt_unlimitedTimer != null ? txt_unlimitedTimer : FindTextByName("UnlimitedLivesTimerText");
        // _refillPriceText = txt_refillPrice;
        
        _unlimitedLivesBanner = obj_unlimitedLives != null ? obj_unlimitedLives : FindDeepChild("UnlimitedLives")?.gameObject;
        _descriptionPanel = panel_description != null ? panel_description : FindDeepChild("DescriptionPanel") as RectTransform;

        if (_closeButton != null) _closeButton.OnClick.AddListener(OnClosePressed);
        if (_refillButton != null) _refillButton.OnClick.AddListener(OnRefillPressed);
        if (_unlimitedButton != null) _unlimitedButton.OnClick.AddListener(OnUnlimitedPressed);
        if (btn_next != null) btn_next.gameObject.SetActive(false);

        CacheLiveContainers();
    }

    private void CacheLiveContainers()
    {
        _liveContainers.Clear();
        _specialContainers.Clear();

        Transform root = _descriptionPanel != null ? _descriptionPanel : transform;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform current = root.GetChild(i);
            if (current == null) continue;
            if (!current.name.StartsWith("LiveContainer", StringComparison.Ordinal)) continue;

            LiveContainerView view = BuildLiveContainerView(current);
            if (current.name.Contains("Special", StringComparison.Ordinal))
            {
                _specialContainers.Add(view);
            }
            else
            {
                _liveContainers.Add(view);
            }
        }

        SortViews(_liveContainers);
        SortViews(_specialContainers);
    }

    private void SubscribeEvents()
    {
        if (_subscribed) return;
        if (!HeatManager.TryGetInstance(out HeatManager heatManager)) return;

        heatManager.OnHeatChanged += RefreshUI;
        heatManager.OnUnlimitedHeatChanged += RefreshUI;
        _subscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!_subscribed) return;
        if (HeatManager.TryGetInstance(out HeatManager heatManager))
        {
            heatManager.OnHeatChanged -= RefreshUI;
            heatManager.OnUnlimitedHeatChanged -= RefreshUI;
        }

        _subscribed = false;
    }

    private void RefreshUI()
    {
        if (_isPlayingLoseHeartAnimation) return;

        UserData userData = GetUserData();
        HeatManager heatManager = HeatManager.TryGetInstance();
        int currentCoin = userData != null ? userData.playerCash : 0;
        bool hasUnlimited = heatManager != null && heatManager.HasUnlimitedHeat();
        int currentHeat = heatManager != null
            ? heatManager.GetCurrentHeat()
            : (userData != null ? Mathf.Clamp(userData.playerHeat, 0, HeatManager.MAX_HEAT_DAY) : 0);

        if (_coinValueText != null) _coinValueText.text = currentCoin.ToString();
        if (txt_refillPrice != null) txt_refillPrice.text = refillCoinPrice.ToString();
        
        if (_unlimitedLivesBanner != null) _unlimitedLivesBanner.SetActive(hasUnlimited);
        bool hasUsedFreeRefill = HasUsedFreeRefill(userData);
        if (_obj_first_refill_free != null) _obj_first_refill_free.SetActive(!hasUsedFreeRefill);
        if (_obj_first_refill_coin != null) _obj_first_refill_coin.SetActive(hasUsedFreeRefill);

        UpdateLiveContainers(currentHeat, hasUnlimited, heatManager);
        UpdateCountdown();

        if (_refillButton != null)
        {
            bool canRefill = !hasUnlimited && currentHeat < HeatManager.MAX_HEAT_DAY;
            _refillButton.SetInteractable(canRefill);
        }

        if (_unlimitedButton != null)
        {
            _unlimitedButton.SetInteractable(true);
        }
    }

    private void PlayLoseHeartFadeIfNeeded()
    {
        _loseHeartTween?.Kill(false);
        _loseHeartTween = null;

        HeatManager heatManager = HeatManager.TryGetInstance();
        if (heatManager == null || heatManager.HasUnlimitedHeat())
        {
            _isPlayingLoseHeartAnimation = false;
            RefreshUI();
            return;
        }

        int currentHeat = heatManager.GetCurrentHeat();
        int previousHeat = Mathf.Clamp(currentHeat + 1, 0, HeatManager.MAX_HEAT_DAY);
        if (previousHeat <= 0 || previousHeat == currentHeat)
        {
            _isPlayingLoseHeartAnimation = false;
            RefreshUI();
            return;
        }

        _isPlayingLoseHeartAnimation = true;
        ApplyLiveContainerState(previousHeat, hasUnlimited: false, showRecoveringHeart: false, TimeSpan.Zero);

        int lostIndex = Mathf.Clamp(previousHeat - 1, 0, _liveContainers.Count - 1);
        if (lostIndex < 0 || lostIndex >= _liveContainers.Count)
        {
            _isPlayingLoseHeartAnimation = false;
            RefreshUI();
            return;
        }

        LiveContainerView lostSlot = _liveContainers[lostIndex];
        CanvasGroup fadeTarget = EnsureCanvasGroup(lostSlot.fullHeart);
        if (fadeTarget == null)
        {
            _isPlayingLoseHeartAnimation = false;
            RefreshUI();
            return;
        }

        fadeTarget.alpha = 1f;
        _loseHeartTween = fadeTarget
            .DOFade(0f, loseHeartFadeDuration)
            .SetDelay(loseHeartFadeDelay)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                fadeTarget.alpha = 1f;
                _loseHeartTween = null;
                _isPlayingLoseHeartAnimation = false;
                RefreshUI();
            });
    }

    private void UpdateLiveContainers(int currentHeat, bool hasUnlimited, HeatManager heatManager)
    {
        if (_liveContainers.Count == 0 && _specialContainers.Count == 0) return;

        TimeSpan nextHeatTime = heatManager != null ? heatManager.GetTimeUntilNextHeat() : TimeSpan.Zero;
        bool showRecoveringHeart = !hasUnlimited
            && currentHeat < HeatManager.MAX_HEAT_DAY
            && nextHeatTime.TotalSeconds > 0;

        ApplyLiveContainerState(currentHeat, hasUnlimited, showRecoveringHeart, nextHeatTime);
    }

    private void ApplyLiveContainerState(int currentHeat, bool hasUnlimited, bool showRecoveringHeart, TimeSpan nextHeatTime)
    {
        if (_liveContainers.Count == 0 && _specialContainers.Count == 0) return;

        for (int i = 0; i < _liveContainers.Count; i++)
        {
            LiveContainerView slot = _liveContainers[i];
            bool unlocked = i < HeatManager.MAX_HEAT_DAY;
            if (slot.root != null) slot.root.gameObject.SetActive(unlocked);
            if (!unlocked) continue;

            bool isFilled = hasUnlimited || i < currentHeat;
            bool isRecovering = showRecoveringHeart && i == currentHeat;

            slot.isLocked = false;
            slot.isRecovering = isRecovering;
            slot.isAvailable = isFilled;

            ResetCanvasGroup(slot.fullHeart);
            SetActive(slot.fullHeart, isFilled);
            SetActive(slot.recoveringHeart, isRecovering);
            SetActive(slot.lockedHeart, false);
            SetActive(slot.lockIcon, false);
            SetText(slot.timerText, isRecovering ? FormatTimeShort(nextHeatTime) : string.Empty);
            SetActive(slot.timerRoot, isRecovering);
            SetText(slot.amountText, isFilled ? "1" : string.Empty);

            bool shouldAnimate = (isFilled && !slot.wasAvailable) || (isRecovering && !slot.wasRecovering);
            if (shouldAnimate)
            {
                PlayAnimationIfExists(slot.animation, LifeRecoverClip, restart: true);
            }

            slot.wasAvailable = isFilled;
            slot.wasRecovering = isRecovering;
        }

        for (int i = 0; i < _specialContainers.Count; i++)
        {
            LiveContainerView slot = _specialContainers[i];
            slot.isLocked = true;
            slot.isRecovering = false;
            slot.isAvailable = false;

            if (slot.root != null) slot.root.gameObject.SetActive(true);
            SetActive(slot.fullHeart, false);
            SetActive(slot.recoveringHeart, false);
            SetActive(slot.lockedHeart, true);
            SetActive(slot.lockIcon, true);
            SetText(slot.timerText, string.Empty);
            SetActive(slot.timerRoot, false);
            SetText(slot.amountText, $"+{i + 1}");
            slot.wasAvailable = false;
            slot.wasRecovering = false;
        }
    }

    private void UpdateCountdown()
    {
        if (_timerText == null) return;

        HeatManager heatManager = HeatManager.TryGetInstance();
        if (heatManager == null)
        {
            _timerText.text = string.Empty;
            return;
        }

        if (heatManager.HasUnlimitedHeat())
        {
            TimeSpan remaining = heatManager.GetUnlimitedHeatTimeRemaining();
            if (remaining.TotalSeconds <= 0)
            {
                _timerText.text = "00m 00s";
            }
            else if (remaining.TotalHours >= 1d)
            {
                _timerText.text = $"{(int)remaining.TotalHours:D2}h {remaining.Minutes:D2}m";
            }
            else
            {
                _timerText.text = $"{remaining.Minutes:D2}m {remaining.Seconds:D2}s";
            }
            return;
        }

        int currentHeat = heatManager.GetCurrentHeat();
        if (currentHeat >= HeatManager.MAX_HEAT_DAY)
        {
            _timerText.text = "Full!";
            return;
        }

        TimeSpan nextHeat = heatManager.GetTimeUntilNextHeat();
        _timerText.text = nextHeat.TotalSeconds > 0
            ? $"{nextHeat.Hours:D2}:{nextHeat.Minutes:D2}:{nextHeat.Seconds:D2}"
            : "Ready!";
    }

    private void OnClosePressed()
    {
        UIManager.Instance.HideUI<UILose>();
    }

    private void OnRefillPressed()
    {
        if (!TrySpendRefillCoin())
        {
            ShowToast(notEnoughCoinToast);
            RefreshUI();
            return;
        }

        HeatManager heatManager = HeatManager.TryGetInstance();
        if (heatManager != null)
        {
            heatManager.RefillToMax();
        }
        else
        {
            UserData userData = GetUserData();
            if (userData != null)
            {
                userData.playerHeat = HeatManager.MAX_HEAT_DAY;
                userData.lastTimePlayGame = string.Empty;
                userData.Save();
            }
        }

        RefreshUI();
    }

    private bool TrySpendRefillCoin()
    {
        UserData userData = GetUserData();
        if (userData == null) return false;

        if (!HasUsedFreeRefill(userData))
        {
            userData.hasClaimedFreeLoseRefill = true;
            userData.Save();
            return true;
        }

        int currentRefillCoinPrice = GetCurrentRefillCoinPrice(userData);
        if (currentRefillCoinPrice <= 0) return true;

        if (InventoryManager.Instance != null)
        {
            return InventoryManager.Instance.SpendCoin(currentRefillCoinPrice);
        }

        if (userData.playerCash < currentRefillCoinPrice)
        {
            return false;
        }

        userData.playerCash -= currentRefillCoinPrice;
        userData.Save();
        return true;
    }

    private int GetCurrentRefillCoinPrice(UserData userData = null)
    {
        userData ??= GetUserData();
        return HasUsedFreeRefill(userData) ? refillCoinPrice : 0;
    }

    private static bool HasUsedFreeRefill(UserData userData)
    {
        return userData != null && userData.hasClaimedFreeLoseRefill;
    }

    private void OnUnlimitedPressed()
    {
        HeatManager heatManager = HeatManager.TryGetInstance();
        if (heatManager != null)
        {
            heatManager.AddUnlimitedHeat(unlimitedLivesMinutes / 60f);
        }
        else
        {
            UserData userData = GetUserData();
            if (userData != null)
            {
                userData.hasUnlimitedHeat = true;
                userData.unlimitedHeatExpireTime = DateTime.Now.AddMinutes(unlimitedLivesMinutes).ToString();
                userData.Save();
            }
        }

        RefreshUI();
    }

    private void ShowToast(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        UINotification notification = UIManager.Instance != null ? UIManager.Instance.Get<UINotification>() : null;
        if (notification != null)
        {
            notification.ShowToast(message);
        }
    }

    private UserData GetUserData()
    {
        GameManagerInGame gameManager = GameManagerInGame.Instance;
        if (gameManager != null && gameManager.userData != null)
        {
            return gameManager.userData;
        }

        return Game.Data.Load<UserData>();
    }

    private ButtonBehavior FindButtonByName(string name)
    {
        Transform target = FindDeepChild(name);
        return target != null ? target.GetComponent<ButtonBehavior>() : null;
    }

    private TextMeshProUGUI FindTextByName(string name)
    {
        Transform target = FindDeepChild(name);
        return target != null ? target.GetComponent<TextMeshProUGUI>() : null;
    }

    private Transform FindDeepChild(string childName)
    {
        Transform[] transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current != null && current.name == childName)
            {
                return current;
            }
        }

        return null;
    }

    private static Transform FindDirectChild(Transform root, string childName)
    {
        if (root == null) return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child != null && child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    [Serializable]
    private sealed class LiveContainerView
    {
        public RectTransform root;
        public Button button;
        public Animation animation;
        public GameObject fullHeart;
        public GameObject recoveringHeart;
        public GameObject lockedHeart;
        public GameObject lockIcon;
        public GameObject timerRoot;
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI amountText;
        public bool isLocked;
        public bool isRecovering;
        public bool isAvailable;
        public bool wasRecovering;
        public bool wasAvailable;
    }

    private LiveContainerView BuildLiveContainerView(Transform root)
    {
        LiveContainerView view = new LiveContainerView
        {
            root = root as RectTransform,
            button = root.GetComponent<Button>(),
            animation = FindDeepChild(root, "Slot")?.GetComponent<Animation>(),
            fullHeart = FindDeepChild(root, "FullHeart")?.gameObject,
            recoveringHeart = FindDeepChild(root, "RecoveringHeart")?.gameObject,
            lockedHeart = FindDeepChild(root, "LockedHeart")?.gameObject,
            lockIcon = FindDeepChild(root, "Lock")?.gameObject,
            timerRoot = FindDeepChild(root, "Timer")?.gameObject,
            timerText = FindTextByName(root, "Timer"),
            amountText = FindTextByName(root, "HeartAmount")
        };

        if (view.button != null)
        {
            view.button.onClick.AddListener(() => OnLiveContainerPressed(view));
        }

        return view;
    }

    private void OnLiveContainerPressed(LiveContainerView view)
    {
        if (view == null) return;

        if (view.isLocked)
        {
            ShowToast(lockedLifeToast);
            return;
        }

        if (view.isAvailable)
        {
            ShowToast(fullLivesToast);
            return;
        }

        if (view.isRecovering)
        {
            HeatManager heatManager = HeatManager.TryGetInstance();
            TimeSpan nextHeatTime = heatManager != null ? heatManager.GetTimeUntilNextHeat() : TimeSpan.Zero;
            ShowToast(string.Format(nextLifeToastFormat, FormatTimeShort(nextHeatTime)));
            return;
        }

        if (_refillButton != null)
        {
            int currentRefillCoinPrice = GetCurrentRefillCoinPrice();
            ShowToast(currentRefillCoinPrice > 0
                ? string.Format(refillPromptToastFormat, currentRefillCoinPrice)
                : "First refill is free");
        }
    }

    private static void SortViews(List<LiveContainerView> views)
    {
        views.Sort((a, b) =>
        {
            if (a.root == null || b.root == null) return 0;
            int byRow = b.root.anchoredPosition.y.CompareTo(a.root.anchoredPosition.y);
            return byRow != 0 ? byRow : a.root.anchoredPosition.x.CompareTo(b.root.anchoredPosition.x);
        });
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null) return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName) return child;

            Transform nested = FindDeepChild(child, childName);
            if (nested != null) return nested;
        }

        return null;
    }

    private static TextMeshProUGUI FindTextByName(Transform root, string name)
    {
        Transform target = FindDeepChild(root, name);
        return target != null ? target.GetComponent<TextMeshProUGUI>() : null;
    }

    private static void SetActive(GameObject target, bool value)
    {
        if (target != null) target.SetActive(value);
    }

    private static void SetText(TextMeshProUGUI target, string value)
    {
        if (target != null) target.text = value;
    }

    private static CanvasGroup EnsureCanvasGroup(GameObject target)
    {
        if (target == null) return null;
        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = target.AddComponent<CanvasGroup>();
        }
        return group;
    }

    private static void ResetCanvasGroup(GameObject target)
    {
        CanvasGroup group = EnsureCanvasGroup(target);
        if (group != null) group.alpha = 1f;
    }

    private static string FormatTimeShort(TimeSpan time)
    {
        if (time.TotalSeconds <= 0) return "00:00";
        if (time.TotalHours >= 1) return $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
        return $"{time.Minutes:D2}:{time.Seconds:D2}";
    }

    private static void PlayAnimationIfExists(Animation animation, string clipName, bool restart)
    {
        if (animation == null || string.IsNullOrEmpty(clipName)) return;
        if (animation.GetClip(clipName) == null) return;

        if (restart && animation.IsPlaying(clipName))
        {
            animation.Stop(clipName);
        }

        animation.Play(clipName, PlayMode.StopAll);
    }
}
