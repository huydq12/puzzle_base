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
    [OdinSerialize] private Dictionary<CubeType, List<Renderer>> _renderers;
    private Quaternion _initRotation;

    public void AssignToBase(Base baseTarget)
    {
        if(baseTarget == null)
        {
            Base availableBase = FindClosestAvailableBase();
            if (availableBase != null)
            {
                availableBase.AssignCube(this);
            }
            else
            {
                Debug.LogWarning($"Không tìm thấy base trống cho cube {name}");
            }
        }
        else
        {
            baseTarget.AssignCube(this);
        }
    }

    public Base FindClosestAvailableBase()
    {
        float radius = 0.5f;
        int mask = LayerMask.GetMask("Base");

        Collider[] hits = Physics.OverlapSphere(transform.position, radius, mask);

        Base closestEmpty = null;
        float minDistEmpty = float.MaxValue;

        foreach (var hit in hits)
        {
            Base b = hit.GetComponent<Base>();
            if (b == null) continue;

            float d = Vector3.Distance(transform.position, b.transform.position);

            // Ưu tiên base trống
            if (!b.IsOccupied)
            {
                if (d < minDistEmpty)
                {
                    minDistEmpty = d;
                    closestEmpty = b;
                }
            }
        }

        // Ưu tiên base trống trước
        if (closestEmpty != null)
        {
            return closestEmpty;
        }

        return null;
    }

    public Base FindClosestBaseForConveyorInsertion()
    {
        float radius = 0.5f;
        int mask = LayerMask.GetMask("Base");

        Collider[] hits = Physics.OverlapSphere(transform.position, radius, mask);

        Base closestEmpty = null;
        Base closestOccupiedOtherLine = null;
        float minDistEmpty = float.MaxValue;
        float minDistOccupied = float.MaxValue;

        foreach (var hit in hits)
        {
            Base b = hit.GetComponent<Base>();
            if (b == null) continue;

            float d = Vector3.Distance(transform.position, b.transform.position);

            if (!b.IsOccupied)
            {
                if (d < minDistEmpty)
                {
                    minDistEmpty = d;
                    closestEmpty = b;
                }
                continue;
            }

            if (b.CubeOnBase != null && b.CubeOnBase.Line != this.Line)
            {
                if (d < minDistOccupied)
                {
                    minDistOccupied = d;
                    closestOccupiedOtherLine = b;
                }
            }
        }

        if (closestEmpty != null) return closestEmpty;
        return closestOccupiedOtherLine;
    }

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