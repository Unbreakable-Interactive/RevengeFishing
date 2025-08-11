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
    
    private const int PLATFORM_LAYER = 5;
    private const int ENEMY_LAYER = 6;
    
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
            GameLogger.LogError($"[BOAT PLATFORM] Initialized {gameObject.name} - ID: {boatID}");
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        ProcessEnemyRegistration(collision.gameObject, "COLLISION");
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        ProcessEnemyRegistration(other.gameObject, "TRIGGER");
    }
    
    private void ProcessEnemyRegistration(GameObject targetGameObject, string eventType)
    {
        if (targetGameObject.layer != ENEMY_LAYER) return;
        
        Enemy enemy = targetGameObject.GetComponent<Enemy>();
        if (enemy != null && enemy is BoatLandEnemy boatEnemy)
        {
            if (!DoesBoatEnemyBelongToThisBoat(boatEnemy))
            {
                if (debugBoatTriggers)
                {
                    GameLogger.LogError($"[BOAT PLATFORM] REJECTED {eventType} - {boatEnemy.name} wrong boat");
                }
                return;
            }
            
            RegisterEnemyOnCollision(enemy);
        }
    }
    
    protected override void RegisterEnemyOnCollision(Enemy enemy)
    {
        if (enemy == null || enemy.gameObject == null) return;
        
        if (!(enemy is BoatLandEnemy boatEnemy))
        {
            if (debugBoatTriggers)
                GameLogger.LogError($"[BOAT PLATFORM] REJECTED - {enemy.name} not BoatLandEnemy");
            return;
        }
        
        if (!DoesBoatEnemyBelongToThisBoat(boatEnemy))
        {
            if (debugBoatTriggers)
            {
                GameLogger.LogError($"[BOAT PLATFORM] DENIED - {boatEnemy.name} wrong boat ID");
            }
            return;
        }
        
        if (assignedEnemies.Contains(enemy)) return;
        
        Platform previousPlatform = boatEnemy.GetAssignedPlatform();
        if (previousPlatform != null && previousPlatform != this)
        {
            previousPlatform.UnregisterEnemy(enemy);
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
            GameLogger.LogError($"[BOAT ASSIGNMENT] {enemy.name} -> {gameObject.name} SUCCESS! Total: {assignedEnemies.Count}");
        
        TriggerBoatMovement(enemy);
    }
    
    private bool DoesBoatEnemyBelongToThisBoat(BoatLandEnemy boatEnemy)
    {
        return boatID.Matches(boatEnemy.GetBoatID());
    }
    
    public override void RegisterEnemyAtRuntime(Enemy enemy)
    {
        if (enemy != null && !assignedEnemies.Contains(enemy))
        {
            if (!(enemy is BoatLandEnemy boatEnemy))
            {
                if (debugBoatTriggers)
                    GameLogger.LogError($"[BOAT PLATFORM] RUNTIME REJECTED - {enemy.name} not BoatLandEnemy");
                return;
            }
            
            if (!DoesBoatEnemyBelongToThisBoat(boatEnemy))
            {
                if (debugBoatTriggers)
                {
                    GameLogger.LogError($"[BOAT PLATFORM] RUNTIME DENIED - {boatEnemy.name} wrong boat");
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
                GameLogger.LogError($"[BOAT RUNTIME] {enemy.name} -> {gameObject.name} SUCCESS!");
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
                GameLogger.LogError($"[BOAT MOVEMENT] Triggered for {enemy.name}");
            }
        }
    }
    
    public void SetBoatFloater(BoatFloater floater)
    {
        boatFloater = floater;
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
