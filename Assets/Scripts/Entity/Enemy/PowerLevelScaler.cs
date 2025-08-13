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

    [HideInInspector] public float cumulativeWeight;
}

public class PowerLevelScaler : MonoBehaviour
{
    [Header("Power Level Distribution")]
    [SerializeField] private EnemyPowerTier[] powerTiers;

    [Header("Optimization")]
    [SerializeField] private bool cachePlayerPowerLevel = true;
    [SerializeField] private float powerLevelCacheTime = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    private int cachedPlayerPowerLevel = -1;
    private float lastPowerLevelCacheTime = 0f;
    private float totalWeight = 0f;

    public static event Action<int> OnPlayerPowerChanged;

    public static PowerLevelScaler Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            GameLogger.LogWarning("Multiple PowerLevelScaler instances found! Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ValidateAndCachePowerTiers();

        if (Player.Instance != null)
        {
            UpdateCachedPowerLevel(true);
        }

        GameLogger.Log("PowerLevelScaler initialized successfully");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public int GetPlayerPowerLevel()
    {
        if (!cachePlayerPowerLevel)
        {
            return Player.Instance?.PowerLevel ?? 1000;
        }

        if (Time.time - lastPowerLevelCacheTime > powerLevelCacheTime)
        {
            UpdateCachedPowerLevel();
        }

        return cachedPlayerPowerLevel > 0 ? cachedPlayerPowerLevel : 1000;
    }

    private void UpdateCachedPowerLevel(bool forceUpdate = false)
    {
        if (Player.Instance == null) return;

        int newPowerLevel = Player.Instance.PowerLevel;

        if (forceUpdate || newPowerLevel != cachedPlayerPowerLevel)
        {
            int oldPowerLevel = cachedPlayerPowerLevel;
            cachedPlayerPowerLevel = newPowerLevel;
            lastPowerLevelCacheTime = Time.time;

            if (oldPowerLevel != newPowerLevel && oldPowerLevel > 0)
            {
                OnPlayerPowerChanged?.Invoke(newPowerLevel);
                GameLogger.LogVerbose($"Player power level updated: {oldPowerLevel} -> {newPowerLevel}");
            }
        }
    }

    public int CalculateEnemyPowerLevel()
    {
        if (Player.Instance == null)
        {
            GameLogger.LogError("PowerLevelScaler: No Player reference!");
            return 100;
        }

        int playerPowerLevel = GetPlayerPowerLevel();

        if (playerPowerLevel <= 0)
        {
            GameLogger.LogVerbose("Player power level is 0 or negative, using fallback");
            return 100;
        }

        EnemyPowerTier selectedTier = SelectRandomTierOptimized();

        float minPower = playerPowerLevel * selectedTier.minPowerMultiplier;
        float maxPower = playerPowerLevel * selectedTier.maxPowerMultiplier;

        int enemyPowerLevel = Mathf.RoundToInt(UnityEngine.Random.Range(minPower, maxPower));
        enemyPowerLevel = Mathf.Max(1, enemyPowerLevel);

        GameLogger.LogVerbose($"Player: {playerPowerLevel} | Tier: {selectedTier.tierName} | Enemy: {enemyPowerLevel}");

        return enemyPowerLevel;
    }

    private EnemyPowerTier SelectRandomTierOptimized()
    {
        if (totalWeight <= 0f)
        {
            GameLogger.LogError("Total weight is 0! Check power tier configuration.");
            return powerTiers[0];
        }

        float randomValue = UnityEngine.Random.Range(0f, totalWeight);

        for (int i = 0; i < powerTiers.Length; i++)
        {
            if (randomValue <= powerTiers[i].cumulativeWeight)
            {
                return powerTiers[i];
            }
        }

        return powerTiers[powerTiers.Length - 1];
    }

    private void ValidateAndCachePowerTiers()
    {
        if (powerTiers == null || powerTiers.Length == 0)
        {
            GameLogger.LogError("No power tiers configured!");
            return;
        }

        totalWeight = 0f;
        float runningTotal = 0f;

        for (int i = 0; i < powerTiers.Length; i++)
        {
            runningTotal += powerTiers[i].spawnPercentage;
            powerTiers[i].cumulativeWeight = runningTotal;
            totalWeight += powerTiers[i].spawnPercentage;
        }

        if (Mathf.Abs(totalWeight - 100f) > 0.1f)
        {
            GameLogger.LogWarning($"Power tier percentages don't add up to 100%! Current total: {totalWeight}%");
        }

        GameLogger.LogVerbose($"Validated {powerTiers.Length} power tiers (Total: {totalWeight}%)");
    }

    public void RefreshPlayerPowerLevel()
    {
        UpdateCachedPowerLevel(true);
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
            GameLogger.LogVerbose($"[PowerLevelScaler] {message}");
    }

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

        GameLogger.Log($"=== POWER DISTRIBUTION STATS (Sample: {sampleSize}) ===");
        for (int i = 0; i < powerTiers.Length; i++)
        {
            float actualPercentage = (tierCounts[i] / (float)sampleSize) * 100f;
            GameLogger.Log($"{powerTiers[i].tierName}: Expected {powerTiers[i].spawnPercentage}% | Actual {actualPercentage:F1}%");
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
            ValidateAndCachePowerTiers();
    }
}
