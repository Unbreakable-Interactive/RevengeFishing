using UnityEngine;
using System.Collections.Generic;

public class Platform : MonoBehaviour
{
    [Header("Player")]
    public GameObject player;

    [Header("Assigned Enemies")]
    public List<EnemyBase> assignedEnemies = new List<EnemyBase>();

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
        
        platformCollider.isTrigger = false;

        if (player != null)
        {
            Collider2D playerCollider = player.GetComponentInChildren<Collider2D>();
            if (playerCollider != null)
            {
                Physics2D.IgnoreCollision(platformCollider, playerCollider, true);
            }
        }

        SetupExistingEnemyCollisions();

        if (showDebugInfo)
        {
            Debug.Log($"Platform {gameObject.name} initialized with {assignedEnemies.Count} pre-assigned enemies");
        }
    }

    // FIXED: Collision2D parameter, not Collider2D!
    void OnCollisionEnter2D(Collision2D collision)
    {
        EnemyBase enemy = collision.gameObject.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            RegisterEnemyOnCollision(enemy);
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        EnemyBase enemy = collision.gameObject.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            // Only unregister if the enemy is actually leaving the platform
            if (assignedEnemies.Contains(enemy))
            {
                if (showDebugInfo)
                {
                    Debug.Log($"Enemy {enemy.name} LEFT platform {gameObject.name}");
                }
                // Don't unregister immediately - enemy might be jumping or moving on platform
                // UnregisterEnemy(enemy);
            }
        }
    }

    private void RegisterEnemyOnCollision(EnemyBase enemy)
    {
        if (enemy == null) return;
        
        // Check if enemy is already assigned to this platform
        if (assignedEnemies.Contains(enemy)) return;
        
        // Remove from previous platform if assigned
        Platform previousPlatform = enemy.GetAssignedPlatform();
        if (previousPlatform != null && previousPlatform != this)
        {
            previousPlatform.UnregisterEnemy(enemy);
            if (showDebugInfo)
            {
                Debug.Log($"Enemy {enemy.name} MOVED from {previousPlatform.name} to {gameObject.name}");
            }
        }
        
        // Assign to this platform
        assignedEnemies.Add(enemy);
        enemy.SetAssignedPlatform(this);
        
        // Setup collision
        Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
        if (enemyCollider != null && platformCollider != null)
        {
            Physics2D.IgnoreCollision(platformCollider, enemyCollider, false);
        }

        // Force recalculate platform bounds
        enemy.platformBoundsCalculated = false;
        enemy.isGrounded = true;

        if (showDebugInfo)
        {
            Debug.Log($"✅ COLLISION ASSIGNMENT: {enemy.name} assigned to platform {gameObject.name}! Total enemies: {assignedEnemies.Count}");
        }
    }

    void SetupExistingEnemyCollisions()
    {
        foreach (EnemyBase enemy in assignedEnemies)
        {
            if (enemy != null)
            {
                SetupEnemyCollisionInternal(enemy);
            }
        }
    }

    private void SetupEnemyCollisionInternal(EnemyBase enemy)
    {
        Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
        if (enemyCollider != null && platformCollider != null)
        {
            Physics2D.IgnoreCollision(platformCollider, enemyCollider, false);
            enemy.SetAssignedPlatform(this);
            
            if (showDebugInfo)
            {
                Debug.Log($"Setup collision for {enemy.name} on platform {gameObject.name}");
            }
        }
    }

    public void UpdateEnemyCollision(EnemyBase enemy, bool shouldCollide)
    {
        Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
        if (enemyCollider != null && platformCollider != null)
        {
            Physics2D.IgnoreCollision(platformCollider, enemyCollider, !shouldCollide);
        }
    }

    public void RegisterEnemyAtRuntime(EnemyBase enemy)
    {
        RegisterEnemyOnCollision(enemy);
    }

    public void UnregisterEnemy(EnemyBase enemy)
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
                Debug.Log($"❌ UNREGISTERED: {enemy.name} from platform {gameObject.name}. Total enemies: {assignedEnemies.Count}");
            }
        }
    }
}
