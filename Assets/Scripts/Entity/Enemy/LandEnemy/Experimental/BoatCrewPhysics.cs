using UnityEngine;
using System.Collections;

public class BoatCrewPhysics : MonoBehaviour
{
    [Header("Physics Mode")]
    [SerializeField] private bool useKinematicBoatPhysics = true;
    [SerializeField] private float kinematicGravityForce = 9.8f;
    [SerializeField] private float boatCrewMass = 0.3f;
    
    [Header("Gravity Transitions")]
    [SerializeField] private float kinematicAirGravity = 2f;
    [SerializeField] private float kinematicUnderwaterGravity = -0.2f;
    [SerializeField] private float gravityTransitionSpeed = 2f;
    
    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundCheckLayers = (1 << 5);
    [SerializeField] private float groundCheckDistance = 0.3f;
    [SerializeField] private float groundCheckInterval = 0.15f;
    
    [Header("Collision Safety")]
    [SerializeField] private float maxMoveDistance = 0.05f;
    [SerializeField] private LayerMask platformLayers = (1 << 5);
    [SerializeField] private float collisionSafetyMargin = 0.1f;
    [SerializeField] private float platformStickDistance = 0.2f;
    
    private Rigidbody2D rb;
    private BoatLandEnemy boatEnemy;
    private BoatCrewPlatformTracker platformTracker;
    
    // Physics state
    private Vector2 simulatedVelocity;
    private bool isGrounded;
    private bool wasKinematicBeforeFall = true;
    private bool isFallingToWater = false;
    private float currentGravityScale = 2f;
    private float targetGravityScale = 2f;
    private bool isStuckToPlatform = false;
    private bool isInitialized = false;
    
    // Caching
    private Collider2D cachedMyCollider;
    private Collider2D cachedPlatformCollider;
    private Vector2 cachedRayStartPosition;
    private float lastGroundCheckTime = 0f;
    private float platformStickDistanceSqr;
    private float maxMoveDistanceSqr;
    
    public bool IsGrounded => isGrounded;
    public bool IsFallingToWater => isFallingToWater;
    public Vector2 SimulatedVelocity => simulatedVelocity;
    public bool IsKinematic => rb != null && rb.bodyType == RigidbodyType2D.Kinematic;
    public bool IsStuckToPlatform => isStuckToPlatform;
    
