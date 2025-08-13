using UnityEngine;

public class BoatCrewPhysics : MonoBehaviour
{
    [Header("Physics Settings")]
    [SerializeField] private float maxFallSpeed = 8f;
    [SerializeField] private float groundCheckDistance = 0.15f;
    
    [Header("Grounding")]
    [SerializeField] private LayerMask groundLayerMask = (1 << 5);
    [SerializeField] private bool isGrounded = false;
    
    [Header("Boat Sync")]
    [SerializeField] private float boatSyncMultiplier = 2f;
    [SerializeField] private float maxSyncForce = 100f;
    
    private Rigidbody2D rb;
    private BoatLandEnemy boatEnemy;
    public bool IsGrounded => isGrounded;
    
    public void Initialize(Rigidbody2D rigidbody, BoatLandEnemy enemy)
    {
        rb = rigidbody;
        boatEnemy = enemy;
        
        SetupCrewPhysics();
        
        GameLogger.LogError($"[CREW PHYSICS] {gameObject.name} - Dynamic physics system initialized");
    }
    
    private void SetupCrewPhysics()
    {
        if (rb != null && boatEnemy != null && boatEnemy.State == Enemy.EnemyState.Alive)
        {
            // rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }
    
    public void UpdatePhysics()
    {
        if (boatEnemy == null || boatEnemy.State != Enemy.EnemyState.Alive) return;
        
        CheckGrounded();
        ApplyPhysicsConstraints();
    }
    
    private void CheckGrounded()
    {
        if (rb == null) return;
        
        Vector2 rayOrigin = transform.position;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, groundCheckDistance, groundLayerMask);
        
        isGrounded = hit.collider != null;
    }
    
    private void ApplyPhysicsConstraints()
    {
        if (rb == null) return;
        
        if (rb.velocity.y < -maxFallSpeed)
        {
            Vector2 vel = rb.velocity;
            vel.y = -maxFallSpeed;
            rb.velocity = vel;
        }
        
        rb.angularVelocity = 0f;
        
        if (rb.velocity.magnitude > 12f)
        {
            rb.velocity = rb.velocity.normalized * 12f;
        }
    }
    
    public void ResetPhysics()
    {
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        
        isGrounded = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position, Vector3.down * groundCheckDistance);
    }
}
