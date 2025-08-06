using System;
using System.Collections.Generic;
using UnityEngine;

public class BoatIntegrityManager : MonoBehaviour
{
    [Header("Integrity Properties")]
    [SerializeField] private float currentIntegrity;
    [SerializeField] private float maxIntegrity;
    [SerializeField] private bool isBoatDestroyed = false;
    
    public Action<float, float> OnIntegrityChanged;
    
    [SerializeField] private BoatCrewManager crewManager;
    
    [SerializeField] private BoatMovementSystem boatMovement;
    [SerializeField] private BoatVisualSystem boatVisualSystem;
    
    public float CurrentIntegrity => currentIntegrity;
    public float MaxIntegrity => maxIntegrity;
    public bool IsDestroyed => isBoatDestroyed;
    
    public void Initialize()
    {
        crewManager = GetComponent<BoatCrewManager>();
        if (crewManager == null)
        {
            GameLogger.LogError("BoatIntegrityManager: BoatCrewManager not found on same GameObject!");
        }
    }
    
    public void CalculateBoatIntegrity()
    {
        if (crewManager == null) return;
        
        float totalCrewPower = 0f;
        int activeCrewCount = 0;
        
        List<BoatLandEnemy> allCrewMembers = crewManager.GetAllCrewMembers();
        
        foreach (BoatLandEnemy crew in allCrewMembers)
        {
            if (IsCrewMemberActive(crew))
            {
                totalCrewPower += crew.PowerLevel;
                activeCrewCount++;
            }
        }
        
        maxIntegrity = totalCrewPower;
        currentIntegrity = maxIntegrity;
        
        OnIntegrityChanged?.Invoke(currentIntegrity, maxIntegrity);
    }
    
    private bool IsCrewMemberActive(BoatLandEnemy crew)
    {
        return crew != null && 
               crew.ParentContainer != null && 
               crew.ParentContainer.activeInHierarchy && 
               crew.State == Enemy.EnemyState.Alive;
    }
    
    public void CheckBoatDestruction()
    {
        if (crewManager == null) return;
        
        int aliveCrewCount = crewManager.GetActiveCrewCount();
        
        if (aliveCrewCount == 0 || currentIntegrity <= 0f)
        {
            TriggerDestruction();
        }
    }
    
    private void TriggerDestruction()
    {
        if (isBoatDestroyed) return;
        
        isBoatDestroyed = true;
        
        boatMovement.DestroyState();
        boatVisualSystem.DestroyEnemy(true);
        
        BoatLifecycleManager lifecycleManager = GetComponent<BoatLifecycleManager>();
        if (lifecycleManager != null)
        {
            lifecycleManager.DestroyBoat();
        }
    }
    
    public void Reset()
    {
        currentIntegrity = 0f;
        maxIntegrity = 0f;
        isBoatDestroyed = false;
    }
}
