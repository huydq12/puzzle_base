using System;
using System.Collections.Generic;

public static class AnalyticsBridge
{
    public static Action<int> GameStartedHandler;
    public static Action<bool, float, int> GameFinishedHandler;
    public static Action<string, Dictionary<string, object>> CustomEventHandler;

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
}
