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

    // Movement states for land enemies
    public enum LandMovementState
    {
        Idle,
        WalkLeft,
        WalkRight,
        RunLeft,
        RunRight
    }

    protected float _powerLevel;
    protected float _fatigue;
    protected float _maxFatigue;
    protected Tier _tier;
    protected EnemyState _state;
    protected EnemyType _type;
    protected LandMovementState _landMovementState;

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

    #region Land Variables

    protected float walkingSpeed;
    protected float runningSpeed;
    protected float edgeBuffer; // Distance from platform edge to change direction
    // assigned platform was set previously in Platform Assignment region

    // Fishing tool equip system
    protected bool hasFishingTool = false; // Can this enemy use a tool?
    public bool fishingToolEquipped = false; // Is tool currently out?

    // For dropping tools when defeated
    [SerializeField] protected GameObject toolDropPrefab; // Tool to spawn when defeated

    // AI state for land movement
    protected float minActionTime; //Minimum seconds enemy will do an action, like walk, idle, or run
    protected float maxActionTime; //Maximum seconds enemy will do an action, like walk, idle, or run

    public LandMovementState currentMovementState;
    protected float nextActionTime;
    protected bool isGrounded;

    // Platform bounds
    protected float platformLeftEdge;
    protected float platformRightEdge;
    protected bool platformBoundsCalculated;


    protected float floatingForce;
    protected float maxUpwardVelocity; // For when they swim upward

    protected float weight; // How much the enemy sinks in water; varies between 60 and 100 kg
    
    #endregion

    #region Water Variables

    protected float swimForce;
    protected float minSwimSpeed;
    protected float maxSwimSpeed;

    #endregion

    #region Base Behaviours

    protected override void Start()
    {
        // Set entity type for water detection
        entityType = EntityType.Enemy;

        // Call base initialization (handles Rigidbody2D and water detection)
        base.Start();

        // Enemy-specific initialization
        Initialize(100f);
    }

    protected override void Update()
    {
        // Call base Update for water detection logic
        base.Update();
    }


    // How enemy behaves when interacts with player
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
    }

    // Override SetMovementMode to add enemy-specific behavior
    public override void SetMovementMode(bool aboveWater)
    {
        base.SetMovementMode(aboveWater); // Call base implementation

        // Enemy-specific mode changes
        if (aboveWater)
        {
            Debug.Log($"{gameObject.name} enemy switched to AIRBORNE mode");
        }
        else
        {
            Debug.Log($"{gameObject.name} enemy switched to UNDERWATER mode");
        }
    }

    public virtual void Initialize(float powerLevel)
    {
        _powerLevel = powerLevel;
        
        _fatigue = 0;
        _maxFatigue = _powerLevel;

        _state = EnemyState.Alive;

        CalculateTier();

        walkingSpeed = 2f;
        runningSpeed = 4f;
        edgeBuffer = .5f; // Distance from platform edge to change direction
        // assigned platform was set previously in Platform Assignment region

        // AI state for land movement
        minActionTime = 1f; //Minimum seconds enemy will do an action, like walk, idle, or run
        maxActionTime = 4f; //Maximum seconds enemy will do an action, like walk, idle, or run

        currentMovementState = LandMovementState.Idle;
        isGrounded = false;

        platformBoundsCalculated = false;

        // weight must be a random value between x and y. Set to default 6f for now
        weight = 6f; // How much the enemy sinks in water; varies between 60 and 100 kg

        // Set initial movement mode
        SetMovementMode(isAboveWater);
    }

    private void CalculateTier()
    {
        // if (_powerLevel is > 100 and < 500)
        // {
        //     _tier = Tier.Tier1;
        // }

        _tier = Tier.Tier1;
    }

    protected float TakeFatigue(float playerPowerLevel)
    {
        // 5% more fatigue
        _fatigue += playerPowerLevel * .05f;
        return Mathf.Clamp(_fatigue, 0, _maxFatigue);
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

    public virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (_state == EnemyState.Defeated && other.CompareTag("PlayerCollider"))
        {
            ChangeState_Eaten();
        }
    }

    public virtual void EnemyDie() { }

    // In your enemy defeat/health system (wherever you handle enemy death)
    public void OnEnemyDefeated()
    {
        // Drop tool if enemy has one
        DropTool();

        // Handle other defeat logic (destroy enemy, play effects, etc.)
        // ...
    }


    #endregion

    #region Land Movement Logic
    public virtual void LandWalk()
    {
        if (_type != EnemyType.Land) return;

        //Check if we have an assigned platform
        CheckGroundedStatus();

        // Calculate platform bounds once we have an assigned platform
        if (assignedPlatform != null && !platformBoundsCalculated)
        {
            CalculatePlatformBounds();
        }

        // Execute current movement behavior
        ExecuteLandMovementBehaviour();

        // Check if it's time to change behavior
        if (Time.time >= nextActionTime)
        {
            ChooseRandomLandAction();
            ScheduleNextAction();
        }

        // Safety check - don't fall off platform
        if (platformBoundsCalculated)
        {
            CheckPlatformBounds();
        }
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
        if (currentX <= platformLeftEdge && (currentMovementState == LandMovementState.WalkLeft || currentMovementState == LandMovementState.RunLeft))
        {
            // Choose a new action that doesn't involve going left
            ChooseRandomActionExcluding(LandMovementState.WalkLeft, LandMovementState.RunLeft);
            ScheduleNextAction();
        }
        // If we're near the right edge and moving right, stop or turn around
        else if (currentX >= platformRightEdge && (currentMovementState == LandMovementState.WalkRight || currentMovementState == LandMovementState.RunRight))
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

        if (randomValue < 0.6f)
            currentMovementState = LandMovementState.Idle;
        else if (randomValue < 0.9f)
            currentMovementState = (UnityEngine.Random.value < 0.5f) ?
                LandMovementState.WalkLeft : LandMovementState.WalkRight;
        else
            currentMovementState = (UnityEngine.Random.value < 0.5f) ?
                LandMovementState.RunLeft : LandMovementState.RunRight;
    }

    protected virtual void ExecuteLandMovementBehaviour()
    {
        if (!isGrounded || assignedPlatform == null) return;

        Vector2 movement = Vector2.zero;

        // Simple movement - no fishing tool checks here!
        switch (currentMovementState)
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
            currentMovementState = validStates[UnityEngine.Random.Range(0, validStates.Count)];
        }
        else
        {
            currentMovementState = LandMovementState.Idle; // Fallback
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
        if (!hasFishingTool) return false;
        if (fishingToolEquipped) return false;
        if (currentMovementState != LandMovementState.Idle) return false;

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

            if (assignedPlatform != null && assignedPlatform.showDebugInfo)
            {
                Debug.Log($"{gameObject.name} dropped their tool");
            }
        }
    }


    #endregion

}
