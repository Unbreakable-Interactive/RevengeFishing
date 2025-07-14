using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnHandler : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] protected SpawnHandlerConfig spawnConfig;
    [SerializeField] private bool useScriptableObjectConfig = true;
    
    [Header("Legacy Settings (Deprecated)")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private string poolName = "Fisherman";
    [SerializeField] private bool enableAutoSpawning = true;
    [SerializeField] private int maxEnemies = 5;
    [SerializeField] private float minPlayerDistance = 10f;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] configuredSpawnPoints;

    // [Header("References")]
    // [SerializeField] private SimpleObjectPool objectPool;
    // [SerializeField] private Player playerMovement;
    // [SerializeField] private PowerLevelScaler powerLevelScaler;

    [Header("Debug")]
    [SerializeField] private bool enableSpawnLogs = true;

    // Runtime state
    private bool isManualSpawning = false;
    private Coroutine manualSpawnCoroutine;
    private bool spawningEnabled = true;
    private int currentEnemyCount = 0;
    private float lastSpawnTime = 0f;
    private int totalSpawnCount = 0; // For limited spawning
    private bool hasInitialized = false;

    protected virtual void Awake()
    {
        // Initialize from config if available
        if (useScriptableObjectConfig && spawnConfig != null)
        {
            ApplyConfigurationSettings();
        }
    }
    
    protected virtual void Start()
    {
        Initialize();
        
        // Handle initial spawning for boats
        if (spawnConfig != null && spawnConfig.spawnOnAwake)
        {
            StartCoroutine(InitialSpawnDelay());
        }
    }
    
    private void ApplyConfigurationSettings()
    {
        if (spawnConfig == null) return;
        
        // Apply settings from ScriptableObject
        poolName = spawnConfig.poolName;
        spawnInterval = spawnConfig.spawnInterval;
        maxEnemies = spawnConfig.maxEnemies;
        minPlayerDistance = spawnConfig.minPlayerDistance;
        enableAutoSpawning = spawnConfig.enableAutoSpawning;
        enableSpawnLogs = spawnConfig.enableSpawnLogs;
        spawningEnabled = spawnConfig.enableOnStart;
        
        if (enableSpawnLogs)
            Debug.Log($"Applied configuration: {spawnConfig.configName}");
    }
    
    public void SetSpawnConfig(SpawnHandlerConfig config)
    {
        spawnConfig = config;
        useScriptableObjectConfig = true;
        ApplyConfigurationSettings();
        
        // Reinitialize if already started
        if (hasInitialized)
        {
            Initialize();
        }
    }

    private void SetupPowerLevelScaler()
    {
        if (enableSpawnLogs)
        {
            Debug.Log(PowerLevelScaler.Instance != null
                ? "PowerLevelScaler singleton found and ready"
                : "PowerLevelScaler singleton not found in scene!");
        }
    }
    
    private IEnumerator InitialSpawnDelay()
    {
        if (spawnConfig.initialDelay > 0)
            yield return new WaitForSeconds(spawnConfig.initialDelay);
            
        if (spawnConfig.spawnType == SpawnHandlerConfig.SpawnType.Limited)
        {
            StartLimitedSpawning();
        }
    }

    public void Initialize()
    {
        SetupPowerLevelScaler();
        ValidateSpawnPoints();
        
        hasInitialized = true;
        
        if (enableSpawnLogs)
        {
            Debug.Log($"SpawnHandler initialized successfully. Singleton status: " +
                     $"Pool={SimpleObjectPool.Instance != null}, Player={Player.Instance != null}, PowerScaler={PowerLevelScaler.Instance != null}");
        }
    }
    
    public void StartLimitedSpawning()
    {
        if (spawnConfig != null && spawnConfig.spawnType == SpawnHandlerConfig.SpawnType.Limited)
        {
            int spawnCount = Random.Range(1, spawnConfig.maxEnemies + 1); // 1 or 2 for boats
            for (int i = 0; i < spawnCount; i++)
            {
                if (totalSpawnCount < spawnConfig.maxSpawns)
                {
                    SpawnSingleAtRandomPoint();
                    totalSpawnCount++;
                }
            }
            
            if (enableSpawnLogs)
                Debug.Log($"Limited spawning completed: {spawnCount} enemies spawned");
        }
    }

    private void ValidateSpawnPoints()
    {
        // Use configured spawn points if available, otherwise fall back to legacy
        Transform[] activeSpawnPoints = configuredSpawnPoints?.Length > 0 ? configuredSpawnPoints : spawnPoints;
        
        if (activeSpawnPoints == null || activeSpawnPoints.Length == 0)
        {
            Debug.LogError($"SpawnHandler on {gameObject.name} has NO SPAWN POINTS! Add spawn point transforms to the array!");
        }
        else
        {
            Debug.Log($"SpawnHandler found {activeSpawnPoints.Length} spawn points");
        }
    }
    
    private Transform[] GetActiveSpawnPoints()
    {
        return configuredSpawnPoints?.Length > 0 ? configuredSpawnPoints : spawnPoints;
    }

    void Update()
    {
        // MANUAL SPAWNING CONTROLS
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (isManualSpawning)
                StopManualSpawning();
            else
                StartManualSpawning();
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            SpawnSingleAtRandomPoint();
        }

        // AUTO SPAWNING CONTROLS
        if (Input.GetKeyDown(KeyCode.P))
        {
            ToggleAutoSpawning();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            LogSpawnStats();
        }

        // AUTO SPAWNING SYSTEM
        if (enableAutoSpawning && spawningEnabled)
        {
            if (spawnConfig != null)
            {
                switch (spawnConfig.spawnType)
                {
                    case SpawnHandlerConfig.SpawnType.Continuous:
                        AutoSpawnEnemies();
                        break;
                    case SpawnHandlerConfig.SpawnType.Limited:
                        // Limited spawning is handled in Start()
                        break;
                    case SpawnHandlerConfig.SpawnType.PlayerTriggered:
                        CheckPlayerTriggeredSpawn();
                        break;
                }
            }
            else
            {
                AutoSpawnEnemies(); // Fallback to legacy behavior
            }
        }
    }

    #region Manual Spawning System

    public void StartManualSpawning()
    {
        if (!isManualSpawning && SimpleObjectPool.Instance != null && HasValidSpawnPoints())
        {
            isManualSpawning = true;
            manualSpawnCoroutine = StartCoroutine(ManualSpawnRoutine());
            Debug.Log("Started manual continuous spawning");
        }
    }

    public void StopManualSpawning()
    {
        if (isManualSpawning)
        {
            isManualSpawning = false;
            if (manualSpawnCoroutine != null)
            {
                StopCoroutine(manualSpawnCoroutine);
                manualSpawnCoroutine = null;
            }
            Debug.Log("Stopped manual continuous spawning");
        }
    }

    public void SpawnSingleAtRandomPoint()
    {
        if (!HasValidSpawnPoints()) return;

        Vector3 spawnPos = GetRandomSpawnPoint();
        GameObject spawned = SpawnAtPosition(spawnPos);

        if (spawned != null)
        {
            currentEnemyCount++;
            Debug.Log($"Manual spawn at {spawnPos}: {spawned.name}");
        }
    }

    public void SpawnAtSpecificPoint(int pointIndex)
    {
        if (!HasValidSpawnPoints() || pointIndex < 0 || pointIndex >= spawnPoints.Length) return;

        Vector3 spawnPos = spawnPoints[pointIndex].position;
        GameObject spawned = SpawnAtPosition(spawnPos);

        if (spawned != null)
        {
            currentEnemyCount++;
            Debug.Log($"Manual spawn at point {pointIndex}: {spawned.name}");
        }
    }

    private IEnumerator ManualSpawnRoutine()
    {
        while (isManualSpawning)
        {
            Vector3 spawnPos = GetRandomSpawnPoint();
            GameObject spawned = SpawnAtPosition(spawnPos);

            if (spawned != null)
            {
                currentEnemyCount++;
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    #endregion

    #region Auto Spawning System

    private void ToggleAutoSpawning()
    {
        spawningEnabled = !spawningEnabled;
        Debug.Log($"Auto Spawning: {(spawningEnabled ? "ENABLED" : "DISABLED")}");
    }

    private void LogSpawnStats()
    {
        Debug.Log($"=== SPAWN STATS ===");
        Debug.Log($"Active Enemies: {currentEnemyCount}/{maxEnemies}");
        Debug.Log($"Spawn Points: {(spawnPoints?.Length ?? 0)}");
        Debug.Log($"Auto Spawning: {(enableAutoSpawning && spawningEnabled ? "ON" : "OFF")}");
        Debug.Log($"Manual Spawning: {(isManualSpawning ? "ON" : "OFF")}");

        if (SimpleObjectPool.Instance != null)
        {
            int poolCount = SimpleObjectPool.Instance.GetActiveCount(poolName);
            Debug.Log($"Pool '{poolName}': {poolCount} active objects");
        }
    }

    private void AutoSpawnEnemies()
    {
        if (Time.time - lastSpawnTime < spawnInterval) return;
        if (currentEnemyCount >= maxEnemies) return;
        
        // Check spawn limits for limited spawning
        if (spawnConfig != null && spawnConfig.maxSpawns > 0 && totalSpawnCount >= spawnConfig.maxSpawns)
            return;
            
        if (!HasValidSpawnPoints()) return;

        Vector3 spawnPos = GetRandomSpawnPoint();
        if (IsValidSpawnPosition(spawnPos))
        {
            GameObject enemy = SpawnAtPosition(spawnPos);
            if (enemy != null)
            {
                currentEnemyCount++;
                totalSpawnCount++;
                lastSpawnTime = Time.time;

                if (enableSpawnLogs)
                    Debug.Log($"Auto-spawned at {spawnPos}. Total: {currentEnemyCount}");
            }
        }
    }
    
    private void CheckPlayerTriggeredSpawn()
    {
        if (spawnConfig == null || Player.Instance == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, Player.Instance.transform.position);
        if (distanceToPlayer <= spawnConfig.playerDetectionRange && currentEnemyCount < maxEnemies)
        {
            if (Time.time - lastSpawnTime >= spawnInterval)
            {
                SpawnSingleAtRandomPoint();
                lastSpawnTime = Time.time;
            }
        }
    }

    #endregion

    #region Spawn Point Management

    private bool HasValidSpawnPoints()
    {
        Transform[] activeSpawnPoints = GetActiveSpawnPoints();
        if (activeSpawnPoints == null || activeSpawnPoints.Length == 0)
        {
            Debug.LogWarning("No spawn points configured!");
            return false;
        }
        return true;
    }

    private Vector3 GetRandomSpawnPoint()
    {
        if (!HasValidSpawnPoints()) return Vector3.zero;

        Transform[] activeSpawnPoints = GetActiveSpawnPoints();
        Transform randomPoint = activeSpawnPoints[Random.Range(0, activeSpawnPoints.Length)];
        return randomPoint != null ? randomPoint.position : Vector3.zero;
    }

    private bool IsValidSpawnPosition(Vector3 position)
    {
        if (Player.Instance != null)
        {
            float distance = Vector3.Distance(position, Player.Instance.transform.position);
            return distance >= minPlayerDistance;
        }
        return true;
    }

    #endregion

    #region Core Spawning

    private void SetEnemyPowerLevel(GameObject enemy)
    {
        // Cache the enemy component lookup
        Enemy enemyComponent = enemy.GetComponentInChildren<Enemy>();

        if (enemyComponent == null || PowerLevelScaler.Instance == null)
        {
            if (enableSpawnLogs)
            {
                Debug.LogWarning($"Cannot set power level for {enemy.name}! " +
                               $"Enemy Component: {enemyComponent != null}, " +
                               $"PowerLevelScaler: {PowerLevelScaler.Instance != null}");
            }
            return;
        }

        // Single method call and assignment
        int newPowerLevel = PowerLevelScaler.Instance.CalculateEnemyPowerLevel();
        enemyComponent.SetPowerLevel(newPowerLevel);

        // Batch the level display update with the power level setting
        LevelDisplay levelDisplay = enemy.GetComponentInChildren<LevelDisplay>();
        levelDisplay?.SetEntity(enemyComponent);

        // Conditional debug logging (only in editor builds)
        if (enableSpawnLogs)
            Debug.Log($"Set {enemy.name} power level to {newPowerLevel}");
    }

    private GameObject SpawnAtPosition(Vector3 position)
    {
        if (SimpleObjectPool.Instance != null && spawningEnabled)
        {
            GameObject enemy = SimpleObjectPool.Instance.Spawn(poolName, position);

            if (enemy != null)
            {
                // Set power level for the spawned enemy
                SetEnemyPowerLevel(enemy);
            }

            return enemy;
        }
        return null;
    }

    public void OnEnemyDestroyed(GameObject obj)
    {
        if (SimpleObjectPool.Instance != null)
        {
            SimpleObjectPool.Instance.ReturnToPool(poolName, obj);
            currentEnemyCount--;
            if (currentEnemyCount < 0) currentEnemyCount = 0;

            if (enableSpawnLogs)
                Debug.Log($"Enemy destroyed. Remaining: {currentEnemyCount}");
        }
        else
        {
            Debug.LogError("Cannot return enemy to pool - SimpleObjectPool singleton not found!");
            Destroy(obj);
        }
    }

    #endregion

    #region Editor Helpers

    void OnDrawGizmos()
    {
        if (spawnPoints == null) return;

        // Draw spawn points
        Gizmos.color = Color.cyan;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                Vector3 pos = spawnPoints[i].position;
                Gizmos.DrawWireSphere(pos, 1f);

                // Draw spawn point index
                Gizmos.color = Color.white;
                Gizmos.DrawRay(pos, Vector3.up * 2f);

                Gizmos.color = Color.cyan;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (spawnPoints == null) return;

        // Draw spawn points with numbers when selected
        Gizmos.color = Color.yellow;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                Vector3 pos = spawnPoints[i].position;
                Gizmos.DrawSphere(pos, 0.5f);

                // Draw player distance check radius
                if (Player.Instance != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(pos, minPlayerDistance);
                    Gizmos.color = Color.yellow;
                }
            }
        }
    }

    #endregion

    public Transform[] GetSpawnPoints()
    {
        return spawnPoints;
    }

    public string GetPoolName()
    {
        return poolName;
    }
}