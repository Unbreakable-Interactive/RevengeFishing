using UnityEngine;

public class PlayerBounds : MonoBehaviour
{
    [Header("Boundary Settings")]
    public float bounceForce = 1.5f;
    public bool allowUpwardExit = true;

    private Transform playerTransform;
    private Rigidbody playerRb;
    private Player player;
    private BoxCollider boundsCollider;

    void Start()
    {
        boundsCollider = GetComponent<BoxCollider>();
        if (boundsCollider == null)
        {
            boundsCollider = gameObject.AddComponent<BoxCollider>();
        }
        boundsCollider.isTrigger = true;
    }

    public void Initialize(Player playerMovementScript)
    {
        if (playerMovementScript != null)
        {
            playerTransform = playerMovementScript.transform;
            playerRb = playerTransform.GetComponent<Rigidbody>();
            player = playerMovementScript;
            Debug.Log("PlayerBounds initialized with player reference");
        }
        else
        {
            Debug.LogError("PlayerBounds: No PlayerMovement provided!");
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Check if it's the player's collider (either direct or child)
        bool isPlayer = player != null && (other.transform == playerTransform || other.transform.IsChildOf(playerTransform));

        if (isPlayer && player != null && playerRb != null)
        {
            Vector3 playerPos = playerTransform.position;
            Bounds bounds = boundsCollider.bounds;

            // Allow upward exit for jumping
            if (allowUpwardExit && playerPos.y > bounds.max.y)
            {
                Debug.Log("Player jumped above bounds - allowed");
                return;
            }

            // Calculate bounce direction based on which boundary was crossed
            Vector3 bounceDirection = Vector3.zero;

            if (playerPos.x < bounds.min.x) // Left boundary
            {
                bounceDirection = Vector3.right;
                Debug.Log("Player hit left boundary");
            }
            else if (playerPos.x > bounds.max.x) // Right boundary  
            {
                bounceDirection = Vector3.left;
                Debug.Log("Player hit right boundary");
            }
            else if (playerPos.y < bounds.min.y) // Bottom boundary
            {
                bounceDirection = Vector3.up;
                Debug.Log("Player hit bottom boundary");
            }

            // Apply bounce
            if (bounceDirection != Vector3.zero)
            {
                Vector3 newVelocity = Vector3.Reflect(playerRb.velocity, bounceDirection);
                playerRb.velocity = newVelocity * bounceForce;

                // Handle rotation based on current movement mode
                HandleBounceRotation(newVelocity);

                Debug.Log($"Applied bounce: {newVelocity * bounceForce}");
            }
        }
    }

    void HandleBounceRotation(Vector3 newVelocity)
    {
        if (player.IsAboveWater)
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
                playerTransform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                Debug.Log($"Underwater bounce - rotated to face: {angle:F1}°");
            }
        }
    }

}
