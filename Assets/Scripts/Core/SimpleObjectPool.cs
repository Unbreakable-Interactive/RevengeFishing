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

        // CLEANUP ENEMY STATE ONLY - NO PLATFORM ASSIGNMENT
        var enemyBase = objectToSpawn.GetComponent<EnemyBase>();
        if (enemyBase != null)
        {
            CleanupEnemyAssignment(enemyBase);
        }

        objectToSpawn.transform.position = position;
        objectToSpawn.SetActive(true);
        activeCount[poolName]++;

        // INITIALIZE ENEMY - PLATFORM ASSIGNMENT HAPPENS ON COLLISION
        if (enemyBase != null)
        {
            enemyBase.Initialize();
            Debug.Log($"Spawned {poolName} at {position} - Platform assignment will happen on collision");
        }

        Debug.Log($"Spawned '{poolName}' at {position}. Active: {activeCount[poolName]}");
        return objectToSpawn;
    }

    private void CleanupEnemyAssignment(EnemyBase enemy)
    {
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
        enemy.isGrounded = false;

        Debug.Log($"Enemy {enemy.name} assignment cleaned up and reset");
    }

    public void ReturnToPool(string poolName, GameObject obj)
    {
        if (!poolDictionary.ContainsKey(poolName))
        {
            Debug.LogWarning($"Pool '{poolName}' does not exist!");
            Destroy(obj);
            return;
        }

        var enemyBase = obj.GetComponent<EnemyBase>();
        if (enemyBase != null)
        {
            CleanupEnemyAssignment(enemyBase);
        }

        obj.SetActive(false);
        poolDictionary[poolName].Enqueue(obj);
        activeCount[poolName]--;

        Debug.Log($"Returned '{poolName}' to pool. Active: {activeCount[poolName]}");
    }

    public int GetActiveCount(string poolName)
    {
        return activeCount.ContainsKey(poolName) ? activeCount[poolName] : 0;
    }
}
