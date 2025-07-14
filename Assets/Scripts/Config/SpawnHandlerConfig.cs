using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New SpawnHandler Config", menuName = "Spawning System/SpawnHandler Config")]
public class SpawnHandlerConfig : ScriptableObject
{
    [Header("Basic Configuration")]
    [Tooltip("Display name for this spawner configuration. Used for organization and debugging in the Inspector.")]
    public string configName = "Default Spawn Config";
    
    [Tooltip("HOW the spawning works:\n• Continuous: Normal behavior - keeps spawning enemies every X seconds (your current fisherman spawners)\n• Limited: Spawns only X total enemies then stops forever (NEW - perfect for boats)\n• OneTime: Spawns once when activated, then never again\n• PlayerTriggered: Only spawns when player gets close")]
    public SpawnType spawnType = SpawnType.Continuous;
    
    [Header("Spawning Settings")]
    [Tooltip("Which pool to use from SimpleObjectPool. Must match exactly the pool name in your PoolingConfig (e.g., 'Fisherman').")]
    public string poolName = "Fisherman";
    
    [Tooltip("Time in seconds between each spawn attempt. Lower = more frequent spawning. Your old spawnInterval setting.")]
    public float spawnInterval = 5f;
    
    [Tooltip("Maximum number of active enemies at the same time. When limit reached, stops spawning until some die. Your old maxEnemies setting.")]
    public int maxEnemies = 5;
    
    [Tooltip("Minimum distance from player before spawning. Prevents enemies spawning too close to player. Your old minPlayerDistance setting.")]
    public float minPlayerDistance = 10f;
    
    [Header("Auto Spawning")]
    [Tooltip("Enable automatic spawning system. If disabled, only manual spawning (F/G keys) will work. Your old enableAutoSpawning setting.")]
    public bool enableAutoSpawning = true;
    
    [Tooltip("Start spawning immediately when scene loads. If disabled, spawning must be activated manually.")]
    public bool enableOnStart = true;
    
    [Header("Limited Spawning (for Boats)")]
    [Tooltip("Total number of enemies this spawner can create:\n• -1 = Unlimited (normal continuous spawning)\n• 1-2 = Only spawn this many enemies total, then stop forever (perfect for boats)\n• 0 = Never spawn")]
    public int maxSpawns = -1;
    
    [Tooltip("Wait this many seconds after spawner activates before first spawn. Useful for boats that need setup time.")]
    public float initialDelay = 0f;
    
    [Tooltip("Start spawning immediately when GameObject becomes active. Perfect for boats that should spawn fishermen right away.")]
    public bool spawnOnAwake = false;
    
    [Header("Player Detection")]
    [Tooltip("Only spawn when player is within detection range. Saves performance by not spawning far away enemies.")]
    public bool requirePlayerProximity = false;
    
    [Tooltip("How close player must be to trigger spawning (when using PlayerTriggered spawn type or requirePlayerProximity).")]
    public float playerDetectionRange = 15f;
    
    [Tooltip("Hide spawned enemies when player moves far away. Helps with performance optimization.")]
    public bool hideWhenPlayerFar = false;
    
    [Header("Power Level")]
    [Tooltip("Use power level scaling system for spawned enemies. Enemies will get stronger as player progresses.")]
    public bool usePowerLevelScaling = true;
    
    [Tooltip("Starting power level for enemies when player is at beginning of game.")]
    public int basePowerLevel = 1;
    
    [Tooltip("Maximum power level enemies can reach. Higher = stronger enemies later in game.")]
    public int maxPowerLevel = 10;
    
    [Header("Debug Settings")]
    [Tooltip("Show detailed spawn messages in Console. Turn off for production to improve performance.")]
    public bool enableSpawnLogs = true;
    
    [Tooltip("Draw spawn point gizmos in Scene view for easier setup and debugging.")]
    public bool showGizmos = true;
    
    [Tooltip("Color of gizmos drawn in Scene view for this spawner's spawn points.")]
    public Color gizmoColor = Color.cyan;
    
    public enum SpawnType
    {
        Continuous,     // Normal continuous spawning
        Limited,        // Spawn only X times (for boats)
        OneTime,        // Spawn once and stop
        PlayerTriggered // Spawn when player gets close
    }
    
    [ContextMenu("Create Default Fisherman Config")]
    public void CreateFishermanConfig()
    {
        configName = "Fisherman Spawner";
        spawnType = SpawnType.Continuous;
        poolName = "Fisherman";
        spawnInterval = 5f;
        maxEnemies = 5;
        enableAutoSpawning = true;
        maxSpawns = -1;
    }
    
    [ContextMenu("Create Boat Config")]
    public void CreateBoatConfig()
    {
        configName = "Boat Fisherman Spawner";
        spawnType = SpawnType.Limited;
        poolName = "Fisherman";
        spawnInterval = 2f;
        maxEnemies = 2;
        enableAutoSpawning = true;
        maxSpawns = 2; // Only spawn 1 or 2 fishermans
        initialDelay = 1f;
        spawnOnAwake = true;
    }
}