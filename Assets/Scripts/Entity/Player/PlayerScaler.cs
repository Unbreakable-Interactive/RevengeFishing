using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScaler : MonoBehaviour
{
    [Header("Scaling Configuration")]
    [SerializeField] private PlayerScalingConfig scalingConfig;

    [Header("Runtime Info")]
    [SerializeField] private Vector3 originalScale;
    [SerializeField] private long currentPowerLevel;
    [SerializeField] private float targetScaleMultiplier = 1f;
    [SerializeField] private float currentScaleMultiplier = 1f;

    private Player player;
    private PlayerStats playerStats;

    private void Awake()
    {
        player = GetComponent<Player>();
        playerStats = GetComponent<PlayerStats>();
        originalScale = transform.localScale;
    }

    private void Start()
    {
        if (scalingConfig == null)
        {
            Debug.LogError("PlayerScaler: No scaling config assigned!");
            return;
        }

        UpdateTargetScale();
        currentScaleMultiplier = targetScaleMultiplier; // Start at correct scale
        ApplyScale();
    }

    private void Update()
    {
        if (player == null || scalingConfig == null) return;

        // Check if power level changed
        if (player.PowerLevel != currentPowerLevel)
        {
            UpdateTargetScale();
        }

        // Apply smooth scaling if enabled
        if (scalingConfig.smoothScaling)
        {
            ApplySmoothScaling();
        }
        else
        {
            currentScaleMultiplier = targetScaleMultiplier;
            ApplyScale();
        }
    }

    private void UpdateTargetScale()
    {
        currentPowerLevel = player.PowerLevel;
        targetScaleMultiplier = scalingConfig.CalculateScaleMultiplier(currentPowerLevel);

        if (!scalingConfig.smoothScaling)
        {
            currentScaleMultiplier = targetScaleMultiplier;
            ApplyScale();
        }
    }

    private void ApplySmoothScaling()
    {
        if (Mathf.Approximately(currentScaleMultiplier, targetScaleMultiplier))
            return;

        currentScaleMultiplier = Mathf.Lerp(
            currentScaleMultiplier,
            targetScaleMultiplier,
            scalingConfig.scalingSpeed * Time.deltaTime
        );

        ApplyScale();

        // Log when scaling is complete
        if (Mathf.Abs(currentScaleMultiplier - targetScaleMultiplier) < 0.01f)
        {
            currentScaleMultiplier = targetScaleMultiplier;
            Debug.Log($"Player scaling complete: {currentScaleMultiplier:F2}x (Power Level: {currentPowerLevel})");
        }
    }

    private void ApplyScale()
    {
        Vector3 newScale = originalScale * currentScaleMultiplier;
        transform.localScale = newScale;
    }

    public void ForceUpdateScale()
    {
        UpdateTargetScale();
        if (!scalingConfig.smoothScaling)
        {
            ApplyScale();
        }
    }

    public float GetCurrentScaleMultiplier() => currentScaleMultiplier;
    public float GetTargetScaleMultiplier() => targetScaleMultiplier;
    public Vector3 GetOriginalScale() => originalScale;
}
