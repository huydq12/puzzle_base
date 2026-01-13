using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;


[CreateAssetMenu(fileName = "Color game Config")]
public class GameColorConfig : SerializedScriptableObject
{
    [DictionaryDrawerSettings(KeyLabel = "Color", ValueLabel = "Materials")]
    public Dictionary<ObjectColor, ColorMaterial> ColorList;
    public Material GetCubeColor(ObjectColor color)
    {
        return ColorList[color].Cube;
    }
    public Material GetCubeHeadColor(ObjectColor color)
    {
        return ColorList[color].Head;
    }
    public Material GetShooterColor(ObjectColor color)
    {
        return ColorList[color].Shooter;
    }
}

[HideReferenceObjectPicker]
public class ColorMaterial
{
    [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
    public Material Cube;
     [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
    public Material Head;
    [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
    public Material Shooter;
}

