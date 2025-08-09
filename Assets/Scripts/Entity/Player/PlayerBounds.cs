using UnityEngine;

public class PlayerBounds : MonoBehaviour
{
    [Header("Boundary Settings")]
    public float bounceForce = 1.5f;
    public bool allowUpwardExit = true;

    private Transform playerTransform;
    private Rigidbody2D playerRb;
    private Player player;
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

    public void Initialize(Player playerMovementScript)
    {
        if (playerMovementScript != null)
        {
            playerTransform = playerMovementScript.transform;
            playerRb = playerTransform.GetComponent<Rigidbody2D>();
            player = playerMovementScript;
            GameLogger.Log("PlayerBounds initialized with player reference");
        }
        else
        {
            GameLogger.LogError("PlayerBounds: No PlayerMovement provided!");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        // Check if it's the player's collider (either direct or child)
        bool isPlayer = player != null && (other.transform == playerTransform || other.transform.IsChildOf(playerTransform));

        if (isPlayer && player != null && playerRb != null)
        {
            Vector2 playerPos = playerTransform.position;
            Bounds bounds = boundsCollider.bounds;

            // Allow upward exit for jumping
            if (allowUpwardExit && playerPos.y > bounds.max.y)
            {
                GameLogger.LogVerbose("Player jumped above bounds - allowed");
                return;
            }

            // Calculate bounce direction based on which boundary was crossed
            Vector2 bounceDirection = Vector2.zero;

            if (playerPos.x < bounds.min.x) // Left boundary
            {
                bounceDirection = Vector2.right;
                GameLogger.LogVerbose("Player hit left boundary");
            }
            else if (playerPos.x > bounds.max.x) // Right boundary  
            {
                bounceDirection = Vector2.left;
                GameLogger.LogVerbose("Player hit right boundary");
            }
            else if (playerPos.y < bounds.min.y) // Bottom boundary
            {
                bounceDirection = Vector2.up;
                GameLogger.LogVerbose("Player hit bottom boundary");
            }

            // Apply bounce
            if (bounceDirection != Vector2.zero)
            {
                Vector2 newVelocity = Vector2.Reflect(playerRb.velocity, bounceDirection);
                playerRb.velocity = newVelocity * bounceForce;

                // Handle rotation based on current movement mode
                HandleBounceRotation(newVelocity);

                GameLogger.LogVerbose($"Applied bounce: {newVelocity * bounceForce}");
            }
        }
    }

    void HandleBounceRotation(Vector2 newVelocity)
    {
        if (player.IsAboveWater)
        {
            // In air: The HandleAirborneRotation() will automatically rotate to face velocity
            // No need to override - it will happen automatically next frame
            GameLogger.LogVerbose("Airborne bounce - auto-rotation will handle this");
        }
        else
        {
            // Underwater: Manually rotate to face the new direction
            if (newVelocity.magnitude > 0.1f)
            {
                float angle = Mathf.Atan2(newVelocity.y, newVelocity.x) * Mathf.Rad2Deg;
                playerTransform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                GameLogger.LogVerbose($"Underwater bounce - rotated to face: {angle:F1}°");
            }
        }
    }
}
