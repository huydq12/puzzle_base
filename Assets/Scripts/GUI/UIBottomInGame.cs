using System.Collections;
using System.Collections.Generic;
using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

public class UIBottomInGame : UIElement
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

    public GameObject iconType1Add;
    public GameObject iconType2Add;
    public GameObject iconType3Add;

    [Header("Lock State")]
    [SerializeField] private Image bgType1;
    [SerializeField] private Image bgType2;
    [SerializeField] private Image bgType3;

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

    private void Start()
    {
        // Only 3 boosters are supported in this mode: Hammer / Conveyor / Rainbow.
        // We reuse the first 3 button slots and hide the 4th one (Swap).
        if (BoosterButtonType1 != null) BoosterButtonType1.onClick.AddListener(UseHammer);
        if (BoosterButtonType2 != null) BoosterButtonType2.onClick.AddListener(UseConveyor);
        if (BoosterButtonType3 != null) BoosterButtonType3.onClick.AddListener(UseRainbow);

        SetSwapBoosterVisible(false);

        RefreshBoosterLockState();
        RefreshBoosterQuantity();
    }

    private void OnDisable()
    {
        _fillTween?.Kill();
        _fillTween = null;
    }

    public override void Show()
    {
        base.Show();
        RefreshBoosterLockState();
        RefreshBoosterQuantity();
    }

    public void UseHammer() => TryUseBooster(BoosterType.Hammer, inventoryBoosterType: 1);
    public void UseConveyor() => TryUseBooster(BoosterType.Conveyor, inventoryBoosterType: 2);
    public void UseRainbow() => TryUseBooster(BoosterType.Rainbow, inventoryBoosterType: 3);

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
                InventoryManager.Instance.AddBoosterType2(1);
                break;

            case BoosterType.Rainbow:
                break;
        }

        Board.Instance.CurrentBooster = BoosterType.None;

        RefreshBoosterQuantity();
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

        bool used = inventoryBoosterType switch
        {
            1 => InventoryManager.Instance.UseBoosterType1(),
            2 => InventoryManager.Instance.UseBoosterType2(),
            3 => InventoryManager.Instance.UseBoosterType3(),
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
        }

        RefreshBoosterQuantity();
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

}
