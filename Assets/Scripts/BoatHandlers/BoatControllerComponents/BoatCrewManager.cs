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
    
    private void ValidateRequiredReferences()
    {
        if (boatPlatform == null)
            throw new System.Exception($"BoatCrewManager on {gameObject.name}: boatPlatform must be assigned in editor!");
            
        if (leftBoundary == null)
            throw new System.Exception($"BoatCrewManager on {gameObject.name}: leftBoundary must be assigned in editor!");
            
        if (rightBoundary == null)
            throw new System.Exception($"BoatCrewManager on {gameObject.name}: rightBoundary must be assigned in editor!");
    }
    
    public void Initialize(BoatPlatform platform, BoatIntegrityManager integrity)
    {
        integrityManager = integrity;
        boatFloater = GetComponent<BoatController>().BoatFloater;
        
        // Configurar IDs únicos para todos los componentes del barco
        ConfigureBoatComponentIDs();
        ConfigureBoatUniqueLayer();
    }
    
    private void ConfigureBoatComponentIDs()
    {
        // Asignar el mismo ID a todos los componentes del barco
        boatPlatform.SetBoatID(boatID);
        leftBoundary.SetBoatID(boatID);
        rightBoundary.SetBoatID(boatID);
        
        Debug.Log($"BoatCrewManager: Configured boat ID {boatID} for all components");
    }
    
    private void ConfigureBoatUniqueLayer()
    {
        int layerToUse = Mathf.Abs(boatID.UniqueID.GetHashCode()) % 16;
        if (layerToUse == 9) layerToUse = 12;
        if (layerToUse == 10) layerToUse = 13;
        
        gameObject.layer = layerToUse;
        
        if (boatPlatform != null)
        {
            boatPlatform.gameObject.layer = layerToUse;
            
            foreach (Transform child in boatPlatform.transform)
            {
                child.gameObject.layer = layerToUse;
            }
        }
        
        Debug.Log($"BoatCrewManager: {gameObject.name} configured with unique layer {layerToUse} and ID {boatID}");
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
        yield return null;
        
        allCrewMembers.Clear();
        
        for (int i = 0; i < maxCrewMembers; i++)
        {
            yield return StartCoroutine(InstantiateAndAssignCrewMember(crewSpawnPoints[0].position, i));
        }
        
        if (boatFloater != null)
        {
            boatFloater.InitializeCrew(allCrewMembers);
        }
        
        yield return null;
        
        RandomlyDeactivateCrewMembers();
        integrityManager.CalculateBoatIntegrity();
    }
    
    private IEnumerator InstantiateAndAssignCrewMember(Vector3 spawnPoint, int index)
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
            // CRÍTICO: Asignar ID del barco al fisherman
            fisherman.SetBoatID(boatID);
            
            if (PowerLevelScaler.Instance != null)
            {
                int powerLevel = PowerLevelScaler.Instance.CalculateEnemyPowerLevel();
                fisherman.SetPowerLevel(powerLevel);
            }
            
            SubscribeToCrewMember(fisherman);
            yield return StartCoroutine(AssignToBoatPlatform(fisherman));
            allCrewMembers.Add(fisherman);
            
            ConfigureFishermanForBoatLife(fisherman);
            ConfigureFishermanCollisions(fisherman);
        }
        else
        {
            Debug.LogError($"Instantiated BoatFishermanHandler doesn't contain Fisherman component!");
            Destroy(crewHandlerObj);
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
        }
    }
    
    private void ConfigureFishermanCollisions(LandEnemy fisherman)
    {
        SetLayerRecursively(fisherman.gameObject, gameObject.layer);
        fisherman.gameObject.layer = 10; // Mantener Layer 10 para BoatEnemy
        
        Collider2D fishermanCollider = fisherman.GetComponent<Collider2D>();
        if (fishermanCollider == null) return;
        
        StartCoroutine(DelayedCollisionSetup(fisherman, fishermanCollider));
    }
    
    private IEnumerator DelayedCollisionSetup(LandEnemy fisherman, Collider2D fishermanCollider)
    {
        yield return new WaitForSeconds(0.1f);
        
        // Encontrar todos los otros BoatCrewManagers
        BoatCrewManager[] allBoatManagers = FindObjectsOfType<BoatCrewManager>();
        
        foreach (BoatCrewManager otherBoatManager in allBoatManagers)
        {
            if (otherBoatManager == this) continue;
            
            // Ignorar colisiones con platform de otros barcos
            if (otherBoatManager.boatPlatform != null)
            {
                Collider2D otherPlatformCollider = otherBoatManager.boatPlatform.GetComponent<Collider2D>();
                if (otherPlatformCollider != null)
                {
                    Physics2D.IgnoreCollision(fishermanCollider, otherPlatformCollider, true);
                    Debug.Log($"BoatCrew: {fisherman.name} DISABLED collision with other boat platform {otherBoatManager.name}");
                }
            }
            
            Collider2D[] otherBoatColliders = otherBoatManager.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D otherCollider in otherBoatColliders)
            {
                if (otherCollider.gameObject.layer == otherBoatManager.gameObject.layer)
                {
                    Physics2D.IgnoreCollision(fishermanCollider, otherCollider, true);
                }
            }
        }
        
        // Habilitar colisión con nuestra propia plataforma
        if (boatPlatform != null)
        {
            Collider2D myPlatformCollider = boatPlatform.GetComponent<Collider2D>();
            if (myPlatformCollider != null)
            {
                Physics2D.IgnoreCollision(fishermanCollider, myPlatformCollider, false);
                Debug.Log($"BoatCrew: {fisherman.name} ENABLED collision with own platform {boatPlatform.name}");
            }
        }
    }
    
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        for (int i = 0; i < obj.transform.childCount; i++)
        {
            SetLayerRecursively(obj.transform.GetChild(i).gameObject, layer);
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
    
    private IEnumerator AssignToBoatPlatform(LandEnemy landEnemy)
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
        yield return null;
        
        foreach (Fisherman fisherman in allCrewMembers)
        {
            if (fisherman != null)
            {
                // Asegurar que mantenga el ID del barco
                fisherman.SetBoatID(boatID);
                
                if (fisherman.ParentContainer != null)
                {
                    fisherman.ParentContainer.SetActive(true);
                }
                
                fisherman.TriggerAlive();
                fisherman.ScheduleNextAction();
                
                SubscribeToCrewMember(fisherman);
                yield return StartCoroutine(AssignToBoatPlatform(fisherman));
                
                ConfigureFishermanForBoatLife(fisherman);
                ConfigureFishermanCollisions(fisherman);
            }
        }
        
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

    /// <summary>
    /// MÉTODO PRINCIPAL: Verificar si un LandEnemy pertenece a ESTE barco usando ID único
    /// </summary>
    public bool LandEnemyBelongToBoat(LandEnemy enemy)
    {
        if (enemy == null) return false;
        
        // Verificación directa por ID único
        if (enemy is IBoatComponent boatComponent)
        {
            return boatID.Matches(boatComponent.GetBoatID());
        }
        
        return false;
    }

    /// <summary>
    /// Método para regenerar ID (útil para testing/debugging)
    /// </summary>
    [ContextMenu("🔄 Generate New Boat ID")]
    public void GenerateNewBoatID()
    {
        boatID.GenerateNewID();
        Debug.Log($"Generated new boat ID: {boatID}");
        
        // Reconfigurar todos los componentes con el nuevo ID
        if (Application.isPlaying)
        {
            ConfigureBoatComponentIDs();
        }
    }
    
    /// <summary>
    /// Debug info sobre el barco
    /// </summary>
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
