using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private bool debugBoatCrew = true;

    [Header("Boat Crew Systems")]
    [SerializeField] private BoatCrewPhysics crewPhysics;
    [SerializeField] public FishermanConfig fishermanConfig;

    [Header("Boat Crew AI Probabilities")]
    [SerializeField, Range(0f, 100f)] 
    [Tooltip("Probabilidad de lanzar hook. El resto se divide entre caminar/correr")]
    private float hookThrowProbability = 40f;

    [SerializeField, Range(0f, 100f)]
    [Tooltip("Del tiempo restante (sin hook), probabilidad de caminar vs quedarse idle")]
    private float walkProbability = 60f;

    [SerializeField, Range(0f, 100f)]
    [Tooltip("Del tiempo de caminar, probabilidad de correr vs caminar normal")]
    private float runProbability = 30f;

    [SerializeField, Range(0.5f, 10f)]
    [Tooltip("Multiplicador de frecuencia de decisiones de AI")]
    private float aiDecisionFrequency = 1f;

    [Header("Boat Specific Settings")]
    [SerializeField] private float localBoundaryLeft = -1.5f;
    [SerializeField] private float localBoundaryRight = 1.5f;
    [SerializeField] private bool enableHookThrowing = true;

    [Header("Crew Behavior")]
    [SerializeField] private CrewRole crewRole = CrewRole.Sailor;
    [SerializeField] private bool isNavigating = false;

    [Header("Debug Probabilities")]
    [SerializeField] private bool debugProbabilities = false;

    private Transform crewContainer;
    private bool boatContextInitialized = false;
    private bool isHandlingDefeat = false;
    private bool hasFallenFromBoat = false;
    private bool hasInteractedWithPlayer = false;

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
            Debug.Log($"[BOAT DEBUG] {gameObject.name} - Set boundaries: Left={left:F2}, Right={right:F2}");
    }

    public override void SetSubscribedHook(FishingProjectile fishingHook)
    {
        base.SetSubscribedHook(fishingHook);
        if (debugBoatCrew)
            Debug.Log($"[BOAT DEBUG] {gameObject.name} - Hook subscribed: {(fishingHook != null ? fishingHook.name : "NULL")}");
    }

    public override void Initialize()
    {
        int powerLevel = CalculateBalancedBoatCrewPowerLevel();
        SetPowerLevel(powerLevel);
        base.Initialize();
        
        if (debugBoatCrew)
        {
            Debug.Log($"[BOAT DEBUG] {gameObject.name} - Initialize: Balanced Power Level {_powerLevel}, State: {_state}");
            Debug.Log($"[BOAT DEBUG] {gameObject.name} - Fatigue: {entityFatigue.fatigue}/{entityFatigue.maxFatigue}");
        }
    }

    private int CalculateBalancedBoatCrewPowerLevel()
    {
        if (Player.Instance == null)
        {
            return 120;
        }

        int playerPowerLevel = Player.Instance.PowerLevel;
        
        float minMultiplier = 0.8f;
        float maxMultiplier = 1.2f;
        
        float randomMultiplier = Random.Range(minMultiplier, maxMultiplier);
        int balancedPowerLevel = Mathf.RoundToInt(playerPowerLevel * randomMultiplier);
        
        balancedPowerLevel = Mathf.Clamp(balancedPowerLevel, 80, 500);
        
        if (debugBoatCrew)
        {
            Debug.Log($"[BOAT DEBUG] {gameObject.name} - Crew Power Calculation:");
            Debug.Log($"    Player Power: {playerPowerLevel}");
            Debug.Log($"    Multiplier: {randomMultiplier:F2}");
            Debug.Log($"    Crew Power: {balancedPowerLevel}");
        }
        
        return balancedPowerLevel;
    }

    public override void TakeFatigue(int playerPowerLevel)
    {
        if (debugBoatCrew)
        {
            Debug.Log($"[BOAT DEBUG] {gameObject.name} - BEFORE: Fatigue {entityFatigue.fatigue}/{entityFatigue.maxFatigue}, State: {_state}");
            Debug.Log($"[BOAT DEBUG] {gameObject.name} - Player hitting with power: {playerPowerLevel}");
        }

        int fatigueToAdd = CalculateBalancedFatigueIncrease(playerPowerLevel);
        
        if (!hasReceivedFirstFatigue)
        {
            hasReceivedFirstFatigue = true;
            canPullPlayer = true;
            OnFirstFatigueReceived();
            GameLogger.Log($"{gameObject.name} received first fatigue damage - can now pull player!");
        }

        entityFatigue.fatigue += fatigueToAdd;

        if (debugBoatCrew)
        {
            Debug.Log($"[BOAT DEBUG] {gameObject.name} - Fatigue added: {fatigueToAdd}");
            Debug.Log($"[BOAT DEBUG] {gameObject.name} - AFTER: Fatigue {entityFatigue.fatigue}/{entityFatigue.maxFatigue}, State: {_state}");
        }

        if (entityFatigue.fatigue >= entityFatigue.maxFatigue && _state == EnemyState.Alive)
        {
            if (debugBoatCrew)
                Debug.Log($"[BOAT DEBUG] {gameObject.name} - FATIGUE FULL! Triggering defeat...");
            TriggerDefeat();
        }
    }

    private int CalculateBalancedFatigueIncrease(int playerPowerLevel)
    {
        float powerRatio = (float)playerPowerLevel / _powerLevel;
        
        int baseFatigue = Mathf.RoundToInt(_powerLevel * 0.15f);
        
        float adjustmentMultiplier = 1f;
        
        if (powerRatio > 1.5f)
        {
            adjustmentMultiplier = 1.5f;
        }
        else if (powerRatio < 0.7f)
        {
            adjustmentMultiplier = 0.7f;
        }
        
        int finalFatigue = Mathf.RoundToInt(baseFatigue * adjustmentMultiplier);
        
        finalFatigue = Mathf.Clamp(finalFatigue, 10, 100);
        
        if (debugBoatCrew)
        {
            Debug.Log($"[BOAT DEBUG] {gameObject.name} - Fatigue Calculation:");
            Debug.Log($"    Player Power: {playerPowerLevel}, Crew Power: {_powerLevel}");
            Debug.Log($"    Power Ratio: {powerRatio:F2}");
            Debug.Log($"    Base Fatigue: {baseFatigue}, Adjustment: {adjustmentMultiplier:F2}");
            Debug.Log($"    Final Fatigue: {finalFatigue}");
        }
        
        return finalFatigue;
    }

    protected override void EnemySetup()
    {
        base.EnemySetup();
        if (debugBoatCrew)
            Debug.Log($"[BOAT DEBUG] {gameObject.name} - EnemySetup completed, State: {_state}");
    }

    public void AssignToWheel()
    {
        // isNavigating = true;
        // crewRole = CrewRole.Navigator;
        // _landMovementState = LandMovementState.Idle;
        // fishingToolEquipped = false;
        
        if (debugBoatCrew)
            Debug.Log($"[BOAT DEBUG] {gameObject.name} - ASSIGNED TO WHEEL! Role: {crewRole}, Navigating: {isNavigating}");
    }

    public void ReleaseFromWheel()
    {
        // isNavigating = false;
        // if (crewRole == CrewRole.Navigator)
        // {
        //     crewRole = CrewRole.Sailor;
        // }
        
        if (debugBoatCrew)
            Debug.Log($"[BOAT DEBUG] {gameObject.name} - RELEASED FROM WHEEL! Role: {crewRole}, Navigating: {isNavigating}");
    }

    public void InitializeBoatContext(BoatController controller, BoatFloater floater, BoatPlatform platform)
    {
        if (boatContextInitialized) return;

        crewContainer = controller.CrewContainer;
        InitializeBoatCrewSystems();
        boatContextInitialized = true;
        
        if (debugBoatCrew)
            Debug.Log($"[BOAT DEBUG] {gameObject.name} - Boat context initialized");
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
                Debug.Log($"[BOAT DEBUG] {gameObject.name} - Joined boat crew");
        }
    }

    public void JoinBoatCrewAtPosition(Transform container, Vector3 localPosition)
    {
        if (crewPhysics != null)
        {
            crewPhysics.SetupAtPosition(container, localPosition);
            isOnBoat = true;
            if (debugBoatCrew)
                Debug.Log($"[BOAT DEBUG] {gameObject.name} - Joined boat crew at position: {localPosition}");
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
                Debug.Log($"[BOAT DEBUG] {gameObject.name} - Joined boat crew as child at position: {handlerLocalPosition}");
        }
    }

    public void LeaveBoatCrew()
    {
        if (crewPhysics != null)
        {
            crewPhysics.LeaveBoat();
            isOnBoat = false;
            if (debugBoatCrew)
                Debug.Log($"[BOAT DEBUG] {gameObject.name} - Left boat crew");
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
            if (debugBoatCrew)
                Debug.Log($"[BOAT DEBUG] {gameObject.name} - DEFEATED AND ON BOAT! Triggering fall...");
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
            if (debugBoatCrew)
                Debug.Log($"[BOAT DEBUG] {gameObject.name} - Subscribed to hook events: {fishingHook.name}");
        }
    }

    public override void OnHookPlayerInteraction(bool isBeingHeld)
    {
        if (hasInteractedWithPlayer) return;

        hasInteractedWithPlayer = true;
        base.OnHookPlayerInteraction(isBeingHeld);
        
        if (debugBoatCrew)
            Debug.Log($"[BOAT DEBUG] {gameObject.name} - Player interaction handled once. IsBeingHeld: {isBeingHeld}");

        StartCoroutine(ResetInteractionFlag());
    }

    private IEnumerator ResetInteractionFlag()
    {
        yield return new WaitForSeconds(0.5f);
        hasInteractedWithPlayer = false;
    }

    private void HandleFallFromBoat()
    {
        if (isHandlingDefeat) return;

        isHandlingDefeat = true;
        if (debugBoatCrew)
            Debug.Log($"[BOAT DEBUG] {gameObject.name} - Handling fall from boat");
            
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
    }

    public override void SetMovementMode(bool aboveWater)
    {
        if (debugBoatCrew)
            Debug.Log($"[BOAT DEBUG] {gameObject.name} - SetMovementMode called: aboveWater={aboveWater}, isOnBoat={isOnBoat}");

        if (isOnBoat)
        {
            if (crewPhysics != null)
                crewPhysics.SetBoatMode(true);
            
            if (debugBoatCrew)
                Debug.Log($"[BOAT DEBUG] {gameObject.name} - Boat movement mode: {(aboveWater ? "Above Water" : "In Water")} - STAYING KINEMATIC");
            return;
        }

        if (debugBoatCrew)
            Debug.Log($"[BOAT DEBUG] {gameObject.name} - CALLING BASE SetMovementMode({aboveWater})");
        
        base.SetMovementMode(aboveWater);
        
        if (crewPhysics != null)
            crewPhysics.SetBoatMode(false);
        
        if (debugBoatCrew)
            Debug.Log($"[BOAT DEBUG] {gameObject.name} - BASE SetMovementMode complete. RigidbodyType: {(rb != null ? rb.bodyType.ToString() : "NULL")}, Gravity: {(rb != null ? rb.gravityScale.ToString("F2") : "NULL")}");
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
        if (crewPhysics == null || !crewPhysics.IsParentedToBoat()) return;

        float localX = transform.localPosition.x;

        if (localX <= localBoundaryLeft && (_landMovementState == LandMovementState.WalkLeft || _landMovementState == LandMovementState.RunLeft))
        {
            Vector3 clampedPos = transform.localPosition;
            clampedPos.x = localBoundaryLeft + 0.1f;
            crewPhysics.SetLocalPosition(clampedPos);
            StopMovementAndChooseNewAction(LandMovementState.WalkLeft, LandMovementState.RunLeft);
        }
        else if (localX >= localBoundaryRight && (_landMovementState == LandMovementState.WalkRight || _landMovementState == LandMovementState.RunRight))
        {
            Vector3 clampedPos = transform.localPosition;
            clampedPos.x = localBoundaryRight - 0.1f;
            crewPhysics.SetLocalPosition(clampedPos);
            StopMovementAndChooseNewAction(LandMovementState.WalkRight, LandMovementState.RunRight);
        }
    }

    private void StopMovementAndChooseNewAction(params LandMovementState[] excludedStates)
    {
        _landMovementState = LandMovementState.Idle;
        ChooseRandomActionExcluding(excludedStates);
        ScheduleNextActionWithFrequency();
    }

    protected virtual void MakeBoatAIDecision()
    {
        if (_state == EnemyState.Defeated || isNavigating) return;

        if (_landMovementState == LandMovementState.Idle && !hasThrownHook)
        {
            if (!fishingToolEquipped)
            {
                if (fishermanConfig != null && Random.value < fishermanConfig.equipToolChance)
                {
                    TryEquipFishingTool();
                    ScheduleNextActionWithFrequency();
                    return;
                }
            }
            else
            {
                float randomValue = Random.Range(0f, 100f);
                
                if (debugProbabilities)
                {
                    Debug.Log($"[BOAT AI] {gameObject.name} - Random: {randomValue:F1}%, Hook Threshold: {hookThrowProbability:F1}%");
                }
                
                if (randomValue <= hookThrowProbability)
                {
                    if (hookSpawner?.CanThrowHook() == true)
                    {
                        if (debugProbabilities)
                            Debug.Log($"[BOAT AI] {gameObject.name} - 🎣 THROWING HOOK!");
                        
                        hookSpawner.ThrowHook();
                        hasThrownHook = true;
                        hookTimer = 0f;
                        SubscribeToHookEvents();
                        ScheduleNextActionWithFrequency();
                        return;
                    }
                }
                else if (fishermanConfig != null && Random.value < fishermanConfig.unequipToolChance)
                {
                    if (debugProbabilities)
                        Debug.Log($"[BOAT AI] {gameObject.name} - 🔄 UNEQUIPPING TOOL!");
                    
                    TryUnequipFishingTool();
                    ScheduleNextActionWithFrequency();
                    return;
                }
            }
            
            DecideMovementWithBalancedProbabilities();
        }
        else
        {
            ChooseRandomLandAction();
        }
        
        ScheduleNextActionWithFrequency();
    }

    private void DecideMovementWithBalancedProbabilities()
    {
        float remainingProbability = 100f - hookThrowProbability;
        float actualWalkChance = (remainingProbability * walkProbability) / 100f;
        
        float randomValue = Random.Range(0f, 100f);
        
        if (debugProbabilities)
        {
            Debug.Log($"[BOAT AI] {gameObject.name} - Movement Decision:");
            Debug.Log($"    Hook Prob: {hookThrowProbability:F1}%");
            Debug.Log($"    Remaining: {remainingProbability:F1}%");
            Debug.Log($"    Walk from remaining: {walkProbability:F1}% = {actualWalkChance:F1}% total");
            Debug.Log($"    Random: {randomValue:F1}%");
        }
        
        if (randomValue <= actualWalkChance)
        {
            float runRandomValue = Random.Range(0f, 100f);
            
            if (runRandomValue <= runProbability)
            {
                _landMovementState = Random.value < 0.5f ? LandMovementState.RunLeft : LandMovementState.RunRight;
                
                if (debugProbabilities)
                    Debug.Log($"[BOAT AI] {gameObject.name} - 🏃 RUNNING {(_landMovementState == LandMovementState.RunLeft ? "LEFT" : "RIGHT")} (Prob: {runProbability:F1}%)");
            }
            else
            {
                _landMovementState = Random.value < 0.5f ? LandMovementState.WalkLeft : LandMovementState.WalkRight;
                
                if (debugProbabilities)
                    Debug.Log($"[BOAT AI] {gameObject.name} - 🚶 WALKING {(_landMovementState == LandMovementState.WalkLeft ? "LEFT" : "RIGHT")}");
            }
        }
        else
        {
            _landMovementState = LandMovementState.Idle;
            
            if (debugProbabilities)
                Debug.Log($"[BOAT AI] {gameObject.name} - 🧍 STAYING IDLE");
        }
    }

    private void ScheduleNextActionWithFrequency()
    {
        float baseInterval = Random.Range(minActionTime, maxActionTime);
        float adjustedInterval = baseInterval / aiDecisionFrequency;
    
        nextActionTime = Time.time + adjustedInterval;
    
        if (debugProbabilities)
            Debug.Log($"[BOAT AI] {gameObject.name} - Next action in {adjustedInterval:F1}s (freq: {aiDecisionFrequency:F1}x)");
    }

    private void ExecuteBoatLandMovementBehaviour()
    {
        if (crewPhysics == null || !crewPhysics.IsParentedToBoat())
        {
            if (debugProbabilities)
                Debug.LogWarning($"[BOAT AI] {gameObject.name} - Cannot move: CrewPhysics not parented to boat");
            return;
        }

        if (fishingToolEquipped && _landMovementState != LandMovementState.Idle)
        {
            _landMovementState = LandMovementState.Idle;
            if (debugProbabilities)
                Debug.Log($"[BOAT AI] {gameObject.name} - STOPPED MOVEMENT: Tool equipped");
            return;
        }

        Vector3 currentPos = transform.localPosition;
        float deltaTime = Time.deltaTime;
        bool isMoving = false;

        switch (_landMovementState)
        {
            case LandMovementState.WalkLeft:
                currentPos.x -= walkingSpeed * deltaTime;
                isMoving = true;
                if (debugProbabilities && Time.frameCount % 60 == 0)
                    Debug.Log($"[BOAT AI] {gameObject.name} - WALKING LEFT at speed {walkingSpeed}");
                break;
            case LandMovementState.WalkRight:
                currentPos.x += walkingSpeed * deltaTime;
                isMoving = true;
                if (debugProbabilities && Time.frameCount % 60 == 0)
                    Debug.Log($"[BOAT AI] {gameObject.name} - WALKING RIGHT at speed {walkingSpeed}");
                break;
            case LandMovementState.RunLeft:
                currentPos.x -= runningSpeed * deltaTime;
                isMoving = true;
                if (debugProbabilities && Time.frameCount % 60 == 0)
                    Debug.Log($"[BOAT AI] {gameObject.name} - RUNNING LEFT at speed {runningSpeed}");
                break;
            case LandMovementState.RunRight:
                currentPos.x += runningSpeed * deltaTime;
                isMoving = true;
                if (debugProbabilities && Time.frameCount % 60 == 0)
                    Debug.Log($"[BOAT AI] {gameObject.name} - RUNNING RIGHT at speed {runningSpeed}");
                break;
            case LandMovementState.Idle:
                if (debugProbabilities && Time.frameCount % 120 == 0)
                    Debug.Log($"[BOAT AI] {gameObject.name} - STAYING IDLE");
                break;
        }

        if (isMoving)
        {
            crewPhysics.SetLocalPosition(currentPos);
        }

        if (EnemyAnimator != null)
        {
            EnemyAnimator.SetBool(IsWalking, _landMovementState == LandMovementState.WalkLeft || _landMovementState == LandMovementState.WalkRight);
            EnemyAnimator.SetBool(IsRunning, _landMovementState == LandMovementState.RunLeft || _landMovementState == LandMovementState.RunRight);
            EnemyAnimator.SetBool(IsIdle, _landMovementState == LandMovementState.Idle);
            EnemyAnimator.SetBool(RodEquipped, fishingToolEquipped);
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

        LandMovementState[] possibleStates = {
            // LandMovementState.Idle,
            LandMovementState.WalkLeft,
            LandMovementState.WalkRight,
            LandMovementState.RunLeft,
            LandMovementState.RunRight
        };

        _landMovementState = possibleStates[Random.Range(0, possibleStates.Length)];
    
        if (debugProbabilities)
            Debug.Log($"[BOAT AI] {gameObject.name} - RANDOM ACTION: {_landMovementState}");
    }

    public override void ChooseRandomActionExcluding(params LandMovementState[] excludedStates)
    {
        var availableStates = new List<LandMovementState>
        {
            LandMovementState.Idle,
            LandMovementState.WalkLeft,
            LandMovementState.WalkRight,
            LandMovementState.RunLeft,
            LandMovementState.RunRight
        };

        foreach (var excludedState in excludedStates)
        {
            availableStates.Remove(excludedState);
        }

        if (availableStates.Count > 0)
        {
            _landMovementState = availableStates[Random.Range(0, availableStates.Count)];
        }
    }

    public void ResetToOriginalState()
    {
        _state = EnemyState.Alive;
        hasFallenFromBoat = false;
        isHandlingDefeat = false;
        fishingToolEquipped = false;
        hasThrownHook = false;
        hookTimer = 0f;
        _landMovementState = LandMovementState.Idle;
        hasInteractedWithPlayer = false;

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
            Debug.Log($"[BOAT DEBUG] {gameObject.name} - Reset to original state");
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
        }
    }

    public override void WaterMovement()
    {
        
    }

    protected override void TriggerDefeat()
    {
        if (debugBoatCrew)
        {
            Debug.Log($"[BOAT DEBUG] {gameObject.name} - TriggerDefeat called. Current state: {_state}, IsOnBoat: {isOnBoat}");
        }

        CleanupFishingTools();

        if (isOnBoat)
        {
            if (debugBoatCrew)
                Debug.Log($"[BOAT DEBUG] {gameObject.name} - Enemy defeated while on boat, will handle fall from boat");
            
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
        }
        else
        {
            if (debugBoatCrew)
                Debug.Log($"[BOAT DEBUG] {gameObject.name} - Enemy defeated while not on boat, normal defeat behavior");
        }

        base.TriggerDefeat();
    }

    protected override void TriggerEscape()
    {
        if (debugBoatCrew)
            Debug.Log($"[BOAT DEBUG] {gameObject.name} - TriggerEscape called");

        CleanupBoatSystems();
        base.TriggerEscape();
    }
}
