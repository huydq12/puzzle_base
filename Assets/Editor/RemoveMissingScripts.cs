using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RemoveMissingScripts
{
    [MenuItem("Tools/Cleanup/Remove Missing Scripts In Project")]
    public static void RemoveMissingScriptsInProject()
    {
        try
        {
            RemoveMissingScriptsInAllPrefabs();
            RemoveMissingScriptsInAllScenes();
            EditorUtility.DisplayDialog("Cleanup Complete", "Removed Missing Scripts from prefabs and scenes.", "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    [MenuItem("Tools/Cleanup/Remove Missing Scripts In Open Scene(s)")]
    public static void RemoveMissingScriptsInOpenScenes()
    {
        try
        {
            RemoveMissingScriptsFromOpenScenesOnly();
            EditorUtility.DisplayDialog("Cleanup Complete", "Removed Missing Scripts from open scene(s).", "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private static void RemoveMissingScriptsInAllPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string guid = prefabGuids[i];
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EditorUtility.DisplayProgressBar("Cleaning Prefabs", path, (float)i / Mathf.Max(1, prefabGuids.Length));

            var root = PrefabUtility.LoadPrefabContents(path);
            bool changed = RemoveMissingScriptsFromHierarchy(root);
            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void RemoveMissingScriptsInAllScenes()
    {
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
        // Store opened scenes to restore later
        List<string> originallyOpenScenes = new List<string>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.IsValid() && s.path != null && s.path.Length > 0)
            {
                originallyOpenScenes.Add(s.path);
            }
        }

        string activeScenePath = SceneManager.GetActiveScene().path;

        for (int i = 0; i < sceneGuids.Length; i++)
        {
            string guid = sceneGuids[i];
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EditorUtility.DisplayProgressBar("Cleaning Scenes", path, (float)i / Mathf.Max(1, sceneGuids.Length));

            var openMode = OpenSceneMode.Single;
            var scene = EditorSceneManager.OpenScene(path, openMode);
            bool changed = RemoveMissingScriptsFromScene(scene);
            if (changed)
            {
                EditorSceneManager.SaveScene(scene);
            }
        }

        // Restore originally open scenes
        if (originallyOpenScenes.Count > 0)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            for (int i = 0; i < originallyOpenScenes.Count; i++)
            {
                var mode = i == 0 ? OpenSceneMode.Single : OpenSceneMode.Additive;
                EditorSceneManager.OpenScene(originallyOpenScenes[i], mode);
            }
            if (!string.IsNullOrEmpty(activeScenePath))
            {
                var active = SceneManager.GetSceneByPath(activeScenePath);
                if (active.IsValid())
                {
                    SceneManager.SetActiveScene(active);
                }
            }
        }
    }

    private static void RemoveMissingScriptsFromOpenScenesOnly()
    {
        int openCount = SceneManager.sceneCount;
        for (int i = 0; i < openCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.IsValid()) continue;
            EditorUtility.DisplayProgressBar("Cleaning Open Scene(s)", scene.path, (float)i / Mathf.Max(1, openCount));
            bool changed = RemoveMissingScriptsFromScene(scene);
            if (changed)
            {
                EditorSceneManager.SaveScene(scene);
            }
        }
    }

    private static bool RemoveMissingScriptsFromScene(Scene scene)
    {
        bool changed = false;
        var roots = scene.GetRootGameObjects();
        foreach (var go in roots)
        {
            if (RemoveMissingScriptsFromHierarchy(go))
            {
                changed = true;
            }
        }
        return changed;
    }

    private static bool RemoveMissingScriptsFromHierarchy(GameObject root)
    {
        bool changed = false;
        var stack = new Stack<Transform>();
        stack.Push(root.transform);
        while (stack.Count > 0)
        {
            var t = stack.Pop();
            if (GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject) > 0)
            {
                changed = true;
                EditorUtility.SetDirty(t.gameObject);
            }
            for (int i = 0; i < t.childCount; i++)
            {
                stack.Push(t.GetChild(i));
            }
        }
        return changed;
    }
}


