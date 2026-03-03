using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class GateDouble : MonoBehaviour, IGate
{
    [SerializeField] private Transform _maskDoor;
    [SerializeField] private Animator _animator;
    [SerializeField] private TextMeshPro _total;

    [SerializeField] private Transform _currentShooterHolder_1;
    [SerializeField] private Transform _nextShooterHolder_1;
    [SerializeField] private Transform _queueShooterHolder_1;
    [SerializeField] private Transform _currentShooterHolder_2;
    [SerializeField] private Transform _nextShooterHolder_2;
    [SerializeField] private Transform _queueShooterHolder_2;
    [SerializeField] private ParticleSystem _collectEffect;
    [SerializeField] private ParticleSystem _closeEffect;

    [ReadOnly] public List<ShooterData> Shooters;

    private readonly List<Shooter> _lane1 = new List<Shooter>();
    private readonly List<Shooter> _lane2 = new List<Shooter>();
    private readonly HashSet<Shooter> _doneShooters = new HashSet<Shooter>();

    private int _totalValue;
    [ReadOnly] public bool IsClosed { get; private set; }

    public Transform RootTransform => transform;
    public bool IsShooterFrozen => false;
    public int RemainingShooterCount => (_lane1 != null ? _lane1.Count : 0) + (_lane2 != null ? _lane2.Count : 0);

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

    public IEnumerable<Shooter> GetCurrentShooters()
    {
        Shooter s1 = GetCurrentShooter(_lane1);
        if (s1 != null && s1.Total > 0) yield return s1;
        Shooter s2 = GetCurrentShooter(_lane2);
        if (s2 != null && s2.Total > 0) yield return s2;
    }

    public void Setup(GateData data)
    {
        OpenGate();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            $"[GateDouble] Setup gate name={name} dir={(data != null ? data.Direction : -1)} counter={(data != null ? data.Counter : 0)} shooters={(data != null && data.Shooters != null ? data.Shooters.Count : 0)}",
            this
        );
#endif
        ClearShooters();
        BuildLaneShootersFromSingleGate(data);
        Total = (_lane1.Count + _lane2.Count);
        UpdateShooterRoles();

        if (_lane1.Count == 0 && _lane2.Count == 0)
        {
            ShooterController.Instance?.RemoveGate(this);
            gameObject.SetActive(false);
        }
    }

    public void Setup(GateDataDouble data)
    {
        OpenGate();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            $"[GateDouble] Setup(double) gate name={name} dir={(data != null ? data.Direction : -1)} counter={(data != null ? data.Counter : 0)} shootersDouble={(data != null && data.ShootersDouble != null ? data.ShootersDouble.Count : 0)}",
            this
        );
