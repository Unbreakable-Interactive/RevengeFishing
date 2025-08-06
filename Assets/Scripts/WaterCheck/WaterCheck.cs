using UnityEngine;

public class WaterCheck : MonoBehaviour
{
    [Header("Water Detection")]
    public Entity entityMovement;
    public Collider2D targetCollider;
    
    [Header("Hook-Specific Behavior")]
    [SerializeField] private bool enableHookBasedTriggerControl = true;

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

        if (enableHookBasedTriggerControl && 
            entityMovement != null && 
            entityMovement.GetComponent<Player>() != null && 
            !entityMovement.IsAboveWater)
        {
            bool hasHooksFromThisEnemy = HasActiveHooksFromThisEnemy();
            GetComponent<Collider2D>().isTrigger = !hasHooksFromThisEnemy;
        }
    }

    private bool HasActiveHooksFromThisEnemy()
    {
        Player player = Player.Instance;
        if (player == null || player.activeBitingHooks == null || player.activeBitingHooks.Count == 0) 
            return false;

        Enemy ownerEnemy = GetOwnerEnemy();
        if (ownerEnemy == null) return false;

        foreach (FishingProjectile hook in player.activeBitingHooks)
        {
            if (IsHookFromEnemy(hook, ownerEnemy))
            {
                return true;
            }
        }

        return false;
    }

    private Enemy GetOwnerEnemy()
    {
        Transform current = transform.parent;
        while (current != null)
        {
            Enemy enemy = current.GetComponent<Enemy>();
            if (enemy != null)
            {
                return enemy;
            }
            
            LandEnemy landEnemy = current.GetComponent<LandEnemy>();
            if (landEnemy != null)
            {
                return landEnemy;
            }
            
            BoatLandEnemy boatEnemy = current.GetComponent<BoatLandEnemy>();
            if (boatEnemy != null)
            {
                return boatEnemy;
            }

            current = current.parent;
        }

        return null;
    }

    private bool IsHookFromEnemy(FishingProjectile hook, Enemy targetEnemy)
    {
        if (hook == null || hook.spawner == null || targetEnemy == null) return false;

        HookSpawner spawner = hook.spawner;
        Transform spawnerParent = spawner.transform;
        
        while (spawnerParent != null)
        {
            Enemy foundEnemy = spawnerParent.GetComponent<Enemy>();
            if (foundEnemy != null && foundEnemy == targetEnemy)
            {
                return true;
            }
            
            LandEnemy landEnemy = spawnerParent.GetComponent<LandEnemy>();
            if (landEnemy != null && landEnemy == targetEnemy)
            {
                return true;
            }
            
            spawnerParent = spawnerParent.parent;
        }

        return false;
    }

    public void SetHookBasedTriggerControl(bool enabled)
    {
        enableHookBasedTriggerControl = enabled;
    }
}
