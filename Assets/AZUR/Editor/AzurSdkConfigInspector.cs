using UnityEditor;
using UnityEngine;

namespace AZUR.Editor
{
    [CustomEditor(typeof(AzurSdkConfig))]
    public sealed class AzurSdkConfigInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Validate SDK Setup"))
            {
                AzurSdkValidator.ValidateSetup();
            }

            if (GUILayout.Button("Enable All Define Symbols"))
            {
                AzurSdkValidator.EnableAllDefineSymbols();
            }

            if (GUILayout.Button("Sync Vendor Settings From Config"))
            {
                AzurVendorSettingsSync.SyncFromConfig();
            }

            if (GUILayout.Button("Create Bootstrap In Scene"))
            {
                AzurSdkEditor.CreateBootstrapInScene();
            }

            if (GUILayout.Button("Create Sample Behaviour In Scene"))
            {
                AzurSdkSampleEditor.CreateSampleInScene();
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "If Remote Config is enabled, keep default values in `remoteConfigDefaultsJson` so the game can run without a network fetch.",
                MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
