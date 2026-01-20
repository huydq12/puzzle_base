using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITutorialBotter : UIPopup
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _title;
    [SerializeField] private TextMeshProUGUI _description;

    [SerializeField] private float autoHideSeconds = 3f;
    private Coroutine autoHideRoutine;

    public override void Show()
    {
        base.Show();
        StartAutoHide();
    }

    public override void Hide()
    {
        StopAutoHide();
        base.Hide();
    }

    private void StartAutoHide()
    {
        StopAutoHide();
        autoHideRoutine = StartCoroutine(AutoHideRoutine());
    }

    private void StopAutoHide()
    {
        if (autoHideRoutine == null) return;
        StopCoroutine(autoHideRoutine);
        autoHideRoutine = null;
    }

    private IEnumerator AutoHideRoutine()
    {
        yield return new WaitForSeconds(autoHideSeconds);
        Hide();
    }

    public void ShowForBooster(int boosterType)
    {
        var cfg = BoosterUnlockService.Config;
        if (cfg == null)
        {
            ShowBoosterTutorial(null, string.Empty, string.Empty);
            return;
        }

        var entry = cfg.GetEntry(boosterType);
        if (entry == null)
        {
            ShowBoosterTutorial(null, string.Empty, string.Empty);
            return;
        }

        ShowBoosterTutorial(entry.tutorialIcon, entry.tutorialTitle, entry.tutorialDescription);
    }

    public void ShowBoosterTutorial(Sprite icon, string title, string description)
    {
        if (_icon != null)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;
        }

        if (_title != null)
        {
            _title.text = title;
        }

        if (_description != null)
        {
            _description.text = description;
        }

        Show();
    }
}
