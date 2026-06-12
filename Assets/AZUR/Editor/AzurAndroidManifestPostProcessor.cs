#if UNITY_ANDROID
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor.Android;

namespace AZUR.Editor
{
    internal sealed class AzurAndroidManifestPostProcessor : IPostGenerateGradleAndroidProject
    {
        private static readonly XNamespace AndroidNs = "http://schemas.android.com/apk/res/android";
        private static readonly XNamespace ToolsNs = "http://schemas.android.com/tools";

        public int callbackOrder => 1000;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            ForceHardwareAcceleration(Path.Combine(path, "src/main/AndroidManifest.xml"));
            ForceHardwareAcceleration(Path.Combine(path, "../launcher/src/main/AndroidManifest.xml"));
        }

        private static void ForceHardwareAcceleration(string manifestPath)
        {
            if (!File.Exists(manifestPath))
            {
                return;
            }

            var document = XDocument.Load(manifestPath);
            var manifest = document.Root;
            var application = manifest?.Element("application");
            if (manifest == null || application == null)
            {
                return;
            }

            manifest.SetAttributeValue(XNamespace.Xmlns + "tools", ToolsNs);

            application.SetAttributeValue(AndroidNs + "hardwareAccelerated", "true");
            application.SetAttributeValue(ToolsNs + "replace", MergeReplaceValue(application.Attribute(ToolsNs + "replace")?.Value, "android:hardwareAccelerated"));

            var unityActivity = application.Elements("activity")
                .FirstOrDefault(element => (string) element.Attribute(AndroidNs + "name") == "com.unity3d.player.UnityPlayerActivity");

            if (unityActivity != null)
            {
                unityActivity.SetAttributeValue(AndroidNs + "hardwareAccelerated", "true");
                unityActivity.SetAttributeValue(ToolsNs + "replace", MergeReplaceValue(unityActivity.Attribute(ToolsNs + "replace")?.Value, "android:hardwareAccelerated"));
            }

            document.Save(manifestPath);
        }

        private static string MergeReplaceValue(string currentValue, string valueToAdd)
        {
            if (string.IsNullOrWhiteSpace(currentValue))
            {
                return valueToAdd;
            }

            var values = currentValue
                .Split(',')
                .Select(value => value.Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList();

            if (!values.Contains(valueToAdd))
            {
                values.Add(valueToAdd);
            }

            return string.Join(",", values);
        }
    }
}
#endif
