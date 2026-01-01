using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class Container : MonoBehaviour
{
    [SerializeField] private Renderer _renderer;
    [SerializeField] private Rigidbody _lid;
    [SerializeField] private Slot[] _slots;
    [ReadOnly] public ObjectColor Color;
    [SerializeField] private ExtraButton _extraButton;
    public int EmptySlotCount
    {
        get
        {
            if (_slots == null) return 0;
            int count = 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];
                if (slot != null && !slot.IsOccupied)
                {
                    count++;
                }
            }
            return count;
        }
    }
    public bool IsFull
    {
        get
        {
            if (_slots == null) return true;
            foreach (var slot in _slots)
            {
                if (slot != null && !slot.IsOccupied) return false;
            }
            return true;
        }
    }
    public Slot NextEmptySlot()
    {
        if (_slots == null) return null;
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null && !_slots[i].IsOccupied) return _slots[i];
        }
        return null;
    }

    public void Clear()
    {
        _extraButton.gameObject.SetActive(false);
        if (_slots == null || _slots.Length == 0)
        {
            return;
        }

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null)
            {
                continue;
            }

            if (_slots[i].IsOccupied && _slots[i].ButtonInSlot != null)
            {
                Destroy(_slots[i].ButtonInSlot.gameObject);
            }

            _slots[i].ButtonInSlot = null;
        }
    }

    public ContainerSnapshot CreateSnapshot()
    {
        ContainerSnapshot snapshot = new ContainerSnapshot();
        snapshot.Color = Color;

        snapshot.Position = transform.position;
        if (_lid != null)
        {
            snapshot.LidEulerAngles = _lid.transform.eulerAngles;
        }

        if (_slots == null)
        {
            return snapshot;
        }

        for (int i = 0; i < _slots.Length; i++)
        {
            Slot slot = _slots[i];
            if (slot == null || !slot.IsOccupied || slot.ButtonInSlot == null)
            {
                continue;
            }

            ContainerSlotSnapshot slotSnapshot = new ContainerSlotSnapshot
            {
                Index = i,
                Color = slot.ButtonInSlot.Color
            };

            snapshot.Slots.Add(slotSnapshot);
        }

        return snapshot;
    }

    public void ApplySnapshot(ContainerSnapshot snapshot)
    {
        if (snapshot == null)
        {
            Clear();
            return;
        }

        Clear();

        SetColor(snapshot.Color);

        transform.position = snapshot.Position;
        if (_lid != null)
        {
            _lid.transform.eulerAngles = snapshot.LidEulerAngles;
        }

        if (snapshot.Slots == null || _slots == null)
        {
            return;
        }

        foreach (ContainerSlotSnapshot slotSnapshot in snapshot.Slots)
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
            button.transform.localRotation = Quaternion.identity;
            button.SetColor(slotSnapshot.Color);

            slot.ButtonInSlot = button;
        }
    }
    public Tween OpenContainer(bool animation)
    {
        if (animation)
        {
            _extraButton.gameObject.SetActive(false);
            return _lid.transform.DORotate(new Vector3(125, 0, 0), 0.2f).SetEase(Ease.OutQuad);
        }
        else
        {
            _extraButton.gameObject.SetActive(false);
            _lid.transform.eulerAngles = new Vector3(125, 0, 0);
            return null;
        }
    }
    public Tween CloseContainer(bool animation)
    {
        if (animation)
        {
            _extraButton.gameObject.SetActive(true);
            return _lid.transform.DORotate(Vector3.zero, 0.2f).SetEase(Ease.InQuad);
        }
        else
        {
            _extraButton.gameObject.SetActive(false);
            _lid.transform.eulerAngles = Vector3.zero;
            return null;
        }
    }
    public void SetColor(ObjectColor color)
    {
        Color = color;
        _renderer.material = Board.Instance.ColorConfig.GetContainerByColor(color);
        if (_extraButton != null)
        {
            _extraButton.SetColor(Board.Instance.ColorConfig.GetContainerByColor(color));
        }
    }
}

[System.Serializable]
public class ContainerSlotSnapshot
{
    public int Index;
    public ObjectColor Color;
}

[System.Serializable]
public class ContainerSnapshot
{
    public ObjectColor Color;
    public List<ContainerSlotSnapshot> Slots = new List<ContainerSlotSnapshot>();
    public Vector3 Position;
    public Vector3 LidEulerAngles;
}

