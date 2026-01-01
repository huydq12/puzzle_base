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
    public AudioClip GetBGClipSettings(BGType bgType)
    {
        if (BackgroundAudioClips.TryGetValue(bgType, out var clip))
        {
            return clip;
        }
        Debug.LogWarning($"BGType {bgType} not found in AudioConfig. Returning null.");
        return null;
    }
    public AudioClip GetSFXClipSettings(SFXType sfxType)
    {
        if (SFXAudioClips.TryGetValue(sfxType, out var clip))
        {
            return clip;
        }
        Debug.LogWarning($"SFXType {sfxType} not found in AudioConfig. Returning null.");
        return null;
    }
}