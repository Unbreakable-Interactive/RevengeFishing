using UnityEngine;

public class FishingHook : FishingProjectile
{
    protected override void OnProjectileSpawned()
    {
        GameLogger.LogVerbose("Fishing hook spawned!");
    }

    protected override void OnProjectileThrown()
    {
        GameLogger.LogVerbose("Fishing hook thrown!");
    }

    protected override void OnProjectileRetracted()
    {
        GameLogger.LogVerbose("Fishing hook retracted!");
        StartCoroutine(IProjectileRetracted());
    }

    protected override void OnAirborneBehavior()
    {
        // Hook behavior in air - maybe add some wobble or wind effects
    }

    protected override void OnUnderwaterBehavior()
    {
        // Hook behavior underwater - maybe slower movement or different physics
    }

}