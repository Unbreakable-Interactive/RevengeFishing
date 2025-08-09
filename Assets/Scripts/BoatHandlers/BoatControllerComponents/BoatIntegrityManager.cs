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
    
    private List<BoatLandEnemy> cachedCrewMembers;
    private int cachedActiveCrewCount = 0;
    private float cachedTotalPower = 0f;
    
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
        
        boatMovement = GetComponent<BoatMovementSystem>();
        boatVisualSystem = GetComponent<BoatVisualSystem>();
        
        RefreshCrewCache();
    }
    
    private void RefreshCrewCache()
    {
        if (crewManager == null) return;
        
        cachedCrewMembers = crewManager.GetAllCrewMembers();
    }
    
    public void CalculateBoatIntegrity()
    {
        if (crewManager == null) return;
        
        RefreshCrewCache();
        
        if (cachedCrewMembers == null || cachedCrewMembers.Count == 0)
        {
            cachedTotalPower = 0f;
            cachedActiveCrewCount = 0;
            maxIntegrity = 0f;
            currentIntegrity = 0f;
            return;
        }
        
        cachedTotalPower = 0f;
        cachedActiveCrewCount = 0;
        
        for (int i = 0; i < cachedCrewMembers.Count; i++)
        {
            if (IsCrewMemberActive(cachedCrewMembers[i]))
            {
                cachedTotalPower += cachedCrewMembers[i].PowerLevel;
                cachedActiveCrewCount++;
            }
        }
        
        maxIntegrity = cachedTotalPower;
        currentIntegrity = maxIntegrity;
        
        OnIntegrityChanged?.Invoke(currentIntegrity, maxIntegrity);
        
        GameLogger.LogVerbose($"BoatIntegrityManager: Calculated integrity - Active crew: {cachedActiveCrewCount}, Total power: {cachedTotalPower}");
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
        
        int totalCrewMembers = cachedCrewMembers?.Count ?? 0;
        
        if (totalCrewMembers == 0)
        {
            GameLogger.LogVerbose($"BoatIntegrityManager: No crew members found, skipping destruction check");
            return;
        }
        
        if (cachedActiveCrewCount == 0 && totalCrewMembers > 0)
        {
            GameLogger.LogVerbose($"BoatIntegrityManager: All crew inactive ({totalCrewMembers} total), triggering destruction");
            TriggerDestruction();
        }
        else if (currentIntegrity <= 0f && maxIntegrity > 0f)
        {
            GameLogger.LogVerbose($"BoatIntegrityManager: Zero integrity with max {maxIntegrity}, triggering destruction");
            TriggerDestruction();
        }
    }
    
    private void TriggerDestruction()
    {
        if (isBoatDestroyed) return;
        
        isBoatDestroyed = true;
        
        if (boatMovement != null)
        {
            boatMovement.DestroyState();
        }
        
        if (boatVisualSystem != null)
        {
            boatVisualSystem.DestroyEnemy(true);
        }
        
        BoatLifecycleManager lifecycleManager = GetComponent<BoatLifecycleManager>();
        if (lifecycleManager != null)
        {
            lifecycleManager.DestroyBoat();
        }
        
        GameLogger.LogVerbose("BoatIntegrityManager: Boat destroyed due to integrity loss");
    }
    
    public void Reset()
    {
        currentIntegrity = 0f;
        maxIntegrity = 0f;
        isBoatDestroyed = false;
        
        cachedActiveCrewCount = 0;
        cachedTotalPower = 0f;
        
        GameLogger.LogVerbose("BoatIntegrityManager: Reset completed");
    }
}
