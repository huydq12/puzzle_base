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
    [SerializeField] private ButtonInfoUI buttonInfoUI;

    [SerializeField] private List<CellClearedVfxByColor> _cellClearedVfxByColor;

    private readonly List<ButtonInfoUI> _activeInfoItems = new List<ButtonInfoUI>();
    
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
        UpdateUIButton(message);
    }

    public void UpdateUIButton(string message)
    {
        if (infoSelectButon == null) return;
        if (buttonInfoUI == null) return;

        buttonInfoUI.gameObject.SetActive(false);

        int baseSiblingIndex = 0;
        if (buttonInfoUI.transform.parent == infoSelectButon)
        {
            buttonInfoUI.transform.SetAsFirstSibling();
            baseSiblingIndex = 1;
        }

        for (int i = 0; i < _activeInfoItems.Count; i++)
        {
            var it = _activeInfoItems[i];
            if (it == null) continue;
            if (it.transform.parent == infoSelectButon) it.transform.SetParent(null, false);
            if (it.gameObject.activeSelf) it.gameObject.SetActive(false);
        }
        _activeInfoItems.Clear();

        if (string.IsNullOrEmpty(message)) return;

        var parts = message.Split(',');
        for (int i = 0; i < parts.Length; i++)
        {
            var p = parts[i];
            if (string.IsNullOrWhiteSpace(p)) continue;

            var raw = p.Trim();
            var seg = raw.Split(':');
            var colorToken = seg.Length > 0 ? seg[0].Trim() : string.Empty;
            var countToken = seg.Length > 1 ? seg[1].Trim() : string.Empty;

            if (string.IsNullOrEmpty(colorToken)) continue;

            if (!Enum.TryParse<ObjectColor>(colorToken, true, out var objColor))
            {
                continue;
            }

            int count = 0;
            int.TryParse(countToken, out count);

            var item = PoolManager.Instance != null ? PoolManager.Instance.Get(buttonInfoUI) : Instantiate(buttonInfoUI);
            if (item == null) continue;

            item.transform.SetParent(infoSelectButon, false);
            item.transform.SetSiblingIndex(baseSiblingIndex + _activeInfoItems.Count);
            item.gameObject.SetActive(true);

            if (item.txtColor != null)
            {
                item.txtColor.text = count > 0 ? count.ToString() : "";
            }

            if (item.imgColor != null)
            {
                item.imgColor.color = TryGetInfoColor(objColor, out var c) ? c : Color.white;
            }

            _activeInfoItems.Add(item);
        }

        var size = infoSelectButon.sizeDelta;
        size.x = _activeInfoItems.Count * 100f;
        infoSelectButon.sizeDelta = size;

        LayoutRebuilder.ForceRebuildLayoutImmediate(infoSelectButon);
    }

    private bool TryGetInfoColor(ObjectColor color, out Color uiColor)
    {
        uiColor = default;
        if (_cellClearedVfxByColor != null)
        {
            for (int i = 0; i < _cellClearedVfxByColor.Count; i++)
            {
                var e = _cellClearedVfxByColor[i];
                if (e == null) continue;
                if (e.Color == color)
                {
                    uiColor = e.ColorVfx;
                    return true;
                }
            }
        }
        return false;
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
