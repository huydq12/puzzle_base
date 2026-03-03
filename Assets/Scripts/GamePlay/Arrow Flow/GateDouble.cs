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

    private class ShooterGroup
    {
        public int TieId;
        public ShooterData Data1;
        public ShooterData Data2;
        public Shooter Shooter1;
        public Shooter Shooter2;
    }

    private readonly List<ShooterGroup> _groups = new List<ShooterGroup>();
    private readonly Dictionary<Shooter, ShooterGroup> _groupByShooter = new Dictionary<Shooter, ShooterGroup>();
    private readonly HashSet<Shooter> _doneShooters = new HashSet<Shooter>();

    private int _totalValue;
    [ReadOnly] public bool IsClosed { get; private set; }

    public Transform RootTransform => transform;
    public bool IsShooterFrozen => false;
    public int RemainingShooterCount => _groups != null ? _groups.Count : 0;

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
        if (_groups == null || _groups.Count == 0) yield break;
        var group = _groups[0];
        if (group.Shooter1 != null) yield return group.Shooter1;
        if (group.Shooter2 != null) yield return group.Shooter2;
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
        Shooters = BuildShooterListForSingleGate(data);
        BuildGroupsFromShooters(Shooters);
        SpawnShooters();
        Total = Shooters != null ? Shooters.Count : 0;
        UpdateShooterRoles();
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
        Shooters = BuildShooterListForDoubleGate(data);
        BuildGroupsFromShooters(Shooters);
        SpawnShooters();
        Total = Shooters != null ? Shooters.Count : 0;
        UpdateShooterRoles();
    }

    private static ShooterData CloneShooterData(ShooterData src, int overrideTieId)
    {
        if (src == null) return null;
        return new ShooterData
        {
            Color = src.Color,
            Counter = src.Counter,
            Type = src.Type,
            TieID = overrideTieId
        };
    }

    private static List<ShooterData> BuildShooterListForSingleGate(GateData data)
    {
        if (data == null) return new List<ShooterData>();

        return data.Shooters != null ? new List<ShooterData>(data.Shooters) : new List<ShooterData>();
    }

    private static List<ShooterData> BuildShooterListForDoubleGate(GateDataDouble data)
    {
        if (data == null) return new List<ShooterData>();
        return BuildShooterListForDoubleGateEntries(data.ShootersDouble);
    }

    private static List<ShooterData> BuildShooterListForDoubleGateEntries(List<ShooterDataDouble> entries)
    {
        if (entries == null || entries.Count == 0) return new List<ShooterData>();

        var result = new List<ShooterData>();
        int autoTieBase = 100000;

        for (int i = 0; i < entries.Count; i++)
        {
            ShooterDataDouble entry = entries[i];
            if (entry == null) continue;

            List<ShooterData> left = entry.ShootersLeft;
            List<ShooterData> right = entry.ShootersRight;

            // If both sides have at least one shooter and both use TieID=-1, auto-tie first pair.
            int overrideTieLeft0 = -1;
            int overrideTieRight0 = -1;
            if (left != null && left.Count > 0 && right != null && right.Count > 0)
            {
                ShooterData l0 = left[0];
                ShooterData r0 = right[0];
                if (l0 != null && r0 != null && l0.TieID == -1 && r0.TieID == -1)
                {
                    int tie = autoTieBase + i;
                    overrideTieLeft0 = tie;
                    overrideTieRight0 = tie;
                }
            }

            if (left != null)
            {
                for (int l = 0; l < left.Count; l++)
                {
                    ShooterData src = left[l];
                    int tie = (l == 0 && overrideTieLeft0 != -1) ? overrideTieLeft0 : (src != null ? src.TieID : -1);
                    ShooterData clone = CloneShooterData(src, tie);
                    if (clone != null) result.Add(clone);
                }
            }

            if (right != null)
            {
                for (int r = 0; r < right.Count; r++)
                {
                    ShooterData src = right[r];
                    int tie = (r == 0 && overrideTieRight0 != -1) ? overrideTieRight0 : (src != null ? src.TieID : -1);
                    ShooterData clone = CloneShooterData(src, tie);
                    if (clone != null) result.Add(clone);
                }
            }
        }

        return result;
    }

    private void BuildGroupsFromShooters(List<ShooterData> shooters)
    {
        _groups.Clear();
        _groupByShooter.Clear();
        _doneShooters.Clear();

        if (shooters == null || shooters.Count == 0) return;
        bool[] used = new bool[shooters.Count];

        for (int i = 0; i < shooters.Count; i++)
        {
            if (used[i]) continue;
            ShooterData data = shooters[i];
            if (data == null)
            {
                used[i] = true;
                continue;
            }

            if (data.TieID == -1)
            {
                var group = new ShooterGroup
                {
                    TieId = -1,
                    Data1 = data,
                    Data2 = null
                };
                _groups.Add(group);
                used[i] = true;
                continue;
            }

            int partnerIndex = -1;
            for (int j = i + 1; j < shooters.Count; j++)
            {
                if (used[j]) continue;
                ShooterData other = shooters[j];
                if (other != null && other.TieID == data.TieID)
                {
                    partnerIndex = j;
                    break;
                }
            }

            var pairedGroup = new ShooterGroup
            {
                TieId = data.TieID,
                Data1 = data,
                Data2 = partnerIndex >= 0 ? shooters[partnerIndex] : null
            };
            _groups.Add(pairedGroup);
            used[i] = true;
            if (partnerIndex >= 0) used[partnerIndex] = true;
        }
    }

    private void SpawnShooters()
    {
        if (_groups == null || _groups.Count == 0) return;
        if (ShooterController.Instance == null || ShooterController.Instance.ShooterPrefab == null) return;

        for (int i = 0; i < _groups.Count; i++)
        {
            ShooterGroup group = _groups[i];
            if (group == null) continue;

            if (group.Data1 != null)
            {
                group.Shooter1 = InstantiateShooter(group.Data1);
            }

            if (group.Data2 != null)
            {
                group.Shooter2 = InstantiateShooter(group.Data2);
            }

            if (group.Shooter1 != null) _groupByShooter[group.Shooter1] = group;
            if (group.Shooter2 != null) _groupByShooter[group.Shooter2] = group;
        }
    }

    private Shooter InstantiateShooter(ShooterData data)
    {
        Shooter shoot = Instantiate(ShooterController.Instance.ShooterPrefab);
        if (shoot == null) return null;
        shoot.ResetForReuse();
        shoot.SetColor(data.Color);
        shoot.SetType(data.Type);
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

        if (_groups == null || _groups.Count == 0)
        {
            CloseGate();
            return;
        }

        PruneEmptyLeadingGroups();

        if (_groups == null || _groups.Count == 0)
        {
            CloseGate();
            return;
        }

        for (int i = 0; i < _groups.Count; i++)
        {
            ShooterGroup group = _groups[i];
            if (group == null) continue;
            ShooterRole role = i == 0 ? ShooterRole.Current : (i == 1 ? ShooterRole.Next : ShooterRole.Queue);
            ApplyRoleToShooter(group.Shooter1, role, 0);
            ApplyRoleToShooter(group.Shooter2, role, 1);
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
        if (amount <= 0 || _groups == null || _groups.Count == 0) return amount;
        int remaining = amount;

        for (int i = _groups.Count - 1; i >= 1; i--)
        {
            if (remaining <= 0) break;
            ShooterGroup group = _groups[i];
            if (group == null) continue;
            remaining = ReduceShooterTotal(group.Shooter1, color, remaining, rainbowOnly);
            remaining = ReduceShooterTotal(group.Shooter2, color, remaining, rainbowOnly);
        }

        return remaining;
    }

    public int ReduceCurrentShooterTotal(ObjectColor color, int amount, bool rainbowOnly = false)
    {
        if (amount <= 0 || _groups == null || _groups.Count == 0) return amount;
        ShooterGroup group = _groups[0];
        if (group == null) return amount;

        int remaining = amount;
        remaining = ReduceShooterTotal(group.Shooter1, color, remaining, rainbowOnly);
        remaining = ReduceShooterTotal(group.Shooter2, color, remaining, rainbowOnly);

        if (IsGroupDone(group))
            CollectCurrentShooter();

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
        if (_groups == null || _groups.Count <= 1) return false;
        _groups.Shuffle();
        UpdateShooterRoles();
        return true;
    }

    public void CollectCurrentShooter()
    {
        if (IsClosed || _groups == null || _groups.Count == 0) return;
        ShooterGroup group = _groups[0];
        if (group == null) return;

        MarkShooterDoneIfNeeded(group.Shooter1);
        MarkShooterDoneIfNeeded(group.Shooter2);
        if (!IsGroupDone(group)) return;

        if (_collectEffect != null)
        {
            _collectEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _collectEffect.Play();
        }

        RemoveGroupAt(0);
    }

    private bool IsGroupDone(ShooterGroup group)
    {
        if (group == null) return true;
        bool done1 = group.Shooter1 == null || group.Shooter1.Total <= 0;
        bool done2 = group.Shooter2 == null || group.Shooter2.Total <= 0;
        return done1 && done2;
    }

    private void RemoveGroupAt(int index)
    {
        if (_groups == null || index < 0 || index >= _groups.Count) return;
        ShooterGroup group = _groups[index];
        if (group != null)
        {
            DestroyShooter(group.Shooter1);
            DestroyShooter(group.Shooter2);
        }

        _groups.RemoveAt(index);

        if (_groups.Count == 0)
        {
            CloseGate();
            return;
        }

        UpdateShooterRoles();
    }

    private void PruneEmptyLeadingGroups()
    {
        if (_groups == null) return;
        bool removedAny = false;
        while (_groups.Count > 0 && IsGroupDone(_groups[0]))
        {
            ShooterGroup group = _groups[0];
            if (group != null)
            {
                MarkShooterDoneIfNeeded(group.Shooter1);
                MarkShooterDoneIfNeeded(group.Shooter2);
                DestroyShooter(group.Shooter1);
                DestroyShooter(group.Shooter2);
            }
            _groups.RemoveAt(0);
            removedAny = true;
        }

        if (removedAny && _groups.Count == 0)
        {
            CloseGate();
        }
    }

    private void DestroyShooter(Shooter shooter, bool immediate = false)
    {
        if (shooter == null) return;
        _groupByShooter.Remove(shooter);
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
        if (_groups.Count > 0)
        {
            for (int i = 0; i < _groups.Count; i++)
            {
                ShooterGroup group = _groups[i];
                if (group == null) continue;
                DestroyShooter(group.Shooter1, immediate: true);
                DestroyShooter(group.Shooter2, immediate: true);
            }
        }
        _groups.Clear();
        _groupByShooter.Clear();
        _doneShooters.Clear();
        Total = 0;
    }
}
