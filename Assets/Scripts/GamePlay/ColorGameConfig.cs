using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;


[CreateAssetMenu(fileName = "Color game Config")]
public class GameColorConfig : SerializedScriptableObject
{
    [DictionaryDrawerSettings(KeyLabel = "Color", ValueLabel = "Materials")]
    public Dictionary<ObjectColor, ColorMaterial> ColorList;
    public Material GetButtonByColor(ObjectColor color)
    {
        return ColorList[color].Button;
    }
    public Material GetContainerByColor(ObjectColor color)
    {
        return ColorList[color].Container;
    }
}
[HideReferenceObjectPicker]
public class ColorMaterial
{
    [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
    public Material Button;
    [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
    public Material Container;
}

