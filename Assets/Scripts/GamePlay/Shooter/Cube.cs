using Sirenix.OdinInspector;
using UnityEngine;

public class Cube : MonoBehaviour
{
    [ReadOnly] public ObjectColor Color;
    [SerializeField] private Renderer _renderer;
    public void SetUp(ObjectColor color)
    {
        Color = color;
        _renderer.material = Board.Instance.ColorConfig.GetCubeColor(color);
    }

}
