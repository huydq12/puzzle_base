using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UITopInGame : UIElement
{
    public override bool ManualHide => false;
    public override bool DestroyOnHide => false;
    public override bool UseBehindPanel => false;
    
    [SerializeField] private TextMeshProUGUI txt_level;
    [SerializeField] private Button buttonSetting;
    [SerializeField] private Button buttonReplay;

    [SerializeField] private RectTransform reactLevelNormal;
    [SerializeField] private RectTransform reactLevelHard;

    [SerializeField] private CanvasGroup trayNotificationGroup;
    [SerializeField] private TextMeshProUGUI textDeps;

    [SerializeField] private RectTransform infoSelectButon;
   
    
    private void Start()
    {
        buttonSetting.onClick.AddListener(() =>
        {
            GameUI.Instance.Get<UISettingInGame>().Show();
        });
        
        buttonReplay.onClick.AddListener(() =>
        {
            if (GameManagerInGame.Instance != null)
            {
                GameManagerInGame.Instance.StartGame();
            }
        });
    }

    public void ShowInfoButton(string message)
    {
        if (infoSelectButon == null) return;
        if (message == null || message.Length == 0){
            infoSelectButon.gameObject.SetActive(false);
            return;
        }
        infoSelectButon.gameObject.SetActive(true);
    }

    public override void Show()
    {
        base.Show();

        int level = GameManagerInGame.Instance != null ? GameManagerInGame.Instance.CurrentLevel : 1;
        txt_level.text = "Level " + level.ToString();

        bool isHard = level % 10 == 0;
        if (reactLevelNormal != null) reactLevelNormal.gameObject.SetActive(!isHard);
        if (reactLevelHard != null) reactLevelHard.gameObject.SetActive(isHard);

        RectTransform target = isHard ? reactLevelHard : reactLevelNormal;
        if (target != null)
        {
            DOTween.Kill(target, false);
            target.localScale = Vector3.one * (isHard ? 0.75f : 0.85f);
            target.localRotation = Quaternion.identity;
            target.anchoredPosition3D = target.anchoredPosition3D;

            Sequence seq = DOTween.Sequence().SetTarget(target);
            seq.Append(target.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack));
            if (isHard)
            {
                seq.Join(target.DOPunchRotation(new Vector3(0f, 0f, 14f), 0.35f, 10, 0.8f));
                seq.Join(target.DOShakeAnchorPos(0.35f, new Vector2(10f, 0f), 12, 90f, false, true));
                seq.Append(target.DOScale(1.06f, 0.12f).SetEase(Ease.OutQuad));
                seq.Append(target.DOScale(1f, 0.12f).SetEase(Ease.InOutQuad));
            }
        }
    }

    public void ShowTrayNotificationLose(float autoHideDelay = 1.2f,string message = "")
    {
        if (trayNotificationGroup == null) return;
        
        if (textDeps != null) {
            textDeps.text = message;
        }

        var t = trayNotificationGroup.transform as RectTransform;

        trayNotificationGroup.gameObject.SetActive(true);
        trayNotificationGroup.alpha = 0f;
        trayNotificationGroup.interactable = false;
        trayNotificationGroup.blocksRaycasts = false;

        DOTween.Kill(trayNotificationGroup, false);
        if (t != null)
        {
            DOTween.Kill(t, false);
            t.localScale = Vector3.one * 0.9f;
            t.localRotation = Quaternion.identity;
        }

        Sequence seq = DOTween.Sequence().SetTarget(trayNotificationGroup);
        seq.Join(trayNotificationGroup.DOFade(1f, 0.18f).SetEase(Ease.OutQuad));
        if (t != null)
        {
            seq.Join(t.DOScale(1f, 0.18f).SetEase(Ease.OutBack).SetTarget(t));
            seq.Append(t.DORotate(new Vector3(0f, 0f, -8f), 0.08f).SetEase(Ease.OutQuad).SetTarget(t));
            seq.Append(t.DORotate(new Vector3(0f, 0f, 8f), 0.12f).SetEase(Ease.InOutQuad).SetTarget(t));
            seq.Append(t.DORotate(Vector3.zero, 0.1f).SetEase(Ease.OutQuad).SetTarget(t));
        }

        if (autoHideDelay > 0f)
        {
            seq.AppendInterval(autoHideDelay);
            seq.Append(trayNotificationGroup.DOFade(0f, 0.2f).SetEase(Ease.InQuad));
            seq.OnComplete(() =>
            {
                if (trayNotificationGroup != null)
                {
                    trayNotificationGroup.gameObject.SetActive(false);
                }
            });
        }
    }
}
