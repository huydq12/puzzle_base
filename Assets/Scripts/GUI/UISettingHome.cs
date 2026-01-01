using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISettingHome : UIPopup
{
    [Header("Setting Buttons")]
    [SerializeField] private Button btnSFX;
    [SerializeField] private Button btnBGM;
    [SerializeField] private Button btnVibrate;
    [SerializeField] private Button btnClose;

    [Header("Button States")]
    [SerializeField] private GameObject soundOnIcon;
    [SerializeField] private GameObject soundOffIcon;
    [SerializeField] private GameObject musicOnIcon;
    [SerializeField] private GameObject musicOffIcon;
    [SerializeField] private GameObject vibrateOnIcon;
    [SerializeField] private GameObject vibrateOffIcon;

    private UserData userData;

    protected override void Start()
    {
        base.Start();
        userData = GameManager.Instance.userData;

        // Đăng ký sự kiện click cho các button
        btnSFX.onClick.AddListener(ToggleSound);
        btnBGM.onClick.AddListener(ToggleMusic);
        btnVibrate.onClick.AddListener(ToggleVibrate);
        btnClose.onClick.AddListener(ClosePopup);

        // Cập nhật UI theo trạng thái hiện tại
        UpdateUI();
    }

    private void ToggleSound()
    {
        if (userData == null) return;
        userData.soundOn = !userData.soundOn;
        userData.Save();
        UpdateSoundUI();

        var audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.SetSFXEnabled(userData.soundOn);
        }
    }

    private void ToggleMusic()
    {
        if (userData == null) return;
        userData.musicOn = !userData.musicOn;
        userData.Save();
        UpdateMusicUI();

        var audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.SetBGEnabled(userData.musicOn);
        }
    }

    private void ToggleVibrate()
    {
        if (userData == null) return;
        userData.vibrateOn = !userData.vibrateOn;
        userData.Save();
        UpdateVibrateUI();

        var vibrateManager = VibrateManager.Instance;
        if (vibrateManager != null)
        {
            vibrateManager.SetVibrateEnabled(userData.vibrateOn);
        }
    }

    private void UpdateUI()
    {
        UpdateSoundUI();
        UpdateMusicUI();
        UpdateVibrateUI();
    }

    private void UpdateSoundUI()
    {
        if (soundOnIcon != null && soundOffIcon != null)
        {
            soundOnIcon.SetActive(userData.soundOn);
            soundOffIcon.SetActive(!userData.soundOn);
        }
    }

    private void UpdateMusicUI()
    {
        if (musicOnIcon != null && musicOffIcon != null)
        {
            musicOnIcon.SetActive(userData.musicOn);
            musicOffIcon.SetActive(!userData.musicOn);
        }
    }

    private void UpdateVibrateUI()
    {
        if (vibrateOnIcon != null && vibrateOffIcon != null)
        {
            vibrateOnIcon.SetActive(userData.vibrateOn);
            vibrateOffIcon.SetActive(!userData.vibrateOn);
        }
    }

    private void ClosePopup()
    {
        Hide();
    }
}
