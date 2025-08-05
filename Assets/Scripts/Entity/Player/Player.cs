using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine;
using RevengeFishing.Hunger;
using static Enemy;

public class Player : Entity
{
    public enum Phase
    {
        Infant,
        Juvenile,
        Adult,
        Beast,
        Monster
    }

    public enum Status
    {
        Alive,
        Fished,
        Starved,
        Slain
    }

    [Header("Player Settings")]
    [SerializeField] private PlayerConfig playerConfig;

    [Header("Camera Reference")]
    [SerializeField] private Camera mainCamera;
    private Vector2 lastMousePosition = Vector2.zero;
    private Vector2 mousePosition = Vector2.zero;
    private Vector3 cachedMouseScreenPosition = Vector3.zero;
    private Vector3 cachedMouseWorldPosition = Vector3.zero;
    private Vector3 cachedPlayerPosition = Vector3.zero;


    private Quaternion targetRotation;
    private bool isRotatingToTarget = false;
    private bool shouldApplyForceAfterRotation = false;
    private bool hasAppliedBoost = false; // Track if boost was already applied

    [SerializeField] protected HungerHandler hungerHandler;

    public HungerHandler HungerHandler => hungerHandler;

    [SerializeField] protected Status status;
    [SerializeField] protected Phase currentPhase;
    [SerializeField] protected int nextPowerLevel; //minimum power level to reach next phase

    [Header("Rotation Settings")]
    [SerializeField] protected float rotationSpeed = 10f;
    [SerializeField] protected float rotationThreshold = 10f; // Degrees - How close to target before considering "complete"
    [SerializeField] protected float boostThreshold = 10f; // Degrees - When to apply boost (higher = earlier boost)

    [SerializeField] protected float underwaterRotationSpeed = 10f;

    [SerializeField] protected bool autoRotateInAir = true;
    [SerializeField] protected float airRotationSpeed = 8f;
    [SerializeField] protected float minSpeedForRotation = 0.3f;

    [Header("Variable Gravity Settings")]
    [SerializeField] protected float airGravityAscending = 1.5f;    // Lighter gravity when moving upward
    [SerializeField] protected float airGravityDescending = 4f;     // Stronger gravity when falling down
    [SerializeField] protected float gravityTransitionSpeed = 2f;   // How quickly gravity changes
    [SerializeField] protected float velocityThreshold = 0.1f;      // Minimum velocity to determine direction

    private float currentGravityScale;          // Current gravity being applied

    [Header("Water Movement Settings")]
    [SerializeField] protected float forceAmount = 1f;
    [SerializeField] protected float maxSpeed = 5f;
    [SerializeField] protected float naturalDrag = 0.5f;
    [SerializeField] protected float rotationDrag = 1f; // Extra drag applied during rotation
    [SerializeField] protected float constantAccel = 0.2f; 
    [SerializeField] protected float minForwardVelocity = 1f; 
    [SerializeField] protected float sidewaysDrag = 1f; // higher = less sideways drift

    [Header("Steering Settings")]
    [SerializeField] protected float steeringForce = 5f;
    [SerializeField] protected float steeringDamping = 0.98f; // Reduces velocity over time when steering

    [Header("External Constraints")]
    private bool isConstrainedByExternalForce = false;
    private Vector3 constraintCenter;
    private float constraintRadius;
    private System.Action<Vector3> onConstraintViolation;

    [Header("Fishing Hook Interaction")]
    public List<FishingProjectile> activeBitingHooks = new List<FishingProjectile>();

    [Header("Line Extension")]
    [SerializeField] private float lineExtensionAmount = 1f; // How much to extend per pull
    [SerializeField] private float maxLineExtension = 12f; // Maximum line length allowed
    [SerializeField] private float lineExtensionSpeedPenalty = 0.6f; // Speed multiplier when extending (0.6 = 40% slower)
    // State tracking for continuous slowdown
    private bool isAtMaxHookDistance = false;
    private float originalMaxSpeed;

    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer playerSpriteRenderer;

