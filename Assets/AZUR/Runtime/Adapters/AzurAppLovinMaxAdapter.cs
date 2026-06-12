using System;
using System.Collections.Generic;
using UnityEngine;

namespace AZUR
{
    internal sealed class AzurAppLovinMaxAdapter : IAzurSdkAdapter
    {
        private readonly bool _isEnabled;
        private AzurSdkConfig _config;
        private bool _mediationDebuggerShown;
        private int _interstitialRetryAttempt;
        private int _rewardedRetryAttempt;
        private bool _isInterstitialLoading;
        private bool _isRewardedLoading;
        private float _interstitialReloadStartedAt = -1f;
        private float _rewardedReloadStartedAt = -1f;

        public bool InitAttempted { get; private set; }
        public bool SdkDetected { get; private set; }
        public string LastStatus { get; private set; } = "Not initialized.";
        public bool IsInterstitialReady => !string.IsNullOrWhiteSpace(_config?.InterstitialId) && IsInterstitialReadyInternal();
        public bool IsRewardedReady => !string.IsNullOrWhiteSpace(_config?.RewardedId) && IsRewardedReadyInternal();
        public bool IsInterstitialLoading => _isInterstitialLoading;
        public bool IsRewardedLoading => _isRewardedLoading;

        public AzurAppLovinMaxAdapter(bool isEnabled)
        {
            _isEnabled = isEnabled;
        }

        public string Name => "AppLovin MAX";
        public bool IsEnabled => _isEnabled;

        public void Initialize(AzurSdkConfig config)
        {
            _config = config;
            InitAttempted = true;
#if AZUR_APPLOVIN_MAX
            SdkDetected = true;
            MaxSdk.SetVerboseLogging(Debug.isDebugBuild && config.enableVerboseLogs);
            MaxSdk.SetCreativeDebuggerEnabled(Debug.isDebugBuild && config.enableCreativeDebuggerInDebug);
            TrySetPrivacyFlags(config.defaultConsent, config.doNotSell, "Initialize");

            if (!string.IsNullOrWhiteSpace(config.userId))
            {
                MaxSdk.SetUserId(config.userId);
            }

            RegisterCallbacks();
            MaxSdk.InitializeSdk();
            LastStatus = "InitializeSdk invoked.";
            AzurSdkLog.Info("AppLovin MAX init invoked. IsInitialized=" + IsSdkInitialized());
#else
            SdkDetected = false;
            LastStatus = "`AZUR_APPLOVIN_MAX` define is missing.";
            AzurSdkLog.Warn("AppLovin MAX is enabled in config but `AZUR_APPLOVIN_MAX` is not defined.");
#endif
        }

        public void ShowMediationDebugger()
        {
#if AZUR_APPLOVIN_MAX
            if (!SdkDetected)
            {
                LastStatus = "MaxSdk type not found.";
                return;
            }

            MaxSdk.ShowMediationDebugger();
            LastStatus = "ShowMediationDebugger invoked.";
            AzurSdkLog.Info("AppLovin MAX Mediation Debugger opened.");
#endif
        }

        public bool IsSdkInitialized()
        {
#if AZUR_APPLOVIN_MAX
            return MaxSdk.IsInitialized();
#else
            return false;
#endif
        }

        public void SetConsent(bool hasConsent)
        {
#if AZUR_APPLOVIN_MAX
            TrySetPrivacyFlags(hasConsent, _config != null && _config.doNotSell, "SetConsent");
#endif
        }

        public void SetUserId(string userId)
        {
#if AZUR_APPLOVIN_MAX
            if (!string.IsNullOrWhiteSpace(userId))
            {
                MaxSdk.SetUserId(userId);
            }
#endif
        }

        public void TrackEvent(AzurCustomEvent customEvent)
        {
#if AZUR_APPLOVIN_MAX
            var data = ToStringDictionary(customEvent.Parameters);
            MaxSdk.TrackEvent(customEvent.Name, data);
#endif
        }

        public void TrackPurchase(AzurPurchaseEvent purchaseEvent)
        {
        }

        public void TrackAdRevenue(AzurAdRevenueEvent adRevenueEvent)
        {
        }

        public void LoadInterstitial()
        {
#if AZUR_APPLOVIN_MAX
            LoadInterstitialInternal();
#endif
        }

        public void ShowInterstitial(string placement = null)
        {
#if AZUR_APPLOVIN_MAX
            if (string.IsNullOrWhiteSpace(_config?.InterstitialId))
            {
                LastStatus = "InterstitialId is empty.";
                AzurSdkLog.Warn(LastStatus);
                return;
            }

            if (!IsInterstitialReadyInternal())
            {
                LastStatus = "Interstitial is not ready. Triggered load before show.";
                AzurSdkLog.Warn(LastStatus);
                LoadInterstitialInternal();
                return;
            }

            if (string.IsNullOrWhiteSpace(placement))
            {
                MaxSdk.ShowInterstitial(_config.InterstitialId);
            }
            else
            {
                MaxSdk.ShowInterstitial(_config.InterstitialId, placement);
            }
#endif
        }

