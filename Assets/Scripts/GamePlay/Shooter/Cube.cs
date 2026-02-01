using System.Linq;
using DG.Tweening;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;

public class Cube : MonoBehaviour
{
    [ReadOnly] public ObjectColor Color;
    [ReadOnly] public Base Base;
    [SerializeField] private Renderer _renderer;
    [SerializeField] private Collider _collider;
    [SerializeField] private LayerMask _fireLayer;
    public bool CanTrigger
    {
        get => _collider.enabled;
        set => _collider.enabled = value;
    }
    public void SetUp(ObjectColor color, Base bs)
    {
        Base = bs;
        Color = color;
        _renderer.material = Board.Instance.ColorConfig.GetCubeColor(color);
    }
    public Tween Destroy()
    {
        CanTrigger = false;
        return transform.DOScale(0, 0.2f).OnComplete(() => Destroy(gameObject));
    }
    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & _fireLayer.value) != 0)
        {
            var holder = Board.Instance.CurrentMap.Holders.FirstOrDefault(b => b.IsOccupied && b.ShooterOnholder.Color == Color);
            if (holder != null)
            {
                holder.ShooterOnholder.Shoot(Base.Cubes[Random.Range(0, Base.Cubes.Count)]);
                Base.DestroyLine();
            }
        }
    }
}
