using UnityEngine;

public class BoatIntegrityDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool enableContinuousLogging = false;
    [SerializeField] private float loggingInterval = 2f;
    
    private float lastLogTime;
    
    void Start()
    {
        GameLogger.Log("🔧 BoatIntegrityDebugger: Monitoring boat integrity system...");
        LogCurrentStats();
    }
    
    void Update()
    {
        if (enableContinuousLogging && Time.time - lastLogTime >= loggingInterval)
        {
            LogCurrentStats();
            lastLogTime = Time.time;
        }
        
        // Manual debugging keys
        if (Input.GetKeyDown(KeyCode.P))
        {
            LogPoolStats();
        }
        
        if (Input.GetKeyDown(KeyCode.O))
        {
            LogCurrentStats();
        }
    }
    
    void LogCurrentStats()
    {
        // Find all active boats
        BoatController[] boats = FindObjectsOfType<BoatController>();
        
        GameLogger.Log($"=== BOAT INTEGRITY STATUS ===");
        GameLogger.Log($"Active boats in scene: {boats.Length}");
        
        for (int i = 0; i < boats.Length; i++)
        {
            BoatController boat = boats[i];
            string status = boat.isActiveAndEnabled ? "ACTIVE" : "INACTIVE";
            GameLogger.Log($"  Boat {i+1} ({boat.name}): {boat.GetCurrentIntegrity()}/{boat.GetMaxIntegrity()} integrity [{status}]");
        }
        
        // Log object pool stats
        if (SimpleObjectPool.Instance != null)
        {
            int available = SimpleObjectPool.Instance.GetAvailableCount("Boat");
            int used = SimpleObjectPool.Instance.GetUsedCount("Boat");
            GameLogger.Log($"Boat Pool: {available} available, {used} used");
        }
        else
        {
            GameLogger.Log("❌ SimpleObjectPool.Instance is null!");
        }
    }
    
    void LogPoolStats()
    {
        if (SimpleObjectPool.Instance != null)
        {
            SimpleObjectPool.Instance.LogPoolStats();
        }
        else
        {
            GameLogger.Log("❌ Cannot log pool stats - SimpleObjectPool.Instance is null!");
        }
    }
}