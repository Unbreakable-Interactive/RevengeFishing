using UnityEngine;
using UnityEngine.SceneManagement;


public enum Phase { Infant, Juvenile, Adult, Beast, Monster }

public class PlayerPhases : MonoBehaviour
{
    [Header("Phase State")]
    [SerializeField] private Phase currentPhase = Phase.Infant;
    [SerializeField] private int nextPowerLevel;

    private PlayerConfig playerConfig;
    private Animator animator;

    public System.Action<Phase, Phase> OnPhaseChanged;
    public System.Action OnVictory;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    public void Initialize(int currentPowerLevel, PlayerConfig config)
    {
        playerConfig = config;
        animator = GetComponent<Animator>();
        
        currentPhase = Phase.Infant;
        nextPowerLevel = playerConfig?.phaseThresholds.juvenile ?? 1000;
        
        SetAnimatorPhase(currentPhase);
        DebugLog($"Initialized. Phase: {currentPhase}, Next: {nextPowerLevel}");
    }

    public void CheckForMaturation(int currentPowerLevel)
    {
        if (currentPowerLevel >= nextPowerLevel)
        {
            Mature();
        }
    }

    private void Mature()
    {
        Phase oldPhase = currentPhase;

        switch (currentPhase)
        {
            case Phase.Infant:
                currentPhase = Phase.Juvenile;
                nextPowerLevel = playerConfig.phaseThresholds.adult;
                SetAnimatorTransition("isInfant", false, "isJuvie", true);
                break;

            case Phase.Juvenile:
                currentPhase = Phase.Adult;
                nextPowerLevel = playerConfig.phaseThresholds.beast;
                SetAnimatorTransition("isJuvie", false, "isAdult", true);
                break;

            case Phase.Adult:
                currentPhase = Phase.Beast;
                nextPowerLevel = playerConfig.phaseThresholds.monster;
                SetAnimatorTransition("isAdult", false, "isBeast", true);
                break;

            case Phase.Beast:
                currentPhase = Phase.Monster;
                nextPowerLevel = playerConfig.phaseThresholds.victory;
                SetAnimatorTransition("isBeast", false, "isMonster", true);
                break;

            case Phase.Monster:
                HandleVictory();
                return;
        }

        OnPhaseChanged?.Invoke(oldPhase, currentPhase);
        DebugLog($"Matured from {oldPhase} to {currentPhase}!");
    }

    private void SetAnimatorTransition(string oldParam, bool oldValue, string newParam, bool newValue)
    {
        if (animator != null)
        {
            animator.SetBool(oldParam, oldValue);
            animator.SetBool(newParam, newValue);
        }
    }

    private void SetAnimatorPhase(Phase phase)
    {
        if (animator == null) return;
        
        animator.SetBool("isInfant", phase == Phase.Infant);
        animator.SetBool("isJuvie", phase == Phase.Juvenile);
        animator.SetBool("isAdult", phase == Phase.Adult);
        animator.SetBool("isBeast", phase == Phase.Beast);
        animator.SetBool("isMonster", phase == Phase.Monster);
    }

    private void HandleVictory()
    {
        OnVictory?.Invoke();
        SceneManager.LoadScene("Victory");
    }

    public Phase GetCurrentPhase() => currentPhase;
    public int GetNextPowerLevel() => nextPowerLevel;
    
    public float GetPhaseProgress(int currentPowerLevel)
    {
        if (currentPhase == Phase.Monster) return 1f;
        
        int previousThreshold = GetPreviousPhaseThreshold();
        return Mathf.Clamp01((float)(currentPowerLevel - previousThreshold) / (nextPowerLevel - previousThreshold));
    }

    private int GetPreviousPhaseThreshold()
    {
        switch (currentPhase)
        {
            case Phase.Infant: return 0;
            case Phase.Juvenile: return playerConfig.phaseThresholds.juvenile;
            case Phase.Adult: return playerConfig.phaseThresholds.adult;
            case Phase.Beast: return playerConfig.phaseThresholds.beast;
            case Phase.Monster: return playerConfig.phaseThresholds.monster;
            default: return 0;
        }
    }

    public void TriggerBiteAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("isBiting");
        }
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs) GameLogger.LogVerbose($"[PlayerPhases] {message}");
    }
}
