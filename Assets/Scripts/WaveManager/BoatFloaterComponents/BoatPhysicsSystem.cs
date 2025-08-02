using UnityEngine;

public class BoatPhysicsSystem : MonoBehaviour
{
    [Header("Force Settings")]
    [SerializeField] private float horizontalForce = 0f;
    [SerializeField] private float maxHorizontalForce = 5f;
    
    [Header("Water Resistance")]
    [SerializeField] private float waterDrag = 0.85f;
    [SerializeField] private float angularDrag = 0.7f;
    
    [Header("Force Protection")]
    [SerializeField] private float maxTotalForce = 50f;
    [SerializeField] private float maxAngularVelocity = 180f;
    [SerializeField] private float forceSmoothing = 0.85f;
    [SerializeField] private bool enableForceProtection = true;
    
    [Header("Vertical Control")]
    [SerializeField] private float maxVerticalSpeed = 3f;
    [SerializeField] private float verticalDamping = 0.8f;
    [SerializeField] private bool enableSpeedLimit = true;
    
    [Header("Wave Rolling")]
    [SerializeField] private float waveRollStrength = 2.5f;
    [SerializeField] private float rollResponseSpeed = 1.5f;
    [SerializeField] private float maxRollAngle = 12f;
    [SerializeField] private float maxTorqueLimit = 8f;
    [SerializeField] private bool enableWaveRolling = true;
    
    [Header("Stability")]
    [SerializeField] private float stabilityForce = 0.3f;
    
    private Rigidbody2D rb;
    private WaterPhysics waterPhysics;
    private Transform[] floatPoints;
    
    // Wave rolling state
    private float currentRollAngle = 0f;
    private float rollVelocity = 0f;
    private Vector2 smoothedForce = Vector2.zero;
    private float smoothedTorque = 0f;
    private Vector2 previousVelocity;
    
    // Visual system reference for direction
    private BoatVisualSystem visualSystem;
    
    public void Initialize(Rigidbody2D rigidbody, WaterPhysics physics)
    {
        rb = rigidbody;
        waterPhysics = physics;
        visualSystem = GetComponent<BoatVisualSystem>();
        
        BoatFloater floater = GetComponent<BoatFloater>();
        if (floater != null)
        {
            floatPoints = floater.floatPoints;
        }
    }
    
    public void UpdatePhysics()
    {
        previousVelocity = rb.velocity;
        
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
    
    private void ApplyHorizontalForce()
    {
        if (Mathf.Abs(horizontalForce) > 0.01f && IsInWater())
        {
            float effectiveForce = Mathf.Clamp(horizontalForce, -maxHorizontalForce, maxHorizontalForce);
            
            if (visualSystem != null)
            {
                effectiveForce *= visualSystem.GetCurrentDirectionMultiplier();
            }
            
            Vector2 horizontalForceVector = Vector2.right * effectiveForce;
            smoothedForce = Vector2.Lerp(smoothedForce, horizontalForceVector, (1f - forceSmoothing) * Time.fixedDeltaTime * 10f);
            rb.AddForce(smoothedForce);
        }
    }
    
    private void ApplyWaterResistance()
    {
        if (IsInWater())
        {
            float effectiveDrag = waterDrag;
            float effectiveAngularDrag = angularDrag;
            
            if (visualSystem != null && visualSystem.GetCurrentDirectionMultiplier() < 0)
            {
                effectiveDrag *= 1.1f;
                effectiveAngularDrag *= 1.2f;
            }
            
            rb.velocity *= effectiveDrag;
            rb.angularVelocity *= effectiveAngularDrag;
        }
    }
    
    private void ApplyWaveRolling()
    {
        if (floatPoints == null || floatPoints.Length < 3) return;
        
        float bowHeight = waterPhysics.GetWaterHeightAt(floatPoints[0].position);
        float sternHeight = waterPhysics.GetWaterHeightAt(floatPoints[2].position);
        
        float heightDifference = bowHeight - sternHeight;
        
        if (visualSystem != null && visualSystem.GetCurrentDirectionMultiplier() < 0)
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
    
    private void ApplyStability()
    {
        float currentAngle = transform.eulerAngles.z;
        if (currentAngle > 180f) currentAngle -= 360f;
        
        float stabilityTorque = -currentAngle * stabilityForce * Time.fixedDeltaTime;
        
        if (visualSystem != null && visualSystem.GetCurrentDirectionMultiplier() < 0)
        {
            stabilityTorque *= 1.3f;
        }
        
        if (enableForceProtection)
        {
            stabilityTorque = Mathf.Clamp(stabilityTorque, -maxTorqueLimit * 0.5f, maxTorqueLimit * 0.5f);
        }
        
        rb.AddTorque(stabilityTorque);
    }
    
    private void LimitVerticalMovement()
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
    
    private void ApplyForceProtection()
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
    
    private bool IsInWater()
    {
        if (floatPoints == null) return false;
        
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

    #region Public Methods

    public void SetHorizontalForce(float force)
    {
        horizontalForce = Mathf.Clamp(force, -maxHorizontalForce, maxHorizontalForce);
    }
    
    public void AddHorizontalForce(float additionalForce)
    {
        float newForce = horizontalForce + additionalForce;
        SetHorizontalForce(newForce);
    }

    #endregion
}
