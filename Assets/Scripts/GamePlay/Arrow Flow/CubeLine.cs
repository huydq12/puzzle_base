
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
    [SerializeField] private ParticleSystem _meltEffect;
    [OdinSerialize] private Dictionary<CubeType, Renderer> _renderers;
    [SerializeField] private Renderer _head;
    [SerializeField] private Renderer _doubleCube;
    [SerializeField] private Renderer _doubleHeadCube;
    [SerializeField] private Outline _outline;
    [SerializeField] private Material _materialElementType2;

    private Quaternion _initRotation;
    private bool _elementType3Revealed;
    private ObjectColor _baseColor;
    private ObjectColor _originalColor;

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

        if (ElementType == 3 && !_elementType3Revealed)
        {
            _elementType3Revealed = true;
            if (!TryGetElementType3ShiftedColorFromOriginal(offset: 3, out ObjectColor shifted))
                shifted = _originalColor;
            _baseColor = shifted;
            RefreshColorAndMaterials(Type);

            SpawnHitEffect();

            return;
        }

        ConveyorController.Instance.RemoveCubeFromPath(this);
        SpawnHitEffect();
        transform.DOScale(0f, 0.1f).OnComplete(() => Destroy(gameObject));
    }

    private void SpawnHitEffect()
    {
        if (_hitEffect == null) return;

        if (PoolManager.Instance == null) return;

        ParticleSystem ps = PoolManager.Instance.Get(_hitEffect, transform.position);

        if (ps == null) return;

        ps.transform.rotation = Quaternion.identity;
        ps.Play(true);

        var main = ps.main;
        float lifetime = 0f;
        if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
            lifetime = main.startLifetime.constantMax;
        else
            lifetime = main.startLifetime.constant;

        float delay = Mathf.Max(0.1f, main.duration + lifetime);
        if (PoolManager.Instance != null)
        {
            DOVirtual.DelayedCall(delay, () =>
            {
                if (ps != null)
                    ps.gameObject.SetActive(false);
            });
        }
    }
    public void SetColor(ObjectColor color)
    {
        _originalColor = color;
        _baseColor = color;
        RefreshColorAndMaterials(Type);
    }

    public void SetElementType(int elementType)
    {
        _elementType3Revealed = false;
        ElementType = elementType;
        RefreshColorAndMaterials(Type);
        Line?.RefreshCounterText();
    }
    public void PlayEffectMelt()
    {
        _meltEffect.Play();
    }

    private Material GetElementTypeMaterial()
    {
        if (ElementType == 2) return _materialElementType2;
        return null;
    }

    private static int GetObjectColorCount()
    {
        return System.Enum.GetValues(typeof(ObjectColor)).Length;
    }

    private static readonly ObjectColor[] ElementType3ColorIndexOrder =
    {
        ObjectColor.Green,
        ObjectColor.Blue,
        ObjectColor.Red,
        ObjectColor.Purple,
        ObjectColor.Pink,
        ObjectColor.Yellow,
        ObjectColor.Orange,
        ObjectColor.Cyan,
        ObjectColor.Brown,
        ObjectColor.Teal,
    };

    private bool TryGetElementType3ShiftedColorFromOriginal(int offset, out ObjectColor shiftedColor)
    {
        int baseIdx = System.Array.IndexOf(ElementType3ColorIndexOrder, _originalColor);
        if (baseIdx < 0)
        {
            shiftedColor = _originalColor;
            return false;
        }

        int idx = baseIdx - offset;
        if (idx < 0 || idx >= ElementType3ColorIndexOrder.Length)
        {
            shiftedColor = _originalColor;
            return false;
        }

        shiftedColor = ElementType3ColorIndexOrder[idx];
        return true;
    }

    private void RefreshColorAndMaterials(CubeType targetType)
    {
        Color = _baseColor;
        ApplyMaterials(targetType);
    }

    private void ApplyMaterials(CubeType targetType)
    {
        if (ElementType == 3)
        {
            ApplyElementType3Materials(targetType);
            return;
        }

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

        if (_doubleCube != null) _doubleCube.gameObject.SetActive(false);
        if (_doubleHeadCube != null) _doubleHeadCube.gameObject.SetActive(false);
    }

    private void ApplyElementType3Materials(CubeType targetType)
    {
        if (Board.Instance == null || Board.Instance.ColorConfig == null) return;

        Material outerCubeMat = Board.Instance.ColorConfig.GetCubeColor(_baseColor);
        Material outerHeadMat = Board.Instance.ColorConfig.GetCubeHeadColor(_baseColor);

        ObjectColor overlayColor = _originalColor;
        if (!TryGetElementType3ShiftedColorFromOriginal(offset: 3, out overlayColor))
            overlayColor = _originalColor;

        Material overlayCubeMat = Board.Instance.ColorConfig.GetCubeColor(overlayColor);
        Material overlayHeadMat = Board.Instance.ColorConfig.GetCubeHeadColor(overlayColor);

        if (_renderers != null && outerCubeMat != null)
        {
            foreach (var renderer in _renderers.Values)
            {
                if (renderer == null) continue;
                renderer.sharedMaterial = outerCubeMat;
            }
        }

        if (_head != null && targetType == CubeType.Head && outerHeadMat != null)
        {
            _head.sharedMaterial = outerHeadMat;
        }

        if (_doubleCube != null)
        {
            if (!_elementType3Revealed && overlayCubeMat != null)
                _doubleCube.sharedMaterial = overlayCubeMat;
            _doubleCube.gameObject.SetActive(!_elementType3Revealed && targetType != CubeType.Head);
        }

        if (_doubleHeadCube != null)
        {
            if (!_elementType3Revealed && targetType == CubeType.Head)
            {
                if (overlayHeadMat != null)
                    _doubleHeadCube.sharedMaterial = overlayHeadMat;
                _doubleHeadCube.gameObject.SetActive(true);
            }
            else
            {
                _doubleHeadCube.gameObject.SetActive(false);
            }
        }
    }
    public void RevertType()
    {
        transform.rotation = _initRotation;
        RefreshColorAndMaterials(Type);
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
        RefreshColorAndMaterials(type);
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
        RefreshColorAndMaterials(type);
        foreach (var pair in _renderers)
        {
            bool enable = pair.Key == type;
            pair.Value.enabled = enable;
        }
    }
}
