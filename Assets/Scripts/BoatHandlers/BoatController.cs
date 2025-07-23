using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoatController : MonoBehaviour
{
    [Header("Boat Configuration")]
    [SerializeField] private GameObject boatFishermanPrefab;
    [SerializeField] private Transform[] crewSpawnPoints;
    [SerializeField] private int minCrewMembers = 1;
    [SerializeField] private int maxCrewMembers = 2;
    
    [Header("Integrity System")]
    [SerializeField] private float baseBoatIntegrity = 0f;
    [SerializeField] private bool debugIntegrity = true;
    
    [Header("Runtime Info (Read Only)")]
    [SerializeField] private List<BoatFisherman> activeCrewMembers = new List<BoatFisherman>();
    [SerializeField] private float currentIntegrity;
    [SerializeField] private float maxIntegrity;
    [SerializeField] private bool isBoatDestroyed = false;
    
    [SerializeField] private BoatFloater boatFloater;
    [SerializeField] private BoatPlatform boatPlatform;
    
    public System.Action<float, float> OnIntegrityChanged;
    public System.Action OnBoatDestroyed;

    void Awake()
    {
        if (boatFloater == null)
            boatFloater = GetComponent<BoatFloater>();
        if (boatPlatform == null)
            boatPlatform = GetComponentInChildren<BoatPlatform>();
    }

    void Start()
    {
        StartCoroutine(InitializeBoatWithCrew());
    }

    IEnumerator InitializeBoatWithCrew()
    {
        yield return null;
        
        int crewCount = Mathf.Min(Random.Range(minCrewMembers, maxCrewMembers + 1), crewSpawnPoints.Length);
        
        if (debugIntegrity)
            Debug.Log($"Spawning {crewCount} BoatFishermanHandler prefabs for boat {gameObject.name}");
        
        for (int i = 0; i < crewCount; i++)
        {
            StartCoroutine(InstantiateAndAssignCrewMember(crewSpawnPoints[i]));
        }
        
        yield return null;
        
        CalculateBoatIntegrity();
        
        if (debugIntegrity)
            Debug.Log($"Boat {gameObject.name} initialized with {activeCrewMembers.Count} crew. Integrity: {currentIntegrity}/{maxIntegrity}");
    }

    IEnumerator InstantiateAndAssignCrewMember(Transform spawnPoint)
    {
        if (boatFishermanPrefab == null)
        {
            Debug.LogError($"No boatFishermanPrefab assigned to {gameObject.name}!");
            yield break;
        }
        
        GameObject crewHandlerObj = Instantiate(boatFishermanPrefab, spawnPoint.position, spawnPoint.rotation);
        crewHandlerObj.transform.SetParent(transform);
        
        yield return StartCoroutine(AssignToBoatPlatform(crewHandlerObj, spawnPoint.position));
        
        BoatFisherman boatFisherman = crewHandlerObj.GetComponentInChildren<BoatFisherman>();
        if (boatFisherman != null)
        {
            boatFisherman.SetBoatController(this);
            
            activeCrewMembers.Add(boatFisherman);
            
            boatFisherman.OnCrewMemberDefeated += OnCrewMemberDefeated;
            boatFisherman.OnCrewMemberEaten += OnCrewMemberEaten;
            
            if (debugIntegrity)
                Debug.Log($"Instantiated and configured BoatFisherman {boatFisherman.name} on boat {gameObject.name}");
        }
        else
        {
            Debug.LogError($"Instantiated BoatFishermanHandler doesn't contain BoatFisherman component!");
            Destroy(crewHandlerObj);
        }
    }

    IEnumerator AssignToBoatPlatform(GameObject enemy, Vector3 spawnPos)
    {
        yield return null;
        
        LandEnemy landEnemy = enemy.GetComponentInChildren<LandEnemy>();
        if (landEnemy != null)
        {
            BoatPlatform platform = FindNearestBoatPlatform(spawnPos);
            if (platform != null)
            {
                platform.RegisterEnemyAtRuntime(landEnemy);
                
                if (debugIntegrity)
                    Debug.Log($"Assigned {landEnemy.name} to BoatPlatform {platform.name}");
            }
        }
    }

    BoatPlatform FindNearestBoatPlatform(Vector3 spawnPos)
    {
        if (boatPlatform != null)
        {
            return boatPlatform;
        }
        
        BoatPlatform[] platforms = GetComponentsInChildren<BoatPlatform>();
        if (platforms.Length > 0)
        {
            return platforms[0];
        }
        
        Debug.LogError($"No BoatPlatform found for {gameObject.name}!");
        return null;
    }

    void CalculateBoatIntegrity()
    {
        float totalCrewPower = 0f;
        
        foreach (BoatFisherman crew in activeCrewMembers)
        {
            if (crew != null && crew.State == Enemy.EnemyState.Alive)
            {
                totalCrewPower += crew.PowerLevel;
            }
        }
        
        maxIntegrity = baseBoatIntegrity + totalCrewPower;
        currentIntegrity = maxIntegrity;
        
        OnIntegrityChanged?.Invoke(currentIntegrity, maxIntegrity);
    }

    void OnCrewMemberDefeated(BoatFisherman defeatedCrew)
    {
        if (debugIntegrity)
            Debug.Log($"Crew member {defeatedCrew.name} defeated. Recalculating boat integrity...");
        
        float powerLost = defeatedCrew.PowerLevel;
        currentIntegrity = Mathf.Max(0f, currentIntegrity - powerLost);
        
        OnIntegrityChanged?.Invoke(currentIntegrity, maxIntegrity);
        
        CheckBoatDestruction();
    }

    void OnCrewMemberEaten(BoatFisherman eatenCrew)
    {
        if (debugIntegrity)
            Debug.Log($"Crew member {eatenCrew.name} eaten. Removing from active crew...");
        
        if (boatPlatform != null)
        {
            boatPlatform.UnregisterEnemy(eatenCrew);
        }
        
        activeCrewMembers.Remove(eatenCrew);
        
        eatenCrew.OnCrewMemberDefeated -= OnCrewMemberDefeated;
        eatenCrew.OnCrewMemberEaten -= OnCrewMemberEaten;
        
        CheckBoatDestruction();
    }

    void CheckBoatDestruction()
    {
        int aliveCrewCount = 0;
        foreach (BoatFisherman crew in activeCrewMembers)
        {
            if (crew != null && crew.State == Enemy.EnemyState.Alive)
            {
                aliveCrewCount++;
            }
        }
        
        // if (aliveCrewCount == 0 || currentIntegrity <= 0f)
        // {
        //     DestroyBoat();
        // }
    }

    void DestroyBoat()
    {
        if (isBoatDestroyed) return;
        
        isBoatDestroyed = true;
        
        if (debugIntegrity)
            Debug.Log($"Boat {gameObject.name} destroyed!");
        
        OnBoatDestroyed?.Invoke();
        
        foreach (BoatFisherman crew in activeCrewMembers)
        {
            if (crew != null && crew.gameObject != null)
            {
                if (boatPlatform != null)
                {
                    boatPlatform.UnregisterEnemy(crew);
                }
                Destroy(crew.transform.parent.gameObject);
            }
        }
        activeCrewMembers.Clear();
        
        StartCoroutine(DestroyBoatDelayed());
    }
    
    IEnumerator DestroyBoatDelayed()
    {
        yield return new WaitForSeconds(1f);
        
        if (SimpleObjectPool.Instance != null)
        {
            SimpleObjectPool.Instance.ReturnToPool("BoatHandler", gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ResetForPooling()
    {
        if (boatPlatform != null)
        {
            foreach (BoatFisherman crew in activeCrewMembers)
            {
                if (crew != null)
                {
                    boatPlatform.UnregisterEnemy(crew);
                }
            }
        }
        
        foreach (BoatFisherman crew in activeCrewMembers)
        {
            if (crew != null && crew.gameObject != null)
            {
                Destroy(crew.transform.parent.gameObject);
            }
        }
        
        activeCrewMembers.Clear();
        currentIntegrity = 0f;
        maxIntegrity = 0f;
        isBoatDestroyed = false;
        
        StartCoroutine(InitializeBoatWithCrew());
    }

    public float GetCurrentIntegrity() => currentIntegrity;
    public float GetMaxIntegrity() => maxIntegrity;
    public int GetAliveCrewCount() 
    {
        int count = 0;
        foreach (BoatFisherman crew in activeCrewMembers)
        {
            if (crew != null && crew.State == Enemy.EnemyState.Alive) count++;
        }
        return count;
    }
    public List<BoatFisherman> GetActiveCrewMembers() => new List<BoatFisherman>(activeCrewMembers);
}
