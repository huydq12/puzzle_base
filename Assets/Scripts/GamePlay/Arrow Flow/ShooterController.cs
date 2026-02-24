using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class ShooterController : Singleton<ShooterController>
{
    [SerializeField] private Gate _gatePrefab;
    [SerializeField] private Shooter _shooterPrefab;
    [ReadOnly] public List<Gate> Gates;
    public Shooter ShooterPrefab => _shooterPrefab;

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
	        if (_gatePrefab == null) return;
	        Gates = new();
	        foreach (var data in datas)
	        {
	            var gate = Instantiate(_gatePrefab);
	            if (gate == null) continue;
	            gate.transform.SetParent(Board.Instance.transform, false);
	            gate.transform.localPosition = data.Position;
	            gate.transform.rotation = DirectionToRotation(data.Direction);
	            gate.Setup(data.Shooters);
	            Gates.Add(gate);
	        }
	    }

    public void NotifyGateClosed(Gate gate)
    {
        if (gate == null) return;
        if (GameManagerInGame.Instance == null) return;
        if (GameManagerInGame.Instance.CurrentGameStateInGame == GameStateInGame.Result) return;
        if (Gates == null || Gates.Count == 0) return;

        for (int i = 0; i < Gates.Count; i++)
        {
            Gate g = Gates[i];
            if (g == null || !g.IsClosed)
                return;
        }

        if (ConveyorController.Instance != null)
            ConveyorController.Instance.WinGame();
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

        // Third pass: reduce non-current rainbow shooters (Type == 6)
        foreach (var gate in Gates)
        {
            if (gate == null || remaining <= 0) continue;
            remaining = gate.ReduceNonCurrentShooterTotal(color, remaining, rainbowOnly: true);
        }

        // Fourth pass: reduce current rainbow shooters (Type == 6)
        foreach (var gate in Gates)
        {
            if (gate == null || remaining <= 0) continue;
            remaining = gate.ReduceCurrentShooterTotal(color, remaining, rainbowOnly: true);
        }
    }

    public bool ConvertRandomShooterToRainbow()
    {
        if (Gates == null || Gates.Count == 0) return false;

        // Collect all current shooters that are not rainbow (Type != 6) and not closed
        List<Shooter> candidates = new List<Shooter>();
        foreach (var gate in Gates)
        {
            if (gate == null || gate.IsClosed) continue;
            if (gate.CurrentShooter != null && gate.CurrentShooter.Type != 6 && gate.CurrentShooter.Total > 0)
            {
                candidates.Add(gate.CurrentShooter);
            }
        }

        // If no non-rainbow candidates, try any current shooter that's not closed
        if (candidates.Count == 0)
        {
            foreach (var gate in Gates)
            {
                if (gate == null || gate.IsClosed) continue;
                if (gate.CurrentShooter != null && gate.CurrentShooter.Total > 0)
                {
                    candidates.Add(gate.CurrentShooter);
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

            var shooter = gate.CurrentShooter;
            if (shooter == null) continue;
            if (shooter.Total <= 0) continue;
            return true;
        }

        return false;
    }

    public bool CanShuffleShootersOnGates()
    {
        if (Gates == null || Gates.Count == 0) return false;

        for (int i = 0; i < Gates.Count; i++)
        {
            var gate = Gates[i];
            if (gate == null || gate.IsClosed) continue;
            if (gate.RemainingShooterCount > 1) return true;
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

    public bool CanShuffleShootersOnGate(Gate gate)
    {
        if (gate == null) return false;
        if (gate.IsClosed) return false;
        return gate.RemainingShooterCount > 1;
    }

    public bool ShuffleShootersOnGate(Gate gate)
    {
        if (!CanShuffleShootersOnGate(gate)) return false;
        return gate.ShuffleRemainingShooters();
    }
}
