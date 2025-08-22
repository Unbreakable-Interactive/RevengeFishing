using UnityEngine;

public class PlayerCollisionForwarder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player player;
    [SerializeField] private AbilitySystem abilitySystem;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    void Start()
    {
        // Auto-find references if not assigned
        if (player == null)
        {
            player = GetComponentInParent<Player>();
        }
        
        if (abilitySystem == null)
        {
            abilitySystem = GetComponentInParent<AbilitySystem>();
        }
        
        if (player == null)
        {
            GameLogger.LogError("[PlayerCollisionForwarder] No Player component found in parent!");
        }
        
        if (abilitySystem == null)
        {
            GameLogger.LogError("[PlayerCollisionForwarder] No AbilitySystem component found in parent!");
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        DebugLog($"Collision detected with: {other.name}");
        
        if (abilitySystem == null) return;
        
        // Forward collision to active abilities
        ForwardCollisionToAbilities(other);
    }
    
    private void ForwardCollisionToAbilities(Collider2D other)
    {
        // Get the Backflip ability specifically
        Backflip backflip = abilitySystem.GetAbility<Backflip>();
        
        if (backflip != null && backflip.IsCharging && !backflip.IsFlipping)
        {
            // Check if we hit a boat
            BoatController boat = other.GetComponentInParent<BoatController>();
            if (boat != null)
            {
                DebugLog($"Forwarding boat collision to Backflip ability: {boat.name}");
                
                // Call the damage method directly since we can't call OnTriggerEnter2D
                backflip.HandleBoatCollision(boat);
            }
        }
        
        // Get the Big Bite ability specifically
        BigBite bigBite = abilitySystem.GetAbility<BigBite>();
        
        if (bigBite != null && bigBite.IsCharging && bigBite.IsMouthOpen)
        {
            // Check if we hit an enemy
            Enemy enemy = other.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                DebugLog($"Forwarding enemy collision to Big Bite ability: {enemy.name}");
                
                // Call the eating method
                bigBite.HandleEnemyCollision(enemy);
            }
        }
    }
    
    void DebugLog(string message)
    {
        if (enableDebugLogs) GameLogger.Log($"[PlayerCollisionForwarder] {message}");
    }
}