    public void Initialize(Rigidbody2D rigidbody, BoatLandEnemy enemy, BoatCrewPlatformTracker tracker)
    {
        rb = rigidbody;
        boatEnemy = enemy;
        platformTracker = tracker;
        
        SetupBoatPhysics();
        CacheComponents();
        
        // NULL SAFETY CHECK
        currentGravityScale = (boatEnemy != null && boatEnemy.IsAboveWater) ? kinematicAirGravity : kinematicUnderwaterGravity;
        targetGravityScale = currentGravityScale;
        
        platformStickDistanceSqr = platformStickDistance * platformStickDistance;
        maxMoveDistanceSqr = maxMoveDistance * maxMoveDistance;
        
        isInitialized = true;
        
        GameLogger.LogError($"[CREW PHYSICS] {gameObject.name} - Physics system initialized");
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
            
            Invoke(nameof(ForceCollisionSettings), 0.1f);
            Invoke(nameof(ForceInitialGroundCheck), 0.2f);
            
            GameLogger.LogVerbose($"BoatCrewPhysics {gameObject.name}: Setup kinematic physics with mass {boatCrewMass}");
        }
    }
    
    private void CacheComponents()
    {
        cachedMyCollider = GetComponent<Collider2D>();
        RefreshPlatformColliderCache();
    }

    public void RefreshPlatformColliderCache()
    {
        if (boatEnemy != null && boatEnemy.GetAssignedPlatform() != null)
        {
            cachedPlatformCollider = boatEnemy.GetAssignedPlatform().PlatformCollider;
        }
    }
    
    private void ForceCollisionSettings()
    {
        if (boatEnemy?.GetAssignedPlatform() != null && cachedMyCollider != null && cachedPlatformCollider != null)
        {
            Physics2D.IgnoreCollision(cachedMyCollider, cachedPlatformCollider, false);
            GameLogger.LogVerbose($"BoatCrewPhysics {gameObject.name}: Forced collision settings");
        }
    }
    
    private void ForceInitialGroundCheck()
    {
        CheckGroundStatusOptimized();
        if (!isGrounded)
        {
            GameLogger.LogVerbose($"BoatCrewPhysics {gameObject.name}: Not grounded at spawn, starting to fall");
        }
    }
    
    public void UpdateGravityTransition()
    {
        if (!isInitialized || boatEnemy == null) return;
        
        targetGravityScale = boatEnemy.IsAboveWater ? kinematicAirGravity : kinematicUnderwaterGravity;
        currentGravityScale = Mathf.Lerp(currentGravityScale, targetGravityScale, Time.fixedDeltaTime * gravityTransitionSpeed);
    }
    
    public void CheckGroundStatusOptimized()
    {
        if (!isInitialized) return;
        
        float currentTime = Time.fixedTime;
        if (currentTime - lastGroundCheckTime < groundCheckInterval) return;
        lastGroundCheckTime = currentTime;
        
        if (isStuckToPlatform) 
        {
            isGrounded = true;
            return;
        }

        cachedRayStartPosition = transform.position;
        RaycastHit2D hit = Physics2D.Raycast(cachedRayStartPosition, Vector2.down, groundCheckDistance, groundCheckLayers);
        
        bool wasGrounded = isGrounded;
        isGrounded = hit.collider != null;

        if (wasGrounded != isGrounded)
        {
            GameLogger.LogVerbose($"BoatCrewPhysics {gameObject.name}: Ground status changed to {isGrounded}, hit: {hit.collider?.name}");
        }
    }
    
    public void ApplyPhysicsMovement()
    {
        if (!isInitialized || rb == null || !useKinematicBoatPhysics) return;
        
        // Only apply if still kinematic and alive
        if (rb.bodyType != RigidbodyType2D.Kinematic || boatEnemy?.State != Enemy.EnemyState.Alive) return;
        
        if (platformTracker != null && platformTracker.IsTrackingMovement)
        {
            // Platform tracker handles movement
            return;
        }
        
        KinematicPhysicsSimulation();
    }
    
    private void KinematicPhysicsSimulation()
    {
        if (!isGrounded && !isStuckToPlatform)
        {
            ApplySimulatedGravity();
            ApplyHorizontalMovement();
            ApplySafeKinematicMovement();
        }
        else
        {
            if (simulatedVelocity.y < 0)
            {
                simulatedVelocity.y = Mathf.Lerp(simulatedVelocity.y, 0, Time.fixedDeltaTime * 8f);
            }
            ApplyHorizontalMovement();
            ApplySafeKinematicMovement();
        }
    }
    
    private void ApplySimulatedGravity()
    {
        float gravityForce = currentGravityScale * kinematicGravityForce;
        simulatedVelocity.y -= gravityForce * Time.fixedDeltaTime;
        
        simulatedVelocity.y = Mathf.Max(simulatedVelocity.y, -8f);
        
        if (boatEnemy != null && !boatEnemy.IsAboveWater && currentGravityScale < 0)
        {
            float buoyantForce = Mathf.Abs(currentGravityScale) * kinematicGravityForce * 1.2f;
            simulatedVelocity.y += buoyantForce * Time.fixedDeltaTime;
            simulatedVelocity.y = Mathf.Min(simulatedVelocity.y, 4f);
        }
    }
    
    private void ApplyHorizontalMovement()
    {
        if (boatEnemy == null) return;
        
        Vector2 targetHorizontalVelocity = boatEnemy.GetTargetHorizontalVelocity();
        
        float horizontalDamping = boatEnemy.IsAboveWater ? 0.15f : 0.08f;
        if (isGrounded) horizontalDamping = 0.25f;
        
        simulatedVelocity.x = Mathf.Lerp(simulatedVelocity.x, targetHorizontalVelocity.x, horizontalDamping);
    }
    
    private void ApplySafeKinematicMovement()
    {
        if (simulatedVelocity.sqrMagnitude < 0.000001f) return;

        Vector2 currentPosition = rb.position;
        Vector2 targetMovement = simulatedVelocity * Time.fixedDeltaTime;
        
        if (targetMovement.sqrMagnitude > maxMoveDistanceSqr)
        {
            targetMovement = targetMovement.normalized * maxMoveDistance;
        }

        Vector2 targetPosition = currentPosition + targetMovement;

        if (boatEnemy?.GetAssignedPlatform() != null && !isStuckToPlatform)
        {
            Vector2 safePosition = GetPlatformSafePosition(currentPosition, targetPosition);
            
            if ((currentPosition - safePosition).sqrMagnitude > 0.000001f)
            {
                rb.MovePosition(safePosition);
            }
        }
        else if (platformTracker != null && !platformTracker.IsTrackingMovement)
        {
            rb.MovePosition(targetPosition);
        }
    }
    
    private Vector2 GetPlatformSafePosition(Vector2 currentPos, Vector2 targetPos)
    {
        if (cachedMyCollider == null || cachedPlatformCollider == null) return targetPos;

        Vector2 direction = (targetPos - currentPos);
        float distance = direction.magnitude;
        
        if (distance < 0.001f) return targetPos;
        
        direction.Normalize();

        RaycastHit2D hit = Physics2D.CapsuleCast(
            currentPos,
            cachedMyCollider.bounds.size,
            CapsuleDirection2D.Vertical,
            0f,
            direction,
            distance + collisionSafetyMargin,
            platformLayers
        );

        if (hit.collider != null && hit.collider == cachedPlatformCollider)
        {
            if (Vector2.Dot(direction, Vector2.down) > 0.3f)
            {
                float safeDistance = Mathf.Max(0, hit.distance - collisionSafetyMargin);
                Vector2 landingPosition = currentPos + direction * safeDistance;
                
                if (platformTracker != null)
                {
                    platformTracker.StartTrackingPlatformMovement();
                }
                
                GameLogger.LogVerbose($"BoatCrewPhysics {gameObject.name}: Landed on Platform {hit.collider.name}");
                
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
    
    public void SetStuckToPlatform(bool stuck)
    {
        isStuckToPlatform = stuck;
        isGrounded = stuck;
        if (stuck)
        {
            simulatedVelocity.y = 0;
        }
    }
    
    public void HandleMovementModeChange(bool aboveWater)
    {
        if (!isInitialized || boatEnemy == null) return;
        
        bool wasAboveWater = !aboveWater;
        
        if (rb.bodyType == RigidbodyType2D.Kinematic)
        {
            targetGravityScale = aboveWater ? kinematicAirGravity : kinematicUnderwaterGravity;
            
            if (wasAboveWater != aboveWater)
            {
                GameLogger.LogVerbose($"BoatCrewPhysics {gameObject.name}: Movement mode changed to {(aboveWater ? "Airborne" : "Underwater")}");
            }
        }
        
        if (aboveWater && wasKinematicBeforeFall && rb.bodyType == RigidbodyType2D.Dynamic && boatEnemy.State == Enemy.EnemyState.Alive)
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(DelayedKinematicReturn());
            }
        }
    }
    
    public void SwitchToDynamicPhysics()
    {
        if (rb != null)
        {
            wasKinematicBeforeFall = rb.bodyType == RigidbodyType2D.Kinematic;
            
            if (platformTracker != null)
            {
                platformTracker.StopTrackingPlatformMovement();
            }
            
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.velocity = simulatedVelocity;
            rb.mass = boatCrewMass;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            
            GameLogger.LogVerbose($"BoatCrewPhysics {gameObject.name}: Switched to Dynamic physics");
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
            
            GameLogger.LogVerbose($"BoatCrewPhysics {gameObject.name}: Switched back to Kinematic physics");
        }
    }
    
    private IEnumerator DelayedKinematicReturn()
    {
        yield return new WaitForSeconds(0.5f);
        
        if (boatEnemy?.GetAssignedPlatform() is BoatPlatform && boatEnemy.State == Enemy.EnemyState.Alive)
        {
            SwitchToKinematicPhysics();
        }
    }
    
    public void HandleAlive()
    {
        if (boatEnemy?.GetAssignedPlatform() is BoatPlatform)
        {
            SetupBoatPhysics();
        }
    }
    
    public void HandleDefeat()
    {
        // Stop all kinematic simulation
        useKinematicBoatPhysics = false;
        isInitialized = false;
        
        if (rb != null)
        {
            // Transfer to dynamic physics with current velocity
            Vector2 transferVelocity = simulatedVelocity;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.velocity = transferVelocity;
            rb.gravityScale = 1f;
            rb.simulated = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            
            GameLogger.LogError($"[CREW PHYSICS] {gameObject.name} - Switched to DYNAMIC for defeat behavior");
        }
    }
    
    public void ResetToOriginalState()
    {
        simulatedVelocity = Vector2.zero;
        wasKinematicBeforeFall = true;
        isStuckToPlatform = false;
        isGrounded = false;
        useKinematicBoatPhysics = true;
        isInitialized = true;
        
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        
        GameLogger.LogVerbose($"BoatCrewPhysics {gameObject.name}: Reset to original physics state");
    }
}
