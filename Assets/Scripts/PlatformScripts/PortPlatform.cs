using UnityEngine;
using System.Collections.Generic;

public class PortPlatform : MonoBehaviour
{
    [Header("Platform Settings")]
    public float platformSurfaceOffset = 0.1f;

    [Header("Debug")]
    public bool showDebugGizmos = true;

    private BoxCollider2D platformCollider;
    private Dictionary<GameObject, EnemyPlatformData> enemiesOnPlatform = new Dictionary<GameObject, EnemyPlatformData>();

    private struct EnemyPlatformData
    {
        public bool isOnPlatform;
        public bool canFallThrough;
    }

    void Start()
    {
        platformCollider = GetComponent<BoxCollider2D>();
        if (platformCollider == null)
        {
            platformCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        platformCollider.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsEnemy(other.gameObject))
        {
            HandleEnemyEnter(other.gameObject);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (IsEnemy(other.gameObject))
        {
            HandleEnemyStay(other.gameObject);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (IsEnemy(other.gameObject))
        {
            HandleEnemyExit(other.gameObject);
        }
    }

    void HandleEnemyEnter(GameObject enemy)
    {
        if (!enemiesOnPlatform.ContainsKey(enemy))
        {
            enemiesOnPlatform[enemy] = new EnemyPlatformData
            {
                isOnPlatform = false,
                canFallThrough = false
            };
        }
    }

    void HandleEnemyStay(GameObject enemy)
    {
        if (enemiesOnPlatform.ContainsKey(enemy))
        {
            var data = enemiesOnPlatform[enemy];
            Vector3 enemyPos = enemy.transform.position;
            float surfaceY = GetPlatformSurfaceY();

            // Check if enemy should be on platform
            bool shouldBeOnPlatform = enemyPos.y >= surfaceY - platformSurfaceOffset;

            // Handle landing on platform
            if (shouldBeOnPlatform && !data.isOnPlatform)
            {
                data.isOnPlatform = true;
                data.canFallThrough = false;

                // Stop downward movement and place on surface
                Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();
                if (enemyRb != null && enemyRb.velocity.y <= 0)
                {
                    enemyRb.velocity = new Vector2(enemyRb.velocity.x, 0);
                    enemy.transform.position = new Vector3(enemyPos.x, surfaceY, enemyPos.z);
                }
            }

            // Handle fall-through conditions
            if (data.isOnPlatform)
            {
                bool shouldFallThrough = CheckFallThroughConditions(enemy);

                if (shouldFallThrough && !data.canFallThrough)
                {
                    data.canFallThrough = true;
                    data.isOnPlatform = false;
                }
            }

            // Apply platform physics if on platform and not falling through
            if (data.isOnPlatform && !data.canFallThrough)
            {
                ApplyPlatformPhysics(enemy, surfaceY);
            }

            enemiesOnPlatform[enemy] = data;
        }
    }

    void HandleEnemyExit(GameObject enemy)
    {
        if (enemiesOnPlatform.ContainsKey(enemy))
        {
            enemiesOnPlatform.Remove(enemy);
        }
    }

    void ApplyPlatformPhysics(GameObject enemy, float surfaceY)
    {
        Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();
        if (enemyRb != null)
        {
            Vector3 enemyPos = enemy.transform.position;

            // Keep enemy on platform surface
            if (enemyPos.y < surfaceY)
            {
                enemy.transform.position = new Vector3(enemyPos.x, surfaceY, enemyPos.z);

                if (enemyRb.velocity.y < 0)
                {
                    enemyRb.velocity = new Vector2(enemyRb.velocity.x, 0);
                }
            }
        }
    }

    bool CheckFallThroughConditions(GameObject enemy)
    {
        // Enemy is moving downward fast (knocked off)
        Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();
        if (enemyRb != null && enemyRb.velocity.y < -2f)
        {
            return true;
        }

        // Add more conditions when you implement enemy states:
        // - Enemy is stunned
        // - Player attacked from below
        // - Enemy was knocked off by combat

        return false;
    }

    float GetPlatformSurfaceY()
    {
        return platformCollider.bounds.max.y;
    }

    bool IsEnemy(GameObject obj)
    {
        return obj.CompareTag("Enemy") || obj.name.ToLower().Contains("enemy");
    }

    bool IsPlayer(GameObject obj)
    {
        return obj.CompareTag("Player") || obj.GetComponent<Player>() != null ||
               obj.GetComponentInParent<Player>() != null;
    }

    void OnDrawGizmos()
    {
        if (showDebugGizmos && platformCollider != null)
        {
            // Draw platform bounds
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(platformCollider.bounds.center, platformCollider.bounds.size);

            // Draw surface line
            Gizmos.color = Color.yellow;
            Vector3 surfaceStart = new Vector3(platformCollider.bounds.min.x, GetPlatformSurfaceY(), 0);
            Vector3 surfaceEnd = new Vector3(platformCollider.bounds.max.x, GetPlatformSurfaceY(), 0);
            Gizmos.DrawLine(surfaceStart, surfaceEnd);

            // Draw enemies on platform
            Gizmos.color = Color.red;
            foreach (var kvp in enemiesOnPlatform)
            {
                if (kvp.Value.isOnPlatform)
                {
                    Gizmos.DrawWireSphere(kvp.Key.transform.position, 0.3f);
                }
            }
        }
    }
}
