using DG.Tweening;
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
        if (shooter != null)
        {
            ShooterOnholder.transform.SetParent(_spawnPos);
            ShooterOnholder.IsMoving = true;
            ShooterOnholder.transform.DOLocalJump(Vector3.zero, 1.2f, 1, 0.45f).SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                ShooterOnholder.IsMoving = false;
                ShooterOnholder.ShowRemaining = true;
            });
            shooter.Holder = this;
        }
    }
}
