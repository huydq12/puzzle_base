using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

public class GridCell : SerializedMonoBehaviour
{
    [ReadOnly] public Vector2Int Position;
    [ReadOnly] public ButtonStack Stack;
    [ReadOnly] public List<GridCell> Neighbors;
    [SerializeField] private Renderer _renderer;
    [OdinSerialize] private Dictionary<HexEdge, Transform> _hexEdges;
    [SerializeField] private Material _defaultMat;
    [SerializeField] private Material _hoverMaterial;
    [SerializeField] private Material _focusMaterial;
    [SerializeField] private Material _lockedMaterial;
    [SerializeField] private Material _lockedItemMaterial;
    [SerializeField] private List<LockedColorMaterial> _lockedColorMaterials;
    [SerializeField] private List<Material> _iceMaterials;
    [SerializeField] private List<Material> _screwMaterials;
    [ReadOnly] public bool IsLocked;
    [ReadOnly] public bool IsFrozen;
    [ReadOnly] public bool IsScrewed;
    public bool IsOccupied => Stack != null;

    private Material _activeLockedMaterial;
    private Material _activeIceMaterial;
    private Material _activeScrewMaterial;

    [Serializable]
    public class LockedColorMaterial
    {
        public ObjectColor Color;
        public Material Material;
    }

    public bool Focus
    {
        get { return _renderer.material == _focusMaterial; }
        set
        {
            if (IsLocked || IsFrozen || IsScrewed)
            {
                _renderer.material = GetActiveStateMaterial();
                return;
            }
            _renderer.material = value ? _focusMaterial : _defaultMat;
        }
    }
    public bool Highlight
    {
        get { return _renderer.material == _hoverMaterial; }
        set
        {
            if (IsLocked || IsFrozen || IsScrewed)
            {
                _renderer.material = GetActiveStateMaterial();
                return;
            }
            _renderer.material = value ? _hoverMaterial : _defaultMat;
        }
    }

    private Material GetActiveStateMaterial()
    {
        if (IsLocked)
        {
            return _activeLockedMaterial != null ? _activeLockedMaterial : (_lockedMaterial != null ? _lockedMaterial : _defaultMat);
        }
        if (IsFrozen)
        {
            return _activeIceMaterial != null ? _activeIceMaterial : _defaultMat;
        }
        if (IsScrewed)
        {
            return _activeScrewMaterial != null ? _activeScrewMaterial : _defaultMat;
        }
        return _defaultMat;
    }

    public void SetLocked(bool value)
    {
        SetLocked(value, null, null);
    }

    public void SetLocked(bool value, CellElementType? lockType, ObjectColor? lockColor)
    {
        IsLocked = value;
        if (_renderer == null) return;

        if (!IsLocked)
        {
            _activeLockedMaterial = null;
            _renderer.material = GetActiveStateMaterial();
            return;
        }

        if (lockType == CellElementType.LockItem && _lockedItemMaterial != null)
        {
            _activeLockedMaterial = _lockedItemMaterial;
            _renderer.material = _lockedItemMaterial;
            return;
        }

        if (lockType == CellElementType.LockItemColor && lockColor.HasValue && _lockedColorMaterials != null)
        {
            for (int i = 0; i < _lockedColorMaterials.Count; i++)
            {
                var e = _lockedColorMaterials[i];
                if (e != null && e.Color == lockColor.Value && e.Material != null)
                {
                    _activeLockedMaterial = e.Material;
                    _renderer.material = e.Material;
                    return;
                }
            }
        }

        _activeLockedMaterial = _lockedMaterial;
        _renderer.material = _lockedMaterial != null ? _lockedMaterial : _defaultMat;
    }

    public void SetIce(int hp)
    {
        IsFrozen = hp > 0;
        if (_renderer == null) return;

        if (!IsFrozen)
        {
            _activeIceMaterial = null;
            _renderer.material = GetActiveStateMaterial();
            return;
        }

        Material mat = null;
        if (_iceMaterials != null && _iceMaterials.Count > 0)
        {
            int index = Mathf.Clamp(hp - 1, 0, _iceMaterials.Count - 1);
            mat = _iceMaterials[index];
        }

        _activeIceMaterial = mat;
        if (!IsLocked)
        {
            _renderer.material = mat != null ? mat : _defaultMat;
        }
    }

    public void SetScrew(int hp)
    {
        IsScrewed = hp > 0;
        if (_renderer == null) return;

        if (!IsScrewed)
        {
            _activeScrewMaterial = null;
            _renderer.material = GetActiveStateMaterial();
            return;
        }

        List<Material> materials = _screwMaterials != null && _screwMaterials.Count > 0 ? _screwMaterials : _iceMaterials;

        Material mat = null;
        if (materials != null && materials.Count > 0)
        {
            int index = Mathf.Clamp(hp - 1, 0, materials.Count - 1);
            mat = materials[index];
        }

        _activeScrewMaterial = mat;
        if (!IsLocked && !IsFrozen)
        {
            _renderer.material = mat != null ? mat : _defaultMat;
        }
    }

    public void AssignStack(ButtonStack stack)
    {
        Stack = stack;

        if (Board.Instance != null)
        {
            Board.Instance.UpdateGateOverlay(Position);
        }
    }
    public void ShowHexEdges(HexEdge edgeTypes)
    {
        foreach (var edge in _hexEdges.Values)
        {
            edge.gameObject.SetActive(false);
        }
        if ((edgeTypes & HexEdge.Top) != 0) ShowHexEdge(HexEdge.Top);
        if ((edgeTypes & HexEdge.TopRight) != 0) ShowHexEdge(HexEdge.TopRight);
        if ((edgeTypes & HexEdge.BottomRight) != 0) ShowHexEdge(HexEdge.BottomRight);
        if ((edgeTypes & HexEdge.Bottom) != 0) ShowHexEdge(HexEdge.Bottom);
        if ((edgeTypes & HexEdge.BottomLeft) != 0) ShowHexEdge(HexEdge.BottomLeft);
        if ((edgeTypes & HexEdge.TopLeft) != 0) ShowHexEdge(HexEdge.TopLeft);
    }
    private void ShowHexEdge(HexEdge edgeType)
    {
        if (_hexEdges.ContainsKey(edgeType))
        {
            _hexEdges[edgeType].gameObject.SetActive(true);
        }
    }

    public void SetActiveMeshRenderer(bool value)
    {
        _renderer.enabled = value;
    }
}
[Flags]
public enum HexEdge
{
    None = 0,                   // 000000 (không có cạnh nào)
    Top = 1 << 0,               // 000001 (cạnh trên - nằm ngang)
    TopRight = 1 << 1,          // 000010 (cạnh trên phải - chéo)
    BottomRight = 1 << 2,       // 000100 (cạnh dưới phải - chéo)
    Bottom = 1 << 3,            // 001000 (cạnh dưới - nằm ngang)
    BottomLeft = 1 << 4,        // 010000 (cạnh dưới trái - chéo)
    TopLeft = 1 << 5            // 100000 (cạnh trên trái - chéo)
}
