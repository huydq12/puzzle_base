using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class AudioConfigAutoFillTool
{
    private const string ConfigPath = "Assets/Resources/Audio Config.asset";
    private const string SfxRoot = "Assets/SFX";

    private static readonly Dictionary<SFXType, string> SfxFolderMap = new()
    {
        { SFXType.BlockTapSelect, "Block_Tap_Select" },
        { SFXType.BlockSliding, "Block_Sliding" },
        { SFXType.BlockCollisionError, "Block_Collision_Error" },
        { SFXType.TurretShoot, "Turret_Shoot" },
        { SFXType.BulletHit, "Bullet_Hit" },
        { SFXType.BlockCollectedHoleIn, "Block_Collected_Hole_In" },
        { SFXType.ComboPitchUp, "Combo_Pitch_Up" },
        { SFXType.LevelClearConfetti, "Level_Clear_Confetti" },
        { SFXType.LevelFailed, "Level_Failed" },
        { SFXType.BoosterHammer, "Booster_Hammer" },
        { SFXType.BoosterWand, "Booster_Wand" },
        { SFXType.BoosterDropper, "Booster_Dropper" },
        { SFXType.UIClick, "UI_Click" },
        { SFXType.UIClickMenuPause, "UI_Click_Menu_Pause" },
    };

    [MenuItem("Tools/Audio/Auto Fill Audio Config From Assets/SFX")]
    public static void AutoFill()
    {
        AudioConfig config = AssetDatabase.LoadAssetAtPath<AudioConfig>(ConfigPath);
        if (config == null)
        {
            Debug.LogError($"AudioConfig not found at {ConfigPath}");
            return;
        }

        config.BackgroundAudioClips ??= new Dictionary<BGType, AudioClip>();
        config.SFXAudioClips ??= new Dictionary<SFXType, AudioClip>();
        config.SFXAudioClipVariants ??= new Dictionary<SFXType, AudioClip[]>();

        AssignBackground(config);
        AssignSfx(config);

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[AudioConfigAutoFillTool] Audio Config updated from Assets/SFX.");
    }

    private static void AssignBackground(AudioConfig config)
    {
        AudioClip[] clips = LoadClips(Path.Combine(SfxRoot, "Gameplay_BGM"));
        if (clips.Length <= 0) return;

        config.BackgroundAudioClips[BGType.Gameplay] = clips[0];
        config.BackgroundAudioClips[BGType.Default] = clips[0];
    }

    private static void AssignSfx(AudioConfig config)
    {
        foreach (var pair in SfxFolderMap)
        {
            AudioClip[] clips = LoadClips(Path.Combine(SfxRoot, pair.Value));
            if (clips.Length <= 0) continue;

            config.SFXAudioClips[pair.Key] = clips[0];
            config.SFXAudioClipVariants[pair.Key] = clips;
        }
    }

    private static AudioClip[] LoadClips(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder)) return new AudioClip[0];

        return AssetDatabase.FindAssets("t:AudioClip", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => path)
            .Select(AssetDatabase.LoadAssetAtPath<AudioClip>)
            .Where(clip => clip != null)
            .ToArray();
    }
}
