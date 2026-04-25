
using System;
using Lofelt.NiceVibrations;
using UnityEngine;

public class VibrateManager : Singleton<VibrateManager>
{
   [Serializable]
    public class VibrateEffect
    {
        public HapticType HapticType;
        public HapticClip HapticClip;
    }
    
    public enum HapticType
    {
        LightImpact,
        MediumImpact,
        SoftImpact,
        Selection,

        // Custom
        StrongLong,
        Lose
    }

    float _androidAmplitude = 0.01f;
    float _androidFrequency = 1f;

    private bool _isVibrationOn;
    public bool IsVibrationOn
    {
        get
        {
            return _isVibrationOn;
        }
        set
        {
            _isVibrationOn = value;
            PlayerPrefs.SetInt("VibrationOn", _isVibrationOn ? 1 : 0);
        }
    }

    public bool Initialized { get; private set; } = false;

    void Start()
    {
        Init();
    }

    public void Init()
    {
        if (Initialized)
        {
            return;
        }
        InitFeedback();
        InitEnable();
        Initialized = true;
    }

    private void InitFeedback()
    {
        HapticController.Init();
        LofeltHaptics.Initialize();
        DeviceCapabilities.Init();
    }

    private void InitEnable()
    {
        if (PlayerPrefs.HasKey("VibrationOn"))
        {
            int enable = PlayerPrefs.GetInt("VibrationOn");
            IsVibrationOn = Convert.ToBoolean(enable);
        }
        else
        {
            IsVibrationOn = true;
        }
    }

    public void PlayVibration(HapticType hapticType = HapticType.LightImpact)
    {
        if (!_isVibrationOn)
        {
            return;
        }

        if (CheckHapticRequirement())
        {
            if (!CheckHapticAdvanced())
            {
                HapticController.fallbackPreset = HapticPatterns.PresetType.MediumImpact;
            }

            switch (hapticType)
            {
                case HapticType.LightImpact:
#if UNITY_IOS
                    HapticPatterns.PlayEmphasis(_lightImpactIOSAmplitude, _lightImpactIOSFrequency);
#else
                    HapticPatterns.PlayConstant(_androidAmplitude, _androidFrequency, 0.03f);
#endif
                    return;
                case HapticType.MediumImpact:
#if UNITY_IOS
                    HapticPatterns.PlayPreset(HapticPatterns.PresetType.MediumImpact);
#else
                    HapticPatterns.PlayPreset(HapticPatterns.PresetType.MediumImpact);
#endif
                    return;
                case HapticType.SoftImpact:
#if UNITY_IOS
                    HapticPatterns.PlayConstant(_softImpactIOSAmplitude, _softImpactIOSFrequency, _softImpactIOSDuration);
#else
                    HapticPatterns.PlayConstant(_androidAmplitude, _androidFrequency, 0.04f);
#endif
                    return;
                case HapticType.Selection:
#if UNITY_IOS
                    HapticPatterns.PlayPreset(HapticPatterns.PresetType.LightImpact);
#else
                    HapticPatterns.PlayPreset(HapticPatterns.PresetType.Selection);
#endif
                    return;

                // custom
                case HapticType.StrongLong:
#if UNITY_IOS
                    HapticPatterns.PlayConstant(_strongLongIOSAmplitude, _strongLongIOSFrequency, _strongLongIOSDuration);
#else
                    HapticPatterns.PlayConstant(_androidAmplitude, _androidFrequency, 0.2f);
#endif
                    return;
                case HapticType.Lose:
#if UNITY_IOS
                    HapticPatterns.PlayConstant(_strongLongIOSAmplitude, _strongLongIOSFrequency, 0.1f);
#else
                    HapticPatterns.PlayConstant(_androidAmplitude, _androidFrequency, 0.1f);
#endif
                    return;
            }
        }
        Handheld.Vibrate();
    }

    public void PlayVibrationCustom(float amp, float freq, float duration)
    {
        if (!_isVibrationOn)
        {
            return;
        }

        if (CheckHapticRequirement())
        {
            if (!CheckHapticAdvanced())
            {
                HapticController.fallbackPreset = HapticPatterns.PresetType.LightImpact;
            }
            HapticPatterns.PlayConstant(amp, freq, duration);
            return;
        }
        Handheld.Vibrate();
    }

    public void SetVibrateEnabled(bool enabled)
    {
        IsVibrationOn = enabled;
    }

    public void StopVibration()
    {
        HapticController.Stop();
    }
    public void MediumVibrate()
    {
        PlayVibrationCustom(amp: 0.1f, freq: 1, duration: 0.025f);
    }
    public void StrongVibrate()
    {
        PlayVibrationCustom(amp: 0.3f, freq: 1, duration: 0.05f);
    }
    private bool CheckHapticRequirement() //CHECK BASIC HAPTIC
    {
        return DeviceCapabilities.isVersionSupported;
    }

    private bool CheckHapticAdvanced()  //CHECK ADVANCED HAPTIC
    {
        return DeviceCapabilities.meetsAdvancedRequirements;
    }
}
