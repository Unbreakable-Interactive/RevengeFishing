using UnityEngine;

public class ApplicationTarget : MonoBehaviour
{
    [Header("Performance Settings")]
    [SerializeField] private int targetFrameRate = 100;
    [SerializeField] private float fixedTimestep = 0.02f;
    
    void Awake()
    {
        Time.fixedDeltaTime = fixedTimestep;
        Application.targetFrameRate = targetFrameRate;
        
        Physics2D.autoSyncTransforms = false;
        Physics2D.queriesStartInColliders = false;
        
        GameLogger.Log($"[BOOTSTRAP] FixedDelta: {Time.fixedDeltaTime}, TargetFPS: {Application.targetFrameRate}");
        GameLogger.Log($"[BOOTSTRAP] AutoSync: {Physics2D.autoSyncTransforms}, QueriesInColliders: {Physics2D.queriesStartInColliders}");
    }
}
