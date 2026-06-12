using System;
using System.Collections.Generic;

public static class AnalyticsBridge
{
    public static Action<int> GameStartedHandler;
    public static Action<bool, float, int> GameFinishedHandler;
    public static Action<string, Dictionary<string, object>> CustomEventHandler;
    public static Action<string> RewardedAdRequestedHandler;
    public static Action<string> RewardedAdUnavailableHandler;
    public static Action<string> RewardedAdRewardGrantedHandler;
    public static Action<string> RewardedAdClosedWithoutGrantHandler;

    public static void OnGameStarted(int level)
    {
        GameStartedHandler?.Invoke(level);
    }

    public static void OnGameFinished(bool win, float duration, int level)
    {
        GameFinishedHandler?.Invoke(win, duration, level);
    }

    public static void TrackCustomEvent(string eventName, Dictionary<string, object> parameters)
    {
        CustomEventHandler?.Invoke(eventName, parameters);
    }

    public static void OnRewardedAdRequested(string placement)
    {
        RewardedAdRequestedHandler?.Invoke(placement);
        TrackCustomEvent("rewarded_ad_requested", CreateRewardedAdParameters(placement));
    }

    public static void OnRewardedAdUnavailable(string placement)
    {
        RewardedAdUnavailableHandler?.Invoke(placement);
        TrackCustomEvent("rewarded_ad_unavailable", CreateRewardedAdParameters(placement));
    }

    public static void OnRewardedAdRewardGranted(string placement)
    {
        RewardedAdRewardGrantedHandler?.Invoke(placement);
        TrackCustomEvent("rewarded_ad_reward_granted", CreateRewardedAdParameters(placement));
    }

    public static void OnRewardedAdClosedWithoutGrant(string placement)
    {
        RewardedAdClosedWithoutGrantHandler?.Invoke(placement);
        TrackCustomEvent("rewarded_ad_closed_without_grant", CreateRewardedAdParameters(placement));
    }

    private static Dictionary<string, object> CreateRewardedAdParameters(string placement)
    {
        return new Dictionary<string, object>
        {
            { "placement", placement ?? string.Empty }
        };
    }
}
