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

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureIndependentClipArrays(BackgroundAudioClipVariants);
        EnsureIndependentClipArrays(SFXAudioClipVariants);
    }
#endif

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

    private static void EnsureIndependentClipArrays<TKey>(Dictionary<TKey, AudioClip[]> variants)
    {
        if (variants == null || variants.Count <= 1) return;

        HashSet<AudioClip[]> usedArrays = new HashSet<AudioClip[]>();
        List<TKey> keys = new List<TKey>(variants.Keys);

        for (int i = 0; i < keys.Count; i++)
        {
            TKey key = keys[i];
            AudioClip[] clips = variants[key];
            if (clips == null) continue;

            if (!usedArrays.Add(clips))
            {
                variants[key] = (AudioClip[])clips.Clone();
            }
        }
    }
}
