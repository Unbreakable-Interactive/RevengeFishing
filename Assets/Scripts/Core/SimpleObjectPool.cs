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
    public static SimpleObjectPool Instance { get; private set; }
    
    [Header("Pool Configurations")]
    public List<PoolConfig> pools = new List<PoolConfig>();
    
    [Header("Debug")]
    public bool showLogs = true;
    
    private Dictionary<string, Queue<GameObject>> poolDictionary;
    private Dictionary<string, int> activeCount;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Initialize()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        activeCount = new Dictionary<string, int>();
        
        foreach (PoolConfig pool in pools)
        {
            CreatePool(pool);
        }
        
        if (showLogs)
            Debug.Log($"SimpleObjectPool initialized with {pools.Count} pools");
    }
    
    void CreatePool(PoolConfig config)
    {
        GameObject poolParent = new GameObject($"Pool_{config.poolName}");
        poolParent.transform.SetParent(transform);
        
        Queue<GameObject> objectPool = new Queue<GameObject>();
        
        for (int i = 0; i < config.initialSize; i++)
        {
            GameObject obj = Instantiate(config.prefab, poolParent.transform);
            obj.SetActive(false);
            objectPool.Enqueue(obj);
        }
        
        poolDictionary[config.poolName] = objectPool;
        activeCount[config.poolName] = 0;
        
        if (showLogs)
            Debug.Log($"Created pool '{config.poolName}' with {config.initialSize} objects");
    }
    
    public GameObject Spawn(string poolName, Vector3 position, Quaternion rotation = default)
    {
        if (!poolDictionary.ContainsKey(poolName))
        {
            Debug.LogError($"Pool '{poolName}' doesn't exist!");
            return null;
        }
        
        GameObject obj;
        
        if (poolDictionary[poolName].Count > 0)
        {
            obj = poolDictionary[poolName].Dequeue();
        }
        else
        {
            // Find pool config
            PoolConfig config = pools.Find(p => p.poolName == poolName);
            if (config != null && activeCount[poolName] < config.maxSize)
            {
                obj = Instantiate(config.prefab);
            }
            else
            {
                Debug.LogWarning($"Pool '{poolName}' is at max capacity");
                return null;
            }
        }
        
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);
        activeCount[poolName]++;
        
        // Initialize if it has the components
        EnemyBase enemy = obj.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            enemy.Initialize();
        }
        
        if (showLogs)
            Debug.Log($"Spawned {obj.name} from pool '{poolName}'");
        
        return obj;
    }
    
    public void Return(string poolName, GameObject obj)
    {
        if (!poolDictionary.ContainsKey(poolName))
        {
            Debug.LogError($"Pool '{poolName}' doesn't exist!");
            return;
        }
        
        obj.SetActive(false);
        poolDictionary[poolName].Enqueue(obj);
        activeCount[poolName]--;
        
        if (showLogs)
            Debug.Log($"Returned {obj.name} to pool '{poolName}'");
    }
    
    public int GetActiveCount(string poolName)
    {
        return activeCount.ContainsKey(poolName) ? activeCount[poolName] : 0;
    }
}
