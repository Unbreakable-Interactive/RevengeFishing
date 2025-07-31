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

    [SerializeField] protected TypeIdentifier identifier;

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
        
        enemy.OnPlatformAssigned(this);
        
        Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
        if (enemyCollider != null)
        {
            Collider2D platformCollider = GetComponent<Collider2D>();
            if (platformCollider != null)
                Physics2D.IgnoreCollision(platformCollider, enemyCollider, false);
        }

        enemy.platformBoundsCalculated = true;

        if (showDebugInfo)
            Debug.Log($"Enemy {enemy.name} assigned to platform {gameObject.name}. Total enemies: {assignedEnemies.Count}");
    }

    public virtual void RegisterEnemyAtRuntime(LandEnemy enemy)
    {
        if (enemy != null && !assignedEnemies.Contains(enemy))
        {
            assignedEnemies.Add(enemy);
            enemy.SetAssignedPlatform(this);

            Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
            if (enemyCollider != null)
            {
                Collider2D platformCollider = GetComponent<Collider2D>();
                if (platformCollider != null)
                {
                    Physics2D.IgnoreCollision(enemyCollider, platformCollider, false);
                }
            }

            if (showDebugInfo)
            {
                Debug.Log($"Auto-assigned {enemy.name} to platform {gameObject.name}");
            }
        }
    }

    private void SetupSelectiveCollisions()
    {
        Collider2D[] allEnemyColliders = FindObjectsOfType<Collider2D>();
        
        foreach (Collider2D enemyCollider in allEnemyColliders)
        {
            LandEnemy enemy = enemyCollider.GetComponent<LandEnemy>();
            if (enemy != null && enemy.landEnemyConfig != null)
            {
                bool shouldCollide = identifier == enemy.landEnemyConfig.identifier;
                SetCollisionWithEnemy(enemyCollider, shouldCollide);
            }
        }
    }

    public void UnregisterEnemy(LandEnemy enemy)
    {
        if (assignedEnemies.Contains(enemy))
        {
            assignedEnemies.Remove(enemy);
            if (showDebugInfo)
                Debug.Log($"Enemy {enemy.name} removed from platform {gameObject.name}");
        }
    }

    public void SetCollisionWithEnemy(Collider2D enemyCollider, bool shouldCollide)
    {
        if (platformCollider == null)
            platformCollider = GetComponent<Collider2D>();
            
        if (platformCollider != null)
        {
            Physics2D.IgnoreCollision(platformCollider, enemyCollider, !shouldCollide);
        }
    }

    public void RefreshCollisions()
    {
        if (showDebugInfo)
            Debug.Log($"Refreshing collisions for platform {gameObject.name}");
        
        SetupSelectiveCollisions();
    }

    public int GetEnemyCount()
    {
        return assignedEnemies.Count;
    }

    public List<LandEnemy> GetAssignedEnemies()
    {
        return new List<LandEnemy>(assignedEnemies);
    }

    public bool HasEnemy(LandEnemy enemy)
    {
        return assignedEnemies.Contains(enemy);
    }

    protected TypeIdentifier GetIdentifier()
    {
        return identifier;
    }
}
