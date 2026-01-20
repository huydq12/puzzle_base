using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITutorial : UIPopup
{
    [Header("Config (optional)")]
    [SerializeField] private TutorialPopupConfig _config;

    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _title;
    [SerializeField] private TextMeshProUGUI _description;
    [SerializeField] private Button _btnClose;

    private bool _useOverrideContent;
    private Sprite _overrideIcon;
    private string _overrideTitle;
    private string _overrideDescription;

    private TutorialPopupConfig Config => _config != null ? _config : TutorialPopupService.Config;

    void Start()
    {
        if (_btnClose != null)
        {
            _btnClose.onClick.AddListener(Hide);
        }
    }

    public override void Show()
    {
        RefreshView();
        base.Show();
    }

    public override void Hide()
    {
        _useOverrideContent = false;
        base.Hide();
    }

    public void ShowBoosterTutorial(Sprite icon, string title, string description)
    {
        _useOverrideContent = true;
        _overrideIcon = icon;
        _overrideTitle = title;
        _overrideDescription = description;
        Show();
    }

    private void RefreshView()
    {
        if (_useOverrideContent)
        {
            if (_icon != null)
            {
                _icon.sprite = _overrideIcon;
                _icon.enabled = _overrideIcon != null;
            }
            if (_title != null) _title.text = _overrideTitle ?? string.Empty;
            if (_description != null) _description.text = _overrideDescription ?? string.Empty;
            return;
        }

        if (TutorialManager.Instance == null) return;

        var cfg = Config;
        if (cfg != null)
        {
            int level = Mathf.Max(1, GameManagerInGame.Instance.CurrentLevel);
            var entry = cfg.GetEntry(level);
            if (entry != null)
            {
                ApplyTutorialContent(entry.icon, entry.title, entry.description);
                return;
            }
        }

	        ApplyTutorialContent(null, string.Empty, string.Empty);
	    }

    private void ApplyTutorialContent(Sprite icon, string title, string description)
    {
        if (_icon != null)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;
        }
        if (_title != null) _title.text = title ?? string.Empty;
        if (_description != null) _description.text = description ?? string.Empty;
    }
}
