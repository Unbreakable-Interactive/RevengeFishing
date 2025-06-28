using UnityEngine;

[System.Serializable]
public class LandEnemyConfig
{
    [Header("Movement Probabilities")]
    [Range(0f, 1f)] public float idleProbability;
    [Range(0f, 1f)] public float walkProbability;
    [Range(0f, 1f)] public float runProbability;
}