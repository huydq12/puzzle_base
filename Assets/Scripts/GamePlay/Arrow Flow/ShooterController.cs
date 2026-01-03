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
        else if (direction == 2) y = 180f;
        else if (direction == 3) y = 270f;
        else if (direction == 4) y = 0f;
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
}
