using UnityEngine;

public static class GameLogger
{
    public static void Log(object message)
    {
        if (DebugManager.Settings.enableAllLogs && DebugManager.Settings.enableGameLogger)
            Debug.Log(message);
    }
    
    public static void LogWarning(object message)
    {
        if (DebugManager.Settings.enableAllLogs && DebugManager.Settings.enableGameLogger)
            Debug.LogWarning(message);
    }
    
    public static void LogError(object message)
    {
        if (DebugManager.Settings.enableAllLogs && DebugManager.Settings.enableGameLogger)
            Debug.LogError(message);
    }
    
    public static void LogVerbose(object message)
    {
        if (DebugManager.Settings.enableAllLogs && DebugManager.Settings.enableVerboseLogs)
            Debug.Log($"[VERBOSE] {message}");
    }
    
    public static void LogEditor(object message)
    {
#if UNITY_EDITOR
        if (DebugManager.Settings.enableAllLogs)
            Debug.Log($"[EDITOR] {message}");
#endif
    }
    
    public static void LogPerformance(object message)
    {
        if (DebugManager.Settings.enableAllLogs && DebugManager.Settings.enablePerformanceLogs)
            Debug.Log($"[PERF] {message}");
    }
}