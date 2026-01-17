
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
    [ReadOnly] public int ElementType;
    [ReadOnly] public GridCell Cell;
    [SerializeField] private ParticleSystem _hitEffect;
    [SerializeField] private ParticleSystem _wariningEffect;
    [SerializeField] private ParticleSystem _warningHeadEffect;
    [OdinSerialize] private Dictionary<CubeType, Renderer> _renderers;
    [SerializeField] private Renderer _head;
    [SerializeField] private Outline _outline;
    [SerializeField] private Material _materialElementType2;
    [SerializeField] private Material _materialElementType3;
    [SerializeField] private Material _materialElementType8;
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
        if (ElementType == 2 && Line != null && Line.IsIceLine && Line.RemainingCounter > 0)
            return;

        ConveyorController.Instance.RemoveCubeFromPath(this);
        Instantiate(_hitEffect, transform.position, Quaternion.identity);
        transform.DOScale(0f, 0.1f).OnComplete(() => Destroy(gameObject));
    }
    public void SetColor(ObjectColor color)
    {
        Color = color;
        ApplyMaterials(Type);
    }

    public void SetElementType(int elementType)
    {
        ElementType = elementType;
        ApplyMaterials(Type);
    }

    private Material GetElementTypeMaterial()
    {
        if (ElementType == 2) return _materialElementType2;
        if (ElementType == 3) return _materialElementType3;
        if (ElementType == 8) return _materialElementType8;
        return null;
    }

    private void ApplyMaterials(CubeType targetType)
    {
        Material overrideMat = GetElementTypeMaterial();

        Material cubeMat = overrideMat;
        Material headMat = overrideMat;

        if (cubeMat == null || headMat == null)
        {
            if (Board.Instance == null || Board.Instance.ColorConfig == null) return;
            cubeMat ??= Board.Instance.ColorConfig.GetCubeColor(Color);
            headMat ??= Board.Instance.ColorConfig.GetCubeHeadColor(Color);
        }

        if (_renderers != null && cubeMat != null)
        {
            foreach (var renderer in _renderers.Values)
            {
                if (renderer == null) continue;
                renderer.sharedMaterial = cubeMat;
            }
        }

        if (_head != null && targetType == CubeType.Head && headMat != null)
        {
            _head.sharedMaterial = headMat;
        }
    }
    public void RevertType()
    {
        transform.rotation = _initRotation;
        ApplyMaterials(Type);
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
        ApplyMaterials(type);
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
        }
        else
        {
            _head.enabled = false;
        }
        ApplyMaterials(type);
        foreach (var pair in _renderers)
        {
            bool enable = pair.Key == type;
            pair.Value.enabled = enable;
        }
    }
}
