using UnityEngine;

public enum UpgradeEffectType
{
    None,
    UnlockSpecial,
    FatigueRegenMultiplier,
    EatSatiationMultiplier,
    PullStrengthMultiplier,
    MaxSpeedAdd,
    AccelerationMultiplier,
    MaxFatigueMultiplier,
    MaxHungerMultiplier,
    HungerDecayMultiplier
}

public enum SpecialToUnlock
{
    Backflip,
    BigBite
}

[CreateAssetMenu(fileName = "NewUpgrade", menuName = "RevengeFishing2D/Upgrade")]
public class UpgradeSO : ScriptableObject
{
    [Header("UI")]
    public string title;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Disponibilidad")]
    public int cost = 1;
    public bool unique = true;
    public Player.Phase minPhase = Player.Phase.Infant;

    [Header("Efecto")]
    public UpgradeEffectType effectType = UpgradeEffectType.None;

    public SpecialToUnlock special;
    public float value = 1f;

    public void Apply(Player player)
    {
        if (!player) return;

        switch (effectType)
        {
            case UpgradeEffectType.UnlockSpecial:
                UnlockAbilityOnPlayer(player, special);
                break;

            case UpgradeEffectType.FatigueRegenMultiplier:
                player.MultiplyFatigueRegen(value);
                break;

            case UpgradeEffectType.EatSatiationMultiplier:
                player.MultiplyEatSatiation(value);
                break;

            case UpgradeEffectType.PullStrengthMultiplier:
                player.MultiplyMagnetForce(value);
                break;

            case UpgradeEffectType.MaxSpeedAdd:
                player.AddMaxSpeed(value);
                break;

            case UpgradeEffectType.AccelerationMultiplier:
                player.MultiplyAcceleration(value);
                break;

            case UpgradeEffectType.MaxFatigueMultiplier:
                player.MultiplyMaxFatigue(value);
                break;

            case UpgradeEffectType.MaxHungerMultiplier:
                player.MultiplyMaxHunger(value);
                break;

            case UpgradeEffectType.HungerDecayMultiplier:
                player.MultiplyHungerDecay(value);
                break;
        }
    }

    private void UnlockAbilityOnPlayer(Player player, SpecialToUnlock which)
    {
        var sys = player.AbilitySystem ?? player.GetComponentInChildren<AbilitySystem>(true);
        if (!sys) return;

        switch (which)
        {
            case SpecialToUnlock.Backflip:
            {
                var ab = player.GetComponentInChildren<Backflip>(true);
                if (!ab) return;
                ((MonoBehaviour)ab).enabled = true;
                ab.Unlock();
                sys.RegisterAbility(ab);
                break;
            }

            case SpecialToUnlock.BigBite:
            {
                var ab = player.GetComponentInChildren<BigBite>(true);
                if (!ab) return;
                ((MonoBehaviour)ab).enabled = true;
                ab.Unlock();
                sys.RegisterAbility(ab);
                break;
            }
        }
    }
}