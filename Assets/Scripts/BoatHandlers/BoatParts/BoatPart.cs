using UnityEngine;
using System.Collections;

public class BoatPart : MonoBehaviour
{
    [Header("Initial Impact Forces")]
    [SerializeField] private float forceMultiplier = 8f;
    [SerializeField] private float torqueMultiplier = 5f;
    
    [Header("Physics Settings")]
    [SerializeField] private float airGravityScale = 2f;
    [SerializeField] private float airDrag = 1.5f;
    [SerializeField] private float underwaterGravityScale = 0f;
    [SerializeField] private float underwaterDrag = 0.5f;
    [SerializeField] private float buoyancyStartDelay = 1.5f;
    
    [Header("Performance Optimization")]
    [SerializeField] private float waterCheckInterval = 0.2f;
    [SerializeField] private bool enableBuoyancy = true;
    [SerializeField] private bool debugFloatation = false;
    
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private bool hasAppliedForce = false;
    private bool isInWaterMode = false;
    
    [SerializeField] private Rigidbody2D rb;
    private WaterPhysics waterPhysics;
    
    private float lastWaterCheckTime = 0f;
    private Vector2 cachedCheckPosition;
    private bool waterCheckEnabled = false;
    
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
            StartCoroutine(StartOptimizedWaterDetection());
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
    
    private IEnumerator StartOptimizedWaterDetection()
    {
        yield return new WaitForSeconds(buoyancyStartDelay);
        
        waterCheckEnabled = true;
        lastWaterCheckTime = Time.time;
        
        if (debugFloatation)
        {
            GameLogger.LogVerbose($"BoatPart {gameObject.name} - Optimized water detection activated");
        }
    }
    
    private void FixedUpdate()
    {
        if (!waterCheckEnabled || !hasAppliedForce || !enableBuoyancy || waterPhysics == null) return;
        
        float currentTime = Time.time;
        if (currentTime - lastWaterCheckTime >= waterCheckInterval)
        {
            CheckWaterStatusOptimized();
            lastWaterCheckTime = currentTime;
        }
    }
    
    private void CheckWaterStatusOptimized()
    {
        cachedCheckPosition = transform.position;
        cachedCheckPosition.y -= 0.1f;
        
        float waterHeight = waterPhysics.GetWaterHeightAt(cachedCheckPosition);
        bool shouldBeInWater = waterHeight > cachedCheckPosition.y;
        
        if (shouldBeInWater != isInWaterMode)
        {
            SetPhysicsMode(!shouldBeInWater);
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
        waterCheckEnabled = false;
        
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
            waterCheckEnabled = false;
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
}
