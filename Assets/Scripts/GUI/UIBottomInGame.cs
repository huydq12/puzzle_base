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

    public Button BoosterBackButton;
    public Button BoosterAddButton;
    public Button BoosterShufferButton;
    public Button BoosterSwapContainerButton;

    public TextMeshProUGUI BoosterBackText;
    public TextMeshProUGUI BoosterAddText;
    public TextMeshProUGUI BoosterShufferText;
    public TextMeshProUGUI BoosterSwapContainerText;

    public GameObject iconBack;
    public GameObject iconAdd;
    public GameObject iconShuffle;
    public GameObject iconSwap;

    private void Start()
    {
        if (BoosterBackButton != null) BoosterBackButton.onClick.AddListener(OnBoosterBackClicked);
        if (BoosterAddButton != null) BoosterAddButton.onClick.AddListener(OnBoosterAddClicked);
        if (BoosterShufferButton != null) BoosterShufferButton.onClick.AddListener(OnBoosterShuffleClicked);
        if (BoosterSwapContainerButton != null) BoosterSwapContainerButton.onClick.AddListener(OnBoosterSwapContainerClicked);

        RefreshBoosterQuantity();
    }

    public override void Show()
    {
        base.Show();
        RefreshBoosterQuantity();
    }

    public void OnBoosterBackClicked()
    {
        TryUseBooster(1, () => GameController.Instance.UndoLastMove());
    }

    public void OnBoosterAddClicked()
    {
        TryUseBooster(2, () => GameController.Instance.AddSlotButton());
    }

    public void OnBoosterShuffleClicked()
    {
        TryUseBooster(3, () => GameController.Instance.ShuffleEqualStacks());
    }

    public void OnBoosterSwapContainerClicked()
    {
        TryUseBooster(4, () => GameController.Instance.SwapCurrentAndNextContainers());
    }

    private void TryUseBooster(int boosterType, Action onUsed)
    {
        if (GameController.Instance == null || InventoryManager.Instance == null)
        {
            return;
        }

        if (GameController.Instance.IsAnimating)
        {
            return;
        }

        bool used = boosterType switch
        {
            1 => InventoryManager.Instance.UseBoosterType1(),
            2 => InventoryManager.Instance.UseBoosterType2(),
            3 => InventoryManager.Instance.UseBoosterType3(),
            4 => InventoryManager.Instance.UseBoosterType4(),
            _ => false
        };

        if (!used)
        {
            GameUI.Instance.Get<UIBuyBooster>().ShowForBooster(boosterType);
            return;
        }

        onUsed?.Invoke();
        RefreshBoosterQuantity();
    }

    public void RefreshBoosterQuantity()
    {
        if (InventoryManager.Instance == null) return;

        int b1 = InventoryManager.Instance.GetBoosterType1();
        int b2 = InventoryManager.Instance.GetBoosterType2();
        int b3 = InventoryManager.Instance.GetBoosterType3();
        int b4 = InventoryManager.Instance.GetBoosterType4();

        if (BoosterBackText != null) BoosterBackText.text = "x"+b1.ToString();
        if (BoosterAddText != null) BoosterAddText.text = "x"+b2.ToString();
        if (BoosterShufferText != null) BoosterShufferText.text = "x"+b3.ToString();
        if (BoosterSwapContainerText != null) BoosterSwapContainerText.text = "x"+b4.ToString();

        if (iconBack != null) {
            if (b1 <= 0) {
                iconBack.SetActive(true);
                BoosterBackText.gameObject.SetActive(false);
            } else {
                iconBack.SetActive(false);
                BoosterBackText.gameObject.SetActive(true);
            }
        };
        if (iconAdd != null) {
            if (b2 <= 0) {
                iconAdd.SetActive(true);
                BoosterAddText.gameObject.SetActive(false);
            } else {
                iconAdd.SetActive(false);
                BoosterAddText.gameObject.SetActive(true);
            }
        };
        if (iconShuffle != null)  {
            if (b3 <= 0) {
                iconShuffle.SetActive(true);
                BoosterShufferText.gameObject.SetActive(false);
            } else {
                iconShuffle.SetActive(false);
                BoosterShufferText.gameObject.SetActive(true);
            }
        };

        if (iconSwap != null)  {
            if (b4 <= 0) {
                iconSwap.SetActive(true);
                BoosterSwapContainerText.gameObject.SetActive(false);
            } else {
                iconSwap.SetActive(false);
                BoosterSwapContainerText.gameObject.SetActive(true);
            }
        };
    }


}
