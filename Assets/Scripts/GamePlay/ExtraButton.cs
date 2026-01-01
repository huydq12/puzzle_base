using System.Collections.Generic;
using UnityEngine;

public class ExtraButton : MonoBehaviour
{
    [SerializeField] private List<MeshRenderer> _renderer;

    public void SetColor(Material material)
    {
        if (material == null || _renderer == null)
        {
            return;
        }

        for (int i = 0; i < _renderer.Count; i++)
        {
            var r = _renderer[i];
            if (r == null) continue;
            r.material = material;
        }
    }
}
