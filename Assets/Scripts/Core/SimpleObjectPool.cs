using System.Collections.Generic;
using UnityEngine;

public class SimpleObjectPool : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PoolingConfig poolingConfig;
    
    [Header("Runtime Info (Read Only)")]
    [SerializeField] private bool showDebugInfo = true;
    
    // Pool structure for COMPLETE HANDLERS
    private Dictionary<string, Queue<GameObject>> availableHandlers = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, List<GameObject>> usedHandlers = new Dictionary<string, List<GameObject>>();
    private Dictionary<string, GameObject> poolContainers = new Dictionary<string, GameObject>();
    
    public static SimpleObjectPool Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        CreateAllPools();
    }

    void CreateAllPools()
    {
        if (poolingConfig == null)
        {
            Debug.LogError("No PoolingConfig assigned to SimpleObjectPool!");
            return;
        }

        foreach (var poolData in poolingConfig.poolConfigs)
        {
            CreatePool(poolData.poolName, poolData.prefab, poolData.initialSize, poolData.maxSize);
        }

        if (poolingConfig.enableDebugLogs)
        {
            Debug.Log($"Created {poolingConfig.poolConfigs.Count} handler pools at game start");
        }
    }

    /// <summary>
    /// Create a pool of COMPLETE HANDLERS
    /// </summary>
    void CreatePool(string poolName, GameObject handlerPrefab, int initialSize, int maxSize)
    {
        if (handlerPrefab == null)
        {
            Debug.LogError($"Cannot create pool '{poolName}' - no handler prefab assigned!");
            return;
        }

        // Create container for organization
        GameObject container = new GameObject($"Pool_{poolName}");
        container.transform.SetParent(transform);
        poolContainers[poolName] = container;

        // Create the queues
        Queue<GameObject> available = new Queue<GameObject>();
        List<GameObject> used = new List<GameObject>();

        // Create initial COMPLETE HANDLERS and put them in available queue
        for (int i = 0; i < initialSize; i++)
        {
            GameObject handler = Instantiate(handlerPrefab, container.transform);
            handler.name = $"{poolName}Handler_{i:00}";
            handler.SetActive(false); // Deactivate the ENTIRE HANDLER
            available.Enqueue(handler);
        }

        availableHandlers[poolName] = available;
        usedHandlers[poolName] = used;

        if (poolingConfig.enableDebugLogs)
        {
            Debug.Log($"Created pool '{poolName}' with {initialSize} complete handlers ready to use (max: {maxSize})");
        }
    }

    /// <summary>
    /// Spawn a COMPLETE HANDLER (LandFishermanHandler or BoatFishermanHandler)
    /// </summary>
    public GameObject Spawn(string poolName, Vector3 position)
    {
        if (!availableHandlers.ContainsKey(poolName))
        {
            Debug.LogError($"Pool '{poolName}' doesn't exist! Check your PoolingConfig.");
            return null;
        }

        Queue<GameObject> available = availableHandlers[poolName];
        List<GameObject> used = usedHandlers[poolName];
        GameObject handlerToSpawn;

        // Try to get a free handler first
        if (available.Count > 0)
        {
            handlerToSpawn = available.Dequeue();
            
            if (showDebugInfo)
                Debug.Log($"Reused handler from pool '{poolName}'. Available left: {available.Count}");
        }
        else
        {
            // No free handlers, need to create a new one
            var poolData = GetPoolData(poolName);
            if (poolData != null && used.Count < poolData.maxSize)
            {
                handlerToSpawn = Instantiate(poolData.prefab, poolContainers[poolName].transform);
                handlerToSpawn.name = $"{poolName}Handler_{used.Count:00}";
                
                if (showDebugInfo)
                    Debug.Log($"Created new handler for pool '{poolName}' (pool was empty). Total: {used.Count + 1}");
            }
            else
            {
                Debug.LogWarning($"Pool '{poolName}' is full! Can't create more handlers.");
                return null;
            }
        }

        // Set up the COMPLETE HANDLER for use
        handlerToSpawn.transform.position = position;
        handlerToSpawn.transform.rotation = Quaternion.identity;
        handlerToSpawn.transform.localScale = Vector3.one;
        
        // Reset the COMPLETE HANDLER
        ResetHandler(handlerToSpawn, position);
        
        // Move handler out of pool to world space
        handlerToSpawn.transform.SetParent(null);
        handlerToSpawn.SetActive(true); // Activate the ENTIRE HANDLER
        used.Add(handlerToSpawn);

        return handlerToSpawn;
    }

    /// <summary>
    /// Return a COMPLETE HANDLER to the pool
    /// </summary>
    public void ReturnToPool(string poolName, GameObject handler)
    {
        if (!availableHandlers.ContainsKey(poolName))
        {
            Debug.LogError($"Pool '{poolName}' doesn't exist!");
            Destroy(handler);
            return;
        }

        Queue<GameObject> available = availableHandlers[poolName];
        List<GameObject> used = usedHandlers[poolName];

        // Remove from used list
        if (used.Contains(handler))
        {
            used.Remove(handler);
        }

        // Clean up the COMPLETE HANDLER
        CleanupHandler(handler);

        // Put back in available queue
        handler.SetActive(false); // Deactivate the ENTIRE HANDLER
        handler.transform.SetParent(poolContainers[poolName].transform);
        available.Enqueue(handler);

        if (showDebugInfo)
        {
            Debug.Log($"Returned handler to pool '{poolName}'. Available: {available.Count}, Used: {used.Count}");
        }
    }

    /// <summary>
    /// FIXED: Reset COMPLETE HANDLER when spawning - comprehensive physics and state reset
    /// </summary>
    /// <summary>
    /// FIXED: Reset COMPLETE HANDLER when spawning - includes child local positions
    /// </summary>
    void ResetHandler(GameObject handler, Vector3 spawnPosition)
    {
        // STEP 1: RESET CHILD POSITIONS TO PREFAB VALUES FIRST
        ResetChildPositions(handler);

        // STEP 2: Reset all physics AFTER positions are corrected
        Rigidbody2D[] rigidbodies = handler.GetComponentsInChildren<Rigidbody2D>();
        foreach (var rb in rigidbodies)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 1f;
            rb.simulated = true;
            rb.freezeRotation = true;
            rb.drag = 0f;
            rb.angularDrag = 0.05f;
            rb.isKinematic = false;
        }

        // STEP 4: Find and reset the Enemy component
        Enemy enemy = handler.GetComponentInChildren<Enemy>();
        if (enemy != null)
        {
            // Reset enemy state
            enemy.ChangeState_Alive();
            enemy.ResetFatigue();
            
            Collider2D collider = enemy.BodyCollider;
            collider.isTrigger = false;
            collider.enabled = true;
            
            // Reset land enemy specific stuff
            if (enemy is LandEnemy landEnemy)
            {
                // Clear old platform assignment
                if (landEnemy.GetAssignedPlatform() != null)
                {
                    landEnemy.GetAssignedPlatform().UnregisterEnemy(landEnemy);
                    landEnemy.SetAssignedPlatform(null);
                }
                
                landEnemy.platformBoundsCalculated = false;
                landEnemy.fishingToolEquipped = false;
                landEnemy.HasStartedFloating = false;
                landEnemy.HasThrownHook = false;
                landEnemy.MovementStateLand = LandEnemy.LandMovementState.Idle;
                
                // Set water mode based on spawn height  
                landEnemy.SetMovementMode(spawnPosition.y > 0f);
                
                // Schedule next action
                landEnemy.ScheduleNextAction();
            }
            
            // FIXED: Force Initialize AFTER everything is reset
            enemy.Initialize();
        }

        // STEP 5: Force Unity to refresh physics
        Physics2D.SyncTransforms();

        if (showDebugInfo)
            Debug.Log($"Handler {handler.name} completely reset with child positions corrected at {spawnPosition}");
    }

    /// <summary>
    /// NEW: Reset child GameObject positions to their prefab values
    /// </summary>
    private void ResetChildPositions(GameObject handler)
    {
        // FIXED: Reset specific known child positions for LandFishermanHandler
        if (handler.name.ToLower().Contains("landfishermanhandler"))
        {
            ResetLandFishermanHandlerPositions(handler);
        }
        else if (handler.name.ToLower().Contains("boatfishermanhandler"))
        {
            ResetBoatFishermanHandlerPositions(handler);
        }
    }

    /// <summary>
    /// FIXED: Reset LandFishermanHandler child positions to prefab values
    /// </summary>
    private void ResetLandFishermanHandlerPositions(GameObject handler)
    {
        Transform handlerTransform = handler.transform;
        
        // Find and reset Fisherman position (should be at Y = 0 according to prefab)
        Transform fishermanTransform = handlerTransform.Find("Fisherman");
        if (fishermanTransform != null)
        {
            fishermanTransform.localPosition = new Vector3(0f, 0f, 0f);
            fishermanTransform.localRotation = Quaternion.identity;
            fishermanTransform.localScale = Vector3.one;
            
            if (showDebugInfo)
                Debug.Log($"Reset Fisherman local position to (0, 0, 0) in {handler.name}");
        }
        
        // Find and reset WaterLine position (should be at Y = -0.3 according to prefab)
        Transform waterLineTransform = handlerTransform.Find("WaterLine");
        if (waterLineTransform != null)
        {
            waterLineTransform.localPosition = new Vector3(0f, -0.3f, 0f);
            waterLineTransform.localRotation = Quaternion.identity;
            waterLineTransform.localScale = Vector3.one;
            
            if (showDebugInfo)
                Debug.Log($"Reset WaterLine local position to (0, -0.3, 0) in {handler.name}");
        }
    }

    /// <summary>
    /// FIXED: Reset BoatFishermanHandler child positions (for future use)
    /// </summary>
    private void ResetBoatFishermanHandlerPositions(GameObject handler)
    {
        Transform handlerTransform = handler.transform;
        
        // Find and reset Fisherman position in boat handler
        Transform fishermanTransform = handlerTransform.Find("Fisherman");
        if (fishermanTransform != null)
        {
            fishermanTransform.localPosition = Vector3.zero;
            fishermanTransform.localRotation = Quaternion.identity;
            fishermanTransform.localScale = Vector3.one;
            
            if (showDebugInfo)
                Debug.Log($"Reset Fisherman local position in {handler.name}");
        }
    }

    /// <summary>
    /// Clean up COMPLETE HANDLER when returning to pool
    /// </summary>
    void CleanupHandler(GameObject handler)
    {
        // Reset transform
        handler.transform.localPosition = Vector3.zero;
        handler.transform.localRotation = Quaternion.identity;
        handler.transform.localScale = Vector3.one;

        // Stop any coroutines in the entire handler
        MonoBehaviour[] components = handler.GetComponentsInChildren<MonoBehaviour>();
        foreach (var component in components)
        {
            if (component != null)
                component.StopAllCoroutines();
        }

        // Clean up enemy references
        Enemy enemy = handler.GetComponentInChildren<Enemy>();
        if (enemy != null && enemy is LandEnemy landEnemy)
        {
            if (landEnemy.GetAssignedPlatform() != null)
            {
                landEnemy.GetAssignedPlatform().UnregisterEnemy(landEnemy);
                landEnemy.SetAssignedPlatform(null);
            }
        }

        if (showDebugInfo)
            Debug.Log($"Handler {handler.name} cleaned up and returned to pool");
    }

    PoolingConfig.PoolData GetPoolData(string poolName)
    {
        if (poolingConfig == null) return null;
        
        foreach (var poolData in poolingConfig.poolConfigs)
        {
            if (poolData.poolName == poolName)
                return poolData;
        }
        return null;
    }

    // Utility methods
    public int GetAvailableCount(string poolName)
    {
        return availableHandlers.ContainsKey(poolName) ? availableHandlers[poolName].Count : 0;
    }

    public int GetUsedCount(string poolName)
    {
        return usedHandlers.ContainsKey(poolName) ? usedHandlers[poolName].Count : 0;
    }

    public int GetActiveCount(string poolName)
    {
        return GetUsedCount(poolName);
    }

    public void LogPoolStats()
    {
        Debug.Log("=== HANDLER POOL STATS ===");
        foreach (string poolName in availableHandlers.Keys)
        {
            int available = GetAvailableCount(poolName);
            int used = GetUsedCount(poolName);
            Debug.Log($"Pool '{poolName}': {available} available handlers, {used} used handlers, {available + used} total");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9))
        {
            LogPoolStats();
        }
    }
}
