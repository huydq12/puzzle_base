using UnityEngine;

public sealed class CrossConnectionMesh : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer _renderer;

    private Material[] _defaultSharedMaterials;
    private readonly System.Collections.Generic.Dictionary<int, Vector3> _meshCenterCache = new();

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

        Vector3 visualCenterLocal = GetStableVisualCenterLocal(mesh);
        Vector3 visualCenterWorld = transform.TransformPoint(visualCenterLocal);
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

        Vector3 visualCenterMeshLocal = GetStableVisualCenterLocal(r.sharedMesh);
        Vector3 visualCenterWorld = transform.TransformPoint(visualCenterMeshLocal);
        Vector3 visualCenterRelativeLocal = relativeTo.InverseTransformPoint(visualCenterWorld);
        Vector3 deltaLocal = localPoint - visualCenterRelativeLocal;

        Vector3 posLocal = transform.localPosition;
        transform.localPosition = new Vector3(posLocal.x + deltaLocal.x, posLocal.y, posLocal.z + deltaLocal.z);
    }

    private Vector3 GetStableVisualCenterLocal(Mesh mesh)
    {
        if (mesh == null) return Vector3.zero;

        int meshId = mesh.GetInstanceID();
        if (_meshCenterCache.TryGetValue(meshId, out Vector3 cachedCenter))
            return cachedCenter;

        Vector3 center = mesh.bounds.center;

        // Some imported skinned meshes have inflated/shifted mesh.bounds due blend-shape ranges.
        // Sub-mesh bounds are usually closer to the rendered geometry center.
        if (mesh.subMeshCount > 0)
        {
            Bounds combined = default;
            bool hasAny = false;

            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                var sub = mesh.GetSubMesh(i);
                Bounds b = sub.bounds;
                if (b.size.sqrMagnitude <= 0.000001f) continue;

                if (!hasAny)
                {
                    combined = b;
                    hasAny = true;
                }
                else
                {
                    combined.Encapsulate(b.min);
                    combined.Encapsulate(b.max);
                }
            }

            if (hasAny)
                center = combined.center;
        }

        _meshCenterCache[meshId] = center;
        return center;
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
