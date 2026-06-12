using System.Collections.Generic;
using UnityEngine;

namespace AZUR
{
    internal sealed class AzurAppsFlyerAdapter : IAzurSdkAdapter
    {
        private readonly bool _isEnabled;

        public bool InitAttempted { get; private set; }
        public bool SdkDetected { get; private set; }
        public string LastStatus { get; private set; } = "Not initialized.";

        public AzurAppsFlyerAdapter(bool isEnabled)
        {
            _isEnabled = isEnabled;
        }

        public string Name => "AppsFlyer";
        public bool IsEnabled => _isEnabled;

        public void Initialize(AzurSdkConfig config)
        {
            InitAttempted = true;
#if AZUR_APPSFLYER
            SdkDetected = AzurReflection.HasType("AppsFlyerSDK.AppsFlyer");
            if (!SdkDetected)
            {
                LastStatus = "AppsFlyerSDK.AppsFlyer type not found.";
                AzurSdkLog.Warn("`AZUR_APPSFLYER` is defined but AppsFlyer SDK is not imported.");
                return;
            }

            AzurReflection.InvokeStatic("AppsFlyerSDK.AppsFlyer", "initSDK", config.appsFlyerDevKey, config.iosAppId);
            if (!string.IsNullOrWhiteSpace(config.userId))
            {
                AzurReflection.InvokeStatic("AppsFlyerSDK.AppsFlyer", "setCustomerUserId", config.userId);
            }

            AzurReflection.InvokeStatic("AppsFlyerSDK.AppsFlyer", "setIsDebug", Debug.isDebugBuild && config.enableVerboseLogs);
#if UNITY_IOS && !UNITY_EDITOR
            AzurReflection.InvokeStatic("AppsFlyerSDK.AppsFlyer", "waitForATTUserAuthorizationWithTimeoutInterval", 30);
#endif
            AzurReflection.InvokeStatic("AppsFlyerSDK.AppsFlyer", "startSDK");
            LastStatus = "startSDK invoked.";
            AzurSdkLog.Info("AppsFlyer init invoked. AppsFlyerId=" + GetAppsFlyerId());
#else
            LastStatus = "`AZUR_APPSFLYER` define is missing.";
            AzurSdkLog.Warn("AppsFlyer is enabled in config but `AZUR_APPSFLYER` is not defined.");
#endif
        }

        public string GetAppsFlyerId()
        {
#if AZUR_APPSFLYER
            var value = AzurReflection.InvokeStatic("AppsFlyerSDK.AppsFlyer", "getAppsFlyerId");
            return value as string ?? string.Empty;
#else
            return string.Empty;
#endif
        }

        public void SetConsent(bool hasConsent)
        {
        }

        public void SetUserId(string userId)
        {
#if AZUR_APPSFLYER
            if (!string.IsNullOrWhiteSpace(userId))
            {
                AzurReflection.InvokeStatic("AppsFlyerSDK.AppsFlyer", "setCustomerUserId", userId);
            }
#endif
        }

        public void TrackEvent(AzurCustomEvent customEvent)
        {
#if AZUR_APPSFLYER
            var values = new Dictionary<string, string>();
            if (customEvent.Parameters != null)
            {
                foreach (var pair in customEvent.Parameters)
                {
                    values[pair.Key] = AzurSdkUtility.ToInvariantString(pair.Value);
                }
            }

            AzurReflection.InvokeStatic("AppsFlyerSDK.AppsFlyer", "sendEvent", customEvent.Name, values);
#endif
        }

        public void TrackPurchase(AzurPurchaseEvent purchaseEvent)
        {
#if AZUR_APPSFLYER
            if (purchaseEvent.IsSubscription)
            {
                LastStatus = "Subscription purchase was ignored. Purchase Connector flow is not configured in AZUR yet.";
                AzurSdkLog.Warn(LastStatus);
                return;
            }

            var values = new Dictionary<string, string>
            {
                ["af_content_id"] = purchaseEvent.ProductId ?? string.Empty,
                ["af_currency"] = purchaseEvent.Currency ?? "USD",
                ["af_revenue"] = purchaseEvent.Revenue.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["af_transaction_id"] = purchaseEvent.TransactionId ?? string.Empty,
                ["af_quantity"] = purchaseEvent.Quantity.ToString()
            };

            AzurReflection.InvokeStatic("AppsFlyerSDK.AppsFlyer", "sendEvent", "af_purchase", values);
            LastStatus = "af_purchase sent.";
#endif
        }

        public void TrackAdRevenue(AzurAdRevenueEvent adRevenueEvent)
        {
#if AZUR_APPSFLYER
            var values = new Dictionary<string, string>
            {
                ["af_adrev_ad_type"] = adRevenueEvent.AdFormat ?? string.Empty,
                ["af_adrev_network_name"] = adRevenueEvent.AdSource ?? string.Empty,
                ["af_adrev_mediation_network"] = adRevenueEvent.AdPlatform ?? string.Empty,
                ["af_revenue"] = adRevenueEvent.Revenue.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["af_currency"] = adRevenueEvent.Currency ?? "USD"
            };

            AzurReflection.InvokeStatic("AppsFlyerSDK.AppsFlyer", "sendEvent", "af_ad_revenue", values);
            LastStatus = "af_ad_revenue sent.";
#endif
        }
    }
}
