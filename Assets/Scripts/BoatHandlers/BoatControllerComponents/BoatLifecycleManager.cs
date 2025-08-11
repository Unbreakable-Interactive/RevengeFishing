using System;
using System.Collections;
using UnityEngine;

public class BoatLifecycleManager : MonoBehaviour
{
    public Action OnBoatDestroyed;
    
    private BoatController boatController;
    private BoatCrewManager crewManager;
    
    public void Initialize(BoatController controller)
    {
        boatController = controller;
        crewManager = GetComponent<BoatCrewManager>();
    }
    
    public void DestroyBoat()
    {
        if (crewManager != null)
        {
            var allCrewMembers = crewManager.GetAllCrewMembers();
            foreach (BoatLandEnemy crew in allCrewMembers)
            {
                if (crew != null)
                {
                    crew.OnEnemyDied -= crewManager.OnCrewMemberDied;
                }
            }
        }
        
        OnBoatDestroyed?.Invoke();
        
        StartCoroutine(DestroyBoatDelayed());
    }
    
    private IEnumerator DestroyBoatDelayed()
    {
        // Add 3 pieces of boat to fall
        // Afet N seconds it dies (already done)
        
        yield return new WaitForSeconds(1f);
        
        if (SimpleObjectPool.Instance != null)
        {
            SimpleObjectPool.Instance.ReturnToPool("Boat", boatController.gameObject);
        }
        else
        {
            Destroy(boatController.gameObject);
        }
    }
    
    public void Reset()
    {
        // Reset any lifecycle-specific state
        // This method is called when the boat is reset for pooling
    }
}