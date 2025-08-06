using UnityEngine;

public class WaterCheck : MonoBehaviour
{
    [Header("Water Detection")]
    public Entity entityMovement;
    public Collider2D targetCollider;

    private void Start()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other != targetCollider) return;

        bool aboveWater = other.transform.position.y > transform.position.y;

        if (entityMovement != null)
        {
            entityMovement.SetMovementMode(aboveWater);
            GameLogger.LogVerbose($"{other.name} {(aboveWater ? "exited" : "entered")} water!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other != targetCollider) return;

        bool aboveWater = other.transform.position.y > transform.position.y;

        if (entityMovement != null && entityMovement.GetComponent<Player>() != null && !entityMovement.IsAboveWater)
        {
            GetComponent<Collider2D>().isTrigger = entityMovement.GetComponent<Player>().activeBitingHooks.Count <= 0;
        }
    }
}
