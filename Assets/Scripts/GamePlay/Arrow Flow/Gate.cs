using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class Gate : MonoBehaviour
    , IGate
{
    private const int FallbackRainbowShooterCounter = 20;

    // [SerializeField] private Transform _maskDoor;
    [SerializeField] private TextMeshPro _total;
    [SerializeField] private Transform _tunnel;
    [SerializeField] private Transform _belt;
    [SerializeField] private Transform _door;
    [SerializeField] private Transform _currentShooterHolder;
    [SerializeField] private Transform _nextShooterHolder;
    [SerializeField] private Transform _queueShooterHolder;
    [SerializeField] private ParticleSystem _collectEffect;
    [SerializeField] private ParticleSystem _closeEffect;
    [SerializeField] private IceShooter _iceShooterPrefab;
    [ReadOnly] public List<ShooterData> Shooters;
    private List<Shooter> _shooterInstances = new List<Shooter>();
    public Shooter CurrentShooter { get; private set; }
    private Shooter NextShooter { get; set; }
    private Shooter QueueShooter { get; set; }
    private int _currentShooterIndex = 0;

    private struct ShuffleShooterEntry
    {
        public int SourceIndex;
        public ShooterData Data;
    }
    private int _totalValue;
    [ReadOnly] public bool IsClosed { get; private set; }
    private bool _isSingleShooterMode = false;
    private IceShooter _iceShooter;
    private readonly List<Lock> _lockInstances = new List<Lock>();
    private readonly List<Lock> _locksByIndex = new List<Lock>();

    public bool IsShooterFrozen => _iceShooter != null && _iceShooter.Counter > 0;

    public Transform RootTransform => transform;

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

    public IEnumerable<Shooter> GetCurrentShooters()
    {
        if (CurrentShooter != null) yield return CurrentShooter;
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
            UpdateLockVisuals();
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

        UpdateLockVisuals();
        UpdateIceShooterAttachment();
    }

    public void Setup(GateData data)
    {
        OpenGate();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        UnityEngine.Debug.Log(
            $"[Gate] Setup gate name={name} dir={(data != null ? data.Direction : -1)} counter={(data != null ? data.Counter : 0)} shooters={(data != null && data.Shooters != null ? data.Shooters.Count : 0)}",
            this
        );
#endif
        ClearLocks();
        if (data == null || data.Shooters == null)
        {
            Total = 0;
            Shooters = new List<ShooterData>();
        }
        else
        {
            Shooters = new List<ShooterData>(data.Shooters);
            Total = Shooters.Count;
        }

        // If a gate is configured with no shooters, keep the gate and spawn a default rainbow shooter.
        // This prevents "empty" gates in level configs from being removed at runtime.
        if (Shooters == null)
            Shooters = new List<ShooterData>();
        if (Shooters.Count == 0)
        {
            Shooters.Add(new ShooterData
            {
                Color = ObjectColor.Red,
                Counter = FallbackRainbowShooterCounter,
                Type = Shooter.RainbowType,
                TieID = Shooter.FallbackRainbowShooterTieId
            });
            Total = Shooters.Count;
        }

        _shooterInstances.Clear();
        _locksByIndex.Clear();
        _currentShooterIndex = 0;

        // Check if single shooter mode (only 1 shooter from the start)
        _isSingleShooterMode = Shooters.Count == 1;
        if (_isSingleShooterMode)
        {
            _tunnel.gameObject.SetActive(false);
            _door.gameObject.SetActive(false);
            _total.enabled = false;
            _belt.localPosition = new Vector3(0f, -0.175f, -2.25f);
            _belt.localScale = new Vector3(0.5f, 0.67f , 0.67f);
        }
        else
        {
            _tunnel.gameObject.SetActive(true);
            _total.enabled = true;
            _belt.localPosition = new Vector3(0f, -0.175f, -2.75f);
        }

	        for (int i = 0; i < Shooters.Count; i++)
	        {
                if (IsLockShooter(Shooters[i]))
                {
                    _shooterInstances.Add(null);
                    _locksByIndex.Add(SpawnLockAtIndex(i));
                    continue;
                }

                _locksByIndex.Add(null);
	            if (ShooterController.Instance == null || ShooterController.Instance.ShooterPrefab == null)
                {
                    _shooterInstances.Add(null);
                    continue;
                }
	            Shooter shoot = Instantiate(ShooterController.Instance.ShooterPrefab);
	            if (shoot == null)
                {
                    _shooterInstances.Add(null);
                    continue;
                }
	            shoot.ResetForReuse();
	            shoot.transform.SetParent(GetShooterHolderByIndex(i), false);
	            shoot.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
	            shoot.SetSize(i == 0 ? 0.75f : 0.65f);
            shoot.transform.localPosition = Vector3.zero;
            shoot.SetColor(Shooters[i].Color);
            shoot.SetType(Shooters[i].Type);
            shoot.SetTieId(Shooters[i].TieID);
            shoot.Total = Shooters[i].Counter;
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
            if (_shooterInstances[i] == null) continue;
            _shooterInstances[i].ShowTotal = i == 0;
        }
        UpdateShooterRoles();

        int iceCounter = data != null ? data.Counter : 0;
        InitializeIceShooter(iceCounter);

        // Note: we no longer remove/deactivate empty gates because we ensure a fallback shooter above.
    }

    public bool TryActivateFallbackShooter(Shooter shooter)
    {
        if (shooter == null) return false;
        if (IsClosed) return false;
        if (shooter.Gate != this) return false;
        if (shooter.Type != Shooter.RainbowType) return false;
        if (shooter.TieID != Shooter.FallbackRainbowShooterTieId) return false;
        if (shooter.Total <= 0) return false;

        shooter.CanShoot = true;
        return true;
    }

    public bool ShuffleRemainingShooters()
    {
        if (!CanShuffleUpcomingShooters()) return false;
        if (Shooters == null || _shooterInstances == null) return false;
        if (_currentShooterIndex < 0 || _currentShooterIndex >= Shooters.Count) return false;

        int startIndex = _currentShooterIndex;
        int remaining = Shooters.Count - startIndex;
        if (remaining <= 1) return false;

        var indices = new List<int>(remaining);
        for (int i = startIndex; i < Shooters.Count; i++)
        {
            if (IsLockIndex(i)) continue;
            indices.Add(i);
        }

        if (indices.Count <= 1) return false;

        var movable = new List<ShuffleShooterEntry>(indices.Count);
        for (int i = 0; i < indices.Count; i++)
        {
            int idx = indices[i];
            ShooterData src = Shooters[idx];
            Shooter inst = idx >= 0 && idx < _shooterInstances.Count ? _shooterInstances[idx] : null;
            int currentTotal = inst != null ? inst.Total : (src != null ? src.Counter : 0);

            movable.Add(new ShuffleShooterEntry
            {
                SourceIndex = idx,
                Data = CloneShooterData(src, currentTotal)
            });
        }

        movable.Shuffle();

        // Ensure the current shooter changes (when possible).
        if (movable.Count > 1 && movable[0].SourceIndex == _currentShooterIndex)
        {
            (movable[0], movable[1]) = (movable[1], movable[0]);
        }

        for (int i = 0; i < indices.Count; i++)
        {
            int idx = indices[i];
            ShooterData data = movable[i].Data;
            Shooters[idx] = data;

            Shooter inst = idx >= 0 && idx < _shooterInstances.Count ? _shooterInstances[idx] : null;
            if (inst == null) continue;
            inst.SetColor(data.Color);
            inst.SetType(data.Type);
            inst.SetTieId(data.TieID);
            inst.Total = data.Counter;
        }

        UpdateShooterRoles();
        return true;
    }

    private static ShooterData CloneShooterData(ShooterData src, int counter)
    {
        if (src == null) return null;
        return new ShooterData
        {
            Color = src.Color,
            Counter = counter,
            Type = src.Type,
            TieID = src.TieID
        };
    }

    public bool CanShuffleUpcomingShooters()
    {
        if (IsClosed) return false;
        if (Shooters == null) return false;

        int startIndex = _currentShooterIndex;
        if (startIndex < 0 || startIndex >= Shooters.Count) return false;

        int movable = 0;
        for (int i = startIndex; i < Shooters.Count; i++)
        {
            if (IsLockIndex(i)) continue;
            movable++;
            if (movable > 1) return true;
        }

        return false;
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
            // _maskDoor.gameObject.SetActive(true);
            Sequence sq = DOTween.Sequence();
            sq.Append(_belt.DOScaleX(0.18f, 0.25f));
            // sq.Append(_maskDoor.DOLocalMoveY(-1.25f, 0.2f));
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
        // _maskDoor.gameObject.SetActive(false);
        _belt.transform.localScale = new Vector3(0.62f, 0.67f , 0.67f);
        // _maskDoor.localPosition = new Vector3(0, 0.3f, -2.5f);
    }

    private Transform GetShooterHolderByIndex(int index)
    {
        if (index == 0) return _currentShooterHolder;
        if (index == 1) return _nextShooterHolder;
        if (index == 2) return _queueShooterHolder;

        return _queueShooterHolder;
    }

    public void ConsumeIceCounter(int amount)
    {
        if (_iceShooter == null) return;
        _iceShooter.Consume(amount);
    }

    private void InitializeIceShooter(int counter)
    {
        if (counter <= 0)
        {
            if (_iceShooter != null)
                _iceShooter.SetCounter(0);
            return;
        }

        if (_iceShooter == null && _iceShooterPrefab != null)
            _iceShooter = Instantiate(_iceShooterPrefab);

        if (_iceShooter == null) return;
        UpdateIceShooterAttachment();
        _iceShooter.SetCounter(counter);
    }

    private void UpdateIceShooterAttachment()
    {
        if (_iceShooter == null) return;
        Transform target = CurrentShooter != null ? CurrentShooter.transform : _currentShooterHolder;
        if (target == null) target = transform;
        _iceShooter.transform.SetParent(transform, false);
        _iceShooter.transform.localPosition = Vector3.zero;
        _iceShooter.transform.localRotation = Quaternion.identity;
    }

    private static bool IsLockShooter(ShooterData data)
    {
        return data != null && data.Color == ObjectColor.None && data.Type == 10;
    }

    private bool IsLockIndex(int index)
    {
        return index >= 0 && Shooters != null && index < Shooters.Count && IsLockShooter(Shooters[index]);
    }

    private void UpdateLockVisuals()
    {
        if (_locksByIndex == null || _locksByIndex.Count == 0) return;

        for (int i = 0; i < _locksByIndex.Count; i++)
        {
            Lock lockObj = _locksByIndex[i];
            if (lockObj == null) continue;
            if (lockObj.IsUnlocking) continue;

            int offset = i - _currentShooterIndex;
            if (offset < 0 || offset > 2)
            {
                lockObj.gameObject.SetActive(false);
                continue;
            }

            Transform holder = GetShooterHolderByIndex(offset);
            if (holder != null)
            {
                lockObj.transform.SetParent(holder, false);
                lockObj.transform.localPosition = Vector3.zero;
                lockObj.transform.localRotation = Quaternion.identity;
            }

            lockObj.gameObject.SetActive(true);
        }
    }

    public bool UnlockCurrentLock(Key key = null)
    {
        if (!IsLockIndex(_currentShooterIndex)) return false;
        return BeginUnlockLockAtIndex(_currentShooterIndex, key);
    }

    public bool UnlockNextLock(Key key = null)
    {
        int lockIndex = FindNextLockIndex(_currentShooterIndex);
        if (lockIndex < 0) return false;
        return BeginUnlockLockAtIndex(lockIndex, key);
    }

    private int FindNextLockIndex(int startIndex)
    {
        if (Shooters == null || Shooters.Count == 0) return -1;
        int start = Mathf.Max(0, startIndex);
        for (int i = start; i < Shooters.Count; i++)
        {
            if (IsLockIndex(i)) return i;
        }
        return -1;
    }

    private bool BeginUnlockLockAtIndex(int index, Key key)
    {
        if (index < 0 || index >= Shooters.Count) return false;

        Lock lockObj = (index < _locksByIndex.Count) ? _locksByIndex[index] : null;
        if (lockObj == null)
        {
            RemoveLockAtIndex(index);
            return true;
        }

        if (key == null)
        {
            RemoveLockAtIndex(index);
            return true;
        }

        if (!lockObj.TryBeginUnlock(key, () => RemoveLock(lockObj)))
            return false;

        return true;
    }

    private void RemoveLock(Lock lockObj)
    {
        if (lockObj == null) return;
        int index = _locksByIndex.IndexOf(lockObj);
        if (index < 0)
        {
            if (lockObj != null && lockObj.gameObject != null)
                Destroy(lockObj.gameObject);
            return;
        }
        RemoveLockAtIndex(index);
    }

    private void RemoveLockAtIndex(int index)
    {
        if (index < 0 || index >= Shooters.Count) return;
        Lock lockObj = (index < _locksByIndex.Count) ? _locksByIndex[index] : null;
        if (lockObj != null)
        {
            _lockInstances.Remove(lockObj);
            Destroy(lockObj.gameObject);
        }

        Shooters.RemoveAt(index);
        _locksByIndex.RemoveAt(index);
        if (index < _shooterInstances.Count)
            _shooterInstances.RemoveAt(index);

        Total = Mathf.Max(0, Total - 1);

        if (index < _currentShooterIndex)
            _currentShooterIndex = Mathf.Max(0, _currentShooterIndex - 1);

        if (_currentShooterIndex >= _shooterInstances.Count)
        {
            CurrentShooter = null;
            NextShooter = null;
            QueueShooter = null;
            CloseGate();
            return;
        }

        UpdateShooterRoles();
        RearrangeShooterPositions();
    }

    private Lock SpawnLockAtIndex(int index)
    {
        if (ShooterController.Instance == null || ShooterController.Instance.LockPrefab == null) return null;

        Lock lockObj = Instantiate(ShooterController.Instance.LockPrefab);
        if (lockObj == null) return null;

        Transform parent = transform;
        lockObj.transform.SetParent(parent, false);
        lockObj.transform.localPosition = Vector3.zero;
        lockObj.transform.localRotation = Quaternion.identity;
        _lockInstances.Add(lockObj);
        return lockObj;
    }

    private void ClearLocks()
    {
        if (_lockInstances.Count == 0)
        {
            _locksByIndex.Clear();
            return;
        }
        for (int i = 0; i < _lockInstances.Count; i++)
        {
            if (_lockInstances[i] != null)
                Destroy(_lockInstances[i].gameObject);
        }
        _lockInstances.Clear();
        _locksByIndex.Clear();
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

            // Check if shooter matches: either same color, or rainbow-only mode
            bool matches = rainbowOnly ? shooter.IsRainbow : (shooter.Color == color);
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

        // Check if shooter matches: either same color, or rainbow-only mode
        bool matches = rainbowOnly ? CurrentShooter.IsRainbow : (CurrentShooter.Color == color);
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

        Board.Instance?.NotifyShooterDisappeared(shooter, "RemoveShooterFromQueue");

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
        if (IsLockIndex(_currentShooterIndex)) return;
        if (_currentShooterIndex > _shooterInstances.Count - 1)
        {
            return;
        }

        var prevCurrent = CurrentShooter;
        var prevNext = NextShooter;
        var prevQueue = QueueShooter;

        Board.Instance?.NotifyShooterDisappeared(prevCurrent, "CollectCurrentShooter");
        AudioManager.Instance.PlaySFX(SFXType.CollectShooter);
        _collectEffect.Stop();
        _collectEffect.Play();

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
                    if (dataIdx < Shooters.Count && !IsLockIndex(dataIdx))
                    {
                        prevCurrent.gameObject.SetActive(true);
                        prevCurrent.SetColor(Shooters[dataIdx].Color);
                        prevCurrent.SetType(Shooters[dataIdx].Type);
                        prevCurrent.Total = Shooters[dataIdx].Counter;
                    }
                    else if (dataIdx < Shooters.Count)
                    {
                        prevCurrent.gameObject.SetActive(false);
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
