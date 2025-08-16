using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BoatCrewManager : MonoBehaviour, IBoatComponent
{
    [Header("Crew Configuration")]
    [SerializeField] private GameObject crewMemberPrefab;
    [SerializeField] private int maxCrewSize = 2;
    [SerializeField] private int maxCrewToDeactivate = 1;
    [SerializeField] private float crewSpawnDelay = 0.5f;
    [SerializeField] private float crewHeightAbovePlatform = 0.1f;
    
    [Header("Crew Behavior")]
    [SerializeField] private bool allowCrewRandomDeactivation = true;
    
    [Header("References")]
    [SerializeField] private BoatID boatID;
    [SerializeField] private BoatController boatController;
    [SerializeField] private BoatPlatform boatPlatform;
    [SerializeField] private BoatFloater boatFloater;
    [SerializeField] private Transform crewContainer;
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    
    [Header("Boundaries")]
    [SerializeField] private BoatBoundaryTrigger leftBoundary;
    [SerializeField] private BoatBoundaryTrigger rightBoundary;
    [SerializeField] private float calculatedLeftBoundary = -1.5f;
    [SerializeField] private float calculatedRightBoundary = 1.5f;
    
    [Header("Debug & Monitoring")]
    [SerializeField] private bool debugCrewManager = true;
    [SerializeField] private bool trackCrewDisappearance = true;
    [SerializeField] private bool showPlatformGizmos = true;
    [SerializeField] private bool logCrewStates = true;
    
    [SerializeField] private List<BoatLandEnemy> allCrewMembers = new List<BoatLandEnemy>();
    private List<GameObject> crewHandlerRoots = new List<GameObject>();
    private List<Vector3> originalCrewPositions = new List<Vector3>();
    private List<bool> originalCrewActiveStates = new List<bool>();
    [SerializeField] private BoatLandEnemy currentNavigator = null;
    private bool isInitialized = false;
    private int currentActiveCrewCount = 0;
    private int minGuaranteedCrew = 1;
    
    private Vector3 platformTopWorld;
    private Vector3 platformTopLocal;
    private float calculatedPlatformHeight;
    
    public string GetBoatID() => boatID?.UniqueID ?? "NO_ID";
    public void SetBoatID(BoatID newBoatID) => boatID = newBoatID;
    
    public void Initialize(BoatController controller, BoatPlatform platform, BoatFloater floater)
    {
        if (isInitialized) return;
        
        boatController = controller;
        boatPlatform = platform;
        boatFloater = floater;
        
        if (crewContainer == null)
            crewContainer = transform;
        
        CalculatePlatformBounds();
        
        isInitialized = true;
        
        if (debugCrewManager)
            GameLogger.LogVerbose($"[CREW MANAGER] {GetBoatID()} - Initialized with max crew: {maxCrewSize}, max deactivate: {maxCrewToDeactivate}");
    }
    
    public void SetupBoundaries(BoatBoundaryTrigger left, BoatBoundaryTrigger right)
    {
        leftBoundary = left;
        rightBoundary = right;
        
        if (debugCrewManager)
            GameLogger.LogVerbose($"[CREW BOUNDARIES] {GetBoatID()} - Boundaries set: Left={calculatedLeftBoundary:F2}, Right={calculatedRightBoundary:F2}");
    }
    
    public void StartCrewInitialization()
    {
        if (!isInitialized)
        {
            GameLogger.LogError($"[CREW MANAGER] {GetBoatID()} - Cannot start crew initialization before Initialize() is called!");
            return;
        }
        
        StartCoroutine(InitializeCrewInstantly());
    }
    
    public IEnumerator InitializeCrewInstantly()
    {
        if (!isInitialized)
        {
            GameLogger.LogError($"[CREW MANAGER] {GetBoatID()} - Cannot start crew initialization before Initialize() is called!");
            yield break;
        }
        
        if (debugCrewManager)
            GameLogger.LogVerbose($"[CREW INSTANT INIT] {GetBoatID()} - Starting instant crew initialization (max: {maxCrewSize})");
        
        for (int i = 0; i < maxCrewSize; i++)
        {
            Vector3 spawnPosition = CalculateCrewMemberWorldPosition(i);
            BoatLandEnemy newCrewMember = SpawnCrewMember(spawnPosition, i);
            
            if (newCrewMember != null)
            {
                allCrewMembers.Add(newCrewMember);
                GameObject handlerRoot = newCrewMember.transform.parent?.gameObject ?? newCrewMember.gameObject;
                crewHandlerRoots.Add(handlerRoot);
                originalCrewPositions.Add(CalculateCrewMemberLocalPosition(i));
                originalCrewActiveStates.Add(true);
                
                ConfigureCrewMember(newCrewMember, i);
                
                if (debugCrewManager)
                    GameLogger.LogVerbose($"[CREW INSTANT SPAWN] {GetBoatID()} - Spawned crew member {i}: {newCrewMember.name}");
            }
        }
        
        ApplyRandomCrewDeactivationInstantly();
        
        InitializeFloaterWithActiveCrew();
        
        CalculateInitialIntegrity();
        
        AssignNavigator();
        
        currentActiveCrewCount = GetActiveCrewCount();
        
        if (debugCrewManager)
            GameLogger.LogVerbose($"[CREW INSTANT INIT COMPLETE] {GetBoatID()} - Total spawned: {allCrewMembers.Count}, Active: {currentActiveCrewCount}, Navigator: {(currentNavigator != null ? currentNavigator.name : "NONE")}");
        
        yield return null;
        
        if (boatController != null)
        {
            boatController.OnCrewInitializationComplete();
        }
    }
    
    private void ApplyRandomCrewDeactivationInstantly()
    {
        if (!allowCrewRandomDeactivation || maxCrewToDeactivate <= 0)
        {
            if (debugCrewManager)
                GameLogger.LogVerbose($"[CREW INSTANT DEACTIVATION] {GetBoatID()} - Random deactivation disabled or maxCrewToDeactivate is 0");
            return;
        }
        
        int crewToDeactivate = Random.Range(0, maxCrewToDeactivate + 1);
        
        int minRequired = Mathf.Max(minGuaranteedCrew, maxCrewSize - maxCrewToDeactivate);
        if (crewToDeactivate >= maxCrewSize)
        {
            crewToDeactivate = maxCrewSize - minRequired;
        }
        
        if (debugCrewManager)
            GameLogger.LogVerbose($"[CREW INSTANT DEACTIVATION] {GetBoatID()} - Will deactivate {crewToDeactivate} of {maxCrewSize} crew members instantly");
        
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < allCrewMembers.Count; i++)
        {
            availableIndices.Add(i);
        }
        
        for (int i = 0; i < crewToDeactivate && availableIndices.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, availableIndices.Count);
            int crewIndex = availableIndices[randomIndex];
            availableIndices.RemoveAt(randomIndex);
            
            if (crewIndex < crewHandlerRoots.Count)
            {
                crewHandlerRoots[crewIndex].SetActive(false);
                originalCrewActiveStates[crewIndex] = false;
                
                LogCrewStateChange(allCrewMembers[crewIndex], "INSTANT_RANDOM_DEACTIVATION", $"Index {crewIndex} deactivated during instant initialization");
                
                if (debugCrewManager)
                    GameLogger.LogVerbose($"[CREW INSTANT DEACTIVATION] {GetBoatID()} - Deactivated crew at index {crewIndex}: {allCrewMembers[crewIndex].name}");
            }
        }
        
        int finalActiveCount = GetActiveCrewCount();
        int expectedMin = maxCrewSize - maxCrewToDeactivate;
        int expectedMax = maxCrewSize;
        
        if (finalActiveCount < expectedMin || finalActiveCount > expectedMax)
        {
            GameLogger.LogError($"[CREW INSTANT DEACTIVATION ERROR] {GetBoatID()} - Active count {finalActiveCount} outside expected range [{expectedMin}-{expectedMax}]!");
        }
        else if (debugCrewManager)
        {
            GameLogger.LogVerbose($"[CREW INSTANT DEACTIVATION SUCCESS] {GetBoatID()} - Final active crew: {finalActiveCount} (range: {expectedMin}-{expectedMax})");
        }
    }
    
    private void CalculateInitialIntegrity()
    {
        float totalPowerLevel = 0f;
        float activePowerLevel = 0f;
        
        foreach (var crew in allCrewMembers)
        {
            if (crew != null)
            {
                totalPowerLevel += crew.PowerLevel;
                
                int crewIndex = allCrewMembers.IndexOf(crew);
                if (crewIndex >= 0 && crewIndex < crewHandlerRoots.Count && crewHandlerRoots[crewIndex].activeInHierarchy)
                {
                    activePowerLevel += crew.PowerLevel;
                }
            }
        }
        
        if (boatController != null)
        {
            boatController.SetInitialIntegrity(totalPowerLevel, activePowerLevel);
        }
        
        if (debugCrewManager)
            GameLogger.LogVerbose($"[CREW INSTANT INTEGRITY] {GetBoatID()} - Calculated Max: {totalPowerLevel}, Current: {activePowerLevel}");
    }
    
    private IEnumerator InitializeCrewWithDelay()
    {
        yield return new WaitForSeconds(0.1f);
        
        if (debugCrewManager)
            GameLogger.LogVerbose($"[CREW INIT] {GetBoatID()} - Starting crew spawning (max: {maxCrewSize})");
        
        for (int i = 0; i < maxCrewSize; i++)
        {
            Vector3 spawnPosition = CalculateCrewMemberWorldPosition(i);
            BoatLandEnemy newCrewMember = SpawnCrewMember(spawnPosition, i);
            
            if (newCrewMember != null)
            {
                allCrewMembers.Add(newCrewMember);
                GameObject handlerRoot = newCrewMember.transform.parent?.gameObject ?? newCrewMember.gameObject;
                crewHandlerRoots.Add(handlerRoot);
                originalCrewPositions.Add(CalculateCrewMemberLocalPosition(i));
                originalCrewActiveStates.Add(true);
                
                ConfigureCrewMember(newCrewMember, i);
                
                if (debugCrewManager)
                    GameLogger.LogVerbose($"[CREW SPAWN] {GetBoatID()} - Spawned crew member {i}: {newCrewMember.name}");
            }
            
            yield return new WaitForSeconds(crewSpawnDelay);
        }
        
        ApplyRandomCrewDeactivation();
        
        InitializeFloaterWithActiveCrew();
        
        currentActiveCrewCount = GetActiveCrewCount();
        
        if (debugCrewManager)
            GameLogger.LogVerbose($"[CREW INIT COMPLETE] {GetBoatID()} - Total spawned: {allCrewMembers.Count}, Active: {currentActiveCrewCount}");
    }
    
    private void ApplyRandomCrewDeactivation()
    {
        if (!allowCrewRandomDeactivation || maxCrewToDeactivate <= 0)
        {
            if (debugCrewManager)
                GameLogger.LogVerbose($"[CREW DEACTIVATION] {GetBoatID()} - Random deactivation disabled or maxCrewToDeactivate is 0");
            return;
        }
        
        int crewToDeactivate = Random.Range(0, maxCrewToDeactivate + 1);
        
        int minRequired = Mathf.Max(minGuaranteedCrew, maxCrewSize - maxCrewToDeactivate);
        if (crewToDeactivate >= maxCrewSize)
        {
            crewToDeactivate = maxCrewSize - minRequired;
        }
        
        if (debugCrewManager)
            GameLogger.LogVerbose($"[CREW DEACTIVATION] {GetBoatID()} - Will deactivate {crewToDeactivate} of {maxCrewSize} crew members (guaranteed active: {maxCrewSize - crewToDeactivate})");
        
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < allCrewMembers.Count; i++)
        {
            availableIndices.Add(i);
        }
        
        for (int i = 0; i < crewToDeactivate && availableIndices.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, availableIndices.Count);
            int crewIndex = availableIndices[randomIndex];
            availableIndices.RemoveAt(randomIndex);
            
            if (crewIndex < crewHandlerRoots.Count)
            {
                crewHandlerRoots[crewIndex].SetActive(false);
                originalCrewActiveStates[crewIndex] = false;
                
                LogCrewStateChange(allCrewMembers[crewIndex], "RANDOM_DEACTIVATION", $"Index {crewIndex} deactivated during initialization");
                
                if (debugCrewManager)
                    GameLogger.LogVerbose($"[CREW DEACTIVATION] {GetBoatID()} - Deactivated crew at index {crewIndex}: {allCrewMembers[crewIndex].name}");
            }
        }
        
        int finalActiveCount = GetActiveCrewCount();
        int expectedMin = maxCrewSize - maxCrewToDeactivate;
        int expectedMax = maxCrewSize;
        
        if (finalActiveCount < expectedMin || finalActiveCount > expectedMax)
        {
            GameLogger.LogError($"[CREW DEACTIVATION ERROR] {GetBoatID()} - Active count {finalActiveCount} outside expected range [{expectedMin}-{expectedMax}]!");
        }
        else if (debugCrewManager)
        {
            GameLogger.LogVerbose($"[CREW DEACTIVATION SUCCESS] {GetBoatID()} - Final active crew: {finalActiveCount} (range: {expectedMin}-{expectedMax})");
        }
    }
    
    private BoatLandEnemy SpawnCrewMember(Vector3 worldPosition, int spawnIndex)
    {
        if (crewMemberPrefab == null)
        {
            GameLogger.LogError($"[CREW SPAWN] {GetBoatID()} - No crew member prefab assigned!");
            return null;
        }
        
        GameObject crewHandlerObj = Instantiate(crewMemberPrefab, worldPosition, Quaternion.identity);
        crewHandlerObj.name = $"{crewMemberPrefab.name}_{spawnIndex:00}_{GetBoatID()}";
        crewHandlerObj.transform.SetParent(crewContainer);
        
        BoatLandEnemy boatFisherman = crewHandlerObj.GetComponentInChildren<BoatLandEnemy>();
        
        if (boatFisherman == null)
        {
            boatFisherman = crewHandlerObj.GetComponent<BoatLandEnemy>();
            
            if (boatFisherman == null)
            {
                GameLogger.LogError($"[CREW SPAWN] {GetBoatID()} - BoatLandEnemy component not found in prefab {crewMemberPrefab.name}!");
                
                Transform[] children = crewHandlerObj.GetComponentsInChildren<Transform>();
                foreach (Transform child in children)
                {
                    GameLogger.LogError($"  Child: {child.name} - Components: {string.Join(", ", child.GetComponents<MonoBehaviour>().Select(c => c.GetType().Name))}");
                }
                
                Destroy(crewHandlerObj);
                return null;
            }
        }
        
        HookSpawner hookSpawner = crewHandlerObj.GetComponentInChildren<HookSpawner>();
        if (hookSpawner == null)
        {
            GameLogger.LogWarning($"[CREW SPAWN] {GetBoatID()} - HookSpawner not found in {crewHandlerObj.name} - fisherman won't be able to throw hooks!");
        }
        
        if (debugCrewManager)
            GameLogger.LogVerbose($"[CREW SPAWN] {GetBoatID()} - {boatFisherman.name} spawned at: {worldPosition} from prefab: {crewMemberPrefab.name}");
        
        return boatFisherman;
    }
    
    private void ConfigureCrewMember(BoatLandEnemy boatFisherman, int spawnIndex)
    {
        if (boatFisherman == null)
        {
            GameLogger.LogError($"[CREW CONFIG] {GetBoatID()} - Cannot configure null crew member!");
            return;
        }
        
        GameObject handlerRoot = boatFisherman.transform.parent?.gameObject ?? boatFisherman.gameObject;
        
        if (debugCrewManager)
            GameLogger.LogVerbose($"[CREW CONFIG] {GetBoatID()} - Configuring {boatFisherman.name} (Handler: {handlerRoot.name})");
        
        boatFisherman.SetBoatID(boatID);
        boatFisherman.SetAssignedPlatform(boatPlatform);
        boatFisherman.OnPlatformAssigned(boatPlatform);
        
        boatFisherman.InitializeBoatContext(boatController, boatFloater, boatPlatform);
        
        boatFisherman.SetLocalBoundaries(calculatedLeftBoundary, calculatedRightBoundary);
        
        Vector3 targetLocalPosition = CalculateCrewMemberLocalPosition(spawnIndex);
        
        BoatCrewPhysics crewPhysics = boatFisherman.GetComponent<BoatCrewPhysics>();
        if (crewPhysics != null)
        {
            if (handlerRoot != boatFisherman.gameObject)
            {
                boatFisherman.JoinBoatCrewAsChild(crewContainer, targetLocalPosition);
            }
            else
            {
                boatFisherman.JoinBoatCrewAtPosition(crewContainer, targetLocalPosition);
            }
        }
        
        if (boatPlatform != null)
        {
            boatPlatform.RegisterEnemyAtRuntime(boatFisherman);
        }
        
        boatFisherman.OnEnemyDied += OnCrewMemberDied;
        
        if (debugCrewManager)
            GameLogger.LogVerbose($"[CREW CONFIG COMPLETE] {GetBoatID()} - {boatFisherman.name} configured and assigned to platform");
    }
    
    private Vector3 CalculateCrewMemberWorldPosition(int index)
    {
        Vector3 localPos = CalculateCrewMemberLocalPosition(index);
        return crewContainer.TransformPoint(localPos);
    }
    
    private Vector3 CalculateCrewMemberLocalPosition(int index)
    {
        float spacing = (calculatedRightBoundary - calculatedLeftBoundary) / (maxCrewSize + 1);
        float xPosition = calculatedLeftBoundary + (spacing * (index + 1));
        
        return new Vector3(xPosition, crewHeightAbovePlatform, 0f);
    }
    
    private void CalculatePlatformBounds()
    {
        if (boatPlatform?.PlatformCollider != null)
        {
            Bounds bounds = boatPlatform.PlatformCollider.bounds;
            calculatedLeftBoundary = bounds.min.x - crewContainer.position.x - 0.3f;
            calculatedRightBoundary = bounds.max.x - crewContainer.position.x + 0.3f;
            
            platformTopWorld = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            platformTopLocal = crewContainer.InverseTransformPoint(platformTopWorld);
            calculatedPlatformHeight = platformTopLocal.y;
        }
        
        if (debugCrewManager)
            GameLogger.LogVerbose($"[CREW BOUNDS] {GetBoatID()} - Platform bounds: Left={calculatedLeftBoundary:F2}, Right={calculatedRightBoundary:F2}, Height={calculatedPlatformHeight:F2}");
    }
    
    private void InitializeFloaterWithActiveCrew()
    {
        if (boatFloater != null)
        {
            List<Enemy> activeCrewAsEnemies = GetActiveCrewMembers().Cast<Enemy>().ToList();
            boatFloater.InitializeCrew(activeCrewAsEnemies);
            
            if (debugCrewManager)
                GameLogger.LogVerbose($"[CREW FLOATER] {GetBoatID()} - Floater initialized with {activeCrewAsEnemies.Count} active crew members");
        }
    }
    
    public void AssignNavigator()
    {
        Debug.Log($"[CREW DEBUG] {GetBoatID()} - AssignNavigator called. Current navigator: {(currentNavigator != null ? currentNavigator.name : "NULL")}");
        
        if (currentNavigator != null && IsValidCrewMember(currentNavigator))
        {
            Debug.Log($"[CREW DEBUG] {GetBoatID()} - Current navigator {currentNavigator.name} is still valid");
            return;
        }
        
        currentNavigator = null;
        
        List<BoatLandEnemy> availableCrew = GetActiveCrewMembers();
        Debug.Log($"[CREW DEBUG] {GetBoatID()} - Available crew count: {availableCrew.Count}");
        
        if (availableCrew.Count > 0)
        {
            List<BoatLandEnemy> validNavigators = new List<BoatLandEnemy>();
            
            foreach (var crew in availableCrew)
            {
                bool isValid = IsValidCrewMember(crew);
                bool isNavigating = crew.IsNavigating();
                Debug.Log($"[CREW DEBUG] {GetBoatID()} - Checking {crew.name}: Valid={isValid}, Navigating={isNavigating}, State={crew.State}, OnBoat={crew.isOnBoat}");
                
                if (isValid && !isNavigating)
                {
                    validNavigators.Add(crew);
                }
            }
            
            Debug.Log($"[CREW DEBUG] {GetBoatID()} - Valid navigators count: {validNavigators.Count}");
            
            if (validNavigators.Count > 0)
            {
                BoatLandEnemy selectedNavigator = validNavigators[Random.Range(0, validNavigators.Count)];
                Debug.Log($"[CREW DEBUG] {GetBoatID()} - Selected navigator: {selectedNavigator.name}");
                
                if (IsValidCrewMember(selectedNavigator))
                {
                    currentNavigator = selectedNavigator;
                    currentNavigator.AssignToWheel();
                    
                    Debug.Log($"[CREW DEBUG] {GetBoatID()} - Navigator assigned successfully: {selectedNavigator.name}, Role: {selectedNavigator.GetCrewRole()}, Navigating: {selectedNavigator.IsNavigating()}");
                    
                    if (boatController != null)
                    {
                        boatController.ChangeState_Driven();
                    }
                    
                    if (debugCrewManager)
                        GameLogger.LogVerbose($"[CREW NAVIGATOR] {GetBoatID()} - Navigator assigned successfully");
                }
                else
                {
                    Debug.LogWarning($"[CREW DEBUG] {GetBoatID()} - Selected navigator became invalid immediately after selection!");
                }
            }
            else
            {
                Debug.LogWarning($"[CREW DEBUG] {GetBoatID()} - No valid navigators available from {availableCrew.Count} active crew members");
            }
        }
        else
        {
            Debug.LogWarning($"[CREW DEBUG] {GetBoatID()} - No active crew members available for navigation");
        }
    }
    
    public void ReleaseNavigator()
    {
        if (currentNavigator != null)
        {
            Debug.Log($"[CREW DEBUG] {GetBoatID()} - Releasing navigator: {currentNavigator.name}");
            
            if (IsValidCrewMember(currentNavigator))
            {
                currentNavigator.ReleaseFromWheel();
            }
            
            if (boatController != null)
            {
                boatController.ChangeState_AutoMove();
            }
            
            currentNavigator = null;
            
            if (debugCrewManager)
                GameLogger.LogVerbose($"[CREW NAVIGATOR] {GetBoatID()} - Navigator released");
        }
        else
        {
            Debug.Log($"[CREW DEBUG] {GetBoatID()} - No navigator to release");
        }
    }
    
    private bool IsValidCrewMember(BoatLandEnemy crew)
    {
        return crew != null && crew.gameObject != null && crew.State == Enemy.EnemyState.Alive && crew.isOnBoat;
    }
    
    public List<BoatLandEnemy> GetAllCrewMembers()
    {
        return new List<BoatLandEnemy>(allCrewMembers);
    }
    
    public List<BoatLandEnemy> GetActiveCrewMembers()
    {
        List<BoatLandEnemy> activeList = new List<BoatLandEnemy>();
        
        for (int i = 0; i < allCrewMembers.Count; i++)
        {
            var crew = allCrewMembers[i];
            var handlerRoot = crewHandlerRoots[i];
            
            if (crew != null && handlerRoot != null && handlerRoot.activeInHierarchy && crew.State == Enemy.EnemyState.Alive)
            {
                activeList.Add(crew);
            }
        }
        
        return activeList;
    }
    
    public int GetActiveCrewCount()
    {
        int count = 0;
        for (int i = 0; i < allCrewMembers.Count; i++)
        {
            var crew = allCrewMembers[i];
            var handlerRoot = crewHandlerRoots[i];
            
            if (crew != null && handlerRoot != null && handlerRoot.activeInHierarchy && crew.State == Enemy.EnemyState.Alive)
            {
                count++;
            }
        }
        return count;
    }
    
    public void OnCrewMemberDied(Enemy deadCrew)
    {
        if (deadCrew is BoatLandEnemy boatEnemy)
        {
            LogCrewStateChange(boatEnemy, "DEATH_EVENT", "OnEnemyDied event triggered");
            HandleCrewMemberDeath(boatEnemy);
        }
        
        StartCoroutine(DelayedIntegrityRecalculation());
    }
    
    public void HandleCrewMemberDeath(BoatLandEnemy deadCrewMember)
    {
        if (!BoatEnemyBelongToBoat(deadCrewMember))
        {
            if (debugCrewManager)
                GameLogger.LogWarning($"[CREW DEATH] {GetBoatID()} - Crew member doesn't belong to this boat");
            return;
        }
        
        if (currentNavigator == deadCrewMember)
        {
            Debug.Log($"[CREW DEBUG] {GetBoatID()} - Current navigator {deadCrewMember.name} died, clearing currentNavigator");
            currentNavigator = null;
        }
        
        if (boatPlatform != null)
        {
            boatPlatform.UnregisterEnemy(deadCrewMember);
        }
        
        deadCrewMember.OnEnemyDied -= OnCrewMemberDied;
        
        int crewIndex = allCrewMembers.IndexOf(deadCrewMember);
        if (crewIndex >= 0 && crewIndex < crewHandlerRoots.Count)
        {
            crewHandlerRoots[crewIndex].SetActive(false);
            LogCrewStateChange(deadCrewMember, "DEATH_DEACTIVATION", $"Handler deactivated due to death at index {crewIndex}");
            
            if (debugCrewManager)
                GameLogger.LogVerbose($"[CREW DEATH] {GetBoatID()} - {deadCrewMember.name} handler deactivated. Remaining active: {GetActiveCrewCount()}");
        }
        
        StartCoroutine(DelayedIntegrityRecalculation());
    }
    
    private bool BoatEnemyBelongToBoat(BoatLandEnemy enemy)
    {
        return enemy != null && enemy.GetBoatID() == boatID.UniqueID;
    }
    
    private IEnumerator DelayedIntegrityRecalculation()
    {
        yield return new WaitForSeconds(0.1f);
        
        if (boatController != null)
        {
            boatController.RecalculateBoatIntegrity();
        }
    }
    
    public void Reset()
    {
        ReleaseNavigator();
        
        for (int i = 0; i < allCrewMembers.Count; i++)
        {
            if (allCrewMembers[i] != null)
            {
                allCrewMembers[i].ResetToOriginalState();
                
                if (i < originalCrewPositions.Count)
                {
                    BoatCrewPhysics crewPhysics = allCrewMembers[i].GetComponent<BoatCrewPhysics>();
                    if (crewPhysics != null)
                    {
                        crewPhysics.SetLocalPosition(originalCrewPositions[i]);
                    }
                }
                
                if (i < originalCrewActiveStates.Count && i < crewHandlerRoots.Count)
                {
                    crewHandlerRoots[i].SetActive(originalCrewActiveStates[i]);
                }
            }
        }
        
        currentActiveCrewCount = GetActiveCrewCount();
        
        if (debugCrewManager)
            GameLogger.LogVerbose($"[CREW RESET] {GetBoatID()} - All crew reset to original state. Active: {currentActiveCrewCount}");
    }
    
    private void LogCrewStateChange(BoatLandEnemy crew, string reason, string details)
    {
        if (!trackCrewDisappearance) return;
        
        bool isHandlerActive = crew.transform.parent?.gameObject.activeInHierarchy ?? crew.gameObject.activeInHierarchy;
        
        GameLogger.LogError($"🚨 [CREW STATE CHANGE] {crew.name} - REASON: {reason}");
        GameLogger.LogError($"    BOAT: {GetBoatID()}");
        GameLogger.LogError($"    DETAILS: {details}");
        GameLogger.LogError($"    CREW STATE: {crew.State}");
        GameLogger.LogError($"    ON BOAT: {crew.isOnBoat}");
        GameLogger.LogError($"    HANDLER ACTIVE: {isHandlerActive}");
        GameLogger.LogError($"    ACTIVE CREW COUNT: {GetActiveCrewCount()}/{allCrewMembers.Count}");
    }
    
    public void LogCurrentCrewStatus()
    {
        if (!logCrewStates) return;
        
        GameLogger.Log($"=== CREW STATUS FOR BOAT {GetBoatID()} ===");
        GameLogger.Log($"Total Crew: {allCrewMembers.Count}, Active: {GetActiveCrewCount()}");
        
        for (int i = 0; i < allCrewMembers.Count; i++)
        {
            var crew = allCrewMembers[i];
            var handler = i < crewHandlerRoots.Count ? crewHandlerRoots[i] : null;
            
            if (crew != null)
            {
                bool handlerActive = handler?.activeInHierarchy ?? false;
                GameLogger.Log($"  [{i}] {crew.name} - State: {crew.State}, OnBoat: {crew.isOnBoat}, Handler Active: {handlerActive}, Role: {crew.GetCrewRole()}, Navigating: {crew.IsNavigating()}");
            }
            else
            {
                GameLogger.Log($"  [{i}] NULL CREW MEMBER");
            }
        }
        GameLogger.Log($"=== END CREW STATUS ===");
    }
    
    private void Update()
    {
        int newActiveCount = GetActiveCrewCount();
        if (newActiveCount != currentActiveCrewCount)
        {
            if (trackCrewDisappearance)
            {
                GameLogger.LogError($"⚠️ [CREW COUNT CHANGE] {GetBoatID()} - Active crew changed from {currentActiveCrewCount} to {newActiveCount}");
                LogCurrentCrewStatus();
            }
            currentActiveCrewCount = newActiveCount;
        }
        
        if (Input.GetKeyDown(KeyCode.F7))
        {
            LogCurrentCrewStatus();
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showPlatformGizmos) return;
        
        Gizmos.color = Color.red;
        Vector3 leftBound = crewContainer.TransformPoint(new Vector3(calculatedLeftBoundary, crewHeightAbovePlatform, 0));
        Vector3 rightBound = crewContainer.TransformPoint(new Vector3(calculatedRightBoundary, crewHeightAbovePlatform, 0));
        
        Gizmos.DrawWireSphere(leftBound, 0.1f);
        Gizmos.DrawWireSphere(rightBound, 0.1f);
        Gizmos.DrawLine(leftBound, rightBound);
        
        Gizmos.color = Color.green;
        for (int i = 0; i < maxCrewSize; i++)
        {
            Vector3 spawnPos = CalculateCrewMemberWorldPosition(i);
            Gizmos.DrawWireSphere(spawnPos, 0.15f);
        }
    }
    
    public BoatLandEnemy GetCurrentNavigator()
    {
        return currentNavigator;
    }
    
    public bool HasActiveNavigator()
    {
        return currentNavigator != null && IsValidCrewMember(currentNavigator) && currentNavigator.IsNavigating();
    }
    
    public void ForceReleaseNavigator(BoatLandEnemy navigatorToRelease)
    {
        if (currentNavigator == navigatorToRelease)
        {
            if (debugCrewManager)
                GameLogger.LogVerbose($"[CREW NAVIGATOR] {GetBoatID()} - Force releasing navigator {navigatorToRelease.name}");
            
            currentNavigator = null;
            
            if (boatController != null)
            {
                boatController.ChangeState_AutoMove();
            }
        }
    }
    
    public void SetCrewMemberPrefab(GameObject prefab)
    {
        crewMemberPrefab = prefab;
    }
    
    public GameObject GetCrewMemberPrefab()
    {
        return crewMemberPrefab;
    }
    
    public bool IsCrewManagerInitialized()
    {
        return isInitialized;
    }
    
    public void SetMaxCrewSize(int newMaxSize)
    {
        maxCrewSize = Mathf.Max(1, newMaxSize);
    }
    
    public int GetMaxCrewSize()
    {
        return maxCrewSize;
    }
    
    public void SetMaxCrewToDeactivate(int newMaxToDeactivate)
    {
        maxCrewToDeactivate = Mathf.Clamp(newMaxToDeactivate, 0, maxCrewSize - 1);
    }
    
    public int GetMaxCrewToDeactivate()
    {
        return maxCrewToDeactivate;
    }
    
    public Transform GetCrewContainer()
    {
        return crewContainer;
    }
    
    public void SetCrewContainer(Transform container)
    {
        crewContainer = container;
    }
}
