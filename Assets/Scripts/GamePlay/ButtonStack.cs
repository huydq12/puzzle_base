using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

public class ButtonStack : MonoBehaviour
{
    [ReadOnly] public List<ButtonCircle> Buttons;
    [ReadOnly] public GridCell Cell;
    [ReadOnly] public GridCell CurrentHoverCell;
    [ReadOnly] public bool CanInteraction;
    [ReadOnly] public bool IsMerging;
    [ReadOnly] public bool IsMoving;
    [ReadOnly] public bool SkipGateOnce;
    [SerializeField] private CapsuleCollider _capsuleCollider;
    [SerializeField] private Outline _outline;
    [SerializeField] private LayerMask _cellLayer;
    private Collider[] _hitBuffer = new Collider[6];
    public ButtonCircle TopButton => Buttons.Count > 0 ? Buttons[^1] : null;
    public ButtonCircle BotButton => Buttons.Count > 0 ? Buttons[0] : null;
    void Start()
    {
        CanInteraction = true;
    }
    public bool Highlight
    {
        get { return _outline.enabled; }
        set
        {
            if (value)
            {
                _outline.Renderers = new List<Renderer>(Buttons.Select(hex => hex.Renderer));
            }
            _outline.enabled = value;
        }
    }
    public void AddButton(ButtonCircle button)
    {
        Buttons ??= new();
        Buttons.Add(button);
        button.Configure(this);
        SetCollider();

        if (Board.Instance != null && Cell != null)
        {
            Board.Instance.UpdateGateOverlay(Cell.Position);
        }
    }
    public void RemoveButton(ButtonCircle button)
    {
        var removedColor = button != null ? button.Color : default;
        if (Buttons.Contains(button))
        {
            Buttons.Remove(button);
            SetCollider();
        }

        if (Board.Instance != null && Cell != null)
        {
            Board.Instance.UpdateGateOverlay(Cell.Position);
        }

        if (Buttons.Count == 0)
        {
            var clearedCell = Cell;
            if (clearedCell != null)
            {
                clearedCell.AssignStack(null);
                if (Board.Instance != null)
                {
                    Board.Instance.OnCellCleared(clearedCell, removedColor);
                }
                if (!IsMerging && Board.Instance != null)
                {
                    Board.Instance.TryActivateIncomingGates(clearedCell);
                }
            }
            Destroy(gameObject);
        }
    }
    private void SetCollider()
    {
        if (Buttons.IsNullOrEmpty()) return;

        Transform transformTarget = TopButton.transform;
        float targetLocalY = transform.InverseTransformPoint(transformTarget.position).y;

        float fixedBottomY = 0f;

        float newHeight = targetLocalY - fixedBottomY;
        float newCenterY = fixedBottomY + (newHeight / 2f);

        _capsuleCollider.center = new Vector3(0, newCenterY, 0);
        _capsuleCollider.height = newHeight;
    }
    public void UpdateCurrentCell()
    {
        GridCell closestCell = null;
        float minDistance = Mathf.Infinity;

        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, 0.5f, _hitBuffer, _cellLayer);

