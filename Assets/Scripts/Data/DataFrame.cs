using System;
using UnityEngine;

[Serializable]
public class DataFrame
{
    public GameObject Frame;
    public string description;
    
    public UnlockType unlockType;
    public int unlockLevel;
    public int price;
}