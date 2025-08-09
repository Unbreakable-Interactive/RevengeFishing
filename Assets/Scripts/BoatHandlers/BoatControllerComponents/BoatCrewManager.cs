using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoatCrewManager : MonoBehaviour, IBoatComponent
{
    [Header("Boat Identity")]
    [SerializeField] private BoatID boatID = new BoatID();
    
    [Header("Required References")]
    [SerializeField] private BoatPlatform boatPlatform;
    [SerializeField] private BoatBoundaryTrigger leftBoundary;
    [SerializeField] private BoatBoundaryTrigger rightBoundary;
    
    [Header("Crew Properties")]
    [SerializeField] private GameObject boatFishermanPrefab;
    [SerializeField] private Transform[] crewSpawnPoints;
    [SerializeField] private int maxCrewMembers = 2;
    [SerializeField] private int maxInactiveCrewMembers = 1;
    
    [Space]
    [SerializeField] private List<BoatLandEnemy> allCrewMembers = new List<BoatLandEnemy>();
    
    private BoatIntegrityManager integrityManager;
    private BoatFloater boatFloater;
    
    private static readonly List<BoatCrewManager> allBoatManagers = new List<BoatCrewManager>();
    private static readonly List<Collider2D> tempColliderList = new List<Collider2D>();
    private static readonly List<Enemy> tempEnemyList = new List<Enemy>();
    
    private Collider2D[] cachedMyColliders;
    private Collider2D cachedPlatformCollider;
    private bool collidersAreCached = false;
    
    public string GetBoatID() => boatID.UniqueID;
    public void SetBoatID(BoatID newBoatID) => boatID = newBoatID;
    
    private void Awake()
    {
        ValidateRequiredReferences();
        allBoatManagers.Add(this);
    }
    
    private void OnDestroy()
    {
        allBoatManagers.Remove(this);
    }
    
    private void Start()
    {
        SetupBoatPhysicsIsolationOptimized();
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
    
    private void CacheColliders()
    {
        if (collidersAreCached) return;
        
        cachedMyColliders = GetComponentsInChildren<Collider2D>();
        cachedPlatformCollider = boatPlatform.GetComponent<Collider2D>();
        collidersAreCached = true;
    }
    
    private void SetupBoatPhysicsIsolationOptimized()
    {
        CacheColliders();
        
        for (int i = 0; i < allBoatManagers.Count; i++)
        {
            BoatCrewManager otherBoat = allBoatManagers[i];
            if (otherBoat == this) continue;
            
            otherBoat.CacheColliders();
            
            for (int j = 0; j < cachedMyColliders.Length; j++)
            {
                for (int k = 0; k < otherBoat.cachedMyColliders.Length; k++)
                {
                    Physics2D.IgnoreCollision(cachedMyColliders[j], otherBoat.cachedMyColliders[k], true);
                }
            }
            
            GameLogger.LogVerbose($"BoatCrewManager: ISOLATED boat physics between {gameObject.name} and {otherBoat.gameObject.name}");
        }
    }
    
    public void Initialize(BoatPlatform platform, BoatIntegrityManager integrity, BoatFloater floater)
    {
        integrityManager = integrity;
        boatFloater = floater;
        
        ConfigureBoatComponentIDs();
    }
    
    private void ConfigureBoatComponentIDs()
    {
        boatPlatform.SetBoatID(boatID);
        leftBoundary.SetBoatID(boatID);
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
        platformPosition.y += 0.5f;
        
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
            ConfigureFishermanForBoatLife(boatFisherman);
            boatPlatform.RegisterEnemyAtRuntime(boatFisherman);
            boatFisherman.OnPlatformAssigned(boatPlatform);
            SubscribeToCrewMember(boatFisherman);
            allCrewMembers.Add(boatFisherman);
        }
        
        yield return new WaitForEndOfFrame();
        
        ConfigureCrewPhysicsIsolationOptimized(newCrewMembers);
        
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
        integrityManager.CalculateBoatIntegrity();
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
    
    private void ConfigureFishermanForBoatLife(BoatLandEnemy boatFisherman)
    {
        boatFisherman.isOnBoat = true;
        
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
    }
    
    private void ConfigureCrewPhysicsIsolationOptimized(List<BoatLandEnemy> crewMembers)
    {
        for (int crewIndex = 0; crewIndex < crewMembers.Count; crewIndex++)
        {
            BoatLandEnemy boatFisherman = crewMembers[crewIndex];
            Collider2D fishermanCollider = boatFisherman.GetComponent<Collider2D>();
            if (fishermanCollider == null) continue;
            
            for (int boatIndex = 0; boatIndex < allBoatManagers.Count; boatIndex++)
            {
                BoatCrewManager otherBoat = allBoatManagers[boatIndex];
                if (otherBoat == this) continue;
                
                string otherBoatID = otherBoat.GetBoatID();
                if (otherBoatID == GetBoatID()) continue;
                
                for (int colliderIndex = 0; colliderIndex < otherBoat.cachedMyColliders.Length; colliderIndex++)
                {
                    Physics2D.IgnoreCollision(fishermanCollider, otherBoat.cachedMyColliders[colliderIndex], true);
                }
                
                for (int otherCrewIndex = 0; otherCrewIndex < otherBoat.allCrewMembers.Count; otherCrewIndex++)
                {
                    BoatLandEnemy otherFisherman = otherBoat.allCrewMembers[otherCrewIndex];
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
            
            for (int otherCrewIndex = 0; otherCrewIndex < crewMembers.Count; otherCrewIndex++)
            {
                if (otherCrewIndex == crewIndex) continue;
                
                BoatLandEnemy otherCrewMember = crewMembers[otherCrewIndex];
                Collider2D otherCrewCollider = otherCrewMember.GetComponent<Collider2D>();
                if (otherCrewCollider != null)
                {
                    Physics2D.IgnoreCollision(fishermanCollider, otherCrewCollider, true);
                }
            }
            
            if (cachedPlatformCollider != null)
            {
                Physics2D.IgnoreCollision(fishermanCollider, cachedPlatformCollider, false);
            }
        }
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
            resetPosition.y += 0.5f;
            
            int memberIndex = allCrewMembers.IndexOf(crewMember);
            if (memberIndex >= 0)
            {
                resetPosition += new Vector3(memberIndex * 0.8f - 0.4f, 0, 0);
            }
            
            crewMember.ParentContainer.transform.position = resetPosition;
            crewMember.ParentContainer.SetActive(false);
        }
        
        ConfigureFishermanForBoatLife(crewMember);
        
        crewMember.isReturningToPool = false;
        
        GameLogger.LogVerbose($"BoatCrewManager: Reset {crewMember.name} to inactive state in boat");
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
    }
    
    private IEnumerator ResetExistingCrew()
    {
        Vector3 platformPosition = boatPlatform.transform.position;
        platformPosition.y += 0.5f;
        
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
                
                ConfigureFishermanForBoatLife(boatFisherman);
                SubscribeToCrewMember(boatFisherman);
                
                boatPlatform.RegisterEnemyAtRuntime(boatFisherman);
                boatFisherman.OnPlatformAssigned(boatPlatform);
            }
        }
        
        yield return new WaitForEndOfFrame();
        
        ConfigureCrewPhysicsIsolationOptimized(allCrewMembers);
        
        yield return null;
        
        RandomlyDeactivateCrewMembers();
        integrityManager.CalculateBoatIntegrity();
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
    }

    public bool BoatEnemyBelongToBoat(BoatLandEnemy enemy)
    {
        if (enemy == null) return false;
        return boatID.Matches(enemy.GetBoatID());
    }
}
