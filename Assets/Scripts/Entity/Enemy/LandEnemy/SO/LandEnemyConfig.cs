using UnityEngine;

[CreateAssetMenu(fileName = "LandEnemyConfig", menuName = "RevengeFishing2D/Enemy/Land Enemy Config")]
public class LandEnemyConfig : ScriptableObject
{
    [Header("Movement Probabilities")]
    [Range(0f, 1f)] public float idleProbability = 0.4f;
    [Range(0f, 1f)] public float walkProbability = 0.3f;
    [Range(0f, 1f)] public float runProbability = 0.3f;
}