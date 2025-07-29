using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "RevengeFishing2D/Player Config")]
public class PlayerConfig : ScriptableObject
{
    [Header("Power System")]
    public int startingPowerLevel = 100;
    public PhaseThresholds phaseThresholds;
    
    [Header("Survival Stats")]
    public float maxHunger = 100f;
    public float maxFatigue = 100f;
    public float hungerDecayRate = 1f;
    public float fatigueRecoveryRate = 0.5f;
}

[System.Serializable]
public class PhaseThresholds
{
    public int juvenile = 101;
    public int adult = 1001;
    public int beast = 10001;
    public int monster = 100001;
}
