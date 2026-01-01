using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
public class UILose : UIPopup
{
    [SerializeField] private Button btn_next;
    [SerializeField] private Button btn_close_hide;
    [SerializeField] private TextMeshProUGUI txt_coin;
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
    }

    private void NextGame()
    {
        DOTween.KillAll();
        GameManagerInGame.Instance.StartGame();
        Hide();
    }

}
