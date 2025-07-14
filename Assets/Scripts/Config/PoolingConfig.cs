using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Pooling Config", menuName = "Spawning System/Pooling Config")]
public class PoolingConfig : ScriptableObject
{
    [Header("Pool Configuration")]
    [Tooltip("Display name for this pooling configuration. Used for organization and debugging.")]
    public string configName = "Default Pool Config";
    
    [Tooltip("List of all object pools. Each pool handles one type of object (e.g., Fishermen, Boats, etc.)")]
    public List<PoolConfigData> poolConfigs = new List<PoolConfigData>();
    
    [Header("Global Settings")]
    [Tooltip("Show detailed pool operations in Console. Turn off for production to improve performance.")]
    public bool enableDebugLogs = true;
    
    [Tooltip("Maximum total objects across ALL pools combined. Prevents memory issues from too many pooled objects.")]
    public int globalMaxPoolSize = 50;
    
    [System.Serializable]
    public class PoolConfigData
    {
        [Tooltip("Unique name for this pool. SpawnHandlers must use this exact name in their 'Pool Name' field.")]
        public string poolName;
        
        [Tooltip("The GameObject prefab to spawn from this pool. Drag your Fisherman/Enemy prefab here.")]
        public GameObject prefab;
        
        [Tooltip("How many objects to create immediately when scene starts. Higher = less lag during gameplay.")]
        public int initialSize = 5;
        
        [Tooltip("Maximum objects this pool can ever have. Prevents infinite spawning if something goes wrong.")]
        public int maxSize = 20;
        
        [Tooltip("Allow creating new objects if pool is empty. If false, stops spawning when pool depleted.")]
        public bool allowDynamicExpansion = true;
        
        [Header("Spawn Settings")]
        [Tooltip("Minimum time between spawns from this pool. Prevents spam spawning.")]
        public float spawnCooldown = 0.1f;
        
        [Tooltip("Check player distance before spawning. Prevents spawning too close to player.")]
        public bool requiresPlayerDistance = true;
        
        [Tooltip("Minimum distance from player before allowing spawn from this pool.")]
        public float minPlayerDistance = 10f;
        
        [Header("Pool Behavior")]
        [Tooltip("Automatically clean up unused objects to save memory.")]
        public bool autoCleanup = true;
        
        [Tooltip("How often (in seconds) to check for cleanup opportunities.")]
        public float cleanupInterval = 30f;
        
        [Tooltip("Only cleanup if this many objects are inactive. Prevents constant cleanup of small pools.")]
        public int cleanupThreshold = 10;
    }
    
    public PoolConfigData GetPoolConfig(string poolName)
    {
        return poolConfigs.Find(config => config.poolName == poolName);
    }
    
    public bool HasPool(string poolName)
    {
        return poolConfigs.Exists(config => config.poolName == poolName);
    }
    
    public void AddPoolConfig(PoolConfigData newConfig)
    {
        if (!HasPool(newConfig.poolName))
        {
            poolConfigs.Add(newConfig);
        }
        else
        {
            Debug.LogWarning($"Pool '{newConfig.poolName}' already exists in config '{configName}'");
        }
    }
    
    [ContextMenu("Validate Config")]
    public void ValidateConfig()
    {
        for (int i = 0; i < poolConfigs.Count; i++)
        {
            var config = poolConfigs[i];
            
            if (string.IsNullOrEmpty(config.poolName))
            {
                Debug.LogError($"Pool at index {i} has no name!");
            }
            
            if (config.prefab == null)
            {
                Debug.LogError($"Pool '{config.poolName}' has no prefab assigned!");
            }
            
            if (config.initialSize > config.maxSize)
            {
                Debug.LogWarning($"Pool '{config.poolName}' initial size ({config.initialSize}) is greater than max size ({config.maxSize})");
                config.initialSize = config.maxSize;
            }
        }
    }
}