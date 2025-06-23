using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyBase : EntityMovement
{
    [Header("Land Enemy Configuration")]
    public LandEnemyConfig landEnemyConfig = new LandEnemyConfig();

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

    // Movement states for land enemies
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
    [SerializeField] protected LandMovementState _landMovementState;

    public LandMovementState MovementStateLand
    {
        get { return _landMovementState; }
        set { _landMovementState = value; }
    }

    [Header("Player Reference")]
    [SerializeField] protected PlayerMovement player; // Reference to the player object

    [Header("Escape System")]
    [SerializeField] protected bool hasStartedFloating = false; // Track if enemy is floating upward

    // time parameters for AI decisions
    [SerializeField] protected float minActionTime; //Minimum seconds enemy will do an action, like walk, idle, or run
    [SerializeField] protected float maxActionTime; //Maximum seconds enemy will do an action, like walk, idle, or run
    [SerializeField] protected float nextActionTime; //actual seconds until next action decision

    protected HookSpawner hookSpawner;
    protected bool hasThrownHook;
    [SerializeField] protected float hookTimer;
    [SerializeField] protected float hookDuration;

    protected FishingHook subscribedHook;


    #region Platform Assignment
    [Header("Platform Assignment")]
    public Platform assignedPlatform; // For land enemies only

    // Method called by Platform when assigning this enemy
    public virtual void SetAssignedPlatform(Platform platform)
    {
        assignedPlatform = platform;
    }

    public Platform GetAssignedPlatform()
    {
        return assignedPlatform;
    }


    #endregion

    #region Land Enemy Variables

    [Header("Land Enemy Variables")]
    [SerializeField] protected float walkingSpeed;
    [SerializeField] protected float runningSpeed;
    [SerializeField] protected float edgeBuffer; // Distance from platform edge to change direction

    // Fishing tool equip system
    public bool fishingToolEquipped = false; // Is tool currently out?

    // For dropping tools when defeated
    [SerializeField] protected GameObject toolDropPrefab; // Tool to spawn when defeated

    protected bool isGrounded;

    // Platform bounds
    protected float platformLeftEdge;
    protected float platformRightEdge;
    protected bool platformBoundsCalculated;

    [SerializeField] protected float maxUpwardVelocity; // For when they swim upward

    [SerializeField] protected float weight; // How much the enemy sinks in water; varies between 60 and 100 kg
    
    #endregion

    #region Water Enemy Variables

    [SerializeField] protected float swimForce;
    [SerializeField] protected float minSwimSpeed;
    //protected float maxSwimSpeed;
    //already assigned in EntityMovement.cs as underwaterMaxSpeed

    #endregion

    #region Base Behaviours

    protected override void Start()
    {
        // Set entity type for water detection
        entityType = EntityType.Enemy;

        // Call base initialization (handles Rigidbody2D and water detection)
        base.Start();

        player = FindObjectOfType<PlayerMovement>();
        
        // Enemy-specific initialization
        Initialize(player.PowerLevel);
    }

    protected override void Initialize(int powerLevel)
    {
        _powerLevel = powerLevel;

        _fatigue = 0;
        _maxFatigue = _powerLevel;

        _state = EnemyState.Alive;

        CalculateTier();

        isGrounded = false;

        platformBoundsCalculated = false;

        //// weight must be a random value between x and y. Set to default 6f for now
        //weight = 6f; // How much the enemy sinks in water; varies between 60 and 100 kg

        hookSpawner = GetComponent<HookSpawner>() ?? gameObject.AddComponent<HookSpawner>();

        // Set initial movement mode
        SetMovementMode(isAboveWater);

        if (assignedPlatform != null && !platformBoundsCalculated)
        {
            CalculatePlatformBounds();
        }

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

    protected override void Update()
    {
        // Call base Update for water detection logic
        base.Update();
    }


    // How enemy behaves when it interacts with player
    //public abstract void ReverseFishingBehaviour();

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
        // Override in derived classes for airborne behavior
        if (_type == EnemyType.Land)
        {
            LandMovement();
        }
    }

    protected override void UnderwaterBehavior()
    {
        // Override in derived classes for underwater behavior
        if (_type == EnemyType.Water)
        {
            WaterMovement();
        }
        if (_type == EnemyType.Land)
        {

        }
    }

    // Override SetMovementMode to add enemy-specific behavior
    public override void SetMovementMode(bool aboveWater)
    {
        base.SetMovementMode(aboveWater); // Call base implementation

        if (_type == EnemyType.Land)
        {
            Debug.Log($"{gameObject.name} SetMovementMode called: aboveWater={aboveWater}, state={_state}, hasStartedFloating={hasStartedFloating}");

            if (aboveWater)
            {
                // If defeated enemy reaches surface while floating, they escape
                if (_state == EnemyState.Defeated && hasStartedFloating)
                {
                    Debug.Log($"{gameObject.name} - ESCAPE CONDITIONS MET! Triggering escape.");

                    TriggerEscape();
                    return; // Exit early since enemy will be destroyed
                }
                else if (_state == EnemyState.Defeated)
                {
                    Debug.Log($"{gameObject.name} - Defeated but escape conditions not met: hasStartedFloating={hasStartedFloating}");
                }

                hasStartedFloating = false; // Reset floating flag when above water
                Debug.Log($"{gameObject.name} enemy switched to AIRBORNE mode");
            }
            else
            {
                // When defeated enemy enters water, mark as floating
                hasStartedFloating = true;

                Debug.Log($"{gameObject.name} enemy switched to UNDERWATER mode");
            }

        }
    }

    public float TakeFatigue(int playerPowerLevel)
    {
        // 5% more fatigue
        _fatigue += (int)((float)playerPowerLevel * .05f);

        // Check if enemy should be defeated
        if (_fatigue >= _maxFatigue && _state == EnemyState.Alive)
        {
            TriggerDefeat();
        }

        return Mathf.Clamp(_fatigue, 0, _maxFatigue);
    }

    protected virtual void TriggerDefeat()
    {
        Debug.Log($"{gameObject.name} has been defeated!");

        // Change state to defeated
        ChangeState_Defeated();

        // Interrupt all current actions
        InterruptAllActions();

        // Start defeat behaviors
        StartDefeatBehaviors();
    }

    protected virtual void TriggerEaten()
    {
        Debug.Log($"{gameObject.name} has been EATEN!");
        // Change state to eaten
        ChangeState_Eaten();
        // Interrupt all current actions
        InterruptAllActions();
        // Handle any specific eaten logic (like cleanup)
        TriggerDead(); 
        //this will be changed later after TriggerDead() is implemented
    }

    protected virtual void TriggerDead()
    {
        Debug.Log($"{gameObject.name} has DIED!");
        // Change state to dead
        ChangeState_Dead();
        // Interrupt all current actions
        InterruptAllActions();
        // Handle any specific death logic (like cleanup)
        EnemyDie();
    }

    protected virtual void TriggerEscape()
    {
        Debug.Log($"{gameObject.name} has ESCAPED! The player can no longer catch this enemy.");

        // Destroy the parent FishermanHandler (or this object if no parent)
        GameObject objectToDestroy = transform.parent != null ? transform.parent.gameObject : gameObject;
        Destroy(objectToDestroy);
    }

    protected virtual void InterruptAllActions()
    {
        if (_type == EnemyType.Land)
        {
            // Stop any movement
            _landMovementState = LandMovementState.Idle;
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
        }

        // Clear any scheduled actions
        nextActionTime = float.MaxValue; // Prevent further AI decisions

        Debug.Log($"{gameObject.name} - All actions interrupted due to defeat");
    }

    protected virtual void StartDefeatBehaviors()
    {
        // Make enemy phase through platforms by turning collider into trigger
        Collider2D enemyCollider = GetComponent<Collider2D>();

        // If not found, look for collider in children (like your setup)
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
            // If this enemy has fishing tools, clean them up
            CleanupFishingTools();

            // Drop any tools they were carrying
            DropTool();
        }
    }

    protected virtual void CleanupFishingTools()
    {
        // This will be overridden in FishermanScript for specific cleanup
        if (fishingToolEquipped)
        {
            fishingToolEquipped = false;
            Debug.Log($"{gameObject.name} - Fishing tool cleaned up due to defeat");
        }
    }

    protected virtual void EnemyDie()
    {
        Debug.Log($"{gameObject.name} has been REVERSE FISHED!");

        // Destroy the parent FishermanHandler (or this object if no parent)
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

    // Make sure your GetState() method is public
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
        // Cleanup any references to player or platform
        player = null;
        assignedPlatform = null;
        // Cleanup hook subscription
        CleanupHookSubscription();
    }
    #endregion

    #region Land Movement Logic
    public virtual void LandWalk()
    {
        if (_type != EnemyType.Land) return;

        CheckGroundedStatus();

        //ExecuteLandMovementBehaviour();

        // Use virtual AI decision method
        if (Time.time >= nextActionTime)
        {
            MakeAIDecision(); // Virtual method that derived classes can override
        }

        if (platformBoundsCalculated)
        {
            CheckPlatformBounds();
        }
    }

    protected virtual void MakeAIDecision()
    {
        // Base enemy AI: simple random movement
        ChooseRandomLandAction();
        if (_landMovementState == LandMovementState.Idle)
        {

        }
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
        // Simple ground check - raycast down
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1f);
        isGrounded = hit.collider != null && hit.collider.gameObject == assignedPlatform?.gameObject;
    }

    protected virtual void CheckPlatformBounds()
    {
        float currentX = transform.position.x;

        // If we're near the left edge and moving left, stop or turn around
        if (currentX <= platformLeftEdge && (_landMovementState == LandMovementState.WalkLeft || _landMovementState == LandMovementState.RunLeft))
        {
            // Choose a new action that doesn't involve going left
            ChooseRandomActionExcluding(LandMovementState.WalkLeft, LandMovementState.RunLeft);
            ScheduleNextAction();
        }
        // If we're near the right edge and moving right, stop or turn around
        else if (currentX >= platformRightEdge && (_landMovementState == LandMovementState.WalkRight || _landMovementState == LandMovementState.RunRight))
        {
            // Choose a new action that doesn't involve going right
            ChooseRandomActionExcluding(LandMovementState.WalkRight, LandMovementState.RunRight);
            ScheduleNextAction();
        }
    }

    // Simplified EnemyBase.cs - remove ALL fisherman-specific code
    protected virtual void ChooseRandomLandAction()
    {
        // Only handle basic enemy movement - no fishing logic!
        float randomValue = UnityEngine.Random.value;

        Debug.Log($"Choosing random land action for {gameObject.name}. Random value = {randomValue}");
        if (randomValue < landEnemyConfig.idleProbability)
        {
            Debug.Log($"{gameObject.name} is idle");
            _landMovementState = LandMovementState.Idle;
        }
        else if (randomValue < (landEnemyConfig.idleProbability + landEnemyConfig.walkProbability))
        {
            Debug.Log($"{gameObject.name} is walking");
            _landMovementState = (UnityEngine.Random.value < 0.5f) ? LandMovementState.WalkLeft : LandMovementState.WalkRight;
        }
        else
        {
            Debug.Log($"{gameObject.name} is running");
            _landMovementState = (UnityEngine.Random.value < 0.5f) ? LandMovementState.RunLeft : LandMovementState.RunRight;
        }

        ExecuteLandMovementBehaviour();
    }

    //actually executes the action chosen by ChooseRandomLandAction
    protected virtual void ExecuteLandMovementBehaviour()
    {
        if (!isGrounded || assignedPlatform == null) return;

        Vector2 movement = Vector2.zero;

        // Simple movement - no fishing tool checks here!
        switch (_landMovementState)
        {
            case LandMovementState.Idle:
                break;
            case LandMovementState.WalkLeft:
                movement = Vector2.left * walkingSpeed;
                break;
            case LandMovementState.WalkRight:
                movement = Vector2.right * walkingSpeed;
                break;
            case LandMovementState.RunLeft:
                movement = Vector2.left * runningSpeed;
                break;
            case LandMovementState.RunRight:
                movement = Vector2.right * runningSpeed;
                break;
        }

        rb.velocity = new Vector2(movement.x, rb.velocity.y);
    }

    //used to choose a random action when at the edge of the platform
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
            _landMovementState = LandMovementState.Idle; // Fallback
        }
    }

    protected virtual void ScheduleNextAction()
    {
        float actionDuration = UnityEngine.Random.Range(minActionTime, maxActionTime);
        nextActionTime = Time.time + actionDuration;
    }

    #endregion

    #region Fishing Tool System

    /// <summary>
    /// Attempt to equip fishing tool. Only works when idle.
    /// </summary>
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

    /// <summary>
    /// Put away the fishing tool. Can only be done when tool is equipped.
    /// </summary>
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

    /// <summary>
    /// Called when fishing tool is equipped. Override in derived classes.
    /// </summary>
    protected virtual void OnFishingToolEquipped()
    {
        // Override in derived classes:
        // - Play "equip tool" animation
        // - Set up internal tool object (inactive, for dropping later)
    }

    /// <summary>
    /// Called when fishing tool is unequipped. Override in derived classes.
    /// </summary>
    protected virtual void OnFishingToolUnequipped()
    {
        // Override in derived classes:
        // - Play "put away tool" animation
    }

    /// <summary>
    /// Called when enemy is defeated. Drops the tool if equipped.
    /// </summary>
    public virtual void DropTool()
    {
        if (toolDropPrefab != null)
        {
            // Instantiate tool only when needed
            GameObject droppedTool = Instantiate(toolDropPrefab, transform.position, transform.rotation);

            // Add physics for dropping effect
            Rigidbody2D toolRb = droppedTool.GetComponent<Rigidbody2D>();
            if (toolRb == null)
            {
                toolRb = droppedTool.AddComponent<Rigidbody2D>();
            }

            // Apply random force for "flying" effect
            Vector2 dropForce = new Vector2(
                UnityEngine.Random.Range(-5f, 5f),
                UnityEngine.Random.Range(2f, 6f)
            );
            toolRb.AddForce(dropForce, ForceMode2D.Impulse);
            toolRb.AddTorque(-dropForce.x * 4, ForceMode2D.Impulse);

            if (assignedPlatform != null && assignedPlatform.showDebugInfo)
            {
                Debug.Log($"{gameObject.name} dropped their tool");
            }
        }
    }


    #endregion

}
