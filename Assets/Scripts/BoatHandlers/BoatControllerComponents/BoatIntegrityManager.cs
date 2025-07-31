using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    
    private BoatCrewManager crewManager;
    
    public float CurrentIntegrity => currentIntegrity;
    public float MaxIntegrity => maxIntegrity;
    public bool IsDestroyed => isBoatDestroyed;
    
    public void Initialize()
    {
        // Get crew manager from same GameObject
        crewManager = GetComponent<BoatCrewManager>();
        if (crewManager == null)
        {
            Debug.LogError("BoatIntegrityManager: BoatCrewManager not found on same GameObject!");
        }
    }
    
    public void CalculateBoatIntegrity()
    {
        if (crewManager == null) return;
        
        float totalCrewPower = 0f;
        int activeCrewCount = 0;
        
        List<Fisherman> allCrewMembers = crewManager.GetAllCrewMembers();
        
        foreach (Fisherman crew in allCrewMembers)
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
    
    private bool IsCrewMemberActive(Fisherman crew)
    {
        return crew != null && 
               crew.ParentContainer != null && 
               crew.ParentContainer.activeInHierarchy && 
               crew.State == Enemy.EnemyState.Alive;
    }
    
    public void CheckBoatDestruction()
    {
        if (crewManager == null) return;
        
        // Filter by enemy Type 
        
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
        
        // Get lifecycle manager and trigger destruction
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
