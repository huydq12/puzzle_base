using UnityEngine;

public sealed class CrossConnectionMesh : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer _renderer;

    private Material[] _defaultSharedMaterials;

    private void Awake()
    {
        CacheDefaultsIfNeeded();
    }

    private void OnEnable()
    {
        CacheDefaultsIfNeeded();
    }

    private void CacheDefaultsIfNeeded()
    {
        if (_defaultSharedMaterials != null) return;
        var r = GetRenderer();
        _defaultSharedMaterials = r != null ? r.sharedMaterials : null;
    }

    private SkinnedMeshRenderer GetRenderer()
    {
        if (_renderer != null) return _renderer;
        _renderer = GetComponentInChildren<SkinnedMeshRenderer>(true);
        return _renderer;
    }

    public void SetMaterials(Material material0, Material material1, bool useSharedMaterials = true)
    {
        var r = GetRenderer();
        if (r == null) return;

        var mats = useSharedMaterials ? r.sharedMaterials : r.materials;
        if (mats == null || mats.Length < 2)
            mats = new Material[2];

        if (material0 != null) mats[0] = material0;
        if (material1 != null) mats[1] = material1;

        if (useSharedMaterials) r.sharedMaterials = mats;
        else r.materials = mats;
    }

    public void ResetToDefaultSharedMaterials()
    {
        var r = GetRenderer();
        if (r == null) return;
        if (_defaultSharedMaterials == null) return;
        r.sharedMaterials = _defaultSharedMaterials;
    }

    public static void Apply(GameObject target, Material material0, Material material1, bool useSharedMaterials = true)
    {
        if (target == null) return;
        var comp = target.GetComponent<CrossConnectionMesh>();
        if (comp != null)
        {
            comp.SetMaterials(material0, material1, useSharedMaterials);
            return;
        }

        var r = target.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (r == null) return;

        var mats = useSharedMaterials ? r.sharedMaterials : r.materials;
        if (mats == null || mats.Length < 2)
            mats = new Material[2];

        if (material0 != null) mats[0] = material0;
        if (material1 != null) mats[1] = material1;

        if (useSharedMaterials) r.sharedMaterials = mats;
        else r.materials = mats;
    }
}

