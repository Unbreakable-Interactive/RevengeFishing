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
    
    private bool isInitialized = false;
    
    public Action<float, float> OnIntegrityChanged => integrityManager.OnIntegrityChanged;
    public Action OnBoatDestroyed => lifecycleManager.OnBoatDestroyed;
    
    public void Initialize(Transform _leftBoundary, Transform _rightBoundary)
    {
        leftBoundary = _leftBoundary;
        rightBoundary = _rightBoundary;
        
        integrityManager.Initialize();
        crewManager.Initialize(boatPlatform, integrityManager);
        lifecycleManager.Initialize(this);
        
        boatFloater.InitializeBoundaries(_leftBoundary, _rightBoundary);
        
        isInitialized = true;
        
        if (gameObject.activeInHierarchy)
        {
            StartCrewInitialization();
        }
    }
    
    void OnEnable()
    {
        boatFloater.Initialize();
        
        if (isInitialized)
        {
            StartCrewInitialization();
        }
    }
    
    private void StartCrewInitialization()
    {
        if (leftBoundary == null || rightBoundary == null)
        {
            Debug.LogError($"BoatController {gameObject.name}: Cannot start crew - boundaries not set! Call Initialize() first.");
            return;
        }
        
        crewManager.StartCrewInitialization();
    }
    
    public void ResetForPooling()
    {
        integrityManager.Reset();
        crewManager.Reset();
        lifecycleManager.Reset();
        isInitialized = false;
    }
    
    public float GetCurrentIntegrity() => integrityManager.CurrentIntegrity;
    public float GetMaxIntegrity() => integrityManager.MaxIntegrity;
    public System.Collections.Generic.List<Fisherman> GetAllCrewMembers() => crewManager.GetAllCrewMembers();
    public bool IsDestroyed() => integrityManager.IsDestroyed;
    
    internal BoatFloater BoatFloater => boatFloater;
    internal BoatPlatform BoatPlatform => boatPlatform;
}
