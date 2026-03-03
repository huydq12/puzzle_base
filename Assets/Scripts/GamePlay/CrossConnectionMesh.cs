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

    public void AlignVisualCenterToWorldXZ(Vector3 worldPoint)
    {
        var r = GetRenderer();
        if (r == null)
        {
            var p = transform.position;
            transform.position = new Vector3(worldPoint.x, p.y, worldPoint.z);
            return;
        }

        var mesh = r.sharedMesh;
        if (mesh == null)
        {
            var p = transform.position;
            transform.position = new Vector3(worldPoint.x, p.y, worldPoint.z);
            return;
        }

        // Use mesh-local bounds center to avoid frame-dependent world AABB drift.
        Vector3 visualCenterWorld = transform.TransformPoint(mesh.bounds.center);
        Vector3 delta = worldPoint - visualCenterWorld;
        transform.position += new Vector3(delta.x, 0f, delta.z);
    }

    public void AlignVisualCenterToLocalXZ(Vector3 localPoint, Transform relativeTo)
    {
        if (relativeTo == null)
        {
            AlignVisualCenterToWorldXZ(localPoint);
            return;
        }

        var r = GetRenderer();
        if (r == null || r.sharedMesh == null)
        {
            Vector3 lp = transform.localPosition;
            transform.localPosition = new Vector3(localPoint.x, lp.y, localPoint.z);
            return;
        }

        Vector3 visualCenterWorld = transform.TransformPoint(r.sharedMesh.bounds.center);
        Vector3 visualCenterLocal = relativeTo.InverseTransformPoint(visualCenterWorld);
        Vector3 deltaLocal = localPoint - visualCenterLocal;

        Vector3 posLocal = transform.localPosition;
        transform.localPosition = new Vector3(posLocal.x + deltaLocal.x, posLocal.y, posLocal.z + deltaLocal.z);
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
