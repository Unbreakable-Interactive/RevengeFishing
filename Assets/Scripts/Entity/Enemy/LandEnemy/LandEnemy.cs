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

    [Header("Pull Mechanic")]
    [SerializeField] protected bool isPullingPlayer = false;

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

    protected override void OnFirstFatigueReceived()
    {
        base.OnFirstFatigueReceived();

        // Start the pull mechanic if we have an active hook
        if (CanPullPlayer() && hookSpawner.HasActiveHook())
        {
            StartCoroutine(ContinuousPullMechanic());
        }
    }

    private IEnumerator ContinuousPullMechanic()
    {
        Debug.Log($"{gameObject.name} starting continuous pull mechanic!");

        // Continue pulling until enemy is defeated
        while (_state == EnemyState.Alive && CanPullPlayer() && hookSpawner.HasActiveHook())
        {
            // Wait for random interval between 0.5 and 1 second
            float waitTime = UnityEngine.Random.Range(0.5f, 1f);
            yield return new WaitForSeconds(waitTime);

            // Check again if we should still be pulling
            if (_state == EnemyState.Alive && CanPullPlayer() && hookSpawner.HasActiveHook())
            {
                yield return StartCoroutine(PerformSinglePull());
            }
        }

        Debug.Log($"{gameObject.name} stopped continuous pulling - enemy defeated or hook lost");
    }

    private IEnumerator PerformSinglePull()
    {
        if (isPullingPlayer) yield break; // Prevent overlapping pulls

        isPullingPlayer = true;

        // Get the hook spawn point
        Vector3 hookSpawnPoint = hookSpawner.spawnPoint.position;

        if (player == null)
        {
            Debug.LogError("Player not found for pull mechanic!");
            isPullingPlayer = false;
            yield break;
        }

        Debug.Log($"{gameObject.name} giving player a firm fishing rod pull!");

        // SHORTEN THE FISHING LINE when enemy pulls
        float currentLineLength = hookSpawner.GetLineLength();
        float lineReduction = UnityEngine.Random.Range(0.8f, 1.2f); // Reduce line by 0.8-1.2 units
        float newLineLength = Mathf.Max(currentLineLength - lineReduction, 2f); // Don't go below 2 units

        hookSpawner.SetLineLength(newLineLength);
        Debug.Log($"Fisherman shortened line from {currentLineLength:F1} to {newLineLength:F1}");

        // Calculate a partial pull toward the hook spawn (like reeling in a fish)
        Vector3 playerPosition = player.transform.position;
        Vector3 pullDirection = (hookSpawnPoint - playerPosition).normalized;

        // Pull them only partway - adjust this value to control pull strength
        float pullDistance = 1f; // How far to pull them (in Unity units)
        Vector3 targetPosition = playerPosition + (pullDirection * pullDistance);

        // Make sure we don't pull them past the hook spawn point
        float distanceToHook = Vector3.Distance(playerPosition, hookSpawnPoint);
        if (pullDistance >= distanceToHook)
        {
            // If they're very close, just pull them 30% of the remaining distance
            targetPosition = Vector3.Lerp(playerPosition, hookSpawnPoint, 0.3f);
        }

        float elapsedTime = 0f;
        float snapBackDuration = 0.3f; // Quick, snappy pull

        // Apply brief movement toward the hook (like a fish being reeled in)
        while (elapsedTime < snapBackDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / snapBackDuration;

            // Use sharp easing for snappy fishing rod feel
            progress = Mathf.SmoothStep(0f, 1f, progress);

            // Lerp player toward the partial target position
            Vector3 newPosition = Vector3.Lerp(playerPosition, targetPosition, progress);
            player.transform.position = newPosition;

            // Add some force for physics effect
            player.GetComponent<Rigidbody2D>().AddForce(pullDirection * pullForce, ForceMode2D.Impulse);

            yield return null;
        }

        // Brief constraint to prevent immediate escape
        player.SetPositionConstraint(player.transform.position, 0.8f);
        yield return new WaitForSeconds(0.2f);

        // Release the player
        player.RemovePositionConstraint();
        isPullingPlayer = false;

        Debug.Log($"{gameObject.name} finished single fishing rod pull");
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

        // Stop any active pulling when defeated
        if (isPullingPlayer)
        {
            StopAllCoroutines();
            isPullingPlayer = false;

            // Release player constraint if active
            Player player = FindObjectOfType<Player>();
            if (player != null)
            {
                player.RemovePositionConstraint();
            }
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
