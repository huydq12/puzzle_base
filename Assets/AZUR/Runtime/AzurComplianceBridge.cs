using System;
using System.Threading.Tasks;
using UnityEngine;

namespace AZUR
{
    internal static class AzurComplianceBridge
    {
        private const string UserConsentManagerType = "UCM.UserConsentManager";
        private const string ApplicationIdentityType = "Azur.Application.Identity.ApplicationIdentity";

        public static bool HasUserConsentManager()
        {
            return AzurReflection.HasType(UserConsentManagerType);
        }

        public static bool HasApplicationIdentity()
        {
            return AzurReflection.HasType(ApplicationIdentityType);
        }

        public static async Task<bool> ResolveConsentAsync(bool useUserConsentManager, bool isGpp, bool fallbackConsent)
        {
            if (!useUserConsentManager)
            {
                return fallbackConsent;
            }

            if (!IsMobileRuntime())
            {
                return fallbackConsent;
            }

            var manager = GetUserConsentManagerInstance();
            if (manager == null)
            {
                AzurSdkLog.Warn("User Consent Manager is enabled in config but not available at runtime.");
                return fallbackConsent;
            }

            var completion = new TaskCompletionSource<bool>();
            Action onFinished = () => completion.TrySetResult(true);

            AzurReflection.InvokeInstance(manager, "StartFlow", onFinished, isGpp);
            await completion.Task;

            var gdprConsent = AzurReflection.GetInstanceProperty(manager, "GdprConsent");
            return gdprConsent is bool hasConsent ? hasConsent : fallbackConsent;
        }

        public static async Task<string> RequestApplicationIdentityAsync(bool useApplicationIdentity)
        {
            if (!useApplicationIdentity)
            {
                return null;
            }

            if (!IsMobileRuntime())
            {
                return null;
            }

            if (!HasApplicationIdentity())
            {
                AzurSdkLog.Warn("Application Identity is enabled in config but not available at runtime.");
                return null;
            }

            var requestTask = AzurReflection.InvokeStatic(ApplicationIdentityType, "RequestAsync") as Task;
            if (requestTask == null)
            {
                AzurSdkLog.Warn("Application Identity request could not be started.");
                return null;
            }

            await requestTask;

            var result = AzurReflection.GetInstanceProperty(requestTask, "Result");
            if (result == null)
            {
                return null;
            }

            var isSuccess = AzurReflection.GetInstanceProperty(result, "IsSuccess");
            if (isSuccess is bool success && success)
            {
                return AzurReflection.GetInstanceProperty(result, "Result") as string;
            }

            var message = AzurReflection.GetInstanceProperty(result, "Message") as string;
            if (!string.IsNullOrWhiteSpace(message))
            {
                AzurSdkLog.Warn("Application Identity failed: " + message);
            }

            return null;
        }

        public static bool NeedShowConsentSettingsButton()
        {
            if (!IsMobileRuntime())
            {
                return false;
            }

            var manager = GetUserConsentManagerInstance();
            if (manager == null)
            {
                return false;
            }

            var result = AzurReflection.InvokeInstance(manager, "NeedShowCmpButton");
            return result is bool shouldShow && shouldShow;
        }

        public static async Task ShowPrivacySettingsAsync()
        {
            var manager = GetUserConsentManagerInstance();
            if (manager == null)
            {
                return;
            }

            var task = AzurReflection.InvokeInstance(manager, "ShowGdprUnityUI", true, null) as Task;
            if (task != null)
            {
                await task;
            }
        }

        public static async Task ShowConsentSettingsAsync()
        {
            var manager = GetUserConsentManagerInstance();
            if (manager == null)
            {
                return;
            }

            var task = AzurReflection.InvokeInstance(manager, "ShowCmpFromSettings") as Task;
            if (task != null)
            {
                await task;
            }
        }

        private static object GetUserConsentManagerInstance()
        {
            return AzurReflection.GetStaticProperty(UserConsentManagerType, "Instance");
        }

        private static bool IsMobileRuntime()
        {
            return Application.platform == RuntimePlatform.Android ||
                   Application.platform == RuntimePlatform.IPhonePlayer;
        }
    }
}
