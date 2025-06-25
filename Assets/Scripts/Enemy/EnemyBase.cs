using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyBase : EntityMovement
{
    public enum Tier
    {
        Tier1=0,
        Tier2=1,
        Tier3=2,
        Tier4=3,
        Tier5=4,
        Tier6=5
    }

    public enum EnemyState
    {
        Alive=0,
        Defeated=1,
        Eaten=2,
        Dead=3
    }

    public enum EnemyType
    {
        Land,
        Water
    }

    public enum LandMovementState
    {
        Idle,
        WalkLeft,
        WalkRight,
        RunLeft,
        RunRight
    }

    [SerializeField] protected Tier _tier;
    [SerializeField] protected EnemyState _state;
    [SerializeField] protected EnemyType _type;

    [Header("Player Reference")]
    [SerializeField] protected PlayerMovement player;

    // time parameters for AI decisions
    [SerializeField] protected float minActionTime = 1f;
    [SerializeField] protected float maxActionTime = 4f;
    [SerializeField] protected float nextActionTime;

    #region Land Enemy Variables

    [SerializeField] protected LandMovementState _landMovementState;
    public LandMovementState MovementStateLand 
    { 
        get { return _landMovementState; } 
        set { _landMovementState = value; } 
    }

    public LandMovementState currentMovementState
    {
        get { return _landMovementState; }
        set { _landMovementState = value; }
    }

    [Header("Land Enemy Variables")]
    [SerializeField] protected float walkingSpeed = 2f;
    [SerializeField] protected float runningSpeed = 4f;
    [SerializeField] protected float edgeBuffer = 0.5f;

    [Header("Movement Probabilities")]
    [Range(0f, 1f)] public float idleProbability = 0.6f;
    [Range(0f, 1f)] public float walkProbability = 0.3f;
    [Range(0f, 1f)] public float runProbability = 0.1f;

    // Fishing tool equip system
    public bool fishingToolEquipped = false;
    protected bool hasFishingTool = false;

    [SerializeField] protected GameObject toolDropPrefab;

    protected bool isGrounded;

    // Platform bounds
    public float platformLeftEdge;
    public float platformRightEdge;
    public bool platformBoundsCalculated;

    [SerializeField] protected float maxUpwardVelocity;
    [SerializeField] protected float weight = 6f;

    [Header("Escape System")]
    [SerializeField] protected bool hasStartedFloating = false;

    protected HookSpawner hookSpawner;
    protected bool hasThrownHook;
    [SerializeField] protected float hookTimer;
    [SerializeField] protected float hookDuration = 5f;
    protected FishingHook subscribedHook;

    #endregion

    public void SetEnemyNotGrounded()
    {
        isGrounded = false;
    }

    #region Platform Assignment
    [Header("Platform Assignment")]
    public Platform assignedPlatform;

    public virtual void SetAssignedPlatform(Platform platform)
    {
        assignedPlatform = platform;
        platformBoundsCalculated = false;
    }

    public Platform GetAssignedPlatform()
    {
        return assignedPlatform;
    }

    public virtual void OnPlatformAssigned(Platform platform)
    {
        Debug.Log($"🏝️ {gameObject.name} - Platform assigned: {platform.name}");
        
        if (Time.time >= nextActionTime - 0.5f)
        {
            nextActionTime = Time.time + 0.5f;
        }
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
        isGrounded = false;
        
        Debug.Log($"{gameObject.name} platform assignment cleared");
    }

    #endregion

    #region Water Enemy Variables

    [SerializeField] protected float swimForce;
    [SerializeField] protected float minSwimSpeed;

    #endregion

    #region Base Behaviours

    // ✅ FIXED: Use Start() properly - NOT override since EntityMovement doesn't have it
    protected virtual void Start()
    {
        entityType = EntityType.Enemy;
        
        if (player == null)
        {
            player = FindObjectOfType<PlayerMovement>();
        }
        
        // Call the base Initialize() method
        Initialize();
        
        EnemySetup();
    }

    // ✅ FIXED: Override the correct Initialize() method (no parameters)
    public override void Initialize()
    {
        base.Initialize();
        
        if (player == null)
        {
            player = FindObjectOfType<PlayerMovement>();
        }
        
        EnemySetup();
    }

    // ✅ FIXED: Enemy-specific setup
    protected virtual void EnemySetup()
    {
        if (player != null)
        {
            _powerLevel = player.PowerLevel;
        }
        else
        {
            _powerLevel = 100;
        }
        
        _fatigue = 0;
        _maxFatigue = _powerLevel;
        _state = EnemyState.Alive;

        CalculateTier();

        isGrounded = false;
        platformBoundsCalculated = false;

        nextActionTime = Time.time + UnityEngine.Random.Range(0.5f, 2f);
        _landMovementState = LandMovementState.Idle;

        hookSpawner = GetComponent<HookSpawner>();
        if (hookSpawner == null)
        {
            hookSpawner = gameObject.AddComponent<HookSpawner>();
        }
        hookSpawner.Initialize();

        SetMovementMode(isAboveWater);

        if (assignedPlatform != null && !platformBoundsCalculated)
        {
            CalculatePlatformBounds();
        }

        Debug.Log($"✅ {gameObject.name} - Enemy initialized with power level {_powerLevel}");
    }

    private void CalculateTier()
    {
        if (_powerLevel > 10000000) _tier = Tier.Tier6;
        else if (_powerLevel > 1000000) _tier = Tier.Tier5;
        else if (_powerLevel > 100000) _tier = Tier.Tier4;
        else if (_powerLevel > 10000) _tier = Tier.Tier3;
        else if (_powerLevel > 1000) _tier = Tier.Tier2;
        else _tier = Tier.Tier1;
    }

    public virtual void ChangeState_Alive() 
    {
        _state = EnemyState.Alive;
        _fatigue = 0;
    
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.gravityScale = 1f;
        }
    
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = false;
        }
    
        Debug.Log($"{gameObject.name} state reset to Alive");
    }

    public abstract void ReverseFishingBehaviour();

    public virtual void LandMovement()
    {
        LandWalk();
    }

    public abstract void WaterMovement();

    protected virtual void HandleEnemyMovement()
    {
        if (isAboveWater)
        {
            AirborneBehavior();
        }
        else
        {
            UnderwaterBehavior();
        }
    }

    protected override void AirborneBehavior()
    {
        if (_type == EnemyType.Land)
        {
            LandMovement();
        }
    }

    protected override void UnderwaterBehavior()
    {
        if (_type == EnemyType.Water)
        {
            WaterMovement();
        }
    }

    public override void SetMovementMode(bool aboveWater)
    {
        base.SetMovementMode(aboveWater);

        if (_type == EnemyType.Land)
        {
            Debug.Log($"{gameObject.name} SetMovementMode called: aboveWater={aboveWater}, state={_state}, hasStartedFloating={hasStartedFloating}");

            if (aboveWater)
            {
                if (_state == EnemyState.Defeated && hasStartedFloating)
                {
                    Debug.Log($"{gameObject.name} - ESCAPE CONDITIONS MET! Triggering escape.");
                    TriggerEscape();
                    return;
                }
                else if (_state == EnemyState.Defeated)
                {
                    Debug.Log($"{gameObject.name} - Defeated but escape conditions not met: hasStartedFloating={hasStartedFloating}");
                }

                hasStartedFloating = false;
                Debug.Log($"{gameObject.name} enemy switched to AIRBORNE mode");
            }
            else
            {
                hasStartedFloating = true;
                Debug.Log($"{gameObject.name} enemy switched to UNDERWATER mode");
            }
        }
    }

    public float TakeFatigue(int playerPowerLevel)
    {
        _fatigue += (int)((float)playerPowerLevel * .05f);

        if (_fatigue >= _maxFatigue && _state == EnemyState.Alive)
        {
            TriggerDefeat();
        }

        return Mathf.Clamp(_fatigue, 0, _maxFatigue);
    }

    protected virtual void TriggerDefeat()
    {
        Debug.Log($"{gameObject.name} has been defeated!");
        ChangeState_Defeated();
        InterruptAllActions();
        StartDefeatBehaviors();
    }

    protected virtual void TriggerEaten()
    {
        Debug.Log($"{gameObject.name} has been EATEN!");
        ChangeState_Eaten();
        InterruptAllActions();
        TriggerDead();
    }

    protected virtual void TriggerDead()
    {
        Debug.Log($"{gameObject.name} has DIED!");
        ChangeState_Dead();
        InterruptAllActions();
        EnemyDie();
    }

    protected virtual void TriggerEscape()
    {
        Debug.Log($"{gameObject.name} has ESCAPED! The player can no longer catch this enemy.");
        GameObject objectToDestroy = transform.parent != null ? transform.parent.gameObject : gameObject;
        Destroy(objectToDestroy);
    }

    protected virtual void InterruptAllActions()
    {
        if (_type == EnemyType.Land)
        {
            _landMovementState = LandMovementState.Idle;
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
        }

        ScheduleNextAction();
        Debug.Log($"{gameObject.name} - Actions interrupted due to defeat");
    }

    protected virtual void StartDefeatBehaviors()
    {
        Collider2D enemyCollider = GetComponent<Collider2D>();

        if (enemyCollider == null)
        {
            enemyCollider = GetComponentInChildren<Collider2D>();
        }

        if (enemyCollider != null)
        {
            enemyCollider.isTrigger = true;
            Debug.Log($"{gameObject.name} - Collider set to trigger (phasing through platforms)");
        }

        if (_type == EnemyType.Land)
        {
            CleanupFishingTools();
            DropTool();
        }
    }

    protected virtual void CleanupFishingTools()
    {
        if (fishingToolEquipped)
        {
            fishingToolEquipped = false;
            Debug.Log($"{gameObject.name} - Fishing tool cleaned up due to defeat");
        }
    }

    protected virtual void EnemyDie()
    {
        Debug.Log($"{gameObject.name} has been REVERSE FISHED!");
        GameObject objectToDestroy = transform.parent != null ? transform.parent.gameObject : gameObject;
        Destroy(objectToDestroy);
    }

    protected virtual void CleanupHookSubscription()
    {
        if (subscribedHook != null)
        {
            subscribedHook.OnPlayerInteraction -= OnHookPlayerInteraction;
            subscribedHook = null;
        }
    }

    protected virtual void OnHookPlayerInteraction(bool isBeingHeld)
    {
        if (hookSpawner.currentHook != null)
        {
            hookSpawner.currentHook.isBeingHeld = isBeingHeld;
            Debug.Log($"Fisherman: Hook is being held: {isBeingHeld}");
        }
    }

    #endregion

    #region State Management

    public EnemyState GetState()
    {
        return _state;
    }

    public virtual void ChangeState_Defeated() => _state = EnemyState.Defeated;
    public virtual void ChangeState_Eaten() => _state = EnemyState.Eaten;
    public virtual void ChangeState_Dead() => _state = EnemyState.Dead;

    #endregion

    #region Actions

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (_state == EnemyState.Defeated && other.CompareTag("PlayerCollider"))
        {
            ChangeState_Eaten();
            TriggerEaten();
        }
    }

    protected virtual void OnDestroy()
    {
        player = null;
        assignedPlatform = null;
        CleanupHookSubscription();
    }

    #endregion

    #region Land Movement Logic

    public virtual void LandWalk()
    {
        if (_type != EnemyType.Land) return;

        CheckGroundedStatus();

        if (Time.time >= nextActionTime)
        {
            MakeAIDecision();
        }

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

        Collider2D platformCollider = assignedPlatform.GetComponent<Collider2D>();
        if (platformCollider != null)
        {
            Bounds bounds = platformCollider.bounds;
            platformLeftEdge = bounds.min.x + edgeBuffer;
            platformRightEdge = bounds.max.x - edgeBuffer;
            platformBoundsCalculated = true;

            if (assignedPlatform.showDebugInfo)
            {
                Debug.Log($"Platform bounds calculated for {gameObject.name}: Left={platformLeftEdge}, Right={platformRightEdge}");
            }
        }
    }

    protected virtual void CheckGroundedStatus()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1f);
        isGrounded = hit.collider != null && hit.collider.gameObject == assignedPlatform?.gameObject;
    }

    protected virtual void CheckPlatformBounds()
    {
        float currentX = transform.position.x;

        if (currentX <= platformLeftEdge && (_landMovementState == LandMovementState.WalkLeft || _landMovementState == LandMovementState.RunLeft))
        {
            ChooseRandomActionExcluding(LandMovementState.WalkLeft, LandMovementState.RunLeft);
            ScheduleNextAction();
        }
        else if (currentX >= platformRightEdge && (_landMovementState == LandMovementState.WalkRight || _landMovementState == LandMovementState.RunRight))
        {
            ChooseRandomActionExcluding(LandMovementState.WalkRight, LandMovementState.RunRight);
            ScheduleNextAction();
        }
    }

    protected virtual void ChooseRandomLandAction()
    {
        float randomValue = UnityEngine.Random.value;

        Debug.Log($"🎲 Choosing random land action for {gameObject.name}. Random value = {randomValue}");
        if (randomValue < idleProbability)
        {
            Debug.Log($"🛑 {gameObject.name} is idle");
            _landMovementState = LandMovementState.Idle;
        }
        else if (randomValue < (idleProbability + walkProbability))
        {
            Debug.Log($"🚶 {gameObject.name} is walking");
            _landMovementState = (UnityEngine.Random.value < 0.5f) ? LandMovementState.WalkLeft : LandMovementState.WalkRight;
        }
        else
        {
            Debug.Log($"🏃 {gameObject.name} is running");
            _landMovementState = (UnityEngine.Random.value < 0.5f) ? LandMovementState.RunLeft : LandMovementState.RunRight;
        }
    }

    protected virtual void ExecuteLandMovementBehaviour()
    {
        if (!isGrounded || assignedPlatform == null) return;

        Vector2 movement = Vector2.zero;

        switch (_landMovementState)
        {
            case LandMovementState.Idle:
                Debug.Log($"🛑 {gameObject.name} - IDLE: No movement");
                break;
            case LandMovementState.WalkLeft:
                movement = Vector2.left * walkingSpeed;
                Debug.Log($"⬅️ {gameObject.name} - WALKING LEFT at speed {walkingSpeed}");
                break;
            case LandMovementState.WalkRight:
                movement = Vector2.right * walkingSpeed;
                Debug.Log($"➡️ {gameObject.name} - WALKING RIGHT at speed {walkingSpeed}");
                break;
            case LandMovementState.RunLeft:
                movement = Vector2.left * runningSpeed;
                Debug.Log($"⬅️⬅️ {gameObject.name} - RUNNING LEFT at speed {runningSpeed}");
                break;
            case LandMovementState.RunRight:
                movement = Vector2.right * runningSpeed;
                Debug.Log($"➡️➡️ {gameObject.name} - RUNNING RIGHT at speed {runningSpeed}");
                break;
        }

        if (rb != null)
        {
            rb.velocity = new Vector2(movement.x, rb.velocity.y);
        }
    }

    protected virtual void ChooseRandomActionExcluding(params LandMovementState[] excludedStates)
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
            _landMovementState = validStates[UnityEngine.Random.Range(0, validStates.Count)];
        }
        else
        {
            _landMovementState = LandMovementState.Idle;
        }
    }

    protected virtual void ScheduleNextAction()
    {
        float actionDuration = UnityEngine.Random.Range(minActionTime, maxActionTime);
        nextActionTime = Time.time + actionDuration;
        
        Debug.Log($"⏰ {gameObject.name} - Next action scheduled in {actionDuration:F1}s");
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
            Debug.Log($"{gameObject.name} equipped fishing tool");
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
            Debug.Log($"{gameObject.name} put away fishing tool");
        }

        return true;
    }

    protected virtual void OnFishingToolEquipped()
    {
    }

    protected virtual void OnFishingToolUnequipped()
    {
    }

    public virtual void DropTool()
    {
        if (toolDropPrefab != null)
        {
            GameObject droppedTool = Instantiate(toolDropPrefab, transform.position, transform.rotation);

            Rigidbody2D toolRb = droppedTool.GetComponent<Rigidbody2D>();
            if (toolRb == null)
            {
                toolRb = droppedTool.AddComponent<Rigidbody2D>();
            }

            Vector2 dropForce = new Vector2(
                UnityEngine.Random.Range(-5f, 5f),
                UnityEngine.Random.Range(2f, 6f)
            );
            toolRb.AddForce(dropForce, ForceMode2D.Impulse);

            if (assignedPlatform != null && assignedPlatform.showDebugInfo)
            {
                Debug.Log($"{gameObject.name} dropped their tool");
            }
        }
    }

    #endregion
}
