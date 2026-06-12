using UnityEngine;

namespace AZUR
{
    [CreateAssetMenu(fileName = "AzurSdkConfig", menuName = "AZUR/SDK Config")]
    public sealed class AzurSdkConfig : ScriptableObject
    {
        [Header("General")]
        public bool autoInitialize = true;
        public bool enableVerboseLogs = true;
        public bool defaultConsent = false;
        public bool doNotSell = false;
        public string userId;

        [Header("Compliance")]
        public bool enableUserConsentManager = true;
        public bool userConsentUseGooglePlayPass = false;
        public bool enableApplicationIdentity = true;

        [Header("AppLovin MAX")]
        public bool enableAppLovinMax = false;
        public bool enableCreativeDebuggerInDebug = true;
        public bool enableMediationDebuggerInDebug = true;
        public string appLovinSdkKey;
        public string adMobAndroidAppId;
        public string adMobIosAppId;
        public string androidInterstitialId;
        public string iosInterstitialId;
        public string androidRewardedId;
        public string iosRewardedId;
        public string androidBannerId;
        public string iosBannerId;

        [Header("Firebase")]
        public bool enableFirebase = true;
        public bool enableRemoteConfig = false;
        public int remoteConfigFetchTimeoutSeconds = 30;
        public int remoteConfigMinimumFetchIntervalSeconds = 3600;
        [TextArea(3, 12)] public string remoteConfigDefaultsJson = "{\n  \"welcome_message\": \"hello\",\n  \"feature_enabled\": false,\n  \"offer_price\": 0\n}";

        [Header("AppsFlyer")]
        public bool enableAppsFlyer = false;
        public string appsFlyerDevKey;
        public string iosAppId;

        [Header("AppMetrica")]
        public bool enableAppMetrica = false;
        public string appMetricaApiKey;
        public int appMetricaSessionTimeoutSeconds = 300;
        public bool appMetricaCrashReporting = true;
        public bool appMetricaLogs = true;
        public bool appMetricaLocationTracking = false;
        public bool appMetricaHandleFirstActivationAsUpdate = false;

        [Header("Facebook")]
        public bool enableFacebook = true;
        public string facebookAppId;
        public string facebookClientToken;
        public int facebookRevenueBufferSize = 15;

        public string InterstitialId
        {
            get
            {
#if UNITY_IOS
                return iosInterstitialId;
#else
                return androidInterstitialId;
#endif
            }
        }

        public string RewardedId
        {
            get
            {
#if UNITY_IOS
                return iosRewardedId;
#else
                return androidRewardedId;
#endif
            }
        }

        public string BannerId
        {
            get
            {
#if UNITY_IOS
                return iosBannerId;
#else
                return androidBannerId;
#endif
            }
        }
    }
}
