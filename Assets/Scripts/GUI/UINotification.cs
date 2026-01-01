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
    
    public override bool ManualHide => false;

    public override bool DestroyOnHide => false;

    public override bool UseBehindPanel => false;

    [SerializeField] GameObject errorToast;

    public void ShowToast(string message, float waitInterval = 0.5f,Color color = default)
    {
        Show();
        errorToast.SetActive(false);
        toastText.text = message;
        if (color == default)
        {
            toastText.color = toast_text_color;
        }else
        {
            toastText.color = color;
        }
        toast.SetActive(true);

        Sequence seq = DOTween.Sequence();
        seq.Append(toast.transform.DOScale(1f, 0.25f));
        seq.AppendInterval(waitInterval);
        seq.Append(toast.transform.DOScale(0f, 0.25f));

        seq.OnComplete(() => {
            Hide();
        });
    }

    public void ShowToastError(string message)
    {
        Show();
        toastText.text = message;
        errorToast.SetActive(true);
    }
}
