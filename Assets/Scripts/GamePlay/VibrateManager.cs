
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
                StartCoroutine(PlayBlockTapSelect());
                break;

            case HapticType.BlockCollisionError:
                StartCoroutine(PlayBlockCollisionError());
                break;

            case HapticType.TurretShoot:
                if (!CanPlayThrottled(ref _lastTurretShootTime, TurretShootMinInterval)) return;
                HapticPatterns.PlayEmphasis(0.32f, 0.75f);
                break;

            case HapticType.BlockCollectedHoleIn:
                StartCoroutine(PlayBlockCollectedHoleIn());
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
                StartCoroutine(PlayBoosterHammer());
                break;

            case HapticType.BoosterWand:
                StartCoroutine(PlayLightRipple());
                break;

            case HapticType.BoosterDropper:
                StartCoroutine(PlaySoftTapFade());
                break;

            case HapticType.UIClick:
                if (!CanPlayThrottled(ref _lastUIClickTime, UIClickMinInterval)) return;
                HapticPatterns.PlayEmphasis(0.20f, 0.7f);
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

    private static IEnumerator PlayBlockTapSelect()
    {
        HapticPatterns.PlayEmphasis(0.24f, 0.72f);
        yield return new WaitForSecondsRealtime(0.03f);
        HapticPatterns.PlayEmphasis(0.12f, 0.82f);
    }

    private static IEnumerator PlayBlockCollisionError()
    {
        HapticPatterns.PlayEmphasis(0.42f, 0.35f);
        yield return new WaitForSecondsRealtime(0.055f);
        HapticPatterns.PlayEmphasis(0.28f, 0.2f);
    }

    private static IEnumerator PlayBlockCollectedHoleIn()
    {
        HapticPatterns.PlayEmphasis(0.32f, 0.68f);
        yield return new WaitForSecondsRealtime(0.04f);
        HapticPatterns.PlayEmphasis(0.18f, 0.9f);
    }

    private static IEnumerator PlayComboPitchUp()
    {
        HapticPatterns.PlayEmphasis(0.26f, 0.45f);
        yield return new WaitForSecondsRealtime(0.055f);
        HapticPatterns.PlayEmphasis(0.42f, 0.62f);
        yield return new WaitForSecondsRealtime(0.055f);
        HapticPatterns.PlayEmphasis(0.62f, 0.8f);
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

    private static IEnumerator PlayBoosterHammer()
    {
        HapticPatterns.PlayPreset(HapticPatterns.PresetType.HeavyImpact);
        yield return new WaitForSecondsRealtime(0.05f);
        HapticPatterns.PlayEmphasis(0.4f, 0.3f);
    }

    private static IEnumerator PlayLightRipple()
    {
        HapticPatterns.PlayEmphasis(0.26f, 0.78f);
        yield return new WaitForSecondsRealtime(0.045f);
        HapticPatterns.PlayEmphasis(0.2f, 0.88f);
        yield return new WaitForSecondsRealtime(0.045f);
        HapticPatterns.PlayEmphasis(0.15f, 0.96f);
    }

    private static IEnumerator PlaySoftTapFade()
    {
        HapticPatterns.PlayEmphasis(0.22f, 0.58f);
        yield return new WaitForSecondsRealtime(0.07f);
        HapticPatterns.PlayEmphasis(0.16f, 0.5f);
        yield return new WaitForSecondsRealtime(0.07f);
        HapticPatterns.PlayEmphasis(0.1f, 0.4f);
    }
}
