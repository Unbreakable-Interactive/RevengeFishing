using UnityEngine;

public class BoatCrewPhysics : MonoBehaviour
{
    [Header("Physics Settings")]
    [SerializeField] private float maxFallSpeed = 8f;
    [SerializeField] private float groundCheckDistance = 0.15f;
    
    [Header("Grounding - Only for Land Mode")]
    [SerializeField] private LayerMask groundLayerMask = (1 << 5);
    [SerializeField] private bool isGrounded = false;
    [SerializeField] private bool isInBoatMode = false;
    
    [Header("Physics Constraints")]
    [SerializeField] private float maxHorizontalSpeed = 5f;
    [SerializeField] private float maxTotalSpeed = 12f;
    [SerializeField] private float waterDamping = 0.8f;
    
    [Header("Debug")]
    [SerializeField] private bool debugPhysics = false;
    
    private Rigidbody2D rb;
    private BoatLandEnemy boatEnemy;
    private Transform crewContainer;
    private Transform originalParent;
    private Vector3 originalLocalPosition;
    private bool physicsInitialized = false;
    private bool isParentedToBoat = false;
    
    public bool IsGrounded => isGrounded;
    public bool IsInBoatMode => isInBoatMode;
    public bool IsParentedToBoat => isParentedToBoat;
    
    private void Start()
    {
        if (!physicsInitialized)
        {
            rb = GetComponent<Rigidbody2D>();
            boatEnemy = GetComponent<BoatLandEnemy>();
            
            if (rb != null && boatEnemy != null)
            {
                Initialize(rb, boatEnemy);
            }
        }
    }
    
    public void Initialize(Rigidbody2D rigidbody, BoatLandEnemy enemy)
    {
        rb = rigidbody;
        boatEnemy = enemy;
        physicsInitialized = true;
        
        originalLocalPosition = transform.localPosition;
        
        SetupCrewPhysics();
        
        if (debugPhysics)
            GameLogger.LogVerbose($"[CREW PHYSICS] {gameObject.name} - Physics system initialized");
    }
    
    public void SetupAtPosition(Transform container, Vector3 localPosition)
    {
        if (container != null && !isParentedToBoat)
        {
            originalParent = transform.parent;
            crewContainer = container;
            
            transform.SetParent(crewContainer);
            transform.localPosition = localPosition;
            
            SetBoatMode(true);
            
            isParentedToBoat = true;
            
            if (debugPhysics)
                GameLogger.LogVerbose($"[CREW PHYSICS] {gameObject.name} - Setup at position: {localPosition}");
        }
    }
    
    public void SetupAsChildHandler(Transform container, Transform handlerTransform, Vector3 handlerLocalPosition)
    {
        crewContainer = container;
        isParentedToBoat = true;
        
        if (handlerTransform != null)
        {
            handlerTransform.SetParent(crewContainer);
            handlerTransform.localPosition = handlerLocalPosition;
        }
        
        SetBoatMode(true);
        
        if (debugPhysics)
            GameLogger.LogVerbose($"[CREW PHYSICS] {gameObject.name} - Setup as child handler at: {handlerLocalPosition}");
    }
    
