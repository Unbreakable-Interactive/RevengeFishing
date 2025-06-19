using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishermanScript : EnemyBase
{
    [Header("Fisherman Configuration")]
    public FishermanConfig config = new FishermanConfig();

    private HookSpawner hookSpawner;
    private float hookTimer;
    private float fishingBehaviorTimer;
    private bool hasThrownHook;

    // ADD: Decision-making timer
    private float fishingDecisionTimer = 0f;
    private float decisionInterval = 2f; // Make fishing decisions every 2 seconds

    protected override void Start()
    {
        base.Start();
        _type = EnemyType.Land;
        hasFishingTool = true;

        hookSpawner = GetComponent<HookSpawner>() ?? gameObject.AddComponent<HookSpawner>();
        ResetHookTimer();

        // Initialize the fishing behavior timer
        fishingBehaviorTimer = config.fishingBehaviorCheckInterval;
    }

    protected override void Update()
    {
        base.Update();

        fishingDecisionTimer += Time.deltaTime;

        // Make fishing decisions every 2 seconds instead of every frame
        if (fishingDecisionTimer >= decisionInterval)
        {
            MakeFishingDecision();
            fishingDecisionTimer = 0f;
        }

        HandleHookRetraction();
        HandleFishingBehaviorCheck();
    }

    private void MakeFishingDecision()
    {
        // Only make decisions when idle with fishing rod equipped
        if (currentMovementState != LandMovementState.Idle || !fishingToolEquipped || hasThrownHook)
            return;

        // First, decide if we should UNEQUIP the fishing rod
        float unequipChance = 0.3f; // 30% chance to unequip each decision
        if (UnityEngine.Random.value < unequipChance)
        {
            TryUnequipFishingTool();
            return; // Exit early if we unequip
        }

        // If we didn't unequip, then consider throwing the hook
        if (hookSpawner?.CanThrowHook() == true && UnityEngine.Random.value < config.hookThrowChance)
        {
            hookSpawner.ThrowHook();
            hasThrownHook = true;
        }
    }

    // increase unequip chances
    private void HandleHookRetraction()
    {
        if (!hasThrownHook) return;

        if (!hookSpawner.HasActiveHook())
        {
            hasThrownHook = false;
            ResetHookTimer();

            // 50% chance to unequip after fishing
            if (UnityEngine.Random.value < 0.5f)
            {
                TryUnequipFishingTool();
            }
            return;
        }

        hookTimer -= Time.deltaTime;
        if (hookTimer <= 0)
        {
            hookSpawner.RetractHook(Time.deltaTime);
        }
    }

    private void HandleFishingBehaviorCheck()
    {
        fishingBehaviorTimer -= Time.deltaTime;
        if (fishingBehaviorTimer <= 0)
        {
            ReverseFishingBehaviour();
            fishingBehaviorTimer = config.fishingBehaviorCheckInterval;
        }
    }

    private void ResetHookTimer()
    {
        hookTimer = UnityEngine.Random.Range(config.minHookWaitTime, config.maxHookWaitTime);
    }

    public override void ReverseFishingBehaviour()
    {
        if (!fishingToolEquipped ||
            (hookSpawner?.HasActiveHook() == true) ||
            UnityEngine.Random.value >= config.unequipToolChance)
            return;

        TryUnequipFishingTool();
    }

    public override void WaterMovement()
    {
        // Implementation for water movement
    }
}