        public void LoadRewarded()
        {
#if AZUR_APPLOVIN_MAX
            LoadRewardedInternal();
#endif
        }

        public void ShowRewarded(string placement = null)
        {
#if AZUR_APPLOVIN_MAX
            if (string.IsNullOrWhiteSpace(_config?.RewardedId))
            {
                LastStatus = "RewardedId is empty.";
                AzurSdkLog.Warn(LastStatus);
                return;
            }

            if (!IsRewardedReadyInternal())
            {
                LastStatus = "Rewarded is not ready. Triggered load before show.";
                AzurSdkLog.Warn(LastStatus);
                LoadRewardedInternal();
                return;
            }

            if (string.IsNullOrWhiteSpace(placement))
            {
                MaxSdk.ShowRewardedAd(_config.RewardedId);
            }
            else
            {
                MaxSdk.ShowRewardedAd(_config.RewardedId, placement);
            }
#endif
        }

        public void ShowBanner()
        {
#if AZUR_APPLOVIN_MAX
            if (string.IsNullOrWhiteSpace(_config?.BannerId))
            {
                LastStatus = "BannerId is empty.";
                AzurSdkLog.Warn(LastStatus);
                return;
            }

            MaxSdk.CreateBanner(_config.BannerId, MaxSdkBase.BannerPosition.BottomCenter);
            MaxSdk.ShowBanner(_config.BannerId);
            LastStatus = "ShowBanner invoked.";
            AzurSdkLog.Info(LastStatus);
#endif
        }

        public void HideBanner()
        {
#if AZUR_APPLOVIN_MAX
            if (!string.IsNullOrWhiteSpace(_config?.BannerId))
            {
                MaxSdk.HideBanner(_config.BannerId);
            }
#endif
        }

        public bool TryForwardRevenueFromAdInfo(object adInfoObject)
        {
#if AZUR_APPLOVIN_MAX
            if (!(adInfoObject is MaxSdkBase.AdInfo adInfo))
            {
                return false;
            }

            var eventData = new AzurAdRevenueEvent(
                "AppLovin",
                adInfo.NetworkName ?? string.Empty,
                adInfo.AdUnitIdentifier ?? string.Empty,
                adInfo.AdFormat ?? string.Empty,
                adInfo.Revenue);

            AzurSdk.ForwardAdRevenue(eventData);
            return true;
#else
            return false;
#endif
        }

#if AZUR_APPLOVIN_MAX
        private void RegisterCallbacks()
        {
            MaxSdkCallbacks.OnSdkInitializedEvent -= OnSdkInitialized;
            MaxSdkCallbacks.OnSdkInitializedEvent += OnSdkInitialized;

            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= OnInterstitialLoaded;
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoaded;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= OnInterstitialLoadFailed;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent -= OnInterstitialDisplayed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialDisplayed;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent -= OnInterstitialHidden;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialHidden;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent -= OnInterstitialDisplayFailed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialDisplayFailed;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent -= OnInterstitialRevenuePaid;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnInterstitialRevenuePaid;

            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent -= OnRewardedLoaded;
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedLoaded;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent -= OnRewardedLoadFailed;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedLoadFailed;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent -= OnRewardedDisplayed;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedDisplayed;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= OnRewardedHidden;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedHidden;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent -= OnRewardedDisplayFailed;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedDisplayFailed;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent -= OnRewardedReceivedReward;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedReceivedReward;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent -= OnRewardedRevenuePaid;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedRevenuePaid;

            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent -= OnBannerRevenuePaid;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnBannerRevenuePaid;
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent -= OnMRecRevenuePaid;
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += OnMRecRevenuePaid;
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent -= OnAppOpenRevenuePaid;
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += OnAppOpenRevenuePaid;
        }

        private void OnSdkInitialized(MaxSdkBase.SdkConfiguration sdkConfiguration)
        {
            var isInitialized = sdkConfiguration != null && sdkConfiguration.IsSuccessfullyInitialized;
            LastStatus = isInitialized
                ? "MAX SDK initialized."
                : "MAX SDK initialization failed.";
            AzurSdkLog.Info(LastStatus);

            if (!isInitialized)
            {
                return;
            }

            if (!_mediationDebuggerShown && Debug.isDebugBuild && _config != null && _config.enableMediationDebuggerInDebug)
            {
                AzurCoroutineRunner.RunDelayed(1f, () =>
                {
                    ShowMediationDebugger();
                    _mediationDebuggerShown = true;
                });
            }

            if (!string.IsNullOrWhiteSpace(_config?.InterstitialId))
            {
                AzurCoroutineRunner.RunDelayed(0.25f, LoadInterstitialInternal);
            }

            if (!string.IsNullOrWhiteSpace(_config?.RewardedId))
            {
                AzurCoroutineRunner.RunDelayed(0.25f, LoadRewardedInternal);
            }
        }

