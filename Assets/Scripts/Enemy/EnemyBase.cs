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

    protected Tier _tier;
    protected EnemyState _state;
    protected EnemyType _type;
    protected LandMovementState _landMovementState;

    [Header("Escape System")]
    public bool canEscape = true;
    private bool hasStartedFloating = false;

    #region Platform Assignment
    [Header("Platform Assignment")]
    public Platform assignedPlatform;

    public virtual void SetAssignedPlatform(Platform platform)
    {
        assignedPlatform = platform;
    }

    public Platform GetAssignedPlatform()
    {
        return assignedPlatform;
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

    #region Land Variables

    protected float walkingSpeed;
    protected float runningSpeed;
    protected float edgeBuffer;

    protected bool hasFishingTool = false;
    public bool fishingToolEquipped = false;

    [SerializeField] protected GameObject toolDropPrefab;

    protected float minActionTime;
    protected float maxActionTime;

    public LandMovementState currentMovementState;
    protected float nextActionTime;

    [Header("Platform Bounds")]
    public float platformLeftEdge;
    public float platformRightEdge;
    public bool platformBoundsCalculated;
    public bool isGrounded;

    protected float floatingForce;
    protected float maxUpwardVelocity;

    protected float weight;
    
    #endregion

    #region Water Variables

    protected float swimForce;
    protected float minSwimSpeed;
    protected float maxSwimSpeed;

    #endregion

    #region Base Behaviours

    public override void Initialize()
    {
        entityType = EntityType.Enemy;
        base.Initialize();
        EnemySetup(100);
    }

    protected override void Update()
    {
        base.Update();
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

        Debug.Log($"{gameObject.name} SetMovementMode called: aboveWater={aboveWater}, state={_state}, hasStartedFloating={hasStartedFloating}, canEscape={canEscape}");

        if (aboveWater)
        {
            if (_state == EnemyState.Defeated && hasStartedFloating && canEscape)
            {
                Debug.Log($"{gameObject.name} - ESCAPE CONDITIONS MET! Triggering escape.");

                TriggerEscape();
                return;
            }
            else if (_state == EnemyState.Defeated)
            {
                Debug.Log($"{gameObject.name} - Defeated but escape conditions not met: hasStartedFloating={hasStartedFloating}, canEscape={canEscape}");
            }

            hasStartedFloating = false;
            Debug.Log($"{gameObject.name} enemy switched to AIRBORNE mode");
        }
        else
        {
            if (_state == EnemyState.Defeated)
            {
                hasStartedFloating = true;
            }

            rb.gravityScale = -0.2f;
            rb.drag = 1f;

            Debug.Log($"{gameObject.name} enemy switched to UNDERWATER mode");
        }
    }

    public void EnemySetup(int powerLevel)
    {
        _powerLevel = powerLevel;
        
        _fatigue = 0;
        _maxFatigue = _powerLevel;

        _state = EnemyState.Alive;

        CalculateTier();

        walkingSpeed = 2f;
        runningSpeed = 4f;
        edgeBuffer = .5f;

        minActionTime = 1f;
        maxActionTime = 4f;

        currentMovementState = LandMovementState.Idle;
        isGrounded = false;

        platformBoundsCalculated = false;

        weight = 6f;

        SetMovementMode(isAboveWater);
    }

    private void CalculateTier()
    {
        _tier = Tier.Tier1;
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

    protected virtual void TriggerEscape()
    {
        Debug.Log($"{gameObject.name} has ESCAPED! The player can no longer catch this enemy.");

        OnEnemyEscaped();

        GameObject objectToDestroy = transform.parent != null ? transform.parent.gameObject : gameObject;
        Destroy(objectToDestroy);
    }

    protected virtual void OnEnemyEscaped()
    {
    }

    
    protected virtual void InterruptAllActions()
    {
        currentMovementState = LandMovementState.Idle;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }

        // FIXED: Don't permanently disable AI - just delay it briefly
        nextActionTime = Time.time + 1f; // 1 second delay instead of float.MaxValue

        Debug.Log($"{gameObject.name} - Actions interrupted, resuming AI in 1 second");
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

        CleanupFishingTools();
        DropTool();
    }

    protected virtual void CleanupFishingTools()
    {
        if (fishingToolEquipped)
        {
            fishingToolEquipped = false;
            Debug.Log($"{gameObject.name} - Fishing tool cleaned up due to defeat");
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

    public virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (_state == EnemyState.Defeated && other.CompareTag("PlayerCollider"))
        {
            ChangeState_Eaten();
        }
    }

    public virtual void EnemyDie() { }

    public void OnEnemyDefeated()
    {
        DropTool();
    }

    #endregion

    #region Land Movement Logic
    public virtual void LandWalk()
    {
        if (_type != EnemyType.Land) return;

        CheckGroundedStatus();
        
        if (assignedPlatform != null && !platformBoundsCalculated)
        {
            CalculatePlatformBounds();
        }

        ExecuteLandMovementBehaviour();

        if (Time.time >= nextActionTime)
        {
            MakeAIDecision();
        }

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

        if (currentX <= platformLeftEdge && (currentMovementState == LandMovementState.WalkLeft || currentMovementState == LandMovementState.RunLeft))
        {
            ChooseRandomActionExcluding(LandMovementState.WalkLeft, LandMovementState.RunLeft);
            ScheduleNextAction();
        }
        else if (currentX >= platformRightEdge && (currentMovementState == LandMovementState.WalkRight || currentMovementState == LandMovementState.RunRight))
        {
            ChooseRandomActionExcluding(LandMovementState.WalkRight, LandMovementState.RunRight);
            ScheduleNextAction();
        }
    }

    protected virtual void ChooseRandomLandAction()
    {
        float randomValue = UnityEngine.Random.value;

        Debug.Log($"Choosing random land action for {gameObject.name}. Random value = {randomValue}");
        if (randomValue < 0.6f)
        {
            Debug.Log($"{gameObject.name} is idle");
            currentMovementState = LandMovementState.Idle;
        }
        else if (randomValue < 0.9f)
        {
            Debug.Log($"{gameObject.name} is walking");
            currentMovementState = (UnityEngine.Random.value < 0.5f) ? LandMovementState.WalkLeft : LandMovementState.WalkRight;
        }
        else
        {
            Debug.Log($"{gameObject.name} is running");
            currentMovementState = (UnityEngine.Random.value < 0.5f) ? LandMovementState.RunLeft : LandMovementState.RunRight;
        }

        ExecuteLandMovementBehaviour();
    }

    protected virtual void ExecuteLandMovementBehaviour()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                Debug.LogError($"{gameObject.name} - NO RIGIDBODY2D FOUND! Adding one now.");
                rb = gameObject.AddComponent<Rigidbody2D>();
            }
        }

        Vector2 movement = Vector2.zero;

        switch (currentMovementState)
        {
            case LandMovementState.Idle:
                Debug.Log($"{gameObject.name} - IDLE: No movement");
                break;
            case LandMovementState.WalkLeft:
                movement = Vector2.left * walkingSpeed;
                Debug.Log($"{gameObject.name} - WALKING LEFT at speed {walkingSpeed}");
                break;
            case LandMovementState.WalkRight:
                movement = Vector2.right * walkingSpeed;
                Debug.Log($"{gameObject.name} - WALKING RIGHT at speed {walkingSpeed}");
                break;
            case LandMovementState.RunLeft:
                movement = Vector2.left * runningSpeed;
                Debug.Log($"{gameObject.name} - RUNNING LEFT at speed {runningSpeed}");
                break;
            case LandMovementState.RunRight:
                movement = Vector2.right * runningSpeed;
                Debug.Log($"{gameObject.name} - RUNNING RIGHT at speed {runningSpeed}");
                break;
        }

        if (rb != null)
        {
            rb.velocity = new Vector2(movement.x, rb.velocity.y);
            Debug.Log($"{gameObject.name} - Applied velocity: {rb.velocity}");
        }
        else
        {
            Debug.LogError($"{gameObject.name} - STILL NO RIGIDBODY2D AFTER ATTEMPTS!");
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
            currentMovementState = validStates[UnityEngine.Random.Range(0, validStates.Count)];
        }
        else
        {
            currentMovementState = LandMovementState.Idle;
        }
    }

    protected virtual void ScheduleNextAction()
    {
        float actionDuration = UnityEngine.Random.Range(minActionTime, maxActionTime);
        nextActionTime = Time.time + actionDuration;
    }

    #endregion

    #region Fishing Tool System

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
