using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AZUR.Editor
{
    public static class AzurSdkValidator
    {
        private const string ConfigPath = "Assets/AZUR/Resources/AzurSdkConfig.asset";
        private static readonly Regex GuidRegex = new Regex("^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$");
        private static readonly Regex AdMobAppIdRegex = new Regex("^ca-app-pub-\\d{16}~\\d{10}$");
        private static readonly Regex FacebookAppIdRegex = new Regex("^\\d{10,20}$");
        private static readonly Regex FacebookClientTokenRegex = new Regex("^[0-9a-fA-F]{32}$");

        [MenuItem("AZUR/Validate SDK Setup")]
        public static void ValidateSetup()
        {
            var config = AssetDatabase.LoadAssetAtPath<AzurSdkConfig>(ConfigPath);
            if (config == null)
            {
                Debug.LogError("[AZUR] Missing config asset at " + ConfigPath);
                return;
            }

            var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            var report = BuildReport(config, defines);
            Debug.Log(report);
            Selection.activeObject = config;
        }

        [MenuItem("AZUR/Enable All Define Symbols")]
        public static void EnableAllDefineSymbols()
        {
            UpdateDefines(new[]
            {
                "AZUR_APPLOVIN_MAX",
                "AZUR_FIREBASE",
                "AZUR_APPSFLYER",
                "AZUR_APPMETRICA",
                "AZUR_FACEBOOK"
            }, true);
        }

        [MenuItem("AZUR/Disable All Define Symbols")]
        public static void DisableAllDefineSymbols()
        {
            UpdateDefines(new[]
            {
                "AZUR_APPLOVIN_MAX",
                "AZUR_FIREBASE",
                "AZUR_APPSFLYER",
                "AZUR_APPMETRICA",
                "AZUR_FACEBOOK"
            }, false);
        }

        private static string BuildReport(AzurSdkConfig config, string defineSymbols)
        {
            var warnings = new List<string>();
            var infos = new List<string>();

            ValidateToggle(config.enableAppLovinMax, "AZUR_APPLOVIN_MAX", defineSymbols, warnings);
            ValidateToggle(config.enableFirebase, "AZUR_FIREBASE", defineSymbols, warnings);
            ValidateToggle(config.enableAppsFlyer, "AZUR_APPSFLYER", defineSymbols, warnings);
            ValidateToggle(config.enableAppMetrica, "AZUR_APPMETRICA", defineSymbols, warnings);
            ValidateToggle(config.enableFacebook, "AZUR_FACEBOOK", defineSymbols, warnings);

            if (config.enableUserConsentManager)
            {
                ValidatePackage(AzurSdkPackageProbe.HasUserConsentManager(), "User Consent Manager", warnings);
                if (!AzurSdkPackageProbe.HasUserConsentSettingsAsset())
                {
                    warnings.Add("User Consent Settings asset is missing from a Resources folder.");
                }

                if (!AzurSdkPackageProbe.HasGoogleUmpBundle())
                {
                    warnings.Add("Google UMP bundle is missing. UCM requires Assets/Plugins/GoogleUMP or your own GoogleMobileAds setup.");
                }
            }

            if (config.enableApplicationIdentity)
            {
                ValidatePackage(AzurSdkPackageProbe.HasApplicationIdentity(), "Application Identity", warnings);
            }

            if (config.enableAppLovinMax)
            {
                ValidateRequired(config.appLovinSdkKey, "AppLovin SDK Key", warnings);
                ValidateOptionalFormat(config.adMobAndroidAppId, "AdMob Android App ID", AdMobAppIdRegex, warnings);
                ValidateOptionalFormat(config.adMobIosAppId, "AdMob iOS App ID", AdMobAppIdRegex, warnings);
                ValidateRequired(config.androidInterstitialId, "Android Interstitial Id", warnings);
                ValidateRequired(config.iosInterstitialId, "iOS Interstitial Id", warnings);
                ValidateRequired(config.androidRewardedId, "Android Rewarded Id", warnings);
                ValidateRequired(config.iosRewardedId, "iOS Rewarded Id", warnings);
                ValidatePackage(AzurSdkPackageProbe.HasAppLovinMax(), "AppLovin MAX", warnings);
            }

            if (config.enableFirebase)
            {
                ValidatePackage(AzurSdkPackageProbe.HasFirebase(), "Firebase", warnings);
                if (config.enableRemoteConfig)
                {
                    ValidateRemoteConfig(config, warnings);
                }
            }

            if (config.enableAppsFlyer)
            {
                ValidateRequired(config.appsFlyerDevKey, "AppsFlyer Dev Key", warnings);
                ValidateRequired(config.iosAppId, "AppsFlyer iOS App Id", warnings);
                ValidatePackage(AzurSdkPackageProbe.HasAppsFlyer(), "AppsFlyer", warnings);
            }

            if (config.enableAppMetrica)
            {
                ValidateRequired(config.appMetricaApiKey, "AppMetrica API Key", warnings);
                ValidateOptionalFormat(config.appMetricaApiKey, "AppMetrica API Key", GuidRegex, warnings);
                ValidatePackage(AzurSdkPackageProbe.HasAppMetrica(), "AppMetrica", warnings);
            }

            if (config.enableFacebook)
            {
                ValidateRequired(config.facebookAppId, "Facebook App ID", warnings);
                ValidateRequired(config.facebookClientToken, "Facebook Client Token", warnings);
                ValidateOptionalFormat(config.facebookAppId, "Facebook App ID", FacebookAppIdRegex, warnings);
                ValidateOptionalFormat(config.facebookClientToken, "Facebook Client Token", FacebookClientTokenRegex, warnings);
                ValidatePackage(AzurSdkPackageProbe.HasFacebook(), "Facebook SDK", warnings);
            }

            infos.Add("Current defines: " + defineSymbols);
            infos.Add("Default consent: " + config.defaultConsent);
            infos.Add("Configured user id: " + (string.IsNullOrWhiteSpace(config.userId) ? "<empty>" : config.userId));
            infos.Add("User Consent Manager enabled: " + config.enableUserConsentManager);
            infos.Add("Application Identity enabled: " + config.enableApplicationIdentity);

            var builder = new StringBuilder();
            builder.AppendLine("[AZUR] Validation Report");

            if (warnings.Count == 0)
            {
                builder.AppendLine("Status: OK");
            }
            else
            {
                builder.AppendLine("Status: Warnings");
                foreach (var warning in warnings)
                {
                    builder.AppendLine("- " + warning);
                }
            }

            foreach (var info in infos)
            {
                builder.AppendLine("- " + info);
            }

            return builder.ToString();
        }

        private static void ValidateToggle(bool isEnabled, string defineSymbol, string defineSymbols, List<string> warnings)
        {
            if (!isEnabled)
            {
                return;
            }

            if (!ContainsDefine(defineSymbols, defineSymbol))
            {
                warnings.Add("Config enables " + defineSymbol + " but the define symbol is missing.");
            }
        }

        private static void ValidateRequired(string value, string label, List<string> warnings)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                warnings.Add(label + " is empty.");
            }
        }

        private static void ValidatePackage(bool packageFound, string label, List<string> warnings)
        {
            if (!packageFound)
            {
                warnings.Add(label + " package footprint was not found in Assets/Packages.");
            }
        }

        private static void ValidateOptionalFormat(string value, string label, Regex regex, List<string> warnings)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (!regex.IsMatch(value))
            {
                warnings.Add(label + " has an unexpected format.");
            }
        }

        private static void ValidateRemoteConfig(AzurSdkConfig config, List<string> warnings)
        {
            if (config.remoteConfigFetchTimeoutSeconds < 0)
            {
                warnings.Add("Remote Config fetch timeout must be >= 0.");
            }

            if (config.remoteConfigMinimumFetchIntervalSeconds < 0)
            {
                warnings.Add("Remote Config minimum fetch interval must be >= 0.");
            }

            if (string.IsNullOrWhiteSpace(config.remoteConfigDefaultsJson))
            {
                warnings.Add("Remote Config defaults json is empty.");
            }
        }

        private static void UpdateDefines(string[] symbols, bool enable)
        {
            var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            var updated = defines;

            for (var index = 0; index < symbols.Length; index++)
            {
                updated = enable
                    ? AddDefine(updated, symbols[index])
                    : RemoveDefine(updated, symbols[index]);
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, updated);
            Debug.Log("[AZUR] Updated define symbols: " + updated);
        }

        private static bool ContainsDefine(string defines, string symbol)
        {
            var parts = defines.Split(';');
            for (var index = 0; index < parts.Length; index++)
            {
                if (parts[index] == symbol)
                {
                    return true;
                }
            }

            return false;
        }

        private static string AddDefine(string defines, string symbol)
        {
            if (ContainsDefine(defines, symbol))
            {
                return defines;
            }

            return string.IsNullOrWhiteSpace(defines) ? symbol : defines + ";" + symbol;
        }

        private static string RemoveDefine(string defines, string symbol)
        {
            var parts = new List<string>(defines.Split(';'));
            parts.RemoveAll(item => item == symbol || string.IsNullOrWhiteSpace(item));
            return string.Join(";", parts);
        }
    }
}
