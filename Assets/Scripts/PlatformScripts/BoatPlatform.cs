using UnityEngine;
using Utils;

public class BoatPlatform : Platform, IBoatComponent
{
    [Header("Boat Identity - ASSIGN IN EDITOR")]
    [SerializeField] private BoatID boatID = new BoatID();
    
    [Header("Required References - ASSIGN IN EDITOR")]
    [SerializeField] private BoatCrewManager crewManager;
    
    [Header("BOAT SPECIFIC SETTINGS")]
    [SerializeField] private bool autoStartMovementOnRegistration = true;
    [SerializeField] private bool debugBoatTriggers = true;
    [SerializeField] private BoatFloater boatFloater;
    
    public string GetBoatID() => boatID.UniqueID;
    public void SetBoatID(BoatID newBoatID) => boatID = newBoatID;
    
    protected override void Start()
    {
        base.Start();
        
        if (boatFloater == null)
        {
            boatFloater = GetComponentInParent<BoatFloater>();
        }
        
        if (debugBoatTriggers)
        {
            Debug.Log($"BoatPlatform: Initialized on {gameObject.name} with ID {boatID}");
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        LandEnemy enemy = collision.gameObject.GetComponent<LandEnemy>();
        if (enemy != null && enemy.landEnemyConfig != null)
        {
            if (!DoesEnemyBelongToThisBoat(enemy))
            {
                if (debugBoatTriggers)
                {
                    string enemyID = enemy is IBoatComponent comp ? comp.GetBoatID() : "NO_ID";
                    Debug.Log($"BoatPlatform: STRICT BLOCK - {enemy.name} (ID: {enemyID}) rejected by boat (ID: {GetBoatID()})");
                }
                return;
            }
            
            if (identifier == enemy.landEnemyConfig.identifier)
            {
                RegisterEnemyOnCollision(enemy);
            }
        }
    }
    
    protected override void RegisterEnemyOnCollision(LandEnemy enemy)
    {
        if (enemy == null || enemy.gameObject == null) return;
        
        if (!DoesEnemyBelongToThisBoat(enemy))
        {
            if (debugBoatTriggers)
            {
                string enemyID = enemy is IBoatComponent comp ? comp.GetBoatID() : "NO_ID";
                Debug.Log($"BoatPlatform: REGISTRATION DENIED - {enemy.name} (ID: {enemyID}) cannot register on boat (ID: {boatID})");
            }
            return;
        }
        
        if (assignedEnemies.Contains(enemy)) return;
        
        Platform previousPlatform = enemy.GetAssignedPlatform();
        if (previousPlatform != null && previousPlatform != this)
        {
            if (previousPlatform is BoatPlatform otherBoatPlatform)
            {
                if (otherBoatPlatform.GetBoatID() != GetBoatID())
                {
                    if (debugBoatTriggers)
                        Debug.Log($"BoatPlatform: BOAT TRANSFER BLOCKED - {enemy.name} cannot switch boats");
                    return;
                }
            }
            
            previousPlatform.UnregisterEnemy(enemy);
            if (showDebugInfo)
                Debug.Log($"Enemy {enemy.name} MOVED from {previousPlatform.name} to {gameObject.name}");
        }
        
        assignedEnemies.Add(enemy);
        enemy.SetAssignedPlatform(this);
        
        enemy.OnPlatformAssigned(this);
        
        Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
        if (enemyCollider != null)
        {
            Collider2D platformCollider = GetComponent<Collider2D>();
            if (platformCollider != null)
                Physics2D.IgnoreCollision(platformCollider, enemyCollider, false);
        }

        enemy.platformBoundsCalculated = true;

        if (debugBoatTriggers)
            Debug.Log($"BOAT ASSIGNMENT SUCCESS: {enemy.name} assigned to boat platform {gameObject.name} (ID: {boatID}). Total: {assignedEnemies.Count}");
        
        TriggerBoatMovement(enemy);
    }
    
    private bool DoesEnemyBelongToThisBoat(LandEnemy enemy)
    {
        if (enemy is IBoatComponent boatComponent)
        {
            bool matches = boatID.Matches(boatComponent.GetBoatID());
            
            if (debugBoatTriggers)
            {
                Debug.Log($"BoatPlatform OWNERSHIP CHECK: Enemy {enemy.name} (ID: {boatComponent.GetBoatID()}) vs Platform (ID: {boatID}) = {matches}");
            }
            
            return matches;
        }
        
        if (debugBoatTriggers)
        {
            Debug.Log($"BoatPlatform: Enemy {enemy.name} has no boat component - rejected");
        }
        
        return false;
    }
    
    public override void RegisterEnemyAtRuntime(LandEnemy enemy)
    {
        if (enemy != null && !assignedEnemies.Contains(enemy))
        {
            if (!DoesEnemyBelongToThisBoat(enemy))
            {
                if (debugBoatTriggers)
                {
                    string enemyID = enemy is IBoatComponent comp ? comp.GetBoatID() : "NO_ID";
                    Debug.Log($"BoatPlatform RUNTIME: DENIED {enemy.name} (ID: {enemyID}) - wrong boat (ID: {boatID})");
                }
                return;
            }
            
            assignedEnemies.Add(enemy);
            enemy.SetAssignedPlatform(this);

            Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
            if (enemyCollider != null)
            {
                Collider2D platformCollider = GetComponent<Collider2D>();
                if (platformCollider != null)
                {
                    Physics2D.IgnoreCollision(enemyCollider, platformCollider, false);
                }
            }

            if (debugBoatTriggers)
            {
                Debug.Log($"BOAT RUNTIME SUCCESS: {enemy.name} assigned to boat platform {gameObject.name} (ID: {boatID})");
            }
            
            TriggerBoatMovement(enemy);
        }
    }
    
    private void TriggerBoatMovement(LandEnemy enemy)
    {
        if (!autoStartMovementOnRegistration) return;
        
        if (boatFloater != null)
        {
            boatFloater.RecalculateBuoyancy();
            boatFloater.OnRegisteredToPlatform(this);
            
            if (debugBoatTriggers)
            {
                Debug.Log($"BoatPlatform: Triggered boat movement for {enemy.name} on boat {boatFloater.name}");
            }
        }
        else if (debugBoatTriggers)
        {
            Debug.LogWarning($"BoatPlatform: Cannot trigger boat movement - BoatFloater not found!");
        }
    }
    
    public void SetBoatFloater(BoatFloater floater)
    {
        boatFloater = floater;
        
        if (debugBoatTriggers)
        {
            Debug.Log($"BoatPlatform: BoatFloater manually assigned: {floater.name}");
        }
    }
    
    public BoatFloater GetBoatFloater()
    {
        return boatFloater;
    }
    
    public void ForceStartBoatMovement()
    {
        if (boatFloater != null)
        {
            boatFloater.ForceStartMovement();
            
            if (debugBoatTriggers)
            {
                Debug.Log("BoatPlatform: Force started boat movement");
            }
        }
    }
    
    public void StopBoatMovement()
    {
        if (boatFloater != null)
        {
            boatFloater.StopMovement();
            
            if (debugBoatTriggers)
            {
                Debug.Log("BoatPlatform: Stopped boat movement");
            }
        }
    }
    
    public bool IsBoatMoving()
    {
        return boatFloater != null && boatFloater.IsMovementActive();
    }
}
