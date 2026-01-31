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
    public Material GetShooterEye(ObjectColor color)
    {
        return ColorList[color].ShooterEye;
    }
    public Material GetShooterColor(ObjectColor color)
    {
        return ColorList[color].Shooter;
    }
}

[HideReferenceObjectPicker]
public class ColorMaterial
{
    public Material Cube;
    public Material Shooter;
    public Material ShooterEye;

}

