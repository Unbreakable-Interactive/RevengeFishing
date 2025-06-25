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
        hasFishingTool = true;
    }

    public override void Initialize()
    {
        base.Initialize();
        _type = EnemyType.Land;
        hasFishingTool = true;
    }

    protected override void Update()
    {
        base.Update();
        
        if (hasThrownHook) 
        {
            HandleActiveHook();
        }
    }

    private void HandleActiveHook()
    {
        if (!hasThrownHook) return;

        hookTimer += Time.deltaTime;

        // ✅ FIXED: Use correct property name
        if (hookSpawner.currentHook != null && hookTimer >= hookDuration && !hookSpawner.currentHook.isBeingHeld)
        {
            if (hookSpawner.HasActiveHook())
            {
                float retractionSpeed = 2f;
                hookSpawner.RetractHook(retractionSpeed * Time.deltaTime);
            }
        }

        if (!hookSpawner.HasActiveHook())
        {
            CleanupHookSubscription();
            hasThrownHook = false;
            hookTimer = 0f;

            if (UnityEngine.Random.value < fishermanConfig.unequipToolChance)
            {
                TryUnequipFishingTool();
            }
        }
    }

    protected override void MakeAIDecision()
    {
        if (_state == EnemyState.Defeated) return;

        if (_landMovementState == LandMovementState.Idle && !hasThrownHook)
        {
            if (!fishingToolEquipped)
            {
                if (UnityEngine.Random.value < fishermanConfig.equipToolChance)
                {
                    TryEquipFishingTool();
                    ScheduleNextAction();
                    return;
                }
            }
            else
            {
                float random = UnityEngine.Random.value;
                
                if (random < fishermanConfig.hookThrowChance)
                {
                    if (hookSpawner?.CanThrowHook() == true)
                    {
                        hookSpawner.ThrowHook();
                        hasThrownHook = true;
                        hookTimer = 0f;
                        SubscribeToHookEvents();
                    }
                }
                else if (random < (fishermanConfig.hookThrowChance + fishermanConfig.unequipToolChance))
                {
                    TryUnequipFishingTool();
                    return;
                }
            }

            ScheduleNextAction();
            return;
        }

        base.MakeAIDecision();
    }

    private void SubscribeToHookEvents()
    {
        CleanupHookSubscription();

        // ✅ FIXED: Use correct property name
        if (hookSpawner.currentHook is FishingHook fishingHook)
        {
            subscribedHook = fishingHook;
            fishingHook.OnPlayerInteraction += OnHookPlayerInteraction;
        }
    }

    protected override void CleanupFishingTools()
    {
        base.CleanupFishingTools();

        if (hookSpawner != null && hookSpawner.HasActiveHook())
        {
            CleanupHookSubscription();
            hookSpawner.OnHookDestroyed();
            hasThrownHook = false;
            hookTimer = 0f;
            Debug.Log($"Fisherman {gameObject.name} - Hook handler destroyed due to defeat");
        }
    }

    public override void ReverseFishingBehaviour() 
    {
        // Implementation for reverse fishing behavior
    }

    public override void WaterMovement()
    {
        // Implementation for water movement
    }
}
