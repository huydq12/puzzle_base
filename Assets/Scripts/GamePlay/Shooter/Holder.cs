using Sirenix.OdinInspector;
using UnityEngine;

public class Holder : MonoBehaviour
{
    [SerializeField] private Renderer _renderer;
    [ReadOnly] public Shooter ShooterOnholder;
    public bool IsOccupied => ShooterOnholder != null;
}
