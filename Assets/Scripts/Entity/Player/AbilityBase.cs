using UnityEngine;

public abstract class AbilityBase : MonoBehaviour
{
    [Header("Ability Info")]
    [SerializeField] protected string abilityName = "Unnamed Ability";
    [SerializeField] protected string description = "No description provided";
    
    [Header("Resource Cost")]
    [SerializeField] protected ResourceType costType = ResourceType.None;
    [SerializeField] protected int resourceCost = 0;
    
    [Header("Requirements")]
    [SerializeField] protected Player.Phase minimumPhase = Player.Phase.Infant;
    [SerializeField] protected bool requiresUnderwater = false;
    [SerializeField] protected bool requiresAboveWater = false;
    
    [Header("Debug")]
    [SerializeField] protected bool enableDebugLogs = false;
    
    // Runtime references
    protected AbilitySystem abilitySystem;
    protected Player player;
    
    public enum ResourceType
    {
        None,           // No cost
        Fatigue,        // Costs fatigue points
        Power,          // Costs power level
        Health          // Could add health cost later
    }
    
    // Public properties
    public string AbilityName => abilityName;
    public string Description => description;
    
    public virtual void Initialize(AbilitySystem system, Player playerRef)
    {
        abilitySystem = system;
        player = playerRef;
        
        OnInitialize();
        DebugLog($"{abilityName} initialized");
    }
    
    public virtual bool CanActivate()
    {
        // Check player reference
        if (player == null)
        {
            return false;
        }
        
        // Check phase requirement
        if (!IsPhaseRequirementMet())
        {
            return false;
        }
        
        // Check environment requirement
        if (!IsEnvironmentRequirementMet())
        {
            return false;
        }
        
        // Check resource cost
        if (!CanAffordCost())
        {
            return false;
        }
        
        // Check custom conditions
        return CanActivateCustom();
    }
    
    public virtual void Activate()
    {
        if (!CanActivate())
        {
            DebugLog($"Cannot activate {abilityName}");
            return;
        }
        
        // Pay resource cost
        PayResourceCost();
        
        // Execute ability
        OnActivate();
        
        DebugLog($"Activated {abilityName}");
    }
    
    public string GetBlockReason()
    {
        if (player == null)
        {
            return "No player reference";
        }
        
        if (!IsPhaseRequirementMet())
        {
            return $"Requires {minimumPhase} phase or higher";
        }
        
        if (!IsEnvironmentRequirementMet())
        {
            string requirement = requiresUnderwater ? "underwater" : "above water";
            return $"Must be {requirement}";
        }
        
        if (!CanAffordCost())
        {
            return $"Not enough {costType.ToString().ToLower()} ({resourceCost} required)";
        }
        
        return GetCustomBlockReason();
    }
    
    protected virtual bool IsPhaseRequirementMet()
    {
        if (player == null) return false;
        return (int)player.currentPhase >= (int)minimumPhase;
    }
    
    protected virtual bool IsEnvironmentRequirementMet()
    {
        if (player == null) return true;
        
        if (requiresUnderwater && player.IsAboveWater) return false;
        if (requiresAboveWater && !player.IsAboveWater) return false;
        
        return true;
    }
    
    protected virtual bool CanAffordCost()
    {
        if (player == null || resourceCost <= 0) return true;
        
        return costType switch
        {
            ResourceType.None => true,
            ResourceType.Fatigue => player.GetFatigue() + resourceCost <= player.entityFatigue.maxFatigue,
            ResourceType.Power => player.PowerLevel >= resourceCost,
            ResourceType.Health => true, // Add health system later if needed
            _ => true
        };
    }
    
    protected virtual void PayResourceCost()
    {
        if (player == null || resourceCost <= 0) return;
        
        switch (costType)
        {
            case ResourceType.Fatigue:
                player.entityFatigue.fatigue += resourceCost;
                DebugLog($"Paid {resourceCost} fatigue, new total: {player.GetFatigue()}");
                break;
                
            case ResourceType.Power:
                // You might want to add a method to reduce power in Player.cs
                DebugLog($"Would pay {resourceCost} power (implement in Player.cs)");
                break;
        }
    }
    
    protected void DebugLog(string message)
    {
        if (enableDebugLogs) GameLogger.Log($"[{abilityName}] {message}");
    }
    
    // Abstract and virtual methods for subclasses to override
    protected virtual void OnInitialize() { }
    protected virtual bool CanActivateCustom() { return true; }
    protected virtual string GetCustomBlockReason() { return "Unknown reason"; }
    
    protected abstract void OnActivate();
}