        for (int i = 0; i < hitCount; i++)
        {
            if (_hitBuffer[i].TryGetComponent(out GridCell cell) && !cell.IsOccupied)
            {
                if (Board.Instance != null && Board.Instance.IsCellLocked(cell))
                {
                    continue;
                }
                if (Board.Instance != null && Board.Instance.IsCellFrozen(cell))
                {
                    continue;
                }
                if (Board.Instance != null && Board.Instance.IsCellScrewed(cell))
                {
                    continue;
                }
                float dist = (transform.position - cell.transform.position).sqrMagnitude;
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestCell = cell;
                }
            }
        }

        if (closestCell != CurrentHoverCell)
        {
            if (CurrentHoverCell != null)
            {
                CurrentHoverCell.Highlight = false;
            }

            CurrentHoverCell = closestCell;

            if (CurrentHoverCell != null)
            {
                CurrentHoverCell.Highlight = true;
            }
        }
    }
    public void Configure(GridCell cell)
    {
        Cell = cell;
        if (cell != null)
        {
            transform.parent = cell.transform;
            cell.AssignStack(this);
        }
        else
        {
            transform.parent = transform.root;
        }
    }
    public void ClearCurrentCell()
    {
        if (CurrentHoverCell != null)
        {
            CurrentHoverCell.Highlight = false;
            CurrentHoverCell = null;
        }
    }
    public List<ButtonCircle> TopColors()
    {
        List<ButtonCircle> topColors = new List<ButtonCircle>();
        for (int i = Buttons.Count - 1; i >= 0; i--)
        {
            if (Buttons[i].Color == TopButton.Color)
            {
                topColors.Add(Buttons[i]);
            }
            else
            {
                break;
            }
        }
        return topColors;
    }
    public Sequence MoveToContainer()
    {
        AudioManager.Instance.PlaySFX(SFXType.CollectStack);
        VibrateManager.Instance.SmallVibrate();
        IsMoving = true;
        if (Board.Instance != null && Cell != null)
        {
            Board.Instance.OnStackClearedFromCell(Cell);
        }
        var topColors = TopColors();
        Sequence sq = DOTween.Sequence();
        for (int i = 0; i < Defines.COLLECT_COUNT; i++)
        {
            var btn = topColors[i];
            sq.Join(btn.MoveToContainer(ContainerManager.Instance.CurrentContainer));
        }
        sq.OnComplete(() =>
        {
            IsMoving = false;
        });
        return sq;
    }
    public Sequence MoveToTray()
    {
        AudioManager.Instance.PlaySFX(SFXType.CollectStack);
        VibrateManager.Instance.SmallVibrate();
        var topColors = TopColors();
        Sequence sq = DOTween.Sequence();
        int moveCount = 0;
        if (Tray.Instance != null)
        {
            moveCount = Mathf.Min(Tray.Instance.SlotRemaining, Mathf.Min(Defines.COLLECT_COUNT, topColors.Count));
        }

        if (moveCount <= 0)
        {
            IsMoving = false;
            sq.AppendCallback(() => { });
            return sq;
        }

        IsMoving = true;
        if (Board.Instance != null && Cell != null)
        {
            Board.Instance.OnStackClearedFromCell(Cell);
        }
        for (int i = 0; i < moveCount; i++)
        {
            var btn = topColors[i];
            var slot = Tray.Instance.NextEmptySlot();
            if (slot == null) break;
            slot.ButtonInSlot = btn;
            btn.transform.SetParent(slot.transform);

            Sequence btnMove = DOTween.Sequence();
            btnMove.Join(btn.transform.DOLocalRotate(new Vector3(90, 90, 0), 0.5f));
            btnMove.Join(btn.transform.DOLocalMove(Vector3.zero, 0.5f));
            btnMove.OnComplete(() => RemoveButton(btn));
            sq.Join(btnMove);
        }
        sq.OnComplete(() =>
        {
            IsMoving = false;
        });
        return sq;
    }

    public Sequence MoveToCell(GridCell targetCell)
    {
        var hexs = TopColors();
        Sequence sq = DOTween.Sequence();

        float initialY = (targetCell.Stack.Buttons.Count + 1) * Board.Instance.Spacing;

        float animDuration = 0.35f;
        float delayStep = 0.02f;
        float delay = 0f;
        for (int i = 0; i < hexs.Count; i++)
        {
            int currentIndex = i;
            ButtonCircle hex = hexs[i];

            Vector3 originalRotation = hex.transform.eulerAngles;
            Vector3 startPosition = hex.transform.position;

            float targetY = initialY + currentIndex * Board.Instance.Spacing;
            Vector3 targetWorldPosition = targetCell.Stack.transform.TransformPoint(Vector3.up * targetY);

            Vector3 worldMoveDirection = targetWorldPosition - startPosition;
            worldMoveDirection.y = 0;


            Vector3 localMoveDirection = hex.transform.InverseTransformDirection(worldMoveDirection.normalized);

            Vector3 rotationAmount = Vector3.zero;

            if (Mathf.Abs(localMoveDirection.x) > Mathf.Abs(localMoveDirection.z))
            {
                rotationAmount.z = localMoveDirection.x > 0 ? -180f : 180f;
            }
            else
            {
                rotationAmount.x = localMoveDirection.z > 0 ? 180f : -180f;
            }

            Sequence capSequence = DOTween.Sequence();

            Tween jumpTween = hex.transform.DOJump(targetWorldPosition, Mathf.Max((targetCell.Stack.Buttons.Count - Buttons.Count) * 0.25f, 1.5f), 1, animDuration);

            Tween rotateTween = hex.transform.DORotate(
                originalRotation + rotationAmount,
                animDuration,
                RotateMode.FastBeyond360
            );

            capSequence.Append(jumpTween);
            capSequence.Join(rotateTween);

            capSequence.OnComplete(() =>
            {
                AudioManager.Instance.PlaySFX(SFXType.Merge);
                targetCell.Stack.AddButton(hex);
                hex.transform.localRotation = Quaternion.identity;
                RemoveButton(hex);
            });

            sq.Insert(delay, capSequence);
            delay += delayStep;
        }
        return sq;
    }
}

