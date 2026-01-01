using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class UIShop : UIElement
{
    public override bool ManualHide => true;
    public override bool DestroyOnHide => false;
    public override bool UseBehindPanel => false;

    [SerializeField] private TextMeshProUGUI txt_playerCash;

    public override void Show()
    {
        base.Show();
        UpdateCash(GameManager.Instance.userData.playerCash);
    }

    public void UpdateCash(int cash)
    {
        txt_playerCash.text = cash.ToString();
        Debug.Log("Shop cash updated: " + cash);
    }

}
