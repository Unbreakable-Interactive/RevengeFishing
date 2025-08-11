using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class LandEnemy : Enemy, IBoatComponent
{
    [Header("Land Enemy Configuration")]
    public LandEnemyConfig landEnemyConfig;

    [Header("Boat Identity")]
    [SerializeField] protected BoatID boatID = new BoatID();
    
    public virtual string GetBoatID() => boatID?.UniqueID ?? "NO_ID";
    public virtual void SetBoatID(BoatID newBoatID) => boatID = newBoatID;

    #region Land Enemy Variables
    public enum LandMovementState
    {
        Idle,
        WalkLeft,
        WalkRight,
        RunLeft,
        RunRight
    }

    [SerializeField] protected LandMovementState _landMovementState;
    public LandMovementState MovementStateLand
    {
        get { return _landMovementState; }
        set { _landMovementState = value; }
    }

    [Header("Land Enemy Variables")]
    [SerializeField] protected float walkingSpeed;
    public float WalkingSpeed=> walkingSpeed;
    [SerializeField] protected float runningSpeed;
    public float RunningSpeed => runningSpeed;
    [SerializeField] protected float edgeBuffer;
    public bool fishingToolEquipped = false;
    public bool isOnBoat = false;
    [SerializeField] protected GameObject toolDropPrefab;
    public float platformLeftEdge;
    public float platformRightEdge;
    public bool platformBoundsCalculated;
    protected IdleDetector idleDetector;
    [SerializeField] protected float maxUpwardVelocity;
    [SerializeField] protected float weight;

    [Header("Escape System")]
    [SerializeField] protected bool hasStartedFloating = false;
    [SerializeField] protected HookSpawner hookSpawner;
    public HookSpawner HookSpawner => hookSpawner;
    protected bool hasThrownHook;
    
    [SerializeField] protected float hookTimer;
    [SerializeField] protected float hookDuration;
    public float HookDuration => hookDuration;
    [SerializeField] protected FishingProjectile subscribedHook;

    [Header("Pull Mechanic")]
    [SerializeField] protected bool isPullingPlayer = false;
    [SerializeField] protected float minLineLength = 2f;
    [SerializeField] protected float maxLineReduction = 1.5f;
    [SerializeField] protected float lineReductionVariation = 0.4f;

    [SerializeField] private Animator animator;
    public Animator EnemyAnimator => animator;

    public Vector3 InitialSpawnPosition { get; set; }

    public bool HasStartedFloating
    {
        get { return hasStartedFloating; }
        set { hasStartedFloating = value; }
    }

    public bool HasThrownHook
    {
        get { return hasThrownHook; }
        set { hasThrownHook = value; }
    }

    public float HookTimer
    {
        get { return hookTimer; }
        set { hookTimer = value; }
    }
    #endregion

    public void SetSubscribedHook(FishingProjectile fishingHook)
    {
        subscribedHook = fishingHook;
    }

    #region Platform Assignment
    [Header("Platform Assignment")]
    public Platform assignedPlatform;

    public virtual void SetAssignedPlatform(Platform platform)
    {
        assignedPlatform = platform;
    }

    public virtual Platform GetAssignedPlatform()
    {
        return assignedPlatform;
    }

    public virtual void OnPlatformAssigned(Platform platform)
    {
        assignedPlatform = platform;
        
        if (assignedPlatform != null && !platformBoundsCalculated)
        {
            CalculatePlatformBounds();
        }

        if (Time.time >= nextActionTime - 0.5f)
        {
            nextActionTime = Time.time + Random.Range(0.5f, 1.5f);
        }
        
        _landMovementState = LandMovementState.Idle;
        fishingToolEquipped = false;
        InitialSpawnPosition = transform.position;
    }

    public void ClearPlatformAssignment()
    {
        if (assignedPlatform != null)
        {
            assignedPlatform.UnregisterEnemy(this);
            assignedPlatform = null;
        }
        platformBoundsCalculated = false;
        platformLeftEdge = 0f;
        platformRightEdge = 0f;
        GameLogger.LogVerbose($"{gameObject.name} platform assignment cleared");
    }

    public override void ScheduleNextAction()
    {
        if (landEnemyConfig != null)
        {
            nextActionTime = Time.time + Random.Range(1f, 3f);
            
            if (assignedPlatform != null && assignedPlatform.showDebugInfo)
                GameLogger.LogVerbose($"{gameObject.name}: Next action scheduled for {nextActionTime:F1}");
        }
        else
        {
            base.ScheduleNextAction();
        }
    }
    #endregion

    #region Basic Actions
    protected override void Start()
    {
        base.Start();
    }

    public override void Initialize()
    {
        base.Initialize();
        EnemySetup();
    }

    protected override void EnemySetup()
    {
        base.EnemySetup();
        platformBoundsCalculated = false;
        if (landEnemyConfig == null)
        {
            landEnemyConfig = Resources.Load<LandEnemyConfig>("LandEnemyConfig");
            
            if (landEnemyConfig == null)
            {
                landEnemyConfig = ScriptableObject.CreateInstance<LandEnemyConfig>();
                landEnemyConfig.idleProbability = 0.4f;
                landEnemyConfig.walkProbability = 0.3f;
                landEnemyConfig.runProbability = 0.3f;
                landEnemyConfig.identifier = TypeIdentifier.Land;
                
                GameLogger.LogWarning($"{gameObject.name}: No LandEnemyConfig assigned! Created default config.");
            }
            else
            {
                GameLogger.LogVerbose($"{gameObject.name}: Loaded default LandEnemyConfig from Resources");
            }
        }

        nextActionTime = Time.time + Random.Range(0.5f, 2f);
        _landMovementState = LandMovementState.Idle;
        
        if (hookSpawner == null)
        {
            hookSpawner = GetComponent<HookSpawner>();
        }
        
        hookSpawner.Initialize();
        SetMovementMode(isAboveWater);
        idleDetector = GetComponentInChildren<IdleDetector>();
        if (idleDetector != null && _landMovementState == LandMovementState.Idle && idleDetector.ShouldAvoidIdle())
        {
            GameLogger.LogVerbose($"{gameObject.name} moved on spawn due to overlap with idle enemies");
            ChooseMovementAction();
        }
        animator = GetComponent<Animator>();
        GameLogger.Log($"{gameObject.name} - Enemy initialized with power level {_powerLevel}");
        GameLogger.LogVerbose($"Animator found: {animator != null}");
    }

    protected override void Update()
    {
        base.Update();
        if (hasStartedFloating && rb.velocity.y > 0)
        {
            if (!animator.GetBool("isRising")) animator?.SetBool("isRising", true);
        }
    }

    public override void SetMovementMode(bool aboveWater)
    {
        base.SetMovementMode(aboveWater);
        GameLogger.LogVerbose($"{gameObject.name} SetMovementMode called: aboveWater={aboveWater}, state={_state}, hasStartedFloating={hasStartedFloating}");
        
        if (aboveWater)
        {
            if (_state == EnemyState.Defeated && hasStartedFloating)
            {
                GameLogger.Log($"{gameObject.name} - ESCAPE CONDITIONS MET! Triggering escape.");
                TriggerEscape();
                return;
            }
            else if (_state == EnemyState.Defeated)
            {
                GameLogger.LogVerbose($"{gameObject.name} - Defeated but escape conditions not met: hasStartedFloating={hasStartedFloating}");
            }
            hasStartedFloating = false;
            GameLogger.LogVerbose($"{gameObject.name} enemy switched to AIRBORNE mode");
        }
        else
        {
            if (_state == EnemyState.Alive)
            {
                TriggerDefeat();
            }
            hasStartedFloating = true;
            GameLogger.LogVerbose($"{gameObject.name} enemy switched to UNDERWATER mode");
        }
    }

    public override void WaterMovement()
    {
        
    }

    protected override void AirborneBehavior()
    {
        LandMovement();
    }

    protected override void UnderwaterBehavior()
    {
        WaterMovement();
    }

    protected override void OnFirstFatigueReceived()
    {
        base.OnFirstFatigueReceived();
        if (CanPullPlayer() && hookSpawner.HasActiveHook())
        {
            StartCoroutine(ContinuousPull());
        }
    }

    private IEnumerator ContinuousPull()
    {
        while (_state == EnemyState.Alive && CanPullPlayer() && hookSpawner.HasActiveHook())
        {
            float waitTime = Random.Range(0.8f, 1.5f);
            yield return new WaitForSeconds(waitTime);
            if (_state == EnemyState.Alive && CanPullPlayer() && hookSpawner.HasActiveHook())
            {
                yield return StartCoroutine(PerformSinglePull());
            }
        }
    }

    private IEnumerator PerformSinglePull()
    {
        if (isPullingPlayer) yield break;

        isPullingPlayer = true;
        if (player == null)
        {
            GameLogger.LogError("Player not found for pull mechanic!");
            isPullingPlayer = false;
            yield break;
        }

        ApplyPullFatigueDamage();
        float lineShortened = ShortenFishingLine();

        if (lineShortened > minLineLength)
        {
            yield return StartCoroutine(ApplyReelForce(lineShortened));
        }
        else
        {
            yield return StartCoroutine(ApplyResistancePull());
        }

        isPullingPlayer = false;
    }

    private IEnumerator ApplyReelForce(float lineShortened)
    {
        Vector3 hookSpawnPoint = hookSpawner.spawnPoint.position;
        Vector3 playerPosition = player.transform.position;
        Vector3 pullDirection = (hookSpawnPoint - playerPosition).normalized;
        float pullStrength = lineShortened * pullForce;
        float pullDuration = 0.4f;

        GameLogger.LogVerbose($"[REEL FORCE] Applying reel force: direction={pullDirection}, strength={pullStrength}, duration={pullDuration}");

        float elapsedTime = 0f;
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();

        while (elapsedTime < pullDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / pullDuration;
            float currentForceMultiplier = Mathf.Lerp(1f, 0.1f, progress * progress);
            Vector2 frameForce = pullDirection * (pullStrength * currentForceMultiplier);
            playerRb.AddForce(frameForce, ForceMode2D.Force);
            yield return null;
        }
    }

    private IEnumerator ApplyResistancePull()
    {
        Vector3 hookSpawnPoint = hookSpawner.spawnPoint.position;
        Vector3 playerPosition = player.transform.position;
        Vector3 pullDirection = (hookSpawnPoint - playerPosition).normalized;
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        float resistanceForce = pullForce * 0.5f;

        GameLogger.LogVerbose($"[RESISTANCE PULL] Applying resistance force: {resistanceForce}");
        playerRb.AddForce(pullDirection * resistanceForce, ForceMode2D.Impulse);
        yield return null;
    }

    private void ApplyPullFatigueDamage()
    {
        if (player == null)
        {
            GameLogger.LogWarning("Cannot apply pull fatigue damage - player not found!");
            return;
        }

        float fatigueDamage = PowerLevel * 0.1f;
        player.TakeFishingFatigue(fatigueDamage);
    }

    private float ShortenFishingLine()
    {
        if (hookSpawner == null || !hookSpawner.HasActiveHook())
        {
            GameLogger.LogWarning("Cannot shorten line - no active hook!");
            return 0f;
        }

        float currentLineLength = hookSpawner.GetLineLength();
        float baseReduction = Random.Range(maxLineReduction - lineReductionVariation, maxLineReduction);
        float newLineLength = Mathf.Max(currentLineLength - baseReduction, minLineLength);
        float actualReduction = currentLineLength - newLineLength;

        if (actualReduction > 0)
        {
            hookSpawner.SetLineLength(newLineLength);
            return actualReduction;
        }
        else
        {
            return 0f;
        }
    }

    protected override void InterruptAllActions()
    {
        _landMovementState = LandMovementState.Idle;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
        base.InterruptAllActions();
    }

    protected override void StartDefeatBehaviors()
    {
        base.StartDefeatBehaviors();
        transform.position += new Vector3(0f, 0f, -1f);
        CleanupFishingTools();
        DropTool();
    }

    protected virtual void CleanupFishingTools()
    {
        if (fishingToolEquipped)
        {
            fishingToolEquipped = false;
            GameLogger.LogVerbose($"{gameObject.name} - Fishing tool cleaned up due to defeat");
        }
    }

    protected virtual void CleanupHookSubscription()
    {
        if (subscribedHook != null)
        {
            subscribedHook.OnPlayerInteraction -= OnHookPlayerInteraction;
            subscribedHook = null;
        }
        if (isPullingPlayer)
        {
            StopAllCoroutines();
            isPullingPlayer = false;
            Player player = Player.Instance;
            if (player != null)
                player.RemovePositionConstraint();
        }
    }

    public virtual void OnHookPlayerInteraction(bool isBeingHeld)
    {
        if (hookSpawner.CurrentHook != null)
        {
            hookSpawner.CurrentHook.isBeingHeld = isBeingHeld;
        }
    }
    #endregion

    #region Land Movement Logic
    public virtual void LandMovement()
    {
        if (Time.time >= nextActionTime)
        {
            MakeAIDecision();
        }
        CalculatePlatformBounds();
        ExecuteLandMovementBehaviour();
        if (platformBoundsCalculated)
        {
            CheckPlatformBounds();
        }
    }

    protected virtual void MakeAIDecision()
    {
        ChooseRandomLandAction();
        ScheduleNextAction();
    }

    protected virtual void CalculatePlatformBounds()
    {
        if (assignedPlatform == null) return;

        Collider2D platformCol = assignedPlatform.GetComponent<Collider2D>();
    
        if (platformCol != null)
        {
            Bounds bounds = platformCol.bounds;
            platformLeftEdge = bounds.min.x + edgeBuffer;
            platformRightEdge = bounds.max.x - edgeBuffer;
            platformBoundsCalculated = true;
        }
    }

    public override void TriggerAlive()
    {
        base.TriggerAlive();
    
        _landMovementState = LandMovementState.Idle;
        fishingToolEquipped = false;
        hasStartedFloating = false;
        hasThrownHook = false;
    
        if (rb != null)
        {
            rb.WakeUp();
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 1f;
            rb.isKinematic = false;
        }
    
        if (bodyCollider != null)
        {
            bodyCollider.isTrigger = false;
            bodyCollider.enabled = true;
        }
    
        GameLogger.Log($"{gameObject.name} LandEnemy state completely reset to Alive");
    }

    protected override void TriggerDefeat()
    {
        base.TriggerDefeat();
        OnFishingToolUnequipped();
        animator?.SetBool("isSinking", true);
    }

    protected virtual void CheckPlatformBounds()
    {
        float currentX = transform.position.x;
        if (currentX <= platformLeftEdge && (_landMovementState == LandMovementState.WalkLeft || _landMovementState == LandMovementState.RunLeft))
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            _landMovementState = LandMovementState.Idle;
            ChooseRandomActionExcluding(LandMovementState.WalkLeft, LandMovementState.RunLeft);
            ScheduleNextAction();
        }
        else if (currentX >= platformRightEdge && (_landMovementState == LandMovementState.WalkRight || _landMovementState == LandMovementState.RunRight))
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            _landMovementState = LandMovementState.Idle;
            ChooseRandomActionExcluding(LandMovementState.WalkRight, LandMovementState.RunRight);
            ScheduleNextAction();
        }
    }

    protected virtual void ChooseRandomLandAction()
    {
        if (fishingToolEquipped) return;

        float randomValue = Random.value;
        GameLogger.LogVerbose($"Choosing random land action for {gameObject.name}. Random value = {randomValue}");

        if (randomValue < landEnemyConfig.idleProbability)
        {
            if (idleDetector != null && idleDetector.ShouldAvoidIdle())
            {
                GameLogger.LogVerbose($"{gameObject.name} prevented from going idle due to overlap with {idleDetector.GetOverlappingIdleEnemyCount()} idle enemy(ies)");
                ChooseMovementAction();
                return;
            }
            _landMovementState = LandMovementState.Idle;
        }
        else if (randomValue < (landEnemyConfig.idleProbability + landEnemyConfig.walkProbability))
        {
            _landMovementState = (Random.value < 0.5f) ? LandMovementState.WalkLeft : LandMovementState.WalkRight;
        }
        else
        {
            _landMovementState = (Random.value < 0.5f) ? LandMovementState.RunLeft : LandMovementState.RunRight;
        }
    }

    protected virtual void ExecuteLandMovementBehaviour()
    {
        if (fishingToolEquipped) return;

        Vector2 movement = Vector2.zero;
        switch (_landMovementState)
        {
            case LandMovementState.Idle:
                animator?.SetBool("isWalking", false);
                animator?.SetBool("isRunning", false);
                animator?.SetBool("isIdle", true);
                break;
            case LandMovementState.WalkLeft:
                animator?.SetBool("isWalking", true);
                animator?.SetBool("isRunning", false);
                animator?.SetBool("isIdle", false);
                transform.localScale = new Vector3(-1f, 1f, 1f);
                movement = Vector2.left * walkingSpeed;
                break;
            case LandMovementState.WalkRight:
                animator?.SetBool("isWalking", true);
                animator?.SetBool("isRunning", false);
                animator?.SetBool("isIdle", false);
                transform.localScale = new Vector3(1f, 1f, 1f);
                movement = Vector2.right * walkingSpeed;
                break;
            case LandMovementState.RunLeft:
                animator?.SetBool("isWalking", false);
                animator?.SetBool("isRunning", true);
                animator?.SetBool("isIdle", false);
                transform.localScale = new Vector3(-1f, 1f, 1f);
                movement = Vector2.left * runningSpeed;
                break;
            case LandMovementState.RunRight:
                animator?.SetBool("isWalking", false);
                animator?.SetBool("isRunning", true);
                animator?.SetBool("isIdle", false);
                transform.localScale = new Vector3(1f, 1f, 1f);
                movement = Vector2.right * runningSpeed;
                break;
        }

        rb.velocity = new Vector2(movement.x, rb.velocity.y);
    }

    public virtual void ChooseRandomActionExcluding(params LandMovementState[] excludedStates)
    {
        LandMovementState[] allStates = {
            LandMovementState.Idle,
            LandMovementState.WalkLeft,
            LandMovementState.WalkRight,
            LandMovementState.RunLeft,
            LandMovementState.RunRight
        };

        List<LandMovementState> validStates = new List<LandMovementState>();

        foreach (LandMovementState state in allStates)
        {
            bool isExcluded = false;
            foreach (LandMovementState excluded in excludedStates)
            {
                if (state == excluded)
                {
                    isExcluded = true;
                    break;
                }
            }
            if (!isExcluded)
            {
                validStates.Add(state);
            }
        }

        if (validStates.Count > 0)
        {
            _landMovementState = validStates[Random.Range(0, validStates.Count)];
        }
        else
        {
            _landMovementState = LandMovementState.Idle;
        }
    }

    private void ChooseMovementAction()
    {
        float movementChoice = Random.value;
        if (movementChoice < 0.8f)
        {
            _landMovementState = (Random.value < 0.5f) ? LandMovementState.WalkLeft : LandMovementState.WalkRight;
            ExecuteLandMovementBehaviour();
        }
        else
        {
            _landMovementState = (Random.value < 0.5f) ? LandMovementState.RunLeft : LandMovementState.RunRight;
            ExecuteLandMovementBehaviour();
        }
    }
    #endregion

    #region Fishing Tool System
    public virtual bool TryEquipFishingTool()
    {
        if (fishingToolEquipped) return false;
        if (_landMovementState != LandMovementState.Idle) return false;

        fishingToolEquipped = true;
        OnFishingToolEquipped();

        if (assignedPlatform != null && assignedPlatform.showDebugInfo)
        {
            GameLogger.LogVerbose($"{gameObject.name} equipped fishing tool");
        }

        return true;
    }

    public virtual bool TryUnequipFishingTool()
    {
        if (!fishingToolEquipped) return false;

        fishingToolEquipped = false;
        OnFishingToolUnequipped();
        ChooseRandomLandAction();
        ScheduleNextAction();

        if (assignedPlatform != null && assignedPlatform.showDebugInfo)
        {
            GameLogger.LogVerbose($"{gameObject.name} put away fishing tool");
        }

        return true;
    }

    protected virtual void OnFishingToolEquipped()
    {
        animator?.SetBool("rodEquipped", true);
    }

    protected virtual void OnFishingToolUnequipped()
    {
        animator?.SetBool("rodEquipped", false);
    }

    public virtual void DropTool()
    {
        if (toolDropPrefab != null)
        {
            //GameObject droppedToolHandler = 
            Instantiate(toolDropPrefab, transform.position, transform.rotation);
            if (assignedPlatform != null && assignedPlatform.showDebugInfo)
            {
                GameLogger.LogVerbose($"{gameObject.name} dropped their tool");
            }
        }
    }
    #endregion
}
