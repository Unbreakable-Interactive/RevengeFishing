using System;
using UnityEngine;

public class BoatController : MonoBehaviour
{
    [Header("Core Components")]
    [SerializeField] private BoatIntegrityManager integrityManager;
    [SerializeField] private BoatCrewManager crewManager;
    [SerializeField] private BoatLifecycleManager lifecycleManager;
    
    [Header("Boat References")]
    [SerializeField] private BoatFloater boatFloater;
    [SerializeField] private BoatPlatform boatPlatform;
    
    [Header("Boundaries")]
    [SerializeField] private Transform leftBoundary;
    [SerializeField] private Transform rightBoundary;
    
    // Public Events (expuestos desde los managers)
    public Action<float, float> OnIntegrityChanged => integrityManager.OnIntegrityChanged;
    public Action OnBoatDestroyed => lifecycleManager.OnBoatDestroyed;
    
    public void Initialize(Transform _leftBoundary, Transform _rightBoundary)
    {
        leftBoundary = _leftBoundary;
        rightBoundary = _rightBoundary;
        
        // Initialize components in order
        integrityManager.Initialize();
        crewManager.Initialize(boatPlatform, integrityManager);
        lifecycleManager.Initialize(this);
        
        boatFloater.InitializeBoundaries(_leftBoundary, _rightBoundary);
    }
    
    void OnEnable()
    {
        boatFloater.Initialize();
        crewManager.StartCrewInitialization();
    }
    
    public void ResetForPooling()
    {
        integrityManager.Reset();
        crewManager.Reset();
        lifecycleManager.Reset();
    }
    
    // Public API methods
    public float GetCurrentIntegrity() => integrityManager.CurrentIntegrity;
    public float GetMaxIntegrity() => integrityManager.MaxIntegrity;
    public System.Collections.Generic.List<Fisherman> GetAllCrewMembers() => crewManager.GetAllCrewMembers();
    public bool IsDestroyed() => integrityManager.IsDestroyed;
    
    internal BoatFloater BoatFloater => boatFloater;
    internal BoatPlatform BoatPlatform => boatPlatform;
}
