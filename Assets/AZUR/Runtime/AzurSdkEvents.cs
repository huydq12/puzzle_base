using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace AZUR
{
    public readonly struct AzurCustomEvent
    {
        public AzurCustomEvent(string name, IReadOnlyDictionary<string, object> parameters = null)
        {
            Name = name;
            Parameters = parameters;
        }

        public string Name { get; }
        public IReadOnlyDictionary<string, object> Parameters { get; }
    }

    public readonly struct AzurPurchaseEvent
    {
        public AzurPurchaseEvent(
            string productId,
            string currency,
            double revenue,
            string transactionId = "",
            int quantity = 1,
            bool isSubscription = false)
        {
            ProductId = productId;
            Currency = currency;
            Revenue = revenue;
            TransactionId = transactionId;
            Quantity = quantity;
            IsSubscription = isSubscription;
        }

        public string ProductId { get; }
        public string Currency { get; }
        public double Revenue { get; }
        public string TransactionId { get; }
        public int Quantity { get; }
        public bool IsSubscription { get; }
    }

    public readonly struct AzurAdRevenueEvent
    {
        public AzurAdRevenueEvent(
            string adPlatform,
            string adSource,
            string adUnitName,
            string adFormat,
            double revenue,
            string currency = "USD",
            string placement = "")
        {
            AdPlatform = adPlatform;
            AdSource = adSource;
            AdUnitName = adUnitName;
            AdFormat = adFormat;
            Revenue = revenue;
            Currency = currency;
            Placement = placement;
        }

        public string AdPlatform { get; }
        public string AdSource { get; }
        public string AdUnitName { get; }
        public string AdFormat { get; }
        public double Revenue { get; }
        public string Currency { get; }
        public string Placement { get; }
    }

    public interface IAzurSdkAdapter
    {
        string Name { get; }
        bool IsEnabled { get; }
        void Initialize(AzurSdkConfig config);
        void SetConsent(bool hasConsent);
        void SetUserId(string userId);
        void TrackEvent(AzurCustomEvent customEvent);
        void TrackPurchase(AzurPurchaseEvent purchaseEvent);
        void TrackAdRevenue(AzurAdRevenueEvent adRevenueEvent);
    }

    internal static class AzurSdkLog
    {
        private const int MaxBufferedEntries = 80;
        private static readonly Queue<string> RecentEntries = new Queue<string>(MaxBufferedEntries);
        private static readonly object SyncRoot = new object();
        private static bool _runtimeHookInstalled;
        private static string _logFilePath;

        public static bool Verbose { get; set; }
        public static string LogFilePath => _logFilePath ??= Path.Combine(Application.persistentDataPath, "azur_runtime.log");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void InstallRuntimeHook()
        {
            if (_runtimeHookInstalled)
            {
                return;
            }

            _runtimeHookInstalled = true;
            Application.logMessageReceived += OnUnityLogReceived;
            WriteEntry("INFO", "Runtime diagnostics hook installed.");
        }

        public static void Info(string message)
        {
            if (Verbose)
            {
                Debug.Log("[AZUR] " + message);
            }

            WriteEntry("INFO", message);
        }

        public static void Warn(string message)
        {
            Debug.LogWarning("[AZUR] " + message);
            WriteEntry("WARN", message);
        }

        public static void Error(string message)
        {
            Debug.LogError("[AZUR] " + message);
            WriteEntry("ERROR", message);
        }

        public static string GetRecentEntriesText(int maxLines = 12)
        {
            lock (SyncRoot)
            {
                if (RecentEntries.Count == 0)
                {
                    return "<no AZUR runtime logs>";
                }

                var lines = RecentEntries.ToArray();
                var startIndex = Math.Max(0, lines.Length - Math.Max(1, maxLines));
                var builder = new StringBuilder();
                for (var index = startIndex; index < lines.Length; index++)
                {
                    builder.AppendLine(lines[index]);
                }

                return builder.ToString().TrimEnd();
            }
        }

        private static void OnUnityLogReceived(string condition, string stackTrace, LogType type)
        {
            if (string.IsNullOrWhiteSpace(condition))
            {
                return;
            }

            if (condition.Contains("[AZUR]"))
            {
                return;
            }

            if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception)
            {
                WriteEntry(type.ToString().ToUpperInvariant(), condition);
                if (!string.IsNullOrWhiteSpace(stackTrace))
                {
                    WriteEntry(type.ToString().ToUpperInvariant(), stackTrace);
                }
            }
        }

        private static void WriteEntry(string level, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            var line = DateTime.Now.ToString("HH:mm:ss") + " [" + level + "] " + message;
            lock (SyncRoot)
            {
                if (RecentEntries.Count >= MaxBufferedEntries)
                {
                    RecentEntries.Dequeue();
                }

                RecentEntries.Enqueue(line);

                try
                {
                    File.AppendAllText(LogFilePath, line + Environment.NewLine);
                }
                catch
                {
                    // Ignore file write failures to avoid breaking runtime diagnostics.
                }
            }
        }
    }
}
