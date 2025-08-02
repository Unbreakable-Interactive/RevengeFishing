using UnityEngine;

public enum SpawnHandlerType
{
    PerPoint,
    Zone
}


[CreateAssetMenu(fileName = "New Spawn Config", menuName = "Fishing Game/Spawn Config")]
public class SpawnHandlerConfig : ScriptableObject
{
    [Header("What does this spawner do?")]
    public string configName = "Land Fisherman Spawner";
    
    [Space(10)]
    [Header("Basic Settings")]
    [Tooltip("What pool to use (must match a pool name in PoolingConfig)")]
    public string poolName = "LandFisherman";
    
    [Header("Spawn Handler Type")]
    [Tooltip("For PerPoint type you can have multiple SpawnPoints /nFor Zone type you MUST HAVE ONLY 2 SPAWNPOINTS TO MAKE A SPAWN ZONE")]
    public SpawnHandlerType spawnHandlerType;
    
    [Tooltip("What type of enemy this is")]
    public EnemyType enemyType = EnemyType.LandFisherman;
    
    [Space(10)]
    [Header("How to Spawn")]
    [Tooltip("How this spawner works")]
    public SpawnType spawnType = SpawnType.Continuous;
    
    [Tooltip("Spawn every X seconds")]
    [Range(0f, 30f)]
    public float spawnEveryXSeconds = 5f;
    
    [Tooltip("How many enemies to keep active at the same time")]
    [Range(1, 10)]
    public int keepActiveAtOnce = 3;
    
    [Space(10)]
    [Header("Boat Settings (only for boats)")]
    [Tooltip("For boats: spawn this many then wait")]
    [Range(1, 3)]
    public int spawnThisManyPerCycle = 1;
    
    [Tooltip("For boats: wait this long before next cycle")]
    [Range(10f, 60f)]
    public float waitBetweenCycles = 30f;
    
    [Space(10)]
    [Header("Unlock Settings")]
    [Tooltip("Does this need to be unlocked?")]
    public bool needsUnlock = false;
    
    [Tooltip("Player level needed to unlock")]
    [Range(1, 20)]
    public int playerLevelNeeded = 1;
    
    [Space(10)]
    [Header("Distance from Player")]
    [Tooltip("Don't spawn closer than this")]
    [Range(5f, 30f)]
    public float dontSpawnCloserThan = 10f;
    
    [Tooltip("Don't spawn farther than this")]
    [Range(20f, 100f)]
    public float dontSpawnFartherThan = 50f;
    
    [Space(10)]
    [Header("Debug")]
    public bool showLogs = true;
    public Color gizmoColor = Color.cyan;

    public enum SpawnType
    {
        Continuous, 
        Cycles, 
        OneTime
    }
    
    public enum EnemyType
    {
        LandFisherman,
        BoatFisherman,
        Boat
    }

    public float GetSpawnInterval()
    {
        if (spawnEveryXSeconds <= 0.1f)
        {
            return Mathf.Max(0f, spawnEveryXSeconds);
        }

        float randomVariation = Mathf.Min(spawnEveryXSeconds * 0.2f, 0.5f);
        return spawnEveryXSeconds + Random.Range(-randomVariation, randomVariation);
    }

    
    public bool IsUnlocked(int playerLevel)
    {
        return !needsUnlock || playerLevel >= playerLevelNeeded;
    }
    
    public bool IsValidDistance(Vector3 spawnPos)
    {
        if (Player.Instance == null) return true;
        
        float distance = Vector3.Distance(spawnPos, Player.Instance.transform.position);
        return distance >= dontSpawnCloserThan && distance <= dontSpawnFartherThan;
    }

    [ContextMenu("Setup for Land Fisherman")]
    private void SetupLandFisherman()
    {
        configName = "Land Fisherman Spawner";
        poolName = "LandFisherman"; 
        enemyType = EnemyType.LandFisherman;
        spawnType = SpawnType.Continuous;
        spawnEveryXSeconds = 4f;
        keepActiveAtOnce = 3;
        needsUnlock = false;
        gizmoColor = Color.green;
        Debug.Log("Setup para Land Fisherman - Pool: LandFisherman");
    }
    
    [ContextMenu("Setup for Boat Fisherman")]
    private void SetupBoatFisherman()
    {
        configName = "Boat Fisherman Spawner";
        poolName = "BoatFisherman"; 
        enemyType = EnemyType.BoatFisherman;
        spawnType = SpawnType.OneTime;
        spawnEveryXSeconds = 0.1f;
        spawnThisManyPerCycle = 2;
        waitBetweenCycles = 45f;
        needsUnlock = false;
        gizmoColor = Color.blue;
        Debug.Log("Setup para Boat Fisherman - Pool: BoatFisherman");
    }
    
    [ContextMenu("Setup for Boat")]
    private void SetupBoat()
    {
        configName = "Boat Spawner";
        poolName = "Boat";
        enemyType = EnemyType.Boat;
        spawnType = SpawnType.OneTime;
        spawnEveryXSeconds = 5f;
        keepActiveAtOnce = 2;
        needsUnlock = false;
        dontSpawnCloserThan = 20f;
        dontSpawnFartherThan = 60f;
        showLogs = true;
        gizmoColor = Color.cyan;
        Debug.Log("🚤 BOAT CONFIG: OneTime spawn with initial crew!");
    }
    
    [ContextMenu("Setup for TESTING - Continuous Respawn")]
    private void SetupTestingContinuousRespawn()
    {
        configName = "TESTING - Continuous Respawn";
        poolName = "LandFisherman";
        enemyType = EnemyType.LandFisherman;
        spawnType = SpawnType.Continuous; 
        spawnEveryXSeconds = 8f; 
        keepActiveAtOnce = 1; 
        needsUnlock = false;
        dontSpawnCloserThan = 15f;
        dontSpawnFartherThan = 25f;
        showLogs = true;
        gizmoColor = Color.yellow;
        Debug.Log("⚡ TESTING MODE: Continuous respawn configured - enemy will respawn after being eaten!");
    }
}
