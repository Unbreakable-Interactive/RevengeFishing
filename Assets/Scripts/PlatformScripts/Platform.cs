using System.Collections.Generic;
using UnityEngine;
using Utils;

public class Platform : MonoBehaviour
{
    [Header("Assigned Enemies")]
    public List<LandEnemy> assignedEnemies = new List<LandEnemy>();
    
    [Header("Debug")]
    public bool showDebugInfo = true;

    private Collider2D platformCollider;

    [SerializeField] private TypeIdentifier identifier;

    protected virtual void Start()
    {
        platformCollider = GetComponent<Collider2D>();
        if (platformCollider == null)
        {
            Debug.LogError($"Platform {gameObject.name} missing Collider2D component!");
            return;
        }
        gameObject.layer = LayerMask.NameToLayer(LayerNames.PLATFORM);
        
        platformCollider.isTrigger = false;

        Collider2D playerCollider = Player.Instance.Collider;
        if (playerCollider != null)
            Physics2D.IgnoreCollision(platformCollider, playerCollider, true);

        SetupSelectiveCollisions();

        if (showDebugInfo)
        {
            Debug.Log($"Platform {gameObject.name} set up selective collisions");
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        LandEnemy enemy = collision.gameObject.GetComponent<LandEnemy>();
        if (enemy != null && enemy.landEnemyConfig != null)
        {
            if (identifier == enemy.landEnemyConfig.identifier)
            {
                RegisterEnemyOnCollision(enemy);
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        LandEnemy enemy = collision.gameObject.GetComponent<LandEnemy>();
        if (enemy != null && enemy.landEnemyConfig != null)
        {
            if (identifier == enemy.landEnemyConfig.identifier)
            {
                if (assignedEnemies.Contains(enemy))
                {
                    if (showDebugInfo)
                    {
                        Debug.Log($"Enemy {enemy.name} LEFT platform {gameObject.name}");
                    }
                }
            }
        }
    }

    protected virtual void RegisterEnemyOnCollision(LandEnemy enemy)
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
        
        // 🔥 CRITICAL FIX: Only apply smart parenting for LAND platforms, not boat platforms
        if (ShouldUseSmartParenting())
        {
            Transform targetToParent = GetCorrectParentTarget(enemy);
            
            // Only parent if we found a Handler, otherwise keep current hierarchy
            if (targetToParent != null)
            {
                targetToParent.SetParent(this.transform);
                
                if (showDebugInfo)
                {
                    string parentType = targetToParent == enemy.transform ? "enemy directly" : "entire handler";
                    Debug.Log($"PLATFORM ASSIGNMENT: Parenting {parentType} ({targetToParent.name}) to platform {gameObject.name}");
                }
            }
            else if (showDebugInfo)
            {
                Debug.Log($"PLATFORM ASSIGNMENT: Keeping {enemy.name} hierarchy unchanged (no Handler found)");
            }
        }
        else if (showDebugInfo)
        {
            Debug.Log($"PLATFORM ASSIGNMENT: Skipping smart parenting for {gameObject.name} (boat platform detected)");
        }
        
        // CRITICAL: Trigger AI activation after platform assignment
        enemy.OnPlatformAssigned(this);
        
        Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
        if (enemyCollider != null && platformCollider != null)
            Physics2D.IgnoreCollision(platformCollider, enemyCollider, false);

        enemy.platformBoundsCalculated = true;

        if (showDebugInfo)
            Debug.Log($"COLLISION ASSIGNMENT: {enemy.name} assigned to platform {gameObject.name}! Total enemies: {assignedEnemies.Count}");
    }

    protected virtual bool ShouldUseSmartParenting()
    {
        // Check if this is a boat platform by layer or component type
        bool isBoatPlatform = gameObject.layer == LayerMask.NameToLayer("BoatPlatform") || 
                              GetComponent<BoatPlatform>() != null ||
                              transform.name.ToLower().Contains("boat");
    
        if (showDebugInfo && isBoatPlatform)
        {
            Debug.Log($"SMART PARENTING: Detected boat platform {gameObject.name} - disabling smart parenting");
        }
    
        return !isBoatPlatform; // Only use smart parenting for non-boat platforms
    }

    // 🔥 UPDATED METHOD: Smart logic to determine what should be parented to the platform
    private Transform GetCorrectParentTarget(LandEnemy enemy)
    {
        // Check if this enemy is part of a FishermanHandler hierarchy
        Transform current = enemy.transform;
        
        // Traverse up the hierarchy looking for FishermanHandler
        while (current.parent != null)
        {
            Transform parent = current.parent;
            
            // Check if parent is a FishermanHandler (Land or Boat)
            if (parent.name.Contains("FishermanHandler"))
            {
                // 🔥 ADDITIONAL CHECK: Don't parent BoatFishermanHandlers to land platforms
                if (parent.name.Contains("BoatFishermanHandler"))
                {
                    if (showDebugInfo)
                    {
                        Debug.Log($"SMART PARENTING: Found BoatFishermanHandler {parent.name} - this should not be parented to land platform");
                    }
                    return null; // Don't parent boat handlers to land platforms
                }
                
                if (showDebugInfo)
                {
                    Debug.Log($"SMART PARENTING: Found {parent.name} as handler for {enemy.name}");
                }
                return parent; // Parent the entire handler, not just the enemy
            }
            
            current = parent;
        }
        
        // If no FishermanHandler found, check if enemy has any meaningful parent structure
        if (enemy.transform.parent != null)
        {
            if (showDebugInfo)
            {
                Debug.Log($"SMART PARENTING: No FishermanHandler found, but {enemy.name} has parent {enemy.transform.parent.name} - preserving hierarchy");
            }
            return null; // Don't change the hierarchy
        }
        
        // Enemy has no parent structure, safe to parent directly
        if (showDebugInfo)
        {
            Debug.Log($"SMART PARENTING: {enemy.name} has no parent - will parent directly to platform");
        }
        return enemy.transform;
    }

    void SetupSelectiveCollisions()
    {
        // Find all enemies in scene
        LandEnemy[] allEnemies = FindObjectsOfType<LandEnemy>();

        foreach (LandEnemy enemy in allEnemies)
        {
            Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
            if (enemyCollider != null)
            {
                if (assignedEnemies.Contains(enemy))
                {
                    // Allow collision with assigned enemies
                    Physics2D.IgnoreCollision(platformCollider, enemyCollider, false);
                    enemy.assignedPlatform = this;
                }
                else
                {
                    // Ignore collision with non-assigned enemies
                    Physics2D.IgnoreCollision(platformCollider, enemyCollider, true);
                }
            }
        }
    }

    public void UpdateEnemyCollision(LandEnemy enemy, bool shouldCollide)
    {
        Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
        if (enemyCollider != null)
        {
            // When enemy is defeated, ignore collision so they fall through
            Physics2D.IgnoreCollision(platformCollider, enemyCollider, !shouldCollide);
        }
    }

    public void ScanForNewEnemies(float radius = 10f)
    {
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, radius);

        foreach (Collider2D col in nearbyColliders)
        {
            LandEnemy enemy = col.GetComponent<LandEnemy>();
            if (enemy != null && !assignedEnemies.Contains(enemy))
            {
                // Check if enemy doesn't already have a platform assigned
                if (enemy.GetAssignedPlatform() == null)
                {
                    RegisterEnemyAtRuntime(enemy);
                }
            }
        }
    }

    public virtual void RegisterEnemyAtRuntime(LandEnemy enemy)
    {
        if (enemy != null && !assignedEnemies.Contains(enemy))
        {
            assignedEnemies.Add(enemy);
            enemy.SetAssignedPlatform(this);

            // 🔥 CRITICAL FIX: Only apply smart parenting for LAND platforms
            if (ShouldUseSmartParenting())
            {
                Transform targetToParent = GetCorrectParentTarget(enemy);
                
                // Only parent if we found a Handler, otherwise keep current hierarchy
                if (targetToParent != null)
                {
                    targetToParent.SetParent(this.transform);
                    
                    if (showDebugInfo)
                    {
                        string parentType = targetToParent == enemy.transform ? "enemy directly" : "entire handler";
                        Debug.Log($"RUNTIME ASSIGNMENT: Parenting {parentType} ({targetToParent.name}) to platform {gameObject.name}");
                    }
                }
            }

            // Apply collision rules immediately
            Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
            if (enemyCollider != null && platformCollider != null)
            {
                Physics2D.IgnoreCollision(enemyCollider, platformCollider, false);
            }

            // 🔥 CRITICAL FIX: Trigger OnPlatformAssigned after runtime registration
            enemy.OnPlatformAssigned(this);

            if (showDebugInfo)
            {
                Debug.Log($"Auto-assigned {enemy.name} to platform {gameObject.name}");
            }
        }
    }

    // 🔥 UPDATED: ForceRegisterEnemy with smart parenting condition
    public void ForceRegisterEnemy(LandEnemy enemy)
    {
        if (enemy == null || enemy.gameObject == null) return;
        
        if (assignedEnemies.Contains(enemy)) return;
        
        // Clear any previous platform assignment
        Platform previousPlatform = enemy.GetAssignedPlatform();
        if (previousPlatform != null && previousPlatform != this)
        {
            previousPlatform.UnregisterEnemy(enemy);
            if (showDebugInfo)
                Debug.Log($"FORCE MOVED: {enemy.name} from {previousPlatform.name} to {gameObject.name}");
        }
        
        assignedEnemies.Add(enemy);
        enemy.SetAssignedPlatform(this);
        
        // 🔥 CRITICAL FIX: Only apply smart parenting for LAND platforms
        if (ShouldUseSmartParenting())
        {
            Transform targetToParent = GetCorrectParentTarget(enemy);
            
            // Only parent if we found a Handler, otherwise keep current hierarchy
            if (targetToParent != null)
            {
                targetToParent.SetParent(this.transform);
                
                if (showDebugInfo)
                {
                    string parentType = targetToParent == enemy.transform ? "enemy directly" : "entire handler";
                    Debug.Log($"FORCE ASSIGNMENT: Parenting {parentType} ({targetToParent.name}) to platform {gameObject.name}");
                }
            }
        }
        
        // CRITICAL: Trigger AI activation after platform assignment
        enemy.OnPlatformAssigned(this);
        
        Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
        if (enemyCollider != null && platformCollider != null)
            Physics2D.IgnoreCollision(platformCollider, enemyCollider, false);

        enemy.platformBoundsCalculated = true;

        if (showDebugInfo)
            Debug.Log($"FORCE ASSIGNED: {enemy.name} to platform {gameObject.name}! Total enemies: {assignedEnemies.Count}");
    }

    public void UnregisterEnemy(LandEnemy enemy)
    {
        if (enemy == null || enemy.gameObject == null) return;

        if (assignedEnemies.Contains(enemy))
        {
            assignedEnemies.Remove(enemy);

            // 🔥 CRITICAL FIX: Only do smart unparenting for LAND platforms
            if (ShouldUseSmartParenting())
            {
                // Smart unparenting - remove the correct object from platform hierarchy
                Transform currentParent = enemy.transform;
                
                // Find what is actually parented to this platform
                while (currentParent != null && currentParent.parent != this.transform)
                {
                    currentParent = currentParent.parent;
                }
                
                // If we found something parented to this platform, unparent it
                if (currentParent != null && currentParent.parent == this.transform)
                {
                    currentParent.SetParent(null);
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"UNREGISTERED: Removed {currentParent.name} from platform {gameObject.name} hierarchy");
                    }
                }
            }

            Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
            if (enemyCollider != null && platformCollider != null)
            {
                Physics2D.IgnoreCollision(enemyCollider, platformCollider, true);
            }

            if (showDebugInfo)
            {
                Debug.Log($"UNREGISTERED: {enemy.name} from platform {gameObject.name}. Total enemies: {assignedEnemies.Count}");
            }
        }
    }
}
