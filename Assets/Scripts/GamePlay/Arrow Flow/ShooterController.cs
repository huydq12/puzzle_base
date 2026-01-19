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
        Gates = new();
        foreach (var data in datas)
        {
            if (PoolManager.Instance == null) return;
            var gate = PoolManager.Instance.Get(_gatePrefab);
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

        // First pass: prioritize non-current shooters (queue shooters)
        foreach (var gate in Gates)
        {
            if (gate == null || remaining <= 0) continue;
            remaining = gate.ReduceNonCurrentShooterTotal(color, remaining);
        }

        // Second pass: reduce current shooters if still remaining
        foreach (var gate in Gates)
        {
            if (gate == null || remaining <= 0) continue;
            remaining = gate.ReduceCurrentShooterTotal(color, remaining);
        }
    }
}
