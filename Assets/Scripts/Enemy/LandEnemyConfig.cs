using UnityEngine;

[System.Serializable]
public class LandEnemyConfig
{
    [Header("Movement Probabilities")]
    [Range(0f, 1f)] public float idleProbability = 0.6f;
    [Range(0f, 1f)] public float walkProbability = 0.3f;
    [Range(0f, 1f)] public float runProbability = 0.1f;
}
