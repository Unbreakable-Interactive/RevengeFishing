using UnityEngine;
using Utils;


public class BoatPlatform : Platform
{
    [Header("🚤 BOAT SPECIFIC SETTINGS")]
    [SerializeField] private bool autoStartMovementOnRegistration = true;
    [SerializeField] private bool debugBoatTriggers = true;
    [SerializeField] private BoatFloater boatFloater;
    
    protected override void Start()
    {
        base.Start();
        
        if (LayerMask.NameToLayer(LayerNames.BOATPLATFORM) != -1)
        {
            gameObject.layer = LayerMask.NameToLayer(LayerNames.BOATPLATFORM);
            if (debugBoatTriggers)
            {
                Debug.Log($"🚤 BoatPlatform: Set layer to BoatPlatform (layer {gameObject.layer})");
            }
        }
        else
        {
            Debug.LogWarning($"🚤 BoatPlatform: 'BoatPlatform' layer not found! Using Platform layer instead.");
        }
        
        // Find BoatFloater in parent hierarchy if not manually assigned
        if (boatFloater == null)
        {
            boatFloater = GetComponentInParent<BoatFloater>();
            
            if (boatFloater != null && debugBoatTriggers)
            {
                Debug.Log($"🚤 BoatPlatform: Auto-found BoatFloater in {boatFloater.name}");
            }
            else if (debugBoatTriggers)
            {
                Debug.LogWarning($"🚤 BoatPlatform: No BoatFloater found in parent hierarchy for {gameObject.name}");
            }
        }
        
        if (debugBoatTriggers)
        {
            Debug.Log($"🚤 BoatPlatform: Initialized on {gameObject.name}");
        }
    }
    
    // Override the registration method to add boat-specific parenting and movement triggering
    protected override void RegisterEnemyOnCollision(LandEnemy enemy)
    {
        if (enemy == null || enemy.gameObject == null) return;
        
        if (assignedEnemies.Contains(enemy)) return;
        
        Platform previousPlatform = enemy.GetAssignedPlatform();
        if (previousPlatform != null && previousPlatform != this)
        {
            previousPlatform.UnregisterEnemy(enemy);
            if (showDebugInfo)
                Debug.Log($"Enemy {enemy.name} MOVED from {previousPlatform.name} to {gameObject.name}");
        }
        
        assignedEnemies.Add(enemy);
        enemy.SetAssignedPlatform(this);
        
        // 🚤 BOAT-SPECIFIC: Smart parenting for BoatFishermanHandler
        Transform targetToParent = enemy.transform;
        
        // Check if this enemy is a child of a BoatFishermanHandler (with null safety)
        if (enemy.transform.parent != null && 
            enemy.transform.parent.gameObject != null && 
            enemy.transform.parent.name.Contains("BoatFishermanHandler"))
        {
            targetToParent = enemy.transform.parent; // Parent the entire BoatFishermanHandler
            if (debugBoatTriggers)
                Debug.Log($"🚤 BOAT PLATFORM: Parenting entire BoatFishermanHandler ({targetToParent.name}) instead of just {enemy.name}");
        }
        
        // Make enemy (or entire handler) a CHILD of this platform (so they move together!)
        targetToParent.SetParent(this.transform);
        
        // CRITICAL: Trigger AI activation after platform assignment
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
            Debug.Log($"🚤 BOAT COLLISION ASSIGNMENT: {enemy.name} assigned to boat platform {gameObject.name} and made CHILD! Total enemies: {assignedEnemies.Count}");
        
        // Add boat-specific logic: trigger movement when crew registers
        TriggerBoatMovement(enemy);
    }
    
