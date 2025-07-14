using System.Collections;
using UnityEngine;

public class ProgressionManager : MonoBehaviour
{
    [Header("Progression Triggers")]
    [SerializeField] private int boatsUnlockPowerLevel = 5;
    [SerializeField] private int boatsUnlockEnemyKills = 10;
    [SerializeField] private int advancedEnemiesUnlockLevel = 15;
    
    [Header("Spawn References")]
    [SerializeField] private SpawnHandler[] landSpawners;
    [SerializeField] private SpawnHandler[] boatSpawners;
    [SerializeField] private SpawnHandler[] advancedSpawners;
    
    [Header("Pool Control")]
    [SerializeField] private bool enableDynamicPooling = true;
    
    private bool boatsUnlocked = false;
    private bool advancedEnemiesUnlocked = false;
    private int currentEnemyKills = 0;
    
    public static ProgressionManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Subscribe to events
        if (PowerLevelScaler.Instance != null)
        {
            // Monitor power level changes
            StartCoroutine(MonitorProgression());
        }
        
        // Initially disable boat spawners
        SetSpawnersActive(boatSpawners, false);
        SetSpawnersActive(advancedSpawners, false);
    }
    
    private IEnumerator MonitorProgression()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f); // Check every second
            
            CheckProgressionTriggers();
        }
    }
    
    private void CheckProgressionTriggers()
    {
        if (!boatsUnlocked)
        {
            CheckBoatUnlock();
        }
        
        if (!advancedEnemiesUnlocked && boatsUnlocked)
        {
            CheckAdvancedEnemiesUnlock();
        }
    }
    
    private void CheckBoatUnlock()
    {
        bool powerCondition = GetCurrentPowerLevel() >= boatsUnlockPowerLevel;
        bool killCondition = currentEnemyKills >= boatsUnlockEnemyKills;
        
        if (powerCondition || killCondition)
        {
            UnlockBoats();
        }
    }
    
    private void CheckAdvancedEnemiesUnlock()
    {
        if (GetCurrentPowerLevel() >= advancedEnemiesUnlockLevel)
        {
            UnlockAdvancedEnemies();
        }
    }
    
    public void UnlockBoats()
    {
        if (boatsUnlocked) return;
        
        boatsUnlocked = true;
        SetSpawnersActive(boatSpawners, true);
        
        // Add boat pool at runtime if needed
        if (enableDynamicPooling)
        {
            AddPoolAtRuntime("Boat", "Assets/Prefabs/BoatFishermanHandler.prefab", 0, 3);
        }
        
        Debug.Log("🚤 BOATS UNLOCKED! Boat enemies can now spawn!");
        
        // Optional: Show UI notification
        // UIManager.Instance?.ShowUnlockNotification("Boats Unlocked!");
    }
    
    public void UnlockAdvancedEnemies()
    {
        if (advancedEnemiesUnlocked) return;
        
        advancedEnemiesUnlocked = true;
        SetSpawnersActive(advancedSpawners, true);
        
        Debug.Log("⚔️ ADVANCED ENEMIES UNLOCKED!");
    }
    
    private void SetSpawnersActive(SpawnHandler[] spawners, bool active)
    {
        if (spawners == null) return;
        
        foreach (var spawner in spawners)
        {
            if (spawner != null)
            {
                spawner.enabled = active;
                spawner.gameObject.SetActive(active);
            }
        }
    }
    
    private void AddPoolAtRuntime(string poolName, string prefabPath, int initialSize, int maxSize)
    {
        // Note: Your current SimpleObjectPool doesn't support runtime pool addition
        // You'd need to extend it or use this method to trigger manual spawning
        Debug.Log($"Would add pool: {poolName} with prefab {prefabPath}");
    }
    
    private int GetCurrentPowerLevel()
    {
        return PowerLevelScaler.Instance?.GetPlayerPowerLevel() ?? 1;
    }
    
    public void OnEnemyKilled()
    {
        currentEnemyKills++;
        Debug.Log($"Enemy killed! Total: {currentEnemyKills}");
    }
    
    // Public methods for manual triggering
    public void TriggerBoatSpawn()
    {
        if (!boatsUnlocked)
        {
            UnlockBoats();
        }
        
        // Manually trigger boat spawning
        foreach (var spawner in boatSpawners)
        {
            if (spawner != null && spawner.enabled)
            {
                spawner.SpawnSingleAtRandomPoint();
            }
        }
    }
    
    public void TriggerConditionalSpawn(string condition)
    {
        switch (condition.ToLower())
        {
            case "boats":
                TriggerBoatSpawn();
                break;
            case "boss":
                // Trigger boss spawn logic
                break;
            case "powerup":
                // Trigger special enemy spawn
                break;
        }
    }
    
    // Getters for UI/other systems
    public bool AreBoatsUnlocked() => boatsUnlocked;
    public bool AreAdvancedEnemiesUnlocked() => advancedEnemiesUnlocked;
    public int GetCurrentKillCount() => currentEnemyKills;
}