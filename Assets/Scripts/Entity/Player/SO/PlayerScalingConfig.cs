using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerScalingConfig", menuName = "RevengeFishing2D/Player Scaling Config")]
public class PlayerScalingConfig : ScriptableObject
{
    [Header("Power Level Range")]
    [Tooltip("Power level at which player is at minimum scale")]
    public long minPowerLevel = 100;

    [Tooltip("Power level at which player reaches maximum scale")]
    public long maxPowerLevel = 1000000;

    [Header("Scale Multipliers")]
    [Tooltip("Scale multiplier at minimum power level (1.0 = normal size)")]
    public float minScaleMultiplier = 1.0f;

    [Tooltip("Scale multiplier at maximum power level (3.0 = triple size)")]
    public float maxScaleMultiplier = 3.0f;

    [Header("Scaling Curve (Optional)")]
    [Tooltip("Optional curve to control scaling progression. If not set, uses linear interpolation.")]
    public AnimationCurve scalingCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Advanced Settings")]
    [Tooltip("Should scaling be applied smoothly over time?")]
    public bool smoothScaling = true;

    [Tooltip("Speed of smooth scaling transitions (if enabled)")]
    public float scalingSpeed = 2f;

    // Helper method to calculate scale multiplier with curve support
    public float CalculateScaleMultiplier(long powerLevel)
    {
        // Clamp power level to configured range
        long clampedPowerLevel = (long)Mathf.Clamp(powerLevel, minPowerLevel, maxPowerLevel);

        // Calculate progress from min to max (0.0 to 1.0)
        float progress = (float)(clampedPowerLevel - minPowerLevel) /
                        (float)(maxPowerLevel - minPowerLevel);

        // Apply curve if available
        if (scalingCurve != null && scalingCurve.keys.Length > 0)
        {
            progress = scalingCurve.Evaluate(progress);
        }

        // Interpolate between min and max scale multipliers
        return Mathf.Lerp(minScaleMultiplier, maxScaleMultiplier, progress);
    }
}
