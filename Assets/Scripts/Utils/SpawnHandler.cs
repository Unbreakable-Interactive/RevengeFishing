using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnHandler : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 5f;
    
    [Header("Object Pool")]
    [SerializeField] private SimpleObjectPool objectPool;
    [SerializeField] private string poolName = "Fisherman";
    
    [Header("Auto Spawning")]
    [SerializeField] private bool enableAutoSpawning = true;
    [SerializeField] private int maxEnemies = 5;
    [SerializeField] private float minPlayerDistance = 10f;
    
    [Header("Player Reference")]
    [SerializeField] private Player playerMovement;
    
    [Header("Debug")]
    [SerializeField] private bool enableSpawnLogs = true;
    
    private bool isManualSpawning = false;
    private Coroutine manualSpawnCoroutine;
    private bool spawningEnabled = true;
    private int currentEnemyCount = 0;
    private float lastSpawnTime = 0f;

    void Start()
    {
        if (objectPool == null)
            objectPool = FindObjectOfType<SimpleObjectPool>();
        
        if (playerMovement == null)
            playerMovement = FindObjectOfType<Player>();
            
        ValidateSpawnPoints();
    }

    public void Initialize()
    {
        if (objectPool == null)
            objectPool = FindObjectOfType<SimpleObjectPool>();
        
        if (playerMovement == null)
            playerMovement = FindObjectOfType<Player>();
            
        ValidateSpawnPoints();
        Debug.Log("SpawnHandler initialized successfully");
    }

    /// <summary>
    /// Check if there's no spawnpoints and shows an error
    /// </summary>
    private void ValidateSpawnPoints()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError($"SpawnHandler on {gameObject.name} has NO SPAWN POINTS! Add spawn point transforms to the array!");
        }
        else
        {
            Debug.Log($"SpawnHandler found {spawnPoints.Length} spawn points");
        }
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
            AutoSpawnEnemies();
        }
    }

    #region Manual Spawning System
    
    public void StartManualSpawning()
    {
        if (!isManualSpawning && objectPool != null && HasValidSpawnPoints())
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
        
        if (objectPool != null)
        {
            int poolCount = objectPool.GetActiveCount(poolName);
            Debug.Log($"Pool '{poolName}': {poolCount} active objects");
        }
    }
    
    private void AutoSpawnEnemies()
    {
        if (Time.time - lastSpawnTime < spawnInterval) return;
        if (currentEnemyCount >= maxEnemies) return;
        if (!HasValidSpawnPoints()) return;
        
        Vector3 spawnPos = GetRandomSpawnPoint();
        if (IsValidSpawnPosition(spawnPos))
        {
            GameObject enemy = SpawnAtPosition(spawnPos);
            if (enemy != null)
            {
                currentEnemyCount++;
                lastSpawnTime = Time.time;
                
                if (enableSpawnLogs)
                    Debug.Log($"Auto-spawned at {spawnPos}. Total: {currentEnemyCount}");
            }
        }
    }
    
    #endregion

    #region Spawn Point Management
    
    private bool HasValidSpawnPoints()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("No spawn points configured!");
            return false;
        }
        return true;
    }
    
    private Vector3 GetRandomSpawnPoint()
    {
        if (!HasValidSpawnPoints()) return Vector3.zero;
        
        Transform randomPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        return randomPoint != null ? randomPoint.position : Vector3.zero;
    }
    
    private bool IsValidSpawnPosition(Vector3 position)
    {
        if (playerMovement != null)
        {
            float distance = Vector3.Distance(position, playerMovement.transform.position);
            return distance >= minPlayerDistance;
        }
        return true;
    }
    
    #endregion

    #region Core Spawning
    
    private GameObject SpawnAtPosition(Vector3 position)
    {
        if (objectPool != null && spawningEnabled)
        {
            return objectPool.Spawn(poolName, position);
        }
        return null;
    }
    
    public void OnEnemyDestroyed(GameObject obj)
    {
        objectPool.ReturnToPool(poolName,obj);
        currentEnemyCount--;
        if (currentEnemyCount < 0) currentEnemyCount = 0;
        
        if (enableSpawnLogs)
            Debug.Log($"Enemy destroyed. Remaining: {currentEnemyCount}");
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
                if (playerMovement != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(pos, minPlayerDistance);
                    Gizmos.color = Color.yellow;
                }
            }
        }
    }
    
    #endregion
}