#endif
        ClearShooters();
        BuildLaneShootersFromDoubleGate(data);
        Total = (_lane1.Count + _lane2.Count);
        UpdateShooterRoles();

        if (_lane1.Count == 0 && _lane2.Count == 0)
        {
            ShooterController.Instance?.RemoveGate(this);
            gameObject.SetActive(false);
        }
    }

    private static ShooterData CloneShooterData(ShooterData src)
    {
        if (src == null) return null;
        return new ShooterData
        {
            Color = src.Color,
            Counter = src.Counter,
            Type = src.Type,
            TieID = src.TieID
        };
    }

    private void BuildLaneShootersFromSingleGate(GateData data)
    {
        Shooters = data != null && data.Shooters != null ? new List<ShooterData>(data.Shooters) : new List<ShooterData>();

        if (Shooters == null || Shooters.Count == 0) return;
        for (int i = 0; i < Shooters.Count; i++)
        {
            ShooterData src = Shooters[i];
            ShooterData clone = CloneShooterData(src);
            if (clone == null) continue;
            Shooter shoot = InstantiateShooter(clone);
            if (shoot != null) _lane1.Add(shoot);
        }
    }

    private void BuildLaneShootersFromDoubleGate(GateDataDouble data)
    {
        Shooters = new List<ShooterData>();
        if (data == null || data.ShootersDouble == null) return;

        for (int i = 0; i < data.ShootersDouble.Count; i++)
        {
            ShooterDataDouble entry = data.ShootersDouble[i];
            if (entry == null) continue;

            if (entry.ShootersLeft != null)
            {
                for (int l = 0; l < entry.ShootersLeft.Count; l++)
                {
                    ShooterData clone = CloneShooterData(entry.ShootersLeft[l]);
                    if (clone == null) continue;
                    Shooters.Add(clone);
                    Shooter shoot = InstantiateShooter(clone);
                    if (shoot != null) _lane1.Add(shoot);
                }
            }

            if (entry.ShootersRight != null)
            {
                for (int r = 0; r < entry.ShootersRight.Count; r++)
                {
                    ShooterData clone = CloneShooterData(entry.ShootersRight[r]);
                    if (clone == null) continue;
                    Shooters.Add(clone);
                    Shooter shoot = InstantiateShooter(clone);
                    if (shoot != null) _lane2.Add(shoot);
                }
            }
        }
    }

    private Shooter InstantiateShooter(ShooterData data)
    {
        if (ShooterController.Instance == null || ShooterController.Instance.ShooterPrefab == null) return null;
        Shooter shoot = Instantiate(ShooterController.Instance.ShooterPrefab);
        if (shoot == null) return null;
        shoot.ResetForReuse();
        shoot.SetColor(data.Color);
        shoot.SetType(data.Type);
        shoot.SetTieId(data.TieID);
        shoot.Total = data.Counter;
        shoot.Gate = this;
        return shoot;
    }

    private Transform GetHolderForRole(int lane, ShooterRole role)
    {
        if (lane == 0)
        {
            if (role == ShooterRole.Current) return _currentShooterHolder_1;
            if (role == ShooterRole.Next) return _nextShooterHolder_1;
            return _queueShooterHolder_1;
        }
        else
        {
            if (role == ShooterRole.Current) return _currentShooterHolder_2;
            if (role == ShooterRole.Next) return _nextShooterHolder_2;
            return _queueShooterHolder_2;
        }
    }

    private void ApplyRoleToShooter(Shooter shooter, ShooterRole role, int lane)
    {
        if (shooter == null) return;
        Transform holder = GetHolderForRole(lane, role);
        if (holder != null)
        {
            shooter.transform.SetParent(holder, false);
            shooter.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            shooter.transform.localPosition = Vector3.zero;
        }
        shooter.SetRole(role);
        if (shooter.Total <= 0) shooter.CanShoot = false;
    }

    private void UpdateShooterRoles()
    {
        if (IsClosed) return;

        TryAdvanceCurrentShooterIfPossible(playCollectEffect: false);

        if ((_lane1 == null || _lane1.Count == 0) && (_lane2 == null || _lane2.Count == 0))
        {
            CloseGate();
            return;
        }

        ApplyLaneRoles(_lane1, 0);
        ApplyLaneRoles(_lane2, 1);
    }

    private void ApplyLaneRoles(List<Shooter> lane, int laneIndex)
    {
        if (lane == null) return;
        for (int i = 0; i < lane.Count; i++)
        {
            Shooter shooter = lane[i];
            if (shooter == null) continue;
            ShooterRole role = i == 0 ? ShooterRole.Current : (i == 1 ? ShooterRole.Next : ShooterRole.Queue);
            ApplyRoleToShooter(shooter, role, laneIndex);
        }
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

        if (_maskDoor != null)
            _maskDoor.gameObject.SetActive(true);

        if (_animator != null)
            _animator.Play("Tunnel_TurnOff_", 0, 0f);

        ShooterController.Instance?.NotifyGateClosed(this);
    }

    [Button]
    public void OpenGate()
    {
        IsClosed = false;
        if (_closeEffect != null)
            _closeEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (_maskDoor != null)
            _maskDoor.gameObject.SetActive(false);
    }

    public void ConsumeIceCounter(int amount)
    {
        // GateDouble doesn't use ice shooter.
    }

    public bool UnlockCurrentLock(Key key = null)
    {
        return false;
    }

    public bool UnlockNextLock(Key key = null)
    {
        return false;
    }

    public int ReduceNonCurrentShooterTotal(ObjectColor color, int amount, bool rainbowOnly = false)
    {
        if (amount <= 0) return amount;
        int remaining = amount;

        remaining = ReduceNonCurrentInLane(_lane1, color, remaining, rainbowOnly);
        remaining = ReduceNonCurrentInLane(_lane2, color, remaining, rainbowOnly);

        return remaining;
    }

    public int ReduceCurrentShooterTotal(ObjectColor color, int amount, bool rainbowOnly = false)
    {
        if (amount <= 0) return amount;

        int remaining = amount;
        remaining = ReduceShooterTotal(GetCurrentShooter(_lane1), color, remaining, rainbowOnly);
        remaining = ReduceShooterTotal(GetCurrentShooter(_lane2), color, remaining, rainbowOnly);

        if (TryAdvanceCurrentShooterIfPossible(playCollectEffect: true))
            UpdateShooterRoles();

        return remaining;
    }

    private int ReduceNonCurrentInLane(List<Shooter> lane, ObjectColor color, int amount, bool rainbowOnly)
    {
        if (amount <= 0) return amount;
        if (lane == null || lane.Count <= 1) return amount;

        int remaining = amount;
        for (int i = lane.Count - 1; i >= 1; i--)
        {
            if (remaining <= 0) break;
            remaining = ReduceShooterTotal(lane[i], color, remaining, rainbowOnly);
        }
        return remaining;
    }

    private int ReduceShooterTotal(Shooter shooter, ObjectColor color, int amount, bool rainbowOnly)
    {
        if (amount <= 0) return amount;
        if (shooter == null || shooter.Total <= 0) return amount;

        bool matches = rainbowOnly ? shooter.IsRainbow : (shooter.Color == color);
        if (!matches) return amount;

        int reduceAmount = Mathf.Min(shooter.Total, amount);
        shooter.Total -= reduceAmount;
        int remaining = amount - reduceAmount;
        if (shooter.Total <= 0)
        {
            MarkShooterDoneIfNeeded(shooter);
        }
        return remaining;
    }

    public bool ShuffleRemainingShooters()
    {
        if (IsClosed) return false;

        bool changed = false;
        changed |= ShuffleUpcomingInLane(_lane1);
        changed |= ShuffleUpcomingInLane(_lane2);

        if (changed)
            UpdateShooterRoles();

        return changed;
    }

    private static bool ShuffleUpcomingInLane(List<Shooter> lane)
    {
        if (lane == null) return false;
        if (lane.Count <= 2) return false; // nothing meaningful to shuffle

        List<Shooter> upcoming = lane.GetRange(1, lane.Count - 1);
        upcoming.Shuffle();
        for (int i = 1; i < lane.Count; i++)
        {
            lane[i] = upcoming[i - 1];
        }
        return true;
    }

    public void CollectCurrentShooter()
    {
        if (IsClosed) return;
        if (!TryAdvanceCurrentShooterIfPossible(playCollectEffect: true)) return;
        UpdateShooterRoles();
    }

    private static Shooter GetCurrentShooter(List<Shooter> lane)
    {
        if (lane == null || lane.Count == 0) return null;
        return lane[0];
    }

    private bool TryAdvanceCurrentShooterIfPossible(bool playCollectEffect)
    {
        if (IsClosed) return false;

        bool removedAny = false;
        while (true)
        {
            Shooter s1 = GetCurrentShooter(_lane1);
            Shooter s2 = GetCurrentShooter(_lane2);

            bool done1 = s1 == null || s1.Total <= 0;
            bool done2 = s2 == null || s2.Total <= 0;

            if (!done1 && !done2) break;

            bool isTiePair = IsTiePair(s1, s2);
            if (isTiePair)
            {
                if (done1 && done2)
                {
                    removedAny |= RemoveCurrentFromLane(_lane1);
                    removedAny |= RemoveCurrentFromLane(_lane2);
                    continue;
                }
                break;
            }

            bool removedThisLoop = false;
            if (done1)
            {
                removedAny |= RemoveCurrentFromLane(_lane1);
                removedThisLoop = true;
            }

            if (done2)
            {
                removedAny |= RemoveCurrentFromLane(_lane2);
                removedThisLoop = true;
            }

            if (!removedThisLoop) break;
        }

        if (removedAny && playCollectEffect && _collectEffect != null)
        {
            _collectEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _collectEffect.Play();
        }

        if ((_lane1 == null || _lane1.Count == 0) && (_lane2 == null || _lane2.Count == 0))
        {
            CloseGate();
        }

        return removedAny;
    }

    private static bool IsTiePair(Shooter s1, Shooter s2)
    {
        if (s1 == null || s2 == null) return false;
        if (s1.Type != 6 || s2.Type != 6) return false;
        if (s1.TieID == -1 || s2.TieID == -1) return false;
        return s1.TieID == s2.TieID;
    }

    private bool RemoveCurrentFromLane(List<Shooter> lane)
    {
        if (lane == null || lane.Count == 0) return false;
        Shooter shooter = lane[0];
        lane.RemoveAt(0);
        MarkShooterDoneIfNeeded(shooter);
        DestroyShooter(shooter);
        return true;
    }

    private void DestroyShooter(Shooter shooter, bool immediate = false)
    {
        if (shooter == null) return;
        _doneShooters.Remove(shooter);
        Board.Instance?.NotifyShooterDisappeared(shooter, "GateDouble.RemoveGroup");
        if (immediate)
        {
            Destroy(shooter.gameObject);
            return;
        }

        shooter.transform.DOScale(Vector3.zero, 0.2f).OnComplete(() =>
        {
            if (shooter != null) Destroy(shooter.gameObject);
        });
    }

    private void MarkShooterDoneIfNeeded(Shooter shooter)
    {
        if (shooter == null) return;
        if (shooter.Total > 0) return;
        if (_doneShooters.Add(shooter))
        {
            Total = Mathf.Max(0, Total - 1);
        }
    }

    private void ClearShooters()
    {
        if (_lane1.Count > 0)
        {
            for (int i = 0; i < _lane1.Count; i++)
            {
                DestroyShooter(_lane1[i], immediate: true);
            }
        }

        if (_lane2.Count > 0)
        {
            for (int i = 0; i < _lane2.Count; i++)
            {
                DestroyShooter(_lane2[i], immediate: true);
            }
        }

        _lane1.Clear();
        _lane2.Clear();
        _doneShooters.Clear();
        Total = 0;
    }
}
