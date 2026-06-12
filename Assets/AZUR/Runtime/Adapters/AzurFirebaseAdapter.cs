using System;
using System.Collections.Generic;

namespace AZUR
{
    internal sealed class AzurFirebaseAdapter : IAzurSdkAdapter
    {
        private readonly bool _isEnabled;
        private bool _isRuntimeDisabled;
        private bool _dependencyCheckStarted;
        private bool _dependencyResolved;
        private bool _initializationSucceeded;
        private bool _pendingConsent;
        private string _pendingUserId;

        public bool DependencyResolved => _dependencyResolved;
        public bool InitializationSucceeded => _initializationSucceeded;
        public string LastStatus { get; private set; } = "Not initialized.";

        public AzurFirebaseAdapter(bool isEnabled)
        {
            _isEnabled = isEnabled;
        }

        public string Name => "Firebase";
        public bool IsEnabled => _isEnabled;

        public void Initialize(AzurSdkConfig config)
        {
#if AZUR_FIREBASE
            if (ShouldDisableRuntimeOnCurrentBuild())
            {
                _isRuntimeDisabled = true;
                _dependencyResolved = false;
                _initializationSucceeded = false;
                LastStatus = "Firebase runtime disabled on this Android toolchain.";
                AzurSdkLog.Warn(LastStatus);
                return;
            }

            if (!AzurReflection.HasType("Firebase.FirebaseApp"))
            {
                LastStatus = "Firebase.FirebaseApp type not found.";
                AzurSdkLog.Warn("`AZUR_FIREBASE` is defined but Firebase SDK is not imported.");
                return;
            }

            _pendingConsent = config.defaultConsent;
            _pendingUserId = config.userId;

            if (_dependencyCheckStarted)
            {
                LastStatus = "Dependency check already in progress.";
                return;
            }

            _dependencyCheckStarted = true;
            LastStatus = "Checking Firebase dependencies.";
            var dependencyTaskObject = AzurReflection.InvokeStatic("Firebase.FirebaseApp", "CheckAndFixDependenciesAsync");
            if (!(dependencyTaskObject is System.Threading.Tasks.Task dependencyTask))
            {
                LastStatus = "CheckAndFixDependenciesAsync did not return a task.";
                AzurSdkLog.Warn(LastStatus);
                return;
            }

            dependencyTask.ContinueWith(task =>
            {
                AzurCoroutineRunner.RunDelayed(0d, () => CompleteDependencyCheck(task, dependencyTaskObject));
            });
#else
            AzurSdkLog.Warn("Firebase is enabled in config but `AZUR_FIREBASE` is not defined.");
#endif
        }

        public void SetConsent(bool hasConsent)
        {
            _pendingConsent = hasConsent;
#if AZUR_FIREBASE
            if (_isRuntimeDisabled || !_dependencyResolved || !_initializationSucceeded)
            {
                return;
            }

            AzurReflection.InvokeStatic("Firebase.Analytics.FirebaseAnalytics", "SetAnalyticsCollectionEnabled", hasConsent);
#endif
        }

        public void SetUserId(string userId)
        {
            _pendingUserId = userId;
#if AZUR_FIREBASE
            if (_isRuntimeDisabled || !_dependencyResolved || !_initializationSucceeded)
            {
                return;
            }

            AzurReflection.InvokeStatic("Firebase.Analytics.FirebaseAnalytics", "SetUserId", userId);
#endif
        }

        public void TrackEvent(AzurCustomEvent customEvent)
        {
#if AZUR_FIREBASE
            if (_isRuntimeDisabled || !_dependencyResolved || !_initializationSucceeded)
            {
                return;
            }

            var parameters = BuildParameters(customEvent.Parameters);
            if (parameters == null)
            {
                AzurReflection.InvokeStatic("Firebase.Analytics.FirebaseAnalytics", "LogEvent", customEvent.Name);
                return;
            }

            AzurReflection.InvokeStatic("Firebase.Analytics.FirebaseAnalytics", "LogEvent", customEvent.Name, parameters);
#endif
        }

        public void TrackPurchase(AzurPurchaseEvent purchaseEvent)
        {
#if AZUR_FIREBASE
            if (_isRuntimeDisabled || !_dependencyResolved || !_initializationSucceeded)
            {
                return;
            }

            var parameters = BuildParameters(new Dictionary<string, object>
            {
                ["item_id"] = purchaseEvent.ProductId ?? string.Empty,
                ["currency"] = purchaseEvent.Currency ?? "USD",
                ["value"] = purchaseEvent.Revenue,
                ["quantity"] = purchaseEvent.Quantity
            });

            AzurReflection.InvokeStatic("Firebase.Analytics.FirebaseAnalytics", "LogEvent", "in_app_purchase", parameters);
#endif
        }

