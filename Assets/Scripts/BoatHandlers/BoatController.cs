using UnityEngine;

public class BoatController : MonoBehaviour
{
    [Header("Required Managers")]
    [SerializeField] private BoatIntegrityManager integrityManager;
    [SerializeField] private BoatCrewManager crewManager;
    [SerializeField] private BoatLifecycleManager lifecycleManager;
    [SerializeField] private BoatFloater boatFloater;
    [SerializeField] private BoatPlatform boatPlatform;
    
    [Header("Boundary References")]
    [SerializeField] private BoatBoundaryTrigger leftBoundary;
    [SerializeField] private BoatBoundaryTrigger rightBoundary;
    
    private void Awake()
    {
        ValidateComponents();
    }
    
    private void ValidateComponents()
    {
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
        // Initialize all boat systems
        boatFloater.Initialize();
        crewManager.Initialize(boatPlatform, integrityManager);
        integrityManager.Initialize();
        lifecycleManager.Initialize(this);
        
        // Set up boundaries
        if (leftBoundaryTransform != null && rightBoundaryTransform != null)
        {
            boatFloater.InitializeBoundaries(leftBoundaryTransform, rightBoundaryTransform);
            
            leftBoundary = leftBoundaryTransform.GetComponent<BoatBoundaryTrigger>();
            rightBoundary = rightBoundaryTransform.GetComponent<BoatBoundaryTrigger>();
        }
        
        StartCrewInitialization();
    }
    
    public void StartCrewInitialization()
    {
        crewManager.StartCrewInitialization();
    }
    
    public void ResetBoat()
    {
        integrityManager.Reset();
        crewManager.Reset();
        lifecycleManager.Reset();
    }
    
    public void DestroyBoat()
    {
        lifecycleManager.DestroyBoat();
    }
    
    public float GetCurrentIntegrity() => integrityManager.CurrentIntegrity;
    public float GetMaxIntegrity() => integrityManager.MaxIntegrity;
    
    public System.Collections.Generic.List<Enemy> GetAllCrewMembers()
    {
        var boatCrew = crewManager.GetAllCrewMembers();
        var enemyCrew = new System.Collections.Generic.List<Enemy>();
        foreach (var crew in boatCrew)
        {
            enemyCrew.Add((Enemy)crew);
        }
        return enemyCrew;
    }
    
    public bool IsDestroyed() => integrityManager.IsDestroyed;
    
    internal BoatFloater BoatFloater => boatFloater;
    internal BoatPlatform BoatPlatform => boatPlatform;
}
