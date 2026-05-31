using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UILevel : BaseScreen
{
    public override bool ManualHide => true;
    public override bool DestroyOnHide => false;
    public override bool UseBehindPanel => false;

    [SerializeField] private Button btn_back;

    private void Start()
    {
        btn_back.onClick.AddListener(BackHome);
    }

    private void BackHome()
    {
        UIManager.Instance.HideUI<UILevel>();
        UIManager.Instance.ShowUI<UIHome>();
    }

}
