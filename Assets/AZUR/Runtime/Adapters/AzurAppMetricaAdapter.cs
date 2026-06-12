using UnityEngine;
using System.Collections.Generic;
#if AZUR_APPMETRICA
using Io.AppMetrica;
#endif

namespace AZUR
{
    internal sealed class AzurAppMetricaAdapter : IAzurSdkAdapter
    {
        private readonly bool _isEnabled;
        private const string LaunchMarkerKey = "AZUR_APPMETRICA_LAUNCH_MARKER";

        public string LastStatus { get; private set; } = "Not initialized.";

        public AzurAppMetricaAdapter(bool isEnabled)
        {
            _isEnabled = isEnabled;
        }

        public string Name => "AppMetrica";
        public bool IsEnabled => _isEnabled;

        public void Initialize(AzurSdkConfig config)
        {
#if AZUR_APPMETRICA
            try
            {
                var appMetricaConfig = new AppMetricaConfig(config.appMetricaApiKey)
                {
                    CrashReporting = config.appMetricaCrashReporting,
                    SessionTimeout = config.appMetricaSessionTimeoutSeconds,
                    LocationTracking = config.appMetricaLocationTracking,
                    Logs = config.appMetricaLogs,
                    FirstActivationAsUpdate = ShouldHandleFirstActivationAsUpdate(config),
                    UserProfileID = config.userId,
                    DataSendingEnabled = config.defaultConsent
                };

                AppMetrica.Activate(appMetricaConfig);
                SaveLaunchMarker();
                LastStatus = "AppMetrica activated.";
                AzurSdkLog.Info("AppMetrica activated. IsActivated=" + AppMetrica.IsActivated());
            }
            catch (System.Exception ex)
            {
                LastStatus = "AppMetrica init failed.";
                AzurSdkLog.Error("AppMetrica init failed: " + ex);
            }
#else
            LastStatus = "`AZUR_APPMETRICA` define is missing.";
            AzurSdkLog.Warn("AppMetrica is enabled in config but `AZUR_APPMETRICA` is not defined.");
#endif
        }

        public void SetConsent(bool hasConsent)
        {
#if AZUR_APPMETRICA
            try
            {
                AppMetrica.SetDataSendingEnabled(hasConsent);
                LastStatus = "DataSendingEnabled set to " + hasConsent + ".";
                AzurSdkLog.Info("AppMetrica SetDataSendingEnabled=" + hasConsent);
            }
            catch (System.Exception ex)
            {
                LastStatus = "SetDataSendingEnabled failed.";
                AzurSdkLog.Error("AppMetrica SetDataSendingEnabled failed: " + ex);
            }
#endif
        }

        public void SetUserId(string userId)
        {
#if AZUR_APPMETRICA
            try
            {
                AppMetrica.SetUserProfileID(userId);
                LastStatus = "UserProfileID set.";
                AzurSdkLog.Info("AppMetrica SetUserProfileID invoked.");
            }
            catch (System.Exception ex)
            {
                LastStatus = "SetUserProfileID failed.";
                AzurSdkLog.Error("AppMetrica SetUserProfileID failed: " + ex);
            }
#endif
        }

        public void TrackEvent(AzurCustomEvent customEvent)
        {
#if AZUR_APPMETRICA
            try
            {
                AppMetrica.ReportEvent(customEvent.Name, SerializeParameters(customEvent.Parameters));
                FlushCriticalEventsIfNeeded(customEvent.Name);
                LastStatus = "ReportEvent sent: " + customEvent.Name;
            }
            catch (System.Exception ex)
            {
                LastStatus = "ReportEvent failed: " + customEvent.Name;
                AzurSdkLog.Error("AppMetrica ReportEvent failed: " + customEvent.Name + " | " + ex);
            }
#endif
        }

        public void TrackPurchase(AzurPurchaseEvent purchaseEvent)
        {
#if AZUR_APPMETRICA
            var parameters = new Dictionary<string, object>
            {
                ["product_id"] = purchaseEvent.ProductId,
                ["currency"] = purchaseEvent.Currency,
                ["revenue"] = purchaseEvent.Revenue,
                ["transaction_id"] = purchaseEvent.TransactionId,
                ["quantity"] = purchaseEvent.Quantity,
                ["is_subscription"] = purchaseEvent.IsSubscription
            };

            try
            {
                AppMetrica.ReportEvent("purchase", SerializeParameters(parameters));
                LastStatus = "Purchase event sent.";
            }
            catch (System.Exception ex)
            {
                LastStatus = "Purchase event failed.";
                AzurSdkLog.Error("AppMetrica purchase event failed: " + ex);
            }
#endif
        }

        public void TrackAdRevenue(AzurAdRevenueEvent adRevenueEvent)
        {
#if AZUR_APPMETRICA
            var parameters = new Dictionary<string, object>
            {
                ["ad_platform"] = adRevenueEvent.AdPlatform,
                ["ad_source"] = adRevenueEvent.AdSource,
                ["ad_unit_name"] = adRevenueEvent.AdUnitName,
                ["ad_format"] = adRevenueEvent.AdFormat,
                ["revenue"] = adRevenueEvent.Revenue,
                ["currency"] = adRevenueEvent.Currency
            };

            try
            {
                AppMetrica.ReportEvent("ad_impression", SerializeParameters(parameters));
                LastStatus = "Ad revenue event sent.";
            }
            catch (System.Exception ex)
            {
                LastStatus = "Ad revenue event failed.";
                AzurSdkLog.Error("AppMetrica ad revenue event failed: " + ex);
            }
#endif
        }

        private static bool ShouldHandleFirstActivationAsUpdate(AzurSdkConfig config)
        {
            if (config == null || !config.appMetricaHandleFirstActivationAsUpdate)
            {
                return false;
            }

            return PlayerPrefs.GetInt(LaunchMarkerKey, 0) == 1;
        }

        private static void SaveLaunchMarker()
        {
            PlayerPrefs.SetInt(LaunchMarkerKey, 1);
            PlayerPrefs.Save();
        }

        private static void FlushCriticalEventsIfNeeded(string eventName)
        {
#if AZUR_APPMETRICA
            if (!IsCriticalBufferedEvent(eventName))
            {
                return;
            }

            try
            {
                AppMetrica.SendEventsBuffer();
            }
            catch (System.Exception ex)
            {
                AzurSdkLog.Error("AppMetrica SendEventsBuffer failed: " + ex);
            }
#endif
        }

        private static bool IsCriticalBufferedEvent(string eventName)
        {
            return eventName == "level_start" ||
                   eventName == "level_finish" ||
                   eventName == "level_complete";
        }

        private static string SerializeParameters(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return "{}";
            }

            var parts = new List<string>(parameters.Count);
            foreach (var pair in parameters)
            {
                var value = AzurSdkUtility.ToInvariantString(pair.Value).Replace("\"", "\\\"");
                parts.Add($"\"{pair.Key}\":\"{value}\"");
            }

            return "{" + string.Join(",", parts) + "}";
        }
    }
}
