using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;


[CreateAssetMenu(fileName = "Color game Config")]
public class GameColorConfig : SerializedScriptableObject
{
    [DictionaryDrawerSettings(KeyLabel = "Color", ValueLabel = "Materials")]
    public Dictionary<ObjectColor, ColorMaterial> ColorList;

    [System.NonSerialized] private readonly HashSet<ObjectColor> _missingColorWarnings = new();

    private ColorMaterial ResolveColorMaterial(ObjectColor color)
    {
        if (ColorList != null && ColorList.TryGetValue(color, out ColorMaterial mapped) && mapped != null)
            return mapped;

        if (_missingColorWarnings.Add(color))
        {
            Debug.LogWarning($"[{nameof(GameColorConfig)}] Missing mapping for color '{color}' on config '{name}'. Using fallback color material if available.");
        }

        if (ColorList == null) return null;

        foreach (var pair in ColorList)
        {
            if (pair.Value != null)
                return pair.Value;
        }

        return null;
    }

    public Material GetCubeColor(ObjectColor color)
    {
        return ResolveColorMaterial(color)?.Cube;
    }
    public Material GetCubeHeadColor(ObjectColor color)
    {
        return ResolveColorMaterial(color)?.Head;
    }
    public Material GetShooterColor(ObjectColor color)
    {
        return ResolveColorMaterial(color)?.Shooter;
    }
    public Color GetOutlineShooter(ObjectColor color)
    {
        return ResolveColorMaterial(color)?.OutlineColor ?? Color.white;
    }
    public Material GetShooterEyeColor(ObjectColor color)
    {
        return ResolveColorMaterial(color)?.ShooterEye;
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
    public Color OutlineColor;
}
