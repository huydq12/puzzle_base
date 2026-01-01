using System;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class UILoading : UIElement
{
    public override bool ManualHide => true;
    public override bool DestroyOnHide => true;
    public override bool UseBehindPanel => false;
    private float elapsed;
    private bool isLoading;
    private Action onLoaded;
    private AnimationCurve curve;
    [SerializeField] private float sizeX;
    [SerializeField] private float minPosX;
    [SerializeField] private float duration;
    [SerializeField] private Image fillBar;
    [SerializeField] private RectTransform knob;
    [SerializeField] private AnimationCurve[] curves;

    public void Load(Action loading, Action loaded)
    {
        curve = curves[Random.Range(0, curves.Length)];
        onLoaded = loaded;
        isLoading = true;
        elapsed = 0;
        base.Show();
        loading?.Invoke();
    }

    private void Update()
    {
        if (!isLoading) return;
        elapsed += Time.deltaTime;
        float percent = elapsed / duration;
        fillBar.fillAmount = curve.Evaluate(Mathf.Clamp01(percent));
        float x = Mathf.Clamp(fillBar.fillAmount * sizeX, minPosX, sizeX);
        knob.anchoredPosition = new Vector2(x, knob.anchoredPosition.y);
        if (x != sizeX) return;
        isLoading = false;
        onLoaded?.Invoke();
        //Monetize.Instance.myFirebase.SendSeasionStart(StatusADS.succeed);
        //Monetize.Instance.adjustManager.SendSeasionStart(StatusADS.succeed);

        ////Show buy premium on 2nd open app

        //if (GameManager.Instance.userData.GetValidTimeShowIncome() >= 30)
        //{
        //    int money = GameManager.Instance.userData.GetInCome();
        //    GameManager.Instance.PlayerCash = money;
            
        //    GameUI.Instance.Get<UIComebackRewards>().Show();
        //    //GameUI.Instance.Get<UIGamePlay>().ResetCountDiamond();
        //}
        ////Debug.Log(GameManager.Instance.userData.GetValidTimeShow() + "time");
        //if(GameManager.Instance.userData.GetValidTimeShow() >= 1440)
        //{
        //    //Debug.Log("Okireset");
        //    GameUI.Instance.Get<UIGamePlay>().ResetCountDiamond();
        //}

        //else
        //{
        //    if (GameManager.Instance.userData.firstOpenAppShop && !GameManager.Instance.userData.premiumPackBought)
        //    {
        //        GameUI.Instance.Get<UIShopPack>().Show();
        //    }
        //}
        //GameManager.Instance.canResetTimeShowRewardGetOfflineMoney = true;
        //GameManager.Instance.userData.firstOpenAppShop = true;
    }
}