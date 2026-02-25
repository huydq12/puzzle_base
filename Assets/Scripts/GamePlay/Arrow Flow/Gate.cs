using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class Gate : MonoBehaviour
{
    [SerializeField] private Transform _maskDoor;
    [SerializeField] private TextMeshPro _total;
    [SerializeField] private Transform _tunnel;
    [SerializeField] private Transform _belt;
    [SerializeField] private Transform _door;
    [SerializeField] private Transform _currentShooterHolder;
    [SerializeField] private Transform _nextShooterHolder;
    [SerializeField] private Transform _queueShooterHolder;
    [SerializeField] private ParticleSystem _collectEffect;
    [SerializeField] private ParticleSystem _closeEffect;
    [ReadOnly] public List<ShooterData> Shooters;
    private List<Shooter> _shooterInstances = new List<Shooter>();
    public Shooter CurrentShooter { get; private set; }
    private Shooter NextShooter { get; set; }
    private Shooter QueueShooter { get; set; }
    private int _currentShooterIndex = 0;
    private int _totalValue;
    [ReadOnly] public bool IsClosed { get; private set; }
    private bool _isSingleShooterMode = false;

    public int RemainingShooterCount
    {
        get
        {
            if (_shooterInstances == null) return 0;
            if (_currentShooterIndex < 0) return 0;
            if (_currentShooterIndex >= _shooterInstances.Count) return 0;
            return _shooterInstances.Count - _currentShooterIndex;
        }
    }

    public int Total
    {
        get => _totalValue;
        set
        {
            _totalValue = value;
            if (_total != null)
                _total.text = _totalValue.ToString();
        }
    }
    private void UpdateShooterRoles()
    {
        if (IsClosed || _shooterInstances == null || _currentShooterIndex >= _shooterInstances.Count)
        {
            CurrentShooter = null;
            NextShooter = null;
            QueueShooter = null;
            return;
        }

        CurrentShooter = (_currentShooterIndex < _shooterInstances.Count) ? _shooterInstances[_currentShooterIndex] : null;
        NextShooter = (_currentShooterIndex + 1 < _shooterInstances.Count) ? _shooterInstances[_currentShooterIndex + 1] : null;
        QueueShooter = (_currentShooterIndex + 2 < _shooterInstances.Count) ? _shooterInstances[_currentShooterIndex + 2] : null;

        if (CurrentShooter != null)
            CurrentShooter.SetRole(ShooterRole.Current);

        if (NextShooter != null)
            NextShooter.SetRole(ShooterRole.Next);

        for (int i = _currentShooterIndex + 2; i < _shooterInstances.Count; i++)
        {
            Shooter shooter = _shooterInstances[i];
            if (shooter == null) continue;
            shooter.SetRole(ShooterRole.Queue);
        }
    }

    public void Setup(List<ShooterData> datas)
    {
        OpenGate();
        Total = datas.Count;
        Shooters = new List<ShooterData>(datas);
        _shooterInstances.Clear();
        _currentShooterIndex = 0;

        // Check if single shooter mode (only 1 shooter from the start)
        _isSingleShooterMode = datas.Count == 1;
        if (_isSingleShooterMode)
        {
            _tunnel.gameObject.SetActive(false);
            _door.gameObject.SetActive(false);
            _total.enabled = false;
            _belt.localPosition = new Vector3(0f, -0.175f, -2.25f);
        }
        else
        {
            _tunnel.gameObject.SetActive(true);
            _total.enabled = true;
            _belt.localPosition = new Vector3(0f, -0.175f, -2.75f);
        }

	        for (int i = 0; i < datas.Count; i++)
	        {
	            if (ShooterController.Instance == null) continue;
	            if (ShooterController.Instance.ShooterPrefab == null) continue;
	            Shooter shoot = Instantiate(ShooterController.Instance.ShooterPrefab);
	            if (shoot == null) continue;
	            shoot.ResetForReuse();
	            shoot.transform.SetParent(GetShooterHolderByIndex(i), false);
	            shoot.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
	            shoot.SetSize(i == 0 ? 0.75f : 0.65f);
            shoot.transform.localPosition = Vector3.zero;
            shoot.SetColor(datas[i].Color);
            shoot.SetType(datas[i].Type);
            shoot.Total = datas[i].Counter;
            shoot.Gate = this;
            _shooterInstances.Add(shoot);
        }

        if (_shooterInstances.Count > 0)
            CurrentShooter = _shooterInstances[0];
        if (_shooterInstances.Count > 1)
            NextShooter = _shooterInstances[1];
        if (_shooterInstances.Count > 2)
            QueueShooter = _shooterInstances[2];

        for (int i = 0; i < _shooterInstances.Count; i++)
        {
            _shooterInstances[i].ShowTotal = i == 0;
        }
        UpdateShooterRoles();
    }

    public bool ShuffleRemainingShooters()
    {
        if (IsClosed) return false;
        if (Shooters == null || _shooterInstances == null) return false;
        if (_currentShooterIndex < 0 || _currentShooterIndex >= Shooters.Count) return false;

        int remaining = Shooters.Count - _currentShooterIndex;
        if (remaining <= 1) return false;

        var slice = new List<ShooterData>(remaining);
        for (int i = _currentShooterIndex; i < Shooters.Count; i++)
        {
            slice.Add(Shooters[i]);
        }

        slice.Shuffle();

        for (int i = 0; i < slice.Count; i++)
        {
            Shooters[_currentShooterIndex + i] = slice[i];
            Shooter inst = _shooterInstances[_currentShooterIndex + i];
            if (inst == null) continue;
            inst.SetColor(slice[i].Color);
            inst.SetType(slice[i].Type);
            inst.Total = slice[i].Counter;
        }

        UpdateShooterRoles();
        return true;
    }

    [Button]
    public void CloseGate()
    {
        IsClosed = true;
        if (_closeEffect != null)
        {
            _closeEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _closeEffect.Play();
        }

        if (_isSingleShooterMode)
        {
            // Single shooter mode: just scale belt to 0, no door animation
            _belt.DOScale(Vector3.zero, 0.5f).OnComplete(() =>
            {
                ShooterController.Instance?.NotifyGateClosed(this);
            });
        }
        else
        {
            _total.enabled = false;
            _door.gameObject.SetActive(true);
            _maskDoor.gameObject.SetActive(true);
            Sequence sq = DOTween.Sequence();
            sq.Append(_belt.DOScaleX(0.18f, 0.25f));
            sq.Append(_maskDoor.DOLocalMoveY(-1.25f, 0.2f));
            sq.AppendCallback(() => ShooterController.Instance?.NotifyGateClosed(this));
        }
    }
    [Button]
    public void OpenGate()
    {
        IsClosed = false;
        if (_closeEffect != null)
            _closeEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _total.enabled = true;
        _door.gameObject.SetActive(false);
        _maskDoor.gameObject.SetActive(false);
        _belt.transform.localScale = new Vector3(0.55f, 0.67f , 0.67f);
        _maskDoor.localPosition = new Vector3(0, 0.3f, -2.5f);
    }

    private Transform GetShooterHolderByIndex(int index)
    {
        if (index == 0) return _currentShooterHolder;
        if (index == 1) return _nextShooterHolder;
        if (index == 2) return _queueShooterHolder;

        return _queueShooterHolder;
    }


    public int ReduceNonCurrentShooterTotal(ObjectColor color, int amount, bool rainbowOnly = false)
    {
        if (amount <= 0 || _shooterInstances == null) return amount;

        int remaining = amount;

        // Start from the end of queue (furthest from current)
        for (int i = _shooterInstances.Count - 1; i > _currentShooterIndex; i--)
        {
            if (remaining <= 0) break;

            Shooter shooter = _shooterInstances[i];
            if (shooter == null || shooter.Total <= 0) continue;

            // Check if shooter matches: either same color, or rainbow mode checking Type == 6
            bool matches = rainbowOnly ? (shooter.Type == 6) : (shooter.Color == color);
            if (!matches) continue;

            int reduceAmount = Mathf.Min(shooter.Total, remaining);
            shooter.Total -= reduceAmount;
            remaining -= reduceAmount;

            if (shooter.Total <= 0)
            {
                RemoveShooterFromQueue(i);
            }
        }

        return remaining;
    }

    public int ReduceCurrentShooterTotal(ObjectColor color, int amount, bool rainbowOnly = false)
    {
        if (amount <= 0 || CurrentShooter == null) return amount;
        if (CurrentShooter.Total <= 0) return amount;

        // Check if shooter matches: either same color, or rainbow mode checking Type == 6
        bool matches = rainbowOnly ? (CurrentShooter.Type == 6) : (CurrentShooter.Color == color);
        if (!matches) return amount;

        int reduceAmount = Mathf.Min(CurrentShooter.Total, amount);
        CurrentShooter.Total -= reduceAmount;
        int remaining = amount - reduceAmount;

        if (CurrentShooter.Total <= 0)
        {
            CollectCurrentShooter();
        }

        return remaining;
    }

    private void RemoveShooterFromQueue(int index)
    {
        if (index <= _currentShooterIndex || index >= _shooterInstances.Count) return;

        Shooter shooter = _shooterInstances[index];
        if (shooter == null) return;

        _shooterInstances.RemoveAt(index);
        Shooters.RemoveAt(index);
        Total = Mathf.Max(0, Total - 1);

        Board.Instance?.NotifyShooterDisappeared();

	        shooter.transform.DOScale(Vector3.zero, 0.2f).OnComplete(() =>
	        {
	            if (shooter != null) Destroy(shooter.gameObject);
	        });

        // Update NextShooter and QueueShooter references
        NextShooter = (_currentShooterIndex + 1 < _shooterInstances.Count) ? _shooterInstances[_currentShooterIndex + 1] : null;
        QueueShooter = (_currentShooterIndex + 2 < _shooterInstances.Count) ? _shooterInstances[_currentShooterIndex + 2] : null;

        UpdateShooterRoles();
        RearrangeShooterPositions();
    }

    private void RearrangeShooterPositions()
    {
        for (int i = _currentShooterIndex; i < _shooterInstances.Count && i < _currentShooterIndex + 3; i++)
        {
            Shooter shooter = _shooterInstances[i];
            if (shooter == null) continue;

            Transform holder = GetShooterHolderByIndex(i - _currentShooterIndex);
            if (shooter.transform.parent != holder)
            {
                shooter.transform.SetParent(holder, false);
                shooter.transform.DOLocalMove(Vector3.zero, 0.2f);
            }
        }
    }

    [Button]
    public void CollectCurrentShooter()
    {
        if (_currentShooterIndex > _shooterInstances.Count - 1)
        {
            return;
        }

        Board.Instance?.NotifyShooterDisappeared();
        AudioManager.Instance.PlaySFX(SFXType.CollectShooter);
        _collectEffect.Stop();
        _collectEffect.Play();
        var prevCurrent = CurrentShooter;
        var prevNext = NextShooter;
        var prevQueue = QueueShooter;

        if (prevCurrent != null)
            prevCurrent.ShowTotal = false;

        bool isLastShooter = _currentShooterIndex >= _shooterInstances.Count - 1;

        if (isLastShooter)
        {
            if (prevCurrent != null)
                prevCurrent.CanShoot = false;
            if (prevNext != null)
                prevNext.CanShoot = false;
            if (prevQueue != null)
                prevQueue.CanShoot = false;
        }

        Total = Mathf.Max(0, Shooters.Count - (_currentShooterIndex + 1));
        Sequence seq = DOTween.Sequence();
        if (prevCurrent != null)
        {
            seq.Append(prevCurrent.transform.DOScale(Vector3.zero, 0.25f));
            if (isLastShooter)
            {
                seq.Join(prevCurrent.transform.DORotate(new Vector3(0, 180, 0), 0.5f, RotateMode.LocalAxisAdd));
            }
            seq.AppendCallback(() =>
            {
                if (isLastShooter)
                {
                    CloseGate();
                    CurrentShooter = null;
                    NextShooter = null;
                    QueueShooter = null;
                }
                else
                {
                    prevCurrent.transform.SetParent(_queueShooterHolder, false);
                    prevCurrent.transform.localPosition = Vector3.zero;
                    prevCurrent.transform.localScale = 0.75f * Vector3.one;
                    prevCurrent.SetRole(ShooterRole.Queue);
                    int dataIdx = _currentShooterIndex + 2;
                    if (dataIdx < Shooters.Count)
                    {
                        prevCurrent.SetColor(Shooters[dataIdx].Color);
                        prevCurrent.SetType(Shooters[dataIdx].Type);
                        prevCurrent.Total = Shooters[dataIdx].Counter;
                    }
                }
            });
        }

        if (!isLastShooter)
        {
            seq.AppendCallback(() =>
            {
                if (prevNext != null)
                {
                    prevNext.transform.SetParent(_currentShooterHolder);
                    prevNext.transform.DOLocalMove(Vector3.zero, 0.25f);
                    prevNext.transform.DOScale(0.75f * Vector3.one, 0.25f);
                }
                if (prevQueue != null)
                {
                    prevQueue.transform.SetParent(_nextShooterHolder);
                    prevQueue.transform.DOLocalMove(Vector3.zero, 0.25f);
                }
            });
            seq.AppendInterval(0.25f);

            seq.AppendCallback(() =>
            {
                _currentShooterIndex++;
                if (_currentShooterIndex < _shooterInstances.Count)
                    CurrentShooter = _shooterInstances[_currentShooterIndex];
                else
                    CurrentShooter = null;
                if (_currentShooterIndex + 1 < _shooterInstances.Count)
                    NextShooter = _shooterInstances[_currentShooterIndex + 1];
                else
                    NextShooter = null;
                if (_currentShooterIndex + 2 < _shooterInstances.Count)
                    QueueShooter = _shooterInstances[_currentShooterIndex + 2];
                else
                    QueueShooter = null;
                UpdateShooterRoles();
            });
        }
        else
        {
            seq.AppendCallback(() =>
            {
                UpdateShooterRoles();
            });
        }
    }
}