    public void SetBoatMode(bool onBoat)
    {
        isInBoatMode = onBoat;
        
        if (isInBoatMode)
        {
            isGrounded = true;
            
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.freezeRotation = true;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.gravityScale = 0f;
                rb.drag = 0f;
                rb.mass = 0.8f;
                rb.angularDrag = 2f;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            }
        }
        else
        {
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.freezeRotation = false;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.gravityScale = 1f;
                rb.drag = 0.5f;
            }
        }
        
        if (debugPhysics)
            GameLogger.LogVerbose($"[CREW PHYSICS] {gameObject.name} - Boat mode: {onBoat}");
    }
    
    public void LeaveBoat()
    {
        if (isParentedToBoat)
        {
            if (originalParent != null)
            {
                transform.SetParent(originalParent);
            }
            else
            {
                transform.SetParent(null);
            }
            
            SetBoatMode(false);
            
            isParentedToBoat = false;
            
            if (debugPhysics)
                GameLogger.LogVerbose($"[CREW PHYSICS] {gameObject.name} - Left boat crew, unparented");
        }
    }
    
    public void SetLocalPosition(Vector3 localPosition)
    {
        if (isParentedToBoat)
        {
            transform.localPosition = localPosition;
        }
    }
    
    private void SetupCrewPhysics()
    {
        if (rb != null && boatEnemy != null && boatEnemy.State == Enemy.EnemyState.Alive)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }
    
    private void FixedUpdate()
    {
        if (!physicsInitialized || boatEnemy == null) return;
        
        UpdatePhysics();
    }
    
    public void UpdatePhysics()
    {
        if (boatEnemy == null || boatEnemy.State != Enemy.EnemyState.Alive) return;
        
        if (!isInBoatMode)
        {
            CheckGrounded();
            ApplyPhysicsConstraints();
        }
        else
        {
            ApplyBoatPhysicsConstraints();
        }
    }
    
    private void CheckGrounded()
    {
        if (rb == null || isInBoatMode) return;
        
        Vector2 rayOrigin = transform.position;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, groundCheckDistance, groundLayerMask);
        
        bool wasGrounded = isGrounded;
        isGrounded = hit.collider != null;
        
        if (debugPhysics && wasGrounded != isGrounded)
            GameLogger.LogVerbose($"[CREW PHYSICS] {gameObject.name} - Grounded state changed: {isGrounded}");
    }
    
    private void ApplyPhysicsConstraints()
    {
        if (rb == null || isInBoatMode) return;
        
        Vector2 velocity = rb.velocity;
        
        if (velocity.y < -maxFallSpeed)
        {
            velocity.y = -maxFallSpeed;
        }
        
        if (Mathf.Abs(velocity.x) > maxHorizontalSpeed)
        {
            velocity.x = Mathf.Sign(velocity.x) * maxHorizontalSpeed;
        }
        
        if (velocity.magnitude > maxTotalSpeed)
        {
            velocity = velocity.normalized * maxTotalSpeed;
        }
        
        rb.velocity = velocity;
        
        if (rb.angularVelocity > 360f)
        {
            rb.angularVelocity = 360f;
        }
        else if (rb.angularVelocity < -360f)
        {
            rb.angularVelocity = -360f;
        }
    }
    
    private void ApplyBoatPhysicsConstraints()
    {
        if (rb == null || !isInBoatMode) return;
        
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }
    
    public void ApplyWaterDamping()
    {
        if (rb == null || isInBoatMode) return;
        
        Vector2 velocity = rb.velocity;
        velocity *= waterDamping;
        rb.velocity = velocity;
        
        rb.angularVelocity *= waterDamping;
        
        if (debugPhysics)
            GameLogger.LogVerbose($"[CREW PHYSICS] {gameObject.name} - Water damping applied");
    }
    
    public void ForceStopMovement()
    {
        if (rb == null) return;
        
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        
        if (debugPhysics)
            GameLogger.LogVerbose($"[CREW PHYSICS] {gameObject.name} - Movement force stopped");
    }
    
    public void ApplyImpulseForce(Vector2 force)
    {
        if (rb == null || isInBoatMode) return;
        
        rb.AddForce(force, ForceMode2D.Impulse);
        
        if (debugPhysics)
            GameLogger.LogVerbose($"[CREW PHYSICS] {gameObject.name} - Impulse force applied: {force}");
    }
    
    public void ResetToOriginalState()
    {
        transform.localPosition = originalLocalPosition;
        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
        
        SetBoatMode(true);
        
        if (debugPhysics)
            GameLogger.LogVerbose($"[CREW PHYSICS] {gameObject.name} - Reset to original state");
    }
    
    public void ResetPhysics()
    {
        if (rb == null) return;
        
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 1f;
        rb.drag = 0.5f;
        rb.freezeRotation = false;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        isInBoatMode = false;
        isGrounded = false;
        isParentedToBoat = false;
        physicsInitialized = false;
        
        if (debugPhysics)
            GameLogger.LogVerbose($"[CREW PHYSICS] {gameObject.name} - Physics reset to defaults");
    }
    
    public Vector2 GetVelocity()
    {
        return rb != null ? rb.velocity : Vector2.zero;
    }
    
    public float GetAngularVelocity()
    {
        return rb != null ? rb.angularVelocity : 0f;
    }
    
    public bool IsMoving()
    {
        return rb != null && rb.velocity.magnitude > 0.1f;
    }
    
    public bool IsPhysicsActive()
    {
        return rb != null && rb.bodyType == RigidbodyType2D.Dynamic;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!debugPhysics) return;
        
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 rayStart = transform.position;
        Vector3 rayEnd = rayStart + Vector3.down * groundCheckDistance;
        Gizmos.DrawLine(rayStart, rayEnd);
        
        if (isInBoatMode)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
        
        if (rb != null && rb.velocity.magnitude > 0.1f)
        {
            Gizmos.color = Color.yellow;
            Vector3 velocityEnd = transform.position + (Vector3)rb.velocity * 0.5f;
            Gizmos.DrawLine(transform.position, velocityEnd);
        }
    }
}
