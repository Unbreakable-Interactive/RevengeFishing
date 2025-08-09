using UnityEngine;

public static class GameLogger
{
    public static void Log(object message)
    {
#if ENABLE_DEBUG_LOGS
        Debug.Log(message);
#endif
    }
    
    public static void LogWarning(object message)
    {
#if ENABLE_DEBUG_LOGS
        Debug.LogWarning(message);
#endif
    }
    
    public static void LogError(object message)
    {
#if ENABLE_DEBUG_LOGS
        Debug.LogError(message);
#endif
    }
    
    public static void LogVerbose(object message)
    {
#if ENABLE_VERBOSE_LOGS
        Debug.Log($"[VERBOSE] {message}");
#endif
    }
    
    public static void LogEditor(object message)
    {
#if UNITY_EDITOR
        Debug.Log($"[EDITOR] {message}");
#endif
    }
    
    public static void LogPerformance(object message)
    {
#if ENABLE_DEBUG_LOGS && !UNITY_STANDALONE
        Debug.Log($"[PERF] {message}");
#endif
    }
}