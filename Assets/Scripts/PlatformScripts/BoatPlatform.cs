using System.Collections.Generic;
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
            GameLogger.LogVerbose($"BoatPlatform: Initialized on {gameObject.name} with ID {boatID}");
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        Enemy enemy = collision.gameObject.GetComponent<Enemy>();
        if (enemy != null && enemy is BoatLandEnemy boatEnemy)
        {
            if (!DoesBoatEnemyBelongToThisBoat(boatEnemy))
            {
                if (debugBoatTriggers)
                {
                    GameLogger.LogVerbose($"BoatPlatform: REJECTED - {boatEnemy.name} (ID: {boatEnemy.GetBoatID()}) rejected by boat (ID: {GetBoatID()})");
                }
                return;
            }
            
            if (boatEnemy.landEnemyConfig != null && identifier == boatEnemy.landEnemyConfig.identifier)
            {
                RegisterEnemyOnCollision(enemy);
            }
        }
    }
    
    protected override void RegisterEnemyOnCollision(Enemy enemy)
    {
        if (enemy == null || enemy.gameObject == null) return;
        
        if (!(enemy is BoatLandEnemy boatEnemy))
        {
            if (debugBoatTriggers)
                GameLogger.LogVerbose($"BoatPlatform: REJECTED - {enemy.name} is not BoatLandEnemy");
            return;
        }
        
        if (!DoesBoatEnemyBelongToThisBoat(boatEnemy))
        {
            if (debugBoatTriggers)
            {
                GameLogger.LogVerbose($"BoatPlatform: REGISTRATION DENIED - {boatEnemy.name} (ID: {boatEnemy.GetBoatID()}) cannot register on boat (ID: {boatID})");
            }
            return;
        }
        
        if (assignedEnemies.Contains(enemy)) return;
        
        Platform previousPlatform = boatEnemy.GetAssignedPlatform();
        if (previousPlatform != null && previousPlatform != this)
        {
            if (previousPlatform is BoatPlatform otherBoatPlatform)
            {
                if (otherBoatPlatform.GetBoatID() != GetBoatID())
                {
                    if (debugBoatTriggers)
                        GameLogger.LogVerbose($"BoatPlatform: BOAT TRANSFER BLOCKED - {enemy.name} cannot switch boats");
                    return;
                }
            }
            
            previousPlatform.UnregisterEnemy(enemy);
            if (showDebugInfo)
                GameLogger.LogVerbose($"Enemy {enemy.name} MOVED from {previousPlatform.name} to {gameObject.name}");
        }
        
        assignedEnemies.Add(enemy);
        boatEnemy.SetAssignedPlatform(this);
        
        boatEnemy.OnPlatformAssigned(this);
        
        Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
        if (enemyCollider != null)
        {
            Collider2D platformCollider = GetComponent<Collider2D>();
            if (platformCollider != null)
                Physics2D.IgnoreCollision(platformCollider, enemyCollider, false);
        }

        boatEnemy.platformBoundsCalculated = true;

        if (debugBoatTriggers)
            GameLogger.LogVerbose($"BOAT ASSIGNMENT SUCCESS: {enemy.name} assigned to boat platform {gameObject.name} (ID: {boatID}). Total: {assignedEnemies.Count}");
        
        TriggerBoatMovement(enemy);
    }
    
    private bool DoesBoatEnemyBelongToThisBoat(BoatLandEnemy boatEnemy)
    {
        bool matches = boatID.Matches(boatEnemy.GetBoatID());
        
        if (debugBoatTriggers)
        {
            GameLogger.LogVerbose($"BoatPlatform OWNERSHIP CHECK: BoatEnemy {boatEnemy.name} (ID: {boatEnemy.GetBoatID()}) vs Platform (ID: {boatID}) = {matches}");
        }
        
        return matches;
    }
    
    public override void RegisterEnemyAtRuntime(Enemy enemy)
    {
        if (enemy != null && !assignedEnemies.Contains(enemy))
        {
            if (!(enemy is BoatLandEnemy boatEnemy))
            {
                if (debugBoatTriggers)
                    GameLogger.LogVerbose($"BoatPlatform RUNTIME: REJECTED - {enemy.name} is not BoatLandEnemy");
                return;
            }
            
            if (!DoesBoatEnemyBelongToThisBoat(boatEnemy))
            {
                if (debugBoatTriggers)
                {
                    GameLogger.LogVerbose($"BoatPlatform RUNTIME: DENIED {boatEnemy.name} (ID: {boatEnemy.GetBoatID()}) - wrong boat (ID: {boatID})");
                }
                return;
            }
            
            assignedEnemies.Add(enemy);
            boatEnemy.SetAssignedPlatform(this);

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
                GameLogger.LogVerbose($"BOAT RUNTIME SUCCESS: {enemy.name} assigned to boat platform {gameObject.name} (ID: {boatID})");
            }
            
            TriggerBoatMovement(enemy);
        }
    }
    
    private void TriggerBoatMovement(Enemy enemy)
    {
        if (!autoStartMovementOnRegistration) return;
        
        if (boatFloater != null)
        {
            boatFloater.RecalculateBuoyancy();
            boatFloater.OnRegisteredToPlatform(this);
            
            if (debugBoatTriggers)
            {
                GameLogger.LogVerbose($"BoatPlatform: Triggered boat movement for {enemy.name} on boat {boatFloater.name}");
            }
        }
        else if (debugBoatTriggers)
        {
            GameLogger.LogWarning($"BoatPlatform: Cannot trigger boat movement - BoatFloater not found!");
        }
    }
    
    public void SetBoatFloater(BoatFloater floater)
    {
        boatFloater = floater;
        
        if (debugBoatTriggers)
        {
            GameLogger.LogVerbose($"BoatPlatform: BoatFloater manually assigned: {floater.name}");
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
                GameLogger.LogVerbose("BoatPlatform: Force started boat movement");
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
                GameLogger.LogVerbose("BoatPlatform: Stopped boat movement");
            }
        }
    }
    
    public bool IsBoatMoving()
    {
        return boatFloater != null && boatFloater.IsMovementActive();
    }
    
    public List<BoatLandEnemy> GetAssignedBoatEnemies()
    {
        List<BoatLandEnemy> boatEnemies = new List<BoatLandEnemy>();
        foreach (Enemy enemy in assignedEnemies)
        {
            if (enemy is BoatLandEnemy boatEnemy)
                boatEnemies.Add(boatEnemy);
        }
        return boatEnemies;
    }
}
