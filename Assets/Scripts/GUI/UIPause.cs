using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPause : UIElement
{
    public override bool ManualHide => true;
    public override bool DestroyOnHide => false;
    public override bool UseBehindPanel => false;

    [SerializeField] private Button btn_close;
    [SerializeField] private Button btn_home;
    [SerializeField] private Button btn_level;

    private void Start()
    {
        btn_close.onClick.AddListener(Close);
        btn_home.onClick.AddListener(UIHomne);
        btn_level.onClick.AddListener(UILevel);
    }

    private void Close()
    {
        Hide();
    }

    private void UIHomne()
    {
        Hide();
        GameUI.Instance.HideAll();
        GameUI.Instance.Get<UIHome>().Show();
    }

    private void UILevel()
    {
        Hide();
        GameUI.Instance.HideAll();
        GameUI.Instance.Get<UILevel>().Show();
    }

}
