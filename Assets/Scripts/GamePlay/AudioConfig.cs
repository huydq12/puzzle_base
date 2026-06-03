using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;


[CreateAssetMenu(fileName = "Audio Config")]
public class AudioConfig : SerializedScriptableObject
{
    [OdinSerialize]
    [DictionaryDrawerSettings(KeyLabel = "Loại", ValueLabel = "Thiết lập")]
    public Dictionary<BGType, AudioClip> BackgroundAudioClips;
    [OdinSerialize]
    [DictionaryDrawerSettings(KeyLabel = "Loại", ValueLabel = "Thiết lập")]
    public Dictionary<SFXType, AudioClip> SFXAudioClips;
    [OdinSerialize]
    [DictionaryDrawerSettings(KeyLabel = "Loại", ValueLabel = "Biến thể")]
    public Dictionary<SFXType, AudioClip[]> SFXAudioClipVariants;
    public AudioClip GetBGClipSettings(BGType bgType)
    {
        if (BackgroundAudioClips != null && BackgroundAudioClips.TryGetValue(bgType, out var clip))
        {
            return clip;
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

        if (SFXAudioClips != null && SFXAudioClips.TryGetValue(sfxType, out var clip))
        {
            return clip;
        }
        Debug.LogWarning($"SFXType {sfxType} not found in AudioConfig. Returning null.");
        return null;
    }
}
