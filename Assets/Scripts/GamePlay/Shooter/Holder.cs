using Sirenix.OdinInspector;
using UnityEngine;

public class Holder : MonoBehaviour
{
    [SerializeField] private Renderer _renderer;
    [SerializeField] private Transform _spawnPos;
    [ReadOnly] public Shooter ShooterOnholder;
    public bool IsOccupied => ShooterOnholder != null;
    public void AssignShooter(Shooter shooter)
    {
        ShooterOnholder = shooter;
        ShooterOnholder.transform.SetParent(_spawnPos);
        ShooterOnholder.transform.localPosition = Vector3.zero;
        shooter.Holder = this;
    }
}
