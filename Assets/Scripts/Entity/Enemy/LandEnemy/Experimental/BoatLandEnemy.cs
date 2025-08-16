using System.Collections;
using UnityEngine;

public class BoatLandEnemy : LandEnemy, IBoatComponent
{
    private static readonly int IsWalking = Animator.StringToHash("isWalking");
    private static readonly int IsRunning = Animator.StringToHash("isRunning");
    private static readonly int IsIdle = Animator.StringToHash("isIdle");
    private static readonly int IsRising = Animator.StringToHash("isRising");
    private static readonly int IsSinking = Animator.StringToHash("isSinking");
    private static readonly int RodEquipped = Animator.StringToHash("rodEquipped");

    [Header("Boat Identity")]
    [SerializeField] private bool debugBoatCrew = false;
    
    [Header("Boat Crew Systems")]
    [SerializeField] private BoatCrewPhysics crewPhysics;
    [SerializeField] public FishermanConfig fishermanConfig;
    
    [Header("Boat Specific Settings")]
    [SerializeField] private float localBoundaryLeft = -1.5f;
    [SerializeField] private float localBoundaryRight = 1.5f;
    [SerializeField] private bool enableHookThrowing = true;
    
    [Header("Crew Behavior")]
    [SerializeField] private CrewRole crewRole = CrewRole.Sailor;
    [SerializeField] private bool isNavigating = false;
    
    private Transform crewContainer;
    private bool boatContextInitialized = false;
    private bool isHandlingDefeat = false;
    private bool hasFallenFromBoat = false;
    private bool isInWater = false;
    
    public enum CrewRole
    {
        Sailor,
        Navigator,
        Lookout
    }
    
    public override string GetBoatID() => boatID?.UniqueID ?? "NO_ID";
    public override void SetBoatID(BoatID newBoatID) => boatID = newBoatID;
    
    public CrewRole GetCrewRole() => crewRole;
    public void SetCrewRole(CrewRole role) => crewRole = role;
    public bool IsNavigating() => isNavigating;
    
    public void SetLocalBoundaries(float left, float right)
    {
        localBoundaryLeft = left;
        localBoundaryRight = right;
        
        if (debugBoatCrew)
            GameLogger.LogVerbose($"[BOAT CREW BOUNDARIES] {gameObject.name} - Set boundaries: Left={left:F2}, Right={right:F2}");
    }
    
    public override void SetSubscribedHook(FishingProjectile fishingHook)
    {
        base.SetSubscribedHook(fishingHook);
        
        if (debugBoatCrew)
            GameLogger.LogVerbose($"[BOAT CREW HOOK] {gameObject.name} - Hook subscribed: {(fishingHook != null ? fishingHook.name : "NULL")}");
    }
    
    public override void Initialize()
    {
        int powerLevel = PowerLevelScaler.Instance.CalculateEnemyPowerLevel();
        SetPowerLevel(powerLevel);
        
        base.Initialize();
        
        if (debugBoatCrew)
            GameLogger.LogVerbose($"[BOAT CREW INIT] {gameObject.name} - Power Level: {_powerLevel}");
    }
    
    protected override void EnemySetup()
    {
        base.EnemySetup();
        
        if (debugBoatCrew)
            GameLogger.LogVerbose($"[BOAT CREW SETUP] {gameObject.name} - EnemySetup completed");
    }
    
    public void AssignToWheel()
    {
        isNavigating = true;
        crewRole = CrewRole.Navigator;
        _landMovementState = LandMovementState.Idle;
        fishingToolEquipped = false;
    }
    
    public void ReleaseFromWheel()
    {
        isNavigating = false;
        if (crewRole == CrewRole.Navigator)
        {
            crewRole = CrewRole.Sailor;
        }
    }

    public void InitializeBoatContext(BoatController controller, BoatFloater floater, BoatPlatform platform)
    {
        if (boatContextInitialized) return;
        
        crewContainer = controller.CrewContainer;
        
        InitializeBoatCrewSystems();
        boatContextInitialized = true;
        
        if (debugBoatCrew)
        {
            GameLogger.LogVerbose($"[BOAT CREW INIT] {gameObject.name} - Found CrewContainer: {(crewContainer != null ? crewContainer.name : "NULL")}");
        }
    }
    
    private void InitializeBoatCrewSystems()
    {
        if (crewPhysics == null) crewPhysics = GetComponent<BoatCrewPhysics>();
        
        if (crewPhysics != null)
            crewPhysics.Initialize(rb, this);
    }

