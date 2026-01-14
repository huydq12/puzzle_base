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
        if (direction == 1) y = 90f;
        else if (direction == 2) y = 0f;
        else if (direction == 3) y = -90f;
        else if (direction == 4) y = -180f;
        return Quaternion.Euler(0f, y, 0f);
    }

    public void Setup(List<GateData> datas)
    {
        Gates = new();
        foreach (var data in datas)
        {
            var gate = Instantiate(_gatePrefab, Board.Instance.transform);
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
}
