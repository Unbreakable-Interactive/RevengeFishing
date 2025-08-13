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
    
    [Header("Dynamic Physics")]
    [SerializeField] private float physicsLifetime = 15f;
    [SerializeField] private float velocityThreshold = 0.1f;
    [SerializeField] private bool autoCleanup = true;
    
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private bool hasAppliedForce = false;
    private bool isInWaterMode = false;
    private bool isDynamicPhysicsActive = false;
    
    [SerializeField] private Rigidbody2D rb;
    private Collider2D partCollider;
    
    private float lastWaterCheckTime = 0f;
    private Vector2 cachedCheckPosition;
    private bool waterCheckEnabled = false;
    private Coroutine physicsLifetimeCoroutine;
    
    private void Awake()
    {
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
        
        rb = GetComponent<Rigidbody2D>();
        partCollider = GetComponent<Collider2D>();
    }
    
    public void ApplyInitialForces()
    {
        if (hasAppliedForce) return;
        
        EnableDynamicPhysics();
        
        if (rb == null)
        {
            if (debugFloatation)
            {
                GameLogger.LogWarning($"BoatPart {gameObject.name} - No Rigidbody2D found after enabling dynamic physics");
            }
            return;
        }
        
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
        
        if (autoCleanup)
        {
            physicsLifetimeCoroutine = StartCoroutine(PhysicsLifetimeManager());
        }
        
        if (debugFloatation)
        {
            GameLogger.LogVerbose($"BoatPart {gameObject.name} - Applied destruction forces: {explosionForce * forceMultiplier}");
        }
    }
    
    private void EnableDynamicPhysics()
    {
        if (isDynamicPhysicsActive) return;
        
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.freezeRotation = false;
        rb.mass = Random.Range(0.3f, 0.8f);
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.angularVelocity = 0f;
        rb.velocity = Vector2.zero;
        
        if (partCollider != null)
        {
            partCollider.isTrigger = false;
        }
        
        isDynamicPhysicsActive = true;
        
        if (debugFloatation)
        {
            GameLogger.LogVerbose($"BoatPart {gameObject.name} - Dynamic physics enabled with mass {rb.mass}");
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
    
    private IEnumerator PhysicsLifetimeManager()
    {
        float startTime = Time.time;
        
        while (Time.time - startTime < physicsLifetime)
        {
            yield return new WaitForSeconds(1f);
            
            if (rb != null && rb.velocity.magnitude < velocityThreshold && isInWaterMode)
            {
                yield return new WaitForSeconds(2f);
                
                if (rb != null && rb.velocity.magnitude < velocityThreshold)
                {
                    if (debugFloatation)
                    {
                        GameLogger.LogVerbose($"BoatPart {gameObject.name} - Early cleanup due to low velocity");
                    }
                    break;
                }
            }
        }
        
        DisableDynamicPhysics();
    }
    
    private void FixedUpdate()
    {
        if (!waterCheckEnabled || !hasAppliedForce || !enableBuoyancy || WaterPhysics.Instance == null || rb == null) 
            return;
        
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
        
        float waterHeight = WaterPhysics.Instance.GetWaterHeightAt(cachedCheckPosition);
        bool shouldBeInWater = waterHeight > cachedCheckPosition.y;
        
        if (shouldBeInWater != isInWaterMode)
        {
            SetPhysicsMode(!shouldBeInWater);
            
            if (shouldBeInWater && !isInWaterMode)
            {
                StartCoroutine(SinkingBehavior());
            }
        }
    }
    
    private IEnumerator SinkingBehavior()
    {
        yield return new WaitForSeconds(3f);
        
        if (rb != null && isInWaterMode)
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                float fadeTime = 2f;
                float elapsed = 0f;
                Color originalColor = spriteRenderer.color;
                
                while (elapsed < fadeTime)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                    spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                    
                    transform.position += Vector3.down * (Time.deltaTime * 0.5f);
                    
                    yield return null;
                }
            }
            
            DisableDynamicPhysics();
        }
    }
    
    private void DisableDynamicPhysics()
    {
        if (!isDynamicPhysicsActive) return;
        
        if (physicsLifetimeCoroutine != null)
        {
            StopCoroutine(physicsLifetimeCoroutine);
        }
        
        StopAllCoroutines();
        
        if (rb != null)
        {
            DestroyImmediate(rb);
            rb = null;
        }
        
        if (partCollider != null)
        {
            partCollider.isTrigger = true;
        }
        
        isDynamicPhysicsActive = false;
        waterCheckEnabled = false;
        
        gameObject.SetActive(false);
        
        if (debugFloatation)
        {
            GameLogger.LogVerbose($"BoatPart {gameObject.name} - Dynamic physics disabled and part deactivated");
        }
    }
    
    public void ResetToOriginalPosition()
    {
        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;
        
        if (rb != null)
        {
            if (isDynamicPhysicsActive)
            {
                DestroyImmediate(rb);
                rb = null;
            }
        }
        
        if (partCollider != null)
        {
            partCollider.isTrigger = false;
        }
        
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;
        }
        
        hasAppliedForce = false;
        isInWaterMode = false;
        isDynamicPhysicsActive = false;
        waterCheckEnabled = false;
        
        if (physicsLifetimeCoroutine != null)
        {
            StopCoroutine(physicsLifetimeCoroutine);
            physicsLifetimeCoroutine = null;
        }
        
        StopAllCoroutines();
        
        gameObject.SetActive(true);
        
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
    
    public void ForceDisablePhysics()
    {
        DisableDynamicPhysics();
    }
    
    public bool IsPhysicsActive()
    {
        return isDynamicPhysicsActive && rb != null;
    }
    
    private void OnDestroy()
    {
        if (physicsLifetimeCoroutine != null)
        {
            StopCoroutine(physicsLifetimeCoroutine);
        }
    }
}
