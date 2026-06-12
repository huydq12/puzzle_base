using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AZUR.Editor
{
    public static class AzurVendorSettingsSync
    {
        private const string ConfigPath = "Assets/AZUR/Resources/AzurSdkConfig.asset";

        [MenuItem("AZUR/Sync Vendor Settings From Config")]
        public static void SyncFromConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<AzurSdkConfig>(ConfigPath);
            if (config == null)
            {
                Debug.LogError("[AZUR] Missing config asset at " + ConfigPath);
                return;
            }

            SyncAppLovin(config);
            SyncFacebook(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AZUR] Synced vendor settings from AZUR config.");
        }

        private static void SyncAppLovin(AzurSdkConfig config)
        {
            var settingsType = Type.GetType("AppLovinSettings");
            if (settingsType == null)
            {
                return;
            }

            var instance = settingsType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            if (instance == null)
            {
                return;
            }

            SetProperty(instance, "SdkKey", config.appLovinSdkKey);
            SetProperty(instance, "AdMobAndroidAppId", config.adMobAndroidAppId);
            SetProperty(instance, "AdMobIosAppId", config.adMobIosAppId);
            EditorUtility.SetDirty((UnityEngine.Object)instance);
        }

        private static void SyncFacebook(AzurSdkConfig config)
        {
            var settingsType = Type.GetType("Facebook.Unity.Settings.FacebookSettings, Facebook.Unity.Settings");
            if (settingsType == null)
            {
                return;
            }

            var instance = settingsType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            if (instance == null)
            {
                return;
            }

            if (!TrySetProperty(instance, "AppId", config.facebookAppId))
            {
                SetStringArrayProperty(instance, "AppIds", config.facebookAppId);
            }

            if (!TrySetProperty(instance, "ClientToken", config.facebookClientToken))
            {
                SetStringArrayProperty(instance, "ClientTokens", config.facebookClientToken);
            }

            EditorUtility.SetDirty((UnityEngine.Object)instance);
        }

        private static void SetProperty(object instance, string propertyName, string value)
        {
            TrySetProperty(instance, propertyName, value);
        }

        private static bool TrySetProperty(object instance, string propertyName, string value)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property == null || !property.CanWrite)
            {
                return false;
            }

            property.SetValue(instance, value ?? string.Empty);
            return true;
        }

        private static void SetStringArrayProperty(object instance, string propertyName, string value)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property == null || !property.CanWrite)
            {
                return;
            }

            var current = property.GetValue(instance) as string[];
            if (current == null || current.Length == 0)
            {
                property.SetValue(instance, new[] { value ?? string.Empty });
                return;
            }

            for (var index = 0; index < current.Length; index++)
            {
                current[index] = value ?? string.Empty;
            }

            property.SetValue(instance, current);
        }
    }
}
