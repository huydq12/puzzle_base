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
        return TryGetColorMaterial(color, out var mat) ? mat.Cube : null;
    }
    public Material GetCubeHeadColor(ObjectColor color)
    {
        return TryGetColorMaterial(color, out var mat) ? mat.Head : null;
    }
    public Material GetShooterColor(ObjectColor color)
    {
        return TryGetColorMaterial(color, out var mat) ? mat.Shooter : null;
    }
    public Material GetShooterEyeColor(ObjectColor color)
    {
        return TryGetColorMaterial(color, out var mat) ? mat.ShooterEye : null;
    }
    public Color GetOutlineShooter(ObjectColor color)
    {
        return TryGetColorMaterial(color, out var mat) ? mat.Outline : Color.clear;
    }

    public Color GetLineDoorColor(ObjectColor color)
    {
        return TryGetColorMaterial(color, out var mat) ? mat.LineDoor : Color.white;
    }

    private bool TryGetColorMaterial(ObjectColor color, out ColorMaterial mat)
    {
        mat = null;
        if (color == ObjectColor.None) return false;
        return ColorList != null && ColorList.TryGetValue(color, out mat) && mat != null;
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
    [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
    public Material ShooterEye;
    public Color Outline;
    public Color LineDoor;
}
