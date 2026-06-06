using System.Collections;
using System.Collections.Generic;
using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

public class UIBottomInGame : BaseScreen
{
    public override bool ManualHide => false;
    public override bool DestroyOnHide => false;
    public override bool UseBehindPanel => false;

    public Button BoosterButtonType1;
    public Button BoosterButtonType2;
    public Button BoosterButtonType3;
    public Button BoosterButtonType4;

    public TextMeshProUGUI BoosterTextType1;
    public TextMeshProUGUI BoosterTextType2;
    public TextMeshProUGUI BoosterTextType3;
    public TextMeshProUGUI BoosterTextType4;

    public GameObject iconType1;
    public GameObject iconType2;
    public GameObject iconType3;
    public GameObject iconType4;

    public GameObject iconType1Locked;
    public GameObject iconType2Locked;
    public GameObject iconType3Locked;
    public GameObject iconType4Locked;

    public GameObject iconType1Add;
    public GameObject iconType2Add;
    public GameObject iconType3Add;
    public GameObject iconType4Add;

    [Header("Lock State")]
    [SerializeField] private Image bgType1;
    [SerializeField] private Image bgType2;
    [SerializeField] private Image bgType3;
    [SerializeField] private Image bgType4;

    [SerializeField] private Sprite bgUnlockedSprite;
    [SerializeField] private Sprite bgLockedSprite;

    public Image fillLevel;
    public TextMeshProUGUI _percentLevel;

    [Header("Conveyor Level Colors")]
    [SerializeField] private Color _fillColor0To25 = Color.green;
    [SerializeField] private Color _fillColor25To50 = Color.yellow;
    [SerializeField] private Color _fillColor50To75 = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color _fillColor75To100 = Color.red;
    [SerializeField] private float _fillSmoothTime = 0.2f;

    private Tween _fillTween;

    [SerializeField] private float _slideDuration = 0.25f;
    [SerializeField] private float _slideOffsetY = 200f;

    private RectTransform _holderRect;
    private Vector2 _holderShownPos;
    private bool _holderPosCached;
    private Tween _slideTween;

    [Header("Group Booster")]
    [SerializeField] private GameObject _groupBooster;
    [SerializeField] private RectTransform _spawnPointBooster;
    [SerializeField] private Image _imageBooster;
    [SerializeField] private TextMeshProUGUI _textBooster;
    [SerializeField] private TextMeshProUGUI _textBoosterDesp;
    [SerializeField] private Button _buttonCancelBooster;

    [SerializeField] private Sprite _spriteBoosterHammer;
    [SerializeField] private Sprite _spriteBoosterConveyor;
    [SerializeField] private Sprite _spriteBoosterShuffle;
    
    [SerializeField] private string _textBoosterHammer = "Hammer !";
    [SerializeField] private string _textBoosterConveyor = "Color Picker !";
    [SerializeField] private string _textBoosterShuffle = "Shuffle !";
    

    [SerializeField] private string _textDespBoosterHammer = "Pick a color on the \n converyor to destroy!";
    [SerializeField] private string _textDespBoosterConveyor = "Pick an arrow to \n destroy!";
    [SerializeField] private string _textDespBoosterShuffle = "Shuffle Shooter on Gate!";

    private BoosterType _lastBooster = (BoosterType)(-1);

    [Header("Tutorial Drop FX")]
    [SerializeField] private float _tutorialDropDuration = 0.6f;
    [SerializeField] private float _tutorialDropScatterX = 120f;
    [SerializeField] private float _tutorialDropStartScale = 0.9f;
    [SerializeField] private float _tutorialDropEndScale = 0.75f;



    private void Start()
    {
        // Only 3 boosters are supported in this mode: Hammer / Conveyor / Rainbow.
        // We reuse the first 3 button slots and hide the 4th one (Swap).
        if (BoosterButtonType1 != null) BoosterButtonType1.onClick.AddListener(UseHammer);
        if (BoosterButtonType2 != null) BoosterButtonType2.onClick.AddListener(UseShuffle);
        if (BoosterButtonType3 != null) BoosterButtonType3.onClick.AddListener(UseConveyor);
        if (_buttonCancelBooster != null) _buttonCancelBooster.onClick.AddListener(CancelBooster);

        SetSwapBoosterVisible(false);

        RefreshBoosterLockState();
        RefreshBoosterQuantity();
        RefreshGroupBoosterUI(force: true);
    }

