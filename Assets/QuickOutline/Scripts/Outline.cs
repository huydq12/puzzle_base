//
//  Outline.cs
//  QuickOutline
//
//  Created by Chris Nolet on 3/30/18.
//  Copyright © 2018 Chris Nolet. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

[DisallowMultipleComponent]

public class Outline : MonoBehaviour
{
  private static HashSet<Mesh> registeredMeshes = new HashSet<Mesh>();

  public enum Mode
  {
    OutlineAll,
    OutlineVisible,
    OutlineHidden,
    OutlineAndSilhouette,
    SilhouetteOnly
  }

  public Mode OutlineMode
  {
    get { return outlineMode; }
    set
    {
      outlineMode = value;
      needsUpdate = true;
    }
  }

  public Color OutlineColor
  {
    get { return outlineColor; }
    set
    {
      outlineColor = value;
      needsUpdate = true;
    }
  }

  public float OutlineWidth
  {
    get { return outlineWidth; }
    set
    {
      outlineWidth = value;
      needsUpdate = true;
    }
  }

  [Serializable]
  private class ListVector3
  {
    public List<Vector3> data;
  }

  [SerializeField]
  private Mode outlineMode;

  [SerializeField]
  private Color outlineColor = Color.white;

  [SerializeField, Range(0f, 10f)]
  private float outlineWidth = 2f;

  [Header("Optional")]

  [SerializeField, Tooltip("Precompute enabled: Per-vertex calculations are performed in the editor and serialized with the object. "
  + "Precompute disabled: Per-vertex calculations are performed at runtime in Awake(). This may cause a pause for large meshes.")]
  private bool precomputeOutline;

  [SerializeField, HideInInspector]
  private List<Mesh> bakeKeys = new List<Mesh>();

  [SerializeField, HideInInspector]
  private List<ListVector3> bakeValues = new List<ListVector3>();

  public List<Renderer> Renderers;
  public bool RenderOnEnable;
  public bool ClearOnDisable;
  private Material outlineMaskMaterial;
  private Material outlineFillMaterial;

  private bool needsUpdate;

  void Awake()
  {

    // Instantiate outline materials
    outlineMaskMaterial = Instantiate(Resources.Load<Material>(@"Materials/OutlineMask"));
    outlineFillMaterial = Instantiate(Resources.Load<Material>(@"Materials/OutlineFill"));

    outlineMaskMaterial.name = "OutlineMask (Instance)";
    outlineFillMaterial.name = "OutlineFill (Instance)";


    needsUpdate = true;
  }
  public void ClearTargetOutline(Renderer target)
  {
    if (Renderers == null || Renderers.Count == 0) return;
    var materials = target.sharedMaterials.ToList();

    materials.Remove(outlineMaskMaterial);
    materials.Remove(outlineFillMaterial);

    target.materials = materials.ToArray();
    Renderers.Remove(target);
  }
  public void ClearOutline()
  {
    foreach (var renderer in Renderers)
    {
      // Remove outline shaders
      if (renderer == null) continue;
      var materials = renderer.sharedMaterials.ToList();

      materials.Remove(outlineMaskMaterial);
      materials.Remove(outlineFillMaterial);

      renderer.materials = materials.ToArray();
    }
    if (ClearOnDisable)
    {
      Renderers = new List<Renderer>();
    }
  }
  [Button]
  public void RenderOutline()
  {
    if (Renderers == null || Renderers.Count == 0) return;
    LoadSmoothNormals();
    foreach (var renderer in Renderers)
    {
      // Append outline shaders
      var materials = renderer.sharedMaterials.ToList();

      materials.Add(outlineMaskMaterial);
      materials.Add(outlineFillMaterial);

      renderer.materials = materials.ToArray();
    }
  }
  void OnEnable()
  {
    if (RenderOnEnable)
    {
      RenderOutline();
    }
  }

  void OnValidate()
  {

    // Update material properties
    needsUpdate = true;

    // Clear cache when baking is disabled or corrupted
    if (!precomputeOutline && bakeKeys.Count != 0 || bakeKeys.Count != bakeValues.Count)
    {
      bakeKeys.Clear();
      bakeValues.Clear();
    }

    // Generate smooth normals when baking is enabled
    if (precomputeOutline && bakeKeys.Count == 0)
    {
      Bake();
    }
  }

