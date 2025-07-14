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
        if (identifier == enemy.landEnemyConfig.identifier)
        {
            if (enemy != null)
            {
                RegisterEnemyOnCollision(enemy);
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        LandEnemy enemy = collision.gameObject.GetComponent<LandEnemy>();
        if (identifier == enemy.landEnemyConfig.identifier)
        {
            if (enemy != null)
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
        if (enemy == null) return;
        
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
        
        // 🚤 SMART PARENTING: Check if enemy is part of a BoatFishermanHandler
        Transform targetToParent = enemy.transform;
        
        // Check if this enemy is a child of a BoatFishermanHandler
        if (enemy.transform.parent != null && enemy.transform.parent.name.Contains("BoatFishermanHandler"))
        {
            targetToParent = enemy.transform.parent; // Parent the entire BoatFishermanHandler
            if (showDebugInfo)
                Debug.Log($"🚤 DETECTED: Parenting entire BoatFishermanHandler ({targetToParent.name}) instead of just {enemy.name}");
        }
        
        // Make enemy (or entire handler) a CHILD of this platform (so they move together!)
        targetToParent.SetParent(this.transform);
        
        // CRITICAL: Trigger AI activation after platform assignment
        enemy.OnPlatformAssigned(this);
        
        Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
        if (enemyCollider != null && platformCollider != null)
            Physics2D.IgnoreCollision(platformCollider, enemyCollider, false);

        enemy.platformBoundsCalculated = true;

        if (showDebugInfo)
            Debug.Log($"COLLISION ASSIGNMENT: {enemy.name} assigned to platform {gameObject.name} and made CHILD! Total enemies: {assignedEnemies.Count}");
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

            // Make enemy a CHILD of this platform (so they move together!)
            enemy.transform.SetParent(this.transform);

            // Apply collision rules immediately
            Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
            if (enemyCollider != null && platformCollider != null)
            {
                Physics2D.IgnoreCollision(enemyCollider, platformCollider, false);
            }

            if (showDebugInfo)
            {
                Debug.Log($"Auto-assigned {enemy.name} to platform {gameObject.name} and made CHILD");
            }
        }
    }

    public void UnregisterEnemy(LandEnemy enemy)
    {
        if (enemy == null) return;

        if (assignedEnemies.Contains(enemy))
        {
            assignedEnemies.Remove(enemy);

            // Remove parent relationship when unregistering
            enemy.transform.SetParent(null);

            Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
            if (enemyCollider != null && platformCollider != null)
            {
                Physics2D.IgnoreCollision(enemyCollider, platformCollider, true);
            }

            if (showDebugInfo)
            {
                Debug.Log($"UNREGISTERED: {enemy.name} from platform {gameObject.name} and removed PARENT. Total enemies: {assignedEnemies.Count}");
            }
        }
    }
}
