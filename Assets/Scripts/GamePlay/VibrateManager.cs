
using System.Collections;
using Lofelt.NiceVibrations;
using UnityEngine;

public enum HapticType
{
    BlockTapSelect,
    BlockCollisionError,
    TurretShoot,
    BlockCollectedHoleIn,
    ComboPitchUp,
    LevelClearConfetti,
    LevelFailed,
    BoosterHammer,
    BoosterWand,
    BoosterDropper,
    UIClick
}

public class VibrateManager : Singleton<VibrateManager>
{
    private bool _vibrateEnabled = true;
    private float _lastTurretShootTime = -1f;
    private float _lastUIClickTime = -1f;

    private const float TurretShootMinInterval = 0.06f;
    private const float UIClickMinInterval = 0.04f;

    private new void Awake()
    {
        base.Awake();
        HapticController.Init();

        Game.Launch();
        var userData = Game.Data.Load<UserData>();
        if (userData != null)
        {
            SetVibrateEnabled(userData.vibrateOn);
        }
    }
    public void SmallVibrate()
    {
        PlayHaptic(HapticType.UIClick);
    }
    public void MediumVibrate()
    {
        PlayHaptic(HapticType.BlockCollectedHoleIn);
    }

    public void PlayHaptic(HapticType hapticType)
    {
        if (!_vibrateEnabled) return;

        switch (hapticType)
        {
            case HapticType.BlockTapSelect:
                HapticPatterns.PlayPreset(HapticPatterns.PresetType.LightImpact);
                break;

            case HapticType.BlockCollisionError:
                HapticPatterns.PlayPreset(HapticPatterns.PresetType.RigidImpact);
                break;

            case HapticType.TurretShoot:
                if (!CanPlayThrottled(ref _lastTurretShootTime, TurretShootMinInterval)) return;
                HapticPatterns.PlayEmphasis(0.22f, 0.55f);
                break;

            case HapticType.BlockCollectedHoleIn:
                HapticPatterns.PlayPreset(HapticPatterns.PresetType.MediumImpact);
                break;

            case HapticType.ComboPitchUp:
                StartCoroutine(PlayComboPitchUp());
                break;

            case HapticType.LevelClearConfetti:
                StartCoroutine(PlayLevelClearConfetti());
                break;

            case HapticType.LevelFailed:
                StartCoroutine(PlayLevelFailed());
                break;

            case HapticType.BoosterHammer:
                HapticPatterns.PlayPreset(HapticPatterns.PresetType.HeavyImpact);
                break;

            case HapticType.BoosterWand:
                StartCoroutine(PlayLightRipple());
                break;

            case HapticType.BoosterDropper:
                StartCoroutine(PlaySoftTapFade());
                break;

            case HapticType.UIClick:
                if (!CanPlayThrottled(ref _lastUIClickTime, UIClickMinInterval)) return;
                HapticPatterns.PlayPreset(HapticPatterns.PresetType.Selection);
                break;
        }
    }

    public void SetVibrateEnabled(bool enabled)
    {
        _vibrateEnabled = enabled;
        HapticController.hapticsEnabled = enabled;
        if (!enabled)
        {
            HapticController.Stop();
        }
    }

    public bool IsVibrateEnabled() => _vibrateEnabled;

    private static bool CanPlayThrottled(ref float lastPlayTime, float minInterval)
    {
        if (Time.unscaledTime - lastPlayTime < minInterval) return false;
        lastPlayTime = Time.unscaledTime;
        return true;
    }

    private static IEnumerator PlayComboPitchUp()
    {
        HapticPatterns.PlayEmphasis(0.20f, 0.35f);
        yield return new WaitForSecondsRealtime(0.055f);
        HapticPatterns.PlayEmphasis(0.35f, 0.55f);
        yield return new WaitForSecondsRealtime(0.055f);
        HapticPatterns.PlayEmphasis(0.55f, 0.75f);
    }

    private static IEnumerator PlayLevelClearConfetti()
    {
        HapticPatterns.PlayPreset(HapticPatterns.PresetType.Success);
        yield return new WaitForSecondsRealtime(0.18f);
        HapticPatterns.PlayEmphasis(0.20f, 0.65f);
        yield return new WaitForSecondsRealtime(0.06f);
        HapticPatterns.PlayEmphasis(0.16f, 0.8f);
        yield return new WaitForSecondsRealtime(0.06f);
        HapticPatterns.PlayEmphasis(0.12f, 0.9f);
    }

    private static IEnumerator PlayLevelFailed()
    {
        HapticPatterns.PlayPreset(HapticPatterns.PresetType.HeavyImpact);
        yield return new WaitForSecondsRealtime(0.22f);
        HapticPatterns.PlayPreset(HapticPatterns.PresetType.HeavyImpact);
    }

    private static IEnumerator PlayLightRipple()
    {
        HapticPatterns.PlayEmphasis(0.18f, 0.75f);
        yield return new WaitForSecondsRealtime(0.045f);
        HapticPatterns.PlayEmphasis(0.14f, 0.85f);
        yield return new WaitForSecondsRealtime(0.045f);
        HapticPatterns.PlayEmphasis(0.10f, 0.95f);
    }

    private static IEnumerator PlaySoftTapFade()
    {
        HapticPatterns.PlayPreset(HapticPatterns.PresetType.SoftImpact);
        yield return new WaitForSecondsRealtime(0.07f);
        HapticPatterns.PlayEmphasis(0.12f, 0.45f);
        yield return new WaitForSecondsRealtime(0.07f);
        HapticPatterns.PlayEmphasis(0.06f, 0.35f);
    }
}
