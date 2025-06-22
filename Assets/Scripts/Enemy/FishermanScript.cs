using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishermanScript : EnemyBase
{
    [Header("Fisherman Configuration")]
    public FishermanConfig config = new FishermanConfig();

    [Header("Fishing Behavior Settings")]
    [SerializeField] private float fishingToolEquipChance = 0.6f; // INCREASED FOR MORE FISHING
    [SerializeField] private float hookThrowChance = 0.8f; // INCREASED FOR MORE HOOKS
    [SerializeField] private float toolUnequipChance = 0.1f; // DECREASED TO KEEP TOOLS LONGER
    [SerializeField] private float fishingActivityCooldown = 1f; // REDUCED COOLDOWN

    private HookSpawner hookSpawner;
    private bool hasThrownHook;
    private float hookTimer = 0f;
    private float hookDuration = 8f;
    private FishingHook subscribedHook;
    
    private float lastFishingActionTime = 0f;
    private bool isFishingActivityAllowed = true;

    public override void Initialize()
    {
        base.Initialize();
        _type = EnemyType.Land;
        hasFishingTool = true;
        
        // GET THE EXISTING HOOKSPAWNER
        hookSpawner = GetComponent<HookSpawner>();
        
        if (hookSpawner != null)
        {
            Debug.Log($"Found HookSpawner on {gameObject.name}, calling Initialize()");
            hookSpawner.Initialize();
            Debug.Log($"Fisherman {gameObject.name} - HookSpawner initialized: CanThrow={hookSpawner.CanThrowHook()}");
        }
        else
        {
            Debug.LogError($"❌ Fisherman {gameObject.name} - NO HookSpawner component found!");
        }
        
        currentMovementState = LandMovementState.Idle;
        ScheduleNextAction();
        
        Debug.Log($"✅ Fisherman {gameObject.name} fully initialized");
    }

    protected override void Update()
    {
        base.Update();
        HandleActiveHook();
        UpdateFishingCooldown();
    }

    private void UpdateFishingCooldown()
    {
        if (!isFishingActivityAllowed && Time.time >= lastFishingActionTime + fishingActivityCooldown)
        {
            isFishingActivityAllowed = true;
            Debug.Log($"Fisherman {gameObject.name} - Fishing cooldown ended");
        }
    }

    private void HandleActiveHook()
    {
        if (!hasThrownHook) return;

        hookTimer += Time.deltaTime;

        if (hookSpawner.currentHook != null &&
            hookTimer >= hookDuration &&
            !hookSpawner.currentHook.isBeingHeld)
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

            if (UnityEngine.Random.value < 0.3f) // REDUCED - Keep tool equipped longer
            {
                TryUnequipFishingTool();
            }
        }
    }

    protected override void MakeAIDecision()
    {
        if (_state == EnemyState.Defeated)
        {
            Debug.Log($"Fisherman {gameObject.name} - Defeated, limited AI");
            base.MakeAIDecision();
            return;
        }

        Debug.Log($"🧠 Fisherman {gameObject.name} - AI Decision: State={currentMovementState}, Tool={fishingToolEquipped}, Hook={hasThrownHook}, CooldownOK={isFishingActivityAllowed}");

        // FISHING BEHAVIOR - TRY MORE OFTEN
        bool didFishingAction = false;
        if (!hasThrownHook && isFishingActivityAllowed)
        {
            didFishingAction = TryFishingBehavior();
        }

        // MOVEMENT BEHAVIOR - ALLOW EVEN WHEN FISHING TOOL EQUIPPED
        if (!hasThrownHook || !hookSpawner.HasActiveHook())
        {
            Debug.Log($"🚶 Fisherman {gameObject.name} - Calling base movement AI");
            base.MakeAIDecision();
        }
        else
        {
            ScheduleNextAction();
            Debug.Log($"🎣 Fisherman {gameObject.name} - Actively fishing, movement restricted");
        }
    }

    private bool TryFishingBehavior()
    {
        Debug.Log($"🎣 Fisherman {gameObject.name} - Trying fishing: Tool={fishingToolEquipped}, CanThrow={hookSpawner?.CanThrowHook()}, HookSpawner={hookSpawner != null}");
        
        if (!fishingToolEquipped)
        {
            if (UnityEngine.Random.value < fishingToolEquipChance)
            {
                if (TryEquipFishingTool())
                {
                    StartFishingCooldown();
                    Debug.Log($"✅ Fisherman {gameObject.name} - Equipped fishing tool");
                    return true;
                }
            }
        }
        else
        {
            float random = UnityEngine.Random.value;
            
            if (random < hookThrowChance)
            {
                Debug.Log($"🪝 Fisherman {gameObject.name} - Attempting hook throw: CanThrow={hookSpawner?.CanThrowHook()}");
                
                if (hookSpawner != null && hookSpawner.CanThrowHook())
                {
                    Debug.Log($"🚀 Fisherman {gameObject.name} - THROWING HOOK NOW!");
                    hookSpawner.ThrowHook();
                    hasThrownHook = true;
                    hookTimer = 0f;
                    SubscribeToHookEvents();
                    StartFishingCooldown();
                    Debug.Log($"✅ Fisherman {gameObject.name} - Hook thrown successfully!");
                    return true;
                }
                else
                {
                    Debug.LogError($"❌ Fisherman {gameObject.name} - Cannot throw hook! HookSpawner exists: {hookSpawner != null}, CanThrow: {hookSpawner?.CanThrowHook()}");
                }
            }
            else if (random < hookThrowChance + toolUnequipChance)
            {
                if (TryUnequipFishingTool())
                {
                    StartFishingCooldown();
                    Debug.Log($"✅ Fisherman {gameObject.name} - Unequipped fishing tool");
                    return true;
                }
            }
        }
        
        return false;
    }

    private void StartFishingCooldown()
    {
        lastFishingActionTime = Time.time;
        isFishingActivityAllowed = false;
        Debug.Log($"⏰ Fisherman {gameObject.name} - Started fishing cooldown ({fishingActivityCooldown}s)");
    }

    protected override void ExecuteLandMovementBehaviour()
    {
        if (_state == EnemyState.Defeated)
        {
            Vector2 movement = Vector2.zero;
            switch (currentMovementState)
            {
                case LandMovementState.WalkLeft:
                    movement = Vector2.left * (walkingSpeed * 0.3f);
                    break;
                case LandMovementState.WalkRight:
                    movement = Vector2.right * (walkingSpeed * 0.3f);
                    break;
            }
            rb.velocity = new Vector2(movement.x, rb.velocity.y);
            Debug.Log($"Fisherman {gameObject.name} - Defeated movement: {movement}");
            return;
        }

        // ONLY STOP MOVEMENT WHEN ACTIVELY FISHING WITH HOOK DEPLOYED
        if (hasThrownHook && hookSpawner.HasActiveHook())
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            Debug.Log($"🎣 Fisherman {gameObject.name} - Movement stopped: actively fishing");
        }
        else
        {
            Debug.Log($"🚶 Fisherman {gameObject.name} - Normal movement: {currentMovementState}");
            base.ExecuteLandMovementBehaviour();
        }
    }

    protected override void CheckGroundedStatus()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1.5f);
        
        if (hit.collider != null)
        {
            Platform hitPlatform = hit.collider.GetComponent<Platform>();
            if (hitPlatform != null || (assignedPlatform != null && hit.collider.transform.IsChildOf(assignedPlatform.transform)))
            {
                isGrounded = true;
            }
            else
            {
                isGrounded = true; // Hit something solid
            }
        }
        else
        {
            isGrounded = false;
        }
    }

    #region Hook Events

    private void SubscribeToHookEvents()
    {
        CleanupHookSubscription();

        if (hookSpawner.currentHook is FishingHook fishingHook)
        {
            subscribedHook = fishingHook;
            fishingHook.OnPlayerInteraction += OnHookPlayerInteraction;
        }
    }

    private void CleanupHookSubscription()
    {
        if (subscribedHook != null)
        {
            subscribedHook.OnPlayerInteraction -= OnHookPlayerInteraction;
            subscribedHook = null;
        }
    }

    private void OnHookPlayerInteraction(bool isBeingHeld)
    {
        if (hookSpawner.currentHook != null)
        {
            hookSpawner.currentHook.isBeingHeld = isBeingHeld;
            Debug.Log($"Fisherman: Hook interaction: {isBeingHeld}");
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
        }
    }

    public override void ReverseFishingBehaviour() { }
    public override void WaterMovement() { }

    private void OnDestroy()
    {
        CleanupHookSubscription();
    }

    #endregion
}