        private void OnInterstitialLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _isInterstitialLoading = false;
            _interstitialRetryAttempt = 0;
            var reloadTime = ConsumeReloadDuration(ref _interstitialReloadStartedAt);
            LastStatus = reloadTime.HasValue
                ? "Interstitial loaded: " + adUnitId + " | reload=" + reloadTime.Value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "s"
                : "Interstitial loaded: " + adUnitId;
            AzurSdkLog.Info(LastStatus);
        }

        private void OnInterstitialLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            _isInterstitialLoading = false;
            _interstitialRetryAttempt++;
            var retryDelay = CalculateRetryDelaySeconds(_interstitialRetryAttempt);
            LastStatus = "Interstitial load failed: " + BuildErrorMessage(errorInfo) + ". Retrying in " + retryDelay.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "s.";
            AzurSdkLog.Warn(LastStatus);
            AzurCoroutineRunner.RunDelayed(retryDelay, LoadInterstitialInternal);
        }

        private void OnInterstitialDisplayed(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LastStatus = "Interstitial displayed: " + adUnitId;
            AzurSdkLog.Info(LastStatus);
        }

        private void OnInterstitialHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LastStatus = "Interstitial hidden: " + adUnitId + ". Preloading next interstitial.";
            AzurSdkLog.Info(LastStatus);
            _interstitialReloadStartedAt = Time.realtimeSinceStartup;
            LoadInterstitialInternal();
        }

        private void OnInterstitialDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            LastStatus = "Interstitial display failed: " + BuildErrorMessage(errorInfo) + ". Preloading next interstitial.";
            AzurSdkLog.Warn(LastStatus);
            _interstitialReloadStartedAt = Time.realtimeSinceStartup;
            LoadInterstitialInternal();
        }

        private void OnRewardedLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _isRewardedLoading = false;
            _rewardedRetryAttempt = 0;
            var reloadTime = ConsumeReloadDuration(ref _rewardedReloadStartedAt);
            LastStatus = reloadTime.HasValue
                ? "Rewarded loaded: " + adUnitId + " | reload=" + reloadTime.Value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "s"
                : "Rewarded loaded: " + adUnitId;
            AzurSdkLog.Info(LastStatus);
            AzurSdk.TrackEvent("video_ads_available", BuildRewardedEventParameters(adUnitId, adInfo));
        }

        private void OnRewardedLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            _isRewardedLoading = false;
            _rewardedRetryAttempt++;
            var retryDelay = CalculateRetryDelaySeconds(_rewardedRetryAttempt);
            LastStatus = "Rewarded load failed: " + BuildErrorMessage(errorInfo) + ". Retrying in " + retryDelay.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "s.";
            AzurSdkLog.Warn(LastStatus);
            AzurCoroutineRunner.RunDelayed(retryDelay, LoadRewardedInternal);
        }

        private void OnRewardedDisplayed(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LastStatus = "Rewarded displayed: " + adUnitId;
            AzurSdkLog.Info(LastStatus);
            AzurSdk.TrackEvent("video_ads_started", BuildRewardedEventParameters(adUnitId, adInfo));
        }

        private void OnRewardedHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LastStatus = "Rewarded hidden: " + adUnitId + ". Preloading next rewarded.";
            AzurSdkLog.Info(LastStatus);
            AzurAds.NotifyRewardedClosed();
            _rewardedReloadStartedAt = Time.realtimeSinceStartup;
            LoadRewardedInternal();
        }

        private void OnRewardedDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            LastStatus = "Rewarded display failed: " + BuildErrorMessage(errorInfo) + ". Preloading next rewarded.";
            AzurSdkLog.Warn(LastStatus);
            AzurAds.NotifyRewardedDisplayFailed();
            _rewardedReloadStartedAt = Time.realtimeSinceStartup;
            LoadRewardedInternal();
        }

        private void OnRewardedReceivedReward(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            var amount = reward.Amount;
            var label = reward.Label ?? string.Empty;
            LastStatus = "Reward received: " + amount + " " + label;
            AzurSdkLog.Info(LastStatus);
            var parameters = BuildRewardedEventParameters(adUnitId, adInfo);
            parameters["reward_amount"] = amount;
            parameters["reward_label"] = label;
            AzurSdk.TrackEvent("video_ads_watch", parameters);
            AzurAds.NotifyRewardedGranted();
        }

        private static Dictionary<string, object> BuildRewardedEventParameters(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            return new Dictionary<string, object>
            {
                ["ad_type"] = "rewarded",
                ["ad_unit_id"] = adUnitId ?? string.Empty,
                ["network_name"] = adInfo != null ? adInfo.NetworkName ?? string.Empty : string.Empty,
                ["ad_format"] = adInfo != null ? adInfo.AdFormat ?? string.Empty : string.Empty,
                ["placement"] = adInfo != null ? adInfo.Placement ?? string.Empty : string.Empty
            };
        }

        private void OnInterstitialRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            OnRevenuePaid(adUnitId, adInfo);
        }

        private void OnRewardedRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            OnRevenuePaid(adUnitId, adInfo);
        }

        private void OnBannerRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            OnRevenuePaid(adUnitId, adInfo);
        }

        private void OnMRecRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            OnRevenuePaid(adUnitId, adInfo);
        }

        private void OnAppOpenRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            OnRevenuePaid(adUnitId, adInfo);
        }

        private void OnRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (TryForwardRevenueFromAdInfo(adInfo))
            {
                LastStatus = "Ad revenue forwarded for unit: " + adUnitId;
                AzurSdkLog.Info(LastStatus);
            }
        }

        private void LoadInterstitialInternal()
        {
            if (string.IsNullOrWhiteSpace(_config?.InterstitialId))
            {
                LastStatus = "InterstitialId is empty.";
                _isInterstitialLoading = false;
                return;
            }

            if (_isInterstitialLoading || IsInterstitialReadyInternal())
            {
                return;
            }

            _isInterstitialLoading = true;
            MaxSdk.LoadInterstitial(_config.InterstitialId);
            LastStatus = "LoadInterstitial invoked.";
            AzurSdkLog.Info(LastStatus);
        }

        private void LoadRewardedInternal()
        {
            if (string.IsNullOrWhiteSpace(_config?.RewardedId))
            {
                LastStatus = "RewardedId is empty.";
                _isRewardedLoading = false;
                return;
            }

            if (_isRewardedLoading || IsRewardedReadyInternal())
            {
                return;
            }

            _isRewardedLoading = true;
            MaxSdk.LoadRewardedAd(_config.RewardedId);
            LastStatus = "LoadRewarded invoked.";
            AzurSdkLog.Info(LastStatus);
        }

        private bool IsInterstitialReadyInternal()
        {
            return MaxSdk.IsInterstitialReady(_config.InterstitialId);
        }

        private bool IsRewardedReadyInternal()
        {
            return MaxSdk.IsRewardedAdReady(_config.RewardedId);
        }

        private static double CalculateRetryDelaySeconds(int retryAttempt)
        {
            return Math.Pow(2d, Math.Min(6, retryAttempt));
        }

        private static float? ConsumeReloadDuration(ref float startedAt)
        {
            if (startedAt < 0f)
            {
                return null;
            }

            var duration = Mathf.Max(0f, Time.realtimeSinceStartup - startedAt);
            startedAt = -1f;
            return duration;
        }

        private static string BuildErrorMessage(MaxSdkBase.ErrorInfo errorInfo)
        {
            if (errorInfo == null)
            {
                return "Unknown MAX error";
            }

            if (string.IsNullOrWhiteSpace(errorInfo.AdLoadFailureInfo))
            {
                return errorInfo.Message ?? "Unknown MAX error";
            }

            if (string.IsNullOrWhiteSpace(errorInfo.Message))
            {
                return errorInfo.AdLoadFailureInfo;
            }

            return errorInfo.Message + " | " + errorInfo.AdLoadFailureInfo;
        }

        private void TrySetPrivacyFlags(bool hasConsent, bool doNotSell, string source)
        {
            try
            {
                MaxSdk.SetHasUserConsent(hasConsent);
                MaxSdk.SetDoNotSell(doNotSell);
            }
            catch (Exception exception)
            {
                LastStatus = "MAX privacy API unavailable on pinned Android SDK. Continuing without privacy bridge. Source=" + source;
                AzurSdkLog.Warn(LastStatus + " | " + exception.Message);
            }
        }

        private static System.Collections.Generic.IDictionary<string, string> ToStringDictionary(System.Collections.Generic.IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return null;
            }

            var result = new System.Collections.Generic.Dictionary<string, string>(parameters.Count);
            foreach (var pair in parameters)
            {
                result[pair.Key] = pair.Value == null
                    ? string.Empty
                    : Convert.ToString(pair.Value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            }

            return result;
        }
#endif
    }
}
