using UnityEngine;

[System.Serializable]
public class FishermanConfig
{
    [Header("Fishing Behavior")]
    [Range(0f, 1f)] public float equipToolChance;
    [Range(0f, 1f)] public float unequipToolChance;
    [Range(0f, 1f)] public float hookThrowChance;

    [Header("Timing")]
    public float minHookWaitTime = 10f;
    public float maxHookWaitTime = 20f;
    public float fishingBehaviorCheckInterval = 10f;
    public float minActionTime = 1f;
    public float maxActionTime = 4f;
}
