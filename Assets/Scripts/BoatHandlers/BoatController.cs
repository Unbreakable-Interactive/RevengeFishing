using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoatController : MonoBehaviour
{
    [Header("Boat Properties")]
    [SerializeField] private float currentIntegrity;
    [SerializeField] private float maxIntegrity;
    [SerializeField] private bool isBoatDestroyed = false;
    
    
    [SerializeField] private BoatFloater boatFloater;
    [SerializeField] private BoatPlatform boatPlatform;
    
    
    [Header("Crew Properties")]
    [SerializeField] private GameObject boatFishermanPrefab;
    [SerializeField] private Transform[] crewSpawnPoints;
    [SerializeField] private int maxCrewMembers = 2;
    [SerializeField] private int maxInactiveCrewMembers = 1;
    
    [Space]
    [SerializeField] private List<Fisherman> allCrewMembers = new List<Fisherman>();
    
    [SerializeField] private Transform leftBoundary;
    [SerializeField] private Transform rightBoundary;
    
    
    public void Initialize(Transform _leftBoundary, Transform _rightBoundary)
    {
        leftBoundary = _leftBoundary;
        rightBoundary = _rightBoundary;
        
        boatFloater.InitializeBoundaries(_leftBoundary, _rightBoundary);
    }
    
    void OnEnable()
    {
        boatFloater.Initialize();
        if (allCrewMembers.Count == 0)
        {
            StartCoroutine(InitializeBoatWithCrew());
        }
        else
        {
            // StartCoroutine(ResetExistingCrew());
        }
    }
    
    
    IEnumerator InitializeBoatWithCrew()
    {
        yield return null;
    
        for (int i = 0; i < maxCrewMembers; i++)
        {
            yield return StartCoroutine(InstantiateAndAssignCrewMember(crewSpawnPoints[0].position, i));
        }
    
        yield return null;
    
        RandomlyDeactivateCrewMembers();
        CalculateBoatIntegrity();
    }
    
    IEnumerator InstantiateAndAssignCrewMember(Vector3 spawnPoint, int index)
    {
        if (boatFishermanPrefab == null)
        {
            Debug.LogError($"No boatFishermanPrefab assigned to {gameObject.name}!");
            yield break;
        }
    
        GameObject crewHandlerObj = Instantiate(boatFishermanPrefab, spawnPoint, Quaternion.identity);
        crewHandlerObj.transform.SetParent(transform);
    
        Fisherman fisherman = crewHandlerObj.GetComponentInChildren<Fisherman>();
        if (fisherman != null)
        {
            if (PowerLevelScaler.Instance != null)
            {
                int powerLevel = PowerLevelScaler.Instance.CalculateEnemyPowerLevel();
                fisherman.SetPowerLevel(powerLevel);
            }
        
            // fisherman.InitializeForBoat(boatPlatform, boatPlatform.PlatformCollider);
            yield return StartCoroutine(AssignToBoatPlatform(fisherman));
        
            allCrewMembers.Add(fisherman);
        }
        else
        {
            Debug.LogError($"Instantiated BoatFishermanHandler doesn't contain Fisherman component!");
            Destroy(crewHandlerObj);
        }
    }

    IEnumerator AssignToBoatPlatform(LandEnemy landEnemy)
    {
        yield return null;

        if (boatPlatform == null)
        {
            Debug.LogError($"No BoatPlatform found for {gameObject.name}!");
            yield break;
        }

        if (landEnemy != null)
        {
            boatPlatform.RegisterEnemyAtRuntime(landEnemy);
            landEnemy.OnPlatformAssigned(boatPlatform);
            yield return null;
        }
    }
    
    void RandomlyDeactivateCrewMembers()
    {
        if (allCrewMembers.Count == 0) return;
    
        foreach (Fisherman crew in allCrewMembers)
        {
            if (crew != null && crew.ParentContainer != null)
                crew.ParentContainer.SetActive(true);
        }
    
        int inactiveCount = Random.Range(0, Mathf.Min(maxInactiveCrewMembers + 1, allCrewMembers.Count));
    
        if (inactiveCount > 0)
        {
            List<Fisherman> availableToDeactivate = new List<Fisherman>(allCrewMembers);
        
            for (int i = 0; i < inactiveCount; i++)
            {
                if (availableToDeactivate.Count > 0)
                {
                    int randomIndex = Random.Range(0, availableToDeactivate.Count);
                    Fisherman toDeactivate = availableToDeactivate[randomIndex];
                    
                    if (toDeactivate.ParentContainer != null)
                    {
                        toDeactivate.ParentContainer.SetActive(false);
                    }
                    
                    availableToDeactivate.RemoveAt(randomIndex);
                }
            }
        }
    }
    
    void CalculateBoatIntegrity()
    {
        float totalCrewPower = 0f;
    
        foreach (Fisherman crew in allCrewMembers)
        {
            if (crew != null && 
                crew.ParentContainer != null && 
                crew.ParentContainer.activeInHierarchy && 
                crew.State == Enemy.EnemyState.Alive)
            {
                totalCrewPower += crew.PowerLevel;
            }
        }
    
        maxIntegrity = totalCrewPower;
        currentIntegrity = maxIntegrity;
    
        // OnIntegrityChanged?.Invoke(currentIntegrity, maxIntegrity);
    }
    
    int GetActiveCrewCount()
    {
        int count = 0;
        
        foreach (Fisherman crew in allCrewMembers)
        {
            if (crew != null && 
                crew.ParentContainer != null && 
                crew.ParentContainer.activeInHierarchy && 
                crew.State == Enemy.EnemyState.Alive)
            {
                count++;
            }
        }
        return count;
    }

    
    public void ResetForPooling()
    {
        currentIntegrity = 0f;
        maxIntegrity = 0f;
        isBoatDestroyed = false;
        
        if (allCrewMembers.Count == 0)
        {
            StartCoroutine(InitializeBoatWithCrew());
        }
        else
        {
            // StartCoroutine(ResetExistingCrew());
        }
    }

    public float GetCurrentIntegrity() => currentIntegrity;
    public float GetMaxIntegrity() => maxIntegrity;
    public List<Fisherman> GetAllCrewMembers() => new List<Fisherman>(allCrewMembers);

}
