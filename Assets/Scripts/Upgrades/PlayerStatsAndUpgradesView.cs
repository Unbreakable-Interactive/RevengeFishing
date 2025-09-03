using UnityEngine;
using TMPro;
using System.Reflection;

public class PlayerStatsAndUpgradesView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI baseStatsText;
    [SerializeField] private TextMeshProUGUI specialsText;

    [Header("References")]
    [SerializeField] private Player player;

    void Start()
    {
        if (!player) player = Player.Instance;
    }

    public void RefreshNow()
    {
        if (!player) player = Player.Instance;
        if (!player) return;

        int power = player.PowerLevel;
        
        float currentEnergy = player.entityEnergy.CurrentEnergy;
        float maxEnergy = player.entityEnergy.MaxEnergy;
        float energyPercentage = player.entityEnergy.EnergyPercentage;

        float maxSpeed = GetPrivateFloat(player, "maxSpeed");
        float accel = GetPrivateFloat(player, "constantAccel");
        float steering = GetPrivateFloat(player, "steeringForce");

        if (baseStatsText)
        {
            baseStatsText.text =
                $"Phase: {player.currentPhase}\n" +
                $"Power: {power}\n" +
                $"Energy: {currentEnergy:F1}/{maxEnergy:F1} ({energyPercentage * 100:F1}%)\n" +
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

        float energyRecoveryMult = player.entityEnergy.EnergyRecoveryMultiplier;

        string specials = 
            $"Backflip: {(backflipUnlocked ? "✓" : "✗")}\n" +
            $"BigBite: {(bigBiteUnlocked ? "✓" : "✗")}\n" +
            $"Energy Recovery: {energyRecoveryMult:0.##}x";

        if (specialsText)
            specialsText.text = specials;
    }

    private float GetPrivateFloat(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null && field.FieldType == typeof(float))
        {
            return (float)field.GetValue(obj);
        }
        return 0f;
    }
}
