using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class Shooter : MonoBehaviour
{
    [SerializeField] private Renderer[] _renderer;
    [SerializeField] private TextMeshPro _total;
    [ReadOnly] public ObjectColor Color;

    public void SetColor(ObjectColor color)
    {
        Color = color;
        foreach(var renderer in _renderer)
        {
            renderer.sharedMaterial = Board.Instance.ColorConfig.GetShooterByColor(color);
        }
    }
    public int Total
    {
        get => int.Parse(_total.text);
        set
        {
            _total.text = value.ToString();
        }
    }
}
