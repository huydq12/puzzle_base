using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class UINotification : BaseNotify
{
    private const float ToastShowDuration = 0.25f;
    private const float ToastHideDuration = 0.25f;
    private const float ToastHideDeadlinePadding = 0.1f;
    private const float HiddenDeadline = -1f;
    private const string RewardedUnavailableMessage = "Rewarded ad is not ready";
    private const string RewardNotGrantedMessage = "Reward not granted";

    [SerializeField] private TextMeshProUGUI toastText;
    [SerializeField] private GameObject toast;
    [SerializeField] private Color toast_text_color;
    [SerializeField] private GameObject errorToast;

    private Sequence toastSequence;
    private Coroutine autoHideCoroutine;
    private float _toastHideDeadline = -1f;
    private bool _isHidingToast;
    
    public override bool ManualHide => false;

    public override bool DestroyOnHide => false;

    public override bool UseBehindPanel => false;

    public void ShowRewardedUnavailableToast()
    {
        ShowToast(RewardedUnavailableMessage);
    }

    public void ShowRewardNotGrantedToast()
    {
        ShowToast(RewardNotGrantedMessage);
    }

    public void HideNow()
    {
        HideNotificationImmediate();
    }

    public void ShowToast(string message, float waitInterval = 0.5f, Color color = default)
    {
        if (toastText == null || toast == null) return;

        UIManager.Instance.ShowUI<UINotification>();
        ResetState();

        SetVisualState(true, false);
        toastText.text = message;
        toastText.color = color == default ? toast_text_color : color;
        toast.transform.localScale = Vector3.zero;
        _isHidingToast = false;
        _toastHideDeadline = Time.realtimeSinceStartup + ToastShowDuration + Mathf.Max(0f, waitInterval) + ToastHideDuration + ToastHideDeadlinePadding;

        toastSequence = DOTween.Sequence()
            .SetTarget(toast)
            .SetUpdate(true);
        toastSequence.Append(toast.transform.DOScale(1f, ToastShowDuration).SetEase(Ease.OutBack));
        toastSequence.OnKill(() => toastSequence = null);

        autoHideCoroutine = StartCoroutine(AutoHideToastRoutine(waitInterval));
    }

    public void ShowToastError(string message)
    {
        if (toastText == null) return;

        UIManager.Instance.ShowUI<UINotification>();
        ResetState();

        toastText.text = message;
        SetVisualState(false, true);
    }

    public override void BeforeHide()
    {
        base.BeforeHide();
        ResetState();
        SetVisualState(false, false);
    }

    private IEnumerator AutoHideToastRoutine(float waitInterval)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, waitInterval));

        ForceHideToast();
        autoHideCoroutine = null;
    }


    private void ForceHideToast()
    {
        if (_isHidingToast) return;
        _isHidingToast = true;
        _toastHideDeadline = HiddenDeadline;

        if (toast == null)
        {
            HideNotificationImmediate();
            return;
        }

        KillToastSequence();

        toastSequence = DOTween.Sequence()
            .SetTarget(toast)
            .SetUpdate(true);
        toastSequence.Append(toast.transform.DOScale(0f, ToastHideDuration).SetEase(Ease.InQuad));
        toastSequence.OnComplete(CompleteHideToast);
        toastSequence.OnKill(() =>
        {
            if (_isHidingToast)
            {
                CompleteHideToast();
            }
        });
    }

    private void TryHideExpiredToast()
    {
        if (ShouldHideToastNow())
        {
            ForceHideToast();
        }
    }

    private void HideNotificationImmediate()
    {
        ResetState();
        SetVisualState(false, false);

        _isHide = true;
        if (holder != null)
        {
            holder.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void CompleteHideToast()
    {
        HideNotificationImmediate();
    }

    private bool ShouldHideToastNow()
    {
        if (_toastHideDeadline < 0f || _isHidingToast) return false;
        if (toast == null || !toast.activeInHierarchy) return false;
        return Time.realtimeSinceStartup >= _toastHideDeadline;
    }

    private void ResetState()
    {
        _toastHideDeadline = HiddenDeadline;
        _isHidingToast = false;
        KillToastSequence();
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
    }

    private void KillToastSequence()
    {
        if (toastSequence != null && toastSequence.IsActive())
        {
            toastSequence.Kill(false);
        }

        toastSequence = null;
    }

    private void SetVisualState(bool showToast, bool showErrorToast)
    {
        if (toast != null) toast.SetActive(showToast);
        if (errorToast != null) errorToast.SetActive(showErrorToast);
    }
}