    // Override runtime registration to also handle boat-specific parenting and trigger boat movement
    public override void RegisterEnemyAtRuntime(LandEnemy enemy)
    {
        if (enemy != null && !assignedEnemies.Contains(enemy))
        {
            assignedEnemies.Add(enemy);
            enemy.SetAssignedPlatform(this);

            // 🚤 BOAT-SPECIFIC: Smart parenting for BoatFishermanHandler
            Transform targetToParent = enemy.transform;
            
            // Check if this enemy is a child of a BoatFishermanHandler (with null safety)
            if (enemy.transform.parent != null && 
                enemy.transform.parent.gameObject != null && 
                enemy.transform.parent.name.Contains("BoatFishermanHandler"))
            {
                targetToParent = enemy.transform.parent; // Parent the entire BoatFishermanHandler
                if (debugBoatTriggers)
                    Debug.Log($"🚤 BOAT PLATFORM RUNTIME: Parenting entire BoatFishermanHandler ({targetToParent.name}) instead of just {enemy.name}");
            }

            // Make enemy (or entire handler) a CHILD of this platform (so they move together!)
            targetToParent.SetParent(this.transform);

            // Apply collision rules immediately
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
                Debug.Log($"🚤 BOAT RUNTIME: Auto-assigned {enemy.name} to boat platform {gameObject.name} and made CHILD");
            }
            
            // Add boat-specific logic: trigger movement when crew registers
            TriggerBoatMovement(enemy);
        }
    }
    
    /// <summary>
    /// Triggers boat movement when an enemy registers to this platform
    /// </summary>
    private void TriggerBoatMovement(LandEnemy enemy)
    {
        if (!autoStartMovementOnRegistration) return;
        
        if (boatFloater != null)
        {
            // 🚤 NEW: Recalculate buoyancy when enemy mass is added to boat
            boatFloater.RecalculateBuoyancy();
            
            // Trigger boat movement through the BoatFloater
            boatFloater.OnRegisteredToPlatform(this);
            
            if (debugBoatTriggers)
            {
                Debug.Log($"🚤 BoatPlatform: Triggered boat movement for {enemy.name} on boat {boatFloater.name}");
            }
        }
        else if (debugBoatTriggers)
        {
            Debug.LogWarning($"🚤 BoatPlatform: Cannot trigger boat movement - BoatFloater not found!");
        }
    }
    
    /// <summary>
    /// Public method to manually assign BoatFloater (for special cases)
    /// </summary>
    public void SetBoatFloater(BoatFloater floater)
    {
        boatFloater = floater;
        
        if (debugBoatTriggers)
        {
            Debug.Log($"🚤 BoatPlatform: BoatFloater manually assigned: {floater.name}");
        }
    }
    
    /// <summary>
    /// Get the current BoatFloater reference
    /// </summary>
    public BoatFloater GetBoatFloater()
    {
        return boatFloater;
    }
    
    /// <summary>
    /// Manual boat movement trigger (for testing or special events)
    /// </summary>
    [ContextMenu("🧪 TEST: Trigger Boat Movement")]
    public void ManualTriggerBoatMovement()
    {
        if (boatFloater != null)
        {
            boatFloater.OnRegisteredToPlatform(this);
            Debug.Log("🧪 BoatPlatform: Manual boat movement triggered!");
        }
        else
        {
            Debug.LogWarning("🧪 BoatPlatform: Cannot trigger - BoatFloater not found!");
        }
    }
    
    /// <summary>
    /// Force start boat movement (for special events)
    /// </summary>
    public void ForceStartBoatMovement()
    {
        if (boatFloater != null)
        {
            boatFloater.ForceStartMovement();
            
            if (debugBoatTriggers)
            {
                Debug.Log("🚤 BoatPlatform: Force started boat movement");
            }
        }
    }
    
    /// <summary>
    /// Stop boat movement
    /// </summary>
    public void StopBoatMovement()
    {
        if (boatFloater != null)
        {
            boatFloater.StopMovement();
            
            if (debugBoatTriggers)
            {
                Debug.Log("🚤 BoatPlatform: Stopped boat movement");
            }
        }
    }
    
    /// <summary>
    /// Check if boat is currently moving
    /// </summary>
    public bool IsBoatMoving()
    {
        return boatFloater != null && boatFloater.IsMovementActive();
    }
}
