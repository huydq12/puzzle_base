using System.Collections.Generic;
using UnityEditor;

namespace AZUR.Editor
{
    internal static class AzurSdkPackageProbe
    {
        public static bool HasAppLovinMax()
        {
            return HasAnyAsset(
                "MaxSdk.cs",
                "MaxSdkCallbacks.cs",
                "AppLovinSettings.asset");
        }

        public static bool HasFirebase()
        {
            return HasAnyAsset(
                "FirebaseApp.cs",
                "FirebaseAnalytics.cs",
                "google-services.json",
                "GoogleService-Info.plist");
        }

        public static bool HasAppsFlyer()
        {
            return HasAnyAsset(
                "AppsFlyer.cs",
                "AppsFlyerObjectScript.cs",
                "AppsFlyerInfo.plist");
        }

        public static bool HasAppMetrica()
        {
            return HasAnyAsset(
                "AppMetrica.cs",
                "AppMetricaConfig.cs",
                "YandexMobileMetrica");
        }

        public static bool HasFacebook()
        {
            return HasAnyAsset(
                "FB.cs",
                "Facebook.Unity.asmdef",
                "FacebookSettings.asset");
        }

        public static bool HasUserConsentManager()
        {
            return HasAnyAsset(
                "UserConsentManager.cs",
                "UserConsentSettings.cs",
                "UCM.asmdef");
        }

        public static bool HasApplicationIdentity()
        {
            return HasAnyAsset(
                "ApplicationIdentity.cs",
                "Azur.Application.Identity.asmdef");
        }

        public static bool HasUserConsentSettingsAsset()
        {
            return HasAnyAsset("UserConsentSettings.asset");
        }

        public static bool HasGoogleUmpBundle()
        {
            return HasAnyAsset(
                "GoogleMobileAds.Ump.dll",
                "GoogleMobileAds.dll",
                "ump.aar");
        }

        private static bool HasAnyAsset(params string[] names)
        {
            for (var index = 0; index < names.Length; index++)
            {
                if (FindAssets(names[index]).Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<string> FindAssets(string token)
        {
            var guids = AssetDatabase.FindAssets(token);
            var result = new List<string>(guids.Length);
            for (var index = 0; index < guids.Length; index++)
            {
                result.Add(AssetDatabase.GUIDToAssetPath(guids[index]));
            }

            return result;
        }
    }
}
