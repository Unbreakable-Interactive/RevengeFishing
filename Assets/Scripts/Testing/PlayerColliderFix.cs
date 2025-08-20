using UnityEngine;

public class PlayerColliderFix : MonoBehaviour
{
    [Header("Collider Settings")]
    [SerializeField] private float colliderRadius = 0.5f;
    [SerializeField] private bool isTrigger = true;
    [SerializeField] private bool enableDebugLogs = true;
    
    [Header("Auto-Setup")]
    [SerializeField] private bool autoSetupOnStart = true;
    
    private CircleCollider2D playerCollider;
    private Player player;
    
    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupPlayerCollider();
        }
    }
    
    void Update()
    {
        // Hotkey to manually setup collider
        if (Input.GetKeyDown(KeyCode.C))
        {
            SetupPlayerCollider();
        }
        
        // Hotkey to test damage
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestDamageSystem();
        }
    }
    
    public void SetupPlayerCollider()
    {
        player = FindObjectOfType<Player>();
        if (player == null)
        {
            DebugLog("❌ No Player found in scene!");
            return;
        }
        
        // Check if player already has a collider
        playerCollider = player.GetComponent<CircleCollider2D>();
        
        if (playerCollider == null)
        {
            // Add a new collider
            playerCollider = player.gameObject.AddComponent<CircleCollider2D>();
            DebugLog("✅ Added CircleCollider2D to player");
        }
        
        // Configure the collider
        playerCollider.radius = colliderRadius;
        playerCollider.isTrigger = isTrigger;
        
        DebugLog($"🔧 Player collider configured: radius={colliderRadius}, isTrigger={isTrigger}");
    }
    
    public void TestDamageSystem()
    {
        if (player == null)
        {
            player = FindObjectOfType<Player>();
        }
        
        // Find any boats in the scene
        BoatController[] boats = FindObjectsOfType<BoatController>();
        
        if (boats.Length == 0)
        {
            DebugLog("❌ No boats found to test damage on. Spawn one with the BoatIntegrityTester (B key).");
            return;
        }
        
        BoatController boat = boats[0];
        AbilitySystem abilitySystem = player.GetComponent<AbilitySystem>();
        
        if (abilitySystem != null)
        {
            Backflip backflip = abilitySystem.GetAbility<Backflip>();
            
            if (backflip != null)
            {
                // Simulate damage manually since collision might not work
                float testDamage = 50f;
                DebugLog($"🧪 Testing manual damage: {testDamage} to boat");
                DebugLog($"   Boat integrity before: {boat.GetCurrentIntegrity()}/{boat.GetMaxIntegrity()}");
                
                boat.TakeDamageFromPlayer(testDamage, "Manual Test");
                
                DebugLog($"   Boat integrity after: {boat.GetCurrentIntegrity()}/{boat.GetMaxIntegrity()}");
                
                if (boat.GetCurrentIntegrity() <= 0)
                {
                    DebugLog("💥 Boat was destroyed!");
                }
            }
            else
            {
                DebugLog("❌ No Backflip ability found on player!");
            }
        }
        else
        {
            DebugLog("❌ No AbilitySystem found on player!");
        }
    }
    
    void DebugLog(string message)
    {
        if (enableDebugLogs) GameLogger.Log($"[PlayerColliderFix] {message}");
    }
}