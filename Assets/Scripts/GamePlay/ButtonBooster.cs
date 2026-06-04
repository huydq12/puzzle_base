using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonBooster : MonoBehaviour
{
    private const string LockedShowClip = "BoosterButtonLockedGroupTextShow";
    private const string UnlockedDefaultClip = "ANI_Booster_Button_Enabled";
    private const string UnlockedHasCountClip = "ANI_Booster_Button_Active";
    private const string UnlockedNoCountClip = "ANI_Booster_Button_Disabled";
    private const string UnlockedNoCountClickClip = "ANI_Booster_Button_Disabled_Click";
    private const string UnlockedUnlockClip = "ANI_Booster_Button_Unlock";
    private const string UnlockedActivatingClip = "ANI_Booster_Button_Activating";
    private const string HighlightClip = "BoosterButtonTimedHighlightShow_0";

    private Button _lockedButton;
    private Button _unlockedButton;
    private GameObject _coinGroupCount;
    private GameObject _coinGroupPrice;
    private GameObject _lockedGroup;
    private GameObject _unlockedGroup;
    private GameObject _timedHighlights;
    private GameObject _purchaseButton;
    private GameObject _addIcon;
    private GameObject _freeGroup;
    private Image _boosterIcon;
    private TextMeshProUGUI _quantityText;
    private TextMeshProUGUI _lockedLevelText;
    private Animation _lockedAnimation;
    private Animation _unlockedAnimation;
    private Animation _timedHighlightsAnimation;
    private Coroutine _timedHighlightRoutine;
    private bool _cached;
    private bool? _lastUnlocked;
    private bool? _lastHasAny;
    private Action _onPressed;

    public RectTransform ClickTargetRect
    {
        get
        {
            CacheRefs();

            if (_unlockedGroup != null && _unlockedGroup.activeInHierarchy && _unlockedButton != null)
            {
                return _unlockedButton.transform as RectTransform;
            }

            if (_lockedGroup != null && _lockedGroup.activeInHierarchy && _lockedButton != null)
            {
                return _lockedButton.transform as RectTransform;
            }

            return transform as RectTransform;
        }
    }

    public RectTransform IconTargetRect
    {
        get
        {
            CacheRefs();
            return _boosterIcon != null ? _boosterIcon.rectTransform : ClickTargetRect;
        }
    }

    public Sprite IconSprite
    {
        get
        {
            CacheRefs();
            return _boosterIcon != null ? _boosterIcon.sprite : null;
        }
    }

    private void Awake()
    {
        CacheRefs();
        BindButtons();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _cached = false;
        CacheRefs();
    }
#endif

    public void Configure(Sprite iconSprite, bool unlocked, int count, int unlockLevel, Action onPressed)
    {
        CacheRefs();

        _onPressed = onPressed;
        BindButtons();

        if (_boosterIcon != null)
        {
            _boosterIcon.overrideSprite = iconSprite;
            _boosterIcon.sprite = iconSprite;
            _boosterIcon.enabled = iconSprite != null;
            _boosterIcon.SetAllDirty();
        }

        if (_lockedGroup != null) _lockedGroup.SetActive(!unlocked);
        if (_unlockedGroup != null) _unlockedGroup.SetActive(unlocked);
        if (_freeGroup != null) _freeGroup.SetActive(false);

        if (_lockedLevelText != null)
        {
            _lockedLevelText.gameObject.SetActive(!unlocked);
            _lockedLevelText.text = $"Reach Level {Mathf.Max(1, unlockLevel)}";
        }

        bool hasAny = count > 0;
        bool stateChanged = _lastUnlocked != unlocked || _lastHasAny != hasAny;

        if (_coinGroupCount != null) _coinGroupCount.SetActive(unlocked && hasAny);
        if (_coinGroupPrice != null) _coinGroupPrice.SetActive(unlocked && !hasAny);
        if (_purchaseButton != null) _purchaseButton.SetActive(unlocked && !hasAny);

        if (_addIcon != null) _addIcon.SetActive(unlocked && !hasAny);

        if (_quantityText != null)
        {
            _quantityText.gameObject.SetActive(unlocked && hasAny);
            _quantityText.text = unlocked ? $"x{Mathf.Max(0, count)}" : string.Empty;
        }

        if (!unlocked || !hasAny)
        {
            HideTimedHighlight();
        }

        PlayStateAnimation(unlocked, hasAny, stateChanged);
        _lastUnlocked = unlocked;
        _lastHasAny = hasAny;
    }

    private void BindButtons()
    {
        if (_lockedButton != null)
        {
            _lockedButton.onClick.RemoveListener(HandlePressed);
            _lockedButton.onClick.AddListener(HandlePressed);
        }

        if (_unlockedButton != null)
        {
            _unlockedButton.onClick.RemoveListener(HandlePressed);
            _unlockedButton.onClick.AddListener(HandlePressed);
        }
    }

    private void HandlePressed()
    {
        bool isUnlocked = _unlockedGroup != null && _unlockedGroup.activeSelf;
        bool hasAny = _coinGroupCount != null && _coinGroupCount.activeSelf;

        if (!isUnlocked)
        {
            PlayAnimationIfExists(_lockedAnimation, LockedShowClip, restart: true);
        }
        else if (hasAny)
        {
            PlayAnimationIfExists(_unlockedAnimation, UnlockedActivatingClip, restart: true);
        }
        else
        {
            PlayAnimationIfExists(_unlockedAnimation, UnlockedNoCountClickClip, restart: true);
        }

        _onPressed?.Invoke();
    }

    private void CacheRefs()
    {
        if (_cached) return;
        _cached = true;

        _lockedGroup = FindDeepChild("LockedGroup")?.gameObject;
        _unlockedGroup = FindDeepChild("UnlockedGroup")?.gameObject;
        _timedHighlights = FindDeepChild("TimedHighlights")?.gameObject;
        _coinGroupCount = FindDeepChild("CoinGroupCount")?.gameObject;
        _coinGroupPrice = FindDeepChild("CoinGroupPrice")?.gameObject;
        _purchaseButton = FindDeepChild("PurchaseButton")?.gameObject;
        _addIcon = FindDeepChild("AddIcon (1)")?.gameObject;
        _freeGroup = FindDeepChild("FreeGroup")?.gameObject;

        if (_lockedGroup != null) _lockedButton = _lockedGroup.GetComponent<Button>();
        if (_unlockedGroup != null) _unlockedButton = _unlockedGroup.GetComponent<Button>();
        if (_lockedGroup != null) _lockedAnimation = _lockedGroup.GetComponent<Animation>();
        if (_unlockedGroup != null) _unlockedAnimation = _unlockedGroup.GetComponent<Animation>();
        if (_timedHighlights != null) _timedHighlightsAnimation = _timedHighlights.GetComponent<Animation>();

        Transform iconRoot = FindDeepChild("Booster");
        if (iconRoot != null) _boosterIcon = iconRoot.GetComponent<Image>();

        Transform quantityRoot = _coinGroupCount != null ? _coinGroupCount.transform : null;
        if (quantityRoot != null)
        {
            _quantityText = quantityRoot.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        Transform lockedLevelRoot = FindDeepChild("LockedLevelText");
        if (lockedLevelRoot != null)
        {
            _lockedLevelText = lockedLevelRoot.GetComponent<TextMeshProUGUI>();
        }
    }

    private void PlayStateAnimation(bool unlocked, bool hasAny, bool stateChanged)
    {
        if (!stateChanged) return;

        if (!unlocked)
        {
            PlayAnimationIfExists(_lockedAnimation, LockedShowClip, restart: true);
            return;
        }

        string clipName = _lastUnlocked == false
            ? UnlockedUnlockClip
            : hasAny ? UnlockedHasCountClip : UnlockedNoCountClip;

        if (!PlayAnimationIfExists(_unlockedAnimation, clipName, restart: true))
        {
            PlayAnimationIfExists(_unlockedAnimation, UnlockedDefaultClip, restart: true);
        }

        if ((_lastHasAny == false || _lastUnlocked == false) && hasAny)
        {
            PlayTimedHighlight();
        }
    }

    public void PlayTimedHighlight()
    {
        if (_timedHighlights == null || _timedHighlightsAnimation == null) return;
        if (!PlayAnimationIfExists(_timedHighlightsAnimation, HighlightClip, restart: true)) return;

        _timedHighlights.SetActive(true);

        if (_timedHighlightRoutine != null)
        {
            StopCoroutine(_timedHighlightRoutine);
        }

        _timedHighlightRoutine = StartCoroutine(HideTimedHighlightAfterDelay(GetClipLength(_timedHighlightsAnimation, HighlightClip)));
    }

    private static bool PlayAnimationIfExists(Animation animationComponent, string clipName, bool restart)
    {
        if (animationComponent == null || string.IsNullOrEmpty(clipName)) return false;

        AnimationState state = animationComponent[clipName];
        if (state == null) return false;

        if (restart)
        {
            animationComponent.Stop(clipName);
            state.time = 0f;
        }

        animationComponent.Play(clipName);
        return true;
    }

    private IEnumerator HideTimedHighlightAfterDelay(float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }
        else
        {
            yield return null;
        }

        HideTimedHighlight();
    }

    private void HideTimedHighlight()
    {
        if (_timedHighlightRoutine != null)
        {
            StopCoroutine(_timedHighlightRoutine);
            _timedHighlightRoutine = null;
        }

        if (_timedHighlights != null)
        {
            _timedHighlights.SetActive(false);
        }
    }

    private static float GetClipLength(Animation animationComponent, string clipName)
    {
        if (animationComponent == null || string.IsNullOrEmpty(clipName)) return 0f;
        AnimationState state = animationComponent[clipName];
        return state != null && state.clip != null ? state.clip.length : 0f;
    }

    private Transform FindDeepChild(string childName)
    {
        return FindDeepChild(transform, childName);
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null) return null;
        if (root.name == childName) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), childName);
            if (found != null) return found;
        }

        return null;
    }
}
