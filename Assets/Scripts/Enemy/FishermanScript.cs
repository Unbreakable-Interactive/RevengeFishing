using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishermanScript : EnemyBase
{
    [Header("Fisherman Configuration")]
    public FishermanConfig config = new FishermanConfig();

    private HookSpawner hookSpawner;
    private float decisionTimer = 0f;
    private bool hasThrownHook;

    private float hookTimer = 0f;
    private float hookDuration = 8f; // Hook stays out for 8 seconds

    protected override void Start()
    {
        base.Start();
        _type = EnemyType.Land;
        hasFishingTool = true;
        hookSpawner = GetComponent<HookSpawner>() ?? gameObject.AddComponent<HookSpawner>();
    }

    protected override void Update()
    {
        base.Update();

        // SINGLE decision system - every 3 seconds
        decisionTimer += Time.deltaTime;
        if (decisionTimer >= 3f)
        {
            MakeAIDecision();
            decisionTimer = 0f;
        }

        HandleActiveHook();
    }

    private void HandleActiveHook()
    {
        if (!hasThrownHook) return;

        // ADD HOOK TIMER LOGIC
        hookTimer += Time.deltaTime;

        // Retract hook after hookDuration seconds
        if (hookTimer >= hookDuration)
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
            hasThrownHook = false;
            hookTimer = 0f; // RESET TIMER

            // 60% chance to put away rod after fishing
            if (UnityEngine.Random.value < 0.6f)
            {
                TryUnequipFishingTool();
            }
        }
    }

    // RESET TIMER WHEN THROWING NEW HOOK
    private void MakeAIDecision()
    {
        // Override base movement decisions when we can fish
        if (currentMovementState == LandMovementState.Idle && !hasThrownHook)
        {
            if (!fishingToolEquipped)
            {
                // 60% chance to equip fishing tool when idle
                if (UnityEngine.Random.value < 0.6f)
                {
                    TryEquipFishingTool();
                    return;
                }
            }
            else
            {
                // With fishing tool equipped, choose between fishing and unequipping
                float random = UnityEngine.Random.value;
                if (random < 0.5f)
                {
                    // Try to fish
                    if (hookSpawner?.CanThrowHook() == true)
                    {
                        hookSpawner.ThrowHook();
                        hasThrownHook = true;
                        hookTimer = 0f; // RESET TIMER WHEN THROWING
                    }
                }
                else
                {
                    // Put away fishing tool
                    TryUnequipFishingTool();
                }
                return;
            }
        }

        // If not fishing, use base movement AI
        if (!fishingToolEquipped)
        {
            ChooseRandomLandAction();
            ScheduleNextAction();
        }
    }


    // Override movement to prevent movement when fishing
    protected override void ExecuteLandMovementBehaviour()
    {
        if (fishingToolEquipped)
        {
            currentMovementState = LandMovementState.Idle;
            // No movement when fishing
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
        else
        {
            base.ExecuteLandMovementBehaviour();
        }
    }

    public override void ReverseFishingBehaviour()
    {
        // This method can be removed or simplified
        // since we have unified decision making
    }

    public override void WaterMovement()
    {
        // Implementation for water movement
    }
}
