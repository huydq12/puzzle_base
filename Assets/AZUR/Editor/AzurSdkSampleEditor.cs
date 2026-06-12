using UnityEditor;
using UnityEngine;

namespace AZUR.Editor
{
    public static class AzurSdkSampleEditor
    {
        [MenuItem("AZUR/Create Sample Behaviour In Scene")]
        public static void CreateSampleInScene()
        {
            var gameObject = new GameObject("AZUR Sample");
            var sample = gameObject.AddComponent<AzurSdkSampleBehaviour>();
            var config = AssetDatabase.LoadAssetAtPath<AzurSdkConfig>("Assets/AZUR/Resources/AzurSdkConfig.asset");

            if (config != null)
            {
                var serializedObject = new SerializedObject(sample);
                serializedObject.FindProperty("config").objectReferenceValue = config;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            Undo.RegisterCreatedObjectUndo(gameObject, "Create AZUR Sample");
            Selection.activeGameObject = gameObject;
        }
    }
}
