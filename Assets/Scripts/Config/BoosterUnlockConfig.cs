using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BoosterUnlockConfig", menuName = "Config/Booster Unlock Config")]
public class BoosterUnlockConfig : ScriptableObject
{
    [Serializable]
    public class BoosterEntry
    {
        public int boosterType = 1;
        public int unlockLevel = 1;
        public Sprite tutorialIcon;
        public string tutorialTitle;
        public string tutorialDescription;
        public int giftAmount = 1;
    }

    public string resourcesPath = "Configs/BoosterUnlockConfig";

    public string lockedToastFormat = "Unlocks at level {0}";

    public List<BoosterEntry> boosters = new List<BoosterEntry>();

    public BoosterEntry GetEntry(int boosterType)
    {
        if (boosters == null) return null;
        for (int i = 0; i < boosters.Count; i++)
        {
            var e = boosters[i];
            if (e != null && e.boosterType == boosterType) return e;
        }
        return null;
    }
}
