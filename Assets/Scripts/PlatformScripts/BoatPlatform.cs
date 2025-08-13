using UnityEngine;

public class BoatPlatform : Platform, IBoatComponent
{
    [Header("Motion Tracking")]
    [SerializeField] private Vector3 lastPosition;
    [SerializeField] private Vector3 currentVelocity;
    [SerializeField] private bool trackMovement = true;
    [SerializeField] private float syncForceMultiplier = 2f;
    
    [Header("Boat Integration")]
    [SerializeField] private BoatID boatID;
    [SerializeField] private bool autoDetectBoatID = true;
    
    [Header("Synchronization")]
    [SerializeField] private bool forceSyncCrew = true;
    [SerializeField] private float syncThreshold = 0.05f;
    [SerializeField] private bool debugSync = false;
    
    private Vector3 deltaPosition;
    private Vector3 lastSyncPosition;
    
    public void SetBoatID(BoatID newBoatID) => boatID = newBoatID;
    
    protected override void Start()
    {
        base.Start();
        
        platformCollider.isTrigger = true;
        
        lastPosition = transform.position;
        lastSyncPosition = transform.position;
        
        if (autoDetectBoatID && boatID == null)
        {
            boatID = GetComponentInParent<BoatID>();
        }
        
        GameLogger.LogError($"[BOAT PLATFORM] {GetBoatID()} - Platform with motion tracking initialized");
    }
    
    public override void RegisterEnemyAtRuntime(Enemy enemy)
    {
        base.RegisterEnemyAtRuntime(enemy);
        
        if (enemy is BoatLandEnemy boatEnemy)
        {
            boatEnemy.SynchronizeWithBoatMovement(Vector3.zero, this);
            
            GameLogger.LogError($"[BOAT PLATFORM] {GetBoatID()} - Registered boat crew member: {enemy.name}, Total enemies: {assignedEnemies.Count}");
            
            if (debugSync)
            {
                GameLogger.LogVerbose($"[BOAT PLATFORM] {GetBoatID()} - Registered and synced boat crew member: {enemy.name}");
            }
        }
    }
    
    public override void UnregisterEnemy(Enemy enemy)
    {
        base.UnregisterEnemy(enemy);
        
        if (enemy is BoatLandEnemy boatEnemy)
        {
            if (debugSync)
            {
                GameLogger.LogVerbose($"[BOAT PLATFORM] {GetBoatID()} - Unregistered boat crew member: {enemy.name}");
            }
        }
    }
    
    public Vector3 GetPlatformVelocity()
    {
        return currentVelocity;
    }
    
    public Vector3 GetPlatformDelta()
    {
        return deltaPosition;
    }
    
    public bool IsMoving()
    {
        return currentVelocity.magnitude > syncThreshold;
    }
    
    public string GetBoatID()
    {
        if (boatID != null)
        {
            return boatID.UniqueID;
        }
        
        return gameObject.name;
    }
    
    public void NotifyBoatMovement(Vector3 deltaPosMovement)
    {
        if (deltaPosMovement.magnitude < 0.001f) return;
        
        foreach (var enemy in assignedEnemies)
        {
            if (enemy is BoatLandEnemy boatEnemy)
            {
                boatEnemy.SynchronizeWithBoatMovement(deltaPosMovement, this);
            }
        }
        
        if (debugSync && deltaPosMovement.magnitude > syncThreshold)
        {
            GameLogger.LogVerbose($"[BOAT SYNC] {GetBoatID()} - Notified {assignedEnemies.Count} crew members of movement: {deltaPosMovement}");
        }
    }
    
    void FixedUpdate()
    {
        if (trackMovement)
        {
            UpdateMotionTracking();
            
            if (forceSyncCrew && deltaPosition.magnitude > syncThreshold)
            {
                ForceSynchronizeAllCrew();
            }
            
            if (deltaPosition.magnitude > 0.001f)
            {
                NotifyBoatMovement(deltaPosition);
            }
        }
    }
    
    private void UpdateMotionTracking()
    {
        Vector3 currentPosition = transform.position;
        deltaPosition = currentPosition - lastPosition;
        currentVelocity = deltaPosition / Time.fixedDeltaTime;
        lastPosition = currentPosition;
    }
    
    private void ForceSynchronizeAllCrew()
    {
        Vector3 syncDelta = transform.position - lastSyncPosition;
        
        foreach (var enemy in assignedEnemies)
        {
            if (enemy is BoatLandEnemy boatEnemy)
            {
                Vector3 targetPosition = boatEnemy.transform.position + syncDelta * syncForceMultiplier;
                
                Rigidbody2D crewRb = boatEnemy.GetComponent<Rigidbody2D>();
                if (crewRb != null)
                {
                    Vector2 syncForce = (targetPosition - boatEnemy.transform.position) * 50f;
                    crewRb.AddForce(syncForce);
                    
                    if (debugSync)
                    {
                        GameLogger.LogVerbose($"[FORCE SYNC] {boatEnemy.name} - Applied sync force: {syncForce}");
                    }
                }
            }
        }
        
        lastSyncPosition = transform.position;
    }
    
    public void SetSyncForceMultiplier(float multiplier)
    {
        syncForceMultiplier = multiplier;
    }
}
