using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoatCrewManager : MonoBehaviour, IBoatComponent
{
    [Header("Boat Identity")]
    [SerializeField] private BoatID boatID = new BoatID();
    
    [Header("Crew Properties")]
    [SerializeField] private GameObject boatFishermanPrefab;
    [SerializeField] private Transform[] crewSpawnPoints;
    [SerializeField] private int maxCrewMembers = 2;
    [SerializeField] private int maxInactiveCrewMembers = 1;
    
    [Space]
    [SerializeField] private List<BoatLandEnemy> allCrewMembers = new List<BoatLandEnemy>();
    
    private BoatPlatform boatPlatform;
    private BoatController boatController;
    private BoatIntegrityManager integrityManager;
    private BoatFloater boatFloater;
    private BoatPhysicsSystem physicsSystem;
    
    private BoatBoundaryTrigger leftBoundary;
    private BoatBoundaryTrigger rightBoundary;
    
    private static readonly List<Enemy> tempEnemyList = new List<Enemy>();
    
    public string GetBoatID() => boatID.UniqueID;
    public void SetBoatID(BoatID newBoatID) => boatID = newBoatID;
    
    public void Initialize(BoatPlatform platform, BoatController controller)
    {
        boatPlatform = platform;
        boatController = controller;
        integrityManager = controller.GetComponent<BoatIntegrityManager>();
        boatFloater = controller.BoatFloater;
        physicsSystem = controller.BoatPhysicsSystem;
        
        ValidateRequiredReferences();
        ConfigureBoatComponentIDs();
        
        GameLogger.LogError($"[CREW MANAGER] {gameObject.name} - Crew management initialized");
    }
    
    public void SetupBoundaries(BoatBoundaryTrigger left, BoatBoundaryTrigger right)
    {
        leftBoundary = left;
        rightBoundary = right;
        ConfigureBoatComponentIDs();
    }
    
    private void ValidateRequiredReferences()
    {
        if (boatPlatform == null)
            throw new System.Exception($"BoatCrewManager on {gameObject.name}: boatPlatform must be assigned!");
    }
    
    private void ConfigureBoatComponentIDs()
    {
        boatPlatform.SetBoatID(boatID);
        
        if (leftBoundary != null)
            leftBoundary.SetBoatID(boatID);
            
        if (rightBoundary != null)
            rightBoundary.SetBoatID(boatID);
        
        GameLogger.LogVerbose($"BoatCrewManager: Configured boat ID {boatID} for all components");
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
        platformPosition.y += 0.1f;
        
        List<BoatLandEnemy> newCrewMembers = new List<BoatLandEnemy>();
        
        for (int i = 0; i < maxCrewMembers; i++)
        {
            Vector3 spawnPosition = platformPosition + new Vector3(i * 0.8f - 0.4f, 0, 0);
            BoatLandEnemy boatFisherman = InstantiateCrewMember(spawnPosition, i);
            
            if (boatFisherman != null)
            {
                newCrewMembers.Add(boatFisherman);
            }
        }
        
        yield return new WaitForEndOfFrame();
        
        for (int i = 0; i < newCrewMembers.Count; i++)
        {
            BoatLandEnemy boatFisherman = newCrewMembers[i];
            ConfigureCrewMemberForBoatLife(boatFisherman);
            RegisterCrewMemberToPlatform(boatFisherman);
            SubscribeToCrewMember(boatFisherman);
            allCrewMembers.Add(boatFisherman);
        }
        
        yield return new WaitForEndOfFrame();
        
        if (physicsSystem != null)
        {
            physicsSystem.ConfigureCrewPhysicsIsolation(newCrewMembers);
        }
        
        if (boatFloater != null)
        {
            tempEnemyList.Clear();
            for (int i = 0; i < allCrewMembers.Count; i++)
            {
                tempEnemyList.Add(allCrewMembers[i]);
            }
            boatFloater.InitializeCrew(tempEnemyList);
        }
        
        RandomlyDeactivateCrewMembers();
        
        if (integrityManager != null)
        {
            integrityManager.CalculateBoatIntegrity();
        }
        
        GameLogger.LogError($"[CREW INIT] {gameObject.name} - Initialized {allCrewMembers.Count} crew members");
    }
    
    private BoatLandEnemy InstantiateCrewMember(Vector3 spawnPosition, int index)
    {
        if (boatFishermanPrefab == null)
        {
            GameLogger.LogError($"No boatFishermanPrefab assigned to {gameObject.name}!");
            return null;
        }
        
        GameObject crewHandlerObj = Instantiate(boatFishermanPrefab, spawnPosition, Quaternion.identity);
        crewHandlerObj.transform.SetParent(transform);
        
        BoatLandEnemy boatFisherman = crewHandlerObj.GetComponentInChildren<BoatLandEnemy>();
        if (boatFisherman != null)
        {
            if (PowerLevelScaler.Instance != null)
            {
                int powerLevel = PowerLevelScaler.Instance.CalculateEnemyPowerLevel();
                boatFisherman.SetPowerLevel(powerLevel);
            }
            
            Rigidbody2D fishermanRb = boatFisherman.GetComponent<Rigidbody2D>();
            if (fishermanRb != null)
            {
                fishermanRb.velocity = Vector2.zero;
                fishermanRb.angularVelocity = 0f;
            }
            
            return boatFisherman;
        }
        else
        {
            GameLogger.LogError($"Instantiated BoatFishermanHandler doesn't contain BoatLandEnemy component!");
            Destroy(crewHandlerObj);
            return null;
        }
    }
    
    private void ConfigureCrewMemberForBoatLife(BoatLandEnemy boatFisherman)
    {
        boatFisherman.isOnBoat = true;
        boatFisherman.SetBoatID(boatID);
        
        Rigidbody2D fishermanRb = boatFisherman.GetComponent<Rigidbody2D>();
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
        
        GameLogger.LogError($"[CREW CONFIG] {boatFisherman.name} configured for boat life - isOnBoat: {boatFisherman.isOnBoat}");
    }
    
    private void RegisterCrewMemberToPlatform(BoatLandEnemy boatFisherman)
    {
        boatPlatform.RegisterEnemyAtRuntime(boatFisherman);
        
        boatFisherman.SetAssignedPlatform(boatPlatform);
        boatFisherman.OnPlatformAssigned(boatPlatform);
        
        GameLogger.LogError($"[PLATFORM ASSIGN] {boatFisherman.name} assigned to platform {boatPlatform.name}");
    }
    
    private void SubscribeToCrewMember(BoatLandEnemy boatFisherman)
    {
        if (boatFisherman != null)
        {
            boatFisherman.OnEnemyDied -= OnCrewMemberDied;
            boatFisherman.OnEnemyDied += OnCrewMemberDied;
        }
    }
    
    public void OnCrewMemberDied(Enemy deadCrew)
    {
        StartCoroutine(DelayedIntegrityRecalculation());
    }
    
    public void HandleCrewMemberDeath(BoatLandEnemy deadCrewMember)
    {
        if (!BoatEnemyBelongToBoat(deadCrewMember))
        {
            GameLogger.LogWarning($"BoatCrewManager: {deadCrewMember.name} doesn't belong to this boat");
            return;
        }
        
        ResetCrewMemberToInactive(deadCrewMember);
        StartCoroutine(DelayedIntegrityRecalculation());
    }

    private void ResetCrewMemberToInactive(BoatLandEnemy crewMember)
    {
        crewMember.StopAllCoroutines();
        
        crewMember.ChangeState_Alive();
        crewMember.ResetFatigue();
        
        Collider2D collider = crewMember.BodyCollider;
        if (collider != null)
        {
            collider.isTrigger = false;
            collider.enabled = true;
        }
        
        Rigidbody2D rb = crewMember.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 1f;
            rb.simulated = true;
            rb.freezeRotation = true;
        }
        
        if (crewMember.ParentContainer != null)
        {
            Vector3 resetPosition = boatPlatform.transform.position;
            resetPosition.y += 0.1f;
            
            int memberIndex = allCrewMembers.IndexOf(crewMember);
            if (memberIndex >= 0)
            {
                resetPosition += new Vector3(memberIndex * 0.8f - 0.4f, 0, 0);
            }
            
            crewMember.ParentContainer.transform.position = resetPosition;
            crewMember.ParentContainer.SetActive(false);
        }
        
        ConfigureCrewMemberForBoatLife(crewMember);
        
        crewMember.isReturningToPool = false;
        
        GameLogger.LogVerbose($"BoatCrewManager: Reset {crewMember.name} to inactive state in boat");
    }
    
    private IEnumerator DelayedIntegrityRecalculation()
    {
        yield return null;
        
        if (integrityManager != null)
        {
            integrityManager.CalculateBoatIntegrity();
            integrityManager.CheckBoatDestruction();
        }
    }
    
    private void RandomlyDeactivateCrewMembers()
    {
        if (allCrewMembers.Count == 0) return;
        
        for (int i = 0; i < allCrewMembers.Count; i++)
        {
            BoatLandEnemy crew = allCrewMembers[i];
            if (crew != null && crew.ParentContainer != null)
                crew.ParentContainer.SetActive(true);
        }
        
        int inactiveCount = Random.Range(0, Mathf.Min(maxInactiveCrewMembers + 1, allCrewMembers.Count));
        
        if (inactiveCount > 0)
        {
            List<BoatLandEnemy> availableToDeactivate = new List<BoatLandEnemy>(allCrewMembers);
            
            for (int i = 0; i < inactiveCount; i++)
            {
                if (availableToDeactivate.Count > 0)
                {
                    int randomIndex = Random.Range(0, availableToDeactivate.Count);
                    BoatLandEnemy toDeactivate = availableToDeactivate[randomIndex];
                    
                    if (toDeactivate.ParentContainer != null)
                    {
                        toDeactivate.ParentContainer.SetActive(false);
                    }
                    
                    availableToDeactivate.RemoveAt(randomIndex);
                }
            }
        }
        
        GameLogger.LogError($"[CREW DEACTIVATE] {gameObject.name} - Deactivated {inactiveCount} crew members");
    }
    
    private IEnumerator ResetExistingCrew()
    {
        Vector3 platformPosition = boatPlatform.transform.position;
        platformPosition.y += 0.1f;
        
        for (int i = 0; i < allCrewMembers.Count; i++)
        {
            BoatLandEnemy boatFisherman = allCrewMembers[i];
            if (boatFisherman != null)
            {
                Vector3 resetPosition = platformPosition + new Vector3(i * 0.8f - 0.4f, 0, 0);
                
                if (boatFisherman.ParentContainer != null)
                {
                    boatFisherman.ParentContainer.transform.position = resetPosition;
                    boatFisherman.ParentContainer.SetActive(true);
                }
                
                ResetCrewMemberToActive(boatFisherman);
                ConfigureCrewMemberForBoatLife(boatFisherman);
                SubscribeToCrewMember(boatFisherman);
                RegisterCrewMemberToPlatform(boatFisherman);
            }
        }
        
        yield return new WaitForEndOfFrame();
        
        if (physicsSystem != null)
        {
            physicsSystem.ConfigureCrewPhysicsIsolation(allCrewMembers);
        }
        
        yield return null;
        
        RandomlyDeactivateCrewMembers();
        
        if (integrityManager != null)
        {
            integrityManager.CalculateBoatIntegrity();
        }
        
        GameLogger.LogError($"[CREW RESET] {gameObject.name} - Reset {allCrewMembers.Count} existing crew members");
    }

    private void ResetCrewMemberToActive(BoatLandEnemy crewMember)
    {
        crewMember.TriggerAlive();
        crewMember.ScheduleNextAction();
        
        Rigidbody2D fishermanRb = crewMember.GetComponent<Rigidbody2D>();
        if (fishermanRb != null)
        {
            fishermanRb.velocity = Vector2.zero;
            fishermanRb.angularVelocity = 0f;
        }
        
        crewMember.isReturningToPool = false;
    }
    
    public int GetActiveCrewCount()
    {
        int count = 0;
        
        for (int i = 0; i < allCrewMembers.Count; i++)
        {
            BoatLandEnemy crew = allCrewMembers[i];
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
    
    public List<BoatLandEnemy> GetAllCrewMembers() => allCrewMembers;
    
    public bool BoatEnemyBelongToBoat(BoatLandEnemy enemy)
    {
        if (enemy == null) return false;
        return boatID.Matches(enemy.GetBoatID());
    }
    
    public void Reset()
    {
        for (int i = 0; i < allCrewMembers.Count; i++)
        {
            BoatLandEnemy crew = allCrewMembers[i];
            if (crew != null)
            {
                crew.OnEnemyDied -= OnCrewMemberDied;
            }
        }
        
        StartCrewInitialization();
        
        GameLogger.LogError($"[CREW RESET] {gameObject.name} - Crew manager reset completed");
    }
}
