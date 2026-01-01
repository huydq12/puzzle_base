using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIChangeName : UIPopup
{
    [Header("Button")]
    [SerializeField] private Button btn_close;
    [SerializeField] private Button btn_confirm;

    [Header("Input")]
    [SerializeField] private TMP_InputField input_name;
    [SerializeField] private TextMeshProUGUI text_name;

    protected override void Start()
    {
        base.Start();
        btn_close.onClick.AddListener(Close);
        btn_confirm.onClick.AddListener(Confirm);
    }

    public override void Show()
    {
        base.Show();
        text_name.text = GameManager.Instance.userData.playerName;
    }

    private void Close()
    {
        Hide();
    }

    private void Confirm()
    {
        GameManager.Instance.userData.playerName = input_name.text;
        GameUI.Instance.Get<UIProfile>().UpdatePLayerName();
        GameManager.Instance.userData.Save();
        Hide();
        
    }
}
