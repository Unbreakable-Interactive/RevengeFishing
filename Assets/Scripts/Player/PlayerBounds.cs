using UnityEngine;

public class PlayerBounds : MonoBehaviour
{
    [Header("Boundary Settings")]
    public float bounceForce = 1.5f;
    public bool allowUpwardExit = true;

    private Transform player;
    private Rigidbody2D playerRb;
    private PlayerMovement playerMovement;
    private BoxCollider2D boundsCollider;

    void Start()
    {
        boundsCollider = GetComponent<BoxCollider2D>();
        if (boundsCollider == null)
        {
            boundsCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        boundsCollider.isTrigger = true;
    }

    public void Initialize(PlayerMovement playerMovementScript)
    {
        if (playerMovementScript != null)
        {
            player = playerMovementScript.transform;
            playerRb = player.GetComponent<Rigidbody2D>();
            playerMovement = playerMovementScript;
            Debug.Log("PlayerBounds initialized with player reference");
        }
        else
        {
            Debug.LogError("PlayerBounds: No PlayerMovement provided!");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        // Check if it's the player's collider (either direct or child)
        bool isPlayer = player != null && (other.transform == player || other.transform.IsChildOf(player));

        if (isPlayer && player != null && playerRb != null)
        {
            Vector2 playerPos = player.position;
            Bounds bounds = boundsCollider.bounds;

            // Allow upward exit for jumping
            if (allowUpwardExit && playerPos.y > bounds.max.y)
            {
                Debug.Log("Player jumped above bounds - allowed");
                return;
            }

            // Calculate bounce direction based on which boundary was crossed
            Vector2 bounceDirection = Vector2.zero;

            if (playerPos.x < bounds.min.x) // Left boundary
            {
                bounceDirection = Vector2.right;
                Debug.Log("Player hit left boundary");
            }
            else if (playerPos.x > bounds.max.x) // Right boundary  
            {
                bounceDirection = Vector2.left;
                Debug.Log("Player hit right boundary");
            }
            else if (playerPos.y < bounds.min.y) // Bottom boundary
            {
                bounceDirection = Vector2.up;
                Debug.Log("Player hit bottom boundary");
            }

            // Apply bounce
            if (bounceDirection != Vector2.zero)
            {
                Vector2 newVelocity = Vector2.Reflect(playerRb.velocity, bounceDirection);
                playerRb.velocity = newVelocity * bounceForce;

                // Handle rotation based on current movement mode
                HandleBounceRotation(newVelocity);

                Debug.Log($"Applied bounce: {newVelocity * bounceForce}");
            }
        }
    }

    void HandleBounceRotation(Vector2 newVelocity)
    {
        if (playerMovement.isAboveWater)
        {
            // In air: The HandleAirborneRotation() will automatically rotate to face velocity
            // No need to override - it will happen automatically next frame
            Debug.Log("Airborne bounce - auto-rotation will handle this");
        }
        else
        {
            // Underwater: Manually rotate to face the new direction
            if (newVelocity.magnitude > 0.1f)
            {
                float angle = Mathf.Atan2(newVelocity.y, newVelocity.x) * Mathf.Rad2Deg;
                player.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                Debug.Log($"Underwater bounce - rotated to face: {angle:F1}°");
            }
        }
    }
}
