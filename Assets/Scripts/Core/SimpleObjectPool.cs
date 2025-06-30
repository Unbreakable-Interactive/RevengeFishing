using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PoolConfig
{
    public string poolName;
    public GameObject prefab;
    public int initialSize = 5;
    public int maxSize = 20;
}

public class SimpleObjectPool : MonoBehaviour
{
    [SerializeField] private List<PoolConfig> poolConfigs = new List<PoolConfig>();
    private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, int> activeCount = new Dictionary<string, int>();

    private void Start()
    {
        InitializePools();
    }

    private void InitializePools()
    {
        foreach (var config in poolConfigs)
        {
            CreatePool(config.poolName, config.prefab, config.initialSize);
        }
    }

    private void CreatePool(string poolName, GameObject prefab, int initialSize)
    {
        if (prefab == null)
        {
            Debug.LogError($"Cannot create pool '{poolName}' - prefab is null!");
            return;
        }

        Queue<GameObject> objectPool = new Queue<GameObject>();

        for (int i = 0; i < initialSize; i++)
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.SetActive(false);
            objectPool.Enqueue(obj);
        }

        poolDictionary[poolName] = objectPool;
        activeCount[poolName] = 0;

        Debug.Log($"Pool '{poolName}' created with {initialSize} objects");
    }

    public GameObject Spawn(string poolName, Vector3 position)
    {
        if (!poolDictionary.ContainsKey(poolName))
        {
            Debug.LogWarning($"Pool '{poolName}' does not exist!");
            return null;
        }

        Queue<GameObject> pool = poolDictionary[poolName];
        GameObject objectToSpawn;

        if (pool.Count > 0)
        {
            objectToSpawn = pool.Dequeue();
        }
        else
        {
            var config = poolConfigs.Find(c => c.poolName == poolName);
            if (config != null)
            {
                if (activeCount[poolName] >= config.maxSize)
                {
                    Debug.LogWarning($"Pool '{poolName}' has reached max size ({config.maxSize}). Cannot spawn more objects.");
                    return null;
                }

                objectToSpawn = Instantiate(config.prefab, transform);
                Debug.Log($"Created new object for pool '{poolName}' (pool was empty). Active: {activeCount[poolName] + 1}/{config.maxSize}");
            }
            else
            {
                Debug.LogError($"Cannot create new object for pool '{poolName}' - config not found!");
                return null;
            }
        }

        // CRITICAL FIX: Reset FishermanHandler structure properly
        // FishermanHandler (parent) -> Fisherman (child with script)
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = Quaternion.identity;
        objectToSpawn.transform.localScale = Vector3.one;

        // Find the actual Fisherman child GameObject with the script
        LandEnemy enemyBase = objectToSpawn.GetComponentInChildren<LandEnemy>();
        if (enemyBase == null)
            enemyBase = objectToSpawn.GetComponent<LandEnemy>(); // Fallback if script is on parent

        // PHYSICS RESET: Get Rigidbody2D from the correct GameObject (usually the child)
        Rigidbody2D rb = null;
        if (enemyBase != null)
            rb = enemyBase.GetComponent<Rigidbody2D>();
        
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 1f;
            rb.simulated = true; // ENSURE PHYSICS IS ENABLED
        }

        // COMPLETE STATE RESET with original spawn position
        if (enemyBase != null)
        {
            CompleteEnemyReset(enemyBase, position);
        }

        objectToSpawn.SetActive(true);
        activeCount[poolName]++;

        // PROPER INITIALIZATION - Must happen AFTER positioning
        if (enemyBase != null)
        {
            enemyBase.Initialize();
            enemyBase.ChangeState_Alive();
            
            // FORCE CORRECT WATER STATE DETECTION
            bool isActuallyAboveWater = CheckIfAboveWater(position);
            enemyBase.SetMovementMode(isActuallyAboveWater);
            
            Debug.Log($"Spawned {poolName} at {position} - Water state: {isActuallyAboveWater}");
        }

        Debug.Log($"Spawned '{poolName}' at {position}. Active: {activeCount[poolName]}");
        return objectToSpawn;
    }

    // Helper method to check if spawn position is above water
    private bool CheckIfAboveWater(Vector3 position)
    {
        // Simple check: if Y position is above 0, consider it above water
        // You can adjust this based on your water level
        return position.y > 0f;
    }

    private void CompleteEnemyReset(LandEnemy enemy, Vector3 spawnPosition)
    {
        // PLATFORM ASSIGNMENT CLEANUP
        Platform oldPlatform = enemy.GetAssignedPlatform();
        if (oldPlatform != null)
        {
            oldPlatform.UnregisterEnemy(enemy);
            Debug.Log($"Cleaned up old assignment: {enemy.name} removed from platform {oldPlatform.name}");
        }

        enemy.SetAssignedPlatform(null);
        enemy.platformBoundsCalculated = false;
        enemy.platformLeftEdge = 0f;
        enemy.platformRightEdge = 0f;

        // // FLOATING STATE RESET
        // enemy.HasStartedFloating = false;
        // enemy.FloatingStartTime = 0f;
        //
        // // MOVEMENT STATE RESET
        // enemy.MovementStateLand = LandEnemy.LandMovementState.Idle;
        // enemy.fishingToolEquipped = false;
        //
        // // HOOK FISHING STATE RESET (CRITICAL FIX)
        // enemy.HasThrownHook = false;
        // enemy.HookTimer = 0f;
        //
        // // TIMING RESET (next action time)
        // enemy.NextActionTime = Time.time + Random.Range(0.5f, 2f);
        //
        // // SAVE INITIAL SPAWN POSITION (THE SOLUTION TO THE MAIN PROBLEM)
        // enemy.InitialSpawnPosition = spawnPosition;

        // COLLISION RESET - ENSURE PROPER PHYSICS
        Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
        if (enemyCollider == null)
            enemyCollider = enemy.GetComponentInChildren<Collider2D>();
        
        if (enemyCollider != null)
        {
            enemyCollider.isTrigger = false; // RESET to solid collision
            enemyCollider.enabled = true; // ENSURE COLLIDER IS ENABLED
        }

        // FORCE PROPER WATER STATE RESET 
        // Use SetMovementMode instead of directly accessing protected field
        bool shouldBeAboveWater = spawnPosition.y > 0f;
        enemy.SetMovementMode(shouldBeAboveWater);
        
        Debug.Log($"Enemy {enemy.name} COMPLETELY RESET for pooling at {spawnPosition} - shouldBeAboveWater: {shouldBeAboveWater}");
    }

    public void ReturnToPool(string poolName, GameObject obj)
    {
        if (!poolDictionary.ContainsKey(poolName))
        {
            Debug.LogWarning($"Pool '{poolName}' does not exist!");
            Destroy(obj);
            return;
        }

        // CRITICAL FIX: Handle FishermanHandler structure properly
        // Find the actual Fisherman child GameObject with the script
        LandEnemy enemyBase = obj.GetComponentInChildren<LandEnemy>();
        if (enemyBase == null)
            enemyBase = obj.GetComponent<LandEnemy>(); // Fallback if script is on parent

        if (enemyBase != null)
        {
            // RESET TO ORIGINAL SPAWN POSITION (stored in InitialSpawnPosition)
            // Vector3 resetPosition = enemyBase.InitialSpawnPosition;
            // obj.transform.position = resetPosition;
            
            // CompleteEnemyReset(enemyBase, resetPosition);
            
            // RESET CHILD TRANSFORM TOO (if enemy is child of handler)
            if (enemyBase.transform != obj.transform)
            {
                enemyBase.transform.localPosition = Vector3.zero;
                enemyBase.transform.localRotation = Quaternion.identity;
                enemyBase.transform.localScale = Vector3.one;
            }
        }

        // PHYSICS AND TRANSFORM RESET on the correct GameObject
        Rigidbody2D rb = null;
        if (enemyBase != null)
            rb = enemyBase.GetComponent<Rigidbody2D>();
        
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 1f;
            rb.simulated = true;
        }

        obj.transform.rotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;

        obj.SetActive(false);
        poolDictionary[poolName].Enqueue(obj);
        activeCount[poolName]--;

        Debug.Log($"Returned '{poolName}' to pool at ORIGINAL position {obj.transform.position}. Active: {activeCount[poolName]}");
    }

    public int GetActiveCount(string poolName)
    {
        return activeCount.ContainsKey(poolName) ? activeCount[poolName] : 0;
    }
}
