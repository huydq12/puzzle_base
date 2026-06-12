using System.IO;
using UnityEditor;
using UnityEngine;

namespace AZUR.Editor
{
    public static class AzurSdkEditor
    {
        private const string ConfigFolder = "Assets/AZUR/Resources";
        private const string ConfigPath = ConfigFolder + "/AzurSdkConfig.asset";

        [MenuItem("AZUR/Create SDK Config")]
        public static void CreateConfig()
        {
            if (!Directory.Exists(ConfigFolder))
            {
                Directory.CreateDirectory(ConfigFolder);
            }

            var config = AssetDatabase.LoadAssetAtPath<AzurSdkConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<AzurSdkConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
                AssetDatabase.SaveAssets();
            }

            EditorGUIUtility.PingObject(config);
            Selection.activeObject = config;
        }

        [MenuItem("AZUR/Create Bootstrap In Scene")]
        public static void CreateBootstrapInScene()
        {
            var config = AssetDatabase.LoadAssetAtPath<AzurSdkConfig>(ConfigPath);
            if (config == null)
            {
                CreateConfig();
                config = AssetDatabase.LoadAssetAtPath<AzurSdkConfig>(ConfigPath);
            }

            var gameObject = new GameObject("AZUR SDK");
            var bootstrap = gameObject.AddComponent<AzurSdkBootstrap>();
            var serializedObject = new SerializedObject(bootstrap);
            serializedObject.FindProperty("config").objectReferenceValue = config;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(gameObject, "Create AZUR SDK");
            Selection.activeGameObject = gameObject;
        }

        [MenuItem("AZUR/Log Active Define Symbols")]
        public static void LogDefineSymbols()
        {
            var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            Debug.Log($"[AZUR] BuildTargetGroup={targetGroup} Defines={defines}");
        }
    }
}
