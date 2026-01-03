
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
public enum Direction
{
    Forward = 0,
    Right = 1,
    Back = 2,
    Left = 3
}
public enum CubeType
{
    Normal,
    Corner,
    Head
}
public class CubeLine : SerializedMonoBehaviour
{
    [ReadOnly] public Line Line;
    [ReadOnly] public CubeType Type;
    [ReadOnly] public ObjectColor Color;
    [OdinSerialize] private Dictionary<CubeType, List<Renderer>> _renderers;
    public void SetAsNormal() => SetType(CubeType.Normal);
    public void SetAsCorner() => SetType(CubeType.Corner);
    public void SetAsHead() => SetType(CubeType.Head);
    public void SetColor(ObjectColor color)
    {
        Color = color;
        var mat = Board.Instance.ColorConfig.GetCubeByColor(color);
        foreach (var listRenderer in _renderers.Values)
        {
            foreach (var renderer in listRenderer)
            {
                renderer.sharedMaterial = mat;
            }
        }
    }
    public void SetType(CubeType type)
    {
        Type = type;

        foreach (var pair in _renderers)
        {
            bool enable = pair.Key == type;
            foreach (var r in pair.Value)
                r.enabled = enable;
        }
    }
}
