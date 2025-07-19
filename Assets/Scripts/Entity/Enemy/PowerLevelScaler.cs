using UnityEngine;
using System;

[System.Serializable]
public class EnemyPowerTier
{
    [Header("Power Level Range")]
    public float minPowerMultiplier;
    public float maxPowerMultiplier;

    [Header("Spawn Probability")]
    [Range(0f, 100f)]
    public float spawnPercentage;

    [Header("Description")]
    public string tierName;

    // Cached values for optimization
    [HideInInspector] public float cumulativeWeight;
}

public class PowerLevelScaler : MonoBehaviour
{
    [Header("Power Level Distribution")]
    [SerializeField] private EnemyPowerTier[] powerTiers;

    // [Header("References")]
        // [SerializeField] private Player player;

    [Header("Optimization")]
    [SerializeField] private bool cachePlayerPowerLevel = true;
    [SerializeField] private float powerLevelCacheTime = 0.5f; // Update cache every 0.5 seconds

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Cached values
    private int cachedPlayerPowerLevel = -1;
    private float lastPowerLevelCacheTime = 0f;
    private float totalWeight = 0f;

    // Events
    public static event Action<int> OnPlayerPowerChanged;

    // Singleton pattern for easy access
    public static PowerLevelScaler Instance { get; private set; }

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple PowerLevelScaler instances found! Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // if (player == null)
        //     player = FindObjectOfType<Player>();
        // player = Player.instance;
        //
        // if (player == null)
        // {
        //     Debug.LogError("PowerLevelScaler: Could not find Player in scene!");
        //     return;
        // }

        ValidateAndCachePowerTiers();

        // Subscribe to player power changes if available
        if (Player.Instance != null)
        {
            // Cache initial power level
            UpdateCachedPowerLevel(true);
        }

        LogDebug("PowerLevelScaler initialized successfully");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Get player power level with caching optimization
    /// </summary>
    public int GetPlayerPowerLevel()
    {
        if (!cachePlayerPowerLevel)
        {
            return Player.Instance?.PowerLevel ?? 1000;
        }

        // Update cache if enough time has passed
        if (Time.time - lastPowerLevelCacheTime > powerLevelCacheTime)
        {
            UpdateCachedPowerLevel();
        }

        return cachedPlayerPowerLevel > 0 ? cachedPlayerPowerLevel : 1000;
    }

    /// <summary>
    /// Update cached player power level
    /// </summary>
    private void UpdateCachedPowerLevel(bool forceUpdate = false)
    {
        if (Player.Instance == null) return;

        int newPowerLevel = Player.Instance.PowerLevel;

        if (forceUpdate || newPowerLevel != cachedPlayerPowerLevel)
        {
            int oldPowerLevel = cachedPlayerPowerLevel;
            cachedPlayerPowerLevel = newPowerLevel;
            lastPowerLevelCacheTime = Time.time;

            // Trigger event if power level changed
            if (oldPowerLevel != newPowerLevel && oldPowerLevel > 0)
            {
                OnPlayerPowerChanged?.Invoke(newPowerLevel);
                LogDebug($"Player power level updated: {oldPowerLevel} -> {newPowerLevel}");
            }
        }
    }

    /// <summary>
    /// Calculate enemy power level (optimized version)
    /// </summary>
    public int CalculateEnemyPowerLevel()
    {
        if (Player.Instance == null)
        {
            Debug.LogError("PowerLevelScaler: No Player reference!");
            return 100;
        }

        int playerPowerLevel = GetPlayerPowerLevel();

        if (playerPowerLevel <= 0)
        {
            LogDebug("Player power level is 0 or negative, using fallback");
            return 100;
        }

        EnemyPowerTier selectedTier = SelectRandomTierOptimized();

        // Calculate power level within the selected tier's range
        float minPower = playerPowerLevel * selectedTier.minPowerMultiplier;
        float maxPower = playerPowerLevel * selectedTier.maxPowerMultiplier;

        int enemyPowerLevel = Mathf.RoundToInt(UnityEngine.Random.Range(minPower, maxPower));
        enemyPowerLevel = Mathf.Max(1, enemyPowerLevel);

        LogDebug($"Player: {playerPowerLevel} | Tier: {selectedTier.tierName} | Enemy: {enemyPowerLevel}");

        return enemyPowerLevel;
    }

    /// <summary>
    /// Optimized tier selection using pre-calculated cumulative weights
    /// </summary>
    private EnemyPowerTier SelectRandomTierOptimized()
    {
        if (totalWeight <= 0f)
        {
            Debug.LogError("Total weight is 0! Check power tier configuration.");
            return powerTiers[0];
        }

        float randomValue = UnityEngine.Random.Range(0f, totalWeight);

        // Use binary search for better performance with many tiers
        for (int i = 0; i < powerTiers.Length; i++)
        {
            if (randomValue <= powerTiers[i].cumulativeWeight)
            {
                return powerTiers[i];
            }
        }

        return powerTiers[powerTiers.Length - 1];
    }

    /// <summary>
    /// Validate tiers and pre-calculate cumulative weights for optimization
    /// </summary>
    private void ValidateAndCachePowerTiers()
    {
        if (powerTiers == null || powerTiers.Length == 0)
        {
            Debug.LogError("No power tiers configured!");
            return;
        }

        totalWeight = 0f;
        float runningTotal = 0f;

        // Calculate cumulative weights for optimized selection
        for (int i = 0; i < powerTiers.Length; i++)
        {
            runningTotal += powerTiers[i].spawnPercentage;
            powerTiers[i].cumulativeWeight = runningTotal;
            totalWeight += powerTiers[i].spawnPercentage;
        }

        // Validate total percentage
        if (Mathf.Abs(totalWeight - 100f) > 0.1f)
        {
            Debug.LogWarning($"Power tier percentages don't add up to 100%! Current total: {totalWeight}%");
        }

        LogDebug($"Validated {powerTiers.Length} power tiers (Total: {totalWeight}%)");
    }

    /// <summary>
    /// Force update cached power level (call when player gains power)
    /// </summary>
    public void RefreshPlayerPowerLevel()
    {
        UpdateCachedPowerLevel(true);
    }

    /// <summary>
    /// Conditional debug logging
    /// </summary>
    private void LogDebug(string message)
    {
#if UNITY_EDITOR
        if (enableDebugLogs)
            Debug.Log($"[PowerLevelScaler] {message}");
#endif
    }

    /// <summary>
    /// Get distribution statistics for debugging
    /// </summary>
    [ContextMenu("Test Distribution")]
    public void LogDistributionStats(int sampleSize = 1000)
    {
        if (!enableDebugLogs) return;

        int[] tierCounts = new int[powerTiers.Length];

        for (int i = 0; i < sampleSize; i++)
        {
            EnemyPowerTier selectedTier = SelectRandomTierOptimized();
            for (int j = 0; j < powerTiers.Length; j++)
            {
                if (powerTiers[j] == selectedTier)
                {
                    tierCounts[j]++;
                    break;
                }
            }
        }

        Debug.Log($"=== POWER DISTRIBUTION STATS (Sample: {sampleSize}) ===");
        for (int i = 0; i < powerTiers.Length; i++)
        {
            float actualPercentage = (tierCounts[i] / (float)sampleSize) * 100f;
            Debug.Log($"{powerTiers[i].tierName}: Expected {powerTiers[i].spawnPercentage}% | Actual {actualPercentage:F1}%");
        }
    }

    // Editor validation
    private void OnValidate()
    {
        if (Application.isPlaying)
            ValidateAndCachePowerTiers();
    }
}
