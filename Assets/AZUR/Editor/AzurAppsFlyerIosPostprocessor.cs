#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace AZUR.Editor
{
    internal static class AzurAppsFlyerIosPostprocessor
    {
        [PostProcessBuild(int.MaxValue)]
        private static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS)
            {
                return;
            }

            if (!AzurAppsFlyerIsEnabled())
            {
                return;
            }

            var plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            if (!File.Exists(plistPath))
            {
                Debug.LogWarning("[AZUR] AppsFlyer iOS postprocess skipped: Info.plist not found.");
                return;
            }

            var plist = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(plistPath));
            plist.root.SetString("NSAdvertisingAttributionReportEndpoint", "https://appsflyer-skadnetwork.com/");
            File.WriteAllText(plistPath, plist.WriteToString());
            Debug.Log("[AZUR] AppsFlyer SCAN postback endpoint added to Info.plist.");
        }

        private static bool AzurAppsFlyerIsEnabled()
        {
            var config = AssetDatabase.LoadAssetAtPath<AzurSdkConfig>("Assets/AZUR/Resources/AzurSdkConfig.asset");
            return config != null && config.enableAppsFlyer;
        }
    }
}
#endif