    public Animator animator;

    [Header("Debug")]
    public bool enableDebugLogs = false;

    public static Player Instance { get; private set; }
    
    [Header("Components to share")] 
    [SerializeField] private Collider2D colliderToShare;
    
    public Collider2D ColliderToShare => colliderToShare;
    
    protected override void Awake()
    {
        base.Awake();
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple Player instances found! Destroying duplicate.");
            Destroy(gameObject);
        }

        Initialize();
    }

    public override void Initialize ()
    {
        entityType = EntityType.Player; // Set entity type to Player

        base.Initialize(); // Call base Initialize to set up Rigidbody2D and movement mode

        // Use cached camera reference or fallback to Camera.main for singleton pattern
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("No main camera found! Please assign camera reference in Player component.");
            }
        }

        animator = GetComponent<Animator>();

        currentPhase = Phase.Infant; // Start in the Infant phase
        //change the next line once an actual fix is found for player threshold not assigning properly
        nextPowerLevel = playerConfig.phaseThresholds.juvenile; // Set next phase threshold

        hungerHandler = new HungerHandler(_powerLevel, entityFatigue, 0);

        rb.drag = naturalDrag;
        targetRotation = transform.rotation; //set target rotation to Player's current rotation
        currentGravityScale = underwaterGravityScale;

        originalMaxSpeed = maxSpeed;

        playerSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    protected override void Update()
    {
        base.Update(); // Call base Update to handle movement mode
        
        //playerSpriteRenderer.flipY = (transform.rotation.z > 0.7f || transform.rotation.z < -0.7f);
        if (transform.rotation.z > 0.7f || transform.rotation.z < -0.7f)
        {
            transform.localScale = new Vector3(transform.localScale.x, -Mathf.Abs(transform.localScale.y), transform.localScale.z); // Flip sprite vertically
        }
        else
        {
            transform.localScale = new Vector3(transform.localScale.x, Mathf.Abs(transform.localScale.y), transform.localScale.z); // Flip sprite vertically
        }

        if (activeBitingHooks != null && activeBitingHooks.Count > 0) CheckMaxHookDistanceState();
        if (activeBitingHooks != null && activeBitingHooks.Count <= 0) maxSpeed = originalMaxSpeed;

        if (CanMature()) Mature();
    }

    protected override void UnderwaterBehavior()
    {
        HandleMouseInput();
        UpdateRotation();
        HandleSteering();
        ApplyConstantAccel();
        ReduceSidewaysVelocity();
        ClampVelocity();
    }

    protected override void AirborneBehavior()
    {
        AirGravity();
        HandleAirborneRotation();
    }

    public override void SetMovementMode(bool aboveWater)
    {
        base.SetMovementMode(aboveWater);

        // Player-specific mode changes
        if (aboveWater)
        {
            currentGravityScale = airGravityScale;
            originalMaxSpeed = airMaxSpeed;
            rotationSpeed = airRotationSpeed;
            DebugLog("Player switched to AIRBORNE mode");
        }
        else
        {
            originalMaxSpeed = underwaterMaxSpeed;
            rotationSpeed = underwaterRotationSpeed;
            DebugLog("Player switched to UNDERWATER mode");
        }

        maxSpeed = isAtMaxHookDistance ? originalMaxSpeed * lineExtensionSpeedPenalty : originalMaxSpeed;
    }


    protected bool CanMature()
    {
        return (_powerLevel >= nextPowerLevel);
    }

    protected void Mature()
    {
        switch (currentPhase)
        {
            case Phase.Infant:
                currentPhase = Phase.Juvenile;
                animator?.SetBool("isInfant", false);
                animator?.SetBool("isJuvie", true);
                nextPowerLevel = playerConfig.phaseThresholds.adult;
                Debug.Log("Player matured to Juvenile phase!");
                break;
            case Phase.Juvenile:
                currentPhase = Phase.Adult;
                nextPowerLevel = playerConfig.phaseThresholds.beast;
                Debug.Log("Player matured to Adult phase!");
                break;
            case Phase.Adult:
                currentPhase = Phase.Beast;
                nextPowerLevel = playerConfig.phaseThresholds.monster;
                Debug.Log("Player matured to Beast phase!");
                break;
            case Phase.Beast:
                currentPhase = Phase.Monster;
                nextPowerLevel = playerConfig.phaseThresholds.victory; //Change this to the desired value for winning the game
                Debug.Log("Player matured to Monster phase!");
                break;
            case Phase.Monster:
                Debug.Log("Victory!");
                //transition to victory scene
                SceneManager.LoadScene("Victory");
                break;
            default:
                break;
        }
    }

    #region Input Handling
    void HandleMouseInput()
    {
        mousePosition = GetMouseWorldPosition();

        if (Input.GetMouseButtonDown(0))
        {
            OnMouseClick(mousePosition);
        }

        if (Input.GetMouseButton(0))
        {
            OnMouseHold(mousePosition);
            lastMousePosition = GetMouseWorldPosition();
        }

        if (!Input.GetMouseButton(0))
        {
            WhileMouseUnheld(lastMousePosition);
        }
    }

    //Triggers frame mouse is clicked
    void OnMouseClick(Vector2 mousePosition)
    {
        rb.drag = naturalDrag / 20; // Reduce drag while mouse is held to allow smoother movement

        SetTargetRotation(mousePosition);
        shouldApplyForceAfterRotation = true; // Flag to apply force when rotation completes

        // NEW: Check for tug-of-war pulling
        TryTugOfWarPull();

        DebugLog("Mouse clicked - rotating to point at: " + mousePosition);
        hasAppliedBoost = false;
    }

    //Triggers as long as mouse is being clicked
    void OnMouseHold(Vector2 mousePosition)
    {
        // If we're not currently doing initial rotation and we're moving, this will trigger steering
        if (!shouldApplyForceAfterRotation && !isRotatingToTarget)
        {
            DebugLog("Mouse held - steering");
        }
        else
        {
            // Still doing initial rotation/launch
            SetTargetRotation(mousePosition);
            DebugLog("Mouse held - continuously rotating to: " + mousePosition);
        }

    }

    //Triggers as long as the mouse remains unclicked
    void WhileMouseUnheld(Vector2 lastMousePosition)
    {
        //Let object coast if mouse is released
        if (rb.drag < naturalDrag) rb.drag += naturalDrag / 60;
        if (rb.drag > naturalDrag) rb.drag = naturalDrag; // Clamp to natural drag
    }
    #endregion

    #region Reverse Fishing

    public int GetFatigue() => entityFatigue.fatigue;

    public void TriggerBite()
    {
        animator?.SetTrigger("isBiting");
    }

    public void TakeFishingFatigue(float fatigueDamage)
    {
        // 10% enemy's fatigue
        entityFatigue.fatigue += (int)(fatigueDamage);

        // Check if enemy should be defeated
        if (entityFatigue.fatigue >= entityFatigue.maxFatigue)
        {
            PlayerDie(Status.Fished);
        }

    }

    // Method for hooks to register when they bite
    public void AddBitingHook(FishingProjectile hook)
    {
        if (!activeBitingHooks.Contains(hook))
        {
            activeBitingHooks.Add(hook);
            GetComponentInChildren<MouthMagnet>().RemoveEntity(hook); // Hook is no longer being drawn to center of magnet
            TriggerBite(); // Trigger bite animation
            DebugLog($"Hook {hook.name} is now biting player. Total hooks: {activeBitingHooks.Count}");
        }
    }

    // Method for hooks to unregister when they release
    public void RemoveBitingHook(FishingProjectile hook)
    {
        if (activeBitingHooks.Contains(hook))
        {
            activeBitingHooks.Remove(hook);
            DebugLog($"Hook {hook.name} released player. Total hooks: {activeBitingHooks.Count}");
        }
    }

    // Efficient tug-of-war method using only active hooks
    private void TryTugOfWarPull()
    {
        cachedPlayerPosition = transform.position; // Cache it once

        // Check if player is constrained by any hooks
        if (activeBitingHooks.Count > 0)
        {
            // Only check hooks that are currently biting the player
            foreach (FishingProjectile hook in activeBitingHooks)
            {
                // Check if this hook is stretching (player is pulling against it)
                if (hook.isBeingHeld /*&& hook.IsLineStretching()*/)
                {
                    // Check if player is at max distance and trying to extend line
                    Vector3 distanceVector = cachedPlayerPosition - hook.spawnPoint.position;
                    float currentDistanceSqr = distanceVector.sqrMagnitude;
                    float hookMaxDistanceSqr = hook.maxDistance * hook.maxDistance;

                    // Player is pulling against this fishing line!
                    // Fisherman fisherman = hook.spawner?.GetComponent<Fisherman>();
                    Enemy enemy = hook.spawner?.GetComponent<Enemy>();
                    
                    // If player is at or near max distance, try extending the fishing line
                    if (currentDistanceSqr >= hookMaxDistanceSqr * (0.99f * 0.99f)) // At 99% or more of max distance
                    {
                        TryExtendFishingLine(hook);
                    }

                    // If player is at or near max distance, deal fatigue damage
                    if (currentDistanceSqr >= hookMaxDistanceSqr * (0.9f * 0.9f)) // 90% of max distance
                    {
                        // if (fisherman != null)
                        // {
                        //     fisherman.TakeFatigue(PowerLevel);
                        //     DebugLog($"Player pulls against {fisherman.name}'s fishing line - fisherman suffers fatigue!");
                        // }

                        if (enemy != null)
                        {
                            enemy.TakeFatigue(PowerLevel);
                            DebugLog($"Player pulls against {enemy.name}'s fishing line - enemy suffers fatigue!");
                        }
                    }

                }
            }
        }
    }

    private void TryExtendFishingLine(FishingProjectile hook)
    {
        cachedPlayerPosition = transform.position;
        if (hook.spawner == null) return;

        HookSpawner hookSpawner = hook.spawner;
        float currentLineLength = hookSpawner.GetLineLength();

        // Only extend if we haven't reached the maximum allowed length
        if (currentLineLength < maxLineExtension)
        {
            float newLineLength = Mathf.Min(currentLineLength + lineExtensionAmount, maxLineExtension);
            hookSpawner.SetLineLength(newLineLength);

            DebugLog($"Player extended fishing line from {currentLineLength:F1} to {newLineLength:F1}");

            // Optional: Add resistance force when extending
            Vector3 directionToHook = (hook.spawnPoint.position - cachedPlayerPosition).normalized;
            rb.AddForce(-directionToHook * 2f, ForceMode2D.Impulse);
        }
        else
        {
            DebugLog("Fishing line is already at maximum length!");
        }
    }

    private void CheckMaxHookDistanceState()
    {
        cachedPlayerPosition = transform.position;

        bool wasAtMaxDistance = isAtMaxHookDistance;
        isAtMaxHookDistance = false; // Reset state

        // Check if player is constrained by any hooks
        if (activeBitingHooks.Count > 0)
        {
            foreach (FishingProjectile hook in activeBitingHooks)
            {
                if (hook.isBeingHeld)
                {
                    // Check if player is at or beyond this hook's max distance
                    Vector3 distanceVector = cachedPlayerPosition - hook.spawnPoint.position;
                    float currentDistanceSqr = distanceVector.sqrMagnitude;
                    float hookMaxDistanceSqr = hook.maxDistance * hook.maxDistance;
                    float thresholdSqr = hookMaxDistanceSqr * (0.95f * 0.95f); // Square the 95% threshold too

                    if (currentDistanceSqr >= thresholdSqr)
                    {
                        isAtMaxHookDistance = true;
                        break; // Found one hook at max distance, that's enough
                    }
                }
            }
        }

        // Apply or remove speed penalty based on state change
        if (isAtMaxHookDistance != wasAtMaxDistance)
        {
            if (isAtMaxHookDistance)
            {
                // Apply slowdown
                maxSpeed = originalMaxSpeed * lineExtensionSpeedPenalty;
                DebugLog($"Player at max hook distance - speed reduced to {maxSpeed:F1}");
            }
            else
            {
                // Remove slowdown
                maxSpeed = originalMaxSpeed;
                DebugLog($"Player moved within hook range - speed restored to {maxSpeed:F1}");
            }
        }
    }

    public void SetPositionConstraint(Vector3 center, float radius, System.Action<Vector3> violationCallback = null)
    {
        isConstrainedByExternalForce = true;
        constraintCenter = center;
        constraintRadius = radius;
        onConstraintViolation = violationCallback;
        DebugLog($"Player constraint set: Center={center}, Radius={radius}");
    }

    public void RemovePositionConstraint()
    {
        isConstrainedByExternalForce = false;
        onConstraintViolation = null;
        DebugLog("Player constraint removed");
    }

    private void ApplyExternalConstraints()
    {
        cachedPlayerPosition = transform.position;

        if (!isConstrainedByExternalForce) return;

        float currentDistance = Vector3.Distance(cachedPlayerPosition, constraintCenter);

        if (currentDistance > constraintRadius)
        {
            // Constrain position to circle boundary
            Vector3 direction = (cachedPlayerPosition - constraintCenter).normalized;
            Vector3 constrainedPosition = constraintCenter + direction * constraintRadius;
            transform.position = constrainedPosition;

            // Apply rope-like physics to velocity
            Vector2 currentVelocity = rb.velocity;
            Vector2 radialDirection = direction;
            Vector2 tangentDirection = new Vector2(-radialDirection.y, radialDirection.x);

            // Remove radial velocity component (can't move away from center)
            float tangentVelocity = Vector2.Dot(currentVelocity, tangentDirection);
            rb.velocity = tangentDirection * tangentVelocity;

            // Notify the constraining object about the violation
            onConstraintViolation?.Invoke(constrainedPosition);

            DebugLog($"Player constrained to radius {constraintRadius} at position {constrainedPosition}");
        }
    }

    public void GainPowerFromEating(int enemyPowerLevel)
    {
        //adjust powerLevel.
        _powerLevel += Mathf.RoundToInt((float)enemyPowerLevel * 0.1f); // 10% of enemy's power
        // Update new max values to match new power level
        entityFatigue.maxFatigue = _powerLevel;

        hungerHandler.GainedPowerFromEating(enemyPowerLevel, _powerLevel);

        DebugLog($"Player gained {Mathf.RoundToInt((float)enemyPowerLevel * 0.2f)} power from eating enemy! New power level: {_powerLevel}");
    }

    public void PlayerDie(Status deathType)
    {
        StartCoroutine(HandlePlayerDeath(deathType));
    }

    private System.Collections.IEnumerator HandlePlayerDeath(Status deathType)
    {
        // Store the death type for the GameOver scene
        DeathManager.SetDeathType(deathType);

        // Optional: Add death animation or effects here
        Debug.Log($"Player died: {deathType}");

        // Optional: Brief pause before scene transition
        yield return new WaitForSeconds(0f);

        // Transition to GameOver scene
        SceneManager.LoadScene("GameOver");
    }

    #endregion

    #region Special Abilities

    protected void SpecialAbilityOne() //this will be the Backflip ability
    {

    }

    protected void SpecialAbilityTwo() //this will be the Big Bite ability
    {

    }

    #endregion

    #region Movement
    void SetTargetRotation(Vector2 mousePosition)
    {
        cachedPlayerPosition = transform.position;

        Vector2 direction = mousePosition - (Vector2)cachedPlayerPosition;

        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
            isRotatingToTarget = true;
        }
    }

    void UpdateRotation()
    {
        if (isRotatingToTarget)
        {
            ApplyRotationDeceleration(); // Start decelerating immediately
            ContinueRotationToTarget();
            CheckForBoostTiming(); // Check if it's time to boost
            CheckRotationCompletion();
        }
    }

    void HandleSteering()
    {
        // Only allow steering if:
        // 1. Object is moving
        // 2. Mouse is being held (for steering input)
        // 3. Not currently doing initial rotation
        if (Input.GetMouseButton(0) && !shouldApplyForceAfterRotation)
        {
            ApplySteering(GetMouseWorldPosition());
        }
    }

    void ApplyRotationDeceleration()
    {
        // Apply extra drag during rotation to simulate fish slowing down to turn
        rb.velocity *= (1f - rotationDrag * Time.deltaTime);
        DebugLog($"Applying rotation deceleration - Current speed: {rb.velocity.magnitude:F2}");
    }

    void ContinueRotationToTarget()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    void CheckForBoostTiming()
    {
        if (hasAppliedBoost) return; // Only boost once per rotation

        float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);

        // Apply boost when close to target (but not complete)
        if (angleDifference <= boostThreshold)
        {
            ApplyForceInDirection();
            hasAppliedBoost = true;
            DebugLog($"Applied boost at {angleDifference:F1} degrees from target!");
        }
    }

    void CheckRotationCompletion()
    {
        float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);

        if (angleDifference < rotationThreshold)
        {
            // Rotation is complete
            transform.rotation = targetRotation; // Snap to exact rotation
            isRotatingToTarget = false;

            // Apply force
            //ApplyForceInDirection();
            //shouldApplyForceAfterRotation = false;

            DebugLog("Rotation completed!");
        }
    }

    void ApplyForceInDirection()
    {
        // Get the direction the object is facing (right direction in local space)
        Vector2 forceDirection = transform.right;

        // Apply force in that direction
        rb.AddForce(forceDirection * forceAmount, ForceMode2D.Impulse);

        DebugLog("Applied force in direction: " + forceDirection);
    }

    void ApplySteering(Vector2 targetPosition)
    {
        cachedPlayerPosition = transform.position;

        Vector2 direction = targetPosition - (Vector2)cachedPlayerPosition;

        if (direction != Vector2.zero)
        {
            // Rotate toward the target
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Apply steering force in the direction we're facing
            Vector2 steeringDirection = transform.right;
            rb.AddForce(steeringDirection * steeringForce, ForceMode2D.Force);

            // Apply damping to prevent infinite acceleration
            rb.velocity *= steeringDamping;

            DebugLog("Steering toward: " + targetPosition);
        }
    }

    void ApplyConstantAccel()
    {
        // Apply small constant force in the direction the object is facing
        Vector2 forwardDirection = transform.right;
        // Calculate how fast we're moving in the forward direction
        float forwardVelocity = Vector2.Dot(rb.velocity, forwardDirection);

        // Only apply constant force if we're not moving forward fast enough
        if (forwardVelocity < minForwardVelocity)
        {
            rb.AddForce(forwardDirection * constantAccel, ForceMode2D.Force);
            DebugLog($"Applying thrust - Forward velocity: {forwardVelocity:F2}");
        }
    }

    void ReduceSidewaysVelocity()
    {
        if (rb.velocity.magnitude < 0.1f) return; // Skip if barely moving

        Vector2 forwardDirection = transform.right;
        Vector2 currentVelocity = rb.velocity;

        // Calculate forward and sideways components of velocity
        float forwardVelocity = Vector2.Dot(currentVelocity, forwardDirection);
        Vector2 forwardVelocityVector = forwardDirection * forwardVelocity;
        Vector2 sidewaysVelocityVector = currentVelocity - forwardVelocityVector;

        // Reduce sideways velocity over time
        float sidewaysReduction = sidewaysDrag * Time.deltaTime;
        sidewaysVelocityVector = Vector2.Lerp(sidewaysVelocityVector, Vector2.zero, sidewaysReduction);

        // Apply the corrected velocity
        rb.velocity = forwardVelocityVector + sidewaysVelocityVector;
    }

    void ClampVelocity()
    {
        // Hard clamp for absolute maximum speed
        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }
        ApplyExternalConstraints();
    }

    void AirGravity()
    {
        // Determine if fish is ascending or descending
        bool isAscending = rb.velocity.y > velocityThreshold;
        bool isDescending = rb.velocity.y < -velocityThreshold;

        // Determine target gravity based on vertical movement
        float targetGravityScale;

        if (isAscending)
        {
            // Fish is jumping up - apply lighter gravity
            targetGravityScale = airGravityAscending;
            DebugLog("Fish ascending - applying lighter gravity");
        }
        else if (isDescending)
        {
            // Fish is falling down - apply stronger gravity
            targetGravityScale = airGravityDescending;
            DebugLog("Fish descending - applying stronger gravity");
        }
        else
        {
            // Fish is at peak or nearly stationary - use default air gravity
            targetGravityScale = airGravityScale;
            DebugLog("Fish at peak - using default air gravity");
        }

        // Smoothly transition to target gravity for natural feel
        currentGravityScale = Mathf.Lerp(currentGravityScale, targetGravityScale, gravityTransitionSpeed * Time.deltaTime);
        rb.gravityScale = currentGravityScale;

        DebugLog($"Current gravity scale: {currentGravityScale:F2}, Y velocity: {rb.velocity.y:F2}");

    }

    void HandleAirborneRotation()
    {
        // Auto-rotate to face velocity direction
        if (autoRotateInAir && rb.velocity.magnitude > minSpeedForRotation)
        {
            float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.AngleAxis(angle, Vector3.forward), airRotationSpeed * Time.deltaTime);
        }

    }
    #endregion

    #region Utils
    //Determines the current position of the mouse in the context of the game world
    Vector2 GetMouseWorldPosition()
    {
        cachedPlayerPosition = transform.position;

        if (mainCamera == null)
        {
            Debug.LogError("Player: Camera is null! Cannot get mouse world position.");
            return Vector2.zero;
        }

        cachedMouseScreenPosition = Input.mousePosition;
        
        if (mainCamera.orthographic)
        {
            // For orthographic cameras, use nearClipPlane
            cachedMouseScreenPosition.z = mainCamera.nearClipPlane;
            cachedMouseWorldPosition = mainCamera.ScreenToWorldPoint(cachedMouseScreenPosition);
        }
        else
        {
            // For perspective cameras, we need to specify the Z distance from camera to the game world plane
            // Calculate the distance from camera to the player's Z position (game world plane)
            float distanceToGamePlane = Mathf.Abs(mainCamera.transform.position.z - cachedPlayerPosition.z);
            cachedMouseScreenPosition.z = distanceToGamePlane;
            cachedMouseWorldPosition = mainCamera.ScreenToWorldPoint(cachedMouseScreenPosition);
        }

        // Reuse existing mousePosition field instead of creating new Vector2
        mousePosition.x = cachedMouseWorldPosition.x;
        mousePosition.y = cachedMouseWorldPosition.y;

        return mousePosition;
    }

    //Passes Debugger messages through enabled check
    void DebugLog(string message)
    {
        if (enableDebugLogs) Debug.Log(message);
    }
    #endregion
}
