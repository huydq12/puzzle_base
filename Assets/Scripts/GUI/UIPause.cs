using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPause : BasePopup
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
        UIManager.Instance.HideUI<UIPause>();
    }

    private void UIHomne()
    {
        UIManager.Instance.HideAll();
        UIManager.Instance.ShowUI<UIHome>();
    }

    private void UILevel()
    {
        UIManager.Instance.HideAll();
        UIManager.Instance.ShowUI<UILevel>();
    }

}
