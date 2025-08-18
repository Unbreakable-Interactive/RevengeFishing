using System.Collections.Generic;
using UnityEngine;
using Utils;

public class Platform : MonoBehaviour
{
    [Header("Assigned Enemies")]
    public List<Enemy> assignedEnemies = new List<Enemy>();
    
    protected Collider2D platformCollider;

    [SerializeField] protected TypeIdentifier identifier;

    public Collider2D PlatformCollider => platformCollider;
    
    protected virtual void Start()
    {
        platformCollider = GetComponent<Collider2D>();
        if (platformCollider == null)
        {
            GameLogger.LogError($"Platform {gameObject.name} missing Collider2D component!");
            return;
        }
        gameObject.layer = LayerMask.NameToLayer(LayerNames.PLATFORM);
        
        platformCollider.isTrigger = false;

        Collider2D playerCollider = Player.Instance.ColliderToShare;
        if (playerCollider != null)
            Physics2D.IgnoreCollision(platformCollider, playerCollider, true);

        SetupSelectiveCollisions();

        GameLogger.LogVerbose($"Platform {gameObject.name} set up selective collisions");
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Enemy enemy = collision.gameObject.GetComponent<Enemy>();
        if (enemy != null && enemy is LandEnemy landEnemy)
        {
            if (landEnemy.landEnemyConfig != null && identifier == landEnemy.landEnemyConfig.identifier)
            {
                RegisterEnemyOnCollision(enemy);
            }
        }
    }

    protected virtual void RegisterEnemyOnCollision(Enemy enemy)
    {
        if (enemy == null || enemy.gameObject == null) return;
        
        if (!(enemy is LandEnemy landEnemy)) return;
        
        if (assignedEnemies.Contains(enemy)) return;
        
        Platform previousPlatform = landEnemy.GetAssignedPlatform();
        if (previousPlatform != null && previousPlatform != this)
        {
            previousPlatform.UnregisterEnemy(enemy);
            GameLogger.LogVerbose($"Enemy {enemy.name} MOVED from {previousPlatform.name} to {gameObject.name}");
        }
        
        assignedEnemies.Add(enemy);
        landEnemy.SetAssignedPlatform(this);
        
        landEnemy.OnPlatformAssigned(this);
        
        Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
        if (enemyCollider != null)
        {
            Collider2D platformCollider = GetComponent<Collider2D>();
            if (platformCollider != null)
                Physics2D.IgnoreCollision(platformCollider, enemyCollider, false);
        }

        landEnemy.platformBoundsCalculated = true;

        GameLogger.LogVerbose($"Enemy {enemy.name} assigned to platform {gameObject.name}. Total enemies: {assignedEnemies.Count}");
    }

    public virtual void RegisterEnemyAtRuntime(Enemy enemy)
    {
        if (enemy != null && !assignedEnemies.Contains(enemy))
        {
            if (!(enemy is LandEnemy landEnemy)) return;
            
            assignedEnemies.Add(enemy);
            landEnemy.SetAssignedPlatform(this);

            Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
            if (enemyCollider != null)
            {
                Collider2D platformCollider = GetComponent<Collider2D>();
                if (platformCollider != null)
                {
                    Physics2D.IgnoreCollision(enemyCollider, platformCollider, false);
                }
            }

            
            GameLogger.LogVerbose($"Auto-assigned {enemy.name} to platform {gameObject.name}");
        }
    }

    private void SetupSelectiveCollisions()
    {
        Collider2D[] allEnemyColliders = FindObjectsOfType<Collider2D>();
        
        foreach (Collider2D enemyCollider in allEnemyColliders)
        {
            Enemy enemy = enemyCollider.GetComponent<Enemy>();
            if (enemy != null && enemy is LandEnemy landEnemy && landEnemy.landEnemyConfig != null)
            {
                bool shouldCollide = identifier == landEnemy.landEnemyConfig.identifier;
                SetCollisionWithEnemy(enemyCollider, shouldCollide);
            }
        }
    }

    private void SetCollisionWithEnemy(Collider2D enemyCollider, bool shouldCollide)
    {
        if (enemyCollider != null && platformCollider != null)
        {
            Physics2D.IgnoreCollision(platformCollider, enemyCollider, !shouldCollide);
        }
    }

    public virtual void UnregisterEnemy(Enemy enemy)
    {
        if (assignedEnemies.Contains(enemy))
        {
            assignedEnemies.Remove(enemy);
            
            if (enemy is LandEnemy landEnemy)
            {
                landEnemy.SetAssignedPlatform(null);
            }
        }
    }

    public void UnregisterEnemy(LandEnemy landEnemy)
    {
        UnregisterEnemy((Enemy)landEnemy);
    }
    
    public void GetRegisteredEnemies(List<Enemy> outputList)
    {
        if (outputList == null) return;
        
        outputList.Clear();
        outputList.AddRange(assignedEnemies);
    }
    
    public List<Enemy> GetRegisteredEnemies() => new List<Enemy>(assignedEnemies);
    
    public int GetEnemyCount() => assignedEnemies.Count;
}
