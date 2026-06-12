using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AZUR
{
    public static class AzurSdk
    {
        private static readonly List<IAzurSdkAdapter> Adapters = new List<IAzurSdkAdapter>();
        private static AzurAppLovinMaxAdapter _appLovinMaxAdapter;
        private static AzurAppsFlyerAdapter _appsFlyerAdapter;

        public static AzurSdkConfig Config { get; private set; }
        public static bool IsInitialized { get; private set; }
        public static bool HasConsent { get; private set; }
        public static string UserId { get; private set; }

        public static event Action Initialized;

        public static void Initialize(AzurSdkConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            Config = config;
            HasConsent = config.defaultConsent;
            UserId = config.userId;
            AzurSdkLog.Verbose = config.enableVerboseLogs;
            AzurRemoteConfig.Initialize(config);

            Adapters.Clear();
            RegisterAdapters(config);

            foreach (var adapter in Adapters)
            {
                if (!adapter.IsEnabled)
                {
                    AzurSdkLog.Info("Skipped disabled adapter: " + adapter.Name);
                    continue;
                }

                try
                {
                    adapter.Initialize(config);
                    adapter.SetConsent(HasConsent);
                    if (!string.IsNullOrWhiteSpace(UserId))
                    {
                        adapter.SetUserId(UserId);
                    }

                    AzurSdkLog.Info("Initialized adapter: " + adapter.Name);
                }
                catch (Exception exception)
                {
                    AzurSdkLog.Error("Adapter init failed: " + adapter.Name + " | " + exception.Message);
                }
            }

            IsInitialized = true;
            Initialized?.Invoke();
            AzurSdkLog.Info("SDK initialized.");
        }

        public static void SetConsent(bool hasConsent)
        {
            HasConsent = hasConsent;
            Broadcast(adapter => adapter.SetConsent(hasConsent));
            AzurSdkLog.Info("Consent updated: " + hasConsent);
        }

        public static void SetUserId(string userId)
        {
            UserId = userId;
            Broadcast(adapter => adapter.SetUserId(userId));
            AzurSdkLog.Info("UserId updated: " + userId);
        }

        public static void TrackEvent(string eventName, IReadOnlyDictionary<string, object> parameters = null)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }

            var customEvent = new AzurCustomEvent(eventName, parameters);
            Broadcast(adapter => adapter.TrackEvent(customEvent));
            AzurSdkLog.Info("TrackEvent: " + eventName + " " + AzurSdkUtility.ParamsToLog(parameters));
        }

        public static void TrackPurchase(AzurPurchaseEvent purchaseEvent)
        {
            Broadcast(adapter => adapter.TrackPurchase(purchaseEvent));
            AzurSdkLog.Info(
                "TrackPurchase: " + purchaseEvent.ProductId +
                " revenue=" + purchaseEvent.Revenue.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                " currency=" + purchaseEvent.Currency);
        }

        public static void TrackAdRevenue(AzurAdRevenueEvent adRevenueEvent)
        {
            Broadcast(adapter => adapter.TrackAdRevenue(adRevenueEvent));
            AzurSdkLog.Info(
                "TrackAdRevenue: " + adRevenueEvent.AdFormat +
                " revenue=" + adRevenueEvent.Revenue.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        internal static void ForwardAdRevenue(AzurAdRevenueEvent adRevenueEvent)
        {
            TrackAdRevenue(adRevenueEvent);
        }

        private static void Broadcast(Action<IAzurSdkAdapter> action)
        {
            foreach (var adapter in Adapters)
            {
                if (!adapter.IsEnabled)
                {
                    continue;
                }

                try
                {
                    action(adapter);
                }
                catch (Exception exception)
                {
                    AzurSdkLog.Error("Adapter call failed: " + adapter.Name + " | " + exception.Message);
                }
            }
        }

        private static void RegisterAdapters(AzurSdkConfig config)
        {
            _appLovinMaxAdapter = new AzurAppLovinMaxAdapter(config.enableAppLovinMax);
            _appsFlyerAdapter = new AzurAppsFlyerAdapter(config.enableAppsFlyer);
            Adapters.Add(_appLovinMaxAdapter);
            Adapters.Add(new AzurFirebaseAdapter(config.enableFirebase));
            Adapters.Add(_appsFlyerAdapter);
            Adapters.Add(new AzurAppMetricaAdapter(config.enableAppMetrica));
            Adapters.Add(new AzurFacebookAdapter(config.enableFacebook));
        }

        internal static AzurAppLovinMaxAdapter AppLovinMaxAdapter => _appLovinMaxAdapter;
        internal static AzurAppsFlyerAdapter AppsFlyerAdapter => _appsFlyerAdapter;
    }

    public sealed class AzurSdkBootstrap : MonoBehaviour
    {
        private const int NextSceneBuildIndex = 1;
        private static AzurSdkBootstrap _instance;
        [SerializeField] private AzurSdkConfig config;
        private bool _isBootstrapping;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private async void Start()
        {
            if (config == null)
            {
                config = Resources.Load<AzurSdkConfig>("AzurSdkConfig");
            }

            if (config == null)
            {
                AzurSdkLog.Warn("Bootstrap has no config assigned.");
                return;
            }

            if (_isBootstrapping || AzurSdk.IsInitialized || !config.autoInitialize)
            {
                return;
            }

            _isBootstrapping = true;

            try
            {
                var runtimeConfig = Instantiate(config);
                runtimeConfig.hideFlags = HideFlags.DontSave;
                runtimeConfig.defaultConsent = await AzurComplianceBridge.ResolveConsentAsync(
                    runtimeConfig.enableUserConsentManager,
                    runtimeConfig.userConsentUseGooglePlayPass,
                    runtimeConfig.defaultConsent);

                var applicationIdentity = await AzurComplianceBridge.RequestApplicationIdentityAsync(
                    runtimeConfig.enableApplicationIdentity);
                if (!string.IsNullOrWhiteSpace(applicationIdentity))
                {
                    runtimeConfig.userId = applicationIdentity;
                }

                AzurSdk.Initialize(runtimeConfig);
                LoadNextSceneIfNeeded();
            }
            finally
            {
                _isBootstrapping = false;
            }
        }

        public void SetConsent(bool hasConsent)
        {
            AzurSdk.SetConsent(hasConsent);
        }

        public void SetUserId(string userId)
        {
            AzurSdk.SetUserId(userId);
        }

        public bool NeedShowConsentSettingsButton()
        {
            return AzurComplianceBridge.NeedShowConsentSettingsButton();
        }

        public async void ShowPrivacySettings()
        {
            await AzurComplianceBridge.ShowPrivacySettingsAsync();
        }

        public async void ShowConsentSettings()
        {
            await AzurComplianceBridge.ShowConsentSettingsAsync();
        }

        private static void LoadNextSceneIfNeeded()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex == NextSceneBuildIndex)
            {
                return;
            }

            if (NextSceneBuildIndex < 0 || NextSceneBuildIndex >= SceneManager.sceneCountInBuildSettings)
            {
                AzurSdkLog.Warn("Next scene build index is invalid: " + NextSceneBuildIndex);
                return;
            }

            AzurSdkLog.Info("Bootstrap completed. Loading scene index " + NextSceneBuildIndex + ".");
            SceneManager.LoadScene(NextSceneBuildIndex);
        }
    }

    public static class AzurAds
    {
        private static Action _pendingRewardGranted;
        private static Action _pendingRewardClosedWithoutGrant;
        private static bool _rewardGrantedForActiveShow;

        public static void LoadInterstitial()
        {
            AzurSdk.AppLovinMaxAdapter?.LoadInterstitial();
        }

        public static void ShowInterstitial(string placement = null)
        {
            AzurSdk.AppLovinMaxAdapter?.ShowInterstitial(placement);
        }

        public static void LoadRewarded()
        {
            AzurSdk.AppLovinMaxAdapter?.LoadRewarded();
        }

        public static void ShowRewarded(string placement = null)
        {
            AzurSdk.AppLovinMaxAdapter?.ShowRewarded(placement);
        }

        public static bool ShowRewarded(Action onRewardGranted, string placement = null, Action onUnavailable = null, Action onClosedWithoutGrant = null)
        {
            if (AzurSdk.AppLovinMaxAdapter == null || !AzurSdk.AppLovinMaxAdapter.IsRewardedReady)
            {
                return false;
            }

            ClearPendingRewardCallbacks();
            _pendingRewardGranted = onRewardGranted;
            _pendingRewardClosedWithoutGrant = onClosedWithoutGrant;
            _rewardGrantedForActiveShow = false;
            AzurSdk.AppLovinMaxAdapter.ShowRewarded(placement);
            return true;
        }

        public static void ShowBanner()
        {
            AzurSdk.AppLovinMaxAdapter?.ShowBanner();
        }

        public static bool IsInterstitialReady()
        {
            return AzurSdk.AppLovinMaxAdapter != null && AzurSdk.AppLovinMaxAdapter.IsInterstitialReady;
        }

        public static bool IsRewardedReady()
        {
            return AzurSdk.AppLovinMaxAdapter != null && AzurSdk.AppLovinMaxAdapter.IsRewardedReady;
        }

        public static void HideBanner()
        {
            AzurSdk.AppLovinMaxAdapter?.HideBanner();
        }

        public static void ShowMediationDebugger()
        {
            AzurSdk.AppLovinMaxAdapter?.ShowMediationDebugger();
        }

        internal static void NotifyRewardedGranted()
        {
            if (_rewardGrantedForActiveShow)
            {
                return;
            }

            _rewardGrantedForActiveShow = true;
            _pendingRewardGranted?.Invoke();
        }

        internal static void NotifyRewardedClosed()
        {
            if (!_rewardGrantedForActiveShow)
            {
                _pendingRewardClosedWithoutGrant?.Invoke();
            }

            ClearPendingRewardCallbacks();
        }

        internal static void NotifyRewardedDisplayFailed()
        {
            if (!_rewardGrantedForActiveShow)
            {
                _pendingRewardClosedWithoutGrant?.Invoke();
            }

            ClearPendingRewardCallbacks();
        }

        private static void ClearPendingRewardCallbacks()
        {
            _pendingRewardGranted = null;
            _pendingRewardClosedWithoutGrant = null;
            _rewardGrantedForActiveShow = false;
        }
    }

    public static class AzurUserConsent
    {
        public static bool NeedShowConsentSettingsButton()
        {
            return AzurComplianceBridge.NeedShowConsentSettingsButton();
        }

        public static Task ShowPrivacySettingsAsync()
        {
            return AzurComplianceBridge.ShowPrivacySettingsAsync();
        }

        public static Task ShowConsentSettingsAsync()
        {
            return AzurComplianceBridge.ShowConsentSettingsAsync();
        }
    }
}
