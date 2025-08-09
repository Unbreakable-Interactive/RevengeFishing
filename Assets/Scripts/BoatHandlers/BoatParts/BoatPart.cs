using UnityEngine;
using System.Collections;

public class BoatPart : MonoBehaviour
{
    [Header("Initial Impact Forces")]
    [SerializeField] private float forceMultiplier = 8f;
    [SerializeField] private float torqueMultiplier = 5f;
    
    [Header("Physics Settings - Match DroppedTool")]
    [SerializeField] private float airGravityScale = 2f;
    [SerializeField] private float airDrag = 1.5f;
    [SerializeField] private float underwaterGravityScale = 0f;
    [SerializeField] private float underwaterDrag = 0.5f;
    [SerializeField] private float buoyancyStartDelay = 1.5f;
    
    [Header("Water Detection")]
    [SerializeField] private float waterDetectionOffset = 0.1f;
    [SerializeField] private bool enableBuoyancy = true;
    [SerializeField] private bool debugFloatation = false;
    
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private bool hasAppliedForce = false;
    private bool isInWaterMode = false;
    
    [SerializeField] private Rigidbody2D rb;
    private WaterPhysics waterPhysics;
    
    private void Awake()
    {
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
        
        if(rb == null)
            rb = GetComponent<Rigidbody2D>();
            
        waterPhysics = WaterPhysics.Instance;
    }
    
    public void ApplyInitialForces()
    {
        if (rb == null || hasAppliedForce) return;
        
        SetPhysicsMode(true);
        
        Vector2 explosionForce = new Vector2(
            Random.Range(-2f, 2f),
            Random.Range(-0.5f, 1.5f) 
        );
        
        rb.AddForce(explosionForce * forceMultiplier, ForceMode2D.Impulse);
        rb.AddTorque(Random.Range(-torqueMultiplier, torqueMultiplier), ForceMode2D.Impulse);
        
        hasAppliedForce = true;
        
        if (enableBuoyancy)
        {
            StartCoroutine(StartWaterDetectionAfterDelay());
        }
        
        if (debugFloatation)
        {
            GameLogger.LogVerbose($"BoatPart {gameObject.name} - Applied destruction forces: {explosionForce * forceMultiplier}");
        }
    }
    
    private void SetPhysicsMode(bool aboveWater)
    {
        if (rb == null) return;
        
        if (aboveWater)
        {
            rb.gravityScale = airGravityScale;
            rb.drag = airDrag;
            isInWaterMode = false;
        }
        else
        {
            rb.gravityScale = underwaterGravityScale;
            rb.drag = underwaterDrag;
            isInWaterMode = true;
        }
        
        if (debugFloatation)
        {
            string mode = aboveWater ? "AIR" : "WATER";
            GameLogger.LogVerbose($"BoatPart {gameObject.name} - Physics mode: {mode} (gravity: {rb.gravityScale}, drag: {rb.drag})");
        }
    }
    
    private IEnumerator StartWaterDetectionAfterDelay()
    {
        yield return new WaitForSeconds(buoyancyStartDelay);
        
        if (debugFloatation)
        {
            GameLogger.LogVerbose($"BoatPart {gameObject.name} - Water detection activated");
        }
    }
    
    private void FixedUpdate()
    {
        if (hasAppliedForce && enableBuoyancy && waterPhysics != null)
        {
            CheckWaterStatus();
        }
    }
    
    private void CheckWaterStatus()
    {
        Vector2 checkPosition = (Vector2)transform.position + Vector2.down * waterDetectionOffset;
        float waterHeight = waterPhysics.GetWaterHeightAt(checkPosition);
        bool shouldBeInWater = waterHeight > checkPosition.y;
        
        if (shouldBeInWater && !isInWaterMode)
        {
            SetPhysicsMode(false);
        }
        else if (!shouldBeInWater && isInWaterMode)
        {
            SetPhysicsMode(true);
        }
    }
    
    public void ResetToOriginalPosition()
    {
        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;
        
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 1f;
            rb.drag = 0f;
        }
        
        hasAppliedForce = false;
        isInWaterMode = false;
        
        StopAllCoroutines();
        
        if (debugFloatation)
        {
            GameLogger.LogVerbose($"BoatPart {gameObject.name} - Reset to original position");
        }
    }
    
    public void SetBuoyancyEnabled(bool enabled)
    {
        enableBuoyancy = enabled;
        if (!enabled)
        {
            SetPhysicsMode(true);
        }
    }
    
    [ContextMenu("Test Apply Forces")]
    public void TestApplyForces()
    {
        hasAppliedForce = false;
        ApplyInitialForces();
    }
    
    [ContextMenu("Test Reset Position")]
    public void TestResetPosition()
    {
        ResetToOriginalPosition();
    }
    
    [ContextMenu("Test Toggle Buoyancy")]
    public void TestToggleBuoyancy()
    {
        SetBuoyancyEnabled(!enableBuoyancy);
        GameLogger.LogVerbose($"BoatPart {gameObject.name} - Buoyancy: {enableBuoyancy}");
    }
    
    [ContextMenu("Force Water Mode")]
    public void ForceWaterMode()
    {
        SetPhysicsMode(false);
    }
    
    [ContextMenu("Force Air Mode")]
    public void ForceAirMode()
    {
        SetPhysicsMode(true);
    }
}

