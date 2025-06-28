using UnityEngine;

public class Fisherman : LandEnemy
{
    [Header("Fisherman Configuration")]
    [SerializeField] private FishermanConfig _fishermanConfig;
    [Tooltip("Units per second")]
    [SerializeField] float retractionSpeed = 2f;

    
    protected override void Start()
    {
        base.Start();
    }

    public override void Initialize()
    {
        base.Initialize();
        
        // Validate config is assigned
        if (_fishermanConfig == null)
        {
            Debug.LogError($"FishermanConfig is not assigned to {gameObject.name}! Please assign it in the inspector.");
        }
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

        // ✅ FIXED: Use correct property name
        if (hookSpawner.CurrentHook != null && hookTimer >= hookDuration && !hookSpawner.CurrentHook.isBeingHeld)
        {
            if (hookSpawner.HasActiveHook())
                hookSpawner.RetractHook(retractionSpeed * Time.deltaTime);
        }

        if (!hookSpawner.HasActiveHook())
        {
            CleanupHookSubscription();
            hasThrownHook = false;
            hookTimer = 0f;

            if (_fishermanConfig != null && Random.value < _fishermanConfig.unequipToolChance)
                TryUnequipFishingTool();
        }
    }

    protected override void MakeAIDecision()
    {
        if (_state == EnemyState.Defeated) return;
        if (_fishermanConfig == null) return; // Safety check

        if (_landMovementState == LandMovementState.Idle && !hasThrownHook)
        {
            if (!fishingToolEquipped)
            {
                if (Random.value < _fishermanConfig.equipToolChance)
                {
                    ScheduleNextAction();
                    TryEquipFishingTool();
                    return;
                }
            }
            else
            {
                float random = Random.value;
                
                if (random < _fishermanConfig.hookThrowChance)
                {
                    if (hookSpawner?.CanThrowHook() == true)
                    {
                        hookSpawner.ThrowHook();
                        hasThrownHook = true;
                        hookTimer = 0f;
                        
                        SubscribeToHookEvents();
                    }
                }
                else if (random < (_fishermanConfig.hookThrowChance + _fishermanConfig.unequipToolChance))
                {
                    // Put away fishing tool
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

        // Get current hook from spawner
        // if (hookSpawner.CurrentHook is FishingProjectile fishingHook)
        // {
        //     subscribedHook = fishingHook;
        //     fishingHook.OnPlayerInteraction += OnHookPlayerInteraction;
        // }
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
