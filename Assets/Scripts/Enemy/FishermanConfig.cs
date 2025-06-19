using UnityEngine;

[System.Serializable]
public class FishermanConfig
{
    [Header("Movement Probabilities")]
    [Range(0f, 1f)] public float idleProbability = 0.7f;
    [Range(0f, 1f)] public float walkProbability = 0.25f;
    [Range(0f, 1f)] public float runProbability = 0.05f;

    [Header("Fishing Behavior")]
    [Range(0f, 1f)] public float equipToolChance = 0.7f;
    [Range(0f, 1f)] public float unequipToolChance = 0.2f;
    [Range(0f, 1f)] public float hookThrowChance = 0.4f;

    [Header("Timing")]
    public float minHookWaitTime = 10f;
    public float maxHookWaitTime = 20f;
    public float fishingBehaviorCheckInterval = 10f;
    public float minActionTime = 1f;
    public float maxActionTime = 4f;
}
