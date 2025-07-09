using UnityEngine;

public class BoatFloater : MonoBehaviour
{
    [Header("Float Points")]
    public Transform[] floatPoints = new Transform[3];
    
    [Header("Buoyancy Settings")]
    [SerializeField] private float buoyancyForce = 6f;
    [SerializeField] private float waterDrag = 0.85f;
    [SerializeField] private float angularDrag = 0.7f;
    
    [Header("Wave Rolling Control")]
    [SerializeField] private float waveRollStrength = 2.5f;        // Main control for boat swaying
    [SerializeField] private float rollResponseSpeed = 1.5f;       // How fast boat responds to waves
    [SerializeField] private float maxRollAngle = 12f;             // Maximum tilt angle
    [SerializeField] private bool enableWaveRolling = true;        // Enable/disable rolling
    
    [Header("Stability")]
    [SerializeField] private float stabilityForce = 0.3f;          // Force to return to upright
    
    private Rigidbody2D rb;
    private WaterPhysics waterPhysics;
    private float currentRollAngle = 0f;
    private float rollVelocity = 0f;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        waterPhysics = WaterPhysics.Instance;
        
        if (floatPoints[0] == null || floatPoints[1] == null || floatPoints[2] == null)
        {
            Debug.LogError("BoatFloater: Float Points not assigned in inspector.");
        }
    }
    
    void FixedUpdate()
    {
        if (waterPhysics == null) return;
        
        ApplyBuoyancy();
        ApplyWaterResistance();
        
        if (enableWaveRolling)
        {
            ApplyWaveRolling();
        }
        
        ApplyStability();
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
                float force = submersion * buoyancyForce;
                totalForce += Vector2.up * force;
            }
        }
        
        if (submergedPoints > 0)
        {
            rb.AddForce(totalForce);
        }
    }
    
    void ApplyWaveRolling()
    {
        if (floatPoints.Length < 3) return;
        
        // Get water heights at bow and stern
        float bowHeight = waterPhysics.GetWaterHeightAt(floatPoints[0].position);      // Bow
        float sternHeight = waterPhysics.GetWaterHeightAt(floatPoints[2].position);   // Stern
        
        // Calculate height difference for rolling effect
        float heightDifference = bowHeight - sternHeight;
        
        // Calculate target roll angle based on wave difference
        float targetRollAngle = heightDifference * waveRollStrength;
        targetRollAngle = Mathf.Clamp(targetRollAngle, -maxRollAngle, maxRollAngle);
        
        // Smooth transition to target angle
        currentRollAngle = Mathf.SmoothDamp(
            currentRollAngle, 
            targetRollAngle, 
            ref rollVelocity, 
            1f / rollResponseSpeed, 
            Mathf.Infinity, 
            Time.fixedDeltaTime
        );
        
        // Apply torque based on angle difference
        float currentBoatAngle = transform.eulerAngles.z;
        if (currentBoatAngle > 180f) currentBoatAngle -= 360f;
        
        float angleDifference = Mathf.DeltaAngle(currentBoatAngle, currentRollAngle);
        float rollTorque = angleDifference * rollResponseSpeed;
        
        rb.AddTorque(rollTorque, ForceMode2D.Force);
    }
    
    void ApplyWaterResistance()
    {
        if (IsInWater())
        {
            rb.velocity *= waterDrag;
            rb.angularVelocity *= angularDrag;
        }
    }
    
    void ApplyStability()
    {
        // Simple force to keep boat relatively upright
        float currentAngle = transform.eulerAngles.z;
        if (currentAngle > 180f) currentAngle -= 360f;
        
        float stabilityTorque = -currentAngle * stabilityForce * Time.fixedDeltaTime;
        rb.AddTorque(stabilityTorque);
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
    
    void OnDrawGizmosSelected()
    {
        if (floatPoints != null)
        {
            Gizmos.color = Color.blue;
            foreach (Transform point in floatPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 0.1f);
                }
            }
            
            // Show roll angle indicator
            if (enableWaveRolling)
            {
                Gizmos.color = Color.yellow;
                Vector3 rollIndicator = transform.position + Vector3.up * currentRollAngle * 0.1f;
                Gizmos.DrawLine(transform.position, rollIndicator);
            }
        }
    }
}
