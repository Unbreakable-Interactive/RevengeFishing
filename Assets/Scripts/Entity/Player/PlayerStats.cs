using UnityEngine;

public enum Phase { infant, juvenile, adult, beast, monster }

public class PlayerStats : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PlayerConfig _config;
    
    // Runtime values
    [SerializeField] private long _powerLevel;
    [SerializeField] private long _hunger;
    [SerializeField] private long _fatigue;
    [SerializeField] private Phase _phase;
    
    public void Initialize()
    {
        _powerLevel = _config.startingPowerLevel;
        SetPhase();
    }
    
    private void SetPhase()
    {
        var thresholds = _config.phaseThresholds;
        if (_powerLevel >= thresholds.monster) _phase = Phase.monster;
        else if (_powerLevel >= thresholds.beast) _phase = Phase.beast;
        else if (_powerLevel >= thresholds.adult) _phase = Phase.adult;
        else if (_powerLevel >= thresholds.juvenile) _phase = Phase.juvenile;
        else _phase = Phase.infant;
    }
}
