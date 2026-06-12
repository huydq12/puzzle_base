using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AZUR
{
    internal sealed class AzurFacebookAdapter : IAzurSdkAdapter
    {
        private readonly bool _isEnabled;
        private string _bufferPath;
        private FacebookRevenueBuffer _buffer;
        private int _bufferSize;

        public string LastStatus { get; private set; } = "Not initialized.";

        public AzurFacebookAdapter(bool isEnabled)
        {
            _isEnabled = isEnabled;
        }

        public string Name => "Facebook";
        public bool IsEnabled => _isEnabled;

        public void Initialize(AzurSdkConfig config)
        {
            _bufferSize = Mathf.Max(1, config.facebookRevenueBufferSize);
            _bufferPath = Path.Combine(Application.persistentDataPath, "azur_fb_revenue.json");
            _buffer = LoadBuffer();
#if AZUR_FACEBOOK
            if (!AzurReflection.HasType("Facebook.Unity.FB"))
            {
                LastStatus = "Facebook.Unity.FB type not found.";
                AzurSdkLog.Warn("`AZUR_FACEBOOK` is defined but Facebook SDK is not imported.");
                return;
            }

            if (IsFacebookInitialized())
            {
                AzurReflection.InvokeStatic("Facebook.Unity.FB", "ActivateApp");
                TryEnableIosAdvertiserTracking();
                LastStatus = "Facebook SDK already initialized.";
            }
            else
            {
                AzurReflection.InvokeStatic("Facebook.Unity.FB", "Init", (Action)(() =>
                {
                    AzurReflection.InvokeStatic("Facebook.Unity.FB", "ActivateApp");
                    TryEnableIosAdvertiserTracking();
                    LastStatus = "Facebook SDK initialized.";
                }));
                LastStatus = "Facebook SDK init invoked.";
            }
#else
            LastStatus = "`AZUR_FACEBOOK` define is missing.";
            AzurSdkLog.Warn("Facebook is enabled in config but `AZUR_FACEBOOK` is not defined.");
#endif
        }

        public void SetConsent(bool hasConsent)
        {
        }

        public void SetUserId(string userId)
        {
        }

        public void TrackEvent(AzurCustomEvent customEvent)
        {
        }

        public void TrackPurchase(AzurPurchaseEvent purchaseEvent)
        {
#if AZUR_FACEBOOK
            if (IsFacebookInitialized())
            {
                AzurReflection.InvokeStatic("Facebook.Unity.FB", "LogPurchase", (decimal)purchaseEvent.Revenue, purchaseEvent.Currency ?? "USD");
                LastStatus = "Facebook purchase sent.";
            }
#endif
        }

        public void TrackAdRevenue(AzurAdRevenueEvent adRevenueEvent)
        {
#if AZUR_FACEBOOK
            if (!IsFacebookInitialized())
            {
                return;
            }

            if (string.Equals(adRevenueEvent.AdFormat, "BANNER", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(adRevenueEvent.AdFormat, "LEADER", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var roundedRevenue = Math.Max(0.01d, adRevenueEvent.Revenue);
            var parameters = new Dictionary<string, object>
            {
                ["fb_num_items"] = 1,
                ["fb_currency"] = adRevenueEvent.Currency ?? "USD",
                ["fb_content_type"] = adRevenueEvent.AdFormat ?? string.Empty,
                ["fb_content_id"] = adRevenueEvent.AdSource ?? string.Empty
            };

            AzurReflection.InvokeStatic("Facebook.Unity.FB", "LogAppEvent", "ad_revenue_max", (float)roundedRevenue, parameters);
            BufferRevenue(adRevenueEvent.Revenue);
            LastStatus = "Facebook ad revenue buffered.";
#endif
        }

        private void BufferRevenue(double revenue)
        {
            _buffer.revenues.Add(revenue);
            SaveBuffer();

#if AZUR_FACEBOOK
            if (_buffer.revenues.Count >= _bufferSize && IsFacebookInitialized())
            {
                float totalRevenue = 0f;
                for (var index = 0; index < _buffer.revenues.Count; index++)
                {
                    totalRevenue += (float)_buffer.revenues[index];
                }

                AzurReflection.InvokeStatic("Facebook.Unity.FB", "LogPurchase", (decimal)totalRevenue, "USD", new Dictionary<string, object>
                {
                    ["fb_num_items"] = _buffer.revenues.Count,
                    ["fb_currency"] = "USD",
                    ["fb_content_type"] = "ad_revenue"
                });

                _buffer.revenues.Clear();
                SaveBuffer();
            }
#endif
        }

        private bool IsFacebookInitialized()
        {
            var value = AzurReflection.GetStaticProperty("Facebook.Unity.FB", "IsInitialized");
            return value is bool initialized && initialized;
        }

        private void TryEnableIosAdvertiserTracking()
        {
#if UNITY_IOS && !UNITY_EDITOR && AZUR_FACEBOOK
            var mobileFacebook = AzurReflection.GetStaticProperty("Facebook.Unity.FB", "Mobile");
            AzurReflection.InvokeInstance(mobileFacebook, "SetAdvertiserTrackingEnabled", true);
            LastStatus = "Facebook advertiser tracking enabled.";
#endif
        }

        private FacebookRevenueBuffer LoadBuffer()
        {
            if (!File.Exists(_bufferPath))
            {
                return new FacebookRevenueBuffer();
            }

            try
            {
                var json = File.ReadAllText(_bufferPath);
                var loaded = JsonUtility.FromJson<FacebookRevenueBuffer>(json);
                return loaded ?? new FacebookRevenueBuffer();
            }
            catch
            {
                return new FacebookRevenueBuffer();
            }
        }

        private void SaveBuffer()
        {
            try
            {
                File.WriteAllText(_bufferPath, JsonUtility.ToJson(_buffer));
            }
            catch (Exception exception)
            {
                AzurSdkLog.Warn("Failed to save Facebook revenue buffer: " + exception.Message);
            }
        }

        [Serializable]
        private sealed class FacebookRevenueBuffer
        {
            public List<double> revenues = new List<double>();
        }
    }
}
