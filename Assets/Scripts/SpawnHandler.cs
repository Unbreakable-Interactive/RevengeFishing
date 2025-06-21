using UnityEngine;
using System.Collections.Generic;

public class SpawnHandler : MonoBehaviour
{
    public static SpawnHandler Instance { get; private set; }

    [Header("Spawn Settings")]
    public float platformScanRadius = 15f;

    // Keep track of all platforms for notifications
    private List<Platform> allPlatforms = new List<Platform>();

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Basic setup - platform scanning will be done by GameBootstrap
        Debug.Log("SpawnHandler ready for initialization");
    }

    // Called by GameBootstrap to initialize properly
    public void Initialize()
    {
        RefreshPlatformList();
        Debug.Log("SpawnHandler initialized - platforms scanned");
    }

    public void RefreshPlatformList()
    {
        allPlatforms.Clear();
        allPlatforms.AddRange(FindObjectsOfType<Platform>());
        Debug.Log($"Found {allPlatforms.Count} platforms in scene");
    }

    // Call this whenever you spawn an enemy
    public GameObject SpawnEnemy(GameObject enemyPrefab, Vector3 position, Quaternion rotation = default)
    {
        // Spawn the enemy
        GameObject newEnemy = Instantiate(enemyPrefab, position, rotation);

        // Notify all platforms to rescan for new enemies
        NotifyPlatformsOfNewSpawn();

        Debug.Log($"Spawned {newEnemy.name} at {position}");
        return newEnemy;
    }

    // Call this whenever you spawn a boat (later)
    public GameObject SpawnBoat(GameObject boatPrefab, Vector3 position, Quaternion rotation = default)
    {
        // Spawn the boat
        GameObject newBoat = Instantiate(boatPrefab, position, rotation);

        // Notify all platforms to rescan (boat might have its own platform)
        NotifyPlatformsOfNewSpawn();

        Debug.Log($"Spawned {newBoat.name} at {position}");
        return newBoat;
    }

    private void NotifyPlatformsOfNewSpawn()
    {
        foreach (Platform platform in allPlatforms)
        {
            if (platform != null)
            {
                platform.ScanForNewEnemies(platformScanRadius);
            }
        }
    }

    // Call this if new platforms are added during gameplay
    public void RegisterNewPlatform(Platform platform)
    {
        if (!allPlatforms.Contains(platform))
        {
            allPlatforms.Add(platform);
        }
    }
}
