using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoatLandEnemy : LandEnemy, IBoatComponent
{
    [Header("Boat Specific Physics")]
    [SerializeField] private bool useKinematicBoatPhysics = true;
    [SerializeField] private float kinematicGravityForce = 9.8f;
    [SerializeField] private LayerMask groundCheckLayers = (1 << 5);
    [SerializeField] private float groundCheckDistance = 0.3f;
    
    [Header("Gravity Transitions")]
    [SerializeField] private float kinematicAirGravity = 2f;
    [SerializeField] private float kinematicUnderwaterGravity = -0.2f;
    [SerializeField] private float gravityTransitionSpeed = 2f;
    
    [Header("Collision Safety")]
    [SerializeField] private float maxMoveDistance = 0.05f;
    [SerializeField] private LayerMask platformLayers = (1 << 5);
    [SerializeField] private float collisionSafetyMargin = 0.1f;
    [SerializeField] private float platformStickDistance = 0.2f;
    
    [Header("Boat Fishing Configuration")]
    [SerializeField] protected FishermanConfig fishermanConfig;
    
    private Vector2 simulatedVelocity;
    private bool isGrounded;
    private bool wasKinematicBeforeFall = true;
    private bool isFallingToWater = false;
    private float currentGravityScale = 2f;
    private float targetGravityScale = 2f;
    private bool isStuckToPlatform = false;
    
    private Vector3 lastPlatformPosition;
    private Vector3 lastValidPlatformPosition;
    private float relativeYOffsetToPlatform = 0f;
    private bool trackingPlatformMovement = false;
    
    [Header("Boat Physics Integration")]
    [SerializeField] private float boatCrewMass = 0.3f;

    protected override void Start()
    {
        base.Start();
        SetupBoatPhysics();
        
        currentGravityScale = isAboveWater ? kinematicAirGravity : kinematicUnderwaterGravity;
        targetGravityScale = currentGravityScale;
        
        if (fishermanConfig == null)
        {
            fishermanConfig = Resources.Load<FishermanConfig>("FishermanConfig");
            if (fishermanConfig == null)
            {
                Debug.LogWarning($"BoatLandEnemy {gameObject.name}: No FishermanConfig found! Fishing behavior will not work.");
            }
        }
    }

    private void SetupBoatPhysics()
    {
        if (rb != null && useKinematicBoatPhysics)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;
            rb.mass = boatCrewMass;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            
            simulatedVelocity = Vector2.zero;
            wasKinematicBeforeFall = true;
            isStuckToPlatform = false;
            trackingPlatformMovement = false;
            
            Invoke(nameof(ForceCollisionSettings), 0.1f);
            Invoke(nameof(ForceInitialGroundCheck), 0.2f);
            
            if (enableDebugMessages)
                Debug.Log($"BoatLandEnemy {gameObject.name}: Physics setup - Kinematic with mass {boatCrewMass}");
        }
    }
    
    private void ForceCollisionSettings()
    {
        if (assignedPlatform != null)
        {
            Collider2D myCollider = GetComponent<Collider2D>();
            Collider2D platformCollider = assignedPlatform.PlatformCollider;
            
            if (myCollider != null && platformCollider != null)
            {
                Physics2D.IgnoreCollision(myCollider, platformCollider, false);
                
                if (enableDebugMessages)
                    Debug.Log($"BoatLandEnemy {gameObject.name}: Forced collision settings with {assignedPlatform.name}");
            }
        }
    }
    
    private void ForceInitialGroundCheck()
    {
        CheckGroundStatus();
        if (!isGrounded)
        {
            if (enableDebugMessages)
                Debug.Log($"BoatLandEnemy {gameObject.name}: Not grounded at spawn, starting to fall");
        }
    }

    protected override void Update()
    {
        base.Update();

        if (hasThrownHook) HandleActiveHook();
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

    private void FixedUpdate()
    {
        if (rb == null) return;

        if (rb.bodyType == RigidbodyType2D.Kinematic && useKinematicBoatPhysics)
        {
            KinematicPhysicsSimulation();
        }
    }

    private void KinematicPhysicsSimulation()
    {
        if (_state != EnemyState.Alive && _state != EnemyState.Defeated) return;

        UpdateGravityTransition();
        UpdatePlatformTracking();
        CheckPlatformSticking();
        CheckGroundStatus();
        
        if (!platformBoundsCalculated && assignedPlatform != null)
        {
            CalculatePlatformBounds();
        }

        if ((isGrounded || isStuckToPlatform) && Time.time >= nextActionTime)
        {
            MakeAIDecision();
        }

        if (!isGrounded && !isStuckToPlatform)
        {
            ApplySimulatedGravity();
            ApplyHorizontalMovement();
            CheckPlatformBounds();
            ApplySafeKinematicMovement();
        }
        else if (isStuckToPlatform)
        {
            ApplyHorizontalMovementOnPlatform();
            CheckPlatformBounds();
        }
        else
        {
            if (simulatedVelocity.y < 0)
            {
                simulatedVelocity.y = Mathf.Lerp(simulatedVelocity.y, 0, Time.fixedDeltaTime * 8f);
            }
            ApplyHorizontalMovement();
            CheckPlatformBounds();
            ApplySafeKinematicMovement();
        }
    }

    protected override void MakeAIDecision()
    {
        if (_state == EnemyState.Defeated) return;

        if (fishermanConfig == null)
        {
            base.MakeAIDecision();
            return;
        }

        if (_landMovementState == LandMovementState.Idle && !hasThrownHook)
        {
            if (!fishingToolEquipped)
            {
                if (Random.value < fishermanConfig.equipToolChance)
                {
                    ScheduleNextAction();
                    TryEquipFishingTool();
                    if (enableDebugMessages)
                        Debug.Log($"BoatLandEnemy {gameObject.name}: Equipped fishing tool");
                    return;
                }
            }
            else
            {
                float random = Random.value;
                if (random < fishermanConfig.hookThrowChance)
                {
                    if (hookSpawner?.CanThrowHook() == true)
                    {
                        hookSpawner.ThrowHook();
                        hasThrownHook = true;
                        hookTimer = 0f;

                        SubscribeToHookEvents();
                        
                        if (enableDebugMessages)
                            Debug.Log($"BoatLandEnemy {gameObject.name}: Threw fishing hook!");
                    }
                }
                else if (random < (fishermanConfig.hookThrowChance + fishermanConfig.unequipToolChance))
                {
                    ScheduleNextAction();
                    TryUnequipFishingTool();
                    if (enableDebugMessages)
                        Debug.Log($"BoatLandEnemy {gameObject.name}: Unequipped fishing tool");
                    return;
                }
            }
            ScheduleNextAction();
            return;
        }

        base.MakeAIDecision();
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

    protected override void CleanupFishingTools()
    {
        base.CleanupFishingTools();

        if (hookSpawner != null && hookSpawner.HasActiveHook())
        {
            CleanupHookSubscription();

            hookSpawner.OnHookDestroyed();

            hasThrownHook = false;
            hookTimer = 0f;

            Debug.Log($"BoatLandEnemy {gameObject.name} - Hook handler destroyed due to defeat");
        }
    }

    private void UpdatePlatformTracking()
    {
        if (!trackingPlatformMovement || assignedPlatform == null) return;

        Collider2D platformCollider = assignedPlatform.PlatformCollider;
        if (platformCollider == null) return;

        Vector3 currentPlatformPosition = platformCollider.bounds.max;
        
        if (lastPlatformPosition != Vector3.zero)
        {
            Vector3 platformMovement = currentPlatformPosition - lastPlatformPosition;
            Vector3 newPosition = transform.position + platformMovement;
            newPosition.y = currentPlatformPosition.y + relativeYOffsetToPlatform;
            transform.position = newPosition;
            
            if (enableDebugMessages && platformMovement.magnitude > 0.001f)
            {
                Debug.Log($"BoatLandEnemy {gameObject.name}: Following platform movement: {platformMovement}");
            }
        }
        
        lastPlatformPosition = currentPlatformPosition;
    }

    private void StartTrackingPlatformMovement()
    {
        if (assignedPlatform == null) return;

        Collider2D platformCollider = assignedPlatform.PlatformCollider;
        if (platformCollider == null) return;

        isStuckToPlatform = true;
        isGrounded = true;
        simulatedVelocity.y = 0;
        trackingPlatformMovement = true;

        Vector3 platformSurface = platformCollider.bounds.max;
    
        if (BodyCollider != null)
        {
            float enemyHeight = BodyCollider.bounds.size.y;
            relativeYOffsetToPlatform = -(enemyHeight / 2f) + 0.25f;
        
            if (enableDebugMessages)
            {
                Debug.Log($"BoatLandEnemy {gameObject.name}: Calculated dynamic offset using BodyCollider. Enemy height: {enemyHeight}, Offset: {relativeYOffsetToPlatform}");
            }
        }
        else
        {
            relativeYOffsetToPlatform = -0.15f;
        
            if (enableDebugMessages)
            {
                Debug.LogWarning($"BoatLandEnemy {gameObject.name}: BodyCollider not assigned, using fallback offset: {relativeYOffsetToPlatform}");
            }
        }

        lastPlatformPosition = platformSurface;

        Vector3 adjustedPosition = transform.position;
        adjustedPosition.y = platformSurface.y + relativeYOffsetToPlatform;
        transform.position = adjustedPosition;

        if (enableDebugMessages)
        {
            Debug.Log($"BoatLandEnemy {gameObject.name}: Started tracking platform movement. Y offset: {relativeYOffsetToPlatform}");
        }
    }

    private void StopTrackingPlatformMovement()
    {
        isStuckToPlatform = false;
        trackingPlatformMovement = false;
        lastPlatformPosition = Vector3.zero;
        relativeYOffsetToPlatform = 0f;
        
        if (enableDebugMessages)
        {
            Debug.Log($"BoatLandEnemy {gameObject.name}: Stopped tracking platform movement");
        }
    }

    private void UpdateGravityTransition()
    {
        targetGravityScale = isAboveWater ? kinematicAirGravity : kinematicUnderwaterGravity;
        currentGravityScale = Mathf.Lerp(currentGravityScale, targetGravityScale, Time.fixedDeltaTime * gravityTransitionSpeed);
    }

    private void CheckPlatformSticking()
    {
        if (assignedPlatform == null) 
        {
            if (trackingPlatformMovement)
            {
                StopTrackingPlatformMovement();
            }
            return;
        }

        Collider2D platformCollider = assignedPlatform.PlatformCollider;
        if (platformCollider == null) return;

        float distance = transform.position.y - platformCollider.bounds.max.y;

        if (trackingPlatformMovement)
        {
            if (distance > platformStickDistance * 4f)
            {
                StopTrackingPlatformMovement();
                isGrounded = false;
            }
        }
        else
        {
            if (distance <= platformStickDistance && distance >= -platformStickDistance)
            {
                StartTrackingPlatformMovement();
            }
        }
    }

    private void CheckGroundStatus()
    {
        if (isStuckToPlatform) 
        {
            isGrounded = true;
            return;
        }

        Vector2 rayStart = (Vector2)transform.position;
        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, groundCheckDistance, groundCheckLayers);
        
        bool wasGrounded = isGrounded;
        isGrounded = hit.collider != null;

        if (enableDebugMessages && wasGrounded != isGrounded)
        {
            Debug.Log($"BoatLandEnemy {gameObject.name}: Ground status changed to {isGrounded}, hit: {hit.collider?.name}");
        }
    }

    private void ApplySimulatedGravity()
    {
        float gravityForce = currentGravityScale * kinematicGravityForce;
        simulatedVelocity.y -= gravityForce * Time.fixedDeltaTime;
        
        simulatedVelocity.y = Mathf.Max(simulatedVelocity.y, -8f);
        
        if (!isAboveWater && currentGravityScale < 0)
        {
            float buoyantForce = Mathf.Abs(currentGravityScale) * kinematicGravityForce * 1.2f;
            simulatedVelocity.y += buoyantForce * Time.fixedDeltaTime;
            simulatedVelocity.y = Mathf.Min(simulatedVelocity.y, 4f);
        }
    }

    private void ApplyHorizontalMovement()
    {
        Vector2 targetHorizontalVelocity = GetTargetHorizontalVelocity();
        
        float horizontalDamping = isAboveWater ? 0.15f : 0.08f;
        if (isGrounded) horizontalDamping = 0.25f;
        
        simulatedVelocity.x = Mathf.Lerp(simulatedVelocity.x, targetHorizontalVelocity.x, horizontalDamping);
    }

    private void ApplyHorizontalMovementOnPlatform()
    {
        Vector2 targetHorizontalVelocity = GetTargetHorizontalVelocity();
        
        if (targetHorizontalVelocity.magnitude > 0.001f)
        {
            Vector3 currentPos = transform.position;
            Vector3 horizontalMovement = (Vector3)targetHorizontalVelocity * Time.fixedDeltaTime;
            Vector3 newPos = currentPos + horizontalMovement;
            
            if (assignedPlatform != null)
            {
                Collider2D platformCollider = assignedPlatform.PlatformCollider;
                if (platformCollider != null)
                {
                    Bounds platformBounds = platformCollider.bounds;
                    
                    float leftEdge = platformBounds.min.x + edgeBuffer;
                    float rightEdge = platformBounds.max.x - edgeBuffer;
                    
                    newPos.x = Mathf.Clamp(newPos.x, leftEdge, rightEdge);
                    newPos.y = currentPos.y;
                    
                    transform.position = newPos;
                }
            }
        }
    }

    private Vector2 GetTargetHorizontalVelocity()
    {
        switch (_landMovementState)
        {
            case LandMovementState.WalkLeft:
                return Vector2.left * walkingSpeed;
            case LandMovementState.WalkRight:
                return Vector2.right * walkingSpeed;
            case LandMovementState.RunLeft:
                return Vector2.left * runningSpeed;
            case LandMovementState.RunRight:
                return Vector2.right * runningSpeed;
            case LandMovementState.Idle:
            default:
                return Vector2.zero;
        }
    }

    protected override void CheckPlatformBounds()
    {
        if (trackingPlatformMovement && assignedPlatform is BoatPlatform)
        {
            CheckBoatPlatformBounds();
            return;
        }
        
        base.CheckPlatformBounds();
    }

    private void CheckBoatPlatformBounds()
    {
        if (assignedPlatform == null) return;

        Collider2D platformCollider = assignedPlatform.PlatformCollider;
        if (platformCollider == null) return;

        Bounds platformBounds = platformCollider.bounds;
        Vector3 currentPos = transform.position;

        float leftEdge = platformBounds.min.x + edgeBuffer;
        float rightEdge = platformBounds.max.x - edgeBuffer;

        if (currentPos.x <= leftEdge + 0.1f || currentPos.x >= rightEdge - 0.1f)
        {
            if (enableDebugMessages)
                Debug.Log($"BoatLandEnemy {gameObject.name}: Reached platform edge, stopping movement");
            
            _landMovementState = LandMovementState.Idle;
            
            float clampedX = Mathf.Clamp(currentPos.x, leftEdge + 0.1f, rightEdge - 0.1f);
            Vector3 clampedPos = new Vector3(clampedX, currentPos.y, currentPos.z);
            transform.position = Vector3.Lerp(currentPos, clampedPos, Time.fixedDeltaTime * 5f);
        }
    }

    protected override void CalculatePlatformBounds()
    {
        if (assignedPlatform == null) return;

        Collider2D platformCollider = assignedPlatform.PlatformCollider;
    
        if (platformCollider != null)
        {
            Bounds bounds = platformCollider.bounds;
            platformLeftEdge = bounds.min.x + edgeBuffer;
            platformRightEdge = bounds.max.x - edgeBuffer;
            platformBoundsCalculated = true;
            
            if (assignedPlatform.showDebugInfo)
            {
                Debug.Log($"BoatLandEnemy {gameObject.name}: Calculated platform bounds - Left: {platformLeftEdge}, Right: {platformRightEdge}");
            }
        }
    }

    private void ApplySafeKinematicMovement()
    {
        if (simulatedVelocity.magnitude < 0.001f) return;

        Vector2 currentPosition = rb.position;
        Vector2 targetMovement = simulatedVelocity * Time.fixedDeltaTime;
        
        if (targetMovement.magnitude > maxMoveDistance)
        {
            targetMovement = targetMovement.normalized * maxMoveDistance;
        }

        Vector2 targetPosition = currentPosition + targetMovement;

        if (assignedPlatform != null && !isStuckToPlatform)
        {
            Vector2 safePosition = GetPlatformSafePosition(currentPosition, targetPosition);
            
            if (Vector2.Distance(currentPosition, safePosition) > 0.001f)
            {
                rb.MovePosition(safePosition);
            }
        }
        else if (!trackingPlatformMovement)
        {
            rb.MovePosition(targetPosition);
        }
    }

    private Vector2 GetPlatformSafePosition(Vector2 currentPos, Vector2 targetPos)
    {
        Collider2D myCollider = GetComponent<Collider2D>();
        Collider2D platformCollider = assignedPlatform.PlatformCollider;
        
        if (myCollider == null || platformCollider == null) return targetPos;

        Vector2 direction = (targetPos - currentPos);
        float distance = direction.magnitude;
        
        if (distance < 0.001f) return targetPos;
        
        direction.Normalize();

        RaycastHit2D hit = Physics2D.CapsuleCast(
            currentPos,
            myCollider.bounds.size,
            CapsuleDirection2D.Vertical,
            0f,
            direction,
            distance + collisionSafetyMargin,
            platformLayers
        );

        if (hit.collider != null && hit.collider == platformCollider)
        {
            if (Vector2.Dot(direction, Vector2.down) > 0.3f)
            {
                float safeDistance = Mathf.Max(0, hit.distance - collisionSafetyMargin);
                Vector2 landingPosition = currentPos + direction * safeDistance;
                
                StartTrackingPlatformMovement();
                
                if (enableDebugMessages)
                    Debug.Log($"BoatLandEnemy {gameObject.name}: Landed on Platform {hit.collider.name}");
                
                return landingPosition;
            }
            else
            {
                float safeDistance = Mathf.Max(0, hit.distance - collisionSafetyMargin);
                return currentPos + direction * safeDistance;
            }
        }

        return targetPos;
    }

    public override void SetAssignedPlatform(Platform platform)
    {
        base.SetAssignedPlatform(platform);
        
        if (platform != null)
        {
            Invoke(nameof(ForceCollisionSettings), 0.1f);
        }
        else
        {
            StopTrackingPlatformMovement();
        }
    }

    public override void SetMovementMode(bool aboveWater)
    {
        bool wasAboveWater = isAboveWater;
        base.SetMovementMode(aboveWater);
        
        if (rb.bodyType == RigidbodyType2D.Kinematic)
        {
            targetGravityScale = aboveWater ? kinematicAirGravity : kinematicUnderwaterGravity;
            
            if (enableDebugMessages && wasAboveWater != aboveWater)
            {
                Debug.Log($"BoatLandEnemy {gameObject.name}: Movement mode changed to {(aboveWater ? "Airborne" : "Underwater")}");
            }
        }
        
        if (aboveWater && wasKinematicBeforeFall && rb.bodyType == RigidbodyType2D.Dynamic && _state == EnemyState.Alive)
        {
            StartCoroutine(DelayedKinematicReturn());
        }
    }

    public void SwitchToDynamicPhysics()
    {
        if (rb != null)
        {
            wasKinematicBeforeFall = rb.bodyType == RigidbodyType2D.Kinematic;
            
            StopTrackingPlatformMovement();
            
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.velocity = simulatedVelocity;
            rb.mass = boatCrewMass;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            
            if (enableDebugMessages)
                Debug.Log($"BoatLandEnemy {gameObject.name}: Switched to Dynamic physics");
        }
    }

    public void SwitchToKinematicPhysics()
    {
        if (rb != null)
        {
            simulatedVelocity = rb.velocity;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.mass = boatCrewMass;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            
            if (enableDebugMessages)
                Debug.Log($"BoatLandEnemy {gameObject.name}: Switched back to Kinematic physics");
        }
    }

    private IEnumerator DelayedKinematicReturn()
    {
        yield return new WaitForSeconds(0.5f);
        
        if (assignedPlatform is BoatPlatform && _state == EnemyState.Alive)
        {
            SwitchToKinematicPhysics();
        }
    }

    public override void WaterMovement()
    {
        if (rb.bodyType == RigidbodyType2D.Dynamic)
        {
            base.WaterMovement();
        }
    }

    public override void ChangeState_Alive()
    {
        base.ChangeState_Alive();
        
        if (assignedPlatform is BoatPlatform)
        {
            SetupBoatPhysics();
        }
    }

    public void SetPhysicsMode(bool kinematic)
    {
        useKinematicBoatPhysics = kinematic;
        
        if (rb != null)
        {
            if (kinematic)
            {
                SwitchToKinematicPhysics();
            }
            else
            {
                SwitchToDynamicPhysics();
            }
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        
        SetupBoatPhysics();
        
        simulatedVelocity = Vector2.zero;
        wasKinematicBeforeFall = true;
        isStuckToPlatform = false;
        trackingPlatformMovement = false;
        lastValidPlatformPosition = Vector3.zero;
        
        if (enableDebugMessages)
            Debug.Log($"BoatLandEnemy {gameObject.name}: Initialize() - Reset to Kinematic physics and cleared tracking state");
    }

    protected override void TriggerDefeat()
    {
        SwitchToDynamicPhysics();
        
        base.TriggerDefeat();
        
        if (enableDebugMessages)
            Debug.Log($"BoatLandEnemy {gameObject.name}: Switched to Dynamic physics on defeat - will fall to water");
    }

    protected override void StartDefeatBehaviors()
    {
        base.StartDefeatBehaviors();
        
        StopTrackingPlatformMovement();
        
        if (enableDebugMessages)
            Debug.Log($"BoatLandEnemy {gameObject.name}: Defeat behaviors started - ready to fall");
    }
}
