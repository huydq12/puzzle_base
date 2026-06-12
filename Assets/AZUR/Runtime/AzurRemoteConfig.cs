using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace AZUR
{
    public static class AzurRemoteConfig
    {
        private static readonly Dictionary<string, string> Defaults = new Dictionary<string, string>();
        private static bool _initialized;
        private static bool _lastFetchSucceeded;
        private static DateTime _lastFetchUtc;

        public static bool IsInitialized => _initialized;
        public static bool LastFetchSucceeded => _lastFetchSucceeded;
        public static DateTime LastFetchUtc => _lastFetchUtc;

        public static void Initialize(AzurSdkConfig config)
        {
            Defaults.Clear();
            ParseDefaults(config != null ? config.remoteConfigDefaultsJson : string.Empty, Defaults);
            _initialized = true;
            AzurSdkLog.Info("RemoteConfig initialized with " + Defaults.Count + " default values.");
        }

        public static void Fetch(Action<bool> onCompleted = null)
        {
            if (!_initialized)
            {
                Initialize(AzurSdk.Config);
            }

#if AZUR_FIREBASE
            if (AzurSdk.Config != null && AzurSdk.Config.enableFirebase && AzurSdk.Config.enableRemoteConfig)
            {
                if (!AzurReflection.HasType("Firebase.RemoteConfig.FirebaseRemoteConfig"))
                {
                    _lastFetchSucceeded = false;
                    _lastFetchUtc = DateTime.UtcNow;
                    onCompleted?.Invoke(false);
                    return;
                }

                var fetchInterval = TimeSpan.FromSeconds(Math.Max(0, AzurSdk.Config.remoteConfigMinimumFetchIntervalSeconds));
                var remoteConfigInstance = AzurReflection.GetStaticProperty("Firebase.RemoteConfig.FirebaseRemoteConfig", "DefaultInstance");
                ApplyDefaults(remoteConfigInstance);
                var fetchTask = AzurReflection.InvokeInstance(remoteConfigInstance, "FetchAsync", fetchInterval);
                if (fetchTask == null)
                {
                    _lastFetchSucceeded = false;
                    _lastFetchUtc = DateTime.UtcNow;
                    onCompleted?.Invoke(false);
                    return;
                }

                var awaitedTask = fetchTask as Task;
                if (awaitedTask == null)
                {
                    _lastFetchSucceeded = false;
                    _lastFetchUtc = DateTime.UtcNow;
                    onCompleted?.Invoke(false);
                    return;
                }

                awaitedTask.ContinueWith(task =>
                {
                    var succeeded = task.IsCompleted && !task.IsFaulted && !task.IsCanceled;
                    if (succeeded)
                    {
                        var activateTask = AzurReflection.InvokeInstance(remoteConfigInstance, "ActivateAsync") as Task;
                        if (activateTask != null)
                        {
                            activateTask.ContinueWith(activateResult =>
                            {
                                var activationSucceeded = activateResult.IsCompleted && !activateResult.IsFaulted && !activateResult.IsCanceled;
                                CompleteFetch(activationSucceeded, onCompleted);
                            });
                            return;
                        }
                    }

                    CompleteFetch(succeeded, onCompleted);
                });
                return;
            }
#endif

            _lastFetchSucceeded = false;
            _lastFetchUtc = DateTime.UtcNow;
            onCompleted?.Invoke(false);
        }

        public static string GetString(string key, string fallback = "")
        {
#if AZUR_FIREBASE
            if (CanUseFirebase())
            {
                var remoteConfigInstance = AzurReflection.GetStaticProperty("Firebase.RemoteConfig.FirebaseRemoteConfig", "DefaultInstance");
                var configValue = AzurReflection.InvokeInstance(remoteConfigInstance, "GetValue", key);
                var stringValue = AzurReflection.GetInstanceProperty(configValue, "StringValue");
                return Convert.ToString(stringValue) ?? fallback;
            }
#endif
            return Defaults.TryGetValue(key, out var value) ? value : fallback;
        }

        public static bool GetBool(string key, bool fallback = false)
        {
            var value = GetString(key, fallback ? "true" : "false");
            return bool.TryParse(value, out var parsed) ? parsed : fallback;
        }

        public static int GetInt(string key, int fallback = 0)
        {
            var value = GetString(key, fallback.ToString());
            return int.TryParse(value, out var parsed) ? parsed : fallback;
        }

        public static double GetDouble(string key, double fallback = 0d)
        {
            var value = GetString(key, fallback.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;
        }

        private static void ParseDefaults(string json, Dictionary<string, string> target)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            try
            {
                var wrapper = JsonUtility.FromJson<RemoteConfigDefaultsWrapper>(NormalizeJson(json));
                if (wrapper?.entries == null)
                {
                    return;
                }

                for (var index = 0; index < wrapper.entries.Length; index++)
                {
                    var entry = wrapper.entries[index];
                    if (!string.IsNullOrWhiteSpace(entry.key))
                    {
                        target[entry.key] = entry.value ?? string.Empty;
                    }
                }
            }
            catch (Exception exception)
            {
                AzurSdkLog.Warn("Failed to parse remote config defaults json: " + exception.Message);
            }
        }

        private static string NormalizeJson(string json)
        {
            var trimmed = json.Trim();
            if (trimmed.StartsWith("{\"entries\":", StringComparison.Ordinal))
            {
                return trimmed;
            }

            var lines = trimmed.Replace("\r", string.Empty).Split('\n');
            var entries = new List<string>();
            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index].Trim().Trim(',');
                if (string.IsNullOrWhiteSpace(line) || line == "{" || line == "}")
                {
                    continue;
                }

                var parts = line.Split(new[] { ':' }, 2);
                if (parts.Length != 2)
                {
                    continue;
                }

                var key = parts[0].Trim().Trim('"');
                var value = parts[1].Trim().Trim('"');
                entries.Add("{\"key\":\"" + Escape(key) + "\",\"value\":\"" + Escape(value) + "\"}");
            }

            return "{\"entries\":[" + string.Join(",", entries) + "]}";
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static void ApplyDefaults(object remoteConfigInstance)
        {
            if (remoteConfigInstance == null || Defaults.Count == 0)
            {
                return;
            }

            var defaults = new Dictionary<string, object>(Defaults.Count);
            foreach (var pair in Defaults)
            {
                defaults[pair.Key] = pair.Value;
            }

            AzurReflection.InvokeInstance(remoteConfigInstance, "SetDefaultsAsync", defaults);
        }

        private static void CompleteFetch(bool succeeded, Action<bool> onCompleted)
        {
            _lastFetchSucceeded = succeeded;
            _lastFetchUtc = DateTime.UtcNow;
            AzurSdkLog.Info("RemoteConfig fetch completed: " + succeeded);
            onCompleted?.Invoke(succeeded);
        }

#if AZUR_FIREBASE
        private static bool CanUseFirebase()
        {
            if (AzurSdk.Config == null || !AzurSdk.Config.enableFirebase || !AzurSdk.Config.enableRemoteConfig)
            {
                return false;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            return false;
#else
            return true;
#endif
        }
#endif

        [Serializable]
        private sealed class RemoteConfigDefaultsWrapper
        {
            public RemoteConfigEntry[] entries;
        }

        [Serializable]
        private sealed class RemoteConfigEntry
        {
            public string key;
            public string value;
        }
    }
}
