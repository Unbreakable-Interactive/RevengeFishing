using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

public class Platform : MonoBehaviour
{
    [Header("Player")]
    public GameObject player;

    [Header("Assigned Enemies")]
    public List<LandEnemy> assignedEnemies = new List<LandEnemy>();

    [Tooltip("Not used")]
    [Header("Platform Settings")]
    public float surfaceOffset = 0.1f;

    [Header("Debug")]
    public bool showDebugInfo = true;

    private Collider2D platformCollider;

    void Start()
    {
        platformCollider = GetComponent<Collider2D>();
        if (platformCollider == null)
        {
            Debug.LogError($"Platform {gameObject.name} missing Collider2D component!");
            return;
        }
        gameObject.layer = LayerMask.NameToLayer(LayerNames.PLATFORM);
        
        platformCollider.isTrigger = false;

        if (player != null)
        {
            Collider2D playerCollider = player.GetComponentInChildren<Collider2D>();
            if (playerCollider != null)
            {
                Physics2D.IgnoreCollision(platformCollider, playerCollider, true);
            }
        }

        SetupSelectiveCollisions();

        if (showDebugInfo)
        {
            Debug.Log($"Platform {gameObject.name} set up selective collisions");
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        LandEnemy enemy = collision.gameObject.GetComponent<LandEnemy>();
        if (enemy != null)
        {
            RegisterEnemyOnCollision(enemy);
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        LandEnemy enemy = collision.gameObject.GetComponent<LandEnemy>();
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
    private void RegisterEnemyOnCollision(LandEnemy enemy)
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
        
        // CRITICAL: Trigger AI activation after platform assignment
        enemy.OnPlatformAssigned(this);
        
        Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
        if (enemyCollider != null && platformCollider != null)
            Physics2D.IgnoreCollision(platformCollider, enemyCollider, false);

        enemy.platformBoundsCalculated = true;

        if (showDebugInfo)
            Debug.Log($"✅ COLLISION ASSIGNMENT: {enemy.name} assigned to platform {gameObject.name}! Total enemies: {assignedEnemies.Count}");
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

    public void RegisterEnemyAtRuntime(LandEnemy enemy)
    {
        if (enemy != null && !assignedEnemies.Contains(enemy))
        {
            assignedEnemies.Add(enemy);
            enemy.SetAssignedPlatform(this);

            // Apply collision rules immediately
            Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
            if (enemyCollider != null && platformCollider != null)
            {
                Physics2D.IgnoreCollision(enemyCollider, platformCollider, false);
            }

            if (showDebugInfo)
            {
                Debug.Log($"Auto-assigned {enemy.name} to platform {gameObject.name}");
            }
        }
    }

    public void UnregisterEnemy(LandEnemy enemy)
    {
        if (enemy == null) return;

        if (assignedEnemies.Contains(enemy))
        {
            assignedEnemies.Remove(enemy);

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
