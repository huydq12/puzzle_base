using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;


[CreateAssetMenu(fileName = "Color game Config")]
public class GameColorConfig : SerializedScriptableObject
{
    [DictionaryDrawerSettings(KeyLabel = "Color", ValueLabel = "Materials")]
    public Dictionary<ObjectColor, ColorMaterial> ColorList;
    public Material GetCubeByColor(ObjectColor color)
    {
        return ColorList[color].Cube;
    }
    public Material GetShooterByColor(ObjectColor color)
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
    public Material Shooter;
}

