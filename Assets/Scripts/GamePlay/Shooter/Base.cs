using System;
using System.Collections.Generic;
using DG.Tweening;
using Dreamteck.Splines;
using Sirenix.OdinInspector;
using UnityEngine;

public class Base : MonoBehaviour
{
    public List<Slot> Slots;
    [SerializeField, OnValueChanged(nameof(RefreshSlots))] private Vector2 _cellSize = new Vector2(1f, 1f);
    [SerializeField, OnValueChanged(nameof(RefreshSlots))] private float _spacingX = 0.1f;
    [SerializeField] private SplinePositioner _positioner;
    [SerializeField] private LayerMask _fireLayer;
    [SerializeField] private LayerMask _routeLayer;
    [ReadOnly] public bool IsTransferring;
    [ReadOnly] public bool IsKill;

    public SplinePositioner Positioner => _positioner;
    private const int COLUMNS = 5;

    private static readonly List<Cube> _cubeBuffer = new();

    public List<Cube> GetAllCubes(bool reverse = false)
    {
        _cubeBuffer.Clear();
        if (reverse)
        {
            for (int i = Slots.Count - 1; i >= 0; i--)
            {
                if (Slots[i].IsOccupied)
                    _cubeBuffer.Add(Slots[i].CubeOnSlot);
            }
        }
        else
        {
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].IsOccupied)
                    _cubeBuffer.Add(Slots[i].CubeOnSlot);
            }
        }
        return _cubeBuffer;
    }

    public bool IsEmpty()
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            if (Slots[i].IsOccupied) return false;
        }
        return true;
    }

    public void RefreshSlots()
    {
        if (Slots == null || Slots.Count == 0) return;

        float totalWidth = COLUMNS * _cellSize.x + (COLUMNS - 1) * _spacingX;
        float startX = -totalWidth * 0.5f + _cellSize.x * 0.5f;

        for (int i = 0; i < Slots.Count; i++)
        {
            var slot = Slots[i];
            if (slot == null) continue;

            slot.transform.SetParent(transform, false);
            float x = startX + i * (_cellSize.x + _spacingX);
            slot.transform.localPosition = new Vector3(x, 0f, 0f);
        }
    }

    public Tween AddCube(Cube cube, bool immediate = true)
    {
        if (cube == null) return null;

        for (int i = 0; i < Slots.Count; i++)
        {
            if (!Slots[i].IsOccupied)
                return Slots[i].AssignCube(cube, immediate);
        }
        return null;
    }

    public void RemoveCube(Cube cube)
    {
        if (cube == null) return;

        for (int i = 0; i < Slots.Count; i++)
        {
            if (Slots[i].CubeOnSlot == cube)
            {
                Slots[i].CubeOnSlot = null;
                return;
            }
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & _fireLayer.value) != 0)
        {
            Board.Instance.CurrentMap.BasesOnFireRange.Remove(this);

        }
    }
    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & _fireLayer.value) != 0)
        {
            Board.Instance.CurrentMap.AddBaseSorted(this);
        }

        if (((1 << other.gameObject.layer) & _routeLayer.value) != 0)
        {
            if (IsKill && !IsTransferring)
            {
                var route = other.GetComponent<Route>();
                if (route == null || route.SplineQueue == null)
                {
                    return;
                }
                TransferCubesFromQueue(route.SplineQueue);
            }
        }
    }

    private void TransferCubesFromQueue(SplineComputer splineQueue)
    {
        var levelMap = Board.Instance.CurrentMap;

        if (splineQueue == null)
        {
            return;
        }

        var lane = levelMap.GetQueueLane(splineQueue);
        if (lane == null || lane.BasesQueue == null || lane.BasesQueue.Count == 0)
        {
            return;
        }

        Base firstQueueBase = lane.BasesQueue[0];

        if (firstQueueBase == null)
        {
            return;
        }

        var cubesToTransfer = firstQueueBase.GetAllCubes();

        if (cubesToTransfer.Count == 0)
        {
            levelMap.RemoveFirstBaseFromQueue(splineQueue);
            return;
        }
        IsTransferring = true;
        Sequence sq = DOTween.Sequence();
        foreach (var cube in cubesToTransfer)
        {
            if (cube == null) continue;

            firstQueueBase.RemoveCube(cube);

            sq.Join(AddCube(cube, immediate: false));
        }
        sq.OnComplete(() =>
        {
            IsTransferring = false;
            IsKill = false;
        });

        levelMap.RemoveFirstBaseFromQueue(splineQueue);
    }
}
