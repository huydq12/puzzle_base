using System.IO;
using UnityEditor;
using UnityEngine;

public static class ConveyorMeshMappingTool
{
    private const string SourceMeshPath = "Assets/Mesh/c_track_part2.asset";
    private const string ReferenceMeshPath = "Assets/Mesh/RB_Mid.v3_.asset";
    private const string OutputMeshPath = "Assets/Mesh/c_track_part2_conveyor_mapped.asset";

    [MenuItem("Tools/Meshes/Generate Conveyor Mesh Mapping")]
    public static void GenerateFromMenu()
    {
        GenerateMappedMesh();
    }

    public static void GenerateMappedMesh()
    {
        var source = AssetDatabase.LoadAssetAtPath<Mesh>(SourceMeshPath);
        var reference = AssetDatabase.LoadAssetAtPath<Mesh>(ReferenceMeshPath);

        if (source == null || reference == null)
        {
            Debug.LogError($"[ConveyorMeshMappingTool] Missing mesh. Source: {source != null}, Reference: {reference != null}");
            return;
        }

        var mapped = Object.Instantiate(source);
        mapped.name = Path.GetFileNameWithoutExtension(OutputMeshPath);

        var sourceBounds = source.bounds;
        var referenceBounds = reference.bounds;

        var scale = new Vector3(
            SafeDivide(referenceBounds.size.x, sourceBounds.size.x),
            SafeDivide(referenceBounds.size.y, sourceBounds.size.y),
            SafeDivide(referenceBounds.size.z, sourceBounds.size.z)
        );

        var sourceCenter = sourceBounds.center;
        var referenceCenter = referenceBounds.center;

        var vertices = source.vertices;
        var normals = source.normals;
        var tangents = source.tangents;

        var vertexMatrix = Matrix4x4.TRS(referenceCenter, Quaternion.identity, scale) *
                           Matrix4x4.TRS(-sourceCenter, Quaternion.identity, Vector3.one);
        var normalMatrix = vertexMatrix.inverse.transpose;

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = vertexMatrix.MultiplyPoint3x4(vertices[i]);
        }

        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = normalMatrix.MultiplyVector(normals[i]).normalized;
        }

        for (int i = 0; i < tangents.Length; i++)
        {
            var tangent = new Vector3(tangents[i].x, tangents[i].y, tangents[i].z);
            tangent = normalMatrix.MultiplyVector(tangent).normalized;
            tangents[i] = new Vector4(tangent.x, tangent.y, tangent.z, tangents[i].w);
        }

        mapped.vertices = vertices;
        mapped.normals = normals;
        mapped.tangents = tangents;
        mapped.bounds = referenceBounds;

        AssetDatabase.DeleteAsset(OutputMeshPath);
        AssetDatabase.CreateAsset(mapped, OutputMeshPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "[ConveyorMeshMappingTool] Generated mapped mesh.\n" +
            $"Source bounds: center={sourceBounds.center} size={sourceBounds.size}\n" +
            $"Reference bounds: center={referenceBounds.center} size={referenceBounds.size}\n" +
            $"Applied scale: {scale}\n" +
            $"Output: {OutputMeshPath}"
        );
    }

    private static float SafeDivide(float a, float b)
    {
        return Mathf.Approximately(b, 0f) ? 1f : a / b;
    }
}
