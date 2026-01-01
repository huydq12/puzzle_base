using System.Diagnostics;

public static class Logger {
    [Conditional("ENABLE_LOGS")]
    public static void Debug(string logMsg) {
        UnityEngine.Debug.Log(logMsg);
    }

    [Conditional("ENABLE_LOGS")]
    public static void Warning(string logMsg) {
        UnityEngine.Debug.LogWarning(logMsg);
    }
    
    [Conditional("ENABLE_LOGS")]
    public static void Error(string logMsg) {
        UnityEngine.Debug.LogError(logMsg);
    }
}