  void Update()
  {
    if (needsUpdate)
    {
      needsUpdate = false;

      UpdateMaterialProperties();
    }
  }

  void OnDisable()
  {
    ClearOutline();
  }

  void OnDestroy()
  {

    // Destroy material instances
    Destroy(outlineMaskMaterial);
    Destroy(outlineFillMaterial);
  }

  void Bake()
  {

    // Generate smooth normals for each mesh
    var bakedMeshes = new HashSet<Mesh>();

    foreach (var meshFilter in GetComponentsInChildren<MeshFilter>())
    {

      // Skip duplicates
      if (!bakedMeshes.Add(meshFilter.sharedMesh))
      {
        continue;
      }

      // Serialize smooth normals
      var smoothNormals = SmoothNormals(meshFilter.sharedMesh);

      bakeKeys.Add(meshFilter.sharedMesh);
      bakeValues.Add(new ListVector3() { data = smoothNormals });
    }
  }

  void LoadSmoothNormals()
  {
    foreach (var renderer in Renderers)
    {
      Mesh mesh = null;

      if (renderer is SkinnedMeshRenderer skinned)
      {
        mesh = skinned.sharedMesh;

        // Bỏ qua nếu đã xử lý
        if (!registeredMeshes.Add(mesh))
          continue;

        // Clear UV3 (UV4 là TEXCOORD3)
        mesh.uv4 = new Vector2[mesh.vertexCount];

        CombineSubmeshes(mesh, skinned.sharedMaterials);
      }
      else if (renderer is MeshRenderer)
      {
        var filter = renderer.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null)
          continue;

        mesh = filter.sharedMesh;

        if (!registeredMeshes.Add(mesh))
          continue;

        // Lấy từ bake hoặc tính lại
        int index = bakeKeys.IndexOf(mesh);
        var smoothNormals = (index >= 0) ? bakeValues[index].data : SmoothNormals(mesh);

        mesh.SetUVs(3, smoothNormals); // TEXCOORD3 = uv3

        CombineSubmeshes(mesh, renderer.sharedMaterials);
      }
    }
  }


  List<Vector3> SmoothNormals(Mesh mesh)
  {

    // Group vertices by location
    var groups = mesh.vertices.Select((vertex, index) => new KeyValuePair<Vector3, int>(vertex, index)).GroupBy(pair => pair.Key);

    // Copy normals to a new list
    var smoothNormals = new List<Vector3>(mesh.normals);

    // Average normals for grouped vertices
    foreach (var group in groups)
    {

      // Skip single vertices
      if (group.Count() == 1)
      {
        continue;
      }

      // Calculate the average normal
      var smoothNormal = Vector3.zero;

      foreach (var pair in group)
      {
        smoothNormal += smoothNormals[pair.Value];
      }

      smoothNormal.Normalize();

      // Assign smooth normal to each vertex
      foreach (var pair in group)
      {
        smoothNormals[pair.Value] = smoothNormal;
      }
    }

    return smoothNormals;
  }

  void CombineSubmeshes(Mesh mesh, Material[] materials)
  {

    // Skip meshes with a single submesh
    if (mesh.subMeshCount == 1)
    {
      return;
    }

    // Skip if submesh count exceeds material count
    if (mesh.subMeshCount > materials.Length)
    {
      return;
    }

    // Append combined submesh
    mesh.subMeshCount++;
    mesh.SetTriangles(mesh.triangles, mesh.subMeshCount - 1);
  }

  void UpdateMaterialProperties()
  {

    // Apply properties according to mode
    outlineFillMaterial.SetColor("_OutlineColor", outlineColor);

    switch (outlineMode)
    {
      case Mode.OutlineAll:
        outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
        break;

      case Mode.OutlineVisible:
        outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
        outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
        break;

      case Mode.OutlineHidden:
        outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
        outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
        break;

      case Mode.OutlineAndSilhouette:
        outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
        outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
        break;

      case Mode.SilhouetteOnly:
        outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
        outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
        outlineFillMaterial.SetFloat("_OutlineWidth", 0f);
        break;
    }
  }
}
