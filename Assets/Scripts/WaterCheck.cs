using UnityEngine;

public class WaterCheck : MonoBehaviour
{
    [Header("Water Detection")]
    public LayerMask playerLayerMask = -1;
    public EntityMovement entityMovement; // Reference to the target entity movement script

    private void Start()
    {
        // Ensure this collider is set as a trigger
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Determine if object is above or below the water line
        bool isAboveWater = other.transform.position.y > transform.position.y;

        // Update the object's water state
        entityMovement.IsAboveWater = isAboveWater;

        entityMovement.SetMovementMode(isAboveWater);

        Debug.Log($"{other.name} {(isAboveWater ? "exited" : "entered")} water!");
    }
}
