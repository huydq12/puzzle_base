using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class Cube : MonoBehaviour
{
    [ReadOnly] public ObjectColor Color;
    [ReadOnly] public Base Base;
    [SerializeField, DisableIn(PrefabKind.PrefabInstance)] private Renderer _renderer;
    [SerializeField, DisableIn(PrefabKind.PrefabInstance)] private ParticleSystem _impactEffect;

    public void SetUp(ObjectColor color, Base bs)
    {
        Base = bs;
        Color = color;
        _renderer.material = Board.Instance.ColorConfig.GetCubeColor(color);
    }
    public Tween Destroy()
    {
        Instantiate(_impactEffect, transform.position, Quaternion.identity);
        return transform.DOScale(0, 0.25f).OnComplete(() => Destroy(gameObject));
    }
}
