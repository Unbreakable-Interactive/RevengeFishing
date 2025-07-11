using UnityEngine;

public class BoatFloater : MonoBehaviour
{
    [Header("Float Points")]
    public Transform[] floatPoints = new Transform[3];

    [Header("Movement Control")]
    [SerializeField] private bool enableFloaterMovement = true;
    [SerializeField] private float horizontalForce = 0f;
    [SerializeField] private float maxHorizontalForce = 5f;
    [SerializeField] private bool adaptToScaleDirection = true;

    // ✅ ADDED: Sprite flip instead of scale rotation
    [Header("Visual Direction (ADDED)")]
    [SerializeField] private SpriteRenderer boatSpriteRenderer; // Reference to boat sprite
    [SerializeField] private bool useSpriteFip = true; // Use sprite flip instead of scale

    // Navigation System
    [Header("Navigation Settings")]
    [SerializeField] private bool enableNavigation = true;
    [SerializeField] private float navigationForce = 3f;
    [SerializeField] private float arrivalDistance = 2f;
    [SerializeField] private float maxNavigationForce = 8f;
    [SerializeField] private string targetPoolName = "BoatFisherman";
    [SerializeField] private bool debugNavigation = false;

    [Header("Buoyancy Settings")]
    [SerializeField] private float buoyancyForce = 6f;
    [SerializeField] private float waterDrag = 0.85f;
    [SerializeField] private float angularDrag = 0.7f;

    [Header("Wave Rolling Control")]
    [SerializeField] private float waveRollStrength = 2.5f;
    [SerializeField] private float rollResponseSpeed = 1.5f;
    [SerializeField] private float maxRollAngle = 12f;
    [SerializeField] private bool enableWaveRolling = true;

    [Header("Stability")]
    [SerializeField] private float stabilityForce = 0.3f;

    [Header("Force Limits - Anti-Break Protection")]
    [SerializeField] private float maxTotalForce = 10f;
    [SerializeField] private float maxTorqueLimit = 8f;
    [SerializeField] private float maxAngularVelocity = 180f;
    [SerializeField] private float forceSmoothing = 0.85f;
    [SerializeField] private bool enableForceProtection = true;

    [Header("VERTICAL MOVEMENT CONTROL")]
    [SerializeField] private float maxVerticalSpeed = 3f;
    [SerializeField] private float verticalDamping = 0.8f;
    [SerializeField] private bool enableSpeedLimit = true;
    [SerializeField] private float smoothBuoyancy = 0.5f;

    private Rigidbody2D rb;
    private WaterPhysics waterPhysics;
    private float currentRollAngle = 0f;
    private float rollVelocity = 0f;
    private Vector2 previousVelocity;
    private float currentDirectionMultiplier = 1f;
    private Vector2 smoothedForce = Vector2.zero;
    private float smoothedTorque = 0f;

    // Navigation variables
    private Transform[] spawnPoints;
    private Transform currentTarget;
    private int currentTargetIndex = 0;
    private Vector2 navigationDirection;
    private float lastDistanceToTarget = float.MaxValue;
    private bool hasFoundSpawnHandler = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        waterPhysics = WaterPhysics.Instance;

        if (floatPoints[0] == null || floatPoints[1] == null || floatPoints[2] == null)
        {
            Debug.LogError("BoatFloater: Float Points not assigned in inspector.");
        }

