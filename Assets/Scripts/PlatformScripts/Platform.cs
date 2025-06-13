using UnityEngine;
using System.Collections.Generic;

public class Platform : MonoBehaviour
{
    [Header("Player")]
    public GameObject player; // Reference to the player object

    [Header("Assigned Enemies")]
    public List<EnemyBase> assignedEnemies = new List<EnemyBase>();

    [Header("Platform Settings")]
    public float surfaceOffset = 0.1f; // How far above surface to place enemies

    [Header("Debug")]
    public bool showDebugInfo = true;

    private Collider2D platformCollider;

    void Start()
    {
        platformCollider = GetComponent<Collider2D>();
        platformCollider.isTrigger = false; // Make it solid

        Collider2D playerCollider = player.GetComponent<Collider2D>();
        Physics2D.IgnoreCollision(platformCollider, playerCollider, true);

        // Set up selective collisions
        SetupSelectiveCollisions();

        if (showDebugInfo)
        {
            Debug.Log($"Platform {gameObject.name} set up selective collisions");
        }
    }

    void SetupSelectiveCollisions()
    {
        // Find all enemies in scene
        EnemyBase[] allEnemies = FindObjectsOfType<EnemyBase>();

        foreach (EnemyBase enemy in allEnemies)
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

    public void UpdateEnemyCollision(EnemyBase enemy, bool shouldCollide)
    {
        Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
        if (enemyCollider != null)
        {
            // When enemy is defeated, ignore collision so they fall through
            Physics2D.IgnoreCollision(platformCollider, enemyCollider, !shouldCollide);
        }
    }
}
