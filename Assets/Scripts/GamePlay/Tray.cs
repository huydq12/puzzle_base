using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class Tray : Singleton<Tray>
{
    [SerializeField, OnValueChanged(nameof(SetPositionSlot))]
    private float _spacingSlot;
    [SerializeField] private Slot[] _slots;
    [SerializeField] private Transform _container;
    [ReadOnly] public int TotalSlot;

    [SerializeField, Range(0f, 1f)] private float _warningThreshold = 0.75f;

    public Slot[] Slots => _slots;

    public int SlotRemaining
    {
        get
        {
            int occupied = 0;
            for (int i = 0; i < TotalSlot && i < _slots.Length; i++)
            {
                if (_slots[i].IsOccupied)
                    occupied++;
            }
            return TotalSlot - occupied;
        }
    }

    public Slot NextEmptySlot()
    {
        for (int i = 0; i < TotalSlot && i < _slots.Length; i++)
        {
            if (!_slots[i].IsOccupied) return _slots[i];
        }
        return null;
    }

    public void CheckAndUpdateWarnings()
    {
        if (TotalSlot <= 0 || _slots == null)
        {
            if (_slots != null)
            {
                for (int i = 0; i < _slots.Length; i++)
                {
                    if (_slots[i] != null)
                    {
                        _slots[i].IsWarning = false;
                    }
                }
            }
            return;
        }
        int occupiedCount = 0;
        for (int i = 0; i < TotalSlot && i < _slots.Length; i++)
        {
            if (_slots[i].IsOccupied)
            {
                occupiedCount++;
            }
        }

        float occupiedRatio = (float)occupiedCount / TotalSlot;
        bool shouldShowWarning = occupiedRatio >= _warningThreshold;

        for (int i = 0; i < TotalSlot && i < _slots.Length; i++)
        {
            _slots[i].IsWarning = shouldShowWarning;
        }
    }


    public void Clear()
    {
        StopAllCoroutines();

        foreach (var slot in _slots)
        {
            if (slot.IsOccupied)
            {
                Destroy(slot.ButtonInSlot.gameObject);
            }
            slot.ButtonInSlot = null;
            slot.IsWarning = false;
        }
    }

    private void SetPositionSlot()
    {
        if (_slots == null || _slots.Length == 0) return;

        float totalWidth = (_slots.Length - 1) * _spacingSlot;
        float startX = -totalWidth / 2f;
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null)
            {
                Vector3 pos = _slots[i].transform.localPosition;
                pos.x = startX + i * _spacingSlot;
                _slots[i].transform.localPosition = pos;
            }
        }
    }

    public void Setup(int totalSlot, bool animation)
    {
        TotalSlot = Mathf.Clamp(totalSlot, 0, _slots.Length);
        for (int i = TotalSlot; i < _slots.Length; i++)
        {
            if (_slots[i] != null)
            {
                if (_slots[i].IsOccupied && _slots[i].ButtonInSlot != null)
                {
                    Destroy(_slots[i].ButtonInSlot.gameObject);
                }
                _slots[i].ButtonInSlot = null;
                _slots[i].IsWarning = false;
            }
        }
        for (int i = TotalSlot; i < _slots.Length; i++)
        {
            _slots[i].gameObject.SetActive(false);
        }
        for (int i = 0; i < TotalSlot; i++)
        {
            _slots[i].gameObject.SetActive(true);
        }
        Vector3 targetContainer = _container.transform.localPosition.With(x: _spacingSlot * (40 - TotalSlot) - 0.8f);
        if (animation)
        {
            _container.transform.DOLocalMove(targetContainer, 0.5f);
            transform.DOMoveX(-targetContainer.x / 2.0f - 0.5f, 0.5f);
        }
        else
        {
            _container.transform.localPosition = targetContainer;
            transform.position = transform.position.With(x: -targetContainer.x / 2.0f - 0.5f);
        }
    }

    public void AddSlots(int addCount, bool animation)
    {
        if (_slots == null || _slots.Length == 0 || addCount <= 0)
        {
            return;
        }

        int newTotal = Mathf.Clamp(TotalSlot + addCount, 0, _slots.Length);

        if (newTotal == TotalSlot)
        {
            return;
        }

        Setup(newTotal, animation);
        CheckAndUpdateWarnings();
    }

    public IEnumerator TryCollectButtons()
    {
        if (GameManagerInGame.Instance.CurrentGameStateInGame == GameStateInGame.Result)
        {
            yield break;
        }
        var container = ContainerManager.Instance != null ? ContainerManager.Instance.CurrentContainer : null;
        var containerColor = container != null ? container.Color : default;
        List<GridCell> listCells = new List<GridCell>();
        foreach (var cell in Board.Instance.Cells)
        {
            if (cell == null || !cell.IsOccupied) continue;
            if (Board.Instance != null && Board.Instance.IsCellLocked(cell)) continue;
            if (Board.Instance != null && Board.Instance.IsCellFrozen(cell)) continue;
            if (Board.Instance != null && Board.Instance.IsCellScrewed(cell)) continue;
            if (cell.Stack.TopColors().Count >= Defines.COLLECT_COUNT)
            {
                if (cell.Stack.IsMerging)
                {
                    continue;
                }
                if (SlotRemaining <= 0)
                {
                    break;
                }
                listCells.Add(cell);
                yield return cell.Stack.MoveToTray().WaitForCompletion();

                int matchNow = 0;
                for (int i = 0; i < TotalSlot && i < _slots.Length; i++)
                {
                    var slot = _slots[i];
                    if (slot != null && slot.IsOccupied && slot.ButtonInSlot != null && slot.ButtonInSlot.Color == containerColor)
                    {
                        matchNow++;
                    }
                }
                if (matchNow >= Defines.COLLECT_COUNT)
                {
                    break;
                }
            }
        }

        int trayMatchCountAfterMove = 0;
        for (int i = 0; i < TotalSlot && i < _slots.Length; i++)
        {
            var slot = _slots[i];
            if (slot != null && slot.IsOccupied && slot.ButtonInSlot != null && slot.ButtonInSlot.Color == containerColor)
            {
                trayMatchCountAfterMove++;
            }
        }
        if (trayMatchCountAfterMove >= Defines.COLLECT_COUNT && ContainerManager.Instance != null)
        {
            ContainerManager.Instance.RequestTryCollect();
        }

        CheckAndUpdateWarnings();
        if (SlotRemaining == 0)
        {
            bool hasContainerColorInTray = false;
            for (int i = 0; i < TotalSlot && i < _slots.Length; i++)
            {
                var slot = _slots[i];
                if (slot != null && slot.IsOccupied && slot.ButtonInSlot != null && slot.ButtonInSlot.Color == containerColor)
                {
                    hasContainerColorInTray = true;
                    break;
                }
            }

            if (!hasContainerColorInTray)
            {
                int boardOccupied = 0;
                int boardButtons = 0;
                if (Board.Instance != null && Board.Instance.Cells != null)
                {
                    foreach (var c in Board.Instance.Cells)
                    {
                        if (c == null || !c.IsOccupied || c.Stack == null || c.Stack.Buttons == null) continue;
                        boardOccupied++;
                        boardButtons += c.Stack.Buttons.Count;
                    }
                }

                int trayOccupied = TotalSlot - SlotRemaining;
                int trayTotal = TotalSlot;
                int trayMatchCount = 0;
                for (int i = 0; i < TotalSlot && i < _slots.Length; i++)
                {
                    var slot = _slots[i];
                    if (slot != null && slot.IsOccupied && slot.ButtonInSlot != null && slot.ButtonInSlot.Color == containerColor)
                    {
                        trayMatchCount++;
                    }
                }

                // int containerTotal = ContainerManager.Instance != null && ContainerManager.Instance.Containers != null ? ContainerManager.Instance.Containers.Count : 0;
                // Debug.Log($"[LoseCheck] reason=TrayFullNoContainerColor boardOccupied={boardOccupied} boardButtons={boardButtons} trayOccupied={trayOccupied}/{trayTotal} trayMatchCount={trayMatchCount} containerColor={containerColor} currentIndex={ContainerManager.Instance._currentIndex} containerTotal={containerTotal}");

                GameManagerInGame.Instance.SetLose();
                // UIManager.Instance.ShowLoseScreen();
                // UIManager.Instance.CloseSettingsPopup();
                
                GameUI.Instance.Get<UITopInGame>().ShowTrayNotificationLose(1.2f, "Tray is full!");
                
                DOVirtual.DelayedCall(2f, () =>
                {
                    GameUI.Instance.Get<UILose>().Show();
                });
            }
            else
            {
                int trayMatchCount = 0;
                for (int i = 0; i < TotalSlot && i < _slots.Length; i++)
                {
                    var slot = _slots[i];
                    if (slot != null && slot.IsOccupied && slot.ButtonInSlot != null && slot.ButtonInSlot.Color == containerColor)
                    {
                        trayMatchCount++;
                    }
                }

                if (trayMatchCount >= Defines.COLLECT_COUNT && ContainerManager.Instance != null)
                {
                    ContainerManager.Instance.RequestTryCollect();
                }
                else
                {
                    yield return null;
                    if (GameController.Instance != null && !GameController.Instance.IsAnimating)
                    {
                        GameController.Instance.CheckLoseByNoValidMove();
                    }
                }
            }
        }
        else
        {
            yield return null;
            foreach (var c in listCells)
            {
                if (c.IsOccupied)
                {
                    GameController.Instance.StackPlaceCallBack(c);
                }
            }
        }
    }

    public TraySnapshot CreateSnapshot()
    {
        TraySnapshot snapshot = new TraySnapshot();
        snapshot.TotalSlot = TotalSlot;

        if (_slots == null)
        {
            return snapshot;
        }

        for (int i = 0; i < TotalSlot && i < _slots.Length; i++)
        {
            Slot slot = _slots[i];
            if (slot == null || !slot.IsOccupied || slot.ButtonInSlot == null)
            {
                continue;
            }

            TraySlotSnapshot slotSnapshot = new TraySlotSnapshot
            {
                Index = i,
                Color = slot.ButtonInSlot.Color
            };

            snapshot.Slots.Add(slotSnapshot);
        }

        return snapshot;
    }

    public void ApplySnapshot(TraySnapshot snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        Clear();

        Setup(snapshot.TotalSlot, false);

        if (snapshot.Slots == null || _slots == null)
        {
            CheckAndUpdateWarnings();
            return;
        }

        foreach (TraySlotSnapshot slotSnapshot in snapshot.Slots)
        {
            if (slotSnapshot.Index < 0 || slotSnapshot.Index >= _slots.Length)
            {
                continue;
            }

            Slot slot = _slots[slotSnapshot.Index];
            if (slot == null)
            {
                continue;
            }

            if (slot.IsOccupied && slot.ButtonInSlot != null)
            {
                Destroy(slot.ButtonInSlot.gameObject);
                slot.ButtonInSlot = null;
            }

            ButtonCircle button = Instantiate(Board.Instance.ButtonPrefab, slot.transform);
            button.transform.localPosition = Vector3.zero;
            button.transform.localRotation = Quaternion.Euler(90f, 90f, 0f);
            button.SetColor(slotSnapshot.Color);

            slot.ButtonInSlot = button;
        }

        CheckAndUpdateWarnings();
    }
}

[System.Serializable]
public class TraySlotSnapshot
{
    public int Index;
    public ObjectColor Color;
}

[System.Serializable]
public class TraySnapshot
{
    public int TotalSlot;
    public List<TraySlotSnapshot> Slots = new List<TraySlotSnapshot>();
}