    public void JoinBoatCrew()
    {
        if (crewPhysics != null)
        {
            isOnBoat = true;
            
            if (debugBoatCrew)
                GameLogger.LogVerbose($"[BOAT CREW] {gameObject.name} - Joined boat crew via physics system");
        }
    }
    
    public void JoinBoatCrewAtPosition(Transform container, Vector3 localPosition)
    {
        if (crewPhysics != null)
        {
            crewPhysics.SetupAtPosition(container, localPosition);
            isOnBoat = true;
            
            if (debugBoatCrew)
                GameLogger.LogVerbose($"[BOAT CREW] {gameObject.name} - Joined boat crew at local position: {localPosition}");
        }
    }

    public void JoinBoatCrewAsChild(Transform container, Vector3 handlerLocalPosition)
    {
        if (crewPhysics != null)
        {
            GameObject handlerRoot = transform.parent?.gameObject ?? gameObject;
            crewPhysics.SetupAsChildHandler(container, handlerRoot.transform, handlerLocalPosition);
            isOnBoat = true;
            
            if (debugBoatCrew)
                GameLogger.LogVerbose($"[BOAT CREW] {gameObject.name} - Joined boat crew as child of handler at: {handlerLocalPosition}");
        }
    }

    public void LeaveBoatCrew()
    {
        if (crewPhysics != null)
        {
            crewPhysics.LeaveBoat();
            isOnBoat = false;
            
            if (debugBoatCrew)
                GameLogger.LogVerbose($"[BOAT CREW] {gameObject.name} - Left boat crew via physics system");
        }
    }

    protected override void Update()
    {
        base.Update();
        
        if (_state == EnemyState.Alive)
        {
            if (isOnBoat && enableHookThrowing)
            {
                if (hasThrownHook) HandleActiveHook();
            }
        }
        else if (_state == EnemyState.Defeated && isOnBoat && !hasFallenFromBoat && !isHandlingDefeat)
        {
            HandleFallFromBoat();
            hasFallenFromBoat = true;
        }
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

            if (fishermanConfig != null && Random.value < fishermanConfig.unequipToolChance)
            {
                TryUnequipFishingTool();
            }
        }
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

    private void HandleFallFromBoat()
    {
        if (isHandlingDefeat) return;
        
        isHandlingDefeat = true;
        
        if (debugBoatCrew)
            GameLogger.LogVerbose($"[BOAT CREW DEFEAT] {gameObject.name} - Falling from boat");
        
        LeaveBoatCrew();
        
        if (assignedPlatform != null)
        {
            assignedPlatform.UnregisterEnemy(this);
            assignedPlatform = null;
        }
        
        if (isNavigating)
        {
            ReleaseFromWheel();
        }
        
        if (_state != EnemyState.Dead)
        {
            OnEnemyDied?.Invoke(this);
        }
    }

    public override void SetMovementMode(bool aboveWater)
    {
        if (_state == EnemyState.Alive && isOnBoat)
        {
            isAboveWater = aboveWater;
            isInWater = !aboveWater;
            
            if (crewPhysics != null)
            {
                crewPhysics.SetBoatMode(true);
            }
            
            if (debugBoatCrew)
                GameLogger.LogVerbose($"[BOAT CREW] {gameObject.name} - Boat movement mode: {(aboveWater ? "Above Water" : "In Water")} - Staying KINEMATIC");
        }
        else
        {
            base.SetMovementMode(aboveWater);
        }
    }
    
    public override void LandMovement()
    {
        if (isOnBoat)
        {
            if (isNavigating)
            {
                _landMovementState = LandMovementState.Idle;
                return;
            }
            
            BoatLandMovement();
        }
        else
        {
            base.LandMovement();
        }
    }
    
    private void BoatLandMovement()
    {
        if (Time.time >= nextActionTime)
        {
            MakeBoatAIDecision();
        }
        
        ExecuteBoatLandMovementBehaviour();
        CheckBoatPlatformBounds();
    }
    
