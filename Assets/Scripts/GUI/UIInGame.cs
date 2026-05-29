using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Utils.Pattern;
using Config;


public class UIInGame : BaseScreen
{
    public override bool ManualHide => true;
    public override bool DestroyOnHide => false;
    public override bool UseBehindPanel => false;


    [SerializeField] private Button btn_pause;

    private void Start()
    {
        btn_pause.onClick.AddListener(UIPause);
    }

    private void UIPause()
    {
        UIManager.Instance.Get<UIPause>().Show();
    }

}