
using System.Collections.Generic;
using DG.Tweening;
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
    [SerializeField] private ParticleSystem _hitEffect;
    [SerializeField] private ParticleSystem _wariningEffect;
    [SerializeField] private ParticleSystem _warningHeadEffect;
    [OdinSerialize] private Dictionary<CubeType, Renderer> _renderers;
    [SerializeField] private Renderer _head;
    [SerializeField] private Outline _outline;
    private Quaternion _initRotation;
    public bool HighlightHead
    {
        get => _outline.enabled;
        set => _outline.enabled = value;
    }
    public void ShowWarning()
    {
        _warningHeadEffect.Stop();
        _wariningEffect.Stop();
        if (Type == CubeType.Head)
        {
            _warningHeadEffect.Play();
        }
        else
        {
            _wariningEffect.Play();
        }
    }
    public void OnHit()
    {
        ConveyorController.Instance.RemoveCubeFromPath(this);
        Instantiate(_hitEffect, transform.position, Quaternion.identity);
        transform.DOScale(0f, 0.1f).OnComplete(() => Destroy(gameObject));
    }
    public void SetColor(ObjectColor color)
    {
        Color = color;
        var mat = Board.Instance.ColorConfig.GetCubeColor(color);
        foreach (var renderer in _renderers.Values)
        {
            renderer.sharedMaterial = mat;
        }
    }
    public void RevertType()
    {
        transform.rotation = _initRotation;
        foreach (var pair in _renderers)
        {
            bool enable = pair.Key == Type;
            pair.Value.enabled = enable;
        }
    }
    public void SetTempType(CubeType type)
    {
        _initRotation = transform.rotation;
        transform.rotation = Quaternion.identity;
        _head.enabled = type == CubeType.Head;
        foreach (var pair in _renderers)
        {
            bool enable = pair.Key == type;
            pair.Value.enabled = enable;
        }
    }
    public void SetType(CubeType type)
    {
        Type = type;
        if (type == CubeType.Head)
        {
            _head.enabled = true;
            _head.material = Board.Instance.ColorConfig.GetCubeHeadColor(Color);
        }
        else
        {
            _head.enabled = false;
        }
        foreach (var pair in _renderers)
        {
            bool enable = pair.Key == type;
            pair.Value.enabled = enable;
        }
    }
}
