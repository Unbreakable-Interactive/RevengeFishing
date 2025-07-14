using UnityEngine;
using Utils;

/// <summary>
/// BOAT PLATFORM - Specialized platform for boats
/// 
/// Inherits from Platform and adds boat-specific features:
/// - Automatically triggers boat movement when crew registers
/// - Finds BoatFloater component in parent hierarchy
/// - Overrides Platform methods to add boat functionality
/// 
/// USAGE:
/// 1. Replace Platform component with BoatPlatform on boat platforms
/// 2. The script automatically finds the BoatFloater in parent objects
/// 3. When crew registers, boat movement starts automatically
/// </summary>
public class BoatPlatform : Platform
{
    [Header("🚤 BOAT SPECIFIC SETTINGS")]
    [SerializeField] private bool autoStartMovementOnRegistration = true;
    [SerializeField] private bool debugBoatTriggers = true;
    [SerializeField] private BoatFloater boatFloater;
    
    // Override Start to add boat-specific initialization
    protected override void Start()
    {
        // Call parent Start() to maintain all Platform functionality
        base.Start();
        
        // 🚤 IMPORTANT: Override the layer set by Platform.Start() to use BoatPlatform layer
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
    
    // Override the registration method to add boat movement triggering
    protected override void RegisterEnemyOnCollision(LandEnemy enemy)
    {
        // Call parent method to handle all normal Platform logic
        base.RegisterEnemyOnCollision(enemy);
        
        // Add boat-specific logic: trigger movement when crew registers
        TriggerBoatMovement(enemy);
    }
    
    // Override runtime registration to also trigger boat movement
    public override void RegisterEnemyAtRuntime(LandEnemy enemy)
    {
        // Call parent method to handle all normal Platform logic
        base.RegisterEnemyAtRuntime(enemy);
        
        // Add boat-specific logic: trigger movement when crew registers
        TriggerBoatMovement(enemy);
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
