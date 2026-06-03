using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;


[CreateAssetMenu(fileName = "Audio Config")]
public class AudioConfig : SerializedScriptableObject
{
    [OdinSerialize]
    [DictionaryDrawerSettings(KeyLabel = "Loại", ValueLabel = "Biến thể")]
    public Dictionary<BGType, AudioClip[]> BackgroundAudioClipVariants;
    [OdinSerialize]
    [DictionaryDrawerSettings(KeyLabel = "Loại", ValueLabel = "Biến thể")]
    public Dictionary<SFXType, AudioClip[]> SFXAudioClipVariants;
    public AudioClip GetBGClipSettings(BGType bgType)
    {
        if (BackgroundAudioClipVariants != null
            && BackgroundAudioClipVariants.TryGetValue(bgType, out var clips)
            && clips != null
            && clips.Length > 0)
        {
            int index = Random.Range(0, clips.Length);
            return clips[index];
        }

        if (bgType == BGType.GameplayHard
            && BackgroundAudioClipVariants != null
            && BackgroundAudioClipVariants.TryGetValue(BGType.Gameplay, out var fallbackClips)
            && fallbackClips != null
            && fallbackClips.Length > 0)
        {
            int index = Random.Range(0, fallbackClips.Length);
            return fallbackClips[index];
        }

        Debug.LogWarning($"BGType {bgType} not found in AudioConfig. Returning null.");
        return null;
    }
    public AudioClip GetSFXClipSettings(SFXType sfxType)
    {
        if (SFXAudioClipVariants != null
            && SFXAudioClipVariants.TryGetValue(sfxType, out var clips)
            && clips != null
            && clips.Length > 0)
        {
            int index = Random.Range(0, clips.Length);
            return clips[index];
        }

        Debug.LogWarning($"SFXType {sfxType} not found in AudioConfig. Returning null.");
        return null;
    }
}