        // Auto-find boat sprite renderer if not assigned
        if (boatSpriteRenderer == null && useSpriteFip)
        {
            // Look for SpriteRenderer in children with name containing "Boat"
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.gameObject.name.ToLower().Contains("boat"))
                {
                    boatSpriteRenderer = renderer;
                    Debug.Log($"BoatFloater: Auto-found boat sprite: {renderer.gameObject.name}");
                    break;
                }
            }
            
            // If still not found, use the first SpriteRenderer
            if (boatSpriteRenderer == null && renderers.Length > 0)
            {
                boatSpriteRenderer = renderers[0];
                Debug.Log($"BoatFloater: Using first SpriteRenderer: {renderers[0].gameObject.name}");
            }
        }

        // Initialize direction multiplier based on scale
        UpdateDirectionMultiplier();
        
        // Initialize navigation system
        if (enableNavigation)
        {
            InitializeNavigation();
        }
    }

    void InitializeNavigation()
    {
        // Find SpawnHandler with matching pool name
        SpawnHandler[] spawnHandlers = FindObjectsOfType<SpawnHandler>();
        
        foreach (SpawnHandler handler in spawnHandlers)
        {
            if (handler.GetPoolName() == targetPoolName)
            {
                spawnPoints = handler.GetSpawnPoints();
                if (spawnPoints != null && spawnPoints.Length >= 2)
                {
                    hasFoundSpawnHandler = true;
                    FindNearestSpawnPoint();
                    SetNextTarget();
                    
                    if (debugNavigation)
                    {
                        Debug.Log($"BoatFloater: Found SpawnHandler '{targetPoolName}' with {spawnPoints.Length} spawn points");
                    }
                    break;
                }
            }
        }

        if (!hasFoundSpawnHandler)
        {
            Debug.LogWarning($"BoatFloater: Could not find SpawnHandler with pool name '{targetPoolName}'");
            enableNavigation = false;
        }
    }

    void FindNearestSpawnPoint()
    {
        float nearestDistance = float.MaxValue;
        int nearestIndex = 0;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                float distance = Vector2.Distance(transform.position, spawnPoints[i].position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = i;
                }
            }
        }

        currentTargetIndex = nearestIndex;
    }

    void SetNextTarget()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        // Move to next spawn point in sequence
        currentTargetIndex = (currentTargetIndex + 1) % spawnPoints.Length;
        currentTarget = spawnPoints[currentTargetIndex];
        lastDistanceToTarget = float.MaxValue;

        if (debugNavigation)
        {
            Debug.Log($"BoatFloater: New target set to spawn point {currentTargetIndex}");
        }
    }

    void FixedUpdate()
    {
        if (!enableFloaterMovement || waterPhysics == null) return;

        previousVelocity = rb.velocity;

        // Update direction multiplier if scale changes
        if (adaptToScaleDirection)
        {
            UpdateDirectionMultiplier();
        }

        // Handle navigation
        if (enableNavigation && hasFoundSpawnHandler)
        {
            HandleNavigation();
        }

        ApplyBuoyancy();
        ApplyHorizontalForce();
        ApplyWaterResistance();

        if (enableWaveRolling)
        {
            ApplyWaveRolling();
        }

        ApplyStability();

        if (enableSpeedLimit)
        {
            LimitVerticalMovement();
        }

        // Apply force protection to prevent breakage
        if (enableForceProtection)
        {
            ApplyForceProtection();
        }
    }

    void HandleNavigation()
    {
        if (currentTarget == null) return;

        // Calculate direction to target
        Vector2 targetPosition = currentTarget.position;
        Vector2 currentPosition = transform.position;
        navigationDirection = (targetPosition - currentPosition).normalized;

        // Check if we've arrived at target
        float distanceToTarget = Vector2.Distance(currentPosition, targetPosition);
        
        if (distanceToTarget <= arrivalDistance)
        {
            OnArriveAtTarget();
            return;
        }

        // Apply navigation force
        Vector2 navForce = navigationDirection * navigationForce;
        
        // Clamp navigation force
        navForce = Vector2.ClampMagnitude(navForce, maxNavigationForce);
        
        // Apply direction consideration
        if (adaptToScaleDirection)
        {
            // Use sprite flip instead of scale
            UpdateVisualDirection(navigationDirection.x);
        }

        // Apply the navigation force to rigidbody
        rb.AddForce(navForce);

        // Update last distance for arrival detection
        lastDistanceToTarget = distanceToTarget;

        if (debugNavigation)
        {
            Debug.DrawRay(transform.position, navigationDirection * 2f, Color.green);
        }
    }

    // New method that uses sprite flip instead of scale
    void UpdateVisualDirection(float directionX)
    {
        if (Mathf.Abs(directionX) > 0.1f) // Only update if there's significant horizontal movement
        {
            if (useSpriteFip && boatSpriteRenderer != null)
            {
                // Use sprite flip instead of scale rotation
                if (directionX > 0)
                {
                    boatSpriteRenderer.flipX = false; // Face right
                }
                else if (directionX < 0)
                {
                    boatSpriteRenderer.flipX = true;  // Face left
                }
            }
            else
            {
                // Fallback to old scale method if sprite flip is disabled
                UpdateScaleForDirection(directionX);
            }
        }
    }

    // Original scale method as fallback
    void UpdateScaleForDirection(float directionX)
    {
        if (Mathf.Abs(directionX) > 0.1f)
        {
            Vector3 scale = transform.localScale;
            
            if (directionX > 0 && scale.x < 0)
            {
                scale.x = Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
            else if (directionX < 0 && scale.x > 0)
            {
                scale.x = -Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
        }
    }

    void OnArriveAtTarget()
    {
        if (debugNavigation)
        {
            Debug.Log($"BoatFloater: Arrived at spawn point {currentTargetIndex}");
        }

        // Move to next target
        SetNextTarget();
    }

    void UpdateDirectionMultiplier()
    {
        // Consider sprite flip state when using sprite flip
        if (useSpriteFip && boatSpriteRenderer != null)
        {
            currentDirectionMultiplier = boatSpriteRenderer.flipX ? -1f : 1f;
        }
        else
        {
            // Determine direction based on X scale
            currentDirectionMultiplier = transform.localScale.x >= 0 ? 1f : -1f;
        }
    }

    void ApplyHorizontalForce()
    {
        if (Mathf.Abs(horizontalForce) > 0.01f && IsInWater())
        {
            // Apply horizontal force considering direction
            float effectiveForce = Mathf.Clamp(horizontalForce, -maxHorizontalForce, maxHorizontalForce);
            
            // Adjust force based on scale direction if enabled
            if (adaptToScaleDirection)
            {
                effectiveForce *= currentDirectionMultiplier;
            }

            Vector2 horizontalForceVector = Vector2.right * effectiveForce;
            
            // Smooth the force application to prevent sudden jerks
            smoothedForce = Vector2.Lerp(smoothedForce, horizontalForceVector, (1f - forceSmoothing) * Time.fixedDeltaTime * 10f);
            
            rb.AddForce(smoothedForce);
        }
    }

    void ApplyBuoyancy()
    {
        int submergedPoints = 0;
        Vector2 totalForce = Vector2.zero;

        foreach (Transform point in floatPoints)
        {
            if (point == null) continue;

            Vector2 worldPos = point.position;
            float waterHeight = waterPhysics.GetWaterHeightAt(worldPos);
            float submersion = waterHeight - worldPos.y;

            if (submersion > 0)
            {
                submergedPoints++;

                float speedFactor = 1f;
                if (enableSpeedLimit && rb.velocity.y > 0)
                {
                    speedFactor = Mathf.Lerp(1f, smoothBuoyancy, rb.velocity.y / maxVerticalSpeed);
                }

                float force = submersion * buoyancyForce * speedFactor;
                
                // Apply direction consideration to buoyancy distribution
                if (adaptToScaleDirection && currentDirectionMultiplier < 0)
                {
                    // Slightly modify buoyancy distribution when facing opposite direction
                    force *= 0.95f; // Small adjustment to prevent instability
                }
                
                totalForce += Vector2.up * force;
            }
        }

        if (submergedPoints > 0)
        {
            // Apply force protection before adding to rigidbody
            if (enableForceProtection)
            {
                totalForce = Vector2.ClampMagnitude(totalForce, maxTotalForce);
            }
            
            rb.AddForce(totalForce);
        }
    }

    void LimitVerticalMovement()
    {
        Vector2 velocity = rb.velocity;

        if (Mathf.Abs(velocity.y) > maxVerticalSpeed)
        {
            velocity.y = Mathf.Sign(velocity.y) * maxVerticalSpeed;
        }

        if (Mathf.Abs(velocity.y) > maxVerticalSpeed * 0.7f)
        {
            velocity.y *= verticalDamping;
        }

        float velocityChange = Mathf.Abs(velocity.y - previousVelocity.y);
        if (velocityChange > maxVerticalSpeed * 0.5f)
        {
            velocity.y = Mathf.Lerp(previousVelocity.y, velocity.y, 0.7f);
        }

        rb.velocity = velocity;
    }

    void ApplyWaveRolling()
    {
        if (floatPoints.Length < 3) return;

        float bowHeight = waterPhysics.GetWaterHeightAt(floatPoints[0].position);
        float sternHeight = waterPhysics.GetWaterHeightAt(floatPoints[2].position);

        float heightDifference = bowHeight - sternHeight;
        
        // Adjust wave rolling based on direction to prevent conflicts
        if (adaptToScaleDirection && currentDirectionMultiplier < 0)
        {
            heightDifference *= 0.8f; // Reduce rolling intensity when direction changes
        }

        float targetRollAngle = heightDifference * waveRollStrength;
        targetRollAngle = Mathf.Clamp(targetRollAngle, -maxRollAngle, maxRollAngle);

        currentRollAngle = Mathf.SmoothDamp(
            currentRollAngle,
            targetRollAngle,
            ref rollVelocity,
            1f / rollResponseSpeed,
            Mathf.Infinity,
            Time.fixedDeltaTime
        );

        float currentBoatAngle = transform.eulerAngles.z;
        if (currentBoatAngle > 180f) currentBoatAngle -= 360f;

        float angleDifference = Mathf.DeltaAngle(currentBoatAngle, currentRollAngle);
        float rollTorque = angleDifference * rollResponseSpeed;

        // Apply torque protection
        if (enableForceProtection)
        {
            rollTorque = Mathf.Clamp(rollTorque, -maxTorqueLimit, maxTorqueLimit);
            smoothedTorque = Mathf.Lerp(smoothedTorque, rollTorque, (1f - forceSmoothing) * Time.fixedDeltaTime * 8f);
            rb.AddTorque(smoothedTorque, ForceMode2D.Force);
        }
        else
        {
            rb.AddTorque(rollTorque, ForceMode2D.Force);
        }
    }

    void ApplyWaterResistance()
    {
        if (IsInWater())
        {
            // Adjust drag based on direction if needed
            float effectiveDrag = waterDrag;
            float effectiveAngularDrag = angularDrag;
            
            if (adaptToScaleDirection && currentDirectionMultiplier < 0)
            {
                // Slightly increase drag when facing opposite direction for stability
                effectiveDrag *= 1.1f;
                effectiveAngularDrag *= 1.2f;
            }
            
            rb.velocity *= effectiveDrag;
            rb.angularVelocity *= effectiveAngularDrag;
        }
    }

    void ApplyStability()
    {
        float currentAngle = transform.eulerAngles.z;
        if (currentAngle > 180f) currentAngle -= 360f;

        float stabilityTorque = -currentAngle * stabilityForce * Time.fixedDeltaTime;
        
        // Apply direction-aware stability adjustments
        if (adaptToScaleDirection && currentDirectionMultiplier < 0)
        {
            stabilityTorque *= 1.3f; // Increase stability when direction changes
        }
        
        // Apply force protection to stability torque
        if (enableForceProtection)
        {
            stabilityTorque = Mathf.Clamp(stabilityTorque, -maxTorqueLimit * 0.5f, maxTorqueLimit * 0.5f);
        }
        
        rb.AddTorque(stabilityTorque);
    }

    void ApplyForceProtection()
    {
        // Limit angular velocity to prevent excessive spinning
        if (Mathf.Abs(rb.angularVelocity) > maxAngularVelocity)
        {
            rb.angularVelocity = Mathf.Sign(rb.angularVelocity) * maxAngularVelocity;
        }

        // Clamp total velocity magnitude
        if (rb.velocity.magnitude > maxTotalForce)
        {
            rb.velocity = rb.velocity.normalized * maxTotalForce;
        }

        // Prevent extreme rotations that could cause breakage
        float currentAngle = transform.eulerAngles.z;
        if (currentAngle > 180f) currentAngle -= 360f;
        
        if (Mathf.Abs(currentAngle) > 45f)
        {
            // Apply corrective torque to prevent excessive rotation
            float correctionTorque = -currentAngle * 0.1f;
            rb.AddTorque(correctionTorque);
        }
    }

    bool IsInWater()
    {
        foreach (Transform point in floatPoints)
        {
            if (point != null)
            {
                Vector2 worldPos = point.position;
                float waterHeight = waterPhysics.GetWaterHeightAt(worldPos);
                if (waterHeight > worldPos.y) return true;
            }
        }
        return false;
    }

    // Public methods for external control
    public void SetMovementEnabled(bool enabled)
    {
        enableFloaterMovement = enabled;
    }

    public void SetNavigationEnabled(bool enabled)
    {
        enableNavigation = enabled;
    }

    public void SetHorizontalForce(float force)
    {
        horizontalForce = Mathf.Clamp(force, -maxHorizontalForce, maxHorizontalForce);
    }

    public void AddHorizontalForce(float additionalForce)
    {
        float newForce = horizontalForce + additionalForce;
        SetHorizontalForce(newForce);
    }

    public float GetCurrentDirectionMultiplier()
    {
        return currentDirectionMultiplier;
    }

    public Transform GetCurrentTarget()
    {
        return currentTarget;
    }

    public float GetDistanceToTarget()
    {
        if (currentTarget == null) return float.MaxValue;
        return Vector2.Distance(transform.position, currentTarget.position);
    }

    // Public method to set boat sprite renderer manually
    public void SetBoatSpriteRenderer(SpriteRenderer renderer)
    {
        boatSpriteRenderer = renderer;
    }
}
