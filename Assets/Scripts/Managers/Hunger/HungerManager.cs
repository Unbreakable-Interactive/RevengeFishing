using UnityEngine;
using System.Collections;

public class HungerManager : MonoBehaviour
{
    [Header("Hunger Configuration")]
    [SerializeField] private float hungerPercentageRate = 0.02f;
    [SerializeField] private float hungerUpdateInterval = 1f;

    [Header("Fatigue Recovery Configuration")]
    [SerializeField] private float fatigueRecoveryPercentageRate = 0.02f;
    [SerializeField] private float fatigueUpdateInterval = 1f;

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
        if (player == null) return;

        StartHungerSystem();
        StartFatigueRecoverySystem();
    }

    public void StartHungerSystem()
    {
        if (player == null) return;
        hungerCoroutine = StartCoroutine(HungerAccumulationCoroutine());
    }

    public void StartFatigueRecoverySystem()
    {
        if (player == null) return;
        fatigueRecoveryCoroutine = StartCoroutine(FatigueRecoveryCoroutine());
    }

    public void StopHungerSystem()
    {
        if (hungerCoroutine != null) { StopCoroutine(hungerCoroutine); hungerCoroutine = null; }
    }

    public void StopFatigueRecoverySystem()
    {
        if (fatigueRecoveryCoroutine != null) { StopCoroutine(fatigueRecoveryCoroutine); fatigueRecoveryCoroutine = null; }
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

            if (newHunger >= maxHunger)
            {
                HandleStarvation();
            }
        }
        
        player.PlayerStats.RefreshNow();
    }

    private void RecoverFatigue()
    {
        if (player == null) return;
        if (player.GetFatigue() <= 0) return;

        float hungerPct = player.HungerHandler.GetHungerPercentage();
        float efficiency = 1f - hungerPct;

        float baseFromHM = player.PowerLevel * fatigueRecoveryPercentageRate;
        float baseFromEF = player.PowerLevel * player.entityFatigue.GetEffectiveRegenPercentPerSecond();
        float perSecond = baseFromHM + baseFromEF;

        int actualRecovery = Mathf.RoundToInt(perSecond * efficiency);
        if (hungerPct < 0.8f && actualRecovery > 0)
        {
            player.HungerHandler.ModifyFatigue(-actualRecovery);
            OnFatigueChanged?.Invoke(player.GetFatigue());
        }
        
        player.PlayerStats.RefreshNow();
    }

    private float CalculateCurrentHungerRate()
    {
        if (player == null) return 0f;
        float decayMult = player.HungerHandler.GetHungerDecayMultiplier();
        return player.PowerLevel * hungerPercentageRate * decayMult;
    }

    private void HandleStarvation()
    {
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

    private void OnDestroy()
    {
        StopHungerSystem();
        StopFatigueRecoverySystem();
    }
}
