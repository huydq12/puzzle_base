using UnityEngine;
using System.Text;

namespace AZUR
{
    public sealed class AzurSdkSampleBehaviour : MonoBehaviour
    {
        [SerializeField] private bool initializeOnStart = true;
        [SerializeField] private AzurSdkConfig config;
        [SerializeField] private bool sendLevelStartOnStart;
        [SerializeField] private int sampleLevelIndex = 1;
        [SerializeField] private string sampleLevelName = "Level_1";
        [SerializeField] private bool logDiagnosticsOnStart = true;
        [SerializeField] private bool showDiagnosticsOverlay = true;
        [SerializeField] private bool showAdTestButtons = true;

        private const float OverlayLeft = 8f;
        private const float OverlayTop = 8f;
        private const float OverlayWidth = 620f;
        private const float OverlayHeight = 430f;
        private const float ButtonTop = 196f;
        private const float ButtonWidth = 145f;
        private const float ButtonHeight = 40f;
        private const float ButtonGap = 10f;
        private const float RuntimeLogTop = 250f;
        private const float RuntimeLogHeight = 170f;

        private void Start()
        {
            if (initializeOnStart && !AzurSdk.IsInitialized)
            {
                if (config == null)
                {
                    config = Resources.Load<AzurSdkConfig>("AzurSdkConfig");
                }

                if (config != null && !config.autoInitialize)
                {
                    AzurSdk.Initialize(config);
                }
            }

            if (sendLevelStartOnStart)
            {
                AzurAnalytics.TrackLevelStart(sampleLevelIndex, sampleLevelName);
            }

            if (logDiagnosticsOnStart)
            {
                LogDiagnostics();
            }
        }

        [ContextMenu("Track Level Start")]
        public void TrackLevelStart()
        {
            AzurAnalytics.TrackLevelStart(sampleLevelIndex, sampleLevelName);
        }

        [ContextMenu("Track Level Complete")]
        public void TrackLevelComplete()
        {
            AzurAnalytics.TrackLevelComplete(sampleLevelIndex, sampleLevelName, 30d);
        }

        [ContextMenu("Track Test Purchase")]
        public void TrackTestPurchase()
        {
            AzurCommerce.TrackPurchase("test_product", "USD", 0.99d, "test-transaction");
        }

        [ContextMenu("Load Interstitial")]
        public void LoadInterstitial()
        {
            AzurAds.LoadInterstitial();
        }

        [ContextMenu("Show Interstitial")]
        public void ShowInterstitial()
        {
            if (!AzurAds.IsInterstitialReady())
            {
                Debug.Log("[AZUR] Interstitial is not ready yet.");
                return;
            }

            AzurAds.ShowInterstitial("sample_placement");
        }

        [ContextMenu("Load Rewarded")]
        public void LoadRewarded()
        {
            AzurAds.LoadRewarded();
        }

        [ContextMenu("Show Rewarded")]
        public void ShowRewarded()
        {
            if (!AzurAds.IsRewardedReady())
            {
                Debug.Log("[AZUR] Rewarded is not ready yet.");
                return;
            }

            AzurAds.ShowRewarded("sample_rewarded");
        }

        [ContextMenu("Show MAX Mediation Debugger")]
        public void ShowMediationDebugger()
        {
            AzurAds.ShowMediationDebugger();
        }

        [ContextMenu("Fetch Remote Config")]
        public void FetchRemoteConfig()
        {
            AzurRemoteConfig.Fetch(success =>
            {
                Debug.Log("[AZUR] RemoteConfig fetched: " + success);
            });
        }

        [ContextMenu("Log Remote Config Sample Values")]
        public void LogRemoteConfigValues()
        {
            Debug.Log(
                "[AZUR] RemoteConfig values | welcome_message=" + AzurRemoteConfig.GetString("welcome_message", "hello") +
                " | feature_enabled=" + AzurRemoteConfig.GetBool("feature_enabled") +
                " | offer_price=" + AzurRemoteConfig.GetInt("offer_price", 0));
        }

        [ContextMenu("Log SDK Diagnostics")]
        public void LogDiagnostics()
        {
            Debug.Log(BuildDiagnosticsText());
        }

        private void OnGUI()
        {
            if (!showDiagnosticsOverlay && !showAdTestButtons)
            {
                return;
            }

            GUI.color = Color.white;

            if (showDiagnosticsOverlay)
            {
                GUI.Box(new Rect(OverlayLeft, OverlayTop, OverlayWidth, OverlayHeight), BuildDiagnosticsText());
                GUI.TextArea(new Rect(OverlayLeft + 8f, RuntimeLogTop, OverlayWidth - 16f, RuntimeLogHeight), BuildRuntimeLogText());
            }

            if (showAdTestButtons)
            {
                DrawAdTestButtons();
            }
        }

