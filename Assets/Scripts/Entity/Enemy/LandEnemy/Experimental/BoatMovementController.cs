using UnityEngine;

public class BoatMovementController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private bool debugMovement = true;
    
    private Rigidbody2D rb;
    
    public void Initialize(float walkingSpeed, float runningSpeed)
    {
        rb = GetComponent<Rigidbody2D>();
        
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
        
        if (debugMovement)
            Debug.Log($"BoatMovementController initialized on {gameObject.name}");
    }

    public void ApplyMovement(Vector2 characterMovement, bool isMoving)
    {
        if (rb == null) return;

        Vector2 boatVelocity = GetBoatVelocity();
        
        Vector2 finalMovement = characterMovement + boatVelocity;
        
        if (isMoving || boatVelocity != Vector2.zero)
        {
            Vector2 newPosition = rb.position + finalMovement * Time.fixedDeltaTime;
            rb.MovePosition(newPosition);
            
            if (debugMovement && isMoving)
            {
                Debug.Log($"BoatMovement: Character={characterMovement}, Boat={boatVelocity}, Final={finalMovement}");
            }
        }
    }

    private Vector2 GetBoatVelocity()
    {
        BoatFloater boatFloater = GetComponentInParent<BoatFloater>();
        if (boatFloater != null)
        {
            Rigidbody2D boatRb = boatFloater.GetComponent<Rigidbody2D>();
            if (boatRb != null)
            {
                return new Vector2(boatRb.velocity.x, 0f);
            }
        }
        return Vector2.zero;
    }

    public void StopMovement()
    {
        if (debugMovement)
            Debug.Log($"BoatMovementController: Movement stopped for {gameObject.name}");
    }
}
