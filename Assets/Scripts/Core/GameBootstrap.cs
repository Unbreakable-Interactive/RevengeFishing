using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [Header("Core Game Systems")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerBounds playerBounds;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Camera gameCamera;
    
    [Header("Spawn Systems")]
    [SerializeField] private SpawnHandler spawnHandler;
    [SerializeField] private SimpleObjectPool objectPool;
    
    [Header("Auto Spawning")]
    [SerializeField] private bool enableAutoSpawning = true;
    [SerializeField] private string enemyPoolName = "Fisherman";
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 8f;
    [SerializeField] private int maxEnemies = 5;
    [SerializeField] private float minPlayerDistance = 10f;
    
    [Header("Debug")]
    [SerializeField] private bool enableBootstrapLogs = true;
    
    private bool spawningEnabled = true;
    private int currentEnemyCount = 0;
    private float lastSpawnTime = 0f;
    
    private void Start()
    {
        InitializeGameSystems();
    }
    
    private void InitializeGameSystems()
    {
        try
        {
            if (enableBootstrapLogs)
                Debug.Log("GameBootstrap: Starting initialization...");
            
            InitializeCoreSystemsPhase();
            InitializePlayerSystemsPhase();
            InitializeSpawnSystemsPhase();
            
            if (enableBootstrapLogs)
                Debug.Log("GameBootstrap: All systems ready!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GameBootstrap failed: {e.Message}");
        }
    }
    
    private void InitializeCoreSystemsPhase()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;
    }
    
    private void InitializePlayerSystemsPhase()
    {
        if (playerStats != null)
            playerStats.Initialize();
        
        if (playerMovement != null)
            playerMovement.Initialize();
        
        if (playerBounds != null && playerMovement != null)
            playerBounds.Initialize(playerMovement);
    }
    
    private void InitializeSpawnSystemsPhase()
    {
        if (spawnHandler != null)
            spawnHandler.Initialize();
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) && enableBootstrapLogs)
        {
            Debug.Log("GameBootstrap: Manual restart");
            InitializeGameSystems();
        }
        
        if (Input.GetKeyDown(KeyCode.P) && enableBootstrapLogs)
        {
            ToggleSpawning();
        }
        
        if (Input.GetKeyDown(KeyCode.T) && enableBootstrapLogs)
        {
            LogSpawnStats();
        }
        
        if (enableAutoSpawning && spawningEnabled)
        {
            AutoSpawnEnemies();
        }
    }
    
    private void ToggleSpawning()
    {
        spawningEnabled = !spawningEnabled;
        Debug.Log($"Spawning: {(spawningEnabled ? "ENABLED" : "DISABLED")}");
    }
    
    private void LogSpawnStats()
    {
        Debug.Log($"Active Enemies: {currentEnemyCount}/{maxEnemies}");
        if (objectPool != null)
        {
            int poolCount = objectPool.GetActiveCount(enemyPoolName);
            Debug.Log($"Pool '{enemyPoolName}': {poolCount} active objects");
        }
    }
    
    private void AutoSpawnEnemies()
    {
        if (Time.time - lastSpawnTime < spawnInterval) return;
        if (currentEnemyCount >= maxEnemies) return;
        
        Vector3 spawnPos = GetRandomSpawnPosition();
        if (IsValidSpawnPosition(spawnPos))
        {
            GameObject enemy = SpawnEnemy(enemyPoolName, spawnPos);
            if (enemy != null)
            {
                currentEnemyCount++;
                lastSpawnTime = Time.time;
                
                if (enableBootstrapLogs)
                    Debug.Log($"Auto-spawned enemy at {spawnPos}. Total: {currentEnemyCount}");
            }
        }
    }
    
    private Vector3 GetRandomSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform randomPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            return randomPoint.position;
        }
        
        Vector3 playerPos = playerMovement != null ? playerMovement.transform.position : Vector3.zero;
        Vector3 randomOffset = new Vector3(Random.Range(-15f, 15f), Random.Range(-5f, 5f), 0f);
        return playerPos + randomOffset;
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
    
    public GameObject SpawnEnemy(string poolName, Vector3 position)
    {
        if (objectPool != null && spawningEnabled)
        {
            return objectPool.Spawn(poolName, position);
        }
        return null;
    }
    
    public void OnEnemyDestroyed()
    {
        currentEnemyCount--;
        if (currentEnemyCount < 0) currentEnemyCount = 0;
        
        if (enableBootstrapLogs)
            Debug.Log($"Enemy destroyed. Remaining: {currentEnemyCount}");
    }
    
    public void SpawnEnemyManually()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Vector3 spawnPos = GetRandomSpawnPosition();
            SpawnEnemy(enemyPoolName, spawnPos);
        }
    }
}
