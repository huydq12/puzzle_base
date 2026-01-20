using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    public Image fillLevel;
    public TextMeshProUGUI _percentLevel;

    [Header("Conveyor Level Colors")]
    [SerializeField] private Color _fillColor0To25 = Color.green;
    [SerializeField] private Color _fillColor25To50 = Color.yellow;
    [SerializeField] private Color _fillColor50To75 = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color _fillColor75To100 = Color.red;

    private void Start()
    {
        // Only 3 boosters are supported in this mode: Hammer / Conveyor / Rainbow.
        // We reuse the first 3 button slots and hide the 4th one (Swap).
        if (BoosterButtonType1 != null) BoosterButtonType1.onClick.AddListener(UseHammer);
        if (BoosterButtonType2 != null) BoosterButtonType2.onClick.AddListener(UseConveyor);
        if (BoosterButtonType3 != null) BoosterButtonType3.onClick.AddListener(UseRainbow);

        SetSwapBoosterVisible(false);

        RefreshBoosterQuantity();
    }

    public override void Show()
    {
        base.Show();
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

    private void TryUseBooster(BoosterType booster, int inventoryBoosterType)
    {
        if (InventoryManager.Instance == null)
        {
            return;
        }

        var board = FindFirstObjectByType<Board>();
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

    public void RefreshBoosterQuantity()
    {
        if (InventoryManager.Instance == null) return;

        int b1 = InventoryManager.Instance.GetBoosterType1();
        int b2 = InventoryManager.Instance.GetBoosterType2();
        int b3 = InventoryManager.Instance.GetBoosterType3();

        if (BoosterTextType1 != null) BoosterTextType1.text = "x"+b1.ToString();
        if (BoosterTextType2 != null) BoosterTextType2.text = "x"+b2.ToString();
        if (BoosterTextType3 != null) BoosterTextType3.text = "x"+b3.ToString();

        if (iconType1 != null) {
            if (b1 <= 0) {
                iconType1.SetActive(true);
                BoosterTextType1.gameObject.SetActive(false);
            } else {
                iconType1.SetActive(false);
                BoosterTextType1.gameObject.SetActive(true);
            }
        };
        if (iconType2 != null) {
            if (b2 <= 0) {
                iconType2.SetActive(true);
                BoosterTextType2.gameObject.SetActive(false);
            } else {
                iconType2.SetActive(false);
                BoosterTextType2.gameObject.SetActive(true);
            }
        };
        if (iconType3 != null)  {
            if (b3 <= 0) {
                iconType3.SetActive(true);
                BoosterTextType3.gameObject.SetActive(false);
            } else {
                iconType3.SetActive(false);
                BoosterTextType3.gameObject.SetActive(true);
            }
        };
    }

    public void SetConveyorPercent(float percent)
    {
        percent = Mathf.Clamp(percent, 0f, 100f);

        if (_percentLevel != null) _percentLevel.text = Mathf.RoundToInt(percent) + "%";

        if (fillLevel != null)
        {
            fillLevel.fillAmount = percent / 100f;
            fillLevel.color = GetFillColor(percent);
        }
    }

    private Color GetFillColor(float percent)
    {
        if (percent < 25f) return _fillColor0To25;
        if (percent < 50f) return _fillColor25To50;
        if (percent < 75f) return _fillColor50To75;
        return _fillColor75To100;
    }

}
