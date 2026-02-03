using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Dreamteck.Splines;
using Sirenix.OdinInspector;
using UnityEngine;

public class Base : MonoBehaviour
{
    [ReadOnly] public List<Cube> Cubes = new();
    [SerializeField, OnValueChanged(nameof(RefreshGrid))] private Vector2 _cellSize = new Vector2(1f, 1f);

    [SerializeField, OnValueChanged(nameof(RefreshGrid))] private float _spacingX = 0.1f;
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
    public void DestroyLine()
    {
        if (Cubes.Count == 0) return;
        CanTrigger = false;
        Sequence sq = DOTween.Sequence();
        float delay = 0f;

        for (int i = Cubes.Count - 1; i >= 0; i--)
        {
            Cube cube = Cubes[i];
            if (cube == null) continue;

            sq.Insert(delay += 0.1f, cube.Destroy());
        }

        sq.OnComplete(() =>
        {
            Cubes.Clear();
        });
    }
    public void RefreshGrid()
    {
        int count = Mathf.Min(Cubes.Count, COLUMNS);

        float totalWidth = COLUMNS * _cellSize.x + (COLUMNS - 1) * _spacingX;

        float startX = -totalWidth * 0.5f + _cellSize.x * 0.5f;

        for (int i = 0; i < count; i++)
        {
            var cube = Cubes[i];
            if (cube == null) continue;

            cube.transform.SetParent(transform, false);

            float x = startX + i * (_cellSize.x + _spacingX);

            cube.transform.localPosition = new Vector3(x, 0f, 0f);
        }
    }

    public void AddCube(Cube cube)
    {
        if (cube == null || Cubes.Contains(cube)) return;

        Cubes.Add(cube);
        RefreshGrid();
    }

    public void RemoveCube(Cube cube)
    {
        if (cube == null) return;

        if (Cubes.Remove(cube))
            RefreshGrid();
    }
    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & _fireLayer.value) != 0)
        {
            var holder = Board.Instance.CurrentMap.Holders.FirstOrDefault(b => b.IsOccupied && b.ShooterOnholder.Color == Cubes[0].Color && !b.ShooterOnholder.IsMoving);
            if (holder != null)
            {
                holder.ShooterOnholder.Shoot(this);
                DestroyLine();
            }
        }

        // Khi Base rỗng va chạm với route layer → lấy Cubes từ queue
        if (((1 << other.gameObject.layer) & _routeLayer.value) != 0)
        {
            if (Cubes.Count == 0)
            {
                TransferCubesFromQueue();
            }
        }
    }

    private void TransferCubesFromQueue()
    {
        var map = Board.Instance.CurrentMap;
        if (map.BasesQueue == null || map.BasesQueue.Count == 0) return;

        // Lấy Base đầu tiên trong queue
        Base queueBase = map.BasesQueue[0];
        if (queueBase.Cubes.Count == 0) return;

        // Chuyển tất cả Cubes từ queueBase sang Base này
        var cubesToTransfer = new List<Cube>(queueBase.Cubes);

        Sequence sq = DOTween.Sequence();
        float delay = 0f;

        foreach (var cube in cubesToTransfer)
        {
            cube.Base = this;
            Cubes.Add(cube);

            // Animate cube di chuyển sang Base mới
            sq.Insert(delay, cube.transform.DOMove(transform.position, 0.3f).SetEase(Ease.OutQuad));
            delay += 0.05f;
        }

        queueBase.Cubes.Clear();

        sq.OnComplete(() =>
        {
            RefreshGrid();

            // Xóa Base đầu tiên khỏi queue
            map.BasesQueue.RemoveAt(0);
            Destroy(queueBase.gameObject);
        });
    }
}
