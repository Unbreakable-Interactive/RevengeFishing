using UnityEngine;

[CreateAssetMenu(fileName = "FishermanConfig", menuName = "RevengeFishing2D/Enemy/Fisherman Config")]
public class FishermanConfig : ScriptableObject
{
    [Header("Fishing Behavior")]
    [Range(0f, 1f)] public float equipToolChance = 0.7f;
    [Range(0f, 1f)] public float unequipToolChance = 0.3f;
    [Range(0f, 1f)] public float hookThrowChance = 0.5f;

    [Header("Timing")]
    public float minHookWaitTime = 10f;
    public float maxHookWaitTime = 20f;
    public float fishingBehaviorCheckInterval = 10f;
    public float minActionTime = 1f;
    public float maxActionTime = 4f;
}

