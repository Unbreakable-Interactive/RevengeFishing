using UnityEngine;

public class WaterCheck : MonoBehaviour
{
    [Header("Water Detection")]
    public LayerMask playerLayerMask = -1;
    public PlayerMovement playerMovement;

    private void Start()
    {
        // Ensure this collider is set as a trigger
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (playerMovement != null)
        {
            // Determine if player is above or below the water line
            bool isAboveWater = other.transform.position.y > transform.position.y;

            // Update the player's movement mode
            playerMovement.SetMovementMode(isAboveWater);

            Debug.Log($"Player {(isAboveWater ? "jumped out of" : "dove into")} water!");
        }
    }
}
