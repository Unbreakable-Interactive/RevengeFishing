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

        SetupSelectiveCollisions();

        if (showDebugInfo)
        {
            Debug.Log($"Platform {gameObject.name} set up selective collisions");
        }
    }

    // ✅ FIXED: Restore old version collision detection
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
            if (assignedEnemies.Contains(enemy))
            {
                if (showDebugInfo)
                {
                    Debug.Log($"Enemy {enemy.name} LEFT platform {gameObject.name}");
                }
            }
        }
    }

    // ✅ FIXED: Add platform assignment callback
    private void RegisterEnemyOnCollision(EnemyBase enemy)
    {
        if (enemy == null) return;
        
        if (assignedEnemies.Contains(enemy)) return;
        
        Platform previousPlatform = enemy.GetAssignedPlatform();
        if (previousPlatform != null && previousPlatform != this)
        {
            previousPlatform.UnregisterEnemy(enemy);
            if (showDebugInfo)
            {
                Debug.Log($"Enemy {enemy.name} MOVED from {previousPlatform.name} to {gameObject.name}");
            }
        }
        
        assignedEnemies.Add(enemy);
        enemy.SetAssignedPlatform(this);
        
        // ✅ CRITICAL: Trigger AI activation after platform assignment
        enemy.OnPlatformAssigned(this);
        
        Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
        if (enemyCollider != null && platformCollider != null)
        {
            Physics2D.IgnoreCollision(platformCollider, enemyCollider, false);
        }

        enemy.platformBoundsCalculated = false;
        // enemy.isGrounded = true;

        if (showDebugInfo)
        {
            Debug.Log($"✅ COLLISION ASSIGNMENT: {enemy.name} assigned to platform {gameObject.name}! Total enemies: {assignedEnemies.Count}");
        }
    }

    void SetupSelectiveCollisions()
    {
        EnemyBase[] allEnemies = FindObjectsOfType<EnemyBase>();
        foreach (EnemyBase enemy in allEnemies)
        {
            Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
            if (enemyCollider != null)
            {
                if (assignedEnemies.Contains(enemy))
                {
                    Physics2D.IgnoreCollision(platformCollider, enemyCollider, false);
                    enemy.assignedPlatform = this;
                }
                else
                {
                    Physics2D.IgnoreCollision(platformCollider, enemyCollider, true);
                }
            }
        }
    }

    public void UpdateEnemyCollision(EnemyBase enemy, bool shouldCollide)
    {
        Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
        if (enemyCollider != null)
        {
            Physics2D.IgnoreCollision(platformCollider, enemyCollider, !shouldCollide);
        }
    }

    public void ScanForNewEnemies(float radius = 10f)
    {
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (Collider2D col in nearbyColliders)
        {
            EnemyBase enemy = col.GetComponent<EnemyBase>();
            if (enemy != null && !assignedEnemies.Contains(enemy))
            {
                if (enemy.GetAssignedPlatform() == null)
                {
                    RegisterEnemyAtRuntime(enemy);
                }
            }
        }
    }

    public void RegisterEnemyAtRuntime(EnemyBase enemy)
    {
        if (enemy != null && !assignedEnemies.Contains(enemy))
        {
            assignedEnemies.Add(enemy);
            enemy.SetAssignedPlatform(this);

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