    protected virtual void CheckBoatPlatformBounds()
    {
        if (crewPhysics == null || !crewPhysics.IsParentedToBoat) return;
        
        float localX = transform.localPosition.x;
        
        if (localX <= localBoundaryLeft && (_landMovementState == LandMovementState.WalkLeft || _landMovementState == LandMovementState.RunLeft))
        {
            Vector3 clampedPos = transform.localPosition;
            clampedPos.x = localBoundaryLeft + 0.1f;
            crewPhysics.SetLocalPosition(clampedPos);
            
            StopMovementAndChooseNewAction(LandMovementState.WalkLeft, LandMovementState.RunLeft);
            
            if (debugBoatCrew)
                GameLogger.LogVerbose($"[BOAT BOUNDS] {gameObject.name} - Hit LEFT boundary at local X: {localX:F2} (limit: {localBoundaryLeft:F2})");
        }
        else if (localX >= localBoundaryRight && (_landMovementState == LandMovementState.WalkRight || _landMovementState == LandMovementState.RunRight))
        {
            Vector3 clampedPos = transform.localPosition;
            clampedPos.x = localBoundaryRight - 0.1f;
            crewPhysics.SetLocalPosition(clampedPos);
            
            StopMovementAndChooseNewAction(LandMovementState.WalkRight, LandMovementState.RunRight);
            
            if (debugBoatCrew)
                GameLogger.LogVerbose($"[BOAT BOUNDS] {gameObject.name} - Hit RIGHT boundary at local X: {localX:F2} (limit: {localBoundaryRight:F2})");
        }
    }

    private void StopMovementAndChooseNewAction(params LandMovementState[] excludedStates)
    {
        _landMovementState = LandMovementState.Idle;
        ChooseRandomActionExcluding(excludedStates);
        ScheduleNextAction();
    }
    
    protected virtual void MakeBoatAIDecision()
    {
        if (_state == EnemyState.Defeated) return;

        if (_landMovementState == LandMovementState.Idle && !hasThrownHook)
        {
            if (!fishingToolEquipped)
            {
                if (fishermanConfig != null && Random.value < fishermanConfig.equipToolChance)
                {
                    ScheduleNextAction();
                    TryEquipFishingTool();
                    return;
                }
            }
            else
            {
                float random = Random.value;
                if (fishermanConfig != null && random < fishermanConfig.hookThrowChance)
                {
                    if (hookSpawner?.CanThrowHook() == true)
                    {
                        hookSpawner.ThrowHook();
                        hasThrownHook = true;
                        hookTimer = 0f;

                        SubscribeToHookEvents();
                    }
                }
                else if (fishermanConfig != null && random < (fishermanConfig.hookThrowChance + fishermanConfig.unequipToolChance))
                {
                    ScheduleNextAction();
                    TryUnequipFishingTool();
                    return;
                }
            }
            ScheduleNextAction();
            return;
        }

        ChooseRandomLandAction();
        ScheduleNextAction();
    }
    
    protected virtual void ExecuteBoatLandMovementBehaviour()
    {
        if (fishingToolEquipped || isNavigating || crewPhysics == null || !crewPhysics.IsParentedToBoat) return;
        
        Vector2 movement = Vector2.zero;
        float currentSpeed = walkingSpeed * 0.3f;
        
        switch (_landMovementState)
        {
            case LandMovementState.Idle:
                UpdateAnimations(false, false, true);
                break;
            case LandMovementState.WalkLeft:
                UpdateAnimations(true, false, false);
                transform.localScale = new Vector3(-1f, 1f, 1f);
                movement = Vector2.left * currentSpeed;
                break;
            case LandMovementState.WalkRight:
                UpdateAnimations(true, false, false);
                transform.localScale = new Vector3(1f, 1f, 1f);
                movement = Vector2.right * currentSpeed;
                break;
            case LandMovementState.RunLeft:
                UpdateAnimations(false, true, false);
                transform.localScale = new Vector3(-1f, 1f, 1f);
                movement = Vector2.left * (currentSpeed * 1.2f);
                break;
            case LandMovementState.RunRight:
                UpdateAnimations(false, true, false);
                transform.localScale = new Vector3(1f, 1f, 1f);
                movement = Vector2.right * (currentSpeed * 1.2f);
                break;
        }
        
        if (movement != Vector2.zero)
        {
            Vector3 currentLocalPos = transform.localPosition;
            Vector3 newLocalPos = new Vector3(
                currentLocalPos.x + (movement.x * Time.fixedDeltaTime),
                currentLocalPos.y,
                currentLocalPos.z
            );
            crewPhysics.SetLocalPosition(newLocalPos);
        }
    }

    private void UpdateAnimations(bool walking = false, bool running = false, bool idle = false)
    {
        EnemyAnimator?.SetBool(IsWalking, walking);
        EnemyAnimator?.SetBool(IsRunning, running);
        EnemyAnimator?.SetBool(IsIdle, idle);
    }
    
    public override void OnPlatformAssigned(Platform platform)
    {
        base.OnPlatformAssigned(platform);
        
        if (debugBoatCrew)
            GameLogger.LogVerbose($"[BOAT CREW] {gameObject.name} - Assigned to platform: {platform.name}");
    }

