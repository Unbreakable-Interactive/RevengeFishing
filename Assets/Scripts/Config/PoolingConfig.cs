using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Pooling Config", menuName = "Fishing Game/Pooling Config")]
public class PoolingConfig : ScriptableObject
{
    [Header("Pool Configuration")]
    public string configName = "Default Pools";
    
    [Space(10)]
    [Header("Enemy Pools - How many objects to create at game start")]
    public List<PoolData> poolConfigs = new List<PoolData>();
    
    [Space(10)]
    [Header("Debug")]
    public bool enableDebugLogs = true;

    [System.Serializable]
    public class PoolData
    {
        [Header("Pool Settings")]
        public string poolName = "LandFisherman";
        public GameObject prefab;
        
        [Space(5)]
        [Header("Pool Sizes")]
        [Tooltip("How many objects to create when the game starts")]
        public int initialSize = 5;
        
        [Tooltip("Maximum objects this pool can have (when it needs to create more)")]
        public int maxSize = 15;
    }

    // Quick setup buttons
    [ContextMenu("Setup Default Pools")]
    private void SetupDefaultPools()
    {
        poolConfigs.Clear();
        
        // Add Land Fisherman pool
        PoolData landFishermanPool = new PoolData();
        landFishermanPool.poolName = "LandFisherman";
        landFishermanPool.initialSize = 6;
        landFishermanPool.maxSize = 20;
        poolConfigs.Add(landFishermanPool);
        
        // Add Boat Fisherman pool
        PoolData boatFishermanPool = new PoolData();
        boatFishermanPool.poolName = "BoatFisherman";
        boatFishermanPool.initialSize = 4;
        boatFishermanPool.maxSize = 12;
        poolConfigs.Add(boatFishermanPool);
        
        // Add Boat pool  
        PoolData boatPool = new PoolData();
        boatPool.poolName = "Boat";
        boatPool.initialSize = 3;
        boatPool.maxSize = 8;
        poolConfigs.Add(boatPool);
        
        Debug.Log("Setup 3 pools: LandFisherman, BoatFisherman, Boat");
    }
    
    [ContextMenu("Clear All Pools")]
    private void ClearAllPools()
    {
        poolConfigs.Clear();
        Debug.Log("All pools cleared. Use 'Setup Default Pools' to recreate them.");
    }
}
