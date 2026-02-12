using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System.Collections.Generic;
public class UIWin : UIPopup
{
    [SerializeField] private Button btn_next;
    [SerializeField] private Button btn_close_hide;
    [SerializeField] private TextMeshProUGUI txt_coin;
    public Button Next;
    public override void Show()
    {
        base.Show();
        VibrateManager.Instance.MediumVibrate();
        AudioManager.Instance.PlaySFX(SFXType.Lose);

        int fromAmount = GameManagerInGame.Instance.userData.playerCash;
        if (txt_coin != null)
        {
            txt_coin.text = fromAmount.ToString();
        }
    }
    protected override void Start()
    {
        base.Start();
        btn_next.onClick.AddListener(NextGame);
        btn_close_hide.onClick.AddListener(NextGame);
        Next.onClick.AddListener(() => AudioManager.Instance.PlaySFX(SFXType.Click));
    }

    private void NextGame()
    {
        DOTween.KillAll();
        GameManagerInGame.Instance.StartNextLevel();
        Hide();
    }
}