    public override void ReturnToPool()
    {
        if (isReturningToPool) return;
        
        isReturningToPool = true;
        
        ReleaseFromWheel();
        LeaveBoatCrew();
        
        if (assignedPlatform != null)
        {
            assignedPlatform.UnregisterEnemy(this);
            assignedPlatform = null;
        }
        
        CleanupBoatSystems();
        
        if (parentContainer != null && parentContainer.activeInHierarchy)
        {
            gameObject.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ResetToOriginalState()
    {
        ReleaseFromWheel();
        
        _state = EnemyState.Alive;
        hasFallenFromBoat = false;
        isHandlingDefeat = false;
        fishingToolEquipped = false;
        hasThrownHook = false;
        hookTimer = 0f;
        _landMovementState = LandMovementState.Idle;
        
        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
        
        if (crewPhysics != null)
        {
            crewPhysics.ResetToOriginalState();
        }
        
        EnemyAnimator?.SetBool(IsWalking, false);
        EnemyAnimator?.SetBool(IsRunning, false);
        EnemyAnimator?.SetBool(IsIdle, true);
        EnemyAnimator?.SetBool(RodEquipped, false);
        EnemyAnimator?.SetBool(IsRising, false);
        EnemyAnimator?.SetBool(IsSinking, false);
        
        if (debugBoatCrew)
            GameLogger.LogVerbose($"[BOAT CREW RESET] {gameObject.name} - Reset to original state");
    }

    private void CleanupBoatSystems()
    {
        StopAllCoroutines();
        
        if (crewPhysics != null)
            crewPhysics.ResetPhysics();
            
        crewContainer = null;
        boatContextInitialized = false;
        isHandlingDefeat = false;
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

            GameLogger.LogVerbose($"BoatLandEnemy {gameObject.name} - Hook handler destroyed due to defeat");
        }
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);
        
        if (other.gameObject.layer == LayerMask.NameToLayer("WaterLine"))
        {
            if (debugBoatCrew)
                GameLogger.LogVerbose($"[BOAT CREW WATER] {gameObject.name} - Hit water line");
            
            if (isOnBoat)
            {
                isInWater = true;
                
                if (_state == EnemyState.Defeated)
                {
                    SetMovementMode(false);
                    _landMovementState = LandMovementState.Idle;
                    ChangeState_Alive();
                }
            }
            else
            {
                SetMovementMode(false);
                _landMovementState = LandMovementState.Idle;
                ChangeState_Alive();
            }
        }
    }

    protected void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("WaterLine"))
        {
            if (debugBoatCrew)
                GameLogger.LogVerbose($"[BOAT CREW WATER] {gameObject.name} - Left water line");
            
            if (isOnBoat)
            {
                isInWater = false;
            }
            else
            {
                SetMovementMode(true);
                
                if (crewPhysics != null)
                {
                    crewPhysics.SetBoatMode(false);
                }
            }
        }
    }

    public override bool TryEquipFishingTool()
    {
        if (isNavigating) return false;
        return base.TryEquipFishingTool();
    }

    public override bool TryUnequipFishingTool()
    {
        bool result = base.TryUnequipFishingTool();
        if (result && isOnBoat)
        {
            _landMovementState = LandMovementState.Idle;
        }
        return result;
    }

    protected override void ChooseRandomLandAction()
    {
        if (fishingToolEquipped || isNavigating) return;
        
        if (isOnBoat)
        {
            float randomValue = Random.value;
            
            if (randomValue < 0.6f)
            {
                _landMovementState = LandMovementState.Idle;
            }
            else if (randomValue < 0.9f)
            {
                _landMovementState = (Random.value < 0.5f) ? LandMovementState.WalkLeft : LandMovementState.WalkRight;
            }
            else
            {
                if (!fishingToolEquipped && hookSpawner != null)
                {
                    TryEquipFishingTool();
                }
            }
        }
        else
        {
            base.ChooseRandomLandAction();
        }
    }
    
    public override void TriggerAlive()
    {
        base.TriggerAlive();
        
        hasFallenFromBoat = false;
        isHandlingDefeat = false;
        
        if (isOnBoat && crewPhysics != null)
        {
            crewPhysics.SetBoatMode(true);
        }
    }

    protected override void TriggerDefeat()
    {
        if (isHandlingDefeat) return;
        
        if (isOnBoat && isInWater)
        {
            LeaveBoatCrew();
        }
        else if (!isOnBoat && crewPhysics != null)
        {
            crewPhysics.SetBoatMode(false);
        }
        
        base.TriggerDefeat();
        
        if (debugBoatCrew)
            GameLogger.LogVerbose($"[BOAT CREW DEFEAT] {gameObject.name} - Base defeat behavior applied");
    }

    public override void WaterMovement()
    {
    }
}
