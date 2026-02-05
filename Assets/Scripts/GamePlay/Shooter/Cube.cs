using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class Cube : MonoBehaviour
{
    [ReadOnly] public ObjectColor Color;
    [ReadOnly] public Base Base;
    [SerializeField] private Renderer _renderer;

    public void SetUp(ObjectColor color, Base bs)
    {
        Base = bs;
        Color = color;
        _renderer.material = Board.Instance.ColorConfig.GetCubeColor(color);
    }
    public Tween Destroy()
    {
        return transform.DOScale(0, 0.15f).OnComplete(() => Destroy(gameObject));
    }
}
