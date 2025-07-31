using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoatCrewManager : MonoBehaviour, IBoatComponent
{
    [Header("Boat Identity - ASSIGN IN EDITOR")]
    [SerializeField] private BoatID boatID = new BoatID();
    
    [Header("Required References - ASSIGN IN EDITOR")]
    [SerializeField] private BoatPlatform boatPlatform;
    [SerializeField] private BoatBoundaryTrigger leftBoundary;
    [SerializeField] private BoatBoundaryTrigger rightBoundary;
    
    [Header("Crew Properties")]
    [SerializeField] private GameObject boatFishermanPrefab;
    [SerializeField] private Transform[] crewSpawnPoints;
    [SerializeField] private int maxCrewMembers = 2;
    [SerializeField] private int maxInactiveCrewMembers = 1;
    
    [Space]
    [SerializeField] private List<Fisherman> allCrewMembers = new List<Fisherman>();
    
    private BoatIntegrityManager integrityManager;
    private BoatFloater boatFloater;
    
    public string GetBoatID() => boatID.UniqueID;
    public void SetBoatID(BoatID newBoatID) => boatID = newBoatID;
    
    private void Awake()
    {
        ValidateRequiredReferences();
    }
    
    private void Start()
    {
        SetupBoatPhysicsIsolation();
    }
    
    private void ValidateRequiredReferences()
    {
        if (boatPlatform == null)
            throw new System.Exception($"BoatCrewManager on {gameObject.name}: boatPlatform must be assigned in editor!");
            
        if (leftBoundary == null)
            throw new System.Exception($"BoatCrewManager on {gameObject.name}: leftBoundary must be assigned in editor!");
            
        if (rightBoundary == null)
            throw new System.Exception($"BoatCrewManager on {gameObject.name}: rightBoundary must be assigned in editor!");
    }
    
    private void SetupBoatPhysicsIsolation()
    {
        BoatCrewManager[] allBoats = FindObjectsOfType<BoatCrewManager>();
        
        foreach (BoatCrewManager otherBoat in allBoats)
        {
            if (otherBoat == this) continue;
            
            Collider2D[] myColliders = GetComponentsInChildren<Collider2D>();
            Collider2D[] otherColliders = otherBoat.GetComponentsInChildren<Collider2D>();
            
            foreach (Collider2D myCollider in myColliders)
            {
                foreach (Collider2D otherCollider in otherColliders)
                {
                    Physics2D.IgnoreCollision(myCollider, otherCollider, true);
                }
            }
            
            Debug.Log($"BoatCrewManager: ISOLATED boat physics between {gameObject.name} and {otherBoat.gameObject.name}");
        }
    }
    
    public void Initialize(BoatPlatform platform, BoatIntegrityManager integrity)
    {
        integrityManager = integrity;
        boatFloater = GetComponent<BoatController>().BoatFloater;
        
        ConfigureBoatComponentIDs();
    }
    
    private void ConfigureBoatComponentIDs()
    {
        boatPlatform.SetBoatID(boatID);
        leftBoundary.SetBoatID(boatID);
        rightBoundary.SetBoatID(boatID);
        
        Debug.Log($"BoatCrewManager: Configured boat ID {boatID} for all components");
    }
    
    public void StartCrewInitialization()
    {
        if (allCrewMembers.Count == 0)
        {
            StartCoroutine(InitializeBoatWithCrew());
        }
        else
        {
            StartCoroutine(ResetExistingCrew());
        }
    }
    
    private IEnumerator InitializeBoatWithCrew()
    {
        allCrewMembers.Clear();
        
        Vector3 platformPosition = boatPlatform.transform.position;
        platformPosition.y += 0.5f;
        
        List<Fisherman> newCrewMembers = new List<Fisherman>();
        
        for (int i = 0; i < maxCrewMembers; i++)
        {
            Vector3 spawnPosition = platformPosition + new Vector3(i * 0.8f - 0.4f, 0, 0);
            Fisherman fisherman = InstantiateCrewMember(spawnPosition, i);
            
            if (fisherman != null)
            {
                newCrewMembers.Add(fisherman);
            }
        }
        
        yield return new WaitForEndOfFrame();
        
        foreach (Fisherman fisherman in newCrewMembers)
        {
            ConfigureFishermanForBoatLife(fisherman);
            boatPlatform.RegisterEnemyAtRuntime(fisherman);
            fisherman.OnPlatformAssigned(boatPlatform);
            SubscribeToCrewMember(fisherman);
            allCrewMembers.Add(fisherman);
        }
        
        yield return new WaitForEndOfFrame();
        
        ConfigureCrewPhysicsIsolation(newCrewMembers);
        
        if (boatFloater != null)
        {
            boatFloater.InitializeCrew(allCrewMembers);
        }
        
        RandomlyDeactivateCrewMembers();
        integrityManager.CalculateBoatIntegrity();
    }
    
    private Fisherman InstantiateCrewMember(Vector3 spawnPosition, int index)
    {
        if (boatFishermanPrefab == null)
        {
            Debug.LogError($"No boatFishermanPrefab assigned to {gameObject.name}!");
            return null;
        }
        
        GameObject crewHandlerObj = Instantiate(boatFishermanPrefab, spawnPosition, Quaternion.identity);
        crewHandlerObj.transform.SetParent(transform);
        
        Fisherman fisherman = crewHandlerObj.GetComponentInChildren<Fisherman>();
        if (fisherman != null)
        {
            fisherman.SetBoatID(boatID);
            
            if (PowerLevelScaler.Instance != null)
            {
                int powerLevel = PowerLevelScaler.Instance.CalculateEnemyPowerLevel();
                fisherman.SetPowerLevel(powerLevel);
            }
            
            Rigidbody2D fishermanRb = fisherman.GetComponent<Rigidbody2D>();
            if (fishermanRb != null)
            {
                fishermanRb.velocity = Vector2.zero;
                fishermanRb.angularVelocity = 0f;
            }
            
            return fisherman;
        }
        else
        {
            Debug.LogError($"Instantiated BoatFishermanHandler doesn't contain Fisherman component!");
            Destroy(crewHandlerObj);
            return null;
        }
    }
    
    private void ConfigureFishermanForBoatLife(Fisherman fisherman)
    {
        fisherman.isOnBoat = true;
    
        Rigidbody2D fishermanRb = fisherman.GetComponent<Rigidbody2D>();
        if (fishermanRb != null)
        {
            fishermanRb.mass = 0.8f;
            fishermanRb.drag = 1.5f;
            fishermanRb.angularDrag = 2f;
            fishermanRb.gravityScale = 1f;
            fishermanRb.constraints = RigidbodyConstraints2D.FreezeRotation;
            fishermanRb.interpolation = RigidbodyInterpolation2D.Interpolate;
            fishermanRb.velocity = Vector2.zero;
            fishermanRb.angularVelocity = 0f;
        }
    }
    
    private void ConfigureCrewPhysicsIsolation(List<Fisherman> crewMembers)
    {
        BoatCrewManager[] allBoats = FindObjectsOfType<BoatCrewManager>();
        
        foreach (Fisherman fisherman in crewMembers)
        {
            Collider2D fishermanCollider = fisherman.GetComponent<Collider2D>();
            if (fishermanCollider == null) continue;
            
            foreach (BoatCrewManager otherBoat in allBoats)
            {
                if (otherBoat == this) continue;
                
                string otherBoatID = otherBoat.GetBoatID();
                if (otherBoatID != GetBoatID())
                {
                    Collider2D[] otherBoatColliders = otherBoat.GetComponentsInChildren<Collider2D>();
                    foreach (Collider2D otherCollider in otherBoatColliders)
                    {
                        Physics2D.IgnoreCollision(fishermanCollider, otherCollider, true);
                    }
                    
                    foreach (Fisherman otherFisherman in otherBoat.allCrewMembers)
                    {
                        if (otherFisherman != null)
                        {
                            Collider2D otherFishermanCollider = otherFisherman.GetComponent<Collider2D>();
                            if (otherFishermanCollider != null)
                            {
                                Physics2D.IgnoreCollision(fishermanCollider, otherFishermanCollider, true);
                            }
                        }
                    }
                }
            }
            
            foreach (Fisherman otherCrewMember in crewMembers)
            {
                if (otherCrewMember != fisherman)
                {
                    Collider2D otherCrewCollider = otherCrewMember.GetComponent<Collider2D>();
                    if (otherCrewCollider != null)
                    {
                        Physics2D.IgnoreCollision(fishermanCollider, otherCrewCollider, true);
                    }
                }
            }
            
            if (boatPlatform != null)
            {
                Collider2D platformCollider = boatPlatform.GetComponent<Collider2D>();
                if (platformCollider != null)
                {
                    Physics2D.IgnoreCollision(fishermanCollider, platformCollider, false);
                }
            }
        }
    }
    
    private void SubscribeToCrewMember(Fisherman fisherman)
    {
        if (fisherman != null)
        {
            fisherman.OnEnemyDied -= OnCrewMemberDied;
            fisherman.OnEnemyDied += OnCrewMemberDied;
        }
    }
    
    public void OnCrewMemberDied(Enemy deadCrew)
    {
        StartCoroutine(DelayedIntegrityRecalculation());
    }
    
    private IEnumerator DelayedIntegrityRecalculation()
    {
        yield return null;
        
        integrityManager.CalculateBoatIntegrity();
        integrityManager.CheckBoatDestruction();
    }
    
    private void RandomlyDeactivateCrewMembers()
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
    
    private IEnumerator ResetExistingCrew()
    {
        Vector3 platformPosition = boatPlatform.transform.position;
        platformPosition.y += 0.5f;
        
        for (int i = 0; i < allCrewMembers.Count; i++)
        {
            Fisherman fisherman = allCrewMembers[i];
            if (fisherman != null)
            {
                fisherman.SetBoatID(boatID);
                
                Vector3 resetPosition = platformPosition + new Vector3(i * 0.8f - 0.4f, 0, 0);
                fisherman.transform.position = resetPosition;
                
                Rigidbody2D fishermanRb = fisherman.GetComponent<Rigidbody2D>();
                if (fishermanRb != null)
                {
                    fishermanRb.velocity = Vector2.zero;
                    fishermanRb.angularVelocity = 0f;
                }
                
                if (fisherman.ParentContainer != null)
                {
                    fisherman.ParentContainer.SetActive(true);
                }
                
                fisherman.TriggerAlive();
                fisherman.ScheduleNextAction();
                
                ConfigureFishermanForBoatLife(fisherman);
                SubscribeToCrewMember(fisherman);
                
                boatPlatform.RegisterEnemyAtRuntime(fisherman);
                fisherman.OnPlatformAssigned(boatPlatform);
            }
        }
        
        yield return new WaitForEndOfFrame();
        
        ConfigureCrewPhysicsIsolation(allCrewMembers);
        
        yield return null;
        
        RandomlyDeactivateCrewMembers();
        integrityManager.CalculateBoatIntegrity();
    }
    
    public int GetActiveCrewCount()
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
    
    public List<Fisherman> GetAllCrewMembers() => new List<Fisherman>(allCrewMembers);
    
    public void Reset()
    {
        foreach (Fisherman crew in allCrewMembers)
        {
            if (crew != null)
            {
                crew.OnEnemyDied -= OnCrewMemberDied;
            }
        }
        
        StartCrewInitialization();
    }

    public bool LandEnemyBelongToBoat(LandEnemy enemy)
    {
        if (enemy == null) return false;
        
        if (enemy is IBoatComponent boatComponent)
        {
            return boatID.Matches(boatComponent.GetBoatID());
        }
        
        return false;
    }

    [ContextMenu("🔄 Generate New Boat ID")]
    public void GenerateNewBoatID()
    {
        boatID.GenerateNewID();
        Debug.Log($"Generated new boat ID: {boatID}");
        
        if (Application.isPlaying)
        {
            ConfigureBoatComponentIDs();
        }
    }
    
    [ContextMenu("🔍 Debug Boat Info")]
    public void DebugBoatInfo()
    {
        Debug.Log($"=== BOAT INFO ===");
        Debug.Log($"Boat ID: {boatID}");
        Debug.Log($"Crew Members: {allCrewMembers.Count}");
        Debug.Log($"Platform: {boatPlatform?.name ?? "NULL"}");
        Debug.Log($"Left Boundary: {leftBoundary?.name ?? "NULL"}");
        Debug.Log($"Right Boundary: {rightBoundary?.name ?? "NULL"}");
        
        foreach (Fisherman crew in allCrewMembers)
        {
            if (crew != null && crew is IBoatComponent component)
            {
                Debug.Log($"Crew {crew.name}: ID = {component.GetBoatID()}");
            }
        }
    }
}

