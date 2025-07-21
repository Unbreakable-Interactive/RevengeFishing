using System.Collections;
using UnityEngine;

public class SpawnHandler : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private SpawnHandlerConfig spawnConfig;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Current State (Read Only)")]
    [SerializeField] private bool isUnlocked = false;
    [SerializeField] private int currentActive = 0;
    [SerializeField] private bool isActive = true;

    // Private variables
    private float nextSpawnTime;
    private int spawnedThisCycle = 0;
    private bool inCooldown = false;
    private float cooldownEndTime;

    // FIXED: Track if OneTime spawning already completed
    private bool oneTimeCompleted = false;

    // Public property to access config from other scripts
    public SpawnHandlerConfig config => spawnConfig;

    void Start()
    {
        Initialize();
    }

    void Update()
    {
        if (!isActive || spawnConfig == null) return;

        CheckUnlockStatus();

        if (!isUnlocked) return;

        HandleSpawning();

        // Debug keys para testing
        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log($"🎮 Manual spawn triggered for {spawnConfig.configName}!");
            SpawnOne();
        }

        if (Input.GetKeyDown(KeyCode.G)) LogStats();

        // NUEVAS TECLAS PARA TESTING
        if (Input.GetKeyDown(KeyCode.R)) ResetAllEnemiesOfThisType();
        if (Input.GetKeyDown(KeyCode.T)) TestEnemyDefeatOfThisType();
    }

    /// <summary>
    /// PUBLIC: Initialize method for GameBootstrap
    /// </summary>
    public void Initialize()
    {
        if (spawnConfig == null)
        {
            Debug.LogError($"SpawnHandler on {gameObject.name} needs a config!");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError($"SpawnHandler on {gameObject.name} needs spawn points!");
            return;
        }

        nextSpawnTime = Time.time + 2f; // Initial delay
        oneTimeCompleted = false; // FIXED: Reset oneTime flag

        if (spawnConfig.showLogs)
        {
            Debug.Log($"SpawnHandler ready: {spawnConfig.configName}");
        }
    }

    void HandleSpawning()
    {
        switch (spawnConfig.spawnType)
        {
            case SpawnHandlerConfig.SpawnType.Continuous:
                HandleContinuousSpawning();
                break;

            case SpawnHandlerConfig.SpawnType.Cycles:
                HandleCycleSpawning();
                break;

            case SpawnHandlerConfig.SpawnType.OneTime:
                HandleOneTimeSpawning();
                break;
        }
    }

    void HandleContinuousSpawning()
    {
        if (Time.time >= nextSpawnTime)
        {
            if (currentActive < spawnConfig.keepActiveAtOnce)
            {
                if (TrySpawnEnemy())
                {
                    ScheduleNextSpawn();
                }
            }
        }
    }

    void HandleCycleSpawning()
    {
        // Handle cooldown period
        if (inCooldown)
        {
            if (Time.time >= cooldownEndTime)
            {
                inCooldown = false;
                spawnedThisCycle = 0;

                if (spawnConfig.showLogs)
                    Debug.Log($"{spawnConfig.configName}: Cooldown ended, starting new cycle");
            }
            return;
        }

        // Normal spawning in cycle
        if (Time.time >= nextSpawnTime)
        {
            if (spawnedThisCycle < spawnConfig.spawnThisManyPerCycle)
            {
                if (TrySpawnEnemy())
                {
                    spawnedThisCycle++;
                    ScheduleNextSpawn();

                    // Check if cycle complete
                    if (spawnedThisCycle >= spawnConfig.spawnThisManyPerCycle)
                    {
                        StartCooldown();
                    }
                }
            }
        }
    }

    /// <summary>
    /// FIXED: OneTime spawning that doesn't prevent respawning when enemies die
    /// </summary>
    void HandleOneTimeSpawning()
    {
        // FIXED: Only spawn initially once, but don't stop spawning when enemies die
        if (Time.time >= nextSpawnTime && !oneTimeCompleted)
        {
            if (TrySpawnEnemy())
            {
                oneTimeCompleted = true;

                if (spawnConfig.showLogs)
                    Debug.Log($"{spawnConfig.configName}: Initial OneTime spawn completed");
            }
        }

        // FIXED: Allow respawning after enemies die, but maintain the limit
        else if (oneTimeCompleted && currentActive < spawnConfig.keepActiveAtOnce)
        {
            if (Time.time >= nextSpawnTime)
            {
                if (TrySpawnEnemy())
                {
                    ScheduleNextSpawn();

                    if (spawnConfig.showLogs)
                        Debug.Log($"{spawnConfig.configName}: OneTime respawn after enemy death");
                }
            }
        }
    }

    void StartCooldown()
    {
        inCooldown = true;
        cooldownEndTime = Time.time + spawnConfig.waitBetweenCycles;

        if (spawnConfig.showLogs)
            Debug.Log($"{spawnConfig.configName}: Starting cooldown for {spawnConfig.waitBetweenCycles} seconds");
    }

    bool TrySpawnEnemy()
    {
        Vector3 spawnPos = GetValidSpawnPosition();
        if (spawnPos == Vector3.zero) return false;

        GameObject enemy = SimpleObjectPool.Instance.Spawn(spawnConfig.poolName, spawnPos);
        if (enemy != null)
        {
            currentActive++;
            SetupEnemy(enemy, spawnPos);

            if (spawnConfig.showLogs)
                Debug.Log($"Spawned {spawnConfig.enemyType} at {spawnPos}. Active: {currentActive}");

            return true;
        }

        return false;
    }

    Vector3 GetValidSpawnPosition()
    {
        for (int i = 0; i < 10; i++) // Try 10 times
        {
            Vector3 pos = spawnPoints[Random.Range(0, spawnPoints.Length)].position;
            if (spawnConfig.IsValidDistance(pos))
            {
                return pos;
            }
        }

        if (spawnConfig.showLogs)
            Debug.LogWarning($"{spawnConfig.configName}: Couldn't find valid spawn position");

        return Vector3.zero;
    }

    void SetupEnemy(GameObject enemy, Vector3 spawnPos)
    {
        // Set power level
        if (PowerLevelScaler.Instance != null)
        {
            Enemy enemyComp = enemy.GetComponentInChildren<Enemy>();
            if (enemyComp != null)
            {
                int powerLevel = PowerLevelScaler.Instance.CalculateEnemyPowerLevel();
                enemyComp.SetPowerLevel(powerLevel);

                LevelDisplay levelDisplay = enemy.GetComponentInChildren<LevelDisplay>();
                levelDisplay?.SetEntity(enemyComp);
            }
        }

        // Handle platform assignment for land enemies
        if (spawnConfig.enemyType == SpawnHandlerConfig.EnemyType.LandFisherman)
        {
            StartCoroutine(AssignToPlatform(enemy, spawnPos));
        }

        // Handle platform assignment for boat enemies
        if (spawnConfig.enemyType == SpawnHandlerConfig.EnemyType.BoatFisherman)
        {
            StartCoroutine(AssignToPlatform(enemy, spawnPos));
        }
    }

    IEnumerator AssignToPlatform(GameObject enemy, Vector3 spawnPos)
    {
        yield return null; // Wait one frame

        LandEnemy landEnemy = enemy.GetComponentInChildren<LandEnemy>();
        if (landEnemy != null)
        {
            Platform platform = FindNearestPlatform(spawnPos);
            if (platform != null)
            {
                platform.RegisterEnemyAtRuntime(landEnemy);
            }
        }
    }

    Platform FindNearestPlatform(Vector3 position)
    {
        Platform[] platforms = FindObjectsOfType<Platform>();
        Platform nearest = null;
        float closestDistance = 15f;

        foreach (Platform platform in platforms)
        {
            float distance = Vector3.Distance(position, platform.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearest = platform;
            }
        }

        return nearest;
    }

    void ScheduleNextSpawn()
    {
        nextSpawnTime = Time.time + spawnConfig.GetSpawnInterval();
    }

    void CheckUnlockStatus()
    {
        if (!spawnConfig.needsUnlock)
        {
            isUnlocked = true;
            return;
        }

        if (PowerLevelScaler.Instance != null)
        {
            int playerLevel = PowerLevelScaler.Instance.GetPlayerPowerLevel();
            bool wasUnlocked = isUnlocked;
            isUnlocked = spawnConfig.IsUnlocked(playerLevel);

            if (!wasUnlocked && isUnlocked && spawnConfig.showLogs)
            {
                Debug.Log($"{spawnConfig.configName} UNLOCKED! Player level: {playerLevel}");
            }
        }
    }

    /// <summary>
    /// Called when enemy dies - basic version for compatibility
    /// </summary>
    public void OnEnemyDestroyed()
    {
        currentActive--;
        if (currentActive < 0) currentActive = 0;

        if (spawnConfig != null && spawnConfig.showLogs)
            Debug.Log($"Enemy destroyed for {spawnConfig.configName}. Active: {currentActive}");
    }

    /// <summary>
    /// FIXED: Called when enemy dies with enemy reference for better tracking
    /// </summary>
    public void OnEnemyDestroyed(GameObject enemyObj)
    {
        currentActive--;
        if (currentActive < 0) currentActive = 0;

        if (spawnConfig != null && spawnConfig.showLogs)
            Debug.Log($"Enemy {enemyObj.name} returned to pool for {spawnConfig.configName}. Active: {currentActive}");

        // FIXED: For OneTime spawners, schedule next spawn after enemy death
        if (spawnConfig.spawnType == SpawnHandlerConfig.SpawnType.OneTime && oneTimeCompleted)
        {
            ScheduleNextSpawn();
        }
    }

    /// <summary>
    /// PUBLIC: Spawn single at random point for ProgressionManager
    /// </summary>
    public void SpawnSingleAtRandomPoint()
    {
        TrySpawnEnemy();
    }

    /// <summary>
    /// Manual spawn for testing
    /// </summary>
    public void SpawnOne()
    {
        TrySpawnEnemy();
    }

    /// <summary>
    /// FIXED: Reset oneTime flag for testing
    /// </summary>
    public void ResetSpawner()
    {
        oneTimeCompleted = false;
        currentActive = 0;
        spawnedThisCycle = 0;
        inCooldown = false;
        nextSpawnTime = Time.time + 2f;

        if (spawnConfig.showLogs)
            Debug.Log($"🔄 {spawnConfig.configName} spawner reset");
    }

    void LogStats()
    {
        Debug.Log($"=== {spawnConfig.configName} ===");
        Debug.Log($"Active: {currentActive}, Unlocked: {isUnlocked}, In Cooldown: {inCooldown}");
        Debug.Log($"Spawned this cycle: {spawnedThisCycle}, OneTime completed: {oneTimeCompleted}");
    }

    /// <summary>
    /// TESTING: Reset all enemies of this spawner's type
    /// </summary>
    private void ResetAllEnemiesOfThisType()
    {
        Enemy[] allEnemies = FindObjectsOfType<Enemy>();
        int resetCount = 0;

        foreach (Enemy enemy in allEnemies)
        {
            if (enemy.gameObject.activeInHierarchy && ShouldManageThisEnemy(enemy))
            {
                enemy.TriggerAlive();
                resetCount++;
                Debug.Log($"🔄 Reset enemy: {enemy.gameObject.name}");
            }
        }
        Debug.Log($"🔄 Reset {resetCount} enemies of type {spawnConfig.enemyType}");
    }

    /// <summary>
    /// TESTING: Force defeat on first enemy of this type
    /// </summary>
    private void TestEnemyDefeatOfThisType()
    {
        Enemy[] allEnemies = FindObjectsOfType<Enemy>();

        foreach (Enemy enemy in allEnemies)
        {
            if (enemy.gameObject.activeInHierarchy &&
                enemy.GetState() == Enemy.EnemyState.Alive &&
                ShouldManageThisEnemy(enemy))
            {
                Debug.Log($"💀 Forcing defeat on: {enemy.gameObject.name} (Type: {spawnConfig.enemyType})");
                // Simulate enough fatigue to defeat
                enemy.TakeFatigue(enemy.entityFatigue.maxFatigue);
                break;
            }
        }
    }

    /// <summary>
    /// FIXED: Check if this spawner should manage this enemy
    /// </summary>
    private bool ShouldManageThisEnemy(Enemy enemy)
    {
        if (spawnConfig.enemyType == SpawnHandlerConfig.EnemyType.LandFisherman)
        {
            return enemy is LandEnemy;
        }
        else if (spawnConfig.enemyType == SpawnHandlerConfig.EnemyType.BoatFisherman)
        {
            return enemy.gameObject.name.ToLower().Contains("boatfisherman");
        }

        return false;
    }

    // Gizmos
    void OnDrawGizmos()
    {
        if (spawnConfig == null || !spawnConfig.showGizmos || spawnPoints == null) return;

        Gizmos.color = isUnlocked ? spawnConfig.gizmoColor : Color.red;

        foreach (Transform point in spawnPoints)
        {
            if (point != null)
            {
                Gizmos.DrawWireSphere(point.position, 1f);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (spawnConfig == null || spawnPoints == null) return;

        foreach (Transform point in spawnPoints)
        {
            if (point != null)
            {
                // Min distance (red)
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(point.position, spawnConfig.dontSpawnCloserThan);

                // Max distance (blue)
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(point.position, spawnConfig.dontSpawnFartherThan);
            }
        }
    }
}
