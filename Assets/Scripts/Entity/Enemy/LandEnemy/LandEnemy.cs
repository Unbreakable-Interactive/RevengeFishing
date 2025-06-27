using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandEnemy : Enemy
{
    [Header("Land Enemy Configuration")]
    public LandEnemyConfig landEnemyConfig = new LandEnemyConfig();

    #region Land Enemy Variables

    // Movement states for land enemies
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
    [SerializeField] protected float runningSpeed;
    [SerializeField] protected float edgeBuffer; // Distance from platform edge to change direction

    // Fishing tool equip system
    public bool fishingToolEquipped = false; // Is tool currently out?

    // For dropping tools when defeated
    [SerializeField] protected GameObject toolDropPrefab; // Tool to spawn when defeated

    // Platform bounds
    public float platformLeftEdge;
    public float platformRightEdge;
    public bool platformBoundsCalculated;

    [SerializeField] protected float maxUpwardVelocity; // For when they swim upward

    [SerializeField] protected float weight; // How much the enemy sinks in water; varies between 60 and 100 kg

    [Header("Escape System")]
    [SerializeField] protected bool hasStartedFloating = false; // Track if enemy is floating upward

    protected HookSpawner hookSpawner;
    protected bool hasThrownHook;
    [SerializeField] protected float hookTimer;
    [SerializeField] protected float hookDuration;

    protected FishingProjectile subscribedHook;

    #endregion

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

    public virtual void OnPlatformAssigned(Platform platform)
    {
        if (assignedPlatform != null && !platformBoundsCalculated)
        {
            CalculatePlatformBounds();
        }

        Debug.Log($"{gameObject.name} - Platform assigned: {platform.name}");

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

        Debug.Log($"{gameObject.name} platform assignment cleared");
    }
    #endregion

    #region Basic Actions
    // Start is called before the first frame update
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

        nextActionTime = Time.time + UnityEngine.Random.Range(0.5f, 2f);
        _landMovementState = LandMovementState.Idle;

        hookSpawner = GetComponent<HookSpawner>();
        if (hookSpawner == null)
        {
            hookSpawner = gameObject.AddComponent<HookSpawner>();
        }
        hookSpawner.Initialize();

        SetMovementMode(isAboveWater);

        Debug.Log($"{gameObject.name} - Enemy initialized with power level {_powerLevel}");
    }
    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
    }

    public override void SetMovementMode(bool aboveWater)
    {
        base.SetMovementMode(aboveWater);
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
            //Enemy is automatically defeated when falling into water
            if (_state == EnemyState.Alive)
            {
                TriggerDefeat();
            }

            // When defeated enemy enters water, mark as floating
            hasStartedFloating = true;

            Debug.Log($"{gameObject.name} enemy switched to UNDERWATER mode");
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

    protected override void InterruptAllActions()
    {
        // Stop any movement
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

        // If this enemy has fishing tools, clean them up
        CleanupFishingTools();

        // Drop any tools they were carrying
        DropTool();

    }

    /// <summary>
    /// Clean hookSpawner and reset variables like finishingToolEquipped, hasThrownHook, hookTimer
    /// </summary>
    protected virtual void CleanupFishingTools()
    {
        // This will be overridden in FishermanScript for specific cleanup
        if (fishingToolEquipped)
        {
            fishingToolEquipped = false;
            Debug.Log($"{gameObject.name} - Fishing tool cleaned up due to defeat");
        }
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
        if (hookSpawner.CurrentHook != null)
        {
            hookSpawner.CurrentHook.isBeingHeld = isBeingHeld;
            Debug.Log($"Fisherman: Hook is being held: {isBeingHeld}");
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        assignedPlatform = null;
        // Cleanup hook subscription
        CleanupHookSubscription();

    }
    #endregion

    #region Land Movement Logic
    public virtual void LandMovement()
    {
        // Use virtual AI decision method
        if (Time.time >= nextActionTime)
        {
            MakeAIDecision(); // Virtual method that derived classes can override
        }

        ExecuteLandMovementBehaviour();

        if (platformBoundsCalculated)
        {
            CheckPlatformBounds();
        }
    }

    protected virtual void MakeAIDecision()
    {
        // Base enemy AI: simple random movement
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

    protected virtual void CheckPlatformBounds()
    {
        float currentX = transform.position.x;

        // If we're near the left edge and moving left, stop or turn around
        if (currentX <= platformLeftEdge && (_landMovementState == LandMovementState.WalkLeft || _landMovementState == LandMovementState.RunLeft))
        {
            // IMMEDIATE STOP - prevent further movement
            rb.velocity = new Vector2(0, rb.velocity.y);
            _landMovementState = LandMovementState.Idle;

            // Choose a new action that doesn't involve going left
            ChooseRandomActionExcluding(LandMovementState.WalkLeft, LandMovementState.RunLeft);
            ScheduleNextAction();
        }
        // If we're near the right edge and moving right, stop or turn around
        else if (currentX >= platformRightEdge && (_landMovementState == LandMovementState.WalkRight || _landMovementState == LandMovementState.RunRight))
        {
            // IMMEDIATE STOP - prevent further movement
            rb.velocity = new Vector2(0, rb.velocity.y);
            _landMovementState = LandMovementState.Idle;

            // Choose a new action that doesn't involve going right
            ChooseRandomActionExcluding(LandMovementState.WalkRight, LandMovementState.RunRight);
            ScheduleNextAction();
        }
    }

    // Simplified LandEnemyScript.cs - remove ALL fisherman-specific code
    protected virtual void ChooseRandomLandAction()
    {
        if (fishingToolEquipped) return;

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
    }

    //actually executes the action chosen by ChooseRandomLandAction
    protected virtual void ExecuteLandMovementBehaviour()
    {
        if (fishingToolEquipped) return;

        Vector2 movement = Vector2.zero;

        // Simple movement - no fishing tool checks here!
        switch (_landMovementState)
        {
            case LandMovementState.Idle:
                Debug.Log("Idle state, no movement");
                break;
            case LandMovementState.WalkLeft:
                Debug.Log("Walking left");
                movement = Vector2.left * walkingSpeed;
                break;
            case LandMovementState.WalkRight:
                Debug.Log("Walking right");
                movement = Vector2.right * walkingSpeed;
                break;
            case LandMovementState.RunLeft:
                Debug.Log("Running left");
                movement = Vector2.left * runningSpeed;
                break;
            case LandMovementState.RunRight:
                Debug.Log("Running right");
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
            GameObject droppedToolHandler = Instantiate(toolDropPrefab, transform.position, transform.rotation);

            if (assignedPlatform != null && assignedPlatform.showDebugInfo)
            {
                Debug.Log($"{gameObject.name} dropped their tool");
            }
        }
    }


    #endregion
}
