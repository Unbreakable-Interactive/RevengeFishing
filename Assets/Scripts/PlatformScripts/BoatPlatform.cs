using UnityEngine;

public class BoatPlatform : Platform, IBoatComponent
{
    [Header("Boat Integration")]
    [SerializeField] private BoatID boatID;
    [SerializeField] private bool autoDetectBoatID = true;
    
    [Header("Debug")]
    [SerializeField] private bool debugPlatform = false;
    
    private bool isInitialized = false;
    
    public void SetBoatID(BoatID newBoatID) => boatID = newBoatID;
    
    public void Initialize()
    {
        if (isInitialized) return;
        
        if (platformCollider == null)
            platformCollider = GetComponent<Collider2D>();
            
        if (platformCollider != null)
            platformCollider.isTrigger = true;
        
        if (autoDetectBoatID && boatID == null)
        {
            boatID = GetComponentInParent<BoatID>();
        }
        
        isInitialized = true;
        
        if (debugPlatform)
            GameLogger.LogVerbose($"[BOAT PLATFORM] {GetBoatID()} - Platform initialized");
    }
    
    protected override void Start()
    {
        base.Start();
        
        if (!isInitialized)
        {
            Initialize();
        }
    }
    
    public override void RegisterEnemyAtRuntime(Enemy enemy)
    {
        base.RegisterEnemyAtRuntime(enemy);
        
        if (enemy is BoatLandEnemy boatEnemy)
        {
            if (debugPlatform)
                GameLogger.LogVerbose($"[BOAT PLATFORM] {GetBoatID()} - Registered boat crew member: {enemy.name}, Total enemies: {assignedEnemies.Count}");
        }
    }
    
    public override void UnregisterEnemy(Enemy enemy)
    {
        base.UnregisterEnemy(enemy);
        
        if (enemy is BoatLandEnemy boatEnemy)
        {
            if (debugPlatform)
                GameLogger.LogVerbose($"[BOAT PLATFORM] {GetBoatID()} - Unregistered boat crew member: {enemy.name}");
        }
    }
    
    public string GetBoatID()
    {
        return boatID?.UniqueID ?? "NO_ID";
    }
}