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

    private float nextSpawnTime;
    private int spawnedThisCycle = 0;
    private bool inCooldown = false;
    private float cooldownEndTime;

    private bool oneTimeCompleted = false;

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
    }

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

        if (Time.time >= nextSpawnTime)
        {
            if (spawnedThisCycle < spawnConfig.spawnThisManyPerCycle)
            {
                if (TrySpawnEnemy())
                {
                    spawnedThisCycle++;
                    ScheduleNextSpawn();

                    if (spawnedThisCycle >= spawnConfig.spawnThisManyPerCycle)
                    {
                        StartCooldown();
                    }
                }
            }
        }
    }

    void HandleOneTimeSpawning()
    {
        if (Time.time >= nextSpawnTime && !oneTimeCompleted)
        {
            if (TrySpawnEnemy())
            {
                oneTimeCompleted = true;

                if (spawnConfig.showLogs)
                    Debug.Log($"{spawnConfig.configName}: Initial OneTime spawn completed");
            }
        }

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
        if (spawnConfig.spawnHandlerType == SpawnHandlerType.PerPoint)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector3 pos = spawnPoints[Random.Range(0, spawnPoints.Length)].position;
                if (spawnConfig.IsValidDistance(pos))
                {
                    if (spawnConfig.enemyType == SpawnHandlerConfig.EnemyType.Boat)
                    {
                        if (BoatSpawnHelper.IsPositionFreeForBoat(pos, spawnConfig.showLogs))
                        {
                            return pos;
                        }
                    }
                    else
                    {
                        return pos;
                    }
                }
            }
        }
        else
        {
            if (spawnPoints.Length != 2)
            {
                Debug.LogError("Spawn handler type Zone only supports 2 SpawnPoints");
                return Vector3.zero;
            }

            if (spawnConfig.enemyType == SpawnHandlerConfig.EnemyType.Boat)
            {
                Vector3 validBoatPos = BoatSpawnHelper.FindValidBoatSpawnPosition(
                    spawnPoints[0].position, 
                    spawnPoints[1].position, 
                    spawnConfig);
                
                return validBoatPos;
            }
            else
            {
                Vector3 tempPos = spawnPoints[0].position;
                float xPosition = Random.Range(tempPos.x, spawnPoints[1].position.x);
                Vector3 pos = new Vector3(xPosition, tempPos.y, 0);
                return pos;
            }
        }

        if (spawnConfig.showLogs)
            Debug.LogWarning($"{spawnConfig.configName}: Couldn't find valid spawn position");

        return Vector3.zero;
    }

    void SetupEnemy(GameObject enemy, Vector3 spawnPos)
    {
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

        if (spawnConfig.enemyType == SpawnHandlerConfig.EnemyType.LandFisherman)
        {
            StartCoroutine(AssignToPlatform(enemy, spawnPos));
        }
        
        if (spawnConfig.enemyType == SpawnHandlerConfig.EnemyType.Boat)
        {
            StartCoroutine(InitializeBoatController(enemy, spawnPoints));
        }
    }
    
    IEnumerator InitializeBoatController(GameObject boatObject, Transform[] spawnPoints)
    {
        yield return null;
    
        BoatController boatController = boatObject.GetComponent<BoatController>();
        if (boatController != null)
        {
            Transform leftBoundary = spawnPoints[0];
            Transform rightBoundary = spawnPoints[1];
        
            boatController.Initialize(leftBoundary, rightBoundary);
        
            if (spawnConfig.showLogs)
            {
                Debug.Log($"BoatController initialized for {boatObject.name} with boundaries L:{leftBoundary?.name} R:{rightBoundary?.name}");
            }
        }
        else
        {
            Debug.LogError($"Spawned boat {boatObject.name} doesn't have BoatController component!");
        }
    }

    IEnumerator AssignToPlatform(GameObject enemy, Vector3 spawnPos)
    {
        yield return null;

        LandEnemy landEnemy = enemy.GetComponentInChildren<LandEnemy>();
        if (landEnemy != null)
        {
            Platform platform = FindNearestPlatform(spawnPos);
            if (platform != null)
            {
                // platform.RegisterEnemyAtRuntime(landEnemy);
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

    public void OnEnemyDestroyed(GameObject enemyObj = null)
    {
        currentActive--;
        if (currentActive < 0) currentActive = 0;

        if (spawnConfig != null && spawnConfig.showLogs)
        {
            string objName = enemyObj != null ? enemyObj.name : "Unknown";
            Debug.Log($"Enemy {objName} destroyed for {spawnConfig.configName}. Active: {currentActive}");
        }

        if (spawnConfig.spawnType == SpawnHandlerConfig.SpawnType.OneTime && oneTimeCompleted)
        {
            ScheduleNextSpawn();
        }
    }

    public void SpawnOne()
    {
        TrySpawnEnemy();
    }

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

#if UNITY_EDITOR
    void LogStats()
    {
        Debug.Log($"=== {spawnConfig.configName} ===");
        Debug.Log($"Active: {currentActive}, Unlocked: {isUnlocked}, In Cooldown: {inCooldown}");
        Debug.Log($"Spawned this cycle: {spawnedThisCycle}, OneTime completed: {oneTimeCompleted}");
    }
#endif
}
