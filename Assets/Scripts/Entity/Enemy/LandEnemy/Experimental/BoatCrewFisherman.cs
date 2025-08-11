using UnityEngine;

public class BoatCrewFisherman : MonoBehaviour
{
    [Header("Fishing Configuration")]
    [SerializeField] protected FishermanConfig fishermanConfig;
    
    private BoatLandEnemy boatEnemy;
    private HookSpawner hookSpawner;
    private FishingProjectile subscribedHook;
    
    public void Initialize(BoatLandEnemy enemy, HookSpawner spawner, FishermanConfig config)
    {
        boatEnemy = enemy;
        hookSpawner = spawner;
        fishermanConfig = config;
        
        if (fishermanConfig == null)
        {
            fishermanConfig = Resources.Load<FishermanConfig>("FishermanConfig");
            if (fishermanConfig == null)
            {
                GameLogger.LogWarning($"BoatCrewFisherman {gameObject.name}: No FishermanConfig found! Fishing behavior will not work.");
            }
        }
        
        GameLogger.LogError($"[CREW FISHERMAN] {gameObject.name} - Fisherman system initialized");
    }
    
    public void HandleActiveHook()
    {
        if (!boatEnemy.HasThrownHook) return;

        boatEnemy.HookTimer += Time.deltaTime;

        if (hookSpawner.CurrentHook != null &&
            boatEnemy.HookTimer >= boatEnemy.HookDuration &&
            !hookSpawner.CurrentHook.isBeingHeld)
        {
            if (hookSpawner.HasActiveHook())
            {
                float retractionSpeed = 2f;
                hookSpawner.RetractHook(retractionSpeed * Time.deltaTime);
            }
        }

        if (!hookSpawner.HasActiveHook())
        {
            CleanupHookSubscription();

            boatEnemy.HasThrownHook = false;
            boatEnemy.HookTimer = 0f;

            if (fishermanConfig != null && Random.value < fishermanConfig.unequipToolChance)
            {
                boatEnemy.TryUnequipFishingTool();
            }
        }
    }
    
    private void CleanupHookSubscription()
    {
        if (subscribedHook != null)
        {
            subscribedHook.OnPlayerInteraction -= OnHookPlayerInteraction;
            subscribedHook = null;
        }
    }
    
    private void OnHookPlayerInteraction(bool doSomething)
    {
        GameLogger.LogVerbose($"BoatCrewFisherman {gameObject.name}: Hook interacted with player!");
    }
    
    public void SubscribeToHookEvents()
    {
        CleanupHookSubscription();

        if (hookSpawner.CurrentHook is FishingProjectile fishingHook)
        {
            subscribedHook = fishingHook;
            fishingHook.OnPlayerInteraction += OnHookPlayerInteraction;
        }
    }
    
    public void CleanupFishingTools()
    {
        if (hookSpawner != null && hookSpawner.HasActiveHook())
        {
            CleanupHookSubscription();

            hookSpawner.OnHookDestroyed();

            boatEnemy.HasThrownHook = false;
            boatEnemy.HookTimer = 0f;

            GameLogger.LogVerbose($"BoatCrewFisherman {gameObject.name} - Hook handler destroyed due to defeat");
        }
    }
    
    public bool CanThrowHook()
    {
        return hookSpawner?.CanThrowHook() == true;
    }
    
    public void ThrowHook()
    {
        if (CanThrowHook())
        {
            hookSpawner.ThrowHook();
            boatEnemy.HasThrownHook = true;
            boatEnemy.HookTimer = 0f;
            
            SubscribeToHookEvents();
            
            GameLogger.LogVerbose($"BoatCrewFisherman {gameObject.name}: Fishing hook thrown!");
        }
    }
    
    public void RetractHook(float speed)
    {
        if (hookSpawner != null && hookSpawner.HasActiveHook())
        {
            hookSpawner.RetractHook(speed);
        }
    }
    
    public bool HasActiveHook()
    {
        return hookSpawner?.HasActiveHook() == true;
    }
}
