using UnityEngine;

public class BoatController : MonoBehaviour
{
    [Header("Required Managers - AUTO ASSIGNED")]
    [SerializeField] private BoatIntegrityManager integrityManager;
    [SerializeField] private BoatCrewManager crewManager;
    [SerializeField] private BoatLifecycleManager lifecycleManager;
    [SerializeField] private BoatFloater boatFloater;
    [SerializeField] private BoatPlatform boatPlatform;
    
    [Header("Boundary References")]
    [SerializeField] private BoatBoundaryTrigger leftBoundary;
    [SerializeField] private BoatBoundaryTrigger rightBoundary;
    
    private static readonly System.Collections.Generic.List<Enemy> tempEnemyList = new System.Collections.Generic.List<Enemy>();
    
    private void Awake()
    {
        CacheComponents();
    }
    
    private void CacheComponents()
    {
        // AUTO-ASSIGN COMPONENTS IF NOT SET
        if (integrityManager == null)
            integrityManager = GetComponent<BoatIntegrityManager>();
            
        if (crewManager == null)
            crewManager = GetComponent<BoatCrewManager>();
            
        if (lifecycleManager == null)
            lifecycleManager = GetComponent<BoatLifecycleManager>();
            
        if (boatFloater == null)
            boatFloater = GetComponent<BoatFloater>();
            
        if (boatPlatform == null)
            boatPlatform = GetComponentInChildren<BoatPlatform>();
    }
    
    public void Initialize(Transform leftBoundaryTransform, Transform rightBoundaryTransform)
    {
        boatFloater.Initialize();
        crewManager.Initialize(boatPlatform, this);
        integrityManager.Initialize(crewManager);
        lifecycleManager.Initialize(this);
        
        if (leftBoundaryTransform != null && rightBoundaryTransform != null)
        {
            boatFloater.InitializeBoundaries(leftBoundaryTransform, rightBoundaryTransform);
            
            crewManager.SetupBoundaries(leftBoundary, rightBoundary);
        }
        
        StartCrewInitialization();
        
        GameLogger.LogError($"[BOAT CONTROLLER] {gameObject.name} - All managers initialized");
    }
    
    public void StartCrewInitialization()
    {
        crewManager.StartCrewInitialization();
    }
    
    public void ResetBoat()
    {
        // ORCHESTRATE RESET - EACH MANAGER HANDLES ITS OWN RESET
        integrityManager.Reset();
        crewManager.Reset();
        lifecycleManager.Reset();
        
        GameLogger.LogError($"[BOAT CONTROLLER] {gameObject.name} - Boat reset completed");
    }
    
    public void DestroyBoat()
    {
        lifecycleManager.DestroyBoat();
    }
    
    #region Public Getters - PURE DELEGATION
    
    public float GetCurrentIntegrity() => integrityManager.CurrentIntegrity;
    public float GetMaxIntegrity() => integrityManager.MaxIntegrity;
    public bool IsDestroyed() => integrityManager.IsDestroyed;
    
    public System.Collections.Generic.List<Enemy> GetAllCrewMembers()
    {
        var boatCrew = crewManager.GetAllCrewMembers();
        tempEnemyList.Clear();
        
        for (int i = 0; i < boatCrew.Count; i++)
        {
            tempEnemyList.Add(boatCrew[i]);
        }
        
        return tempEnemyList;
    }
    
    internal BoatFloater BoatFloater => boatFloater;
    internal BoatPlatform BoatPlatform => boatPlatform;
    internal BoatPhysicsSystem BoatPhysicsSystem => boatFloater.GetComponent<BoatPhysicsSystem>();
    
    #endregion
}
