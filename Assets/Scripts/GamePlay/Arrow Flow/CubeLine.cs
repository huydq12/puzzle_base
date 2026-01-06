
using System.Collections.Generic;
using DG.Tweening;
using Dreamteck.Splines;
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
    [ReadOnly] public GridCell Cell;
    [OdinSerialize] private Dictionary<CubeType, List<Renderer>> _renderers;
    public bool isEngine = false;
    public CubeLine Back;
    public float offset = 0f;
    public CubeLine front;
    public SplinePositioner Positioner;
    private Quaternion _initRotation;

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
    public void RevertType()
    {
        transform.rotation = _initRotation;
        foreach (var pair in _renderers)
        {
            bool enable = pair.Key == Type;
            foreach (var r in pair.Value)
                r.enabled = enable;
        }
    }
    public void SetTempType(CubeType type)
    {
        _initRotation = transform.rotation;
        transform.rotation = Quaternion.identity;
        foreach (var pair in _renderers)
        {
            bool enable = pair.Key == type;
            foreach (var r in pair.Value)
                r.enabled = enable;
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