        private void DrawAdTestButtons()
        {
            var interReady = AzurAds.IsInterstitialReady();
            var rewardReady = AzurAds.IsRewardedReady();

            if (interReady)
            {
                if (GUI.Button(new Rect(OverlayLeft, ButtonTop, ButtonWidth, ButtonHeight), "Show Inter"))
                {
                    ShowInterstitial();
                    Debug.Log("[AZUR] UI Action: Show Interstitial");
                }
            }
            else
            {
                GUI.Box(new Rect(OverlayLeft, ButtonTop, ButtonWidth, ButtonHeight), "Loading Inter");
            }

            if (rewardReady)
            {
                if (GUI.Button(new Rect(OverlayLeft + (ButtonWidth + ButtonGap), ButtonTop, ButtonWidth, ButtonHeight), "Show Reward"))
                {
                    ShowRewarded();
                    Debug.Log("[AZUR] UI Action: Show Rewarded");
                }
            }
            else
            {
                GUI.Box(new Rect(OverlayLeft + (ButtonWidth + ButtonGap), ButtonTop, ButtonWidth, ButtonHeight), "Loading Reward");
            }

            if (GUI.Button(new Rect(OverlayLeft + ((ButtonWidth + ButtonGap) * 2f), ButtonTop, ButtonWidth, ButtonHeight), "Show Banner"))
            {
                AzurAds.ShowBanner();
                Debug.Log("[AZUR] UI Action: Show Banner");
            }

            if (GUI.Button(new Rect(OverlayLeft + ((ButtonWidth + ButtonGap) * 3f), ButtonTop, ButtonWidth, ButtonHeight), "Hide Banner"))
            {
                AzurAds.HideBanner();
                Debug.Log("[AZUR] UI Action: Hide Banner");
            }

            if (GUI.Button(new Rect(OverlayLeft + ((ButtonWidth + ButtonGap) * 4f), ButtonTop, ButtonWidth, ButtonHeight), "MAX Debugger"))
            {
                ShowMediationDebugger();
                Debug.Log("[AZUR] UI Action: MAX Debugger");
            }

            if (GUI.Button(new Rect(OverlayLeft, ButtonTop + ButtonHeight + ButtonGap, ButtonWidth * 2f, ButtonHeight), "Privacy Settings"))
            {
                _ = AzurUserConsent.ShowPrivacySettingsAsync();
                Debug.Log("[AZUR] UI Action: Privacy Settings");
            }

            if (AzurUserConsent.NeedShowConsentSettingsButton() &&
                GUI.Button(new Rect(OverlayLeft + (ButtonWidth * 2f) + ButtonGap, ButtonTop + ButtonHeight + ButtonGap, ButtonWidth * 2f, ButtonHeight), "Consent Settings"))
            {
                _ = AzurUserConsent.ShowConsentSettingsAsync();
                Debug.Log("[AZUR] UI Action: Consent Settings");
            }
        }

        private static string BuildDiagnosticsText()
        {
            var builder = new StringBuilder();
            builder.AppendLine("[AZUR] Diagnostics");
            builder.AppendLine("Initialized: " + AzurSdk.IsInitialized);
            builder.AppendLine("Consent: " + AzurSdk.HasConsent);
            builder.AppendLine("UserId empty=" + string.IsNullOrWhiteSpace(AzurSdk.UserId));
            builder.AppendLine("UCM available=" + AzurComplianceBridge.HasUserConsentManager());
            builder.AppendLine("App Identity available=" + AzurComplianceBridge.HasApplicationIdentity());

            var max = AzurSdk.AppLovinMaxAdapter;
            if (max != null)
            {
                builder.AppendLine(
                    "MAX | attempted=" + max.InitAttempted +
                    " detected=" + max.SdkDetected +
                    " initialized=" + max.IsSdkInitialized() +
                    " interReady=" + max.IsInterstitialReady +
                    " interLoading=" + max.IsInterstitialLoading +
                    " rewardReady=" + max.IsRewardedReady +
                    " rewardLoading=" + max.IsRewardedLoading +
                    " status=" + max.LastStatus);
            }

            var appsFlyer = AzurSdk.AppsFlyerAdapter;
            if (appsFlyer != null)
            {
                var appsFlyerId = appsFlyer.GetAppsFlyerId();
                builder.AppendLine(
                    "AppsFlyer | attempted=" + appsFlyer.InitAttempted +
                    " detected=" + appsFlyer.SdkDetected +
                    " appsFlyerId=" + (string.IsNullOrWhiteSpace(appsFlyerId) ? "<empty>" : appsFlyerId) +
                    " status=" + appsFlyer.LastStatus);
            }

            if (AzurSdk.Config != null)
            {
                builder.AppendLine("MAX interstitialId empty=" + string.IsNullOrWhiteSpace(AzurSdk.Config.InterstitialId));
                builder.AppendLine("MAX rewardedId empty=" + string.IsNullOrWhiteSpace(AzurSdk.Config.RewardedId));
                builder.AppendLine("MAX bannerId empty=" + string.IsNullOrWhiteSpace(AzurSdk.Config.BannerId));
                builder.AppendLine("MAX mediationDebuggerInDebug=" + AzurSdk.Config.enableMediationDebuggerInDebug);
                builder.AppendLine("AppsFlyer iOS App ID empty=" + string.IsNullOrWhiteSpace(AzurSdk.Config.iosAppId));
                builder.AppendLine("UCM enabled=" + AzurSdk.Config.enableUserConsentManager);
                builder.AppendLine("App Identity enabled=" + AzurSdk.Config.enableApplicationIdentity);
            }

            builder.AppendLine("Log file=" + AzurSdkLog.LogFilePath);

            return builder.ToString();
        }

        private static string BuildRuntimeLogText()
        {
            return "[AZUR] Runtime Log\n" + AzurSdkLog.GetRecentEntriesText();
        }
    }
}
