using System.Collections.Generic;

public static class AnalyticsBridgeAzurRelay
{
    private static bool _isInstalled;

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Install()
    {
        if (_isInstalled)
            return;

        _isInstalled = true;
        AnalyticsBridge.CustomEventHandler -= ForwardCustomEvent;
        AnalyticsBridge.CustomEventHandler += ForwardCustomEvent;
    }

    private static void ForwardCustomEvent(string eventName, Dictionary<string, object> parameters)
    {
    }
}
