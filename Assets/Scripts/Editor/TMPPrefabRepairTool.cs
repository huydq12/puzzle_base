using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class TMPPrefabRepairTool
{
    private const string DefaultFontGuid = "651a769f17a6fe144a1deea3025b3804";
    private const string DefaultMaterialGuid = "8edbc6f602abffe49af8fd43f3e74d98";

    private const string LegacyMissingFontGuid = "a1e54c109e0e04464a0328a5b68e6de9";
    private const string LegacyMissingMaterialFileId = "-1301785742879483132";

    private static readonly Regex TmpBlockRegex = new Regex(
        @"(?ms)^--- !u!114 &\d+\nMonoBehaviour:\n.*?^\s*m_Script: \{fileID: 11500000, guid: f4688fdb7df04437aeb418b961361dc5, type: 3\}\n.*?(?=^--- !u!|\z)",
        RegexOptions.Compiled);

    private static readonly Regex FontAssetRegex = new Regex(
        @"^\s*m_fontAsset:\s*\{fileID:\s*(?<fileId>-?\d+), guid:\s*(?<guid>[0-9a-f]{32}), type:\s*2\}\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex NullFontAssetRegex = new Regex(
        @"^\s*m_fontAsset:\s*\{fileID:\s*0\}\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex SharedMaterialRegex = new Regex(
        @"^\s*m_sharedMaterial:\s*\{fileID:\s*(?<fileId>-?\d+), guid:\s*(?<guid>[0-9a-f]{32}), type:\s*2\}\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex NullSharedMaterialRegex = new Regex(
        @"^\s*m_sharedMaterial:\s*\{fileID:\s*0\}\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex MaterialOverrideRegex = new Regex(
        @"^\s*m_Material:\s*\{fileID:\s*2100000, guid:\s*[0-9a-f]{32}, type:\s*2\}\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    [MenuItem("Tools/TMP/Repair Prefab TMP References")]
    public static void RepairPrefabTmpReferences()
    {
        var prefabPaths = CollectPrefabPaths();
        int prefabFixCount = 0;
        int textFixCount = 0;

        try
        {
            for (int i = 0; i < prefabPaths.Count; i++)
            {
                string path = prefabPaths[i];
                EditorUtility.DisplayProgressBar("TMP Repair", path, (float)i / prefabPaths.Count);

                string content = File.ReadAllText(path);
                int localFixCount;
                string updated = RepairPrefabContent(content, out localFixCount);
                if (localFixCount <= 0 || updated == content)
                {
                    continue;
                }

                File.WriteAllText(path, updated);
                prefabFixCount++;
                textFixCount += localFixCount;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        Debug.Log($"TMP prefab repair finished. Prefabs fixed: {prefabFixCount}, TMP blocks fixed: {textFixCount}.");
    }

    private static List<string> CollectPrefabPaths()
    {
        var paths = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("/Downloads/"))
            {
                continue;
            }

            paths.Add(path);
        }

        return paths;
    }

    private static string RepairPrefabContent(string content, out int localFixCount)
    {
        int fixCount = 0;
        string updated = TmpBlockRegex.Replace(content, match =>
        {
            string block = match.Value;
            string original = block;

            string fontGuid = null;
            var fontMatch = FontAssetRegex.Match(block);
            if (fontMatch.Success)
            {
                fontGuid = fontMatch.Groups["guid"].Value;
                if (fontGuid == LegacyMissingFontGuid)
                {
                    block = FontAssetRegex.Replace(block, "  m_fontAsset: {fileID: 11400000, guid: " + DefaultFontGuid + ", type: 2}", 1);
                    fontGuid = DefaultFontGuid;
                }
            }
            else if (NullFontAssetRegex.IsMatch(block))
            {
                block = NullFontAssetRegex.Replace(block, "  m_fontAsset: {fileID: 11400000, guid: " + DefaultFontGuid + ", type: 2}", 1);
                fontGuid = DefaultFontGuid;
            }

            var sharedMaterialMatch = SharedMaterialRegex.Match(block);
            if (sharedMaterialMatch.Success)
            {
                string materialGuid = sharedMaterialMatch.Groups["guid"].Value;
                string materialFileId = sharedMaterialMatch.Groups["fileId"].Value;
                if (materialGuid == LegacyMissingFontGuid || materialFileId == LegacyMissingMaterialFileId)
                {
                    block = SharedMaterialRegex.Replace(block, "  m_sharedMaterial: {fileID: 2100000, guid: " + DefaultMaterialGuid + ", type: 2}", 1);
                }
            }
            else if (NullSharedMaterialRegex.IsMatch(block) && fontGuid == DefaultFontGuid)
            {
                block = NullSharedMaterialRegex.Replace(block, "  m_sharedMaterial: {fileID: 2100000, guid: " + DefaultMaterialGuid + ", type: 2}", 1);
            }

            if (fontGuid == DefaultFontGuid)
            {
                block = MaterialOverrideRegex.Replace(block, "  m_Material: {fileID: 0}");
            }

            if (block != original)
            {
                fixCount++;
            }

            return block;
        });

        localFixCount = fixCount;
        return updated;
    }
}
