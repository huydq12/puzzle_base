using UnityEngine;
using System;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Newtonsoft.Json;


#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
using UnityEditor;
using Sirenix.OdinInspector.Editor;
#endif

[CreateAssetMenu(fileName = "New Level config")]
public class LevelConfig : SerializedScriptableObject
{
    public int Level;

}