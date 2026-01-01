using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "AvatarCollection", menuName = "Game/DataAvatar")]
public class DataAvatarCollection : ScriptableObject
{
    public List<DataAvatar> listAvatar;
}

[Serializable]
public class DataAvatar
{
    public GameObject Avatar;
    public string description;
    
    public UnlockType unlockType;
    public int unlockLevel;
    public int price;
}

public enum UnlockType
{
    Default,
    Level,
    Purchase,
    LevelOrPurchase
}
