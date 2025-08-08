using System.Collections.Generic;
using UnityEngine;

public class SimpleObjectPool : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PoolingConfig poolingConfig;
    
    [Header("Runtime Info (Read Only)")]
    [SerializeField] private bool showDebugInfo = true;
    
    private Dictionary<string, Queue<GameObject>> availableHandlers = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, HashSet<GameObject>> usedHandlers = new Dictionary<string, HashSet<GameObject>>();
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
            GameLogger.LogError("No PoolingConfig assigned to SimpleObjectPool!");
            return;
        }

        foreach (var poolData in poolingConfig.poolConfigs)
        {
            CreatePool(poolData.poolName, poolData.prefab, poolData.initialSize, poolData.maxSize);
        }

        if (poolingConfig.enableDebugLogs)
        {
            GameLogger.Log($"Created {poolingConfig.poolConfigs.Count} handler pools at game start");
        }
    }

    void CreatePool(string poolName, GameObject handlerPrefab, int initialSize, int maxSize)
    {
        if (handlerPrefab == null)
        {
            GameLogger.LogError($"Cannot create pool '{poolName}' - no handler prefab assigned!");
            return;
        }

        GameObject container = new GameObject($"Pool_{poolName}");
        container.transform.SetParent(transform);
        poolContainers[poolName] = container;

        Queue<GameObject> available = new Queue<GameObject>();
        HashSet<GameObject> used = new HashSet<GameObject>();

        for (int i = 0; i < initialSize; i++)
        {
            GameObject handler = Instantiate(handlerPrefab, container.transform);
            handler.name = $"{poolName}Handler_{i:00}";
            handler.SetActive(false);
            available.Enqueue(handler);
        }

        availableHandlers[poolName] = available;
        usedHandlers[poolName] = used;

        if (poolingConfig.enableDebugLogs)
        {
            GameLogger.LogVerbose($"Created pool '{poolName}' with {initialSize} complete handlers ready to use (max: {maxSize})");
        }
    }

    public GameObject Spawn(string poolName, Vector3 position)
    {
        if (!availableHandlers.ContainsKey(poolName))
        {
            GameLogger.LogError($"Pool '{poolName}' doesn't exist! Check your PoolingConfig.");
            return null;
        }

        Queue<GameObject> available = availableHandlers[poolName];
        HashSet<GameObject> used = usedHandlers[poolName];
        GameObject handlerToSpawn;

        if (available.Count > 0)
        {
            handlerToSpawn = available.Dequeue();
            
            if (showDebugInfo)
                GameLogger.LogVerbose($"Reused handler from pool '{poolName}'. Available left: {available.Count}");
        }
        else
        {
            var poolData = GetPoolData(poolName);
            if (poolData != null && used.Count < poolData.maxSize)
            {
                handlerToSpawn = Instantiate(poolData.prefab, poolContainers[poolName].transform);
                handlerToSpawn.name = $"{poolName}Handler_{used.Count:00}";
                
                if (showDebugInfo)
                    GameLogger.LogVerbose($"Created new handler for pool '{poolName}' (pool was empty). Total: {used.Count + 1}");
            }
            else
            {
                GameLogger.LogWarning($"Pool '{poolName}' is full! Can't create more handlers.");
                return null;
            }
        }

        handlerToSpawn.transform.position = position;
        handlerToSpawn.transform.rotation = Quaternion.identity;
        handlerToSpawn.transform.localScale = Vector3.one;
        
        ResetHandler(handlerToSpawn, position);
        
        handlerToSpawn.transform.SetParent(null);
        handlerToSpawn.SetActive(true);
        used.Add(handlerToSpawn);

        return handlerToSpawn;
    }

    public void ReturnToPool(string poolName, GameObject handler)
    {
        if (handler == null || !handler.activeInHierarchy)
        {
            if (showDebugInfo)
                GameLogger.LogVerbose($"Cannot return to pool '{poolName}' - handler is null or already inactive");
            return;
        }
        
        if (!poolContainers.ContainsKey(poolName))
        {
            GameLogger.LogError($"Pool '{poolName}' not found!");
            return;
        }

        Queue<GameObject> available = availableHandlers[poolName];
        HashSet<GameObject> used = usedHandlers[poolName];

        if (!used.Contains(handler))
        {
            if (showDebugInfo)
                GameLogger.LogVerbose($"Handler not found in used pool '{poolName}', skipping return");
            return;
        }

        used.Remove(handler);
        CleanupHandler(handler);

        if (handler != null && handler.transform != null)
        {
            try
            {
                handler.SetActive(false);
                handler.transform.SetParent(poolContainers[poolName].transform);
                available.Enqueue(handler);
            }
            catch (System.Exception e)
            {
                GameLogger.LogError($"Error returning handler to pool '{poolName}': {e.Message}");
                return;
            }
        }

        if (showDebugInfo)
        {
            GameLogger.LogVerbose($"Returned handler to pool '{poolName}'. Available: {available.Count}, Used: {used.Count}");
        }
    }

    void ResetHandler(GameObject handler, Vector3 spawnPosition)
    {
        ResetChildPositions(handler);

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

        Enemy enemy = handler.GetComponentInChildren<Enemy>();
        if (enemy != null)
        {
            enemy.ChangeState_Alive();
            enemy.ResetFatigue();
            
            Collider2D collider = enemy.BodyCollider;
            collider.isTrigger = false;
            collider.enabled = true;
            
            if (enemy is LandEnemy landEnemy)
            {
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
                
                landEnemy.SetMovementMode(spawnPosition.y > 0f);
                
                landEnemy.ScheduleNextAction();
            }
            
            enemy.Initialize();
        }

        Physics2D.SyncTransforms();

        if (showDebugInfo)
            GameLogger.LogVerbose($"Handler {handler.name} completely reset with child positions corrected at {spawnPosition}");
    }

    private void ResetChildPositions(GameObject handler)
    {
        if (handler.name.ToLower().Contains("landfishermanhandler"))
        {
            ResetLandFishermanHandlerPositions(handler);
        }
        else if (handler.name.ToLower().Contains("boatfishermanhandler"))
        {
            ResetBoatFishermanHandlerPositions(handler);
        }
    }

    private void ResetLandFishermanHandlerPositions(GameObject handler)
    {
        Transform handlerTransform = handler.transform;
        
        Transform fishermanTransform = handlerTransform.Find("Fisherman");
        if (fishermanTransform != null)
        {
            fishermanTransform.localPosition = new Vector3(0f, 0f, 0f);
            fishermanTransform.localRotation = Quaternion.identity;
            fishermanTransform.localScale = Vector3.one;
            
            if (showDebugInfo)
                GameLogger.LogVerbose($"Reset Fisherman local position to (0, 0, 0) in {handler.name}");
        }
        
        Transform waterLineTransform = handlerTransform.Find("WaterLine");
        if (waterLineTransform != null)
        {
            waterLineTransform.localPosition = new Vector3(0f, -0.3f, 0f);
            waterLineTransform.localRotation = Quaternion.identity;
            waterLineTransform.localScale = Vector3.one;
            
            if (showDebugInfo)
                GameLogger.LogVerbose($"Reset WaterLine local position to (0, -0.3, 0) in {handler.name}");
        }
    }

    private void ResetBoatFishermanHandlerPositions(GameObject handler)
    {
        Transform handlerTransform = handler.transform;
        
        Transform fishermanTransform = handlerTransform.Find("Fisherman");
        if (fishermanTransform != null)
        {
            fishermanTransform.localPosition = Vector3.zero;
            fishermanTransform.localRotation = Quaternion.identity;
            fishermanTransform.localScale = Vector3.one;
            
            if (showDebugInfo)
                GameLogger.LogVerbose($"Reset Fisherman local position in {handler.name}");
        }
    }

    void CleanupHandler(GameObject handler)
    {
        handler.transform.localPosition = Vector3.zero;
        handler.transform.localRotation = Quaternion.identity;
        handler.transform.localScale = Vector3.one;

        MonoBehaviour[] components = handler.GetComponentsInChildren<MonoBehaviour>();
        foreach (var component in components)
        {
            if (component != null)
                component.StopAllCoroutines();
        }

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
            GameLogger.LogVerbose($"Handler {handler.name} cleaned up and returned to pool");
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
        GameLogger.Log("=== HANDLER POOL STATS ===");
        foreach (string poolName in availableHandlers.Keys)
        {
            int available = GetAvailableCount(poolName);
            int used = GetUsedCount(poolName);
            GameLogger.Log($"Pool '{poolName}': {available} available handlers, {used} used handlers, {available + used} total");
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
