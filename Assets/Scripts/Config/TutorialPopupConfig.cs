using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TutorialPopupConfig", menuName = "Config/Tutorial Popup Config")]
public class TutorialPopupConfig : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Min(1)] public int level = 1;
        public TutorialType type;
        public Sprite icon;
        public string title;
        public string description;
    }

    public string resourcesPath = "Configs/TutorialPopupConfig";

    public List<Entry> entries = new List<Entry>();

    public Entry GetEntry(TutorialType type)
    {
        if (entries == null) return null;
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e != null && e.type == type) return e;
        }
        return null;
    }

    public Entry GetEntry(int level)
    {
        if (entries == null) return null;
        level = Mathf.Max(1, level);
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e != null && Mathf.Max(1, e.level) == level) return e;
        }
        return null;
    }
}
