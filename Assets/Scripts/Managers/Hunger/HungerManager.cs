using UnityEngine;
using System.Collections;

public class HungerManager : MonoBehaviour
{
    [Header("Hunger Configuration")]
    [SerializeField] private float hungerPercentageRate = 0.02f; // 2% of power level per second
    [SerializeField] private float hungerUpdateInterval = 1f; // How often to update hunger, in seconds

    [Header("Fatigue Recovery Configuration")]
    [SerializeField] private float fatigueRecoveryPercentageRate = 0.02f; // 2% of power level per second at 0% hunger
    [SerializeField] private float fatigueUpdateInterval = 1f; // How often to recover fatigue

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    [SerializeField] private Player player;
    private Coroutine hungerCoroutine;
    private Coroutine fatigueRecoveryCoroutine;

    public float CurrentHungerRate => CalculateCurrentHungerRate();
    public int MaxHunger => player?.HungerHandler.GetMaxHunger() ?? 0;

    public System.Action<int> OnHungerChanged;
    public System.Action<float> OnHungerRateChanged;
    public System.Action<int> OnFatigueChanged;

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (player == null) player = GetComponent<Player>();

        if (player == null)
        {
            GameLogger.LogError("HungerManager: No Player component found!");
            return;
        }

        StartHungerSystem();
        StartFatigueRecoverySystem();

        DebugLog($"HungerManager initialized.");
    }

    public void StartHungerSystem()
    {
        if (player == null)
        {
            GameLogger.LogError("HungerManager: Cannot start hunger system - no player reference!");
            return;
        }

        hungerCoroutine = StartCoroutine(HungerAccumulationCoroutine());
        DebugLog("Hunger system started");
    }

    public void StartFatigueRecoverySystem()
    {
        if (player == null)
        {
            GameLogger.LogError("HungerManager: Cannot start fatigue recovery system - no player reference!");
            return;
        }

        fatigueRecoveryCoroutine = StartCoroutine(FatigueRecoveryCoroutine());
        DebugLog("Fatigue recovery system started");
    }

    public void StopHungerSystem()
    {
        if (hungerCoroutine != null)
        {
            StopCoroutine(hungerCoroutine);
            hungerCoroutine = null;
        }
        DebugLog("Hunger system stopped");
    }

    public void StopFatigueRecoverySystem()
    {
        if (fatigueRecoveryCoroutine != null)
        {
            StopCoroutine(fatigueRecoveryCoroutine);
            fatigueRecoveryCoroutine = null;
        }
        DebugLog("Fatigue recovery system stopped");
    }

    private IEnumerator HungerAccumulationCoroutine()
    {
        while (player != null)
        {
            AccumulateHunger();
            yield return new WaitForSeconds(hungerUpdateInterval);
        }
    }

    private IEnumerator FatigueRecoveryCoroutine()
    {
        while (player != null)
        {
            RecoverFatigue();
            yield return new WaitForSeconds(fatigueUpdateInterval);
        }
    }

    private void AccumulateHunger()
    {
        if (player == null) return;

        float hungerIncrease = CalculateCurrentHungerRate() * hungerUpdateInterval;
        int hungerIncreaseInt = Mathf.RoundToInt(hungerIncrease);

        int currentHunger = GetPlayerHunger();
        int maxHunger = MaxHunger;

        if (currentHunger < maxHunger && hungerIncreaseInt > 0)
        {
            int newHunger = Mathf.Min(currentHunger + hungerIncreaseInt, maxHunger);
            SetPlayerHunger(newHunger);

            OnHungerChanged?.Invoke(newHunger);
            OnHungerRateChanged?.Invoke(CalculateCurrentHungerRate());

            DebugLog($"Hunger +{hungerIncreaseInt}. Current: {newHunger}/{maxHunger} ({GetHungerPercentage():F1}%). Rate: {CalculateCurrentHungerRate():F1}/s");

            if (newHunger >= maxHunger)
            {
                HandleStarvation();
            }
        }
    }

    private void RecoverFatigue()
    {
        if (player == null) return;
        if (player.GetFatigue() <= 0) return;

        float hungerPercentage = player.HungerHandler.GetHungerPercentage();
        float recoveryEfficiency = 1f - hungerPercentage;

        float baseRecovery = player.PowerLevel * fatigueRecoveryPercentageRate;

        int actualRecovery = Mathf.RoundToInt(baseRecovery * recoveryEfficiency);

        if (hungerPercentage >= .8f)
        {
            DebugLog("No fatigue recovery - too hungry!");
        }
        else if (actualRecovery > 0)
        {
            player.HungerHandler.ModifyFatigue(-actualRecovery);
            DebugLog($"Fatigue -{actualRecovery} (Efficiency: {recoveryEfficiency * 100:F0}%).");
        }
    }

    private float CalculateCurrentHungerRate()
    {
        if (player == null) return 0f;
        return player.PowerLevel * hungerPercentageRate;
    }

    private void HandleStarvation()
    {
        DebugLog("Player has starved!");
        player.PlayerDie(Player.Status.Starved);
        StopHungerSystem();
        StopFatigueRecoverySystem();
    }

    public float GetHungerPercentage()
    {
        if (player == null || MaxHunger == 0) return 0f;
        return (float)GetPlayerHunger() / MaxHunger;
    }

    private int GetPlayerHunger()
    {
        return player.HungerHandler.GetHunger();
    }

    private void SetPlayerHunger(int value)
    {
        player.HungerHandler.SetHunger(value);
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            GameLogger.LogVerbose($"[HungerManager] {message}");
        }
    }

    private void OnDestroy()
    {
        StopHungerSystem();
        StopFatigueRecoverySystem();
    }
}
