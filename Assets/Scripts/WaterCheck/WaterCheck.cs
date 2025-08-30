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
        
        // Auto-assign references if not set
        if (entityMovement == null)
        {
            entityMovement = FindObjectOfType<Player>();
            if (entityMovement != null)
            {
                GameLogger.LogVerbose("[WATER WALL] Auto-assigned Player as entityMovement");
            }
        }
        
        if (targetCollider == null && entityMovement != null)
        {
            targetCollider = entityMovement.GetComponent<Collider2D>();
            if (targetCollider != null)
            {
                GameLogger.LogVerbose("[WATER WALL] Auto-assigned Player collider as targetCollider");
            }
        }
    }

    private void Update()
    {
        if (GameStates.instance.IsGameplayRunning())
        {
            // Continuously update wall state when player is in water
            if (enableHookBasedTriggerControl && 
                entityMovement != null && 
                entityMovement.GetComponent<Player>() != null && 
                !entityMovement.IsAboveWater)
            {
                bool hasAnyBitingHooks = HasAnyActiveBitingHooks();
                bool shouldBeWall = hasAnyBitingHooks;
                bool currentlyWall = !GetComponent<Collider2D>().isTrigger;
                
                if (shouldBeWall != currentlyWall)
                {
                    GetComponent<Collider2D>().isTrigger = !shouldBeWall;
                    GameLogger.LogVerbose($"[WATER WALL] Wall state changed - Hooks: {hasAnyBitingHooks}, Wall Active: {shouldBeWall}");
                }
            }
        }
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

        // Handle wall logic when player tries to exit water
        if (enableHookBasedTriggerControl && 
            entityMovement != null && 
            entityMovement.GetComponent<Player>() != null && 
            aboveWater) // Player trying to exit water
        {
            bool hasAnyBitingHooks = HasAnyActiveBitingHooks();
            GetComponent<Collider2D>().isTrigger = !hasAnyBitingHooks;
            
            GameLogger.LogVerbose($"[WATER WALL] Player trying to exit water - Hooks: {hasAnyBitingHooks}, Wall Active: {!GetComponent<Collider2D>().isTrigger}");
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
            bool hasAnyBitingHooks = HasAnyActiveBitingHooks();
            GetComponent<Collider2D>().isTrigger = !hasAnyBitingHooks;
            
            GameLogger.LogVerbose($"[WATER WALL] Player in water - Hooks: {hasAnyBitingHooks}, Wall Active: {!GetComponent<Collider2D>().isTrigger}");
        }
    }

    private bool HasAnyActiveBitingHooks()
    {
        Player player = Player.Instance;
        if (player == null)
        {
            GameLogger.LogVerbose("[WATER WALL] No Player.Instance found");
            return false;
        }
        
        if (player.activeBitingHooks == null)
        {
            GameLogger.LogVerbose("[WATER WALL] Player.activeBitingHooks is null");
            return false;
        }
        
        int hookCount = player.activeBitingHooks.Count;
        GameLogger.LogVerbose($"[WATER WALL] Player has {hookCount} active biting hooks");
        
        return hookCount > 0;
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

    // Debug method to manually test wall functionality
    public void ForceWallState(bool isWall)
    {
        GetComponent<Collider2D>().isTrigger = !isWall;
        GameLogger.LogVerbose($"[WATER WALL] Manually set wall state to: {isWall}");
    }
}
