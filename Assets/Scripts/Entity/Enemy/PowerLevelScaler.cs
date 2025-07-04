using UnityEngine;

[System.Serializable]
public class EnemyPowerTier
{
    [Header("Power Level Range")]
    public float minPowerMultiplier;  // Multiplier of player power (e.g., 0.9 for 90%)
    public float maxPowerMultiplier;  // Multiplier of player power (e.g., 1.1 for 110%)

    [Header("Spawn Probability")]
    [Range(0f, 100f)]
    public float spawnPercentage;     // Percentage chance this tier spawns

    [Header("Description")]
    public string tierName;           // For debugging/editor display
}

public class PowerLevelScaler : MonoBehaviour
{
    [Header("Power Level Distribution")]
    [SerializeField]
    private EnemyPowerTier[] powerTiers = new EnemyPowerTier[]
    {
        new EnemyPowerTier { tierName = "Around Player Level", minPowerMultiplier = 0.9f, maxPowerMultiplier = 1.1f, spawnPercentage = 50f },
        new EnemyPowerTier { tierName = "Moderately Below", minPowerMultiplier = 0.7f, maxPowerMultiplier = 0.899f, spawnPercentage = 14f },
        new EnemyPowerTier { tierName = "Moderately Above", minPowerMultiplier = 1.101f, maxPowerMultiplier = 1.3f, spawnPercentage = 14f },
        new EnemyPowerTier { tierName = "Significantly Below", minPowerMultiplier = 0.5f, maxPowerMultiplier = 0.699f, spawnPercentage = 8f },
        new EnemyPowerTier { tierName = "Significantly Above", minPowerMultiplier = 1.301f, maxPowerMultiplier = 1.5f, spawnPercentage = 8f },
        new EnemyPowerTier { tierName = "Very High Above", minPowerMultiplier = 1.501f, maxPowerMultiplier = 2.0f, spawnPercentage = 6f }
    };

    [Header("References")]
    [SerializeField] private Player player;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    private void Start()
    {
        // Don't get player power level here - get it dynamically when needed
        if (player == null)
            player = FindObjectOfType<Player>();

        if (player == null)
            Debug.LogError("PowerLevelScaler: Could not find Player in scene!");
        else
            Debug.Log("PowerLevelScaler: Found player reference (will get power level dynamically)");

        ValidatePowerTiers();
    }

    /// <summary>
    /// Calculate enemy power level based on player's current power level and weighted distribution
    /// </summary>
    public int CalculateEnemyPowerLevel()
    {
        // Always try to find player if reference is missing
        if (player == null)
        {
            player = FindObjectOfType<Player>();

            if (player != null && enableDebugLogs)
                Debug.Log("PowerLevelScaler: Found player reference during runtime");
        }

        if (player == null)
        {
            Debug.LogError("PowerLevelScaler: No Player found in scene!");
            return 1000; // Fallback
        }

        // Get player power level DYNAMICALLY each time
        int playerPowerLevel = player.PowerLevel;

        // Handle case where player hasn't been initialized yet
        if (playerPowerLevel <= 0)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"PowerLevelScaler: Player power level is {playerPowerLevel}, player may not be initialized yet. Using fallback of 1000.");
            return 1000; // Fallback when player isn't ready
        }

        if (enableDebugLogs)
            Debug.Log($"PowerLevelScaler: Using player power level: {playerPowerLevel}");

        EnemyPowerTier selectedTier = SelectRandomTier();

        // Calculate power level within the selected tier's range
        float minPower = playerPowerLevel * selectedTier.minPowerMultiplier;
        float maxPower = playerPowerLevel * selectedTier.maxPowerMultiplier;

        int enemyPowerLevel = Mathf.RoundToInt(Random.Range(minPower, maxPower));

        // Ensure minimum power level of 1
        enemyPowerLevel = Mathf.Max(1, enemyPowerLevel);

        if (enableDebugLogs)
        {
            Debug.Log($"Player Power: {playerPowerLevel} | Selected Tier: {selectedTier.tierName} | Enemy Power: {enemyPowerLevel}");
        }

        return enemyPowerLevel;
    }

    /// <summary>
    /// Select a random tier based on weighted probabilities
    /// </summary>
    private EnemyPowerTier SelectRandomTier()
    {
        float totalWeight = 0f;

        // Calculate total weight
        foreach (var tier in powerTiers)
        {
            totalWeight += tier.spawnPercentage;
        }

        // Generate random value
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        // Select tier based on weight
        foreach (var tier in powerTiers)
        {
            currentWeight += tier.spawnPercentage;
            if (randomValue <= currentWeight)
            {
                return tier;
            }
        }

        // Fallback to first tier
        return powerTiers[0];
    }

    /// <summary>
    /// Validate that tier percentages add up to 100%
    /// </summary>
    private void ValidatePowerTiers()
    {
        float totalPercentage = 0f;
        foreach (var tier in powerTiers)
        {
            totalPercentage += tier.spawnPercentage;
        }

        if (Mathf.Abs(totalPercentage - 100f) > 0.1f)
        {
            Debug.LogWarning($"Power tier percentages don't add up to 100%! Current total: {totalPercentage}%");
        }

        if (enableDebugLogs)
        {
            Debug.Log($"Power Level Scaler initialized with {powerTiers.Length} tiers totaling {totalPercentage}%");
        }
    }

    /// <summary>
    /// Get distribution statistics for debugging
    /// </summary>
    public void LogDistributionStats(int sampleSize = 1000)
    {
        if (!enableDebugLogs) return;

        int[] tierCounts = new int[powerTiers.Length];

        // Generate sample
        for (int i = 0; i < sampleSize; i++)
        {
            EnemyPowerTier selectedTier = SelectRandomTier();
            for (int j = 0; j < powerTiers.Length; j++)
            {
                if (powerTiers[j] == selectedTier)
                {
                    tierCounts[j]++;
                    break;
                }
            }
        }

        // Log results
        Debug.Log($"=== POWER DISTRIBUTION STATS (Sample Size: {sampleSize}) ===");
        for (int i = 0; i < powerTiers.Length; i++)
        {
            float actualPercentage = (tierCounts[i] / (float)sampleSize) * 100f;
            Debug.Log($"{powerTiers[i].tierName}: Expected {powerTiers[i].spawnPercentage}% | Actual {actualPercentage:F1}% | Count: {tierCounts[i]}");
        }
    }
}
