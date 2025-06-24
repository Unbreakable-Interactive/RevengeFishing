using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishermanScript : EnemyBase
{
    [Header("Fisherman Configuration")]
    public FishermanConfig fishermanConfig = new FishermanConfig();

    protected override void Start()
    {
        base.Start();
        _type = EnemyType.Land;
    }

    protected override void Update()
    {
        base.Update();

        if (hasThrownHook) HandleActiveHook();
    }

    private void HandleActiveHook()
    {
        if (!hasThrownHook) return;

        // ADD HOOK TIMER LOGIC
        hookTimer += Time.deltaTime;

        // Retract hook after hookDuration seconds
        if (hookSpawner.CurrentHook != null &&
            hookTimer >= hookDuration &&
            !hookSpawner.CurrentHook.isBeingHeld)
        {
            if (hookSpawner.HasActiveHook())
            {
                // Start retracting the hook gradually
                float retractionSpeed = 2f; // Units per second (adjustable)
                hookSpawner.RetractHook(retractionSpeed * Time.deltaTime);
            }
        }

        // Check if hook is gone
        if (!hookSpawner.HasActiveHook())
        {
            CleanupHookSubscription();

            hasThrownHook = false;
            hookTimer = 0f; // RESET TIMER

            // chance to put away rod after fishing
            if (UnityEngine.Random.value < fishermanConfig.unequipToolChance)
            {
                TryUnequipFishingTool();
            }
        }
    }

    // RESET TIMER WHEN THROWING NEW HOOK
    protected override void MakeAIDecision()
    {
        if (_state == EnemyState.Defeated)  return;

        // Override base movement decisions when we can fish
        if (_landMovementState == LandMovementState.Idle && !hasThrownHook)
        {
            if (!fishingToolEquipped)
            {
                // chance to equip fishing tool when idle
                if (UnityEngine.Random.value < fishermanConfig.equipToolChance)
                {
                    TryEquipFishingTool();
                    ScheduleNextAction();
                    return;
                }
            }
            else
            {
                // With fishing tool equipped, choose between fishing and unequipping
                float random = UnityEngine.Random.value;
                if (random < fishermanConfig.hookThrowChance)
                {
                    // Try to fish
                    if (hookSpawner?.CanThrowHook() == true)
                    {
                        hookSpawner.ThrowHook();
                        hasThrownHook = true;
                        hookTimer = 0f; // RESET TIMER WHEN THROWING

                        SubscribeToHookEvents();
                    }
                }
                else if (random < (fishermanConfig.hookThrowChance + fishermanConfig.unequipToolChance))
                {
                    // Put away fishing tool
                    TryUnequipFishingTool();
                    return;
                }
            }
            ScheduleNextAction();
            return;
        }

        //If not fishing, use base movement AI
        base.MakeAIDecision();
    }

    private void SubscribeToHookEvents()
    {
        CleanupHookSubscription();

        // Get current hook from spawner
        if (hookSpawner.CurrentHook is FishingHook fishingHook)
        {
            subscribedHook = fishingHook;
            fishingHook.OnPlayerInteraction += OnHookPlayerInteraction;
        }
    }

    protected override void CleanupFishingTools()
    {
        base.CleanupFishingTools();

        // Destroy the fishing hook handler immediately when defeated
        if (hookSpawner != null && hookSpawner.HasActiveHook())
        {
            // Clean up hook subscription first
            CleanupHookSubscription();

            // Destroy the hook handler (same as when putting it away)
            hookSpawner.OnHookDestroyed();

            // Reset fishing state
            hasThrownHook = false;
            hookTimer = 0f;

            Debug.Log($"Fisherman {gameObject.name} - Hook handler destroyed due to defeat");
        }
    }

    public override void WaterMovement()
    {
        // Implementation for water movement
    }

}
