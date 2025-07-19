using UnityEngine;

[CreateAssetMenu(fileName = "New Spawn Config", menuName = "Fishing Game/Spawn Config")]
public class SpawnHandlerConfig : ScriptableObject
{
    [Header("What does this spawner do?")]
    public string configName = "Land Fisherman Spawner";
    
    [Space(10)]
    [Header("Basic Settings")]
    [Tooltip("What pool to use (must match a pool name in PoolingConfig)")]
    public string poolName = "LandFisherman";
    
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
    public bool showGizmos = true;
    public Color gizmoColor = Color.cyan;

    public enum SpawnType
    {
        Continuous,    // Keep spawning to maintain count
        Cycles,        // Spawn in cycles with breaks (good for boats)
        OneTime        // Spawn once and stop
    }
    
    public enum EnemyType
    {
        LandFisherman,
        BoatFisherman,
        Boat
    }

    /// <summary>
    /// FIXED: GetSpawnInterval que respeta valores pequeños y no añade delay aleatorio innecesario
    /// </summary>
    public float GetSpawnInterval()
    {
        // FIXED: Para valores muy pequeños (spawn inmediato), no añadir randomness
        if (spawnEveryXSeconds <= 0.1f)
        {
            return Mathf.Max(0f, spawnEveryXSeconds); // Ensure no negative values
        }
    
        // FIXED: Para valores normales, añadir menos randomness
        float randomVariation = Mathf.Min(spawnEveryXSeconds * 0.2f, 0.5f); // Max 20% variation or 0.5s
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

    // Quick setup buttons - CORREGIDOS CON NOMBRES DE POOLS CORRECTOS
    [ContextMenu("Setup for Land Fisherman")]
    private void SetupLandFisherman()
    {
        configName = "Land Fisherman Spawner";
        poolName = "LandFisherman";  // CORREGIDO: era "Fisherman"
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
        poolName = "BoatFisherman";  // CORREGIDO: era "Fisherman"
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
        spawnType = SpawnType.OneTime;     // FIXED: Solo aparece una vez
        spawnEveryXSeconds = 5f;           // Delay inicial
        keepActiveAtOnce = 2;              // Máximo 2 botes activos
        needsUnlock = false;               // Sin unlock para testing
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
        spawnType = SpawnType.Continuous;    // FIXED: Cambiado de OneTime a Continuous
        spawnEveryXSeconds = 8f;             // FIXED: Respawn cada 8 segundos después de morir
        keepActiveAtOnce = 1;                // Solo 1 enemigo activo a la vez
        needsUnlock = false;
        dontSpawnCloserThan = 15f;
        dontSpawnFartherThan = 25f;
        showLogs = true;
        gizmoColor = Color.yellow;
        Debug.Log("⚡ TESTING MODE: Continuous respawn configured - enemy will respawn after being eaten!");
    }
}
