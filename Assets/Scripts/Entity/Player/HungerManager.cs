using UnityEngine;
using System.Collections;

public class HungerManager : MonoBehaviour
{
    [Header("Hunger Configuration")]
    [SerializeField] private float hungerPercentageRate = 0.02f; // 2% of power level per second
    [SerializeField] private float hungerUpdateInterval = 1f; // How often to update hunger, in seconds

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    [SerializeField] private Player player;
    private Coroutine hungerCoroutine;

    // Properties for external access
    public float CurrentHungerRate => CalculateCurrentHungerRate();
    public int MaxHunger => player?.GetMaxHunger() ?? 0; 

    // Events
    public System.Action<int> OnHungerChanged;
    public System.Action<float> OnHungerRateChanged;

    //private void Start()
    //{
    //    Initialize();
    //}

    public void Initialize()
    {
        if (player == null)
        {
            player = GetComponent<Player>();
        }

        if (player == null)
        {
            Debug.LogError("HungerManager: No Player component found!");
            return;
        }

        StartHungerSystem();

        DebugLog($"HungerManager initialized.");
    }

    public void StartHungerSystem()
    {
        if (player == null)
        {
            Debug.LogError("HungerManager: Cannot start hunger system - no player reference!");
            return;
        }

        hungerCoroutine = StartCoroutine(HungerAccumulationCoroutine());
        DebugLog("Hunger system started");
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

    private IEnumerator HungerAccumulationCoroutine()
    {
        while (player != null)
        {
            AccumulateHunger();
            yield return new WaitForSeconds(hungerUpdateInterval);
        }
    }

    private void AccumulateHunger()
    {
        if (player == null) return;

        // Calculate hunger increase as percentage of current power level
        float hungerIncrease = CalculateCurrentHungerRate() * hungerUpdateInterval;
        int hungerIncreaseInt = Mathf.RoundToInt(hungerIncrease);

        int currentHunger = GetPlayerHunger();
        int maxHunger = MaxHunger; // This equals PowerLevel

        if (currentHunger < maxHunger && hungerIncreaseInt > 0)
        {
            int newHunger = Mathf.Min(currentHunger + hungerIncreaseInt, maxHunger);
            SetPlayerHunger(newHunger);

            OnHungerChanged?.Invoke(newHunger);
            OnHungerRateChanged?.Invoke(CalculateCurrentHungerRate());

            DebugLog($"Hunger +{hungerIncreaseInt}. Current: {newHunger}/{maxHunger} ({GetHungerPercentage():F1}%). Rate: {CalculateCurrentHungerRate():F1}/s");

            // Check for starvation
            if (newHunger >= maxHunger)
            {
                HandleStarvation();
            }
        }
    }

    private float CalculateCurrentHungerRate()
    {
        if (player == null) return 0f;

        // Hunger rate = Power Level × Percentage Rate
        return player.PowerLevel * hungerPercentageRate;
    }

    private void HandleStarvation()
    {
        DebugLog("Player has starved!");
        player.PlayerDie(Player.Status.Starved);
        StopHungerSystem();
    }

    public float GetHungerPercentage()
    {
        if (player == null || MaxHunger == 0) return 0f;
        return (float)GetPlayerHunger() / MaxHunger;
    }

    // Helper methods to access Player's hunger fields
    private int GetPlayerHunger()
    {
        return (int)typeof(Player).GetField("hunger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(player);
    }

    private void SetPlayerHunger(int value)
    {
        typeof(Player).GetField("hunger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(player, value);
    }

    // Public methods for external control
    public void ModifyHunger(int amount)
    {
        if (player == null) return;

        int currentHunger = GetPlayerHunger();
        int maxHunger = MaxHunger;
        int newHunger = Mathf.Clamp(currentHunger + amount, 0, maxHunger);

        SetPlayerHunger(newHunger);
        OnHungerChanged?.Invoke(newHunger);

        DebugLog($"Hunger modified by {amount}. New hunger: {newHunger}/{maxHunger}");
    }

    public void SetHungerPercentageRate(float newRate)
    {
        hungerPercentageRate = newRate;
        OnHungerRateChanged?.Invoke(CalculateCurrentHungerRate());
        DebugLog($"Hunger percentage rate changed to {newRate:F3}");
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[HungerManager] {message}");
        }
    }

    private void OnDestroy()
    {
        StopHungerSystem();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            StopHungerSystem();
        }
        else
        {
            StartHungerSystem();
        }
    }
}
