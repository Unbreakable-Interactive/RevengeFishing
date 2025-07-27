using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoatFloater : MonoBehaviour
{
    [SerializeField] private float horizontalForce = 0f;
    [SerializeField] private float maxHorizontalForce = 5f;
    [SerializeField] private float movementSpeed = 2f;
    [SerializeField] private float maxMovementForce = 6f;
    
    
    [Header("Float Points")]
    public Transform[] floatPoints = new Transform[3];

    [Header("Movement Control")]
    [SerializeField] private bool enableFloaterMovement = true;
    [SerializeField] private bool adaptToScaleDirection = true;

    [Header("Visual Direction")]
    [SerializeField] private SpriteRenderer boatSpriteRenderer;

    [SerializeField] private bool useSpriteFlip = true;

    [Header("BOAT MOVEMENT SYSTEM")]
    [SerializeField] private bool enableAutomaticMovement = true;
    [SerializeField] private float movementChangeInterval = 3f;
    [SerializeField] private float minMovementTime = 2f;
    [SerializeField] private bool debugMovement = false;

    [Header("Platform References")]
    [SerializeField] private Platform[] cachedPlatforms;

    [Header("Buoyancy Settings")]
    [SerializeField] private float buoyancyForce = 25f;
    [SerializeField] private float waterDrag = 0.85f;
    [SerializeField] private float angularDrag = 0.7f;
    
    [Header("DYNAMIC MASS COMPENSATION")]
    [SerializeField] private bool enableDynamicBuoyancy = true;
    [SerializeField] private float baseMass = 1f;
    [SerializeField] private float buoyancyPerMass = 8f;
    [SerializeField] private float maxBuoyancyMultiplier = 5f;
    [SerializeField] private bool debugMassChanges = true;

    [Header("Wave Rolling Control")]
    [SerializeField] private float waveRollStrength = 2.5f;
    [SerializeField] private float rollResponseSpeed = 1.5f;
    [SerializeField] private float maxRollAngle = 12f;
    [SerializeField] private bool enableWaveRolling = true;

    [Header("Stability")]
    [SerializeField] private float stabilityForce = 0.3f;

    [Header("Force Limits - Anti-Break Protection")]
    [SerializeField] private float maxTotalForce = 50f;
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

    private bool isRegisteredToPlatform = false;
    private bool movementActive = false;
    private float currentMovementDirection = 1f;
    private Vector2 currentMovementTarget;
    private Platform assignedPlatform;
    
    private float lastKnownMass = 0f;
    private float effectiveBuoyancyForce = 6f;
    private float massCheckTimer = 0f;
    private const float MASS_CHECK_INTERVAL = 0.1f; 
    [SerializeField] private List<Rigidbody2D> cachedChildRigidbodies;
    [SerializeField] private Fisherman[] cachedEnemies;
    private bool componentsCached = false;

    [Header("BOUNDARY-BASED MOVEMENT")]
    [SerializeField] private Transform leftBoundary;
    [SerializeField] private Transform rightBoundary;
    [SerializeField] private float boundaryBuffer = 1f;

    public void InitializeCrew(List<Fisherman> fishermans)
    {
        cachedEnemies = fishermans.ToArray();
        foreach (var enemy in cachedEnemies)
        {
            cachedChildRigidbodies.Add(enemy.Rigidbody2D);
        }
    }
    
    public void InitializeBoundaries(Transform _leftBoundary, Transform _rightBoundary)
    {
        leftBoundary = _leftBoundary;
        rightBoundary = _rightBoundary;
    }
    
    public void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        waterPhysics = WaterPhysics.Instance;

        if (floatPoints[0] == null || floatPoints[1] == null || floatPoints[2] == null)
        {
            Debug.LogError("BoatFloater: Float Points not assigned in inspector.");
        }
        
        baseMass = rb.mass;
        RefreshComponentCache();
        lastKnownMass = CalculateTotalMassOptimized();
        effectiveBuoyancyForce = buoyancyForce;
        
        float requiredBuoyancy = rb.mass * Physics2D.gravity.magnitude * 1.5f;
        if (effectiveBuoyancyForce < requiredBuoyancy)
        {
            effectiveBuoyancyForce = requiredBuoyancy;
            Debug.Log($"AUTO-ADJUSTED: Buoyancy increased to {effectiveBuoyancyForce:F1} to support mass {rb.mass}");
        }
        
        if (debugMassChanges)
        {
            Debug.Log($"BoatFloater: Initial mass setup - Base: {baseMass}, Total: {lastKnownMass}, Buoyancy: {effectiveBuoyancyForce}");
        }

        if (boatSpriteRenderer == null && useSpriteFlip)
        {
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
            
            if (boatSpriteRenderer == null && renderers.Length > 0)
            {
                boatSpriteRenderer = renderers[0];
                Debug.Log($"BoatFloater: Using first SpriteRenderer: {renderers[0].gameObject.name}");
            }
        }

        UpdateDirectionMultiplier();
        
        StartCoroutine(CheckForPlatformRegistration());
        
        if (debugMovement)
        {
            Debug.Log("BoatFloater: Initialized, waiting for platform registration to start movement");
        }
    }

    private IEnumerator CheckForPlatformRegistration()
    {
        float checkTime = 0f;
        int checkCount = 0;
        const int MAX_CHECKS = 6; 
        
        if (cachedPlatforms == null || cachedPlatforms.Length == 0)
        {
            cachedPlatforms = FindObjectsOfType<Platform>();
            if (debugMovement)
            {
                Debug.Log($"BoatFloater: Cached {cachedPlatforms.Length} platforms for registration checks");
            }
        }
        
        while (checkTime < 3f && checkCount < MAX_CHECKS) 
        {
            if (checkCount % 2 == 0) 
            {
                foreach (Platform platform in cachedPlatforms)
                {
                    if (platform != null && platform.assignedEnemies != null)
                    {
                        foreach (var enemy in platform.assignedEnemies)
                        {
                            if (enemy != null && enemy.transform.IsChildOf(this.transform))
                            {
                                OnRegisteredToPlatform(platform);
                                yield break;
                            }
                        }
                    }
                }
            }
            
            checkCount++;
            checkTime += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }
        
        if (debugMovement)
        {
            Debug.Log("BoatFloater: No platform registration detected after optimized search, starting movement anyway");
        }
        StartMovement();
    }

    public void OnRegisteredToPlatform(Platform platform)
    {
        if (isRegisteredToPlatform) return;
        
        isRegisteredToPlatform = true;
        assignedPlatform = platform;
        
        if (debugMovement)
        {
            Debug.Log($"BoatFloater: Registered to platform {platform.name}, starting movement!");
        }
        
        StartMovement();
    }

    public void StartMovement()
    {
        if (movementActive) return;
        
        movementActive = true;
        ChooseNewMovementDirection();
        
        if (debugMovement)
        {
            Debug.Log("BoatFloater: Movement started!");
        }
    }

    void ChooseNewMovementDirection()
    {
        currentMovementDirection = Random.Range(0, 2) == 0 ? -1f : 1f;
        
        if (useSpriteFlip && boatSpriteRenderer != null)
        {
            boatSpriteRenderer.flipX = currentMovementDirection < 0;
        }
        
        if (debugMovement)
        {
            string direction = currentMovementDirection > 0 ? "RIGHT" : "LEFT";
            Debug.Log($"BoatFloater: New movement direction: {direction}");
        }
    }

    void FixedUpdate()
    {
        if (!enableFloaterMovement || waterPhysics == null) return;

        previousVelocity = rb.velocity;

        if (enableDynamicBuoyancy)
        {
            UpdateDynamicBuoyancyOptimized();
        }

        if (adaptToScaleDirection)
        {
            UpdateDirectionMultiplier();
        }

        if (enableAutomaticMovement && movementActive)
        {
            HandleBoatMovement();
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

        if (enableForceProtection)
        {
            ApplyForceProtection();
        }
    }

    void HandleBoatMovement()
    {
        if (!IsInWater()) return;
    
        bool nearLeftBoundary = leftBoundary != null && transform.position.x <= leftBoundary.position.x + boundaryBuffer;
        bool nearRightBoundary = rightBoundary != null && transform.position.x >= rightBoundary.position.x - boundaryBuffer;
    
        if (nearLeftBoundary && currentMovementDirection < 0)
        {
            currentMovementDirection = 1f; 
            UpdateVisualDirection();
        }
        else if (nearRightBoundary && currentMovementDirection > 0)
        {
            currentMovementDirection = -1f;
            UpdateVisualDirection();
        }
    
        Vector2 moveForce = Vector2.right * (currentMovementDirection * movementSpeed);
        moveForce = Vector2.ClampMagnitude(moveForce, maxMovementForce);
        rb.AddForce(moveForce);
    }
    
    void UpdateVisualDirection()
    {
        if (useSpriteFlip && boatSpriteRenderer != null)
        {
            boatSpriteRenderer.flipX = currentMovementDirection < 0;
        }
    
        if (debugMovement)
        {
            string dir = currentMovementDirection > 0 ? "RIGHT" : "LEFT";
            Debug.Log($"BoatFloater: Visual direction updated to {dir}");
        }
    }
    
    void UpdateDirectionMultiplier()
    {
        if (useSpriteFlip && boatSpriteRenderer != null)
        {
            currentDirectionMultiplier = boatSpriteRenderer.flipX ? -1f : 1f;
        }
        else
        {
            currentDirectionMultiplier = transform.localScale.x >= 0 ? 1f : -1f;
        }
    }

    void ApplyHorizontalForce()
    {
        if (Mathf.Abs(horizontalForce) > 0.01f && IsInWater())
        {
            float effectiveForce = Mathf.Clamp(horizontalForce, -maxHorizontalForce, maxHorizontalForce);

            if (adaptToScaleDirection)
            {
                effectiveForce *= currentDirectionMultiplier;
            }

            Vector2 horizontalForceVector = Vector2.right * effectiveForce;
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

                float force = submersion * effectiveBuoyancyForce * speedFactor;
                
                if (adaptToScaleDirection && currentDirectionMultiplier < 0)
                {
                    force *= 0.95f; 
                }
                
                totalForce += Vector2.up * force;
            }
        }

        if (submergedPoints > 0)
        {
            if (enableForceProtection)
            {
                totalForce = Vector2.ClampMagnitude(totalForce, maxTotalForce * (effectiveBuoyancyForce / buoyancyForce));
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
        
        if (adaptToScaleDirection && currentDirectionMultiplier < 0)
        {
            heightDifference *= 0.8f;
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
            float effectiveDrag = waterDrag;
            float effectiveAngularDrag = angularDrag;
            
            if (adaptToScaleDirection && currentDirectionMultiplier < 0)
            {
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
        
        if (adaptToScaleDirection && currentDirectionMultiplier < 0)
        {
            stabilityTorque *= 1.3f; 
        }
        
        if (enableForceProtection)
        {
            stabilityTorque = Mathf.Clamp(stabilityTorque, -maxTorqueLimit * 0.5f, maxTorqueLimit * 0.5f);
        }
        
        rb.AddTorque(stabilityTorque);
    }

    void ApplyForceProtection()
    {
        if (Mathf.Abs(rb.angularVelocity) > maxAngularVelocity)
        {
            rb.angularVelocity = Mathf.Sign(rb.angularVelocity) * maxAngularVelocity;
        }

        if (rb.velocity.magnitude > maxTotalForce)
        {
            rb.velocity = rb.velocity.normalized * maxTotalForce;
        }

        float currentAngle = transform.eulerAngles.z;
        if (currentAngle > 180f) currentAngle -= 360f;
        
        if (Mathf.Abs(currentAngle) > 45f)
        {
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

    public void SetMovementEnabled(bool enabled)
    {
        enableFloaterMovement = enabled;
    }

    public void SetAutomaticMovementEnabled(bool enabled)
    {
        enableAutomaticMovement = enabled;
        
        if (enabled && !movementActive && isRegisteredToPlatform)
        {
            StartMovement();
        }
        else if (!enabled)
        {
            movementActive = false;
        }
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

    public void SetBoatSpriteRenderer(SpriteRenderer renderer)
    {
        boatSpriteRenderer = renderer;
    }

    public bool IsMovementActive() => movementActive;
    public bool IsRegisteredToPlatform() => isRegisteredToPlatform;
    public float GetCurrentMovementDirection() => currentMovementDirection;
    public Platform GetAssignedPlatform() => assignedPlatform;

    public void ForceStartMovement()
    {
        StartMovement();
    }

    public void StopMovement()
    {
        movementActive = false;
        if (debugMovement)
        {
            Debug.Log("BoatFloater: Movement stopped manually");
        }
    }

    float CalculateTotalMassOptimized()
    {
        if (!componentsCached)
        {
            RefreshComponentCache();
        }
        
        float totalMass = rb.mass;
        
        if (cachedChildRigidbodies != null)
        {
            foreach (Rigidbody2D childRb in cachedChildRigidbodies)
            {
                if (childRb != null && childRb != rb)
                {
                    totalMass += childRb.mass;
                }
            }
        }
        
        if (cachedEnemies != null)
        {
            totalMass += cachedEnemies.Length * 0.5f;
        }
        
        return totalMass;
    }
    
    void RefreshComponentCache()
    {
        componentsCached = true;
        
        if (debugMassChanges)
        {
            Debug.Log($"CACHE REFRESH: Found {cachedChildRigidbodies?.Count ?? 0} rigidbodies, {cachedEnemies?.Length ?? 0} enemies");
        }
    }
    
    void UpdateDynamicBuoyancyOptimized()
    {
        massCheckTimer += Time.fixedDeltaTime;
        
        if (massCheckTimer >= MASS_CHECK_INTERVAL)
        {
            massCheckTimer = 0f;
            
            float currentTotalMass = CalculateTotalMassOptimized();
            
            if (Mathf.Abs(currentTotalMass - lastKnownMass) > 0.5f)
            {
                lastKnownMass = currentTotalMass;
                
                float requiredBuoyancy = currentTotalMass * Physics2D.gravity.magnitude * 2.0f;
                effectiveBuoyancyForce = Mathf.Max(buoyancyForce, requiredBuoyancy);
                maxTotalForce = Mathf.Max(50f, effectiveBuoyancyForce * 1.5f);
                
                if (debugMassChanges)
                {
                    Debug.Log($"MASS UPDATE: Total: {currentTotalMass:F2}, Buoyancy: {effectiveBuoyancyForce:F1}");
                }
            }
        }
    }
    
    public void RecalculateBuoyancy()
    {
        componentsCached = false;
        RefreshComponentCache();
        lastKnownMass = 0f;
        UpdateDynamicBuoyancyOptimized();
        
        if (debugMassChanges)
        {
            Debug.Log("BoatFloater: Manual buoyancy recalculation triggered with cache refresh");
        }
    }
}
