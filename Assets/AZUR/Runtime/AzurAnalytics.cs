using System.Collections.Generic;

namespace AZUR
{
    public static class AzurAnalytics
    {
        public static void TrackLevelStart(int levelIndex, string levelName = null)
        {
            AzurSdk.TrackEvent("level_start", new Dictionary<string, object>
            {
                ["level_index"] = levelIndex,
                ["level_name"] = levelName ?? string.Empty
            });
        }

        public static void TrackLevelComplete(int levelIndex, string levelName = null, double durationSeconds = 0d)
        {
            AzurSdk.TrackEvent("level_complete", new Dictionary<string, object>
            {
                ["level_index"] = levelIndex,
                ["level_name"] = levelName ?? string.Empty,
                ["duration_seconds"] = durationSeconds
            });
        }

        public static void TrackLevelFail(int levelIndex, string levelName = null, string reason = null)
        {
            AzurSdk.TrackEvent("level_fail", new Dictionary<string, object>
            {
                ["level_index"] = levelIndex,
                ["level_name"] = levelName ?? string.Empty,
                ["reason"] = reason ?? string.Empty
            });
        }

        public static void TrackAdOpportunity(string eventName, string placement = null)
        {
            AzurSdk.TrackEvent(eventName, new Dictionary<string, object>
            {
                ["placement"] = placement ?? string.Empty
            });
        }

        public static void TrackTutorialStep(string stepName, int stepIndex)
        {
            AzurSdk.TrackEvent("tutorial_step", new Dictionary<string, object>
            {
                ["step_name"] = stepName ?? string.Empty,
                ["step_index"] = stepIndex
            });
        }
    }
}
