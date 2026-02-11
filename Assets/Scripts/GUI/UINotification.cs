using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
public class UINotification : UIElement
{
    [SerializeField] TextMeshProUGUI toastText;
    [SerializeField] GameObject toast;

    [SerializeField] Color toast_text_color;

    private Sequence toastSequence;
    
    public override bool ManualHide => false;

    public override bool DestroyOnHide => false;

    public override bool UseBehindPanel => false;

    [SerializeField] GameObject errorToast;

    public void ShowToast(string message, float waitInterval = 0.5f,Color color = default)
    {
        if (toastText == null || toast == null) return;

        Show();
        if (errorToast != null) errorToast.SetActive(false);
        toastText.text = message;
        if (color == default)
        {
            toastText.color = toast_text_color;
        }else
        {
            toastText.color = color;
        }
        toast.SetActive(true);
        toast.transform.localScale = Vector3.zero;

        if (toastSequence != null && toastSequence.IsActive())
        {
            toastSequence.Kill(false);
        }

        toastSequence = DOTween.Sequence().SetTarget(toast);
        toastSequence.Append(toast.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack));
        toastSequence.AppendInterval(waitInterval);
        toastSequence.Append(toast.transform.DOScale(0f, 0.25f).SetEase(Ease.InQuad));

        toastSequence.OnComplete(() =>
        {
            toastSequence = null;
            Hide();
        });
    }

    public void ShowToastError(string message)
    {
        if (toastText == null) return;

        Show();
        toastText.text = message;
        if (errorToast != null) errorToast.SetActive(true);
    }

    public override void Hide()
    {
        if (toastSequence != null && toastSequence.IsActive())
        {
            toastSequence.Kill(false);
        }
        toastSequence = null;
        base.Hide();
    }
}
