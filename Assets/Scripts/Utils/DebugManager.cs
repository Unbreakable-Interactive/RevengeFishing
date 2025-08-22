using UnityEngine;

[System.Serializable]
public class DebugSettings
{
    [Header("🔧 MASTER DEBUG CONTROL")]
    [SerializeField] public bool enableAllLogs = true;
    
    [Header("📚 SYSTEM-SPECIFIC DEBUG")]
    [SerializeField] public bool enableGameLogger = true;
    [SerializeField] public bool enableVerboseLogs = false;
    [SerializeField] public bool enablePerformanceLogs = false;
    
    [Header("🚢 BOAT SYSTEM DEBUG")]
    [SerializeField] public bool enableBoatCrewDebug = true;
    [SerializeField] public bool enableBoatProbabilitiesDebug = false;
    [SerializeField] public bool enableBoatTriggersDebug = true;
    [SerializeField] public bool enableCrewManagerDebug = true;
    [SerializeField] public bool enableBoatPhysicsDebug = true;
    [SerializeField] public bool enableBoatPlatformDebug = false;
    
    [Header("🌊 WATER SYSTEM DEBUG")]
    [SerializeField] public bool enableWaterPhysicsDebug = false;
    [SerializeField] public bool enableWaveIsolationDebug = false;
    [SerializeField] public bool enableBoatPositioningDebug = false;
    [SerializeField] public bool enableFloatationDebug = false;
}

public class DebugManager : MonoBehaviour
{
    [Header("CENTRALIZED DEBUG CONTROL")]
    [SerializeField] private DebugSettings debugSettings = new DebugSettings();
    
    public static DebugManager Instance { get; private set; }
    public static DebugSettings Settings => Instance?.debugSettings ?? new DebugSettings();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public static void LogBoatCrew(object message)
    {
        if (Settings.enableAllLogs && Settings.enableBoatCrewDebug)
            Debug.Log($"[BOAT CREW] {message}");
    }
    
    public static void LogBoatProbabilities(object message)
    {
        if (Settings.enableAllLogs && Settings.enableBoatProbabilitiesDebug)
            Debug.Log($"[BOAT AI] {message}");
    }
    
    public static void LogBoundaryTrigger(object message)
    {
        if (Settings.enableAllLogs && Settings.enableBoatTriggersDebug)
            Debug.Log($"[BOUNDARY] {message}");
    }
    
    public static void LogCrewManager(object message)
    {
        if (Settings.enableAllLogs && Settings.enableCrewManagerDebug)
            Debug.Log($"[CREW MANAGER] {message}");
    }
    
    public static void LogBoatPhysics(object message)
    {
        if (Settings.enableAllLogs && Settings.enableBoatPhysicsDebug)
            Debug.Log($"[PHYSICS] {message}");
    }
    
    public static void LogWaterPhysics(object message)
    {
        if (Settings.enableAllLogs && Settings.enableWaterPhysicsDebug)
            Debug.Log($"[WATER] {message}");
    }
    
    public static void LogBoatPlatform(object message)
    {
        if (Settings.enableAllLogs && Settings.enableBoatPlatformDebug)
            Debug.Log($"[PLATFORM] {message}");
    }
    
    public static void LogWarning(string system, object message)
    {
        if (Settings.enableAllLogs)
            Debug.LogWarning($"[{system.ToUpper()}] {message}");
    }
    
    public static void LogError(string system, object message)
    {
        if (Settings.enableAllLogs)
            Debug.LogError($"[{system.ToUpper()}] {message}");
    }
    
    [ContextMenu("🔇 Disable All Logs")]
    public void DisableAllLogs()
    {
        debugSettings.enableAllLogs = false;
        Debug.Log("🔇 ALL LOGS DISABLED via DebugManager");
    }
    
    [ContextMenu("🔊 Enable All Logs")]
    public void EnableAllLogs()
    {
        debugSettings.enableAllLogs = true;
        Debug.Log("🔊 ALL LOGS ENABLED via DebugManager");
    }
}
