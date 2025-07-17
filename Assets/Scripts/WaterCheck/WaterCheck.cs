using UnityEngine;
using static Enemy;

public class WaterCheck : MonoBehaviour
{
    [Header("Water Detection")]
    //public LayerMask playerLayerMask = -1;
    public Entity entityMovement; // Reference to the target entity movement script
    public Collider2D targetCollider;

    private void Start()
    {
        // Ensure this collider is set as a trigger
        GetComponent<Collider2D>().isTrigger = true;
        
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other != targetCollider) return;

        // Determine if object is above or below the water line
        bool aboveWater = other.transform.position.y > transform.position.y;

        if (entityMovement != null)
        {
            entityMovement.SetMovementMode(aboveWater);
            Debug.Log($"{other.name} {(aboveWater ? "exited" : "entered")} water!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other != targetCollider) return;

        // Determine if object is above or below the water line
        bool aboveWater = other.transform.position.y > transform.position.y;

        if (entityMovement != null && entityMovement.GetComponent<Player>().activeBitingHooks != null && !entityMovement.IsAboveWater)
        {
            GetComponent<Collider2D>().isTrigger = entityMovement.GetComponent<Player>().activeBitingHooks.Count <= 0;
        }
    }
}
