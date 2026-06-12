using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class UISettingInGame : UIPopup
{
    [Header("Setting Buttons")]
    [SerializeField] private ButtonBehavior btnSFX;
    [SerializeField] private ButtonBehavior btnBGM;
    [SerializeField] private ButtonBehavior btnVibrate;
    [SerializeField] private ButtonBehavior btnPrivacyPolicy;
    [SerializeField] private ButtonBehavior btnTermsOfUse;
    [SerializeField] private ButtonBehavior btnClose;
    [SerializeField] private ButtonBehavior btnContinue;
    [SerializeField] private ButtonBehavior btnResetLevel;

    private UserData userData;
    private Object userConsentSettings;

    protected override void Start()
    {
        base.Start();
        userData = GetUserData();

        if (btnSFX != null) btnSFX.OnClick.AddListener(ToggleSound);
        if (btnBGM != null) btnBGM.OnClick.AddListener(ToggleMusic);
        if (btnVibrate != null) btnVibrate.OnClick.AddListener(ToggleVibrate);
        if (btnPrivacyPolicy != null) btnPrivacyPolicy.OnClick.AddListener(OpenPrivacyPolicy);
        if (btnTermsOfUse != null) btnTermsOfUse.OnClick.AddListener(OpenTermsOfUse);
        if (btnClose != null) btnClose.OnClick.AddListener(ClosePopup);
        if (btnContinue != null) btnContinue.OnClick.AddListener(ContinueGame);
        if (btnResetLevel != null) btnResetLevel.OnClick.AddListener(ResetLevel);

        UpdateUI();
    }

    private void ToggleSound()
    {
        if (userData == null) return;
        SetSoundEnabled(!userData.soundOn);
    }

    private void ToggleMusic()
    {
        if (userData == null) return;
        SetMusicEnabled(!userData.musicOn);
    }

    private void ToggleVibrate()
    {
        if (userData == null) return;
        SetVibrateEnabled(!userData.vibrateOn);
    }

    private void UpdateUI()
    {
        if (userData == null) return;
        UpdateSoundUI();
        UpdateMusicUI();
        UpdateVibrateUI();
    }

    private void UpdateSoundUI()
    {
        if (userData == null) return;
        if (btnSFX != null)
        {
            btnSFX.SetSelected(userData.soundOn, true);
        }
    }

    private void UpdateMusicUI()
    {
        if (userData == null) return;
        if (btnBGM != null)
        {
            btnBGM.SetSelected(userData.musicOn, true);
        }
    }

    private void UpdateVibrateUI()
    {
        if (userData == null) return;
        if (btnVibrate != null)
        {
            btnVibrate.SetSelected(userData.vibrateOn, true);
        }
    }

    private void ClosePopup()
    {
        UIManager.Instance.HideUI<UISettingInGame>();
    }

    private void OpenPrivacyPolicy()
    {
        OpenConfiguredUrl("privacyLink", "privacyLinkIos");
    }

    private void OpenTermsOfUse()
    {
        OpenConfiguredUrl("termsLink", "termsLinkIos");
    }

    private void OpenConfiguredUrl(string defaultFieldName, string iosFieldName)
    {
        var settings = GetUserConsentSettings();
        if (settings == null)
        {
            Debug.LogWarning("UISettingInGame: Could not load Resources/UserConsentSettings.");
            return;
        }

        string url = GetStringFieldValue(settings, defaultFieldName);
#if UNITY_IOS
        string iosUrl = GetStringFieldValue(settings, iosFieldName);
        if (!string.IsNullOrEmpty(iosUrl))
        {
            url = iosUrl;
        }
#endif

        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogWarning($"UISettingInGame: URL field is empty for {defaultFieldName}.");
            return;
        }

        Application.OpenURL(url);
    }

    private Object GetUserConsentSettings()
    {
        if (userConsentSettings == null)
        {
            userConsentSettings = Resources.Load("UserConsentSettings");
        }

        return userConsentSettings;
    }

    private static string GetStringFieldValue(Object target, string fieldName)
    {
        if (target == null || string.IsNullOrEmpty(fieldName))
        {
            return string.Empty;
        }

        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null || field.FieldType != typeof(string))
        {
            return string.Empty;
        }

        return field.GetValue(target) as string ?? string.Empty;
    }

    private void SetSoundEnabled(bool enabled)
    {
        if (userData == null) return;
        userData.soundOn = enabled;
        userData.Save();
        UpdateSoundUI();

        var audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.SetSFXEnabled(enabled);
        }
    }

    private void SetMusicEnabled(bool enabled)
    {
        if (userData == null) return;
        userData.musicOn = enabled;
        userData.Save();
        UpdateMusicUI();

        var audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.SetBGEnabled(enabled);
        }
    }

    private void SetVibrateEnabled(bool enabled)
    {
        if (userData == null) return;
        userData.vibrateOn = enabled;
        userData.Save();
        UpdateVibrateUI();

        var vibrateManager = VibrateManager.Instance;
        if (vibrateManager != null)
        {
            vibrateManager.SetVibrateEnabled(enabled);
        }
    }

    private void ContinueGame()
    {
        UIManager.Instance.HideUI<UISettingInGame>();
    }

    private void ResetLevel()
    {
        var heatManager = HeatManager.TryGetInstance();
        if (heatManager != null && !heatManager.CanPlay())
        {
            UIManager.Instance.HideUI<UISettingInGame>();
            UIManager.Instance.ShowUI<UIPopupNoHeat>();
            return;
        }

        heatManager?.ConsumeHeat();
        UIManager.Instance.HideUI<UISettingInGame>();

        var gameManager = GameManagerInGame.Instance;
        if (gameManager != null)
        {
            gameManager.ReplayLevel();
        }
    }

    private UserData GetUserData()
    {
        var gameManagerInGame = GameManagerInGame.Instance;
        if (gameManagerInGame != null && gameManagerInGame.userData != null)
        {
            return gameManagerInGame.userData;
        }

        return null;
    }
}
