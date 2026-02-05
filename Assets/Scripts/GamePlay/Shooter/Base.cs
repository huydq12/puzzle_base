using System.Collections.Generic;
using System.Linq;
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
    [SerializeField] private Collider _collider;

    public bool CanTrigger
    {
        get => _collider.enabled;
        set => _collider.enabled = value;
    }

    public SplinePositioner Positioner => _positioner;
    private const int COLUMNS = 5;

    // Helper để lấy tất cả cubes hiện có
    public List<Cube> GetAllCubes()
    {
        return Slots.Where(s => s.IsOccupied).Select(s => s.CubeOnSlot).ToList();
    }

    public bool IsEmpty()
    {
        return !Slots.Any(s => s.IsOccupied);
    }

    public bool IsFull()
    {
        return Slots.All(s => s.IsOccupied);
    }

    public int GetCubeCount()
    {
        return Slots.Count(s => s.IsOccupied);
    }

    public void DestroyLine()
    {
        var cubes = GetAllCubes();
        if (cubes.Count == 0) return;

        // CanTrigger = false;
        Sequence sq = DOTween.Sequence();
        float delay = 0f;

        for (int i = cubes.Count - 1; i >= 0; i--)
        {
            Cube cube = cubes[i];
            if (cube == null) continue;

            sq.Insert(delay += 0.1f, cube.Destroy());
        }

        sq.OnComplete(() =>
        {
            // Clear tất cả slots
            foreach (var slot in Slots)
            {
                slot.CubeOnSlot = null;
            }
        });
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

    public void AddCube(Cube cube, bool immediate = true)
    {
        if (cube == null) return;

        var emptySlot = Slots.FirstOrDefault(slot => !slot.IsOccupied);
        if (emptySlot != null)
        {
            emptySlot.AssignCube(cube, immediate);
        }
        else
        {
            Debug.LogWarning("Không còn slot trống!");
        }
    }

    public void RemoveCube(Cube cube)
    {
        if (cube == null) return;

        // Tìm slot chứa cube này
        var slot = Slots.FirstOrDefault(s => s.CubeOnSlot == cube);
        if (slot != null)
        {
            slot.CubeOnSlot = null;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & _fireLayer.value) != 0)
        {
            var cubes = GetAllCubes();
            if (cubes.Count == 0) return;

            var firstCube = cubes[0];
            var holder = Board.Instance.CurrentMap.Holders.FirstOrDefault(h => h.IsOccupied &&
                               h.ShooterOnholder.Color == firstCube.Color &&
                               !h.ShooterOnholder.IsMoving && h.ShooterOnholder.Remaining > 0);

            if (holder != null)
            {
                holder.ShooterOnholder.Shoot(this);
                DestroyLine();
            }
        }

        if (((1 << other.gameObject.layer) & _routeLayer.value) != 0)
        {
            if (IsEmpty() && !_isTransferring)
            {
                _isTransferring = true;
                var route = other.GetComponent<Route>() ?? other.GetComponentInParent<Route>();
                if (route == null || route.SplineQueue == null)
                {
                    _isTransferring = false;
                    return;
                }
                TransferCubesFromQueue(route.SplineQueue);

                DOVirtual.DelayedCall(1f, () => _isTransferring = false);
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

        // Lấy base đầu tiên trong queue
        Base firstQueueBase = lane.BasesQueue[0];

        if (firstQueueBase == null)
        {
            return;
        }

        // Lấy tất cả cubes từ base queue
        var cubesToTransfer = firstQueueBase.GetAllCubes();

        if (cubesToTransfer.Count == 0)
        {
            levelMap.RemoveFirstBaseFromQueue(splineQueue);
            return;
        }

        // Transfer từng cube sang base hiện tại
        foreach (var cube in cubesToTransfer)
        {
            if (cube == null) continue;

            // Xóa cube khỏi slot của queue base
            firstQueueBase.RemoveCube(cube);

            // Setup lại cube cho base mới (có thể cần update color, parent, etc.)
            // cube.SetUp(cube.Color, this); // Nếu cần

            // Gán vào slot của base hiện tại với animation
            AddCube(cube, immediate: false);
        }

        // Sau khi transfer xong, xóa base queue đầu tiên và đẩy các base khác lên
        levelMap.RemoveFirstBaseFromQueue(splineQueue);
    }
    private bool _isTransferring = false; // Flag để tránh trigger nhiều lần

}
