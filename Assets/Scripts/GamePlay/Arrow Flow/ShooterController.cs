using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class ShooterController : Singleton<ShooterController>
{
    [SerializeField] private Gate _gatePrefab;
    [SerializeField] private GateDouble _gateDoublePrefab;
    [SerializeField] private Shooter _shooterPrefab;
    [SerializeField] private Lock _lockPrefab;
    [ReadOnly] public List<IGate> Gates;
    public Shooter ShooterPrefab => _shooterPrefab;
    public Lock LockPrefab => _lockPrefab;

    private Quaternion DirectionToRotation(int direction)
    {
        float y = 0f;
        if (direction == 1) y = 45f;
        else if (direction == 2) y = 90f;
        else if (direction == 3) y = 135f;
        else if (direction == 4) y = 180f;
        else if (direction == 5) y = 225f;
        else if (direction == 6) y = 270f;
        else if (direction == 7) y = 315f;
        else if (direction == 8) y = 360f;
        return Quaternion.Euler(0f, y, 0f);
    }

	    public void Setup(List<GateData> datas)
	    {
	        Gates = new();
	        Setup(datas, null);
	    }

        public void Setup(List<GateData> datas, List<GateDataDouble> datasDouble)
        {
            Gates = new();

            if (datas != null)
            {
                for (int i = 0; i < datas.Count; i++)
                {
                    GateData data = datas[i];
                    if (data == null) continue;

                    bool useDouble = data.ElementType == 6;
                    MonoBehaviour instance = null;

                    if (useDouble)
                    {
                        if (_gateDoublePrefab == null) continue;
                        instance = Instantiate(_gateDoublePrefab);
                    }
                    else
                    {
                        if (_gatePrefab == null) continue;
                        instance = Instantiate(_gatePrefab);
                    }

                    if (instance == null) continue;

                    instance.transform.SetParent(Board.Instance.transform, false);
                    instance.transform.localPosition = data.Position;
                    instance.transform.rotation = DirectionToRotation(data.Direction);

                    if (instance is IGate iGate)
                        Gates.Add(iGate);

                    if (instance is Gate gate)
                        gate.Setup(data);
                    else if (instance is GateDouble gateDouble)
                        gateDouble.Setup(data);
                }
            }

            if (datasDouble != null)
            {
                for (int i = 0; i < datasDouble.Count; i++)
                {
                    GateDataDouble data = datasDouble[i];
                    if (data == null) continue;
                    if (_gateDoublePrefab == null) continue;

                    GateDouble gateDouble = Instantiate(_gateDoublePrefab);
                    if (gateDouble == null) continue;

                    gateDouble.transform.SetParent(Board.Instance.transform, false);
                    gateDouble.transform.localPosition = data.Position;
                    gateDouble.transform.rotation = DirectionToRotationGateDouble(data.Direction);
                    Debug.Log($"GateDouble direction={data.Direction} rotation={gateDouble.transform.rotation}");

                    Gates.Add(gateDouble);
                    gateDouble.Setup(data);
                }
            }
        }

    public void NotifyGateClosed(IGate gate)
    {
        if (gate == null) return;
        if (GameManagerInGame.Instance == null) return;
        if (GameManagerInGame.Instance.CurrentGameStateInGame == GameStateInGame.Result) return;
    }

    private Quaternion DirectionToRotationGateDouble(int direction)
    {
        float y = 0f;
        if (direction == 1) y = 135f;
        else if (direction == 2) y = 180f;
        else if (direction == 3) y = 225f;
        else if (direction == 4) y = 270f;
        else if (direction == 5) y = 315f;
        else if (direction == 6) y = 360f;
        else if (direction == 7) y = 45f;
        else if (direction == 8) y = 90f;
        return Quaternion.Euler(0f, y, 0f);
    }

    public void RemoveGate(IGate gate)
    {
        if (gate == null || Gates == null) return;
        Gates.Remove(gate);
    }

    public void ConsumeShooterIceStep(int amount)
    {
        if (Gates == null || Gates.Count == 0) return;
        int clamped = Mathf.Max(0, amount);
        if (clamped <= 0) return;

        for (int i = 0; i < Gates.Count; i++)
        {
            IGate gate = Gates[i];
            if (gate == null) continue;
            gate.ConsumeIceCounter(clamped);
        }
    }

    public bool UnlockCurrentLockOnGate(Gate gate, Key key = null)
    {
        if (gate == null) return false;
        return gate.UnlockCurrentLock(key);
    }

    public bool UnlockNextLockOnGate(Gate gate, Key key = null)
    {
        if (gate == null) return false;
        return gate.UnlockNextLock(key);
    }

    public bool UnlockCurrentLockOnAnyGate(Key key = null)
    {
        if (Gates == null || Gates.Count == 0) return false;
        for (int i = 0; i < Gates.Count; i++)
        {
            IGate gate = Gates[i];
            if (gate != null && gate.UnlockCurrentLock(key))
                return true;
        }
        return false;
    }

    public bool UnlockNextLockOnAnyGate(Key key = null)
    {
        if (Gates == null || Gates.Count == 0) return false;

        for (int i = 0; i < Gates.Count; i++)
        {
            IGate gate = Gates[i];
            if (gate != null && gate.UnlockCurrentLock(key))
                return true;
        }

        for (int i = 0; i < Gates.Count; i++)
        {
            IGate gate = Gates[i];
            if (gate != null && gate.UnlockNextLock(key))
                return true;
        }
        return false;
    }

    public void ReduceShooterTotalByColor(ObjectColor color, int amount)
    {
        if (Gates == null || Gates.Count == 0 || amount <= 0) return;

        int remaining = amount;

        // First pass: prioritize non-current shooters with matching color
        foreach (var gate in Gates)
        {
            if (gate == null || remaining <= 0) continue;
            remaining = gate.ReduceNonCurrentShooterTotal(color, remaining, rainbowOnly: false);
        }

        // Second pass: reduce current shooters with matching color
        foreach (var gate in Gates)
        {
            if (gate == null || remaining <= 0) continue;
            remaining = gate.ReduceCurrentShooterTotal(color, remaining, rainbowOnly: false);
        }

        // Third pass: reduce non-current rainbow shooters
        foreach (var gate in Gates)
        {
            if (gate == null || remaining <= 0) continue;
            remaining = gate.ReduceNonCurrentShooterTotal(color, remaining, rainbowOnly: true);
        }

        // Fourth pass: reduce current rainbow shooters
        foreach (var gate in Gates)
        {
            if (gate == null || remaining <= 0) continue;
            remaining = gate.ReduceCurrentShooterTotal(color, remaining, rainbowOnly: true);
        }
    }

    public bool ConvertRandomShooterToRainbow()
    {
        if (Gates == null || Gates.Count == 0) return false;

        // Collect all current shooters that are not rainbow and not closed
        List<Shooter> candidates = new List<Shooter>();
        foreach (var gate in Gates)
        {
            if (gate == null || gate.IsClosed) continue;
            foreach (Shooter shooter in gate.GetCurrentShooters())
            {
                if (shooter == null) continue;
                if (shooter.IsRainbow) continue;
                if (shooter.Total <= 0) continue;
                candidates.Add(shooter);
            }
        }

        // If no non-rainbow candidates, try any current shooter that's not closed
        if (candidates.Count == 0)
        {
            foreach (var gate in Gates)
            {
                if (gate == null || gate.IsClosed) continue;
                foreach (Shooter shooter in gate.GetCurrentShooters())
                {
                    if (shooter == null) continue;
                    if (shooter.Total <= 0) continue;
                    candidates.Add(shooter);
                }
            }
        }

        if (candidates.Count == 0) return false;

        // Pick random shooter from candidates
        int randomIndex = Random.Range(0, candidates.Count);
        Shooter chosen = candidates[randomIndex];
        chosen.SetRainbow();
        return true;
    }

    public bool CanConvertRandomShooterToRainbow()
    {
        if (Gates == null || Gates.Count == 0) return false;

        for (int i = 0; i < Gates.Count; i++)
        {
            var gate = Gates[i];
            if (gate == null || gate.IsClosed) continue;

            foreach (Shooter shooter in gate.GetCurrentShooters())
            {
                if (shooter == null) continue;
                if (shooter.Total <= 0) continue;
                return true;
            }
        }

        return false;
    }

    private static bool CanShuffleGate(IGate gate)
    {
        if (gate == null) return false;
        if (gate.IsClosed) return false;
        if (gate is Gate concreteGate) return concreteGate.CanShuffleUpcomingShooters();
        if (gate is GateDouble concreteGateDouble) return concreteGateDouble.CanShuffleUpcomingShooters();
        return gate.RemainingShooterCount > 1;
    }

    public bool CanShuffleShootersOnGates()
    {
        if (Gates == null || Gates.Count == 0) return false;

        for (int i = 0; i < Gates.Count; i++)
        {
            var gate = Gates[i];
            if (CanShuffleGate(gate)) return true;
        }

        return false;
    }

    public bool ShuffleShootersOnGates()
    {
        if (Gates == null || Gates.Count == 0) return false;

        bool shuffledAny = false;
        for (int i = 0; i < Gates.Count; i++)
        {
            var gate = Gates[i];
            if (gate == null) continue;
            if (gate.ShuffleRemainingShooters()) shuffledAny = true;
        }

        return shuffledAny;
    }

    public bool CanShuffleShootersOnGate(IGate gate)
    {
        if (gate == null) return false;
        return CanShuffleGate(gate);
    }

    public bool ShuffleShootersOnGate(IGate gate)
    {
        if (!CanShuffleShootersOnGate(gate)) return false;
        return gate.ShuffleRemainingShooters();
    }
}
