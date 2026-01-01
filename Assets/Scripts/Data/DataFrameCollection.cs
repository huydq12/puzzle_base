using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FrameCollection", menuName = "Game/FrameCollection")]
public class DataFrameCollection : ScriptableObject
{
    public List<DataFrame> listFrame;
}