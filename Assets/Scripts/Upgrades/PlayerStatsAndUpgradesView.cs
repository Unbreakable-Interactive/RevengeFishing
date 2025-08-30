using TMPro;
using UnityEngine;

public class PlayerStatsAndUpgradesView : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private TMP_Text baseStatsText;
    [SerializeField] private TMP_Text upgradesText;

    private HungerManager hungerManager;

    private void OnEnable()
    {
        if (!player) player = Player.Instance;
        if (!player) return;

        if (!hungerManager) hungerManager = player.GetComponent<HungerManager>();
        if (hungerManager != null)
        {
            hungerManager.OnHungerChanged += _ => RefreshNow();
            hungerManager.OnFatigueChanged += _ => RefreshNow();
            hungerManager.OnHungerRateChanged += _ => RefreshNow();
        }

        RefreshNow();
    }

    private void OnDisable()
    {
        if (hungerManager != null)
        {
            hungerManager.OnHungerChanged -= _ => RefreshNow();
            hungerManager.OnFatigueChanged -= _ => RefreshNow();
            hungerManager.OnHungerRateChanged -= _ => RefreshNow();
        }
    }

    public void RefreshNow()
    {
        if (!player) player = Player.Instance;
        if (!player) return;

        int power = player.PowerLevel;
        int hunger = player.HungerHandler.GetHunger();
        int maxHunger = player.HungerHandler.GetMaxHunger();
        int fatigue = player.entityFatigue.fatigue;
        int maxFatigue = player.entityFatigue.maxFatigue;

        float effRegenPct = player.entityFatigue.GetEffectiveRegenPercentPerSecond();
        float effRegenPerSec = power * effRegenPct;

        float maxSpeed = GetPrivateFloat(player, "maxSpeed");
        float accel = GetPrivateFloat(player, "constantAccel");
        float steering = GetPrivateFloat(player, "steeringForce");

        if (baseStatsText)
        {
            baseStatsText.text =
                $"Phase: {player.currentPhase}\n" +
                $"Power: {power}\n" +
                $"Hunger: {hunger}/{maxHunger} ({(maxHunger > 0 ? (100f * hunger / maxHunger) : 0f):0.#}%)\n" +
                $"Fatigue: {fatigue}/{maxFatigue}\n" +
                $"FatigueRegen: {effRegenPerSec:0.##}/s\n" +
                $"MaxSpeed: {maxSpeed:0.##}\n" +
                $"Acceleration: {accel:0.##}\n" +
                $"Steering: {steering:0.##}";
        }

        bool backflipUnlocked = false;
        bool bigBiteUnlocked = false;
        if (player.AbilitySystem != null)
        {
            var bf = player.AbilitySystem.GetAbility<Backflip>();
            var bb = player.AbilitySystem.GetAbility<BigBite>();
            if (bf != null) backflipUnlocked = bf.IsUnlocked;
            if (bb != null) bigBiteUnlocked = bb.IsUnlocked;
        }
        else
        {
            var bf = player.GetComponentInChildren<Backflip>(true);
            var bb = player.GetComponentInChildren<BigBite>(true);
            if (bf != null) backflipUnlocked = bf.IsUnlocked;
            if (bb != null) bigBiteUnlocked = bb.IsUnlocked;
        }

        float hungerDecayMult = player.HungerHandler.GetHungerDecayMultiplier();
        float eatSatiationMult = player.HungerHandler.GetEatSatiationMultiplier();
        float regenMult = player.entityFatigue.GetRegenMultiplier();

        string specials =
            (backflipUnlocked ? "Backflip" : "-") + " | " +
            (bigBiteUnlocked ? "BigBite" : "-");

        if (upgradesText)
        {
            upgradesText.text =
                $"Specials: {specials}\n" +
                $"FatigueRegen Mult: x{regenMult:0.##}\n" +
                $"HungerDecay Mult: x{hungerDecayMult:0.##}\n" +
                $"EatSatiation Mult: x{eatSatiationMult:0.##}\n" +
                $"MaxHunger: {maxHunger}\n" +
                $"MaxFatigue: {maxFatigue}";
        }
    }

    private float GetPrivateFloat(Player p, string field)
    {
        var f = typeof(Player).GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(float)) return (float)f.GetValue(p);
        return 0f;
    }
}