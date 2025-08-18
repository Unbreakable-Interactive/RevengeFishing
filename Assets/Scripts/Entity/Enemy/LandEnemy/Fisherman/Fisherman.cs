using UnityEngine;

public class Fisherman : LandEnemy
{
    [Header("Fisherman Configuration")]
    [SerializeField] protected FishermanConfig fishermanConfig;

    protected override void Start()
    {
        base.Start();
    }

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void Update()
    {
        base.Update();

        if (hasThrownHook) HandleActiveHook();
    }

    private void HandleActiveHook()
    {
        if (!hasThrownHook) return;

        hookTimer += Time.deltaTime;

        if (hookSpawner.CurrentHook != null &&
            hookTimer >= hookDuration &&
            !hookSpawner.CurrentHook.isBeingHeld)
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

            if (Random.value < fishermanConfig.unequipToolChance)
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
                if (Random.value < fishermanConfig.equipToolChance)
                {
                    ScheduleNextAction();
                    TryEquipFishingTool();
                    return;
                }
            }
            else
            {
                float random = Random.value;
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
                    ScheduleNextAction();
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

        if (hookSpawner.CurrentHook is FishingProjectile fishingHook)
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

            GameLogger.LogVerbose($"Fisherman {gameObject.name} - Hook handler destroyed due to defeat");
        }
    }

    public override void WaterMovement()
    {
        
    }
}