        public void TrackAdRevenue(AzurAdRevenueEvent adRevenueEvent)
        {
#if AZUR_FIREBASE
            if (_isRuntimeDisabled || !_dependencyResolved || !_initializationSucceeded)
            {
                return;
            }

            var parameters = BuildParameters(new Dictionary<string, object>
            {
                ["ad_platform"] = adRevenueEvent.AdPlatform ?? string.Empty,
                ["ad_source"] = adRevenueEvent.AdSource ?? string.Empty,
                ["ad_unit_name"] = adRevenueEvent.AdUnitName ?? string.Empty,
                ["ad_format"] = adRevenueEvent.AdFormat ?? string.Empty,
                ["value"] = adRevenueEvent.Revenue,
                ["currency"] = adRevenueEvent.Currency ?? "USD"
            });

            AzurReflection.InvokeStatic("Firebase.Analytics.FirebaseAnalytics", "LogEvent", "ad_impression", parameters);
#endif
        }

#if AZUR_FIREBASE
        private static bool ShouldDisableRuntimeOnCurrentBuild()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }

        private void CompleteDependencyCheck(System.Threading.Tasks.Task task, object dependencyTaskObject)
        {
            _dependencyCheckStarted = false;
            if (task.IsFaulted || task.IsCanceled)
            {
                _dependencyResolved = false;
                _initializationSucceeded = false;
                LastStatus = "Firebase dependency check failed.";
                AzurSdkLog.Warn(LastStatus);
                return;
            }

            var dependencyStatus = AzurReflection.GetInstanceProperty(dependencyTaskObject, "Result");
            var dependencyStatusText = Convert.ToString(dependencyStatus) ?? string.Empty;
            if (!string.Equals(dependencyStatusText, "Available", StringComparison.Ordinal))
            {
                _dependencyResolved = false;
                _initializationSucceeded = false;
                LastStatus = "Firebase dependencies unavailable: " + dependencyStatusText;
                AzurSdkLog.Warn(LastStatus);
                return;
            }

            _dependencyResolved = true;
            _initializationSucceeded = true;
            LastStatus = "Firebase dependencies available.";
            AzurReflection.InvokeStatic("Firebase.Analytics.FirebaseAnalytics", "SetAnalyticsCollectionEnabled", _pendingConsent);
            if (!string.IsNullOrWhiteSpace(_pendingUserId))
            {
                AzurReflection.InvokeStatic("Firebase.Analytics.FirebaseAnalytics", "SetUserId", _pendingUserId);
            }

            AzurSdkLog.Info("Firebase initialization completed.");
        }
#endif

        private static Array BuildParameters(IReadOnlyDictionary<string, object> parameters)
        {
            var parameterType = AzurReflection.FindType("Firebase.Analytics.Parameter");
            if (parameterType == null || parameters == null || parameters.Count == 0)
            {
                return null;
            }

            var array = Array.CreateInstance(parameterType, parameters.Count);
            var index = 0;
            foreach (var pair in parameters)
            {
                var instance = CreateParameter(parameterType, pair.Key, pair.Value);
                if (instance != null)
                {
                    array.SetValue(instance, index++);
                }
            }

            if (index == parameters.Count)
            {
                return array;
            }

            var resized = Array.CreateInstance(parameterType, index);
            Array.Copy(array, resized, index);
            return resized;
        }

        private static object CreateParameter(Type parameterType, string key, object value)
        {
            try
            {
                if (value is int intValue)
                {
                    return Activator.CreateInstance(parameterType, key, (long)intValue);
                }

                if (value is long longValue)
                {
                    return Activator.CreateInstance(parameterType, key, longValue);
                }

                if (value is float floatValue)
                {
                    return Activator.CreateInstance(parameterType, key, floatValue);
                }

                if (value is double doubleValue)
                {
                    return Activator.CreateInstance(parameterType, key, doubleValue);
                }

                return Activator.CreateInstance(parameterType, key, AzurSdkUtility.ToInvariantString(value));
            }
            catch
            {
                return null;
            }
        }
    }
}