    private void Update()
    {
        RefreshGroupBoosterUI();
    }

    private void OnDisable()
    {
        _fillTween?.Kill();
        _fillTween = null;

        _slideTween?.Kill();
        _slideTween = null;
    }

    public void PlayTutorialDropToBoosterButton(int boosterType, int count)
    {
        PlayTutorialDropToBoosterButton(boosterType, count, null);
    }

    public void PlayTutorialDropToBoosterButton(int boosterType, int count, Action onComplete)
    {
        if (count <= 0) return;

        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        var canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null) return;

        var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        RectTransform targetBtn = ResolveBoosterButtonRect(boosterType);
        if (targetBtn == null) return;

        Sprite sprite = ResolveBoosterSprite(boosterType);
        if (sprite == null) return;

        RectTransform spawnParentRect = _spawnPointBooster != null ? _spawnPointBooster : canvasRect;
        Vector2 startLocal;
        if (_spawnPointBooster != null)
        {
            startLocal = Vector2.zero;
        }
        else
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    spawnParentRect,
                    new Vector2(Screen.width * 0.5f, Screen.height * 0.5f),
                    cam,
                    out startLocal))
            {
                return;
            }
        }

        int spawnCount = Mathf.Clamp(count, 1, 30);
        int completed = 0;
        void HandleOneComplete()
        {
            completed++;
            if (completed >= spawnCount)
            {
                onComplete?.Invoke();
            }
        }

        for (int i = 0; i < spawnCount; i++)
        {
            float offsetX = UnityEngine.Random.Range(-_tutorialDropScatterX, _tutorialDropScatterX);
            float offsetY = UnityEngine.Random.Range(-_tutorialDropScatterX * 0.2f, _tutorialDropScatterX * 0.2f);
            Vector2 spawnLocal = startLocal + new Vector2(offsetX, offsetY);
            float delay = i * 0.03f;
            SpawnAndDrop(spawnParentRect, cam, sprite, spawnLocal, targetBtn, delay, HandleOneComplete);
        }
    }

    private RectTransform ResolveBoosterButtonRect(int boosterType)
    {
        Button btn = boosterType switch
        {
            1 => BoosterButtonType1,
            2 => BoosterButtonType2,
            3 => BoosterButtonType3,
            _ => null
        };

        return btn != null ? btn.GetComponent<RectTransform>() : null;
    }

    private Sprite ResolveBoosterSprite(int boosterType)
    {
        GameObject iconRoot = boosterType switch
        {
            1 => iconType1,
            2 => iconType2,
            3 => iconType3,
            _ => null
        };

        if (iconRoot == null) return null;
        var img = iconRoot.GetComponentInChildren<Image>(includeInactive: true);
        if (img == null) return null;
        return img.sprite;
    }

    private void SpawnAndDrop(RectTransform spawnParentRect, Camera cam, Sprite sprite, Vector2 startLocal, RectTransform target, float delay, Action onComplete)
    {
        Vector2 targetLocal;
        Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(cam, target.position);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(spawnParentRect, targetScreen, cam, out targetLocal)) return;

        var go = new GameObject("TutorialDropIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(spawnParentRect, worldPositionStays: false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = startLocal;
        rt.localScale = Vector3.one * _tutorialDropStartScale;

        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.SetNativeSize();

        DOTween.Kill(rt, complete: false);
        DOTween.Kill(img, complete: false);

        Sequence seq = DOTween.Sequence();
        seq.SetTarget(go);
        if (delay > 0f) seq.AppendInterval(delay);
        seq.Join(rt.DOAnchorPos(targetLocal, _tutorialDropDuration).SetEase(Ease.InQuad));
        seq.Join(rt.DOScale(_tutorialDropEndScale, _tutorialDropDuration).SetEase(Ease.OutCubic));
        seq.Append(rt.DOScale(_tutorialDropEndScale * 1.08f, 0.12f).SetEase(Ease.OutBack));
        seq.Append(rt.DOScale(_tutorialDropEndScale, 0.08f).SetEase(Ease.InQuad));
        seq.OnComplete(() =>
        {
            if (go != null) Destroy(go);

            onComplete?.Invoke();
        });
    }

    public override void Show()
    {
        CacheHolderPos();
        _slideTween?.Kill();
        if (_holderRect != null)
        {
            _holderRect.anchoredPosition = _holderShownPos + new Vector2(0f, -_slideOffsetY);
        }
        base.Show();

        if (_holderRect != null)
        {
            _slideTween = _holderRect.DOAnchorPos(_holderShownPos, _slideDuration).SetEase(Ease.OutCubic);
        }
        RefreshBoosterLockState();
        RefreshBoosterQuantity();
        RefreshGroupBoosterUI(force: true);
    }

    public override void Hide()
    {
        CacheHolderPos();
        _slideTween?.Kill();
        if (_holderRect == null)
        {
            base.Hide();
            return;
        }

        GameUI.Instance.Unsubmit(this);
        _slideTween = _holderRect.DOAnchorPos(_holderShownPos + new Vector2(0f, -_slideOffsetY), _slideDuration)
            .SetEase(Ease.InCubic)
            .OnComplete(() => base.Hide());
    }

    private void CacheHolderPos()
    {
        if (_holderPosCached) return;
        if (holder == null) return;
        _holderRect = holder.GetComponent<RectTransform>();
        if (_holderRect == null) return;
        _holderShownPos = _holderRect.anchoredPosition;
        _holderPosCached = true;
    }

    public void UseHammer() => TryUseBooster(BoosterType.Hammer, inventoryBoosterType: 1);
    public void UseShuffle() => TryUseBooster(BoosterType.Shuffle, inventoryBoosterType: 2);
    public void UseConveyor() => TryUseBooster(BoosterType.Conveyor, inventoryBoosterType: 3);

    private void SetSwapBoosterVisible(bool visible)
    {
        if (BoosterButtonType4 != null) BoosterButtonType4.gameObject.SetActive(visible);
        if (BoosterTextType4 != null) BoosterTextType4.gameObject.SetActive(visible);
        if (iconType4 != null) iconType4.SetActive(visible);
    }
    [Button]
    public void CancelBooster()
    {
        switch (Board.Instance.CurrentBooster)
        {
            case BoosterType.Hammer:
                Board.Instance.ResetBooster();
                InventoryManager.Instance.AddBoosterType1(1);
                break;

            case BoosterType.Conveyor:
                Board.Instance.ResetBooster();
                ConveyorController.Instance.BringToTop = false;
                ConveyorController.Instance.SetAllCubesBringToTop(false);
                ConveyorController.Instance.ResumeConveyor();
                InventoryManager.Instance.AddBoosterType3(1);
                break;

            case BoosterType.Rainbow:
                break;

            case BoosterType.Shuffle:
                Board.Instance.ResetBooster();
                break;
        }

        Board.Instance.CurrentBooster = BoosterType.None;

        RefreshBoosterQuantity();
        RefreshGroupBoosterUI(force: true);
    }
    private void TryUseBooster(BoosterType booster, int inventoryBoosterType)
    {
        if (!IsBoosterUnlocked(inventoryBoosterType))
        {
            ShowLockedToast(inventoryBoosterType);
            return;
        }

        if (InventoryManager.Instance == null)
        {
            return;
        }

        var board = Board.Instance;
        if (board == null)
        {
            return;
        }

        if (board.CurrentBooster != BoosterType.None)
        {
            return;
        }

        if (!board.CanActivateBooster(booster))
        {
            ShowCannotUseBoosterToast(booster);
            return;
        }

        if (booster == BoosterType.Shuffle)
        {
            int count = InventoryManager.Instance.GetBoosterType2();
            if (count <= 0)
            {
                GameUI.Instance.Get<UIBuyBooster>().ShowForBooster(inventoryBoosterType);
                return;
            }

            board.UseShuffle();
            RefreshBoosterQuantity();
            RefreshGroupBoosterUI(force: true);
            return;
        }

        bool used = inventoryBoosterType switch
        {
            1 => InventoryManager.Instance.UseBoosterType1(),
            2 => InventoryManager.Instance.UseBoosterType2(),
            3 => InventoryManager.Instance.UseBoosterType3(),
            4 => InventoryManager.Instance.UseBoosterType4(),
            _ => false
        };

        if (!used)
        {
            GameUI.Instance.Get<UIBuyBooster>().ShowForBooster(inventoryBoosterType);
            return;
        }

        switch (booster)
        {
            case BoosterType.Hammer:
                board.UseHammer();
                break;
            case BoosterType.Conveyor:
                board.UseConveyor();
                break;
            case BoosterType.Rainbow:
                board.UseRainbow();
                break;
            case BoosterType.Shuffle:
                board.UseShuffle();
                break;
        }

        RefreshBoosterQuantity();
        RefreshGroupBoosterUI(force: true);
    }

    private void ShowCannotUseBoosterToast(BoosterType booster)
    {
        string msg = booster switch
        {
            BoosterType.Hammer => "No blocks to destroy",
            BoosterType.Conveyor => "No blocks on conveyor",
            BoosterType.Rainbow => "No shooters available",
            BoosterType.Shuffle => "No shooters to shuffle",
            _ => "Can't use booster right now"
        };

        if (GameUI.Instance != null)
        {
            var toast = GameUI.Instance.Get<UINotification>();
            if (toast != null)
            {
                toast.ShowToast(msg);
                return;
            }
        }

        Debug.Log(msg);
    }

    private bool IsBoosterUnlocked(int boosterType)
    {
        int level = GameManagerInGame.Instance != null ? GameManagerInGame.Instance.CurrentLevel : 1;
        return BoosterUnlockService.IsUnlocked(boosterType, level);
    }

    private void ShowLockedToast(int boosterType)
    {
        string msg = BoosterUnlockService.GetLockedToast(boosterType);
        if (GameUI.Instance != null)
        {
            var toast = GameUI.Instance.Get<UINotification>();
            if (toast != null)
            {
                toast.ShowToast(msg);
                return;
            }
        }

        Debug.Log(msg);
    }

    public void RefreshBoosterLockState()
    {
        bool unlocked1 = IsBoosterUnlocked(1);
        bool unlocked2 = IsBoosterUnlocked(2);
        bool unlocked3 = IsBoosterUnlocked(3);

        ApplyBoosterLockState(unlocked1, BoosterButtonType1, bgType1, iconType1Locked, iconType1, iconType1Add, BoosterTextType1);
        ApplyBoosterLockState(unlocked2, BoosterButtonType2, bgType2, iconType2Locked, iconType2, iconType2Add, BoosterTextType2);
        ApplyBoosterLockState(unlocked3, BoosterButtonType3, bgType3, iconType3Locked, iconType3, iconType3Add, BoosterTextType3);
    }

    private void ApplyBoosterLockState(
        bool unlocked,
        Button button,
        Image background,
        GameObject lockedIcon,
        GameObject ownedIcon,
        GameObject addIcon,
        TextMeshProUGUI quantityText)
    {
        // Keep the button clickable so we can show the "unlock at level" toast when locked.
        if (button != null) button.interactable = true;

        if (background != null)
        {
            background.sprite = unlocked ? bgUnlockedSprite : bgLockedSprite;
        }

        if (lockedIcon != null) lockedIcon.SetActive(!unlocked);

        if (!unlocked)
        {
            if (ownedIcon != null) ownedIcon.SetActive(false);
            if (addIcon != null) addIcon.SetActive(false);
            if (quantityText != null) quantityText.gameObject.SetActive(false);
        }
    }

    public void RefreshBoosterQuantity()
    {
        if (InventoryManager.Instance == null) return;

        bool unlocked1 = IsBoosterUnlocked(1);
        bool unlocked2 = IsBoosterUnlocked(2);
        bool unlocked3 = IsBoosterUnlocked(3);

        int b1 = InventoryManager.Instance.GetBoosterType1();
        int b2 = InventoryManager.Instance.GetBoosterType2();
        int b3 = InventoryManager.Instance.GetBoosterType3();

        SetBoosterVisual(unlocked1, b1, iconType1Locked, iconType1, iconType1Add, BoosterTextType1);
        SetBoosterVisual(unlocked2, b2, iconType2Locked, iconType2, iconType2Add, BoosterTextType2);
        SetBoosterVisual(unlocked3, b3, iconType3Locked, iconType3, iconType3Add, BoosterTextType3);
    }

    public void RefreshBoosterUIImmediate()
    {
        RefreshBoosterQuantity();
        RefreshGroupBoosterUI(force: true);
    }

    private static void SetBoosterVisual(
        bool unlocked,
        int count,
        GameObject lockedIcon,
        GameObject ownedIcon,
        GameObject addIcon,
        TextMeshProUGUI quantityText)
    {
        if (!unlocked)
        {
            if (lockedIcon != null) lockedIcon.SetActive(true);
            if (ownedIcon != null) ownedIcon.SetActive(false);
            if (addIcon != null) addIcon.SetActive(false);
            if (quantityText != null) quantityText.gameObject.SetActive(false);
            return;
        }

        if (lockedIcon != null) lockedIcon.SetActive(false);

        if (quantityText != null) quantityText.text = "x" + Mathf.Max(0, count);

        bool hasAny = count > 0;
        if (ownedIcon != null) ownedIcon.SetActive(true);
        if (addIcon != null) addIcon.SetActive(!hasAny);
        if (quantityText != null) quantityText.gameObject.SetActive(hasAny);
    }

    public void SetConveyorPercent(float percent)
    {
        percent = Mathf.Clamp(percent, 0f, 100f);

        if (_percentLevel != null) _percentLevel.text = Mathf.RoundToInt(percent) + "%";

        if (fillLevel != null)
        {
            float targetFill = percent / 100f;

            _fillTween?.Kill();
            if (_fillSmoothTime <= 0.0001f)
            {
                fillLevel.fillAmount = targetFill;
                fillLevel.color = GetFillColor(percent);
                return;
            }

            _fillTween = DOTween.To(
                    () => fillLevel.fillAmount,
                    v =>
                    {
                        fillLevel.fillAmount = v;
                        fillLevel.color = GetFillColor(v * 100f);
                    },
                    targetFill,
                    _fillSmoothTime
                )
                .SetEase(Ease.OutCubic);
        }
    }

    private Color GetFillColor(float percent)
    {
        if (percent <= 25f) return Color.Lerp(_fillColor0To25, _fillColor25To50, percent / 25f);
        if (percent <= 50f) return Color.Lerp(_fillColor25To50, _fillColor50To75, (percent - 25f) / 25f);
        if (percent <= 75f) return Color.Lerp(_fillColor50To75, _fillColor75To100, (percent - 50f) / 25f);
        return _fillColor75To100;
    }

    private void RefreshGroupBoosterUI(bool force = false)
    {
        BoosterType current = Board.Instance != null ? Board.Instance.CurrentBooster : BoosterType.None;
        if (!force && current == _lastBooster) return;
        _lastBooster = current;

        if (_groupBooster == null) return;

        bool show = current == BoosterType.Hammer || current == BoosterType.Conveyor || current == BoosterType.Rainbow || current == BoosterType.Shuffle;
        _groupBooster.SetActive(show);
        if (!show) return;

        if (_imageBooster != null)
        {
            if (current == BoosterType.Hammer) _imageBooster.sprite = _spriteBoosterHammer;
            else if (current == BoosterType.Conveyor) _imageBooster.sprite = _spriteBoosterConveyor;
            else if (current == BoosterType.Shuffle) _imageBooster.sprite = _spriteBoosterShuffle;
        }

        if (_textBooster != null)
        {
            if (current == BoosterType.Hammer) _textBooster.text = _textBoosterHammer;
            else if (current == BoosterType.Conveyor) _textBooster.text = _textBoosterConveyor;
            else if (current == BoosterType.Rainbow || current == BoosterType.Shuffle) _textBooster.text = _textBoosterShuffle;
        }

        if (_textBoosterDesp != null)
        {
            if (current == BoosterType.Hammer) _textBoosterDesp.text = _textDespBoosterHammer;
            else if (current == BoosterType.Conveyor) _textBoosterDesp.text = _textDespBoosterConveyor;
            else if (current == BoosterType.Rainbow || current == BoosterType.Shuffle) _textBoosterDesp.text = _textDespBoosterShuffle;
        }
    }

}
